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
    public partial class UpdateSongSearchPage : Window
    {
        public class SongItem
        {
            public int SongId { get; set; }
            public int RowIndex { get; set; }
            public string Title { get; set; }
            public string Artist { get; set; }
            public string Album { get; set; }
            public string Duration { get; set; }
            public BitmapImage Cover { get; set; }
        }

        private List<SongItem> _allSongs = new List<SongItem>();

        public UpdateSongSearchPage()
        {
            InitializeComponent();
            LoadSongs();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
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
                        SELECT s.ID, s.Title, s.Duration_Sec, s.Image_Data,
                               ar.Name AS ArtistName,
                               al.Title AS AlbumTitle
                        FROM Songs s
                        LEFT JOIN SongArtists sa ON sa.Song_ID = s.ID AND sa.Role = 'Main'
                        LEFT JOIN Artists ar ON ar.ID = sa.Artist_ID
                        LEFT JOIN Albums al ON al.ID = s.Album_ID
                        ORDER BY s.Title";

                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        int idx = 1;
                        foreach (DataRow row in dt.Rows)
                        {
                            int dur = row["Duration_Sec"] != DBNull.Value
                                ? Convert.ToInt32(row["Duration_Sec"]) : 0;
                            string duration = $"{dur / 60}:{dur % 60:D2}";

                            _allSongs.Add(new SongItem
                            {
                                SongId = Convert.ToInt32(row["ID"]),
                                RowIndex = idx++,
                                Title = row["Title"].ToString(),
                                Artist = row["ArtistName"] != DBNull.Value
                                         ? row["ArtistName"].ToString() : "",
                                Album = row["AlbumTitle"] != DBNull.Value
                                        ? row["AlbumTitle"].ToString() : "",
                                Duration = duration,
                                Cover = LoadImageFromBytes(row["Image_Data"] != DBNull.Value
                                        ? (byte[])row["Image_Data"] : null)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading songs: " + ex.Message,
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            PopulateList(_allSongs);
        }

        private void PopulateList(List<SongItem> songs)
        {
            listViewSongs.ItemsSource = null;
            listViewSongs.ItemsSource = songs;
        }

        private void textBoxSearch_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            string q = textBoxSearch.Text.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(q)) { PopulateList(_allSongs); return; }
            PopulateList(_allSongs.FindAll(s =>
                s.Title.ToLower().Contains(q) ||
                s.Artist.ToLower().Contains(q) ||
                s.Album.ToLower().Contains(q)));
        }

        private void UpdateRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is int id)
                OpenUpdateForm(id);
        }

        private void buttonUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (listViewSongs.SelectedItem is SongItem selected)
                OpenUpdateForm(selected.SongId);
            else
                MessageBox.Show("Please select a song to update!", "Required",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void OpenUpdateForm(int songId)
        {
            var updatePage = new UpdateSongPage(songId);
            updatePage.ShowDialog();
            LoadSongs();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private static BitmapImage LoadImageFromBytes(byte[] data)
        {
            if (data == null || data.Length == 0) return null;
            try
            {
                using (var ms = new MemoryStream(data))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    return image;
                }
            }
            catch { return null; }
        }
    }
}