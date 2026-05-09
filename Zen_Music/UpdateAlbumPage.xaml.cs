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

      

        public UpdateAlbumPage(int albumId)
        {
            InitializeComponent();
            _selectedAlbumId = albumId;
            LoadAlbumDetails(albumId);
           
        }

        

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        

        private void LoadAlbumDetails(int id)
        {
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT Title, Release_Date, Cover_URL FROM Albums WHERE ID = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                textBoxAlbumTitle.Text = reader["Title"].ToString();

                                textBoxYear.Text = reader["Release_Date"] != DBNull.Value
                                    ? ((DateTime)reader["Release_Date"]).Year.ToString() : "";

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
                                    else
                                    {
                                        imageCover.Source = null;
                                    }
                                }
                                else
                                {
                                    imageCover.Source = null;
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