using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
        private readonly Stack<DavinciImage> _sourceHistory;
        private IImageProvider _source;
        
        
        private DavinciImage(IImageProvider source)
        {
            _sourceHistory=new Stack<DavinciImage>();
            _source = source;
        }
        private DavinciImage(IImageProvider source, DavinciImage parent)
        {
            _sourceHistory = parent._sourceHistory;
            _sourceHistory.Push(parent);
            _source = source;
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
            var source =new DavinciImage(new ContrastEffect(this._source) {Level = level});
            return source;
        }

        public DavinciImage Sharpness(double level)
        {
            var source = new DavinciImage(new SharpnessEffect(this._source) { Level = level },this);
            return source;
            
        }

        public DavinciImage SaturationLightness(Curve lightnessCurve, Curve saturationCurve)
        {
            return new DavinciImage(new SaturationLightnessEffect(_source)
            {
                LightnessCurve = lightnessCurve,
                SaturationCurve = saturationCurve
            },this);
        }

        public DavinciImage TemperatureAndTint(double temperature, double tint)
        {
            return new DavinciImage(new TemperatureAndTintEffect(_source) {Temperature = temperature, Tint = tint},this);
        }

        public DavinciImage Scale(double scale)
        {
            return new DavinciImage(new ScaleEffect(_source) { Scale = scale },this);

            
        }

        public DavinciImage Crop(Rect area)
        {
            return new DavinciImage(new CropEffect(_source) {CropArea = area},this);
        }

        public async Task<BitmapImage> ToImageAsync()
        {
            using (var jpegRenderer = new JpegRenderer(_source))
            {
                var biSource = new BitmapImage();
                var pixels= await jpegRenderer.RenderAsync();
              
               await biSource.SetSourceAsync(pixels.AsStream().AsRandomAccessStream());
                return biSource;
            }
        }
        public async Task<Stream> ToStreamAsync()
        {
            using (var jpegRenderer = new JpegRenderer(_source))
            {
                var biSource = new BitmapImage();
                var pixels = await jpegRenderer.RenderAsync();
                return pixels.AsStream();
                
            }
        }
        public async Task<IBuffer> ToBufferAsync()
        {
            using (var jpegRenderer = new JpegRenderer(_source))
            {
                var pixels = await jpegRenderer.RenderAsync();
                return pixels;
            }
        }

        public async Task<IEnumerable<Tuple<Point, DavinciImage>>> ToTilesAsync(double tileWidth, double tileHight)
        {
            var listResults = new List<Tuple<Point, DavinciImage>>();

            Size size;
            size = await GetSize(_source);
            var numberOfHorizontalTiles = (int)size.Width / tileWidth;
            var numberOfVerticalTiles = (int)size.Height / tileHight;

            for (var y = 0; y < numberOfVerticalTiles; y++)
            {
                for (var x = 0; x < numberOfHorizontalTiles; x++)

                {
                    var cropEffect = new CropEffect(_source)
                    {
                        CropArea = new Rect(x * tileWidth, y * tileHight, tileWidth, tileHight)
                    };
                    listResults.Add(new Tuple<Point, DavinciImage>(new Point(x, y), new DavinciImage(cropEffect)));

                }
            }

            return listResults;
        }
        [Pure]
        private async Task<Size> GetSize(IImageProvider source)
        {
            if (source is ScaleEffect)
            {
               
                    var imageProviderInfo = await GetSize((source as ScaleEffect).Source);
                    return new Size(imageProviderInfo.Width * (source as ScaleEffect).Scale,
                        imageProviderInfo.Height * (source as ScaleEffect).Scale);
                
            }
            else
                return (await source.GetInfoAsync()).ImageSize;
        }

        public void Dispose()
        {
            while (_sourceHistory.Any())
            {
                (_sourceHistory.Pop() as IDisposable)?.Dispose();
            }
        }
    }
}
