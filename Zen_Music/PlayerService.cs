using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Zen_Music
{
    public static class PlayerService
    {
        public static MediaPlayer Player { get; } = new MediaPlayer();
        public static string CurrentTitle { get; set; } = "";
        public static string CurrentArtist { get; set; } = "";
        public static bool IsPlaying { get; set; } = false;
        public static BitmapImage CurrentCover { get; set; }

        public static void PlaySong(string path, string title, string artist, BitmapImage cover)
        {
            Player.Open(new Uri(path));
            Player.Play();

            CurrentTitle = title;
            CurrentArtist = artist;
            CurrentCover = cover;

            IsPlaying = true;
        }

        public static void TogglePlayPause()
        {
            if (IsPlaying) { Player.Pause(); IsPlaying = false; }
            else { Player.Play(); IsPlaying = true; }
        }
    }
}