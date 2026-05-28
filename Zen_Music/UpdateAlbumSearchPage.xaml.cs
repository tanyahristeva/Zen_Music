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
    public partial class UpdateAlbumSearchPage : Window
    {
        public class AlbumItem
        {
            public int AlbumId { get; set; }
            public int RowIndex { get; set; }
            public string Title { get; set; }
            public string Artist { get; set; }
            public string Year { get; set; }
            public BitmapImage Cover { get; set; }
        }

        private List<AlbumItem> _allAlbums = new List<AlbumItem>();

        public UpdateAlbumSearchPage()
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
                    string query = @"
                        SELECT a.ID, a.Title, a.Release_Date, a.Cover_URL,
                               ar.Name AS ArtistName
                        FROM Albums a
                        LEFT JOIN AlbumArtists aa ON aa.Album_ID = a.ID
                        LEFT JOIN Artists ar ON ar.ID = aa.Artist_ID
                        ORDER BY a.Title";

                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        int idx = 1;
                        foreach (DataRow row in dt.Rows)
                        {
                            string year = row["Release_Date"] != DBNull.Value
                                ? ((DateTime)row["Release_Date"]).Year.ToString() : "";

                            string coverPath = row["Cover_URL"] != DBNull.Value
                                ? row["Cover_URL"].ToString() : null;

                            _allAlbums.Add(new AlbumItem
                            {
                                AlbumId = Convert.ToInt32(row["ID"]),
                                RowIndex = idx++,
                                Title = row["Title"].ToString(),
                                Artist = row["ArtistName"] != DBNull.Value
                                         ? row["ArtistName"].ToString() : "",
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
            PopulateList(_allAlbums.FindAll(a =>
                a.Title.ToLower().Contains(q) ||
                a.Artist.ToLower().Contains(q)));
        }

        private void UpdateRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is int id)
            {
                AlbumItem album = _allAlbums.Find(a => a.AlbumId == id);
                if (album != null) OpenUpdateForm(album.AlbumId);
            }
        }

        private void buttonUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (listViewAlbums.SelectedItem is AlbumItem selected)
                OpenUpdateForm(selected.AlbumId);
            else
                MessageBox.Show("Please select an album to update!", "Required",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void OpenUpdateForm(int albumId)
        {
            var updatePage = new UpdateAlbumPage(albumId);
            updatePage.ShowDialog();
            LoadAlbums();
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