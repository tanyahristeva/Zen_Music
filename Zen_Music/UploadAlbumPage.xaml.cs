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
    public partial class UploadAlbumPage : Window
    {
        private string _selectedImagePath = "";
        private string _selectedFilePath = "";

        // Прости ViewModel-и за ComboBox-овете
        private class DropdownItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public override string ToString() => Name;
        }

        public UploadAlbumPage()
        {
            InitializeComponent();
            LoadArtistsDropdown();
            LoadGenresDropdown();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        // ── Зарежда артистите ────────────────────────────────────────────────
        private void LoadArtistsDropdown()
        {
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(
                        "SELECT ID, Name FROM Artists ORDER BY Name", conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        var list = new List<DropdownItem>
                        {
                            new DropdownItem { Id = -1, Name = "-- Select Artist --" }
                        };
                        foreach (DataRow row in dt.Rows)
                            list.Add(new DropdownItem
                            {
                                Id = Convert.ToInt32(row["ID"]),
                                Name = row["Name"].ToString()
                            });

                        comboBoxArtist.ItemsSource = list;
                        
                        comboBoxArtist.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading artists: " + ex.Message,
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Зарежда жанровете ────────────────────────────────────────────────
        private void LoadGenresDropdown()
        {
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(
                        "SELECT ID, Name FROM Genres ORDER BY Name", conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        var list = new List<DropdownItem>
                        {
                            new DropdownItem { Id = -1, Name = "-- Select Genre --" }
                        };
                        foreach (DataRow row in dt.Rows)
                            list.Add(new DropdownItem
                            {
                                Id = Convert.ToInt32(row["ID"]),
                                Name = row["Name"].ToString()
                            });

                        comboBoxGenre.ItemsSource = list;
                        
                        comboBoxGenre.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading genres: " + ex.Message,
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Качване на корица ────────────────────────────────────────────────
        private void buttonUpload_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Album Cover",
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
                imageCover.Source = bmp;
            }
        }

        // ── Избор на файл ────────────────────────────────────────────────────
        private void buttonSelectFile_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Album File",
                Filter = "Archive/Audio Files (*.zip;*.rar;*.mp3)|*.zip;*.rar;*.mp3|All Files (*.*)|*.*"
            };
            if (ofd.ShowDialog() == true)
            {
                _selectedFilePath = ofd.FileName;
                MessageBox.Show("File selected: " + Path.GetFileName(_selectedFilePath),
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ── Запазване ────────────────────────────────────────────────────────
        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            string title = textBoxAlbumName.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Album name cannot be empty!", "Required",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int artistId = comboBoxArtist.SelectedItem is DropdownItem a ? a.Id : -1;
            if (artistId == -1)
            {
                MessageBox.Show("Please select an artist!", "Required",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int genreId = comboBoxGenre.SelectedItem is DropdownItem g ? g.Id : -1;

            int numberOfSongs = 0;
            string numText = textBoxNumberOfSongs.Text.Trim();
            if (!string.IsNullOrWhiteSpace(numText) && !int.TryParse(numText, out numberOfSongs))
            {
                MessageBox.Show("Number of songs must be a valid number!", "Format Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            object yearValue = DBNull.Value;
            string yearText = textBoxYear.Text.Trim();
            if (!string.IsNullOrWhiteSpace(yearText))
            {
                if (!int.TryParse(yearText, out int parsedYear))
                {
                    MessageBox.Show("Please enter a valid year (e.g. 2024)!", "Format Error",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                yearValue = parsedYear;
            }

            // Запис
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
                            // ПРОМЯНА 1: Оставяме само колоните, които реално съществуват в таблицата Albums
                            string insertAlbum = @"
                                INSERT INTO Albums (Title, Release_Date, Cover_URL)
                                OUTPUT INSERTED.ID
                                VALUES (@Title, @Date, @CoverURL)";

                            int newAlbumId;
                            using (SqlCommand cmd = new SqlCommand(insertAlbum, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@Title", title);

                                // ПРОМЯНА 2: Конвертираме въведената година (int) във валидна дата (Date), напр. 2024-01-01
                                if (yearValue is int year)
                                {
                                    cmd.Parameters.AddWithValue("@Date", new DateTime(year, 1, 1));
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue("@Date", DBNull.Value);
                                }

                                // ПРОМЯНА 3: Записваме текстовия път към снимката, вместо масив от байтове
                                cmd.Parameters.AddWithValue("@CoverURL", string.IsNullOrWhiteSpace(_selectedImagePath)
                                                                         ? (object)DBNull.Value : _selectedImagePath);

                                newAlbumId = (int)cmd.ExecuteScalar();
                            }

                            // Връзка Албум ↔ Артист (предполагаме, че таблицата AlbumArtists съществува и е наред)
                            using (SqlCommand cmd = new SqlCommand(
                                "INSERT INTO AlbumArtists (Album_ID, Artist_ID) VALUES (@AlbumId, @ArtistId)",
                                conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@AlbumId", newAlbumId);
                                cmd.Parameters.AddWithValue("@ArtistId", artistId);
                                cmd.ExecuteNonQuery();
                            }

                            tx.Commit();
                            MessageBox.Show("Album uploaded successfully!", "Success",
                                            MessageBoxButton.OK, MessageBoxImage.Information);
                            this.DialogResult = true;
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            tx.Rollback();
                            MessageBox.Show("Error saving data:\n" + ex.Message,
                                            "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection Error:\n" + ex.Message,
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