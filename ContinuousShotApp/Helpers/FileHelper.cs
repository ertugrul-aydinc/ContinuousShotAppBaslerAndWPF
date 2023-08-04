using ContinuousShotApp.Helpers.Converters;
using ContinuousShotApp.Utilities.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ContinuousShotApp.Helpers
{
    public class FileHelper
    {
        public static FileHelper Instance { get; } = new FileHelper();

        public void SaveImageFile(ImageType imageType, Bitmap image)
        {
            string mainPath = @$"C:\Users\VM-Support\source\repos\ContinuousShotApp\ContinuousShotApp\images\{DateTime.Now.Day}-{DateTime.Now.Month}-{DateTime.Now.Year}\";

            if (!Directory.Exists(mainPath))
                Directory.CreateDirectory(mainPath);

            image.Save($"{mainPath}{Guid.NewGuid()}.{imageType}");

            MessageBox.Show("Image saved successfully.");
        }
    }
}
