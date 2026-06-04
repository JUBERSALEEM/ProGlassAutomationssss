using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ProGlassAutomation.Views.Print
{
    public partial class PrintPreviewWindow : Window
    {
        private const double DPI = 96;

        public PrintPreviewWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => TxtStatus.Text = "Ready to print";
        }

        public PrintPreviewWindow(PrintData printData) : this()
        {
            DataContext = printData;
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtStatus.Text = "Preparing print...";

                var printDialog = new PrintDialog();
                if (printDialog == null || !printDialog.ShowDialog().GetValueOrDefault())
                {
                    TxtStatus.Text = "Print cancelled";
                    return;
                }

                TxtStatus.Text = "Printing...";

                Dispatcher.Invoke(new Action(() => { }), System.Windows.Threading.DispatcherPriority.Render);

                var border = PrintBorder;
                if (border == null)
                {
                    TxtStatus.Text = "Error: No content";
                    return;
                }

                // KEY: UpdateLayout → Measure → Arrange (exactly like working pattern)
                border.UpdateLayout();
                border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                border.Arrange(new Rect(border.DesiredSize));

                int pixelWidth = (int)Math.Ceiling(border.ActualWidth * DPI / DPI);
                int pixelHeight = (int)Math.Ceiling(border.ActualHeight * DPI / DPI);

                if (pixelWidth <= 0) pixelWidth = 794;
                if (pixelHeight <= 0) pixelHeight = 1123;

                // KEY: Render to bitmap FIRST
                var renderBitmap = new RenderTargetBitmap(
                    pixelWidth, pixelHeight, DPI, DPI, PixelFormats.Pbgra32);
                renderBitmap.Render(border);

                // KEY: Create Image, then print Image
                var visual = new Image();
                visual.Source = renderBitmap;
                visual.Width = border.ActualWidth;
                visual.Height = border.ActualHeight;

                string title = DataContext is PrintData pd ? pd.DocumentNumber : "Job Order";
                printDialog.PrintVisual(visual, title);

                TxtStatus.Text = "Print completed - " + title;
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Print error";
                MessageBox.Show($"Print error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPageSetup_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "Page Setup",
                Width = 350,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid { Margin = new Thickness(25) };
            grid.Children.Add(new TextBlock
            {
                Text = "Page Setup",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            });
            grid.Children.Add(new TextBlock
            {
                Text = "Page: 794 x 1123 px\nMargins: 60px all sides\nA4 Portrait",
                Foreground = Brushes.LightGray,
                FontSize = 12,
                Margin = new Thickness(0, 30, 0, 0)
            });
            var closeBtn = new Button
            {
                Content = "Close",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Padding = new Thickness(25, 10, 25, 10),
                Background = new SolidColorBrush(Color.FromRgb(75, 85, 99)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            closeBtn.Click += (s, args) => dialog.Close();
            grid.Children.Add(closeBtn);

            dialog.Content = grid;
            dialog.ShowDialog();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}