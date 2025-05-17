using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ZXing.Common;
using ZXing.ImageSharp;

namespace CheapHelpers.Services
{
    public class BarcodeService : IBarcodeService
    {
        public BarcodeService()
        {
        }

        //public event System.Action<string> BarcodeScanned = delegate { };

        private event Func<string, Task> Handler;

        event Func<string, Task> IBarcodeService.BarcodeScanned
        {
            add { Handler += value; }
            remove { Handler -= value; }
        }

        public byte[] GetBarcode(string input, int height = 30, int width = 100)
        {
            var barcodeWriter = new ZXing.ImageSharp.BarcodeWriter<Rgba32>()
            {
                Format = ZXing.BarcodeFormat.CODE_39,
                Options = new EncodingOptions
                {
                    Height = height,
                    Width = width,
                    //Margin = margin
                }
            };

            using (var image = barcodeWriter.Write(input))
            {
                using (var ms = new MemoryStream())
                {
                    image.Save(ms, new JpegEncoder());
                    return ms.ToArray();
                }
            }
        }

        public void OnScan(string barcode)
        {
            Handler.Invoke(barcode);
        }

        /// <summary>
        /// reads an image from bitmap and returns the values, returns null if nothing found
        /// IronBarcode
        /// </summary>
        /// <returns>resulttext, format</returns>
        public async Task<(string, string)> ReadBarcode(byte[] bytes, int width, int height)
        {
            throw new NotImplementedException();
            //var source = new RGBLuminanceSource(bytes, width, height);
            //// create a barcode reader instance
            //var reader = new ZXing.ImageSharp.BarcodeReader<Image>();
            //// load a bitmap
            //var barcodeBitmap = Image.Load(bytes);
            //// detect and decode the barcode inside the bitmap
            //var result = reader.Decode(bytes, width, height, (RGBLuminanceSource.BitmapFormat)4);

            //return (result.Text, result.BarcodeFormat.ToString());
        }
    }
}
