using Basler.Pylon;
using ContinuousShotApp.Utilities.ExceptionMessage;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ContinuousShotApp.Helpers.Converters
{
    public class GrabResultConverterHelper
    {
        private readonly PixelDataConverter converter = new();

        public static GrabResultConverterHelper Instance { get;} = new GrabResultConverterHelper();


        public BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        public Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                Bitmap bitmap = new Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }

        public Bitmap? GrabResultToBitmap(IGrabResult grabResult)
        {
            try
            {
                Bitmap bitmap = new Bitmap(grabResult.Width, grabResult.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                
                BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
                converter.OutputPixelFormat = PixelType.BGR8packed;
                IntPtr ptrBmp = bitmapData.Scan0;
                converter.Convert(ptrBmp, bitmapData.Stride * bitmapData.Height, grabResult);
                bitmap.UnlockBits(bitmapData);

                return bitmap;
            }
            catch (Exception ex)
            {
                ExceptionMessage.ShowException(ex, MethodBase.GetCurrentMethod()!.Name);

                return null;
            }
        }

        //private SetParametersToSelectedPixelFormat()
    }
}
