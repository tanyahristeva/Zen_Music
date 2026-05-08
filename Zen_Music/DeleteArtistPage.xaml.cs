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
    public partial class DeleteArtistPage : Window
    {
        public class ArtistItem
        {
            public int ArtistId { get; set; }
            public int RowIndex { get; set; }
            public string Name { get; set; }
            public string Since { get; set; }   // "since YYYY"
            public BitmapImage Photo { get; set; }

            // Застраховка за правилно показване, ако се ползва ComboBox/List
            public override string ToString() => Name;
        }

        private List<ArtistItem> _allArtists = new List<ArtistItem>();


        public DeleteArtistPage()
        {
            InitializeComponent();
            LoadArtists();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        // ── Зарежда всички артисти ───────────────────────────────────────────
        private void LoadArtists()
        {
            _allArtists.Clear();
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    // ПРОМЯНА: Търсим Image_URL вместо Image_Data
                    string query = @"
                        SELECT a.ID, a.Name, a.Image_URL,
                               MIN(al.Release_Date) AS SinceYear
                        FROM Artists a
                        LEFT JOIN AlbumArtists aa ON aa.Artist_ID = a.ID
                        LEFT JOIN Albums al        ON al.ID = aa.Album_ID
                        GROUP BY a.ID, a.Name, a.Image_URL
                        ORDER BY a.Name";

                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        int idx = 1;
                        foreach (DataRow row in dt.Rows)
                        {
                            string since = row["SinceYear"] != DBNull.Value
                                ? $"since {row["SinceYear"]}"
                                : "";

                            // Взимаме пътя към снимката, ако има такъв
                            string imagePath = row["Image_URL"] != DBNull.Value
                                ? row["Image_URL"].ToString()
                                : null;

                            _allArtists.Add(new ArtistItem
                            {
                                ArtistId = Convert.ToInt32(row["ID"]),
                                RowIndex = idx++,
                                Name = row["Name"].ToString(),
                                Since = since,
                                Photo = LoadImageFromPath(imagePath) // Зареждаме от пътя
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading artists: " + ex.Message,
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            PopulateList(_allArtists);
        }

        private void PopulateList(List<ArtistItem> artists)
        {
            listViewArtists.ItemsSource = null;
            listViewArtists.ItemsSource = artists;
        }

        // ── Реално-времево търсене ───────────────────────────────────────────
        private void textBoxSearch_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            string q = textBoxSearch.Text.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(q)) { PopulateList(_allArtists); return; }

            PopulateList(_allArtists.FindAll(a => a.Name.ToLower().Contains(q)));
        }

        // ── Изтриване чрез иконата в реда ───────────────────────────────────
        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is int id)
            {
                ArtistItem artist = _allArtists.Find(a => a.ArtistId == id);
                if (artist != null) ConfirmAndDelete(artist.ArtistId, artist.Name);
            }
        }

        // ── Изтриване на селектирания ред чрез бутона Delete ─────────────────
        private void buttonDelete_Click(object sender, RoutedEventArgs e)
        {
            if (listViewArtists.SelectedItem is ArtistItem selected)
                ConfirmAndDelete(selected.ArtistId, selected.Name);
            else
                MessageBox.Show("Please select an artist to delete!", "Required",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // ── Обща логика ──────────────────────────────────────────────────────
        private void ConfirmAndDelete(int artistId, string name)
        {
            var result = MessageBox.Show(
                $"Are you absolutely sure you want to delete '{name}'?\n\nThis action cannot be undone!",
                "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);

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
                                "FollowArtists", "AlbumArtists", "SongArtists"
                            };
                            foreach (string table in related)
                            {
                                using (SqlCommand cmd = new SqlCommand(
                                    $"DELETE FROM {table} WHERE Artist_ID = @Id", conn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@Id", artistId);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            using (SqlCommand cmd = new SqlCommand(
                                "DELETE FROM Artists WHERE ID = @Id", conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@Id", artistId);
                                cmd.ExecuteNonQuery();
                            }

                            tx.Commit();
                            MessageBox.Show("Artist deleted successfully!", "Deleted",
                                            MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadArtists();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting artist:\n" + ex.Message,
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // ПРОМЯНА: Нов метод за зареждане на снимки от път (string)
        private static BitmapImage LoadImageFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(path);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();

                return image;
            }
            catch
            {
                return null;
            }
        }
    }
}