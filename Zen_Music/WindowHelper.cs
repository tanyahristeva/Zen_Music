using System.Windows;

namespace Zen_Music
{
    public static class WindowHelper
    {
        public static void SaveState(Window w)
        {
            App.WinWidth = w.Width;
            App.WinHeight = w.Height;
            App.WinLeft = w.Left;
            App.WinTop = w.Top;
        }

        public static void ApplyState(Window w)
        {
            w.Width = App.WinWidth;
            w.Height = App.WinHeight;
            w.Left = App.WinLeft;
            w.Top = App.WinTop;

            if (App.IsFullScreen)
                w.WindowState = WindowState.Maximized;
        }

        public static void ToggleFullScreen(Window w)
        {
            if (!App.IsFullScreen)
            {
                App.WinWidth = w.Width;
                App.WinHeight = w.Height;
                App.IsFullScreen = true;
                w.WindowState = WindowState.Maximized;
            }
            else
            {
                App.IsFullScreen = false;
                w.WindowState = WindowState.Normal;
                w.Width = App.WinWidth;
                w.Height = App.WinHeight;
            }
        }
    }
}