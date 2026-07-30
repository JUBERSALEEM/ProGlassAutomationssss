using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ProGlassAutomation.ViewModels.Optimization.Algorithms;
using ProGlassAutomation.Views.Optimization.Algorithms;

namespace ProGlassAutomation.Views.Optimization
{
    public partial class OptimizationView : UserControl
    {
        private readonly SmartNestingEngine _engine = new SmartNestingEngine();
        private readonly OptimizationServices _services = new OptimizationServices();
        private readonly OptimizationViewModel _viewModel = new OptimizationViewModel();

        private readonly ObservableCollection<StockSheet> _stockSheets = new ObservableCollection<StockSheet>();
        private readonly ObservableCollection<CutPart> _cutParts = new ObservableCollection<CutPart>();
        private readonly ObservableCollection<OptimizationResult> _results = new ObservableCollection<OptimizationResult>();
        private readonly List<PlacedPart> _allPlacedParts = new List<PlacedPart>();
        private readonly ObservableCollection<OptimizationJob> _savedJobs = new ObservableCollection<OptimizationJob>();

        private double _zoomLevel = 1.5;
        private int _currentIndex = 0;
        private readonly int _sheetsPerPage = 8;

        public OptimizationView()
        {
            InitializeComponent();
            DataContext = _viewModel;

            dgStock.ItemsSource = _stockSheets;
            dgParts.ItemsSource = _cutParts;
            icResults.ItemsSource = _results;

            if (PreviewCanvas != null)
            {
                PreviewCanvas.Width = 900;
                PreviewCanvas.Height = 650;
            }

            // Sync level slider to viewmodel
            if (sliderLevel != null)
            {
                sliderLevel.ValueChanged += (s, e) =>
                {
                    _viewModel.SelectedLevel = (OptimizationLevel)(int)Math.Round(sliderLevel.Value);
                };
            }
        }

        public void SetStockSheets(IEnumerable<StockSheet> sheets)
        {
            _stockSheets.Clear();
            if (sheets == null) return;
            int idx = 1;
            foreach (var s in sheets)
            {
                var copy = new StockSheet { Ref = string.IsNullOrEmpty(s.Ref) ? $"S{idx++}" : s.Ref, L = s.L, W = s.W, Qty = s.Qty };
                _stockSheets.Add(copy);
            }
        }

        public void AddStockSheet(double width, double height, int qty = 9999999)
        {
            int idx = _stockSheets.Count + 1;
            _stockSheets.Add(new StockSheet { Ref = $"S{idx}", L = width, W = height, Qty = qty });
        }

        public void ClearStockSheets()
        {
            _stockSheets.Clear();
        }

        public List<OptimizationResult> GetResultsList()
        {
            return _results.Where(r => r.Ref != "TOTAL").ToList();
        }

        public OptimizationResult GetTotalResult()
        {
            return _results.FirstOrDefault(r => r.Ref == "TOTAL");
        }

        public List<PlacedPart> GetAllPlacedParts()
        {
            return _allPlacedParts;
        }

        private void OptTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                string tab = btn.Tag.ToString();
                ResetTabs();
                if (btn != null)
                {
                    try { btn.Style = (Style)FindResource("TabActive"); } catch { }
                }

