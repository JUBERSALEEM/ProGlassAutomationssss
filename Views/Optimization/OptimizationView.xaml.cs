using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Views.Optimization
{
    public partial class OptimizationView : UserControl
    {
        private readonly OptimizationViewModel _vm;

        public OptimizationView()
        {
            InitializeComponent();

            _vm = new OptimizationViewModel();
            DataContext = _vm;

            // Bind DataGrids to ViewModel collections
            dgStock.ItemsSource = _vm.StockSheets;
            dgParts.ItemsSource = _vm.Parts;

            // Initial UI state
            ShowTab("Stock");
            UpdateCounts();
        }

        // ═══════════════════════════════════════════════════════
        // ADD / REMOVE BUTTONS
        // ═══════════════════════════════════════════════════════

        private void AddStock_Click(object sender, RoutedEventArgs e)
        {
            _vm.StockSheets.Add(new StockSheet
            {
                L = 3210,
                W = 2250,
                Qty = 10
            });
            UpdateCounts();
        }

        private void AddPart_Click(object sender, RoutedEventArgs e)
        {
            _vm.Parts.Add(new CutPart
            {
                L = 1000,
                W = 800,
                Qty = 1
            });
            UpdateCounts();
        }

        // ═══════════════════════════════════════════════════════
        // RUN OPTIMIZATION
        // ═══════════════════════════════════════════════════════

        private async void RunOptimization_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool success = await _vm.RunOptimizationAsync();
                if (success)
                {
                    RefreshKpis();
                    UpdateReport();
                    DrawLayout();
                    ShowTab("Layout");
                }
                else
                {
                    MessageBox.Show("Please add parts to optimize. Stock sheets will be added automatically.",
                        "No Parts", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Optimization error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════════
        // TAB SWITCHING
        // ═══════════════════════════════════════════════════════

        private void TabStock_Click(object sender, RoutedEventArgs e) => ShowTab("Stock");
        private void TabParts_Click(object sender, RoutedEventArgs e) => ShowTab("Parts");
        private void TabLayout_Click(object sender, RoutedEventArgs e) => ShowTab("Layout");
        private void TabReport_Click(object sender, RoutedEventArgs e) => ShowTab("Report");

        private void ShowTab(string tab)
        {
            pnlStockTab.Visibility = Visibility.Collapsed;
            pnlPartsTab.Visibility = Visibility.Collapsed;
            pnlLayoutTab.Visibility = Visibility.Collapsed;
            pnlReportTab.Visibility = Visibility.Collapsed;

            btnTabStock.Style = (Style)Resources["SegmentTabInactive"];
            btnTabParts.Style = (Style)Resources["SegmentTabInactive"];
            btnTabLayout.Style = (Style)Resources["SegmentTabInactive"];
            btnTabReport.Style = (Style)Resources["SegmentTabInactive"];

            switch (tab)
            {
                case "Stock":
                    pnlStockTab.Visibility = Visibility.Visible;
                    btnTabStock.Style = (Style)Resources["SegmentTabActive"];
                    break;
                case "Parts":
                    pnlPartsTab.Visibility = Visibility.Visible;
                    btnTabParts.Style = (Style)Resources["SegmentTabActive"];
                    break;
                case "Layout":
                    pnlLayoutTab.Visibility = Visibility.Visible;
                    btnTabLayout.Style = (Style)Resources["SegmentTabActive"];
                    DrawLayout();
                    break;
                case "Report":
                    pnlReportTab.Visibility = Visibility.Visible;
                    btnTabReport.Style = (Style)Resources["SegmentTabActive"];
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════
        // UI UPDATE HELPERS
        // ═══════════════════════════════════════════════════════

        private void UpdateCounts()
        {
            txtStockCount.Text = $"({_vm.StockSheets.Count})";
            txtPartCount.Text = $"({_vm.Parts.Count})";
        }

        private void RefreshKpis()
        {
            txtUtilization.Text = $"{_vm.Utilization:0.##}%";
            txtWaste.Text = $"{_vm.Waste:0.##}%";
        }

        private void UpdateReport()
        {
            if (_vm.LastResult == null)
            {
                txtReport.Text = "Run optimization to generate report";
                return;
            }

            var r = _vm.LastResult;
            int placed = r.PlacedParts?.Count ?? 0;

            txtReport.Text =
                $"=== OPTIMIZATION REPORT ===\n\n" +
                $"Sheet Reference: {r.Ref}\n" +
                $"Sheet Size: {r.L} x {r.W} mm\n" +
                $"Sheet Area: {(r.L * r.W):N0} mm²\n\n" +
                $"Area Used: {r.Area:N0} mm²\n" +
                $"Utilization: {r.Util}%\n" +
                $"Waste: {r.Waste}%\n\n" +
                $"Parts Placed: {placed}\n" +
                $"Runtime: {r.RuntimeMs} ms";
        }

        // ═══════════════════════════════════════════════════════
        // 2D LAYOUT DRAWING
        // ═══════════════════════════════════════════════════════

        private void DrawLayout()
        {
            LayoutCanvas.Children.Clear();

            if (_vm.LastResult == null || _vm.LastResult.PlacedParts == null)
                return;

            if (_vm.LastResult.PlacedParts.Count == 0)
                return;

            double sheetW = _vm.LastResult.L;
            double sheetH = _vm.LastResult.W;

            if (sheetW <= 0 || sheetH <= 0)
                return;

            // Auto-scale to fit canvas
            double canvasW = LayoutCanvas.ActualWidth > 0 ? LayoutCanvas.ActualWidth : 1000;
            double canvasH = LayoutCanvas.ActualHeight > 0 ? LayoutCanvas.ActualHeight : 500;

            double scaleX = (canvasW - 40) / sheetW;
            double scaleY = (canvasH - 40) / sheetH;
            double scale = Math.Min(scaleX, scaleY);

            double offsetX = 20;
            double offsetY = 20;

            // Draw sheet border
            var sheetBorder = new Rectangle
            {
                Width = sheetW * scale,
                Height = sheetH * scale,
                Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0F, 0x17, 0x2A)),
                StrokeThickness = 2,
                Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFA, 0xFB, 0xFC))
            };
            Canvas.SetLeft(sheetBorder, offsetX);
            Canvas.SetTop(sheetBorder, offsetY);
            LayoutCanvas.Children.Add(sheetBorder);

            // Draw each placed part
            var colors = new[]
            {
                System.Windows.Media.Color.FromRgb(0x3B, 0x82, 0xF6),
                System.Windows.Media.Color.FromRgb(0x10, 0xB9, 0x81),
                System.Windows.Media.Color.FromRgb(0xF5, 0x9E, 0x0B),
                System.Windows.Media.Color.FromRgb(0xEF, 0x44, 0x44),
                System.Windows.Media.Color.FromRgb(0x8B, 0x5C, 0xF6),
                System.Windows.Media.Color.FromRgb(0x06, 0xB6, 0xD4),
                System.Windows.Media.Color.FromRgb(0xEC, 0x48, 0x99),
            };

            int colorIndex = 0;
            foreach (var part in _vm.LastResult.PlacedParts)
            {
                var rect = new Rectangle
                {
                    Width = part.L * scale,
                    Height = part.W * scale,
                    Fill = new System.Windows.Media.SolidColorBrush(colors[colorIndex % colors.Length]),
                    Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF)),
                    StrokeThickness = 1,
                    Opacity = 0.85
                };
                Canvas.SetLeft(rect, offsetX + part.X * scale);
                Canvas.SetTop(rect, offsetY + part.Y * scale);
                LayoutCanvas.Children.Add(rect);
                colorIndex++;
            }
        }

        private void LayoutCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (pnlLayoutTab.Visibility == Visibility.Visible)
            {
                DrawLayout();
            }
        }

        // ═══════════════════════════════════════════════════════
        // PROFORMA INVOICE COMPATIBILITY
        // ═══════════════════════════════════════════════════════

        public int SheetsUsed => _vm.SheetsUsed;
        public double AverageUtilization => _vm.AverageUtilization;
        public List<OptimizationResult> GetResultsList() => _vm.GetResultsList();

        public void ImportInvoiceItems(List<InvoiceItemModel> items)
        {
            _vm.ImportInvoiceItems(items);
            UpdateCounts();
        }

        public void RunOptimizationFromInvoice()
        {
            try
            {
                _vm.RunOptimizationFromInvoice();
                RefreshKpis();
                UpdateReport();
                DrawLayout();
                UpdateCounts();
                ShowTab("Layout");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OptimizationView] RunOptimizationFromInvoice: {ex.Message}");
            }
        }

        public void SetStockSheet(double width, double height)
        {
            _vm.SetStockSheet(width, height);
            UpdateCounts();
        }

        public void SetTrimSettings(double lr, double br, double tr, double rm, double kerf, double breakout)
        {
            _vm.SetTrimSettings(lr, br, tr, rm, kerf, breakout);
        }
    }
}