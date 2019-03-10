using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Lumia.Imaging;
using Lumia.Imaging.Adjustments;
using Lumia.Imaging.Transforms;

namespace Davinci.net
{
    public partial class DavinciImage : IDisposable
    {
        private readonly Stack<IImageProvider> _sourceHistory;
        private IImageProvider _source;
        private IImageProvider Source
        {
            get => _source;
            set
            {
                _sourceHistory.Push(Source);
                _source = value;
            }
        }
        
        private DavinciImage(IImageProvider source)
        {
            _sourceHistory=new Stack<IImageProvider>();
            Source = source;
        }

        private readonly Action _disposeAction;

        public DavinciImage(StorageFile file,  bool delete) : this(new StorageFileImageSource(file))
        {
            if (delete)
            {
                _disposeAction =
                    new Action(async () => { await file.DeleteAsync(StorageDeleteOption.PermanentDelete); });
            }
        }
        public DavinciImage(StorageFile file):this(new StorageFileImageSource(file))
        {}
        public DavinciImage(IBuffer buffer) : this(new BufferImageSource(buffer))
        {}

        public DavinciImage(Stream stream) : this(new StreamImageSource(stream))
        {}
        public DavinciImage Contast(double level)
        {
            Source = new ContrastEffect(Source) {Level = level};
            return this;
        }

        public DavinciImage Sharpness(double level)
        {
            Source = new SharpnessEffect(Source) {Level = level};
            return this;
        }

        public DavinciImage SaturationLightness(Curve lightnessCurve, Curve saturationCurve)
        {
            Source = new SaturationLightnessEffect(Source)
            {
                LightnessCurve = lightnessCurve,
                SaturationCurve = saturationCurve
            };
            return this;
        }

        public DavinciImage TemperatureAndTint(double temperature, double tint)
        {
            Source = new TemperatureAndTintEffect(Source) {Temperature = temperature, Tint = tint};
            return this;
        }

        public DavinciImage Scale(double scale)
        {
            Source = new ScaleEffect(Source) {Scale = scale };
            return this;
        }

        public DavinciImage Crop(Rect area)
        {
            Source = new CropEffect(Source) {CropArea = area};
            return this;
        }

        public async Task<BitmapImage> ToImageAsync()
        {
            using (var jpegRenderer = new JpegRenderer(Source))
            {
                var biSource = new BitmapImage();
                var pixels= await jpegRenderer.RenderAsync();
              
               await biSource.SetSourceAsync(pixels.AsStream().AsRandomAccessStream());
                return biSource;
            }
        }
        public async Task<Stream> ToStreamAsync()
        {
            using (var jpegRenderer = new JpegRenderer(Source))
            {
                var biSource = new BitmapImage();
                var pixels = await jpegRenderer.RenderAsync();
                return pixels.AsStream();
                
            }
        }
        public async Task<IBuffer> ToBufferAsync()
        {
            using (var jpegRenderer = new JpegRenderer(Source))
            {
                var pixels = await jpegRenderer.RenderAsync();
                return pixels;
            }
        }

        public async Task<IEnumerable<Tuple<Point, DavinciImage>>> ToTilesAsync(double tileWidth, double tileHight)
        {
            var listResults = new List<Tuple<Point, DavinciImage>>();

            Size size;
            if (Source is ScaleEffect)
            {
                var imageProviderInfo = await _sourceHistory.Peek().GetInfoAsync();
                size = new Size(imageProviderInfo.ImageSize.Width * (Source as ScaleEffect).Scale,
                    imageProviderInfo.ImageSize.Height * (Source as ScaleEffect).Scale);
            }
            else
                size = (await Source.GetInfoAsync()).ImageSize;
            var numberOfHorizontalTiles = (int) size.Width / tileWidth;
            var numberOfVerticalTiles = (int) size.Height / tileHight;

            for (var y = 0; y < numberOfVerticalTiles; y++)
            {
                for (var x = 0; x < numberOfHorizontalTiles; x++)

                {
                    var cropEffect = new CropEffect(Source)
                    {
                        CropArea = new Rect(x * tileWidth, y * tileHight, tileWidth, tileHight)
                    };
                    listResults.Add(new Tuple<Point, DavinciImage>(new Point(x, y), new DavinciImage(cropEffect)));

                }
            }

            return listResults;
        }

        public void Dispose()
        {
            while (_sourceHistory.Any())
            {
                (_sourceHistory.Pop() as IDisposable)?.Dispose();
            }
            _disposeAction?.Invoke();
        }
    }
}