                switch (tab)
                {
                    case "Settings": if (pnlSettings != null) pnlSettings.Visibility = Visibility.Visible; break;
                    case "Layouts": if (pnlSummary != null) pnlSummary.Visibility = Visibility.Visible; break;
                    case "Report": if (pnlReport != null) pnlReport.Visibility = Visibility.Visible; break;
                }
            }
        }

        private void ResetTabs()
        {
            if (btnSettings != null) { try { btnSettings.Style = (Style)FindResource("TabInactive"); } catch { } }
            if (btnSummary != null) { try { btnSummary.Style = (Style)FindResource("TabInactive"); } catch { } }
            if (btnReport != null) { try { btnReport.Style = (Style)FindResource("TabInactive"); } catch { } }

            if (pnlSettings != null) pnlSettings.Visibility = Visibility.Collapsed;
            if (pnlSummary != null) pnlSummary.Visibility = Visibility.Collapsed;
            if (pnlReport != null) pnlReport.Visibility = Visibility.Collapsed;
        }

        private void ShowTab(string tab)
        {
            ResetTabs();
            switch (tab)
            {
                case "Settings": if (btnSettings != null) { try { btnSettings.Style = (Style)FindResource("TabActive"); } catch { } } if (pnlSettings != null) pnlSettings.Visibility = Visibility.Visible; break;
                case "Layouts": if (btnSummary != null) { try { btnSummary.Style = (Style)FindResource("TabActive"); } catch { } } if (pnlSummary != null) pnlSummary.Visibility = Visibility.Visible; break;
                case "Report": if (btnReport != null) { try { btnReport.Style = (Style)FindResource("TabActive"); } catch { } } if (pnlReport != null) pnlReport.Visibility = Visibility.Visible; break;
            }
        }

        private void AddStockSheet_Click(object sender, RoutedEventArgs e)
        {
            _stockSheets.Add(new StockSheet { Ref = $"S{_stockSheets.Count + 1}", L = 3210, W = 2250, Qty = 100 });
        }

        private void DeleteStockRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is StockSheet sheet)
                _stockSheets.Remove(sheet);
        }

        private void AddPart_Click(object sender, RoutedEventArgs e)
        {
            _cutParts.Add(new CutPart { Ref = $"P{_cutParts.Count + 1}", L = 1000, W = 1000, Rot = true, Qty = 1 });
        }

        private void DeletePartRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CutPart part)
                _cutParts.Remove(part);
        }

        private void DG_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) { }

        private void DG_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.Text))
                {
                    string text = e.Data.GetData(DataFormats.Text) as string;
                    if (string.IsNullOrEmpty(text)) return;

                    if (sender == dgStock)
                    {
                        var sheets = _services.ParseStockData(text, _stockSheets.Count + 1);
                        foreach (var s in sheets) _stockSheets.Add(s);
                    }
                    else if (sender == dgParts)
                    {
                        var parts = _services.ParsePartsData(text, _cutParts.Count + 1);
                        foreach (var p in parts) _cutParts.Add(p);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to parse raw layout values: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RunOptimization_Click(object sender, RoutedEventArgs e)
        {
            double.TryParse(txtLR?.Text, out double lr);
            double.TryParse(txtRM?.Text, out double rm);
            double.TryParse(txtTR?.Text, out double tm);
            double.TryParse(txtBR?.Text, out double bm);
            double.TryParse(txtKerf?.Text, out double kerf);
            double.TryParse(txtBreakout?.Text, out double breakout);

            _viewModel.LM = lr;
            _viewModel.RM = rm;
            _viewModel.TM = tm;
            _viewModel.BM = bm;
            _viewModel.Kerf = kerf;
            _viewModel.Breakout = breakout;

            if (_stockSheets.Count == 0 || _cutParts.Count == 0)
            {
                MessageBox.Show("Please add stock sheets and parts first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _engine.Configure(_viewModel.LM, _viewModel.RM, _viewModel.TM, _viewModel.BM, _viewModel.Kerf, _viewModel.Breakout);
                _engine.SetLevel(_viewModel.SelectedLevel);

                _engine.Execute(_stockSheets.ToList(), _cutParts.ToList(), _results, _allPlacedParts);

                // Update results totals
                _viewModel.OverallUtilization = _engine.OverallUtilization;
                _viewModel.OverallWastage = _engine.OverallWastage;
                _viewModel.UsedSQM = _engine.UsedSQM;
                _viewModel.TotalPartsCut = _engine.TotalPartsCut;
                _viewModel.TotalPartsUnplaced = _engine.TotalPartsUnplaced;

                if (txtUtilization != null) txtUtilization.Text = $"{_viewModel.OverallUtilization:N2}%";
                if (txtWaste != null) txtWaste.Text = $"{_viewModel.OverallWastage:N2}%";
                if (txtSheetsUsed != null) txtSheetsUsed.Text = _results.Count(r => r.Ref != "TOTAL").ToString();
                if (txtTotalPartsCut != null) txtTotalPartsCut.Text = _viewModel.TotalPartsCut.ToString();
                if (txtTotalCost != null) txtTotalCost.Text = (_viewModel.UsedSQM * 250).ToString("N2");

                _currentIndex = 0;
                DrawSingleSheetLayout(_currentIndex);
                ShowTab("Layouts");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearOptimization_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Clear all data?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _stockSheets.Clear();
                _cutParts.Clear();
                _results.Clear();
                _allPlacedParts.Clear();

                if (txtSheetsUsed != null) txtSheetsUsed.Text = "0";
                if (txtUtilization != null) txtUtilization.Text = "0%";
                if (txtWaste != null) txtWaste.Text = "0%";
                if (txtTotalPartsCut != null) txtTotalPartsCut.Text = "0";
                if (txtTotalCost != null) txtTotalCost.Text = "0.00";

                _currentIndex = 0;
                if (LayoutCanvas != null) LayoutCanvas.Children.Clear();
            }
        }

        // =====================================================
        // DRAWING METHODS
        // =====================================================

        private void DrawSingleSheetLayout(int startIndex)
        {
            if (LayoutCanvas == null) return;
            LayoutCanvas.Children.Clear();

            var validResults = _results.Where(r => r.Ref != "TOTAL").ToList();
            if (validResults.Count == 0 || startIndex < 0 || startIndex >= validResults.Count) return;

            int sheetsToDraw = Math.Min(_sheetsPerPage, validResults.Count - startIndex);
            if (sheetsToDraw <= 0) return;

            int cols = 2;
            double canvasW = 850, margin = 15, headerSpace = 22;
            double sheetAreaH = 260, sheetAreaW = (canvasW - margin * 2) / cols;
            double canvasH = ((_sheetsPerPage / cols) + 1) * sheetAreaH + margin * 2 + headerSpace;

            LayoutCanvas.Width = canvasW;
            LayoutCanvas.Height = canvasH;

            Color[] partColors = new Color[]
            {
                Color.FromRgb(59, 130, 246), Color.FromRgb(16, 185, 129),
                Color.FromRgb(139, 92, 246), Color.FromRgb(245, 158, 11),
                Color.FromRgb(239, 68, 68), Color.FromRgb(6, 182, 212),
                Color.FromRgb(236, 72, 153), Color.FromRgb(34, 197, 94)
            };

            for (int i = 0; i < sheetsToDraw; i++)
            {
                int idx = startIndex + i;
                if (idx >= validResults.Count) break;

                var currentResult = validResults[idx];
                double sheetW = currentResult.L;
                double sheetH = currentResult.W;
                int row = i / cols, col = i % cols;
                double areaTop = margin + row * (sheetAreaH + headerSpace);
                double areaLeft = margin + col * sheetAreaW;

                double scaleX = (sheetAreaW - margin * 2) / sheetW;
                double scaleY = (sheetAreaH - margin * 2) / sheetH;
                double scale = Math.Min(scaleX, scaleY) * 0.80 * _zoomLevel;

                if (scale < 0.01) scale = 0.01;
                if (scale > 1.0) scale = 1.0;

                double drawW = sheetW * scale;
                double drawH = sheetH * scale;
                double startX = areaLeft + (sheetAreaW - drawW) / 2;
                double startY = areaTop + headerSpace;

                double leftOffset = _viewModel.LM * scale;
                double rightOffset = _viewModel.RM * scale;
                double topOffset = _viewModel.TM * scale;
                double bottomOffset = _viewModel.BM * scale;

                double usableW = drawW - leftOffset - rightOffset;
                double usableH = drawH - topOffset - bottomOffset;

                TextBlock info = new TextBlock
                {
                    Text = $"#{idx + 1}: {currentResult.L:N0}×{currentResult.W:N0}mm U:{currentResult.Util:N2}%",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42))
                };
                Canvas.SetLeft(info, areaLeft + 5);
                Canvas.SetTop(info, areaTop + 2);
                LayoutCanvas.Children.Add(info);

                // RED line for breakout safety boundary as requested
                Rectangle trimBorder = new Rectangle { Width = drawW, Height = drawH, Fill = Brushes.Transparent, Stroke = Brushes.Red, StrokeThickness = 2 };
                Canvas.SetLeft(trimBorder, startX);
                Canvas.SetTop(trimBorder, startY);
                LayoutCanvas.Children.Add(trimBorder);

                Rectangle usableSheet = new Rectangle
                {
                    Width = usableW,
                    Height = usableH,
                    Fill = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                    Stroke = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    StrokeThickness = 1
                };
                Canvas.SetLeft(usableSheet, startX + leftOffset);
                Canvas.SetTop(usableSheet, startY + topOffset);
                LayoutCanvas.Children.Add(usableSheet);

                var partsOnSheet = _allPlacedParts
                    .Where(p => p.Sheet == currentResult.SheetRef && p.SheetNum == currentResult.SheetNum)
                    .ToList();

                var colorMap = new Dictionary<string, Color>();
                var partGroups = partsOnSheet.GroupBy(p => p.Ref).ToList();
                for (int c = 0; c < partGroups.Count; c++)
                    colorMap[partGroups[c].Key] = partColors[c % partColors.Length];

                foreach (var part in partsOnSheet)
                {
                    if (part.L <= 0 || part.W <= 0) continue;

                    double px = startX + leftOffset + part.X * scale;
                    double py = startY + topOffset + part.Y * scale;
                    double pw = part.L * scale;
                    double ph = part.W * scale;

                    var color = colorMap.ContainsKey(part.Ref) ? colorMap[part.Ref] : Color.FromRgb(128, 128, 128);

                    Border partBorder = new Border
                    {
                        Width = pw,
                        Height = ph,
                        Background = new SolidColorBrush(color),
                        BorderBrush = Brushes.White,
                        BorderThickness = new Thickness(1),
                        Child = new TextBlock
                        {
                            Text = $"{part.Ref}\n{part.L:N0}×{part.W:N0}",
                            FontSize = 7,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextAlignment = TextAlignment.Center
                        }
                    };
                    Canvas.SetLeft(partBorder, px);
                    Canvas.SetTop(partBorder, py);
                    LayoutCanvas.Children.Add(partBorder);
                }
            }

            if (txtCurrentSheet != null)
                txtCurrentSheet.Text = $"Layouts: {startIndex + 1}-{startIndex + sheetsToDraw} of {validResults.Count}";
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _zoomLevel = Math.Min(_zoomLevel + 0.1, 2.5);
            if (txtZoom != null) txtZoom.Text = $"{(_zoomLevel * 100):N0}%";
            DrawSingleSheetLayout(_currentIndex);
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _zoomLevel = Math.Max(_zoomLevel - 0.1, 0.3);
            if (txtZoom != null) txtZoom.Text = $"{(_zoomLevel * 100):N0}%";
            DrawSingleSheetLayout(_currentIndex);
        }

        private void PrevLayout_Click(object sender, RoutedEventArgs e)
        {
            var validResults = _results.Where(r => r.Ref != "TOTAL").ToList();
            if (validResults.Count == 0) return;

            if (_currentIndex > 0)
            {
                _currentIndex = Math.Max(0, _currentIndex - _sheetsPerPage);
                DrawSingleSheetLayout(_currentIndex);
            }
        }

        private void NextLayout_Click(object sender, RoutedEventArgs e)
        {
            var validResults = _results.Where(r => r.Ref != "TOTAL").ToList();
            if (validResults.Count == 0) return;

            if (_currentIndex < validResults.Count - _sheetsPerPage)
            {
                _currentIndex = Math.Min(validResults.Count - 1, _currentIndex + _sheetsPerPage);
                DrawSingleSheetLayout(_currentIndex);
            }
        }

        public double AverageUtilization => _viewModel.OverallUtilization;
        public int SheetsUsed => _results.Count(r => r.Ref != "TOTAL");

        public void SetStockSheet(double width, double height)
        {
            int idx = _stockSheets.Count + 1;
            _stockSheets.Add(new StockSheet { Ref = $"S{idx}", L = width, W = height, Qty = 9999999 });
        }

        public void SetTrimSettings(double lr, double br, double tr, double rm, double kerf, double breakout)
        {
            _viewModel.LM = lr;
            _viewModel.BM = br;
            _viewModel.TM = tr;
            _viewModel.RM = rm;
            _viewModel.Kerf = kerf;
            _viewModel.Breakout = breakout;

            if (txtLR != null) txtLR.Text = lr.ToString();
            if (txtRM != null) txtRM.Text = rm.ToString();
            if (txtTR != null) txtTR.Text = tr.ToString();
            if (txtBR != null) txtBR.Text = br.ToString();
            if (txtKerf != null) txtKerf.Text = kerf.ToString();
            if (txtBreakout != null) txtBreakout.Text = breakout.ToString();
        }

        public void ImportInvoiceItems(List<ProGlassAutomation.Models.InvoiceItemModel> invoiceItems)
        {
            _cutParts.Clear();
            if (invoiceItems == null) return;

            int idx = 1;
            foreach (var item in invoiceItems)
            {
                _cutParts.Add(new CutPart
                {
                    Ref = item.GlassRef ?? $"P{idx++}",
                    L = item.Width1,
                    W = item.Height1,
                    Rot = true,
                    Qty = item.Qty
                });
            }

            if (_stockSheets.Count > 0 && _cutParts.Count > 0)
            {
                RunOptimizationFromInvoice();
            }
        }

        public void RunOptimizationFromInvoice()
        {
            if (_stockSheets.Count == 0 || _cutParts.Count == 0) return;

            try
            {
                _engine.Configure(_viewModel.LM, _viewModel.RM, _viewModel.TM, _viewModel.BM, _viewModel.Kerf, _viewModel.Breakout);
                _engine.SetLevel(_viewModel.SelectedLevel);

                _engine.Execute(_stockSheets.ToList(), _cutParts.ToList(), _results, _allPlacedParts);

                _viewModel.OverallUtilization = _engine.OverallUtilization;
                _viewModel.OverallWastage = _engine.OverallWastage;
                _viewModel.UsedSQM = _engine.UsedSQM;
                _viewModel.TotalPartsCut = _engine.TotalPartsCut;
                _viewModel.TotalPartsUnplaced = _engine.TotalPartsUnplaced;

                if (txtUtilization != null) txtUtilization.Text = $"{_viewModel.OverallUtilization:N2}%";
                if (txtWaste != null) txtWaste.Text = $"{_viewModel.OverallWastage:N2}%";
                if (txtSheetsUsed != null) txtSheetsUsed.Text = _results.Count(r => r.Ref != "TOTAL").ToString();
                if (txtTotalPartsCut != null) txtTotalPartsCut.Text = _viewModel.TotalPartsCut.ToString();
                if (txtTotalCost != null) txtTotalCost.Text = (_viewModel.UsedSQM * 250).ToString("N2");

                _currentIndex = 0;
                DrawSingleSheetLayout(_currentIndex);
                ShowTab("Layouts");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
