using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            UpdateClockDisplay();
        }

        public MainViewModel ViewModel => _viewModel;

        private void StartClock()
        {
            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += (s, e) => UpdateClockDisplay();
            _clockTimer.Start();
        }

        private void UpdateClockDisplay()
        {
            Dispatcher.Invoke(() =>
            {
                ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
                CurrentDateText.Text = DateTime.Now.ToString("dd-MMM-yyyy");
            });
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var aboutDialog = new Views.About.AboutDialog();
            aboutDialog.Owner = this;
            aboutDialog.ShowDialog();
        }

        // ═══════════════════════════════════════════════════════
        // NAVIGATION HANDLERS
        // ═══════════════════════════════════════════════════════
        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowDashboard();
            AnimateButton(BtnDashboard, BorderSGU);
        }

        private void BtnSubscription_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowSubscriptionPlan();
            AnimateButton(BtnSubscription, BorderSGU);
        }

        private void BtnSGU_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.IsLicensed)
            {
                MessageBox.Show("Please activate your license to access calculators",
                    "License Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                _viewModel.ShowSubscriptionPlan();
                return;
            }
            _viewModel.ShowSGUCalculator();
            AnimateButton(BtnSGU, BorderSGU);
        }

        private void BtnDGU_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.IsLicensed)
            {
                MessageBox.Show("Please activate your license to access calculators",
                    "License Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                _viewModel.ShowSubscriptionPlan();
                return;
            }
            _viewModel.ShowDGUCalculator();
            AnimateButton(BtnDGU, BorderDGU);
        }

        private void BtnLamination_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.IsLicensed)
            {
                MessageBox.Show("Please activate your license to access calculators",
                    "License Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                _viewModel.ShowSubscriptionPlan();
                return;
            }
            _viewModel.ShowLaminationCalculator();
            AnimateButton(BtnLam, BorderLam);
        }

        private void BtnDguLam_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.IsLicensed)
            {
                MessageBox.Show("Please activate your license to access calculators",
                    "License Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                _viewModel.ShowSubscriptionPlan();
                return;
            }
            _viewModel.ShowDGULaminationCalculator();
            AnimateButton(BtnDguLam, BorderDguLam);
        }

        private void BtnOptimization_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.IsLicensed)
            {
                MessageBox.Show("Please activate your license to access calculators",
                    "License Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                _viewModel.ShowSubscriptionPlan();
                return;
            }
            _viewModel.ShowGlassOptimization();
            AnimateButton(BtnOpt, BorderOpt);
        }

        private void BtnSheetStore_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowSheetStore();
        }

        private void BtnDailyWorks_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowDailyWorks();
        }

        private void BtnDeliveries_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowDeliveries();
        }

        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowProfile();
        }

        private void BtnUsers_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowUsers();
        }

        // ═══════════════════════════════════════════════════════
        // BUTTON ANIMATION
        // ═══════════════════════════════════════════════════════
        private void AnimateButton(Button btn, Border border)
        {
            if (btn == null || border == null) return;

            DoubleAnimation animation = new DoubleAnimation(1.02, TimeSpan.FromMilliseconds(100));
            border.RenderTransformOrigin = new Point(0.5, 0.5);
            if (border.RenderTransform == null || !(border.RenderTransform is ScaleTransform))
            {
                border.RenderTransform = new ScaleTransform(1, 1);
            }
            var scale = (ScaleTransform)border.RenderTransform;
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);

            // Reset after delay
            var resetAnimation = new DoubleAnimation(1, TimeSpan.FromMilliseconds(100))
            {
                BeginTime = TimeSpan.FromMilliseconds(150)
            };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, resetAnimation);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, resetAnimation);
        }

        // ═══════════════════════════════════════════════════════
        // SCREENSHOT - MAXIMUM QUALITY + FAST
        // ═══════════════════════════════════════════════════════
        private void ScreenshotBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int width = (int)(this.ActualWidth * 2);
                int height = (int)(this.ActualHeight * 2);

                if (width <= 0 || height <= 0)
                {
                    MessageBox.Show("Please wait for the page to fully load.", "Error");
                    return;
                }

                var screenshotBtn = GetScreenshotButton();
                if (screenshotBtn != null)
                {
                    screenshotBtn.Visibility = Visibility.Collapsed;
                }

                Dispatcher.Invoke(() => { }, DispatcherPriority.Render);

                RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                    width,
                    height,
                    192,
                    192,
                    PixelFormats.Pbgra32);

                RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
                renderBitmap.Render(this);

                if (screenshotBtn != null)
                {
                    screenshotBtn.Visibility = Visibility.Visible;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JPEG Image|*.jpg",
                    FileName = $"ScreenCapture_{DateTime.Now:yyyyMMdd_HHmmss}.jpg"
                };

                if (dialog.ShowDialog() == true)
                {
                    JpegBitmapEncoder encoder = new JpegBitmapEncoder
                    {
                        QualityLevel = 100
                    };
                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                    using (var stream = new System.IO.FileStream(dialog.FileName, System.IO.FileMode.Create))
                    {
                        encoder.Save(stream);
                    }

                    MessageBox.Show($"Screenshot saved!\n\n📁 {dialog.FileName}\n\nResolution: {width} × {height}\nQuality: Maximum", "Success");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private Button GetScreenshotButton()
        {
            if (FindName("ScreenshotBtn") is Button btn)
                return btn;

            foreach (var child in GetAllChildren(this))
            {
                if (child is Button button && button.Content?.ToString()?.Contains("Screenshot") == true)
                    return button;
            }

            return null;
        }

        private System.Collections.Generic.IEnumerable<DependencyObject> GetAllChildren(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                yield return child;
                foreach (var descendant in GetAllChildren(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}