using Basler.Pylon;
using ContinuousShotApp.Helpers.Converters;
using ContinuousShotApp.Models.Camera;
using ContinuousShotApp.Utilities.Business;
using ContinuousShotApp.Utilities.Constants.ValidationMessages;
using ContinuousShotApp.Utilities.ExceptionMessage;
using ContinuousShotApp.Validations.Camera;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ContinuousShotApp.Helpers
{
    public class CameraHelper
    {
        Stopwatch stopwatch = new();
        public static CameraHelper Instance { get; } = new CameraHelper();

        public BitmapImage? GrabImage(Camera camera, CameraSettings cameraSettings, IGrabResult grabResult)
        {
            try
            {
                //imageViewer.Dispose(); 
                //SetChangeableCameraSettings(camera, GetChangeableCameraSettings(sliders));
                SetChangeableCameraSettings(camera, cameraSettings);



                Bitmap? bitmap = GrabResultConverterHelper.Instance.GrabResultToBitmap(grabResult);
                //SetCameraStartupSettings(camera, cameraSettings);
                //var gain = camera.Parameters[PLCamera.Gain].GetValue();
                if (grabResult.IsValid)
                {
                    stopwatch.Restart();

                    BitmapImage? bitmapImage = null;
                    if (bitmapImage is not null)
                    {
                        Bitmap? image = bitmap;
                        image!.Dispose();
                    }
                    bitmapImage = GrabResultConverterHelper.Instance.BitmapToImageSource(bitmap!);
                    return bitmapImage!;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"An error occured: {ex.Message}_{MethodBase.GetCurrentMethod()!.Name}");
                return null;
            }
        }

        public void Stop(Camera? camera)
        {
            var result = BusinessRules.Run(CameraStateValidator.CheckCameraIsNull(camera), CameraStateValidator.CheckCameraIsNotGrabbing(camera));

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.Message);
                return;
            }

            try
            {
                camera?.StreamGrabber.Stop();
            }
            catch (Exception ex)
            {
                ExceptionMessage.ShowException(ex, MethodBase.GetCurrentMethod()!.Name);
            }
        }

        public void OneShot(Camera? camera, CameraSettings cameraSettings)
        {
            var result = BusinessRules.Run(CameraStateValidator.CheckCameraIsNull(camera), CameraStateValidator.CheckCameraIsGrabbing(camera));

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.Message);
                return;
            }

            try
            {
                SetCameraStartupSettings(camera, cameraSettings);

                Configuration.AcquireSingleFrame(camera, null);
                camera?.StreamGrabber.Start(1, GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
            }
            catch (Exception ex)
            {
                ExceptionMessage.ShowException(ex, MethodBase.GetCurrentMethod()!.Name);
            }
        }

        public void ContinuousShot(Camera? camera)
        {
            var result = BusinessRules.Run(CameraStateValidator.CheckCameraIsNull(camera), CameraStateValidator.CheckCameraIsGrabbing(camera));

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.Message);
                return;
            }

            try
            {
                Configuration.AcquireContinuous(camera, null);
                camera?.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
            }
            catch (Exception ex)
            {
                ExceptionMessage.ShowException(ex, MethodBase.GetCurrentMethod()!.Name);
            }
        }

        public void SetChangeableCameraSettings(Camera? camera, CameraSettings cameraSettings)
        {
            camera?.Parameters[PLCamera.Gain].TrySetValue(cameraSettings.Gain);
            camera?.Parameters[PLCamera.ExposureTime].TrySetValue(cameraSettings.ExposureTime);
            camera?.Parameters[PLCamera.Gamma].TrySetValue(cameraSettings.Gamma);
        }

        public CameraSettings GetChangeableCameraSettings(params Slider[] sliders) 
            => new CameraSettings()
                {
                    Gain = sliders[0].Value,
                    ExposureTime = sliders[1].Value,
                    Gamma = sliders[2].Value
                };

        public void SetFixedCameraSettings(Camera? camera, CameraSettings cameraSettings)
        {
            camera?.Parameters[PLCamera.Width].SetValue(cameraSettings.Width, IntegerValueCorrection.Nearest);
            camera?.Parameters[PLCamera.Height].TrySetValue(cameraSettings.Height, IntegerValueCorrection.Nearest);
        }

        public CameraSettings GetFixedCameraSettings(params Slider[] sliders) => new CameraSettings()
        {
            Width = Convert.ToInt32(sliders[0].Value),
            Height = Convert.ToInt32(sliders[1].Value),
        };


        public void SetCameraStartupSettings(Basler.Pylon.Camera? camera, CameraSettings cameraSettings)
        {
            

            camera?.Parameters[PLCamera.Gain].TrySetValue(cameraSettings.Gain);
            camera?.Parameters[PLCamera.ExposureTime].TrySetValue(cameraSettings.ExposureTime);
            camera?.Parameters[PLCamera.Gamma].TrySetValue(cameraSettings.Gamma);
            camera?.Parameters[PLCamera.Width].SetValue(cameraSettings.Width, IntegerValueCorrection.Nearest);
            camera?.Parameters[PLCamera.Height].TrySetValue(cameraSettings.Height, IntegerValueCorrection.Nearest);
        }

        public CameraSettings GetCameraStartupSettings(params Slider[] sliders)
            => new CameraSettings()
            {
                Gain = sliders[0].Value,
                ExposureTime = sliders[1].Value,
                Gamma = sliders[2].Value,
                Width = Convert.ToInt32(sliders[3].Value),
                Height = Convert.ToInt32(sliders[4].Value)
            };

    }
}
