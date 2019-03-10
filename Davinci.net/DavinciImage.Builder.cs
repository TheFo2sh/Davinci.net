using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Microsoft.Graphics.Canvas;

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
            var device = CanvasDevice.GetSharedDevice();

            var canvasBitmaps = await Task.WhenAll(imageFiles.Select(async file =>
               new Tuple<Point, CanvasBitmap>(file.Item1,await CanvasBitmap.LoadAsync(device, await file.Item2.OpenReadAsync()))));

            if (width == 0)
                width = (float)(canvasBitmaps.Sum(f => f.Item2.Size.Width));
            if (height == 0)
                height = (float)(canvasBitmaps.Sum(f => f.Item2.Size.Height));

            var offscreen = new CanvasRenderTarget(device, width,height, 96);

            using (var ds = offscreen.CreateDrawingSession())
            {
                ds.Clear(Colors.White);
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
            using (var stream = new MemoryStream())
            {
                await offscreen.SaveAsync(stream.AsRandomAccessStream(), CanvasBitmapFileFormat.Png);
                stream.Seek(0, SeekOrigin.Begin);
                resultFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(Guid.NewGuid() + ".png");
                using (var sw = await resultFile.OpenStreamForWriteAsync())
                {
                    await stream.CopyToAsync(sw);
                    await sw.FlushAsync();
                }
            }

            return new DavinciImage( resultFile,true);
        }

       
    }
}
