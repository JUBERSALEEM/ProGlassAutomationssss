using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ProGlassAutomation.Views.Optimization
{
    public partial class LayoutDialog : Window
    {
        private readonly OptimizationViewModel _vm;
        private int _currentSheetIndex = 0;

        public LayoutDialog(OptimizationViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            _currentSheetIndex = 0;
            DrawCurrentSheet();
        }

        private void Prev_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.LastSheets == null || _vm.LastSheets.Count == 0) return;
            _currentSheetIndex = Math.Max(0, _currentSheetIndex - 1);
            DrawCurrentSheet();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.LastSheets == null || _vm.LastSheets.Count == 0) return;
            _currentSheetIndex = Math.Min(_vm.LastSheets.Count - 1, _currentSheetIndex + 1);
            DrawCurrentSheet();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void LayoutCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => DrawCurrentSheet();

        private void DrawCurrentSheet()
        {
            if (LayoutCanvas == null) return;
            LayoutCanvas.Children.Clear();

            if (_vm.LastSheets == null || _vm.LastSheets.Count == 0)
            {
                txtLayoutHeader.Text = "No Layout Available";
                txtLayoutSubtitle.Text = "Run optimization first";
                txtSheetIndicator.Text = "Sheet 0 / 0";
                return;
            }

            if (_currentSheetIndex >= _vm.LastSheets.Count) _currentSheetIndex = 0;
            var sheet = _vm.LastSheets[_currentSheetIndex];

            txtLayoutHeader.Text = $"Sheet #{sheet.SheetNum} — {sheet.StockWidth:N0} × {sheet.StockHeight:N0} mm";
            int count = sheet.PlacedParts?.Count ?? 0;
            int rotated = sheet.PlacedParts?.Count(p => p.Rotated) ?? 0;
            txtLayoutSubtitle.Text = $"{count} glass pieces · {rotated} rotated (90°) · {sheet.Utilization:0.0}% yield";
            txtSheetIndicator.Text = $"Sheet {_currentSheetIndex + 1} / {_vm.LastSheets.Count}";

            txtPieces.Text = count.ToString();
            txtRotated.Text = rotated.ToString();
            txtYield.Text = $"{sheet.Utilization:0.0}%";
            txtScrap.Text = $"{(sheet.WasteArea / 1_000_000.0):0.000} m²";

            double cw = LayoutCanvas.ActualWidth > 0 ? LayoutCanvas.ActualWidth - 40 : 800;
            double ch = LayoutCanvas.ActualHeight > 0 ? LayoutCanvas.ActualHeight - 40 : 500;
            if (sheet.StockWidth <= 0 || sheet.StockHeight <= 0) return;

            double scale = Math.Min(cw / sheet.StockWidth, ch / sheet.StockHeight);
            if (scale <= 0) scale = 0.1;
            double sw = sheet.StockWidth * scale;
            double sh = sheet.StockHeight * scale;
            double ox = (LayoutCanvas.ActualWidth - sw) / 2;
            double oy = 20;

            var border = new Rectangle
            {
                Width = sw,
                Height = sh,
                Stroke = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A)),
                StrokeThickness = 2,
                Fill = Brushes.White
            };
            Canvas.SetLeft(border, ox);
            Canvas.SetTop(border, oy);
            LayoutCanvas.Children.Add(border);

            var wt = new TextBlock
            {
                Text = $"{sheet.StockWidth:N0} mm (Width)",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80))
            };
            Canvas.SetLeft(wt, ox + sw / 2 - 50);
            Canvas.SetTop(wt, oy - 18);
            LayoutCanvas.Children.Add(wt);

            var ht = new TextBlock
            {
                Text = $"{sheet.StockHeight:N0} mm (Height)",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)),
                RenderTransform = new RotateTransform(-90)
            };
            Canvas.SetLeft(ht, ox - 22);
            Canvas.SetTop(ht, oy + sh / 2 + 50);
            LayoutCanvas.Children.Add(ht);

            var partFill = new SolidColorBrush(Color.FromArgb(220, 0xA8, 0x55, 0xF7));
            var partStroke = new SolidColorBrush(Color.FromRgb(0x93, 0x4B, 0xE0));

            if (sheet.PlacedParts != null)
            {
                foreach (var p in sheet.PlacedParts)
                {
                    if (p.L <= 0 || p.W <= 0) continue;
                    var r = new Rectangle
                    {
                        Width = p.L * scale,
                        Height = p.W * scale,
                        Fill = partFill,
                        Stroke = partStroke,
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(r, ox + p.X * scale);
                    Canvas.SetTop(r, oy + p.Y * scale);
                    LayoutCanvas.Children.Add(r);

                    var lbl = new TextBlock
                    {
                        Text = $"{p.L} × {p.W}\n{(p.Rotated ? "↻" : "")}",
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.White,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap
                    };
                    Canvas.SetLeft(lbl, ox + p.X * scale);
                    Canvas.SetTop(lbl, oy + p.Y * scale);
                    lbl.Width = p.L * scale;
                    lbl.Height = p.W * scale;
                    lbl.Padding = new Thickness(4);
                    LayoutCanvas.Children.Add(lbl);
                }
            }
        }
    }
}