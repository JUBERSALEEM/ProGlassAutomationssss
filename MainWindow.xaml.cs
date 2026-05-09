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
        // DASHBOARD BOUNCY EFFECT
        // ═══════════════════════════════════════════════════════
        private void BtnDashboard_MouseEnter(object sender, MouseEventArgs e)
        {
            if (FindVisualChild<Border>(BtnDashboard) is Border border)
            {
                DoubleAnimation animation = new DoubleAnimation(1.02, TimeSpan.FromMilliseconds(100));
                border.RenderTransformOrigin = new Point(0.5, 0.5);
                if (border.RenderTransform == null || !(border.RenderTransform is ScaleTransform))
                {
                    border.RenderTransform = new ScaleTransform(1, 1);
                }
                var scale = (ScaleTransform)border.RenderTransform;
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
            }
        }

        private void BtnDashboard_MouseLeave(object sender, MouseEventArgs e)
        {
            if (FindVisualChild<Border>(BtnDashboard) is Border border)
            {
                DoubleAnimation animation = new DoubleAnimation(1, TimeSpan.FromMilliseconds(100));
                var scale = (ScaleTransform)border.RenderTransform;
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
            }
        }

        private void BtnSubscription_MouseEnter(object sender, MouseEventArgs e)
        {
            if (FindVisualChild<Border>(BtnSubscription) is Border border)
            {
                DoubleAnimation animation = new DoubleAnimation(1.02, TimeSpan.FromMilliseconds(100));
                border.RenderTransformOrigin = new Point(0.5, 0.5);
                if (border.RenderTransform == null || !(border.RenderTransform is ScaleTransform))
                {
                    border.RenderTransform = new ScaleTransform(1, 1);
                }
                var scale = (ScaleTransform)border.RenderTransform;
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
            }
        }

        private void BtnSubscription_MouseLeave(object sender, MouseEventArgs e)
        {
            if (FindVisualChild<Border>(BtnSubscription) is Border border)
            {
                DoubleAnimation animation = new DoubleAnimation(1, TimeSpan.FromMilliseconds(100));
                var scale = (ScaleTransform)border.RenderTransform;
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
            }
        }

        // ═══════════════════════════════════════════════════════
        // CALCULATOR BOUNCY EFFECT
        // ═══════════════════════════════════════════════════════
        private void CalcBtn_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button btn && FindVisualChild<Border>(btn) is Border border)
            {
                AnimateScale(border, 1.02);
            }
        }

        private void CalcBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Button btn && FindVisualChild<Border>(btn) is Border border)
            {
                AnimateScale(border, 1);
            }
        }

        private void AnimateScale(Border border, double targetValue)
        {
            border.RenderTransformOrigin = new Point(0.5, 0.5);
            if (border.RenderTransform == null || !(border.RenderTransform is ScaleTransform))
            {
                border.RenderTransform = new ScaleTransform(1, 1);
            }
            var scale = (ScaleTransform)border.RenderTransform;
            DoubleAnimation animation = new DoubleAnimation(targetValue, TimeSpan.FromMilliseconds(100));
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
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

        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;
                var found = FindVisualChild<T>(child);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}