using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Zen_Music.AlbumPages
{
    public partial class DeleteAlbumPage : Window
    {
        public class AlbumItem
        {
            public int AlbumId { get; set; }
            public int RowIndex { get; set; }
            public string Title { get; set; }
            public string Year { get; set; }
            public BitmapImage Cover { get; set; }
            public override string ToString() => Title;
        }

        private List<AlbumItem> _allAlbums = new List<AlbumItem>();

        public DeleteAlbumPage()
        {
            InitializeComponent();
            LoadAlbums();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void LoadAlbums()
        {
            _allAlbums.Clear();
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    string query = "SELECT ID, Title, Release_Date, Cover_URL FROM Albums ORDER BY Title";
                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        int idx = 1;
                        foreach (DataRow row in dt.Rows)
                        {
                            string year = row["Release_Date"] != DBNull.Value
                                ? $"since {((DateTime)row["Release_Date"]).Year}"
                                : "";

                            string coverPath = row["Cover_URL"] != DBNull.Value
                                ? row["Cover_URL"].ToString() : null;

                            _allAlbums.Add(new AlbumItem
                            {
                                AlbumId = Convert.ToInt32(row["ID"]),
                                RowIndex = idx++,
                                Title = row["Title"].ToString(),
                                Year = year,
                                Cover = LoadImageFromPath(coverPath)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading albums: " + ex.Message,
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            PopulateList(_allAlbums);
        }

        private void PopulateList(List<AlbumItem> albums)
        {
            listViewAlbums.ItemsSource = null;
            listViewAlbums.ItemsSource = albums;
        }

        private void textBoxSearch_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            string q = textBoxSearch.Text.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(q)) { PopulateList(_allAlbums); return; }
            PopulateList(_allAlbums.FindAll(a => a.Title.ToLower().Contains(q)));
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is int id)
            {
                AlbumItem album = _allAlbums.Find(a => a.AlbumId == id);
                if (album != null) ConfirmAndDelete(album.AlbumId, album.Title);
            }
        }

        private void buttonDelete_Click(object sender, RoutedEventArgs e)
        {
            if (listViewAlbums.SelectedItem is AlbumItem selected)
                ConfirmAndDelete(selected.AlbumId, selected.Title);
            else
                MessageBox.Show("Please select an album to delete!", "Required",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ConfirmAndDelete(int albumId, string title)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete '{title}'?\n\nThis action cannot be undone!",
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
                            // Изтриваме свързаните записи първо
                            foreach (string table in new[] { "AlbumArtists" })
                            {
                                using (SqlCommand cmd = new SqlCommand(
                                    $"DELETE FROM {table} WHERE Album_ID = @Id", conn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@Id", albumId);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            using (SqlCommand cmd = new SqlCommand(
                                "DELETE FROM Albums WHERE ID = @Id", conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@Id", albumId);
                                cmd.ExecuteNonQuery();
                            }

                            tx.Commit();
                            MessageBox.Show("Album deleted successfully!", "Deleted",
                                            MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadAlbums();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting album:\n" + ex.Message,
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private static BitmapImage LoadImageFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(path);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                return image;
            }
            catch { return null; }
        }
    }
}