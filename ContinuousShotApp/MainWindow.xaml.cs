using Basler.Pylon;
using ContinuousShotApp.Helpers;
using ContinuousShotApp.Helpers.Converters;
using ContinuousShotApp.Models.Camera;
using ContinuousShotApp.Utilities;
using ContinuousShotApp.Utilities.Business;
using ContinuousShotApp.Utilities.Enums;
using ContinuousShotApp.Utilities.ExceptionMessage;
using ContinuousShotApp.Validations.Camera;
using ContinuousShotApp.Validations.ImageSource;
using ContinuousShotApp.Views;
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
                ExceptionMessage.ShowException(ex, MethodBase.GetCurrentMethod()!.Name);
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
            imageViewer.Dispatcher.Invoke(() =>
            {
                //CameraHelper.Instance.SetChangeableCameraSettings(camera!, CameraHelper.Instance.GetChangeableCameraSettings(gainSlider, exposureSlider, gammaSlider));
                //imageViewer.Source = CameraHelper.Instance.GrabImage(e.GrabResult);

                imageViewer.Source = CameraHelper.Instance.GrabImage(camera!, GetChangeableCameraSettings(), e.GrabResult);
            });
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
            CameraHelper.Instance.SetCameraStartupSettings(camera, CameraHelper.Instance.GetCameraStartupSettings(gainSlider, exposureSlider, gammaSlider, widthSlider, heightSlider));
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
            CameraHelper.Instance.Stop(camera);
            EnableFixedFeatureSliders();
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
                ExceptionMessage.ShowException(ex, MethodBase.GetCurrentMethod()!.Name);
            }
        }

        private void OneShot()
        {
            CameraHelper.Instance.OneShot(camera, GetCurrentCameraSettings());
        }

        private void ContinuousShot()
        {
            CameraHelper.Instance.ContinuousShot(camera);

            DisableFixedFeatureSliders();
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
                    ExceptionMessage.ShowException(ex, MethodBase.GetCurrentMethod()!.Name);
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
                ExceptionMessage.ShowException(ex, MethodBase.GetCurrentMethod()!.Name);
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
            //txtGain.Text = txtGain.Text == string.Empty ? "" : String.Format("{0:0.000}", Convert.ToDouble(txtGain.Text));
            txtGain.Text = FormatNumbers(txtGain.Text);
        }

        private void txtGamma_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBoxToSlider(0, 3.99, gammaSlider, txtGamma.Text);
            txtGamma.Text = FormatNumbers(txtGamma.Text);
        }

        private string? FormatNumbers(string text) => String.Format("{0:0.000}", Convert.ToDouble(text)) ?? string.Empty;


        private void txtExposureTime_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBoxToSlider(8, 9999992, exposureSlider, txtExposureTime.Text);
            txtExposureTime.Text = FormatNumbers(txtExposureTime.Text);
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

        private void btnSingleShot_Click(object sender, RoutedEventArgs e)
        {
            SaveFrame();
        }

        private void SaveFrame()
        {
            FileHelper.Instance.SaveImageFile(camera, imgType, imageViewer);
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

        private void SetSlidersEnableAndDisableSettings()
        {

            if (gainAutoSettings.Items.Count > 0)
            {
                ComboBoxItem? item = gainAutoSettings.SelectedItem as ComboBoxItem;

                if (gainSlider is not null)
                {
                    if (item?.Name != "Off")
                    {
                        gainSlider.IsEnabled = false;
                        txtGain.IsEnabled = false;
                    }

                    else
                    {
                        gainSlider.IsEnabled = true;
                        txtGain.IsEnabled = true;
                    }
                }
            }

        }

        private void DisableFixedFeatureSliders()
        {
            widthSlider.IsEnabled = false;
            heightSlider.IsEnabled = false;
        }

        private void EnableFixedFeatureSliders()
        {
            widthSlider.IsEnabled = true;
            heightSlider.IsEnabled = true;
        }

        private void gainAutoSettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetSlidersEnableAndDisableSettings();
        }

        private void deviceInfos_Click(object sender, RoutedEventArgs e)
        {
            Info info = new Info();

            if (camera is null)
            {
                MessageBox.Show("Please select device first");
                return;
            }

            var cameraInfo = camera.CameraInfo;
            info.lblVendorName.Content = cameraInfo[CameraInfoKey.VendorName];
            info.lblModelName.Content = cameraInfo[CameraInfoKey.ModelName];
            info.lblManufacturerInfo.Content = cameraInfo[CameraInfoKey.ManufacturerInfo];
            info.lblSerialNumber.Content = cameraInfo[CameraInfoKey.SerialNumber];
            info.lblVersion.Content = Library.VersionInfo.ToString();
            info.DataContext = camera;
            info.Show();
        }

        #region Icon Buttons
        private DateTime downTime;
        private object downSender;
        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.downSender = sender;
                this.downTime = DateTime.Now;
            }
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ExecuteMethod(OneShot, sender, e);
        }

        private void Image_MouseUp_1(object sender, MouseButtonEventArgs e)
        {
            ExecuteMethod(ContinuousShot, sender, e);
        }

        private void saveIcon_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ExecuteMethod(SaveFrame, sender, e);
        }

        private void stopVideoIcon_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ExecuteMethod(Stop, sender, e);
        }

        delegate void MyImageButtonDelegate();
        private void ExecuteMethod(MyImageButtonDelegate myImageButtonDelegate, object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released &&
            sender == this.downSender)
            {
                TimeSpan timeSinceDown = DateTime.Now - this.downTime;
                if (timeSinceDown.TotalMilliseconds < 500)
                    myImageButtonDelegate.Invoke();

            }
        }
        #endregion

        private CameraSettings GetCurrentCameraSettings() => new CameraSettings()
        {
            Width = Convert.ToInt32(widthSlider.Value),
            Height = Convert.ToInt32(heightSlider.Value),
            Gain = gainSlider.Value,
            ExposureTime = exposureSlider.Value,
            Gamma = gammaSlider.Value
        };

        private CameraSettings GetChangeableCameraSettings() => new CameraSettings()
        {
            Gain = gainSlider.Value,
            ExposureTime = exposureSlider.Value,
            Gamma = gammaSlider.Value
        };
    }
}