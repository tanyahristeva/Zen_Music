using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Zen_Music
{
    public partial class AddArtistPage : Window
    {
        private string _selectedImagePath = "";

        private List<int> _selectedSongIds = new List<int>();
        private List<int> _selectedAlbumIds = new List<int>();

        public AddArtistPage()
        {
            InitializeComponent();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void buttonUpload_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Profile Picture for Artist",
                Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (ofd.ShowDialog() == true)
            {
                _selectedImagePath = ofd.FileName;

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(_selectedImagePath);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();

                imageArtist.Source = bmp;
            }
        }

        private void buttonSelectSongs_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Song selection dialog – coming soon.",
                            "Select Songs", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void buttonSelectAlbums_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Album selection dialog – coming soon.",
                            "Select Albums", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            string artistName = textBoxArtistName.Text.Trim();
            string description = textBoxDescription.Text.Trim();

            if (string.IsNullOrWhiteSpace(artistName))
            {
                MessageBox.Show("Please enter an Artist name!", "Required Field",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
                            string insertArtist = @"
                            INSERT INTO Artists (Name, Bio, Image_URL)
                            OUTPUT INSERTED.ID
                            VALUES (@Name, @Bio, @ImageURL)";

                            int newArtistId;
                            using (SqlCommand cmd = new SqlCommand(insertArtist, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@Name", artistName);
                                cmd.Parameters.AddWithValue("@Bio", string.IsNullOrWhiteSpace(description)
                                                                              ? (object)DBNull.Value
                                                                              : description);
                                cmd.Parameters.AddWithValue("@ImageURL", string.IsNullOrWhiteSpace(_selectedImagePath)
                                             ? (object)DBNull.Value
                                             : _selectedImagePath);
                                newArtistId = (int)cmd.ExecuteScalar();
                            }

                            foreach (int songId in _selectedSongIds)
                            {
                                using (SqlCommand cmd = new SqlCommand(
                                    "INSERT INTO SongArtists (Song_ID, Artist_ID, Role) VALUES (@S, @A, 'Main')",
                                    conn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@S", songId);
                                    cmd.Parameters.AddWithValue("@A", newArtistId);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            foreach (int albumId in _selectedAlbumIds)
                            {
                                using (SqlCommand cmd = new SqlCommand(
                                    "UPDATE Albums SET Artist_ID = @A WHERE ID = @Al",
                                    conn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@A", newArtistId);
                                    cmd.Parameters.AddWithValue("@Al", albumId);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            tx.Commit();
                            MessageBox.Show($"Artist '{artistName}' has been added successfully!",
                                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                            this.DialogResult = true;
                            this.Close();
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
                MessageBox.Show("An error occurred while saving the artist:\n\n" + ex.Message,
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