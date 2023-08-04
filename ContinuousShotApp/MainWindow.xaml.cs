using Basler.Pylon;
using ContinuousShotApp.Helpers;
using ContinuousShotApp.Helpers.Converters;
using ContinuousShotApp.Models.Camera;
using ContinuousShotApp.Utilities;
using ContinuousShotApp.Utilities.Enums;
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
        private ImageType imgType = ImageType.jpeg;

        private Camera? camera = null;
        private Stopwatch stopwatch = new();

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

        #region Camera Events
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

                Bitmap? bitmap = GrabResultConverterHelper.Instance.GrabResultToBitmap(grabResult);

                if (grabResult.IsValid)
                {
                    if (!stopwatch.IsRunning || stopwatch.ElapsedMilliseconds > 33)
                    {
                        stopwatch.Restart();

                        BitmapImage? bitmapImage = null;

                        imageViewer.Dispatcher.Invoke(() =>
                        {
                            bitmapImage = GrabResultConverterHelper.Instance.BitmapToImageSource(bitmap!);
                            imageViewer.Source = bitmapImage;
                        });

                        if (bitmapImage is not null)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                Bitmap? image = bitmap;
                                image!.Dispose();
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

        #endregion

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //DestroyCamera();
        }

        private void Stop()
        {
            try
            {
                camera?.StreamGrabber.Stop();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private void DestroyCamera()
        {
            try
            {
                if (camera is not null)
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
                if (camera.StreamGrabber.IsGrabbing)
                {
                    MessageBox.Show("Camera is already grabbing");
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

        #region UIElements
        private void cbxDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (camera is not null)
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
                    ShowException(ex);
                }

            }
        }

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
                ShowException(ex);
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

        private void txtGamma_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBoxToSlider(0, 3.99, gammaSlider, txtGamma.Text);
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

        private void DisableStopButton()
        {

        }
        #endregion

        private void ShowException(Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}_{MethodBase.GetCurrentMethod()!.Name}");
        }


        #region CameraSettings
        private void SetChangeableCameraSettings(Camera camera, CameraSettings cameraSettings)
        {
            camera.Parameters[PLCamera.Gain].TrySetValue(cameraSettings.Gain);
            camera.Parameters[PLCamera.ExposureTime].TrySetValue(cameraSettings.ExposureTime);
            camera.Parameters[PLCamera.Gamma].TrySetValue(cameraSettings.Gamma);
        }

        private CameraSettings GetChangeableCameraSettings() => new CameraSettings()
        {
            Gain = gainSlider.Value,
            ExposureTime = exposureSlider.Value,
            Gamma = gammaSlider.Value
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
        #endregion


        private void btnSingleShot_Click(object sender, RoutedEventArgs e)
        {
            if(camera is null)
            {
                MessageBox.Show("Please select a camera device");
                return;
            }

            if (imageViewer.Source is null)
            {
                MessageBox.Show("Camera is not grabbing");
                return;
            }

            BitmapImage image = (BitmapImage)imageViewer.Source;
            var savingImage = GrabResultConverterHelper.Instance.BitmapImage2Bitmap(image);
            FileHelper.Instance.SaveImageFile(imgType, savingImage);
        }

        private void MenuItem_Checked(object sender, RoutedEventArgs e)
        {
            
        }

        #region MenuItems
        private void jpegItem_Click(object sender, RoutedEventArgs e)
        {
            imgType = SetImageType((sender as MenuItem)!, ImageType.jpeg);
        }

        private void pngItem_Click(object sender, RoutedEventArgs e)
        {
            imgType = SetImageType((sender as MenuItem)!, ImageType.png);
        }

        private void webpItem_Click(object sender, RoutedEventArgs e)
        {
            imgType = SetImageType((sender as MenuItem)!, ImageType.webp);
        }

        private void heifItem_Click(object sender, RoutedEventArgs e)
        {
            imgType = SetImageType((sender as MenuItem)!, ImageType.heif);
        }

        #endregion

        private void btnTakeFrame_Click(object sender, RoutedEventArgs e)
        {
            if(camera is null)
            {
                MessageBox.Show("Please select a camera device");
                return;
            }

            if (camera.StreamGrabber.IsGrabbing)
            {
                MessageBox.Show("Please stop video and try again");
                return;
            }

            OneShot();
        }

        private ImageType SetImageType(MenuItem menuItem, ImageType type)
        {
            foreach (MenuItem item in settingsMenu.Items)
            {
                if (item != menuItem)
                    item.IsChecked = false;
            }

            menuItem.IsChecked = true;
            return type;
        }

    }
}
