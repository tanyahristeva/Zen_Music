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
    public partial class UpdateArtistPage : Window
    {
        private string _selectedImagePath = "";
        private bool _isNewImageSelected = false;
        private int _selectedArtistId = -1;

        private List<int> _selectedSongIds = new List<int>();
        private List<int> _selectedAlbumIds = new List<int>();


        // ViewModel за ComboBox
        public class ArtistItem
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public override string ToString() => Name;
        }


        public UpdateArtistPage()
        {
            InitializeComponent();
            LoadArtistsDropdown();
        }

        public UpdateArtistPage(int artistId)
        {
            InitializeComponent();
            _selectedArtistId = artistId;
            LoadArtistDetails(artistId);
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        // ── 1. Зарежда артистите в ComboBox ─────────────────────────────────
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

                        var list = new List<Artist>
                {
                    new Artist { ID = -1, Name = "-- Select an Artist --" }
                };

                        foreach (DataRow row in dt.Rows)
                            list.Add(new Artist
                            {
                                ID = Convert.ToInt32(row["ID"]),
                                Name = row["Name"].ToString()
                            });

                        //comboBoxSelectArtist.ItemsSource = list;
                        //comboBoxSelectArtist.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading artists: " + ex.Message, "Database Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── 2. Избор на артист ───────────────────────────────────────────────
       // private void comboBoxSelectArtist_SelectionChanged(object sender,
        //System.Windows.Controls.SelectionChangedEventArgs e)
        //{
         //   if (comboBoxSelectArtist.SelectedItem is Artist artist)
           // {
            //    _selectedArtistId = artist.ID;
            //    if (artist.ID == -1) { ClearFields(); return; }
            //    LoadArtistDetails(artist.ID);
           // }
       // }

        // ── 3. Зарежда данните на артиста ────────────────────────────────────
        private void LoadArtistDetails(int id)
        {
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    conn.Open();
                    // ПРОМЯНА 1: Търсим Image_URL вместо Image_Data
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT Name, Bio, Image_URL FROM Artists WHERE ID = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                textBoxArtistName.Text = reader["Name"].ToString();
                                textBoxDescription.Text = reader["Bio"] != DBNull.Value
                                                          ? reader["Bio"].ToString() : "";

                                // ПРОМЯНА 2: Зареждаме снимката от пътя към файла
                                if (reader["Image_URL"] != DBNull.Value)
                                {
                                    string imagePath = reader["Image_URL"].ToString();
                                    if (File.Exists(imagePath))
                                    {
                                        var bmp = new BitmapImage();
                                        bmp.BeginInit();
                                        bmp.UriSource = new Uri(imagePath);
                                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                                        bmp.EndInit();
                                        imageArtist.Source = bmp;
                                    }
                                    else
                                    {
                                        imageArtist.Source = null; // Файлът е бил изтрит/преместен
                                    }
                                }
                                else
                                {
                                    imageArtist.Source = null;
                                }

                                _isNewImageSelected = false;
                                _selectedImagePath = "";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading artist details: " + ex.Message,
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearFields()
        {
            textBoxArtistName.Clear();
            textBoxDescription.Clear();
            imageArtist.Source = null;
            _isNewImageSelected = false;
            _selectedImagePath = "";
        }

        private static BitmapImage LoadBitmap(byte[] data)
        {
            if (data == null || data.Length == 0) return null;

            try
            {
                using (var ms = new System.IO.MemoryStream(data))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = ms;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();

                    // Add this line to return the successfully created image!
                    return image;
                }
            }
            catch
            {
                return null;
            }
        }

        // ── 4. Качване на нова снимка ────────────────────────────────────────
        private void buttonUpload_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select New Profile Picture",
                Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
            };
            if (ofd.ShowDialog() == true)
            {
                _selectedImagePath = ofd.FileName;
                _isNewImageSelected = true;

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(_selectedImagePath);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                imageArtist.Source = bmp;
            }
        }

        // ── Placeholders ─────────────────────────────────────────────────────
        private void buttonSelectSongs_Click(object sender, RoutedEventArgs e)
            => MessageBox.Show("Song selection dialog – coming soon.", "Select Songs",
                               MessageBoxButton.OK, MessageBoxImage.Information);

        private void buttonSelectAlbums_Click(object sender, RoutedEventArgs e)
            => MessageBox.Show("Album selection dialog – coming soon.", "Select Albums",
                               MessageBoxButton.OK, MessageBoxImage.Information);

        // ── 5. Запазване ─────────────────────────────────────────────────────
        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedArtistId == -1)
            {
                MessageBox.Show("Please select an artist to update!", "Required",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string name = textBoxArtistName.Text.Trim();
            string bio = textBoxDescription.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Artist name cannot be empty!", "Required Field",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    conn.Open();
                    string query = _isNewImageSelected
        ? "UPDATE Artists SET Name=@Name, Bio=@Bio, Image_URL=@Img WHERE ID=@Id"
        : "UPDATE Artists SET Name=@Name, Bio=@Bio WHERE ID=@Id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", _selectedArtistId);
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Bio", string.IsNullOrWhiteSpace(bio)
                                                            ? (object)DBNull.Value : bio);
                        if (_isNewImageSelected)
                        {
                            // ПРОМЯНА 4: Подаваме стринга с пътя, вместо File.ReadAllBytes
                            cmd.Parameters.AddWithValue("@Img", _selectedImagePath);
                        }

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show($"Artist '{name}' updated successfully!", "Success",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating artist:\n" + ex.Message,
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