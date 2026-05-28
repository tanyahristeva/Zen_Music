using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Zen_Music
{
    public partial class UploadSongPage : Window
    {
        private string selectedImagePath = "";
        private string selectedFilePath = "";

        public UploadSongPage()
        {
            InitializeComponent();
            LoadArtistsDropdown();
            LoadGenresDropdown();
            LoadAlbumsDropdown();
        }

        private void LoadArtistsDropdown()
        {
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    string query = "SELECT ID, Name FROM Artists ORDER BY Name";
                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        DataRow empty = dt.NewRow();
                        empty["ID"] = -1;
                        empty["Name"] = "-- Select Artist --";
                        dt.Rows.InsertAt(empty, 0);
                        cmbArtist.ItemsSource = dt.DefaultView;
                        cmbArtist.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading artists: " + ex.Message);
            }
        }

        private void LoadGenresDropdown()
        {
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    string query = "SELECT ID, Name FROM Genres ORDER BY Name";
                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        DataRow empty = dt.NewRow();
                        empty["ID"] = -1;
                        empty["Name"] = "-- Select Genre --";
                        dt.Rows.InsertAt(empty, 0);
                        cmbGenre.ItemsSource = dt.DefaultView;
                        cmbGenre.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading genres: " + ex.Message);
            }
        }

        private void LoadAlbumsDropdown()
        {
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    string query = "SELECT ID, Title FROM Albums ORDER BY Title";
                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        DataRow empty = dt.NewRow();
                        empty["ID"] = -1;
                        empty["Title"] = "-- Select Album --";
                        dt.Rows.InsertAt(empty, 0);
                        cmbAlbum.ItemsSource = dt.DefaultView;
                        cmbAlbum.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading albums: " + ex.Message);
            }
        }

        private void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
            };
            if (ofd.ShowDialog() == true)
            {
                selectedImagePath = ofd.FileName;
                imgCover.Source = new BitmapImage(new Uri(selectedImagePath));
            }
        }

        private void btnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Audio Files (*.mp3;*.wav;*.flac)|*.mp3;*.wav;*.flac"
            };
            if (ofd.ShowDialog() == true)
            {
                selectedFilePath = ofd.FileName;
                lblFileName.Text = Path.GetFileName(selectedFilePath);
            }
        }

        private void cmbAlbum_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbAlbum.SelectedValue == null) return;
            if (!int.TryParse(cmbAlbum.SelectedValue.ToString(), out int albumId) || albumId == -1)
            {
                txtYear.Clear();
                return;
            }

            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    conn.Open();
                    string query = "SELECT Release_Date FROM Albums WHERE ID = @Id";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", albumId);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            txtYear.Text = ((DateTime)result).Year.ToString();
                        else
                            txtYear.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading album year: " + ex.Message);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string songName = txtSongName.Text.Trim();
            string durationText = txtDuration.Text.Trim();

            if (string.IsNullOrWhiteSpace(songName))
            {
                MessageBox.Show("Please enter a Song name!", "Required Field",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int albumId = -1;
            if (cmbAlbum.SelectedValue != null)
                int.TryParse(cmbAlbum.SelectedValue.ToString(), out albumId);

            int artistId = -1;
            if (cmbArtist.SelectedValue != null)
                int.TryParse(cmbArtist.SelectedValue.ToString(), out artistId);

            int genreId = -1;
            if (cmbGenre.SelectedValue != null)
                int.TryParse(cmbGenre.SelectedValue.ToString(), out genreId);

            int durationSec = 0;
            if (!string.IsNullOrWhiteSpace(durationText))
            {
                var parts = durationText.Split(':');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int min) &&
                    int.TryParse(parts[1], out int sec))
                {
                    durationSec = min * 60 + sec;
                }
                else
                {
                    MessageBox.Show("Duration format must be mm:ss (e.g. 3:41)", "Format Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            try
            {
                byte[] imageBytes = null;
                if (!string.IsNullOrWhiteSpace(selectedImagePath))
                    imageBytes = File.ReadAllBytes(selectedImagePath);

                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    conn.Open();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            string insertSong = @"INSERT INTO Songs (Title, Album_ID, Duration_Sec, File_URL, Is_Explicit, Image_Data)
                                                  OUTPUT INSERTED.ID
                                                  VALUES (@Title, @AlbumId, @Duration, @FileUrl, 0, @ImageData)";

                            int newSongId;
                            using (SqlCommand cmd = new SqlCommand(insertSong, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@Title", songName);
                                cmd.Parameters.AddWithValue("@AlbumId", albumId == -1 ? (object)DBNull.Value : albumId);
                                cmd.Parameters.AddWithValue("@Duration", durationSec);
                                cmd.Parameters.AddWithValue("@FileUrl",
                                    string.IsNullOrWhiteSpace(selectedFilePath)
                                    ? (object)DBNull.Value : selectedFilePath);
                                cmd.Parameters.AddWithValue("@ImageData",
                                    imageBytes ?? (object)DBNull.Value);
                                newSongId = (int)cmd.ExecuteScalar();
                            }

                            if (artistId != -1)
                            {
                                string insertSongArtist = @"INSERT INTO SongArtists (Song_ID, Artist_ID, Role)
                                                            VALUES (@SongId, @ArtistId, 'Main')";
                                using (SqlCommand cmd = new SqlCommand(insertSongArtist, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@SongId", newSongId);
                                    cmd.Parameters.AddWithValue("@ArtistId", artistId);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            if (genreId != -1)
                            {
                                string insertGenre = @"INSERT INTO SongGenres (Song_ID, Genre_ID)
                                                       VALUES (@SongId, @GenreId)";
                                using (SqlCommand cmd = new SqlCommand(insertGenre, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@SongId", newSongId);
                                    cmd.Parameters.AddWithValue("@GenreId", genreId);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                MessageBox.Show($"Song '{songName}' added successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving song:\n\n" + ex.Message, "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}