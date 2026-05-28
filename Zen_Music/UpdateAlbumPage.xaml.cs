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
    public partial class UpdateAlbumPage : Window
    {
        private string _selectedImagePath = "";
        private bool _isNewImageSelected = false;
        private int _selectedAlbumId = -1;

        private class AlbumDropdownItem
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public override string ToString() => Title;
        }

        private class ArtistItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public override string ToString() => Name;
        }

        private class GenreItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public override string ToString() => Name;
        }


        public UpdateAlbumPage(int albumId)
        {
            InitializeComponent();
            _selectedAlbumId = albumId;
            LoadArtists();
            LoadGenres();
            LoadAlbumDetails(albumId);

        }

        

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void LoadArtists()
        {
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT ID, Name FROM Artists ORDER BY Name", conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            comboBoxArtist.Items.Add(new ArtistItem
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }
                comboBoxArtist.DisplayMemberPath = "Name";
                comboBoxArtist.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading artists: " + ex.Message,
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadGenres()
        {
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT ID, Name FROM Genres ORDER BY Name", conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            comboBoxGenre.Items.Add(new GenreItem
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }
                comboBoxGenre.DisplayMemberPath = "Name";
                comboBoxGenre.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading genres: " + ex.Message,
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAlbumDetails(int id)
        {
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    conn.Open();
                    string query = @"
                            SELECT a.Title, a.Release_Date, a.Cover_URL,
                                    aa.Artist_ID,
                                    (SELECT COUNT(*) FROM Songs WHERE Album_ID = a.ID) AS SongCount,
                                    (SELECT TOP 1 Genre_ID FROM SongGenres sg
                                    JOIN Songs s ON s.ID = sg.Song_ID
                                    WHERE s.Album_ID = a.ID) AS Genre_ID
                            FROM Albums a
                            LEFT JOIN AlbumArtists aa ON aa.Album_ID = a.ID
                            WHERE a.ID = @Id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                textBoxAlbumTitle.Text = reader["Title"].ToString();

                                textBoxYear.Text = reader["Release_Date"] != DBNull.Value
                                    ? ((DateTime)reader["Release_Date"]).Year.ToString() : "";

                                textBoxNumberOfSongs.Text = reader["SongCount"].ToString();

                                // Избираме артиста в ComboBox
                                if (reader["Artist_ID"] != DBNull.Value)
                                {
                                    int artistId = Convert.ToInt32(reader["Artist_ID"]);
                                    foreach (ArtistItem item in comboBoxArtist.Items)
                                    {
                                        if (item.Id == artistId)
                                        {
                                            comboBoxArtist.SelectedItem = item;
                                            break;
                                        }
                                    }
                                }

                                if (reader["Genre_ID"] != DBNull.Value)
                                {
                                    int genreId = Convert.ToInt32(reader["Genre_ID"]);
                                    foreach (GenreItem item in comboBoxGenre.Items)
                                    {
                                        if (item.Id == genreId)
                                        {
                                            comboBoxGenre.SelectedItem = item;
                                            break;
                                        }
                                    }
                                }

                                if (reader["Cover_URL"] != DBNull.Value)
                                {
                                    string imagePath = reader["Cover_URL"].ToString();
                                    if (File.Exists(imagePath))
                                    {
                                        var bmp = new BitmapImage();
                                        bmp.BeginInit();
                                        bmp.UriSource = new Uri(imagePath);
                                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                                        bmp.EndInit();
                                        imageCover.Source = bmp;
                                    }
                                    else imageCover.Source = null;
                                }
                                else imageCover.Source = null;

                                _isNewImageSelected = false;
                                _selectedImagePath = "";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading album details: " + ex.Message,
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearFields()
        {
            textBoxAlbumTitle.Clear();
            textBoxYear.Clear();
            imageCover.Source = null;
            _isNewImageSelected = false;
            _selectedImagePath = "";
        }

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
                _isNewImageSelected = true;

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(_selectedImagePath);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                imageCover.Source = bmp;
            }
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAlbumId == -1)
            {
                MessageBox.Show("Please select an album to update!", "Required",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string title = textBoxAlbumTitle.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Album title cannot be empty!", "Required Field",
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
                yearValue = new DateTime(parsedYear, 1, 1);
            }

            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    conn.Open();

                    // Albums
                    string query = _isNewImageSelected
                        ? "UPDATE Albums SET Title=@Title, Release_Date=@Date, Cover_URL=@Img WHERE ID=@Id"
                        : "UPDATE Albums SET Title=@Title, Release_Date=@Date WHERE ID=@Id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", _selectedAlbumId);
                        cmd.Parameters.AddWithValue("@Title", title);
                        cmd.Parameters.AddWithValue("@Date", yearValue);
                        if (_isNewImageSelected)
                            cmd.Parameters.AddWithValue("@Img", _selectedImagePath);

                        cmd.ExecuteNonQuery();
                    }

                    // Update Genre
                    if (comboBoxGenre.SelectedItem is GenreItem selectedGenre)
                    {
                        using (SqlCommand cmdGenre = new SqlCommand(
                            @"UPDATE SongGenres SET Genre_ID = @GenreId
                            WHERE Song_ID IN (SELECT ID FROM Songs WHERE Album_ID = @Id)", conn))
                        {
                            cmdGenre.Parameters.AddWithValue("@Id", _selectedAlbumId);
                            cmdGenre.Parameters.AddWithValue("@GenreId", selectedGenre.Id);
                            cmdGenre.ExecuteNonQuery();
                        }
                    }
                }  
                MessageBox.Show($"Album '{title}' updated successfully!", "Success",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating album:\n" + ex.Message,
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void textBoxAlbumTitle_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        private void textBoxAlbumTitle_TextChanged_1(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }
    }
}