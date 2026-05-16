using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Zen_Music
{
    public partial class SearchPage : Window
    {
        public class SongResult
        {
            public int SongId { get; set; }
            public string Title { get; set; }
            public string Artist { get; set; }
            public string Duration { get; set; }
            public BitmapImage Cover { get; set; }
            public bool IsLiked { get; set; }
            public bool IsDownloaded { get; set; }
        }

        public class PlaylistResult
        {
            public int PlaylistId { get; set; }
            public string Name { get; set; }
            public string Creator { get; set; }
            public BitmapImage Cover { get; set; }
        }

        private string _searchQuery = "";
        private List<string> _selectedGenres = new List<string>();
        private List<string> _selectedDecades = new List<string>();
        private bool _filterLiked = false;
        private bool _filterDownloaded = false;
        private bool _filterExplicit = false;
        private int _minDuration = 0;
        private int _maxDuration = 600;

        public SearchPage(string initialQuery = "")
        {
            InitializeComponent();
            _searchQuery = initialQuery;
            txtSearch.Text = initialQuery;
            if (!string.IsNullOrWhiteSpace(initialQuery))
                Search();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Search();
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void Search()
        {
            _searchQuery = txtSearch.Text.Trim();
            if (string.IsNullOrWhiteSpace(_searchQuery)) return;

            txtResultsLabel.Text = $"Results for '{_searchQuery}'";
            LoadSongs();
            LoadPlaylists();
        }

        private void LoadSongs()
        {
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    conn.Open();
                    string query = BuildSongQuery();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Query", "%" + _searchQuery + "%");
                        cmd.Parameters.AddWithValue("@UserId", SessionManager.UserId);
                        cmd.Parameters.AddWithValue("@MinDur", _minDuration);
                        cmd.Parameters.AddWithValue("@MaxDur", _maxDuration);

                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            var list = new List<SongResult>();
                            foreach (DataRow row in dt.Rows)
                            {
                                int dur = Convert.ToInt32(row["Duration_Sec"]);
                                list.Add(new SongResult
                                {
                                    SongId = Convert.ToInt32(row["ID"]),
                                    Title = row["Title"].ToString(),
                                    Artist = row["ArtistName"] != DBNull.Value
                                             ? row["ArtistName"].ToString() : "",
                                    Duration = $"{dur / 60}:{dur % 60:D2}",
                                    Cover = LoadImageFromBytes(row["Image_Data"] != DBNull.Value
                                            ? (byte[])row["Image_Data"] : null),
                                    IsLiked = Convert.ToInt32(row["IsLiked"]) > 0,
                                    IsDownloaded = Convert.ToInt32(row["IsDownloaded"]) > 0
                                });
                            }
                            listSongs.ItemsSource = list;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private string BuildSongQuery()
        {
            string query = @"
                SELECT s.ID, s.Title, s.Duration_Sec, s.Image_Data, s.Is_Explicit,
                       ar.Name AS ArtistName,
                       al.Release_Date,
                       g.Name AS GenreName,
                       (SELECT COUNT(*) FROM Likes l 
                        WHERE l.Song_ID = s.ID AND l.User_ID = @UserId) AS IsLiked,
                       (SELECT COUNT(*) FROM Downloads d 
                        WHERE d.Song_ID = s.ID AND d.User_ID = @UserId) AS IsDownloaded
                FROM Songs s
                LEFT JOIN SongArtists sa ON sa.Song_ID = s.ID AND sa.Role = 'Main'
                LEFT JOIN Artists ar ON ar.ID = sa.Artist_ID
                LEFT JOIN Albums al ON al.ID = s.Album_ID
                LEFT JOIN SongGenres sg ON sg.Song_ID = s.ID
                LEFT JOIN Genres g ON g.ID = sg.Genre_ID
                WHERE s.Title LIKE @Query ";

            if (_filterLiked)
                query += " AND EXISTS (SELECT 1 FROM Likes l WHERE l.Song_ID = s.ID AND l.User_ID = @UserId)";

            if (_filterDownloaded)
                query += " AND EXISTS (SELECT 1 FROM Downloads d WHERE d.Song_ID = s.ID AND d.User_ID = @UserId)";

            if (_filterExplicit)
                query += " AND s.Is_Explicit = 1";

            if (_selectedGenres.Count > 0)
            {
                string genres = "'" + string.Join("','", _selectedGenres) + "'";
                query += $" AND g.Name IN ({genres})";
            }

            if (_selectedDecades.Count > 0)
            {
                var decadeConditions = new List<string>();
                foreach (string decade in _selectedDecades)
                {
                    if (int.TryParse(decade.Replace("s", ""), out int year))
                        decadeConditions.Add($"(YEAR(al.Release_Date) >= {year} AND YEAR(al.Release_Date) < {year + 10})");
                }
                if (decadeConditions.Count > 0)
                    query += " AND (" + string.Join(" OR ", decadeConditions) + ")";
            }

            query += " AND s.Duration_Sec BETWEEN @MinDur AND @MaxDur";
            query += " ORDER BY s.Title";

            return query;
        }

        private void LoadPlaylists()
        {
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    string query = @"
                        SELECT p.ID, p.Name, p.Cover_URL, u.Username AS Creator
                        FROM Playlists p
                        LEFT JOIN Users u ON u.ID = p.Creator_ID
                        WHERE p.Name LIKE @Query AND p.Is_Public = 1
                        ORDER BY p.Name";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Query", "%" + _searchQuery + "%");
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            var list = new List<PlaylistResult>();
                            foreach (DataRow row in dt.Rows)
                                list.Add(new PlaylistResult
                                {
                                    PlaylistId = Convert.ToInt32(row["ID"]),
                                    Name = row["Name"].ToString(),
                                    Creator = row["Creator"].ToString(),
                                    Cover = LoadImageFromPath(row["Cover_URL"] != DBNull.Value
                                            ? row["Cover_URL"].ToString() : null)
                                });

                            listPlaylists.ItemsSource = list;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading playlists: " + ex.Message);
            }
        }

        // ── Филтри ───────────────────────────────────────────────────────────
        private void btnGenre_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string genre = btn.Tag.ToString();
                if (_selectedGenres.Contains(genre))
                {
                    _selectedGenres.Remove(genre);
                    btn.Background = System.Windows.Media.Brushes.Transparent;
                }
                else
                {
                    _selectedGenres.Add(genre);
                    btn.Background = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                        .ConvertFromString("#6AB0F5"));
                }
            }
        }

        private void btnDecade_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string decade = btn.Tag.ToString();
                if (_selectedDecades.Contains(decade))
                {
                    _selectedDecades.Remove(decade);
                    btn.Background = System.Windows.Media.Brushes.Transparent;
                }
                else
                {
                    _selectedDecades.Add(decade);
                    btn.Background = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                        .ConvertFromString("#6AB0F5"));
                }
            }
        }

        private void sliderDuration_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _minDuration = (int)sliderMinDur.Value;
            _maxDuration = (int)sliderMaxDur.Value;
            if (txtMinDur != null)
                txtMinDur.Text = $"{_minDuration / 60}:{_minDuration % 60:D2}";
            if (txtMaxDur != null)
                txtMaxDur.Text = $"{_maxDuration / 60}:{_maxDuration % 60:D2}";
        }

        private void toggleLiked_Click(object sender, RoutedEventArgs e)
        {
            _filterLiked = !_filterLiked;
            toggleLiked.Background = _filterLiked
                ? new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                    .ConvertFromString("#6AB0F5"))
                : System.Windows.Media.Brushes.Transparent;
        }

        private void toggleDownloaded_Click(object sender, RoutedEventArgs e)
        {
            _filterDownloaded = !_filterDownloaded;
            toggleDownloaded.Background = _filterDownloaded
                ? new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                    .ConvertFromString("#6AB0F5"))
                : System.Windows.Media.Brushes.Transparent;
        }

        private void toggleExplicit_Click(object sender, RoutedEventArgs e)
        {
            _filterExplicit = !_filterExplicit;
            toggleExplicit.Background = _filterExplicit
                ? new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                    .ConvertFromString("#6AB0F5"))
                : System.Windows.Media.Brushes.Transparent;
        }

        private void btnApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            LoadSongs();
        }

        private void LikeSong_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int songId)
            {
                try
                {
                    string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                    using (SqlConnection conn = new SqlConnection(cs))
                    {
                        conn.Open();
                        // Toggle like
                        string check = "SELECT COUNT(*) FROM Likes WHERE User_ID=@U AND Song_ID=@S";
                        using (SqlCommand cmd = new SqlCommand(check, conn))
                        {
                            cmd.Parameters.AddWithValue("@U", SessionManager.UserId);
                            cmd.Parameters.AddWithValue("@S", songId);
                            int count = (int)cmd.ExecuteScalar();

                            string toggle = count > 0
                                ? "DELETE FROM Likes WHERE User_ID=@U AND Song_ID=@S"
                                : "INSERT INTO Likes (User_ID, Song_ID) VALUES (@U, @S)";

                            using (SqlCommand cmd2 = new SqlCommand(toggle, conn))
                            {
                                cmd2.Parameters.AddWithValue("@U", SessionManager.UserId);
                                cmd2.Parameters.AddWithValue("@S", songId);
                                cmd2.ExecuteNonQuery();
                            }
                        }
                    }
                    LoadSongs();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private void DownloadSong_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int songId)
            {
                try
                {
                    string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                    using (SqlConnection conn = new SqlConnection(cs))
                    {
                        conn.Open();
                        string check = "SELECT COUNT(*) FROM Downloads WHERE User_ID=@U AND Song_ID=@S";
                        using (SqlCommand cmd = new SqlCommand(check, conn))
                        {
                            cmd.Parameters.AddWithValue("@U", SessionManager.UserId);
                            cmd.Parameters.AddWithValue("@S", songId);
                            int count = (int)cmd.ExecuteScalar();
                            if (count == 0)
                            {
                                using (SqlCommand cmd2 = new SqlCommand(
                                    "INSERT INTO Downloads (User_ID, Song_ID) VALUES (@U, @S)", conn))
                                {
                                    cmd2.Parameters.AddWithValue("@U", SessionManager.UserId);
                                    cmd2.Parameters.AddWithValue("@S", songId);
                                    cmd2.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                    LoadSongs();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

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
        private void btnProfile_Click(object sender, RoutedEventArgs e)
        {
            var signIn = new SignInPage();
            signIn.Show();
            this.Close();
        }

        // tuk
        private void txtZen_Click(object sender, MouseButtonEventArgs e)
        {
            WindowHelper.SaveState(this);
            var home = new HomePageMain();
            WindowHelper.ApplyState(home);
            home.Show();
            this.Close();
        }

        private static BitmapImage LoadImageFromBytes(byte[] data)
        {
            if (data == null || data.Length == 0) return null;
            try
            {
                using (var ms = new MemoryStream(data))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    return image;
                }
            }
            catch { return null; }
        }


        private bool _isFullScreen = false;
        private WindowState _prevState;

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
                WindowHelper.ToggleFullScreen(this);
        }

        private void ToggleFullScreen()
        {
            if (!_isFullScreen)
            {
                _prevState = this.WindowState;
                this.WindowState = WindowState.Maximized;
                _isFullScreen = true;
            }
            else
            {
                this.WindowState = _prevState == WindowState.Maximized
                    ? WindowState.Normal : _prevState;
                _isFullScreen = false;
            }
        }
    }
}
