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
            public string FileUrl { get; set; }
            public BitmapImage Cover { get; set; }
            public bool IsLiked { get; set; }
            public bool IsDownloaded { get; set; }
        }

        public class AlbumResult
        {
            public int AlbumId { get; set; }
            public string Title { get; set; }
            public string Artist { get; set; }
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

            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            imgPlayerCover.Source = PlayerService.CurrentCover;

            if (!string.IsNullOrWhiteSpace(PlayerService.CurrentTitle))
            {
                txtPlayerTitle.Text = PlayerService.CurrentTitle;
                txtPlayerArtist.Text = PlayerService.CurrentArtist;
                txtPlayPause.Text = PlayerService.IsPlaying ? "⏸" : "▶";


            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                var player = PlayerService.Player;
                if (player.Source != null && player.NaturalDuration.HasTimeSpan)
                {
                    TimeSpan current = player.Position;
                    TimeSpan total = player.NaturalDuration.TimeSpan;
                    txtCurrentTime.Text = current.ToString(@"m\:ss");
                    txtTotalTime.Text = total.ToString(@"m\:ss");
                    progressFill.Width = (current.TotalSeconds / total.TotalSeconds)
                                          * progressBackground.ActualWidth;
                }
            }
            catch { }
        }

        private void btnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            PlayerService.TogglePlayPause();
            txtPlayPause.Text = PlayerService.IsPlaying ? "⏸" : "▶";
        }

        private void progressBackground_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var player = PlayerService.Player;
                if (!player.NaturalDuration.HasTimeSpan) return;
                Point p = e.GetPosition(progressBackground);
                double percent = p.X / progressBackground.ActualWidth;
                player.Position = TimeSpan.FromSeconds(
                    player.NaturalDuration.TimeSpan.TotalSeconds * percent);
            }
            catch { }
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
            LoadAlbums();
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
                                    FileUrl = row["File_URL"] != DBNull.Value ? row["File_URL"].ToString() : "",
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
                SELECT s.ID, s.Title, s.Duration_Sec, s.File_URL, s.Image_Data, s.Is_Explicit,
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

        private void LoadAlbums()
        {
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    conn.Open();
                    string query = BuildAlbumQuery();

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Query", "%" + _searchQuery + "%");
                        cmd.Parameters.AddWithValue("@UserId", SessionManager.UserId);

                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            var list = new List<AlbumResult>();
                            foreach (DataRow row in dt.Rows)
                                list.Add(new AlbumResult
                                {
                                    AlbumId = Convert.ToInt32(row["ID"]),
                                    Title = row["Title"].ToString(),
                                    Artist = row["ArtistName"] != DBNull.Value ? row["ArtistName"].ToString() : "Unknown Artist",
                                    Cover = LoadImageFromPath(row["Cover_URL"] != DBNull.Value ? row["Cover_URL"].ToString() : null)
                                });

                            listAlbums.ItemsSource = list;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading albums: " + ex.Message);
            }
        }

        private string BuildAlbumQuery()
        {
            string query = @"
                SELECT DISTINCT al.ID, al.Title, al.Cover_URL, ar.Name AS ArtistName, al.Release_Date
                FROM Albums al
                LEFT JOIN AlbumArtists aa ON aa.Album_ID = al.ID
                LEFT JOIN Artists ar ON ar.ID = aa.Artist_ID
                WHERE al.Title LIKE @Query ";

            if (_filterLiked)
                query += " AND EXISTS (SELECT 1 FROM Likes l JOIN Songs s ON l.Song_ID = s.ID WHERE s.Album_ID = al.ID AND l.User_ID = @UserId)";

            if (_filterDownloaded)
                query += " AND EXISTS (SELECT 1 FROM Downloads d JOIN Songs s ON d.Song_ID = s.ID WHERE s.Album_ID = al.ID AND d.User_ID = @UserId)";

            if (_filterExplicit)
                query += " AND EXISTS (SELECT 1 FROM Songs s WHERE s.Album_ID = al.ID AND s.Is_Explicit = 1)";

            if (_selectedGenres.Count > 0)
            {
                string genres = "'" + string.Join("','", _selectedGenres) + "'";
                query += $" AND EXISTS (SELECT 1 FROM Songs s JOIN SongGenres sg ON sg.Song_ID = s.ID JOIN Genres g ON g.ID = sg.Genre_ID WHERE s.Album_ID = al.ID AND g.Name IN ({genres}))";
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

            query += " ORDER BY al.Title";

            return query;
        }

        private void btnGenre_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;
            string genre = btn.Tag.ToString();
            var wrap = (WrapPanel)btn.Parent;

            if (genre == "All")
            {
                _selectedGenres.Clear();
                foreach (var child in wrap.Children)
                {
                    if (child is Button b)
                        b.Background = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                            .ConvertFromString("#2A2E45"));
                }
                btn.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                    .ConvertFromString("#6AB0F5"));
            }
            else
            {
                foreach (var child in wrap.Children)
                {
                    if (child is Button b && b.Tag?.ToString() == "All")
                        b.Background = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                            .ConvertFromString("#2A2E45"));
                }

                if (_selectedGenres.Contains(genre))
                {
                    _selectedGenres.Remove(genre);
                    btn.Background = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                        .ConvertFromString("#2A2E45"));
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
            if (!(sender is Button btn)) return;
            string decade = btn.Tag.ToString();

            if (_selectedDecades.Contains(decade))
            {
                _selectedDecades.Remove(decade);
                btn.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                    .ConvertFromString("#2A2E45"));
            }
            else
            {
                _selectedDecades.Add(decade);
                btn.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                    .ConvertFromString("#6AB0F5"));
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

        private void toggleLiked_Click(object sender, MouseButtonEventArgs e)
        {
            _filterLiked = !_filterLiked;
            toggleLikedBorder.Background = _filterLiked
                ? new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                    .ConvertFromString("#8EBBFF"))
                : new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                    .ConvertFromString("#2A2E45"));
            toggleLikedDot.HorizontalAlignment = _filterLiked
                ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            toggleLikedDot.Margin = _filterLiked
                ? new Thickness(0, 0, 3, 0) : new Thickness(3, 0, 0, 0);
            toggleLikedDot.Fill = _filterLiked
                ? System.Windows.Media.Brushes.White
                : new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                    .ConvertFromString("#7B80A8"));
        }

        private void toggleDownloaded_Click(object sender, MouseButtonEventArgs e)
        {
            _filterDownloaded = !_filterDownloaded;
            toggleDownloadedBorder.Background = _filterDownloaded
                ? new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                    .ConvertFromString("#8EBBFF"))
                : new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                    .ConvertFromString("#2A2E45"));
            toggleDownloadedDot.HorizontalAlignment = _filterDownloaded
                ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            toggleDownloadedDot.Margin = _filterDownloaded
                ? new Thickness(0, 0, 3, 0) : new Thickness(3, 0, 0, 0);
            toggleDownloadedDot.Fill = _filterDownloaded
                ? System.Windows.Media.Brushes.White
                : new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                    .ConvertFromString("#7B80A8"));
        }

        private void toggleExplicit_Click(object sender, MouseButtonEventArgs e)
        {
            _filterExplicit = !_filterExplicit;
            toggleExplicitBorder.Background = _filterExplicit
                ? new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                    .ConvertFromString("#8EBBFF"))
                : new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                    .ConvertFromString("#2A2E45"));
            toggleExplicitDot.HorizontalAlignment = _filterExplicit
                ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            toggleExplicitDot.Margin = _filterExplicit
                ? new Thickness(0, 0, 3, 0) : new Thickness(3, 0, 0, 0);
            toggleExplicitDot.Fill = _filterExplicit
                ? System.Windows.Media.Brushes.White
                : new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                    .ConvertFromString("#7B80A8"));
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

        private System.Windows.Threading.DispatcherTimer _timer
                = new System.Windows.Threading.DispatcherTimer();
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

        private void btnToggleFilters_Click(object sender, RoutedEventArgs e)
        {
            filtersPanel.Visibility = filtersPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void PlaySong_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int songId)
            {
                if (listSongs.ItemsSource is System.Collections.Generic.List<SongResult> songs)
                {
                    var song = songs.Find(s => s.SongId == songId);
                    if (song == null || string.IsNullOrWhiteSpace(song.FileUrl)) return;

                    string fullPath = System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory, song.FileUrl);

                    PlayerService.PlaySong(fullPath, song.Title, song.Artist, song.Cover);
                    txtPlayerTitle.Text = song.Title;
                    txtPlayerArtist.Text = song.Artist;
                    txtPlayPause.Text = "⏸";
                    imgPlayerCover.Source = song.Cover;
                }
            }
        }

    }
}
