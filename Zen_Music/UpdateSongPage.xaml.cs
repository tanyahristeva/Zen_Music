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
    public partial class UpdateSongPage : Window
    {
        private string selectedImagePath = "";
        private string selectedFilePath = "";
        private bool isNewImageSelected = false;
        private int selectedSongId = -1;

        public UpdateSongPage()
        {
            InitializeComponent();
            LoadSongsDropdown();
            LoadArtistsDropdown();
            LoadGenresDropdown();
            LoadAlbumsDropdown();
        }

        private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                DragMove();
        }

        private void LoadSongsDropdown()
        {
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    string query = @"SELECT s.ID, s.Title + ' - ' + ISNULL(a.Name, 'Unknown') AS DisplayName
                                     FROM Songs s
                                     LEFT JOIN SongArtists sa ON sa.Song_ID = s.ID AND sa.Role = 'Main'
                                     LEFT JOIN Artists a ON a.ID = sa.Artist_ID
                                     ORDER BY s.Title";
                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        DataRow empty = dt.NewRow();
                        empty["ID"] = -1;
                        empty["DisplayName"] = "-- Select a Song --";
                        dt.Rows.InsertAt(empty, 0);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading songs: " + ex.Message); }
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
            catch (Exception ex) { MessageBox.Show("Error loading artists: " + ex.Message); }
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
            catch (Exception ex) { MessageBox.Show("Error loading genres: " + ex.Message); }
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
            catch (Exception ex) { MessageBox.Show("Error loading albums: " + ex.Message); }
        }

        private void LoadSongDetails(int id)
        {
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    conn.Open();
                    string query = @"SELECT s.Title, s.Duration_Sec, s.Image_Data,
                                            ISNULL(sa.Artist_ID, -1) AS ArtistId,
                                            ISNULL(sg.Genre_ID,  -1) AS GenreId,
                                            ISNULL(s.Album_ID,   -1) AS AlbumId
                                     FROM Songs s
                                     LEFT JOIN SongArtists sa ON sa.Song_ID = s.ID AND sa.Role = 'Main'
                                     LEFT JOIN SongGenres sg  ON sg.Song_ID  = s.ID
                                     WHERE s.ID = @Id";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtSongName.Text = reader["Title"].ToString();

                                int dur = (int)reader["Duration_Sec"];
                                txtDuration.Text = $"{dur / 60}:{dur % 60:D2}";

                                cmbArtist.SelectedValue = (int)reader["ArtistId"];
                                cmbGenre.SelectedValue = (int)reader["GenreId"];
                                cmbAlbum.SelectedValue = (int)reader["AlbumId"];

                                LoadAlbumYear((int)reader["AlbumId"]);

                                if (reader["Image_Data"] != DBNull.Value)
                                {
                                    byte[] imgData = (byte[])reader["Image_Data"];
                                    using (MemoryStream ms = new MemoryStream(imgData))
                                    {
                                        BitmapImage bmp = new BitmapImage();
                                        bmp.BeginInit();
                                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                                        bmp.StreamSource = ms;
                                        bmp.EndInit();
                                        imgCover.Source = bmp;
                                    }
                                }
                                else imgCover.Source = null;

                                isNewImageSelected = false;
                                selectedImagePath = "";
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading song: " + ex.Message); }
        }

        private void LoadAlbumYear(int albumId)
        {
            if (albumId == -1) { txtYear.Clear(); return; }
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT Release_Date FROM Albums WHERE ID = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", albumId);
                        object result = cmd.ExecuteScalar();
                        txtYear.Text = (result != null && result != DBNull.Value)
                            ? ((DateTime)result).Year.ToString() : "";
                    }
                }
            }
            catch { txtYear.Clear(); }
        }

        private void ClearFields()
        {
            txtSongName.Clear();
            txtDuration.Clear();
            txtYear.Clear();
            imgCover.Source = null;
            cmbArtist.SelectedIndex = 0;
            cmbGenre.SelectedIndex = 0;
            cmbAlbum.SelectedIndex = 0;
            isNewImageSelected = false;
            selectedSongId = -1;
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
                isNewImageSelected = true;
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

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (selectedSongId == -1)
            {
                MessageBox.Show("Please select a song!", "Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string title = txtSongName.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Song name cannot be empty!", "Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var parts = txtDuration.Text.Trim().Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int min) || !int.TryParse(parts[1], out int sec))
            {
                MessageBox.Show("Duration format must be mm:ss (e.g. 3:41)", "Format Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int durationSec = min * 60 + sec;

            int artistId = -1;
            if (cmbArtist.SelectedValue != null)
                int.TryParse(cmbArtist.SelectedValue.ToString(), out artistId);

            int genreId = -1;
            if (cmbGenre.SelectedValue != null)
                int.TryParse(cmbGenre.SelectedValue.ToString(), out genreId);

            int albumId = -1;
            if (cmbAlbum.SelectedValue != null)
                int.TryParse(cmbAlbum.SelectedValue.ToString(), out albumId);

            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    conn.Open();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            string updateQuery = isNewImageSelected
                                ? "UPDATE Songs SET Title=@Title, Duration_Sec=@Dur, Album_ID=@AlbumId, Image_Data=@Img WHERE ID=@Id"
                                : "UPDATE Songs SET Title=@Title, Duration_Sec=@Dur, Album_ID=@AlbumId WHERE ID=@Id";

                            using (SqlCommand cmd = new SqlCommand(updateQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@Id", selectedSongId);
                                cmd.Parameters.AddWithValue("@Title", title);
                                cmd.Parameters.AddWithValue("@Dur", durationSec);
                                cmd.Parameters.AddWithValue("@AlbumId", albumId == -1 ? (object)DBNull.Value : albumId);
                                if (isNewImageSelected)
                                    cmd.Parameters.AddWithValue("@Img", File.ReadAllBytes(selectedImagePath));
                                cmd.ExecuteNonQuery();
                            }

                            if (artistId != -1)
                            {
                                new SqlCommand($"DELETE FROM SongArtists WHERE Song_ID = {selectedSongId}", conn, transaction).ExecuteNonQuery();
                                using (SqlCommand cmd = new SqlCommand(
                                    "INSERT INTO SongArtists (Song_ID, Artist_ID, Role) VALUES (@SId, @AId, 'Main')", conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@SId", selectedSongId);
                                    cmd.Parameters.AddWithValue("@AId", artistId);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            if (genreId != -1)
                            {
                                new SqlCommand($"DELETE FROM SongGenres WHERE Song_ID = {selectedSongId}", conn, transaction).ExecuteNonQuery();
                                using (SqlCommand cmd = new SqlCommand(
                                    "INSERT INTO SongGenres (Song_ID, Genre_ID) VALUES (@SId, @GId)", conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@SId", selectedSongId);
                                    cmd.Parameters.AddWithValue("@GId", genreId);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                            MessageBox.Show("Song updated successfully!", "Success",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            this.DialogResult = true;
                            this.Close();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        public UpdateSongPage(int songId)
        {
            InitializeComponent();
            LoadArtistsDropdown();
            LoadGenresDropdown();
            LoadAlbumsDropdown();
            selectedSongId = songId;
            LoadSongDetails(songId);
        }
    }
}
