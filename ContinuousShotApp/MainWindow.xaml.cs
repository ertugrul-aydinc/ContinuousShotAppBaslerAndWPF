using Basler.Pylon;
using ContinuousShotApp.Models.Camera;
using ContinuousShotApp.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ContinuousShotApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Camera? camera = null;
        Stopwatch stopwatch = new();
        private PixelDataConverter converter = new();

        public MainWindow()
        {
            InitializeComponent();

            GetDeviceList();
        }

        public void GetDeviceList()
        {
            try
            {
                List<ICameraInfo> allCameras = CameraFinder.Enumerate();
                //cbxDevices.ItemsSource= allCameras;

                List<ComboBoxPairs> cbp = new();

                foreach (ICameraInfo cameraInfo in allCameras)
                {
                    cbp.Add(new ComboBoxPairs(cameraInfo, cameraInfo[CameraInfoKey.FriendlyName]));
                }

                cbxDevices.DisplayMemberPath = "_Value";
                cbxDevices.SelectedValuePath = "_Key";

                cbxDevices.ItemsSource = cbp;
                
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private void OnConnectionLost(Object sender, EventArgs e)
        {
            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke(new EventHandler<EventArgs>(OnConnectionLost!), sender, e);
                return;
            }

            DestroyCamera();
            GetDeviceList();
        }



        private void OnImageGrabbed(Object sender, ImageGrabbedEventArgs e)
        {
            //if (!CheckAccess())
            //{
            //    Dispatcher.BeginInvoke(new EventHandler<ImageGrabbedEventArgs>(OnImageGrabbed!), sender, e.Clone());
            //    return;
            //}
            

            try
            {
                
                IGrabResult grabResult = e.GrabResult;

                Dispatcher.Invoke(() =>
                {
                    SetChangeableCameraSettings(camera!, GetChangeableCameraSettings());
                });

                

                if (grabResult.IsValid)
                {
                    if(!stopwatch.IsRunning || stopwatch.ElapsedMilliseconds > 33)
                    {
                        stopwatch.Restart();

                        Bitmap bitmap = new Bitmap(grabResult.Width, grabResult.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                        BitmapData bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
                        converter.OutputPixelFormat = PixelType.BGR8packed;
                        IntPtr ptrBmp = bitmapData.Scan0;
                        converter.Convert(ptrBmp, bitmapData.Stride * bitmapData.Height, grabResult);
                        bitmap.UnlockBits(bitmapData);

                        BitmapImage? bitmapImage = null;

                        imageViewer.Dispatcher.Invoke(() =>
                        {
                            bitmapImage = BitmapToImageSource(bitmap);
                            imageViewer.Source = bitmapImage;
                        });

                        if(bitmapImage is not null)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                Bitmap image = bitmap;
                                image.Dispose();
                            });
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
            finally
            {
                e.DisposeGrabResultIfClone();
            }
        }
        
        private void OnGrabStopped(Object sender, GrabStopEventArgs e)
        {
            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke(new EventHandler<GrabStopEventArgs>(OnGrabStopped!), sender, e);
                return;
            }

            stopwatch.Reset();

            if (e.Reason != GrabStopReason.UserRequest)
                MessageBox.Show($"A grab error occured:\n{e.ErrorMessage}");
        }

        private void OnGrabStarted(Object sender, EventArgs e)
        {
            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke(new EventHandler<EventArgs>(OnGrabStarted!), sender, e);
                return;
            }

            stopwatch.Reset();
        }

        private void OnCameraOpened(Object sender, EventArgs e)
        {
            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke(new EventHandler<EventArgs>(OnCameraOpened!), sender, e);
                return;
            }
        }

        private void OnCameraClosed(Object sender, EventArgs e)
        {
            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke(new EventHandler<EventArgs>(OnCameraClosed!), sender, e);
                return;
            }
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
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

        private void Stop()
        {
            try
            {
                camera?.StreamGrabber.Stop();
                //camera!.Close();
                //camera = null;
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private void OneShot()
        {
            try
            {
                Configuration.AcquireSingleFrame(camera, null);
                camera?.StreamGrabber.Start(1, GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private void ContinuousShot()
        {
            try
            {
                if(camera is null)
                {
                    MessageBox.Show("Please select a device.");
                    return;
                }

                SetFixedCameraSettings(camera, GetFixedCameraSettings());
                Configuration.AcquireContinuous(camera, null);
                camera?.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        

        private void cbxDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(camera is not null)
                DestroyCamera();

            if (cbxDevices.Items.Count > 0)
            {
                ICameraInfo? selectedCamera = cbxDevices.SelectedValue as ICameraInfo;

               
                try
                {
                    camera = new Camera(selectedCamera);

                    camera.CameraOpened += Configuration.AcquireContinuous;

                    camera.CameraOpened += OnCameraOpened!;
                    camera.CameraClosed += OnCameraClosed!;

                    camera.StreamGrabber.GrabStarted += OnGrabStarted!;
                    camera.StreamGrabber.ImageGrabbed += OnImageGrabbed!;
                    camera.StreamGrabber.GrabStopped += OnGrabStopped!;

                    camera.Open();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error: {ex.Message}_{MethodBase.GetCurrentMethod()!.Name}");
                }

            }
        }

        #region UIElements
        private double TextBoxToSlider(double min, double max, Slider slider, string text)
        {
            try
            {
                double value;

                if (double.TryParse(text, out value))
                    if (!(value < min || value > max))
                        slider.Value = value;

                return value;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message} --- {MethodBase.GetCurrentMethod()!.Name}");
                return -1;
            }
        }

        private void txtHeight_TextChanged(object sender, TextChangedEventArgs e)
        {
            var value = TextBoxToSlider(320, 2064, heightSlider, txtHeight.Text);
            if (value % 2 != 0)
                heightSlider.Value = value - 1;
        }

        private void txtGain_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBoxToSlider(0, 36, gainSlider, txtGain.Text);
        }

        private void txtExposureTime_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBoxToSlider(8, 9999992, exposureSlider, txtExposureTime.Text);
        }

        private void txtWidth_TextChanged(object sender, TextChangedEventArgs e)
        {
            var value = TextBoxToSlider(376, 3088, widthSlider, txtWidth.Text);
            if (value % 2 != 0)
                widthSlider.Value = value - 1;
        }

        private void btnTakeVideo_Click(object sender, RoutedEventArgs e)
        {
            ContinuousShot();
        }

        private void btnStopVideo_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }
        #endregion

        private void DestroyCamera()
        {
            try
            {
                if(camera is not null)
                {
                    camera.Close();
                    //camera.Dispose();
                    camera = null;
                }
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private void ShowException(Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}_{MethodBase.GetCurrentMethod()!.Name}");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //DestroyCamera();
        }

        private void SetChangeableCameraSettings(Camera camera, CameraSettings cameraSettings)
        {
            //camera.Parameters[PLCamera.Width].SetValue(cameraSettings.Width, IntegerValueCorrection.Nearest);
            //camera.Parameters[PLCamera.Height].TrySetValue(cameraSettings.Height, IntegerValueCorrection.Nearest);
            camera.Parameters[PLCamera.Gain].TrySetValue(cameraSettings.Gain);
            camera.Parameters[PLCamera.ExposureTime].TrySetValue(cameraSettings.ExposureTime);
        }

        private CameraSettings GetChangeableCameraSettings() => new CameraSettings()
        {
            //Width = Convert.ToInt32(widthSlider.Value),
            //Height = Convert.ToInt32(heightSlider.Value),
            Gain = gainSlider.Value,
            ExposureTime = exposureSlider.Value
        };

        private void SetFixedCameraSettings(Camera camera, CameraSettings cameraSettings)
        {
            camera.Parameters[PLCamera.Width].SetValue(cameraSettings.Width, IntegerValueCorrection.Nearest);
            camera.Parameters[PLCamera.Height].TrySetValue(cameraSettings.Height, IntegerValueCorrection.Nearest);
        }

        private CameraSettings GetFixedCameraSettings() => new CameraSettings()
        {
            Width = Convert.ToInt32(widthSlider.Value),
            Height = Convert.ToInt32(heightSlider.Value),
        };

    }
}
