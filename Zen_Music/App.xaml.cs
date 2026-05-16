using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Zen_Music
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static bool IsFullScreen { get; set; } = false;
        public static double WinWidth { get; set; } = 1200;
        public static double WinHeight { get; set; } = 750;
        public static double WinLeft { get; set; } = 100;
        public static double WinTop { get; set; } = 100;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnLastWindowClose;
            var signIn = new SignInPage();
            // WindowHelper.ApplyState(signIn);
            signIn.Show();
        }
    }

}
