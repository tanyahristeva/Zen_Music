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
    public partial class UpdateArtistSearchPage : Window
    {
        public class ArtistListItem
        {
            public int ArtistId { get; set; }
            public int RowIndex { get; set; }
            public string Name { get; set; }
            public string Since { get; set; }
            public BitmapImage Photo { get; set; }
        }

        private List<ArtistListItem> _allArtists = new List<ArtistListItem>();

        public UpdateArtistSearchPage()
        {
            InitializeComponent();
            LoadArtists();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void LoadArtists()
        {
            _allArtists.Clear();
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    string query = @"
                        SELECT a.ID, a.Name, a.Image_URL,
                               COUNT(DISTINCT aa.Album_ID) AS AlbumCount,
                               COUNT(DISTINCT sa.Song_ID)  AS SongCount
                        FROM Artists a
                        LEFT JOIN AlbumArtists aa ON aa.Artist_ID = a.ID
                        LEFT JOIN SongArtists  sa ON sa.Artist_ID = a.ID
                        GROUP BY a.ID, a.Name, a.Image_URL
                        ORDER BY a.Name";

                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        int idx = 1;
                        foreach (DataRow row in dt.Rows)
                        {
                            string since = $"{Convert.ToInt32(row["AlbumCount"])} album{(Convert.ToInt32(row["AlbumCount"]) == 1 ? "" : "s")}  •  {Convert.ToInt32(row["SongCount"])} song{(Convert.ToInt32(row["SongCount"]) == 1 ? "" : "s")}";

                            string imagePath = row["Image_URL"] != DBNull.Value
                                ? row["Image_URL"].ToString() : null;

                            _allArtists.Add(new ArtistListItem
                            {
                                ArtistId = Convert.ToInt32(row["ID"]),
                                RowIndex = idx++,
                                Name = row["Name"].ToString(),
                                Since = since,
                                Photo = LoadImageFromPath(imagePath)
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

        private void PopulateList(List<ArtistListItem> artists)
        {
            listViewArtists.ItemsSource = null;
            listViewArtists.ItemsSource = artists;
        }

        private void textBoxSearch_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            string q = textBoxSearch.Text.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(q)) { PopulateList(_allArtists); return; }
            PopulateList(_allArtists.FindAll(a => a.Name.ToLower().Contains(q)));
        }

        private void UpdateRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is int id)
                OpenUpdateForm(id);
        }

        private void buttonUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (listViewArtists.SelectedItem is ArtistListItem selected)
                OpenUpdateForm(selected.ArtistId);
            else
                MessageBox.Show("Please select an artist to update!", "Required",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void OpenUpdateForm(int artistId)
        {
            var updatePage = new UpdateArtistPage(artistId);
            updatePage.ShowDialog();
            LoadArtists();
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