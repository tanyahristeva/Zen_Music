using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Zen_Music
{
    public partial class DeleteSongPage : Window
    {
        public class SongItem
        {
            public int SongId { get; set; }
            public int RowIndex { get; set; }
            public string Title { get; set; }
            public string ArtistName { get; set; }
            public string AlbumName { get; set; }
            public string Duration { get; set; }
            public BitmapImage CoverImage { get; set; }
        }

        private List<SongItem> _allSongs = new List<SongItem>();

        public DeleteSongPage()
        {
            InitializeComponent();
            LoadSongs();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void LoadSongs()
        {
            _allSongs.Clear();

            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    string query = @"
                        SELECT s.ID, s.Image_Data, s.Title,
                               ISNULL(a.Name,  'Unknown')  AS ArtistName,
                               ISNULL(al.Title, '')         AS AlbumName,
                               ISNULL(g.Name,  'No Genre') AS GenreName,
                               s.Duration_Sec
                        FROM Songs s
                        LEFT JOIN SongArtists sa ON sa.Song_ID = s.ID AND sa.Role = 'Main'
                        LEFT JOIN Artists     a  ON a.ID  = sa.Artist_ID
                        LEFT JOIN Albums      al ON al.ID = s.Album_ID
                        LEFT JOIN SongGenres  sg ON sg.Song_ID = s.ID
                        LEFT JOIN Genres      g  ON g.ID  = sg.Genre_ID
                        ORDER BY s.Title";

                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        int index = 1;
                        foreach (DataRow row in dt.Rows)
                        {
                            int dur = Convert.ToInt32(row["Duration_Sec"]);

                            var item = new SongItem
                            {
                                SongId = Convert.ToInt32(row["ID"]),
                                RowIndex = index++,
                                Title = row["Title"].ToString(),
                                ArtistName = row["ArtistName"].ToString(),
                                AlbumName = row["AlbumName"].ToString(),
                                Duration = $"{dur / 60}:{dur % 60:D2}",
                                CoverImage = LoadBitmapFromBytes(row["Image_Data"] as byte[])
                            };

                            _allSongs.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading songs: " + ex.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            PopulateList(_allSongs);
        }

        private static BitmapImage LoadBitmapFromBytes(byte[] data)
        {
            if (data == null || data.Length == 0) return null;
            try
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = ms;
                    bmp.EndInit();
                    bmp.Freeze();
                    return bmp;
                }
            }
            catch { return null; }
        }

        private void PopulateList(List<SongItem> songs)
        {
            listViewSongs.ItemsSource = null;
            listViewSongs.ItemsSource = songs;
        }

        private void textBoxSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string search = textBoxSearch.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(search))
            {
                PopulateList(_allSongs);
                return;
            }

            var filtered = _allSongs.FindAll(s =>
                s.Title.ToLower().Contains(search) ||
                s.ArtistName.ToLower().Contains(search));

            PopulateList(filtered);
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is int songId)
            {
                SongItem song = _allSongs.Find(s => s.SongId == songId);
                if (song == null) return;
                ConfirmAndDelete(songId, song.Title);
            }
        }

        private void buttonDelete_Click(object sender, RoutedEventArgs e)
        {
            if (listViewSongs.SelectedItem is SongItem selected)
            {
                ConfirmAndDelete(selected.SongId, selected.Title);
            }
            else
            {
                MessageBox.Show("Please select a song to delete!",
                                "Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ConfirmAndDelete(int songId, string title)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete '{title}'?\n\nThis will remove it from all playlists and history!",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    conn.Open();
                    using (SqlTransaction tx = conn.BeginTransaction())
                    {
                        try
                        {
                            string[] related = {
                                "SongArtists", "SongGenres", "PlaylistSongs",
                                "Likes", "Downloads", "Plays"
                            };

                            foreach (string table in related)
                            {
                                using (SqlCommand cmd = new SqlCommand(
                                    $"DELETE FROM {table} WHERE Song_ID = @Id", conn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@Id", songId);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            using (SqlCommand cmd = new SqlCommand(
                                "DELETE FROM Songs WHERE ID = @Id", conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@Id", songId);
                                cmd.ExecuteNonQuery();
                            }

                            tx.Commit();
                            MessageBox.Show($"'{title}' was successfully deleted.",
                                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadSongs();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during deletion:\n" + ex.Message,
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}