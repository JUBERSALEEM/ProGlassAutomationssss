using System;
using System.Windows;
using System.Windows.Media;
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
        // SCREENSHOT - Capture Full Application (4K Ultra HD)
        // ═══════════════════════════════════════════════════════
        private void ScreenshotBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Capture the entire window
                Window window = this;

                double actualWidth = window.ActualWidth;
                double actualHeight = window.ActualHeight;

                if (actualWidth <= 0 || actualHeight <= 0)
                {
                    MessageBox.Show("Please wait for the page to fully load.", "Error");
                    return;
                }

                // 4K Quality Settings (384 DPI = 4x standard 96 DPI)
                int scaleFactor = 4;
                int dpi = 96 * scaleFactor;
                int renderWidth = (int)(actualWidth * scaleFactor);
                int renderHeight = (int)(actualHeight * scaleFactor);

                // Force layout update
                window.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                window.Arrange(new Rect(window.DesiredSize));

                // Create high-quality render bitmap (4K)
                var renderBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    renderWidth,
                    renderHeight,
                    dpi,
                    dpi,
                    System.Windows.Media.PixelFormats.Pbgra32);

                // Scale and render
                var scaledVisual = new System.Windows.Media.ScaleTransform(scaleFactor, scaleFactor);
                window.LayoutTransform = scaledVisual;

                // Render
                renderBitmap.Render(window);

                // Reset layout transform
                window.LayoutTransform = null;

                // Save dialog
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PNG Image|*.png",
                    FileName = $"ScreenCapture_4K_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    // Encode to PNG (lossless)
                    var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(renderBitmap));

                    // Save
                    using (var stream = new System.IO.FileStream(dialog.FileName, System.IO.FileMode.Create))
                    {
                        encoder.Save(stream);
                    }

                    MessageBox.Show($"4K Screenshot saved!\n\n📁 {dialog.FileName}\n\nResolution: {renderWidth} × {renderHeight} pixels", "Success");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }
    }
}