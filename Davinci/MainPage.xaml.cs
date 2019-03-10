using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Davinci.net;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Davinci
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            var imageStorageFile = await KnownFolders.MusicLibrary.GetFileAsync("beautiful.jpg");
            var imageStorageFile2 = await KnownFolders.MusicLibrary.GetFileAsync("trees.jpg");

            var davinci = await DavinciImage.FromTilesAsync(
                (new Point(1, 1), imageStorageFile),
                (new Point(2, 1), imageStorageFile2));

            GridView.ItemsSource = await
                (await davinci.Scale(2).ToTilesAsync(256, 256))
                .Select(x => x.Item2.Scale(0.5).ToImageAsync())
                .ExecuteAsync();

            davinci.Dispose();

        }
    }
}
