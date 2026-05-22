using System;
using System.Windows.Media;
using System.Windows.Threading;

namespace Zen_Music
{
    public static class PlayerService
    {
        public static MediaPlayer Player { get; } = new MediaPlayer();
        public static string CurrentTitle { get; set; } = "";
        public static string CurrentArtist { get; set; } = "";
        public static bool IsPlaying { get; set; } = false;

        public static void PlaySong(string fileUrl, string title, string artist)
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return;

            Player.Open(new Uri(fileUrl));
            Player.Play();
            IsPlaying = true;
            CurrentTitle = title;
            CurrentArtist = artist;
        }

        public static void TogglePlayPause()
        {
            if (IsPlaying) { Player.Pause(); IsPlaying = false; }
            else { Player.Play(); IsPlaying = true; }
        }
    }
}