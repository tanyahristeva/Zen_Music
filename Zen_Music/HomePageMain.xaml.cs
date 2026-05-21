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
    public partial class HomePageMain : Window
    {
        public class AlbumCard
        {
            public int AlbumId { get; set; }
            public string Title { get; set; }
            public string Artist { get; set; }
            public BitmapImage Cover { get; set; }
        }

        public class SongCard
        {
            public int SongId { get; set; }
            public string Title { get; set; }
            public string Artist { get; set; }
            public BitmapImage Cover { get; set; }
        }

        public class EventCard
        {
            public int EventId { get; set; }
            public string Title { get; set; }
            public BitmapImage Poster { get; set; }
        }

        private List<EventCard> _events = new List<EventCard>();
        private int _currentEventIndex = 0;

        public HomePageMain()
        {
            InitializeComponent();
            LoadForYou();
            LoadTrending();
            LoadEvents();
            LoadPlaylists();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        // scroll from anywhere
        private void LeftPanel_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer == null) return;
            scrollViewer.ScrollToVerticalOffset(
                scrollViewer.VerticalOffset - e.Delta / 3.0);
            e.Handled = true;
        }

        // ── For You: албуми от следвани артисти ─────────────────────────────
        private void LoadForYou()
        {
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    string query = @"
                        SELECT TOP 15 al.ID, al.Title, al.Cover_URL, ar.Name AS ArtistName
                        FROM Albums al
                        JOIN AlbumArtists aa ON aa.Album_ID = al.ID
                        JOIN Artists ar ON ar.ID = aa.Artist_ID
                        JOIN FollowArtists fa ON fa.Artist_ID = ar.ID
                        WHERE fa.User_ID = @UserId
                        ORDER BY al.Release_Date DESC";

                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
                    {
                        var cmd = adapter.SelectCommand;
                        cmd.Parameters.AddWithValue("@UserId", SessionManager.UserId);

                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        var list = new List<AlbumCard>();
                        foreach (DataRow row in dt.Rows)
                        {
                            list.Add(new AlbumCard
                            {
                                AlbumId = Convert.ToInt32(row["ID"]),
                                Title = row["Title"].ToString(),
                                Artist = row["ArtistName"].ToString(),
                                Cover = LoadImageFromPath(row["Cover_URL"] != DBNull.Value
                                        ? row["Cover_URL"].ToString() : null)
                            });
                        }

                        // Ако няма следвани артисти — зареди случайни
                        if (list.Count == 0)
                            list = LoadRandomAlbums();

                        listForYou.ItemsSource = list;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading For You: " + ex.Message);
            }
        }

        private List<AlbumCard> LoadRandomAlbums()
        {
            var list = new List<AlbumCard>();
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    string query = @"
                        SELECT TOP 15 al.ID, al.Title, al.Cover_URL, ar.Name AS ArtistName
                        FROM Albums al
                        LEFT JOIN AlbumArtists aa ON aa.Album_ID = al.ID
                        LEFT JOIN Artists ar ON ar.ID = aa.Artist_ID
                        ORDER BY NEWID()";

                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        foreach (DataRow row in dt.Rows)
                            list.Add(new AlbumCard
                            {
                                AlbumId = Convert.ToInt32(row["ID"]),
                                Title = row["Title"].ToString(),
                                Artist = row["ArtistName"] != DBNull.Value
                                         ? row["ArtistName"].ToString() : "",
                                Cover = LoadImageFromPath(row["Cover_URL"] != DBNull.Value
                                        ? row["Cover_URL"].ToString() : null)
                            });
                    }
                }
            }
            catch { }
            return list;
        }

        // ── Trending: случайни песни ─────────────────────────────────────────
        private void LoadTrending()
        {
            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    string query = @"
                        SELECT TOP 15 s.ID, s.Title, s.Image_Data, ar.Name AS ArtistName
                        FROM Songs s
                        LEFT JOIN SongArtists sa ON sa.Song_ID = s.ID AND sa.Role = 'Main'
                        LEFT JOIN Artists ar ON ar.ID = sa.Artist_ID
                        ORDER BY NEWID()";

                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        var list = new List<SongCard>();
                        foreach (DataRow row in dt.Rows)
                            list.Add(new SongCard
                            {
                                SongId = Convert.ToInt32(row["ID"]),
                                Title = row["Title"].ToString(),
                                Artist = row["ArtistName"] != DBNull.Value
                                         ? row["ArtistName"].ToString() : "",
                                Cover = LoadImageFromBytes(row["Image_Data"] != DBNull.Value
                                        ? (byte[])row["Image_Data"] : null)
                            });

                        listTrending.ItemsSource = list;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading Trending: " + ex.Message);
            }
        }



        public class PlaylistCard
        {
            public int PlaylistId { get; set; }
            public string Name { get; set; }
            public string Creator { get; set; }
            public BitmapImage Cover { get; set; }
        }

        // --- Personalized playlists --- if there are real playlists, this shoud work
        //private void LoadPlaylists()
        //{
        //    try
        //    {
        //        string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
        //        using (SqlConnection conn = new SqlConnection(cs))
        //        {
        //            string query = @"
        //        SELECT TOP 15 p.ID, p.Name, p.Cover_URL, u.Username AS Creator
        //        FROM Playlists p
        //        LEFT JOIN Users u ON u.ID = p.Creator_ID
        //        WHERE p.Is_Public = 1
        //        ORDER BY NEWID()";

        //            using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
        //            {
        //                DataTable dt = new DataTable();
        //                adapter.Fill(dt);

        //                var list = new List<PlaylistCard>();
        //                foreach (DataRow row in dt.Rows)
        //                    list.Add(new PlaylistCard
        //                    {
        //                        PlaylistId = Convert.ToInt32(row["ID"]),
        //                        Name = row["Name"].ToString(),
        //                        Creator = row["Creator"] != DBNull.Value
        //                                     ? row["Creator"].ToString() : "",
        //                        Cover = LoadImageFromPath(row["Cover_URL"] != DBNull.Value
        //                                     ? row["Cover_URL"].ToString() : null)
        //                    });

        //                listPlaylists.ItemsSource = list;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error loading playlists: " + ex.Message);
        //    }
        //}

        private void LoadPlaylists()
        {
            var list = new List<PlaylistCard>
            {
                new PlaylistCard { PlaylistId = 1, Name = "Chill Vibes", Creator = "zen", Cover = null },
                new PlaylistCard { PlaylistId = 2, Name = "Top Hits", Creator = "zen", Cover = null },
                new PlaylistCard { PlaylistId = 3, Name = "Late Night", Creator = "zen", Cover = null },
                new PlaylistCard { PlaylistId = 4, Name = "Workout", Creator = "zen", Cover = null },
                new PlaylistCard { PlaylistId = 5, Name = "Weekly Hits", Creator = "zen", Cover = null },
                new PlaylistCard { PlaylistId = 6, Name = "Rhythm", Creator = "zen", Cover = null },
                new PlaylistCard { PlaylistId = 7, Name = "Slow and Steady", Creator = "zen", Cover = null },
                new PlaylistCard { PlaylistId = 8, Name = "Race", Creator = "zen", Cover = null },
                new PlaylistCard { PlaylistId = 9, Name = "New Music Friday", Creator = "zen", Cover = null },
                new PlaylistCard { PlaylistId = 10, Name = "Pop Off", Creator = "zen", Cover = null }
            };

            listPlaylists.ItemsSource = list;
        }




        // ── Events ───────────────────────────────────────────────────────────
        //private void LoadEvents()
        //{
        //    try
        //    {
        //        string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
        //        using (SqlConnection conn = new SqlConnection(cs))
        //        {
        //            using (SqlDataAdapter adapter = new SqlDataAdapter(
        //                "SELECT ID, Title, Image_URL FROM Events", conn))
        //            {
        //                DataTable dt = new DataTable();
        //                adapter.Fill(dt);

        //                foreach (DataRow row in dt.Rows)
        //                    _events.Add(new EventCard
        //                    {
        //                        EventId = Convert.ToInt32(row["ID"]),
        //                        Title = row["Title"] != DBNull.Value
        //                                ? row["Title"].ToString() : "",
        //                        Poster = LoadImageFromPath(row["Image_URL"] != DBNull.Value
        //                                 ? row["Image_URL"].ToString() : null)
        //                    });
        //            }
        //        }
        //    }
        //    catch { }

        //    ShowCurrentEvent();
        //}

        private void LoadEvents()
        {
            // Тестова снимка — замени пътя с твоя
            imgEvent.Source = LoadImageFromPath(@"C:\Users\azrax\Documents\GitHub\Zen_Music\Zen_Music\pics\dawes_poster.png");
        }

        private void ShowCurrentEvent()
        {
            if (_events.Count == 0) return;
            var ev = _events[_currentEventIndex];
            imgEvent.Source = ev.Poster;
            txtEventTitle.Text = ev.Title;
        }

        private void btnEventPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_events.Count == 0) return;
            _currentEventIndex = (_currentEventIndex - 1 + _events.Count) % _events.Count;
            ShowCurrentEvent();
        }

        private void btnEventNext_Click(object sender, RoutedEventArgs e)
        {
            if (_events.Count == 0) return;
            _currentEventIndex = (_currentEventIndex + 1) % _events.Count;
            ShowCurrentEvent();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        //// button maximize
        //private void btnMaximize_Click(object sender, RoutedEventArgs e)
        //{
        //    if (this.WindowState == WindowState.Normal)
        //        this.WindowState = WindowState.Maximized;
        //    else
        //        this.WindowState = WindowState.Normal;
        //}


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
        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OpenSearchPage(((TextBox)sender).Text.Trim());
            }
        }

        private void OpenSearchPage(string query)
        {
            WindowHelper.SaveState(this);
            var sp = new SearchPage(query);
            WindowHelper.ApplyState(sp);
            sp.Show();
            this.Close();
        }
        private void txtZen_Click(object sender, MouseButtonEventArgs e)
        {
            var home = new HomePageMain();
            home.Show();
            this.Close();
        }
        private void btnProfile_Click(object sender, RoutedEventArgs e)
        {
            var signIn = new SignInPage();
            signIn.Show();
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }


        // full screen option
        private bool _isFullScreen = false;
        private WindowState _prevState;

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
                WindowHelper.ToggleFullScreen(this);
        }


        // scroll left right
        private void ForYou_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var sv = sender as ScrollViewer;
            if (sv == null) return;
            sv.ScrollToHorizontalOffset(sv.HorizontalOffset - e.Delta / 3.0);
            e.Handled = true;
        }

        private void Trending_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var sv = sender as ScrollViewer;
            if (sv == null) return;
            sv.ScrollToHorizontalOffset(sv.HorizontalOffset - e.Delta / 3.0);
            e.Handled = true;
        }

        private void Playlists_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var sv = sender as ScrollViewer;
            if (sv == null) return;
            sv.ScrollToHorizontalOffset(sv.HorizontalOffset - e.Delta / 3.0);
            e.Handled = true;
        }

    }
}