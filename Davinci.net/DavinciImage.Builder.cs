using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace Davinci.net
{
    public partial class DavinciImage
    {
        public static  Task<DavinciImage> FromTilesAsync(
            params (Point, StorageFile imageStorageFile)[] imageFiles)
        {
            return FromTilesAsync(0, 0, imageFiles);
        }
        public static async Task<DavinciImage> FromTilesAsync( float width=0,float height=0, params (Point, StorageFile imageStorageFile)[] imageFiles)
        {
            var device = new CanvasVirtualControl();

            var canvasBitmaps = await Task.WhenAll(imageFiles.Select(async file =>
               new Tuple<Point, CanvasBitmap>(file.Item1,await CanvasBitmap.LoadAsync(device, await file.Item2.OpenReadAsync()))));

            if (width == 0)
                width = (float)(canvasBitmaps.GroupBy(f=>f.Item1.X).Select(g=>g.Sum(f=>f.Item2.Size.Width)).Max());
            if (height == 0)
                height = (float)(canvasBitmaps.GroupBy(f => f.Item1.Y).Select(g => g.Sum(f => f.Item2.Size.Height)).Max());

            var offscreen = new CanvasVirtualImageSource(device, width,height);

            using (var ds = offscreen.CreateDrawingSession(Colors.White,new Rect(0,0,width,height)))
            {
                var previous = new List<Tuple<Point, CanvasBitmap>>();
                foreach (var item in canvasBitmaps)
                {

                    var x = (float)(previous.Where(i => i.Item1.Y == item.Item1.Y).Sum(p => p.Item2.Size.Width));
                    var y = (float)(previous.Where(i => i.Item1.X == item.Item1.X).Sum(p => p.Item2.Size.Height));
                    ds.DrawImage(item.Item2, x, y);
                    previous.Add(item);
                }
                previous.Clear();
            }

            StorageFile resultFile;
            using (var stream = new InMemoryRandomAccessStream())
            {
                var image = new Image {Source = offscreen.Source};
                var bmp = new RenderTargetBitmap();
                await bmp.RenderAsync(image);
                var displayInformation = DisplayInformation.GetForCurrentView();
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, // RGB with alpha
                    BitmapAlphaMode.Premultiplied,
                    (uint)bmp.PixelWidth,
                    (uint)bmp.PixelHeight,
                    displayInformation.RawDpiX,
                    displayInformation.RawDpiY,
                    (await bmp.GetPixelsAsync()).ToArray());
                await encoder.FlushAsync();
                stream.Seek(0);

                resultFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(Guid.NewGuid() + ".png");
                using (var sw = await resultFile.OpenReadAsync())
                {
                    await RandomAccessStream.CopyAndCloseAsync(stream.GetInputStreamAt(0), sw.GetOutputStreamAt(0));
                }
            }
            device.di
            return new DavinciImage( resultFile,true);
        }

       
    }
}
