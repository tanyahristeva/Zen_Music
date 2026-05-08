using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Zen_Music.AlbumPages;

namespace Zen_Music
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnTestUpload_Click(object sender, RoutedEventArgs e)
        {
           
            UploadSongPage uploadPage = new UploadSongPage();
            uploadPage.ShowDialog();
        }

        private void btnTestUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateSongSearchPage searchPage = new UpdateSongSearchPage();
            searchPage.ShowDialog();
        }

        private void btnTestDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteSongPage deletePage = new DeleteSongPage();
            deletePage.ShowDialog();
        }

        private void btnTestAddArtist_Click(object sender, RoutedEventArgs e)
        {
            AddArtistPage addArtistPage = new AddArtistPage();
            addArtistPage.ShowDialog();
        }

        private void btnTestUpdateArtist_Click(object sender, RoutedEventArgs e)
        {
            UpdateArtistSearchPage searchPage = new UpdateArtistSearchPage();
            searchPage.ShowDialog();
        }

        private void btnTestDeleteArtist_Click(object sender, RoutedEventArgs e)
        {
            DeleteArtistPage deleteArtistPage = new DeleteArtistPage();
            deleteArtistPage.ShowDialog();
        }

        private void btnTestUploadAlbum_Click(object sender, RoutedEventArgs e)
        {
            UploadAlbumPage uploadAlbumPage = new UploadAlbumPage();
            uploadAlbumPage.ShowDialog();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DeleteAlbumPage deleteAlbumPage = new DeleteAlbumPage();
            deleteAlbumPage.ShowDialog();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
           UpdateAlbumSearchPage updateAlbumSearchPage = new UpdateAlbumSearchPage();
            updateAlbumSearchPage.ShowDialog();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
  

     
    }
