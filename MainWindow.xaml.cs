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

        // For smooth 60 FPS rendering
        private DateTime _fpsTimer = DateTime.MinValue;
        private int _frameCount = 0;

        public MainWindow()
        {
            InitializeComponent();

            // Enable composition target rendering for smooth 60 FPS
            CompositionTarget.Rendering += CompositionTarget_Rendering;

            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            StartClock();
            UpdateClockDisplay();
        }

        // ═══════════════════════════════════════════════════════
        // 60 FPS SMOOTH RENDERING
        // ═══════════════════════════════════════════════════════
        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            _frameCount++;
            var now = DateTime.Now;

            if ((now - _fpsTimer).TotalSeconds >= 1)
            {
                _fpsTimer = now;
                _frameCount = 0;
            }
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
            DoubleAnimation animation = new DoubleAnimation(1.05, TimeSpan.FromMilliseconds(100));
            DashScale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            DashScale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }

        private void BtnDashboard_MouseLeave(object sender, MouseEventArgs e)
        {
            DoubleAnimation animation = new DoubleAnimation(1, TimeSpan.FromMilliseconds(100));
            DashScale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            DashScale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }

        private void BtnSubscription_MouseEnter(object sender, MouseEventArgs e)
        {
            DoubleAnimation animation = new DoubleAnimation(1.05, TimeSpan.FromMilliseconds(100));
            SubScale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            SubScale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }

        private void BtnSubscription_MouseLeave(object sender, MouseEventArgs e)
        {
            DoubleAnimation animation = new DoubleAnimation(1, TimeSpan.FromMilliseconds(100));
            SubScale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            SubScale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }

        // ═══════════════════════════════════════════════════════
        // CALCULATOR BOUNCY EFFECT
        // ═══════════════════════════════════════════════════════
        private void CalcBtn_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender == BtnSGU)
            {
                AnimateScale(ScaleSGU, 1.05);
            }
            else if (sender == BtnDGU)
            {
                AnimateScale(ScaleDGU, 1.05);
            }
            else if (sender == BtnLam)
            {
                AnimateScale(ScaleLam, 1.05);
            }
            else if (sender == BtnDguLam)
            {
                AnimateScale(ScaleDguLam, 1.05);
            }
            else if (sender == BtnOpt)
            {
                AnimateScale(ScaleOpt, 1.05);
            }
        }

        private void CalcBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender == BtnSGU)
            {
                AnimateScale(ScaleSGU, 1);
            }
            else if (sender == BtnDGU)
            {
                AnimateScale(ScaleDGU, 1);
            }
            else if (sender == BtnLam)
            {
                AnimateScale(ScaleLam, 1);
            }
            else if (sender == BtnDguLam)
            {
                AnimateScale(ScaleDguLam, 1);
            }
            else if (sender == BtnOpt)
            {
                AnimateScale(ScaleOpt, 1);
            }
        }

        private void AnimateScale(ScaleTransform scale, double targetValue)
        {
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
                // Get window size at 2x resolution
                int width = (int)(this.ActualWidth * 2);
                int height = (int)(this.ActualHeight * 2);

                if (width <= 0 || height <= 0)
                {
                    MessageBox.Show("Please wait for the page to fully load.", "Error");
                    return;
                }

                // Hide button temporarily
                var screenshotBtn = GetScreenshotButton();
                if (screenshotBtn != null)
                {
                    screenshotBtn.Visibility = Visibility.Collapsed;
                }

                // Force visual refresh (faster than Thread.Sleep)
                Dispatcher.Invoke(() => { }, DispatcherPriority.Render);

                // Create render bitmap at 2x resolution for crisp quality
                RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                    width,
                    height,
                    192, // 2x DPI
                    192,
                    PixelFormats.Pbgra32);

                // Render with bitmap scaling for better quality
                RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
                renderBitmap.Render(this);

                // Show button again immediately
                if (screenshotBtn != null)
                {
                    screenshotBtn.Visibility = Visibility.Visible;
                }

                // Save dialog - JPEG with maximum quality
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JPEG Image|*.jpg",
                    FileName = $"ScreenCapture_{DateTime.Now:yyyyMMdd_HHmmss}.jpg"
                };

                if (dialog.ShowDialog() == true)
                {
                    // JPEG ENCODER - MAXIMUM QUALITY (100)
                    JpegBitmapEncoder encoder = new JpegBitmapEncoder
                    {
                        QualityLevel = 100  // Maximum quality - no compression artifacts
                    };
                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                    // Save file asynchronously for faster UI response
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
            // Find screenshot button by name
            if (FindName("ScreenshotBtn") is Button btn)
                return btn;

            // Try finding by content
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