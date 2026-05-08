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

        // Списъци с избрани ID-та (попълват се от диалозите)
        private List<int> _selectedSongIds = new List<int>();
        private List<int> _selectedAlbumIds = new List<int>();

        public AddArtistPage()
        {
            InitializeComponent();
        }

        // ── Drag на прозореца ────────────────────────────────────────────────
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        // ── Качване на снимка ────────────────────────────────────────────────
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

        // ── Избор на песни (placeholder – свърже се с реален диалог) ─────────
        private void buttonSelectSongs_Click(object sender, RoutedEventArgs e)
        {
            // TODO: отвори диалог за избор на песни и попълни _selectedSongIds
            MessageBox.Show("Song selection dialog – coming soon.",
                            "Select Songs", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ── Избор на албуми (placeholder) ────────────────────────────────────
        private void buttonSelectAlbums_Click(object sender, RoutedEventArgs e)
        {
            // TODO: отвори диалог за избор на албуми и попълни _selectedAlbumIds
            MessageBox.Show("Album selection dialog – coming soon.",
                            "Select Albums", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ── Запазване ────────────────────────────────────────────────────────
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
                //byte[] imageBytes = null;
               // if (!string.IsNullOrWhiteSpace(_selectedImagePath))
                  //  imageBytes = File.ReadAllBytes(_selectedImagePath);

                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;

                using (SqlConnection conn = new SqlConnection(cs))
                {
                    conn.Open();
                    using (SqlTransaction tx = conn.BeginTransaction())
                    {
                        try
                        {
                            // Вмъкваме артиста
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

                            // Свързваме песните (ако има избрани)
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

                            // Свързваме албумите (ако има избрани)
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

        // ── Cancel ───────────────────────────────────────────────────────────
        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}