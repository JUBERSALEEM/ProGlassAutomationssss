using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ProGlassAutomation.ViewModels;

namespace ProGlassAutomation
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private DispatcherTimer _clockTimer;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            StartClock();
            UpdateLicenseStatus();

            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.IsLicensed))
                {
                    UpdateLicenseStatus();
                }
            };
        }

        private void UpdateLicenseStatus()
        {
            if (_viewModel.IsLicensed)
            {
                LicenseWarningBorder.Visibility = Visibility.Collapsed;
                LicenseActiveBorder.Visibility = Visibility.Visible;
            }
            else
            {
                LicenseWarningBorder.Visibility = Visibility.Visible;
                LicenseActiveBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void LicenseWarningBorder_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _viewModel.ShowSubscriptionPlan();
        }

        private void StartClock()
        {
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => {
                ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
                CurrentDateText.Text = DateTime.Now.ToString("dd-MMM-yyyy");
            };
            _clockTimer.Start();
        }

        private void AnimateButton(Button btn)
        {
            if (btn == null) return;
            btn.RenderTransformOrigin = new Point(0.5, 0.5);
            if (!(btn.RenderTransform is ScaleTransform)) btn.RenderTransform = new ScaleTransform(1, 1);
            var scale = (ScaleTransform)btn.RenderTransform;

            DoubleAnimation grow = new DoubleAnimation(1.04, TimeSpan.FromMilliseconds(100));
            DoubleAnimation shrink = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(100)) { BeginTime = TimeSpan.FromMilliseconds(100) };

            scale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, shrink);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, shrink);
        }

        private bool CheckLicense()
        {
            if (!_viewModel.IsLicensed)
            {
                MessageBox.Show("Security Warning: Enterprise License Required.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                _viewModel.ShowSubscriptionPlan();
                UpdateLicenseStatus();
                return false;
            }
            return true;
        }

        // ==================== NAVIGATION BUTTONS ====================

        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowDashboard();
            AnimateButton(BtnHome);
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowDashboard();
            AnimateButton(BtnDashboard);
        }

        private void BtnSubscription_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowSubscriptionPlan();
            AnimateButton(BtnSubscription);
        }

        // ==================== CALCULATORS DROPDOWN ====================

        private void BtnCalculators_Click(object sender, RoutedEventArgs e)
        {
            CalculatorsMenu.PlacementTarget = BtnCalculators;
            CalculatorsMenu.IsOpen = true;
        }

        private void BtnSGU_Click(object sender, RoutedEventArgs e)
        {
            if (CheckLicense())
            {
                _viewModel.ShowSGUCalculator();
                AnimateButton(BtnCalculators);
            }
        }

        private void BtnDGU_Click(object sender, RoutedEventArgs e)
        {
            if (CheckLicense())
            {
                _viewModel.ShowDGUCalculator();
                AnimateButton(BtnCalculators);
            }
        }

        private void BtnLamination_Click(object sender, RoutedEventArgs e)
        {
            if (CheckLicense())
            {
                _viewModel.ShowLaminationCalculator();
                AnimateButton(BtnCalculators);
            }
        }

        private void BtnDguLam_Click(object sender, RoutedEventArgs e)
        {
            if (CheckLicense())
            {
                _viewModel.ShowDGULaminationCalculator();
                AnimateButton(BtnCalculators);
            }
        }

        private void BtnOptimization_Click(object sender, RoutedEventArgs e)
        {
            if (CheckLicense())
            {
                _viewModel.ShowGlassOptimization();
                AnimateButton(BtnCalculators);
            }
        }

        // ==================== OPERATIONS DROPDOWN ====================

        private void BtnOperations_Click(object sender, RoutedEventArgs e)
        {
            OperationsMenu.PlacementTarget = BtnOperations;
            OperationsMenu.IsOpen = true;
        }

        private void BtnSheetStore_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowSheetStore();
            AnimateButton(BtnOperations);
        }

        private void BtnDailyWorks_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowDailyWorks();
            AnimateButton(BtnOperations);
        }

        private void BtnDeliveries_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowDeliveries();
            AnimateButton(BtnOperations);
        }

        // ==================== REPORTS DROPDOWN ====================

        private void BtnReports_Click(object sender, RoutedEventArgs e)
        {
            ReportsMenu.PlacementTarget = BtnReports;
            ReportsMenu.IsOpen = true;
        }

        // ==================== SETTINGS DROPDOWN ====================

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsMenu.PlacementTarget = BtnSettings;
            SettingsMenu.IsOpen = true;
        }

        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowProfile();
            AnimateButton(BtnSettings);
        }

        private void BtnUsers_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowUsers();
            AnimateButton(BtnSettings);
        }

        // ==================== SCREENSHOT ====================

        private void ScreenshotBtn_Click(object sender, RoutedEventArgs e)
        {
            CaptureScreenshot();
        }

        private void CaptureScreenshot()
        {
            try
            {
                double scale = 4.0;
                int width = (int)(ActualWidth * scale);
                int height = (int)(ActualHeight * scale);
                int dpi = (int)(96 * scale);

                var rtb = new RenderTargetBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32);

                UpdateLayout();
                InvalidateArrange();
                InvalidateMeasure();
                InvalidateVisual();

                rtb.Render(this);
                rtb.Freeze();

                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PNG Image (Lossless)|*.png|BMP Image|*.bmp|JPEG Image|*.jpg",
                    FileName = $"ProGlass_Capture_{DateTime.Now:HHmm}.png"
                };

                if (sfd.ShowDialog() == true)
                {
                    BitmapEncoder encoder;
                    string ext = System.IO.Path.GetExtension(sfd.FileName).ToLower();

                    switch (ext)
                    {
                        case ".png":
                            encoder = new PngBitmapEncoder();
                            break;
                        case ".bmp":
                            encoder = new BmpBitmapEncoder();
                            break;
                        case ".jpg":
                        default:
                            encoder = new JpegBitmapEncoder { QualityLevel = 100 };
                            break;
                    }

                    encoder.Frames.Add(BitmapFrame.Create(rtb));

                    var directory = System.IO.Path.GetDirectoryName(sfd.FileName);
                    if (!System.IO.Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
                        System.IO.Directory.CreateDirectory(directory);

                    using (var fs = System.IO.File.OpenWrite(sfd.FileName))
                        encoder.Save(fs);

                    var fileInfo = new System.IO.FileInfo(sfd.FileName);
                    MessageBox.Show(
                        $"Screenshot saved!\n\nFile: {sfd.FileName}\nSize: {fileInfo.Length / 1024} KB",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}