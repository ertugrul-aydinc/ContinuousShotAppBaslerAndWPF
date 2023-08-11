using Basler.Pylon;
using ContinuousShotApp.Helpers.Converters;
using ContinuousShotApp.Utilities.Business;
using ContinuousShotApp.Utilities.Enums;
using ContinuousShotApp.Validations.Camera;
using ContinuousShotApp.Validations.ImageSource;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ContinuousShotApp.Helpers
{
    public class FileHelper
    {
        public static FileHelper Instance { get; } = new FileHelper();

        public void SaveImageFile(Camera? camera, ImageType imageType, System.Windows.Controls.Image imageViewer)
        {
            string mainPath = @$"C:\Users\VM-Support\source\repos\ContinuousShotApp\ContinuousShotApp\images\{DateTime.Now.Day}-{DateTime.Now.Month}-{DateTime.Now.Year}\";

            if (!Directory.Exists(mainPath))
                Directory.CreateDirectory(mainPath);

            try
            {
                var result = BusinessRules.Run(CameraStateValidator.CheckCameraIsNull(camera), ImageSourceValidator.CheckImageSourceIsNull(imageViewer));

                if (!result.IsSuccess)
                {
                    MessageBox.Show(result.Message);
                    return;
                }

                BitmapImage bitmapImage = (BitmapImage)imageViewer.Source;
                var savingImage = GrabResultConverterHelper.Instance.BitmapImage2Bitmap(bitmapImage);

                savingImage.Save($"{mainPath}{Guid.NewGuid()}.{imageType}");

                MessageBox.Show("Image saved successfully.");
            }
            catch (Exception)
            {
                MessageBox.Show("An error occured during save photo");
            }
        }
    }
}
