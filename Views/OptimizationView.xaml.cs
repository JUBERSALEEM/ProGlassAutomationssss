using System;
using System.Collections.Generic;
using ProGlassAutomation.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ProGlassAutomation.Views
{
    public enum RotationPolicy
    {
        None = 0,
        Rotate90 = 1,
        BestFit = 2,
        FirstFit = 3,  // Added - like PLUS 2D
        StripFill = 4,
        ColumnFill = 5,
        RowFill = 6
    }

    public partial class OptimizationView : UserControl
    {
        private ObservableCollection<StockSheet> _stockSheets = new ObservableCollection<StockSheet>();
        private ObservableCollection<CutPart> _cutParts = new ObservableCollection<CutPart>();
        private ObservableCollection<OptimizationResult> _results = new ObservableCollection<OptimizationResult>();
        private List<PlacedPart> _allPlacedParts = new List<PlacedPart>();

        private double _zoomLevel = 1.0;
        private int _currentIndex = 0;
        private int _sheetsPerPage = 6;

        private double _lr = 15, _br = 15, _tr = 15, _rm = 15, _kerf = 4.0, _breakout = 4.0;
        private RotationPolicy _rotationPolicy = RotationPolicy.BestFit;
        private double _bridgeWidth = 15;

        private double _overallUtilization = 0;
        private double _overallWastage = 0;
        private int _totalPartsCut = 0;
        private int _totalPartsUnplaced = 0;
        private double _usedSQM = 0;

        // New feature fields
        private List<Remnant> _remnants = new List<Remnant>();
        private ObservableCollection<OptimizationJob> _savedJobs = new ObservableCollection<OptimizationJob>();
        private CostSettings _costSettings = new CostSettings();
        private Dictionary<string, double> _stockPrices = new Dictionary<string, double>();
        private int _highPriorityParts = 0;
        private bool _multiStockMode = false;
        private DispatcherTimer _simulateTimer;
        private int _simulateStep = 0;
        private List<CutOperation> _cutOperations = new List<CutOperation>();

        public OptimizationView()
        {
            InitializeComponent();
            DataContext = this;

            dgStock.ItemsSource = _stockSheets;
            dgParts.ItemsSource = _cutParts;
            icResults.ItemsSource = _results;
            cmbSheetSelector.ItemsSource = _results;
            cmbSavedJobs.ItemsSource = _savedJobs;

            PreviewCanvas.Width = 900;
            PreviewCanvas.Height = 650;
        }

        private void OptTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                string tab = btn.Tag.ToString();
                btnStock.Style = (Style)FindResource("TabInactive");
                btnParts.Style = (Style)FindResource("TabInactive");
                btnSettings.Style = (Style)FindResource("TabInactive");
                btnSummary.Style = (Style)FindResource("TabInactive");
                btnReport.Style = (Style)FindResource("TabInactive");
                btn.Style = (Style)FindResource("TabActive");
                pnlStock.Visibility = tab == "Stock" ? Visibility.Visible : Visibility.Collapsed;
                pnlParts.Visibility = tab == "Parts" ? Visibility.Visible : Visibility.Collapsed;
                pnlSettings.Visibility = tab == "Settings" ? Visibility.Visible : Visibility.Collapsed;
                pnlSummary.Visibility = tab == "Layouts" ? Visibility.Visible : Visibility.Collapsed;
                pnlReport.Visibility = tab == "Report" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void AddStockSheet_Click(object sender, RoutedEventArgs e)
        {
            _stockSheets.Add(new StockSheet { Ref = $"S{_stockSheets.Count + 1}", L = 3300, W = 2433, Qty = 9999999 });
            UpdateStockSummary();
        }

        private void DeleteStockRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is StockSheet sheet) _stockSheets.Remove(sheet);
            UpdateStockSummary();
        }

        private void AddPart_Click(object sender, RoutedEventArgs e)
        {
            _cutParts.Add(new CutPart { Ref = $"P{_cutParts.Count + 1}", L = 1000, W = 1000, Rot = true, Qty = 1 });
            UpdatePartsSummary();
        }

        private void DeletePartRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CutPart part) _cutParts.Remove(part);
            UpdatePartsSummary();
        }

        private void DG_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) { }

        private void DG_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                string text = e.Data.GetData(DataFormats.Text) as string;
                if (sender == dgStock) ParseStockData(text);
                else if (sender == dgParts) ParsePartsData(text);
            }
        }

        private void ParseStockData(string text)
        {
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3 && double.TryParse(parts[0].Trim(), out double w) && double.TryParse(parts[1].Trim(), out double h) && int.TryParse(parts[2].Trim(), out int qty))
                    _stockSheets.Add(new StockSheet { Ref = $"S{_stockSheets.Count + 1}", L = w, W = h, Qty = qty });
            }
            UpdateStockSummary();
        }

        private void ParsePartsData(string text)
        {
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3 && double.TryParse(parts[0].Trim(), out double w) && double.TryParse(parts[1].Trim(), out double h) && int.TryParse(parts[2].Trim(), out int qty))
                    _cutParts.Add(new CutPart { Ref = $"P{_cutParts.Count + 1}", L = w, W = h, Rot = true, Qty = qty });
            }
            UpdatePartsSummary();
        }

        private void UpdateStockSummary()
        {
            if (_stockSheets.Count == 0) { txtStockSummary.Text = "No stock added"; return; }
            var groups = _stockSheets.GroupBy(s => new { s.L, s.W }).Select(g => new { g.Key.L, g.Key.W, TotalQty = g.Sum(s => s.Qty), SQM = g.Sum(s => s.L * s.W * s.Qty) / 1000000.0 }).OrderByDescending(x => x.SQM).ToList();
            var lines = groups.Select(g => $"{g.L:N0}×{g.W:N0}mm = {g.TotalQty} ({g.SQM:N2}m²)").ToList();
            lines.Insert(0, $"Stock: {_stockSheets.Count} types, {_stockSheets.Sum(s => s.Qty)} total");
            txtStockSummary.Text = string.Join("\n", lines);
        }

        private void UpdatePartsSummary()
        {
            if (_cutParts.Count == 0) { txtPartsSummary.Text = "No parts added"; return; }
            var totalQty = _cutParts.Sum(p => p.Qty);
            var totalSQM = _cutParts.Sum(p => p.L * p.W * p.Qty) / 1000000.0;
            var groups = _cutParts.GroupBy(p => new { p.L, p.W, p.Ref }).Select(g => new { Ref = g.Key.Ref, L = g.Key.L, W = g.Key.W, Qty = g.Sum(x => x.Qty), SQM = g.Sum(x => x.L * x.W * x.Qty) / 1000000.0 }).OrderByDescending(x => x.SQM).ToList();
            var lines = groups.Select(g => $"{g.Ref}: {g.L}×{g.W}×{g.Qty} = {g.SQM:N2}m²").ToList();
            lines.Insert(0, $"Parts: {totalQty} total ({totalSQM:N2}m²)");
            txtPartsSummary.Text = string.Join("\n", lines);
        }

        private void RunOptimization_Click(object sender, RoutedEventArgs e)
        {
            double.TryParse(txtLR.Text, out _lr); double.TryParse(txtBR.Text, out _br);
            double.TryParse(txtTR.Text, out _tr); double.TryParse(txtRM.Text, out _rm);
            double.TryParse(txtKerf.Text, out _kerf); double.TryParse(txtBreakout.Text, out _breakout);

            if (cmbRotation != null)
                _rotationPolicy = (RotationPolicy)cmbRotation.SelectedIndex;

            if (_stockSheets.Count == 0 || _cutParts.Count == 0)
            {
                MessageBox.Show("Please add stock sheets and parts first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                RunNestingAlgorithm(_kerf, _rotationPolicy);
                UpdateReportSection();
                UpdateResultsGrouping();
                ShowTab("Layouts");
                MessageBox.Show($"Optimization completed!\n{_results.Count(r => r.Ref != "TOTAL")} sheets @ {_overallUtilization:N2}%", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ShowTab(string tab)
        {
            btnStock.Style = (Style)FindResource("TabInactive"); btnParts.Style = (Style)FindResource("TabInactive");
            btnSettings.Style = (Style)FindResource("TabInactive"); btnSummary.Style = (Style)FindResource("TabInactive");
            btnReport.Style = (Style)FindResource("TabInactive");
            switch (tab)
            {
                case "Stock": btnStock.Style = (Style)FindResource("TabActive"); break;
                case "Parts": btnParts.Style = (Style)FindResource("TabActive"); break;
                case "Settings": btnSettings.Style = (Style)FindResource("TabActive"); break;
                case "Layouts": btnSummary.Style = (Style)FindResource("TabActive"); break;
                case "Report": btnReport.Style = (Style)FindResource("TabActive"); break;
            }
            pnlStock.Visibility = tab == "Stock" ? Visibility.Visible : Visibility.Collapsed;
            pnlParts.Visibility = tab == "Parts" ? Visibility.Visible : Visibility.Collapsed;
            pnlSettings.Visibility = tab == "Settings" ? Visibility.Visible : Visibility.Collapsed;
            pnlSummary.Visibility = tab == "Layouts" ? Visibility.Visible : Visibility.Collapsed;
            pnlReport.Visibility = tab == "Report" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RunNestingAlgorithm(double kerf, RotationPolicy rotationPolicy)
        {
            _results.Clear();
            _allPlacedParts.Clear();
            _remnants.Clear();
            _cutOperations.Clear();

            var allParts = new List<CutPart>();
            int partId = 1;
            foreach (var p in _cutParts)
            {
                for (int i = 0; i < p.Qty; i++)
                {
                    allParts.Add(new CutPart { Id = partId++, Ref = p.Ref, L = p.L, W = p.W, Rot = p.Rot, Qty = 1, IsPlaced = false });
                }
            }
            allParts = allParts.OrderByDescending(x => x.L * x.W).ThenByDescending(x => Math.Max(x.L, x.W)).ToList();

            var sortedStock = _stockSheets.OrderByDescending(s => s.L * s.W).ToList();

            double totalUsedAreaAll = 0, totalAreaUsedSheets = 0;
            int totalSheetsUsed = 0;
            _totalPartsCut = 0;
            _totalPartsUnplaced = 0;
            _usedSQM = 0;

            foreach (var stock in sortedStock)
            {
                double oneSheetArea = stock.L * stock.W / 1000000.0;
                int availableQty = stock.Qty;
                if (availableQty <= 0) continue;

                double usableW = stock.L - _lr - _rm;
                double usableH = stock.W - _tr - _br;

                if (usableW < _breakout || usableH < _breakout) continue;

                int sheetsUsedForThisStock = 0;

                for (int sheetNum = 0; sheetNum < availableQty; sheetNum++)
                {
                    var remaining = allParts.Where(p => !p.IsPlaced).ToList();
                    if (remaining.Count == 0) break;

                    var wasteRects = new List<Rect>();
                    wasteRects.Add(new Rect(0, 0, usableW, usableH));

                    double usedAreaThisSheet = 0;
                    int placedOnThisSheet = 0;
                    var placedOnThisSheetList = new List<PlacedPart>();
                    var sheetCuts = new List<CutOperation>();

                    foreach (var part in remaining)
                    {
                        if (part.IsPlaced) continue;

                        double pW = part.L;
                        double pH = part.W;
                        bool rotated = false;

                        bool canFitNormal = CanFit(pW + kerf, pH + kerf, wasteRects);
                        bool canFitRotated = false;

                        if (part.Rot && rotationPolicy != RotationPolicy.None)
                            canFitRotated = CanFit(pH + kerf, pW + kerf, wasteRects);

                        if (rotationPolicy == RotationPolicy.Rotate90)
                        {
                            if (canFitRotated) { pW = part.W; pH = part.L; rotated = true; }
                            else if (!canFitNormal) continue;
                        }
                        else if (rotationPolicy == RotationPolicy.BestFit)
                        {
                            if (canFitNormal && canFitRotated)
                            {
                                double scoreNormal = GetFitScore(pW + kerf, pH + kerf, wasteRects);
                                double scoreRotated = GetFitScore(pH + kerf, pW + kerf, wasteRects);
                                if (scoreRotated < scoreNormal) { pW = part.W; pH = part.L; rotated = true; }
                            }
                            else if (canFitRotated) { pW = part.W; pH = part.L; rotated = true; }
                            else if (!canFitNormal) continue;
                        }
                        else if (rotationPolicy == RotationPolicy.FirstFit)
                        {
                            // First Fit - place in first available spot (like PLUS 2D)
                            if (canFitRotated) { pW = part.W; pH = part.L; rotated = true; }
                            else if (!canFitNormal) continue;
                        }
                        else if (rotationPolicy == RotationPolicy.StripFill)
                        {
                            // Strip Fill - place parts in horizontal strips
                            if (!canFitNormal) continue;
                        }
                        else if (rotationPolicy == RotationPolicy.ColumnFill)
                        {
                            // Column Fill - try to place in column pattern
                            if (part.Rot && canFitRotated) { pW = part.W; pH = part.L; rotated = true; }
                            else if (!canFitNormal) continue;
                        }
                        else if (rotationPolicy == RotationPolicy.RowFill)
                        {
                            // Row Fill - try to place in row pattern  
                            if (canFitNormal && canFitRotated)
                            {
                                double scoreNormal = GetFitScore(pW + kerf, pH + kerf, wasteRects);
                                double scoreRotated = GetFitScore(pH + kerf, pW + kerf, wasteRects);
                                if (scoreRotated < scoreNormal) { pW = part.W; pH = part.L; rotated = true; }
                            }
                            else if (!canFitNormal) continue;
                        }
                        else
                        {
                            if (!canFitNormal) continue;
                        }

                        var placeRect = FindBestRect(pW + kerf, pH + kerf, wasteRects);
                        if (placeRect == Rect.Empty) continue;

                        part.IsPlaced = true;
                        part.PlacedX = placeRect.X;
                        part.PlacedY = placeRect.Y;
                        part.PlacedW = pW;
                        part.PlacedH = pH;

                        placedOnThisSheetList.Add(new PlacedPart { Ref = part.Ref, X = part.PlacedX + _lr, Y = part.PlacedY + _tr, L = part.PlacedW, W = part.PlacedH, IsRotated = rotated, Sheet = stock.Ref, SheetNum = sheetNum + 1 });

                        double partArea = (pW * pH) / 1000000.0;
                        usedAreaThisSheet += partArea;
                        _usedSQM += partArea;
                        placedOnThisSheet++;
                        _totalPartsCut++;

                        double cutX = placeRect.X;
                        double cutY = placeRect.Y;
                        double cutW = pW + kerf;
                        double cutH = pH + kerf;

                        // Track cut operations
                        sheetCuts.Add(new CutOperation { X = placeRect.X + _lr, Y = placeRect.Y + _tr, Width = pW, Height = pH, PartRef = part.Ref, IsRotated = rotated });

                        wasteRects.Remove(placeRect);

                        double rightW = placeRect.Width - cutW;
                        if (rightW > _breakout)
                        {
                            wasteRects.Add(new Rect(cutX + cutW, cutY, rightW, cutH));
                        }

                        double bottomH = placeRect.Height - cutH;
                        if (bottomH > _breakout)
                        {
                            wasteRects.Add(new Rect(cutX, cutY + cutH, cutW, bottomH));
                        }

                        // Track remnants
                        if (rightW > 100 || bottomH > 100)
                        {
                            _remnants.Add(new Remnant
                            {
                                Ref = $"R{_remnants.Count + 1}",
                                L = rightW > 100 ? rightW : cutW,
                                W = bottomH > 100 ? bottomH : cutH,
                                X = rightW > 100 ? cutX + cutW : cutX,
                                Y = bottomH > 100 ? cutY + cutH : cutY,
                                FromSheet = stock.Ref,
                                SheetNum = sheetNum + 1,
                                IsUsed = false
                            });
                        }
                    }

                    if (placedOnThisSheet > 0)
                    {
                        sheetsUsedForThisStock++;
                        totalSheetsUsed++;
                        totalAreaUsedSheets += oneSheetArea;
                        totalUsedAreaAll += usedAreaThisSheet;
                        double thisSheetUtil = (usedAreaThisSheet / oneSheetArea) * 100;
                        _results.Add(new OptimizationResult { Ref = $"{stock.Ref}-{sheetsUsedForThisStock}", SheetRef = stock.Ref, SheetNum = sheetsUsedForThisStock, L = stock.L, W = stock.W, Used = 1, Area = usedAreaThisSheet, Util = thisSheetUtil, Waste = 100 - thisSheetUtil });
                        _allPlacedParts.AddRange(placedOnThisSheetList);
                        _cutOperations.AddRange(sheetCuts);
                    }

                    if (placedOnThisSheet == 0 && allParts.Count(p => !p.IsPlaced) > 0) break;
                }
            }

            _totalPartsUnplaced = allParts.Count(p => !p.IsPlaced);
            _overallUtilization = totalAreaUsedSheets > 0 ? (totalUsedAreaAll / totalAreaUsedSheets) * 100 : 0;
            _overallWastage = 100 - _overallUtilization;

            int totalAvailable = _stockSheets.Sum(s => s.Qty);
            _results.Add(new OptimizationResult { Ref = "TOTAL", L = 0, W = 0, Used = totalSheetsUsed, Area = totalUsedAreaAll, Util = _overallUtilization, Waste = _overallWastage });

            // Group by sheet size for right panel
            var groupedResults = _results
                .Where(r => r.Ref != "TOTAL")
                .GroupBy(r => $"{r.L}x{r.W}")
                .Select(g => new OptimizationResult
                {
                    Ref = $"{g.First().L:N0}x{g.First().W:N0}",
                    SheetRef = g.First().SheetRef,
                    L = g.First().L,
                    W = g.First().W,
                    Used = g.Sum(x => x.Used),
                    Area = g.Sum(x => x.Area),
                    Util = g.Average(x => x.Util),
                    Waste = g.Average(x => x.Waste)
                })
                .ToList();

            icResults.ItemsSource = groupedResults;

            txtSheetsUsed.Text = totalSheetsUsed.ToString();
            txtSheetsRemaining.Text = (totalAvailable - totalSheetsUsed).ToString();
            txtUtilization.Text = $"{_overallUtilization:N2}%";
            txtWaste.Text = $"{_overallWastage:N2}%";
            txtTotalSheetsUsed.Text = totalSheetsUsed.ToString();
            txtTotalPartsCut.Text = _totalPartsCut.ToString();
            txtAvgUtilization.Text = $"{_overallUtilization:N2}%";
            txtTotalStats.Text = $"{totalSheetsUsed} sheets, {_totalPartsCut} parts";
            pnlUnplaced.Visibility = _totalPartsUnplaced > 0 ? Visibility.Visible : Visibility.Collapsed;
            txtUnplaced.Text = $"{_totalPartsUnplaced} parts could not be placed";
            _currentIndex = 0;
            if (_results.Count > 0) cmbSheetSelector.SelectedIndex = 0;
            DrawCurrentLayout(_currentIndex);
            DrawSingleSheetLayout(_currentIndex);
            UpdateLayoutCount();
            TrackRemnants();
            CalculateCost();
        }

        private bool CanFit(double w, double h, List<Rect> wasteRects)
        {
            foreach (var rect in wasteRects)
            {
                if (w <= rect.Width && h <= rect.Height)
                    return true;
            }
            return false;
        }

        private double GetFitScore(double w, double h, List<Rect> wasteRects)
        {
            double bestScore = double.MaxValue;
            foreach (var rect in wasteRects)
            {
                if (w <= rect.Width && h <= rect.Height)
                {
                    double score = (rect.Width * rect.Height) - (w * h);
                    if (score < bestScore) bestScore = score;
                }
            }
            return bestScore;
        }

        private Rect FindBestRect(double w, double h, List<Rect> wasteRects)
        {
            double bestScore = double.MaxValue;
            Rect bestRect = Rect.Empty;

            foreach (var rect in wasteRects)
            {
                if (w <= rect.Width && h <= rect.Height)
                {
                    double score = (rect.Width * rect.Height) - (w * h);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestRect = rect;
                    }
                }
            }
            return bestRect;
        }

        private void UpdateReportSection()
        {
            spReportDetails.Children.Clear();

            var header = new TextBlock
            {
                Text = "CUTTING REPORT",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                Margin = new Thickness(0, 0, 0, 15)
            };
            spReportDetails.Children.Add(header);

            var overallBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 59, 100)),
                Padding = new Thickness(15),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 0, 15)
            };
            var overallStack = new StackPanel();

            overallStack.Children.Add(new TextBlock
            {
                Text = "OVERALL SUMMARY",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(0, 0, 0, 10)
            });

            overallStack.Children.Add(new TextBlock
            {
                Text = $"Total Sheets Used: {_results.Count(r => r.Ref != "TOTAL")}",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11))
            });

            overallStack.Children.Add(new TextBlock
            {
                Text = $"Total Parts Cut: {_totalPartsCut}",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246))
            });

            overallStack.Children.Add(new TextBlock
            {
                Text = $"Glass Area Used: {_usedSQM:N2} m²",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129))
            });

            overallStack.Children.Add(new TextBlock
            {
                Text = $"Average Utilization: {_overallUtilization:N2}%",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(139, 92, 246))
            });

            overallStack.Children.Add(new TextBlock
            {
                Text = $"Wastage: {_overallWastage:N2}%",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68))
            });

            if (_totalPartsUnplaced > 0)
            {
                overallStack.Children.Add(new TextBlock
                {
                    Text = $"⚠ Unplaced Parts: {_totalPartsUnplaced}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(252, 165, 165)),
                    Margin = new Thickness(0, 10, 0, 0)
                });
            }

            overallBorder.Child = overallStack;
            spReportDetails.Children.Add(overallBorder);

            // Grouped results
            var groupedResults = _results.Where(r => r.Ref != "TOTAL")
                .GroupBy(r => new { r.L, r.W, Util = Math.Round(r.Util, 1) })
                .OrderByDescending(g => g.Key.L * g.Key.W)
                .ToList();

            foreach (var sheetGroup in groupedResults)
            {
                var first = sheetGroup.First();
                int sheetCount = sheetGroup.Count();
                double avgUtil = sheetGroup.Average(s => s.Util);
                double avgWaste = sheetGroup.Average(s => s.Waste);
                double totalArea = sheetGroup.Sum(s => s.Area);

                var sheetBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(26, 29, 35)),
                    Padding = new Thickness(12),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(0, 0, 0, 8)
                };
                var sheetStack = new StackPanel();

                sheetStack.Children.Add(new TextBlock
                {
                    Text = $"{first.SheetRef}: {first.L:N0} × {first.W:N0}mm",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246))
                });

                sheetStack.Children.Add(new TextBlock
                {
                    Text = $"Sheets: {sheetCount} | Area: {totalArea:N3} m² | U: {avgUtil:N2}% | W: {avgWaste:N2}%",
                    Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                    FontSize = 11
                });

                sheetBorder.Child = sheetStack;
                spReportDetails.Children.Add(sheetBorder);
            }

            var totalBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                Padding = new Thickness(15),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 10, 0, 0)
            };
            var totalStack = new StackPanel();

            totalStack.Children.Add(new TextBlock
            {
                Text = "AUTO-SUM TOTAL",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White)
            });

            totalStack.Children.Add(new TextBlock
            {
                Text = $"Total Qty: {_totalPartsCut} parts",
                FontSize = 14,
                Foreground = new SolidColorBrush(Colors.White)
            });

            totalStack.Children.Add(new TextBlock
            {
                Text = $"Total SQM: {_usedSQM:N2}",
                FontSize = 14,
                Foreground = new SolidColorBrush(Colors.White)
            });

            totalBorder.Child = totalStack;
            spReportDetails.Children.Add(totalBorder);
        }

        private void UpdateResultsGrouping()
        {
            // Group results for display - handled in ReportSection
        }

        private void DrawCurrentLayout(int startIndex)
        {
            PreviewCanvas.Children.Clear();

            if (_results.Count == 0 || startIndex < 0) return;
            var validResults = _results.Where(r => r.Ref != "TOTAL").ToList();
            if (validResults.Count == 0) return;

            // Show ALL sheets in left list (single loop only)
            int sheetsToShow = validResults.Count;
            if (sheetsToShow > 100) sheetsToShow = 100;

            PreviewCanvas.Width = 390;
            PreviewCanvas.Height = sheetsToShow * 26 + 10;

            // Draw rows ONCE only
            for (int i = 0; i < sheetsToShow; i++)
            {
                var result = validResults[i];

                Border rowBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(30, 35, 45)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(60, 70, 90)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Width = 370,
                    Height = 22,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                Canvas.SetLeft(rowBorder, 5);
                Canvas.SetTop(rowBorder, 5 + i * 26);
                PreviewCanvas.Children.Add(rowBorder);

                TextBlock info = new TextBlock
                {
                    Text = $"{result.Ref}: {result.L:N0}×{result.W:N0}mm | U:{result.Util:N1}% | W:{result.Waste:N1}%",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Canvas.SetLeft(info, 10);
                Canvas.SetTop(info, 5 + i * 26 + 3);
                PreviewCanvas.Children.Add(info);
            }

            // Single stats line
            txtCurrentLayoutStats.Text = $"Full List: {validResults.Count} sheets";
        }

        private void DrawSingleSheetLayout(int startIndex)
        {
            LayoutCanvas.Children.Clear();

            var validResults = _results.Where(r => r.Ref != "TOTAL").ToList();
            if (validResults.Count == 0 || startIndex < 0) return;

            int sheetsToDraw = Math.Min(_sheetsPerPage, validResults.Count - startIndex);
            if (sheetsToDraw <= 0) return;

            // Calculate rows/cols based on sheetsPerPage
            int cols = 2;
            int rows = (int)Math.Ceiling((double)_sheetsPerPage / cols);

            double canvasW = 800;
            double headerSpace = 25;
            double margin = 15;

            // Each sheet takes ~280px height
            double sheetAreaH = 280;
            double canvasH = rows * sheetAreaH + margin * 2 + headerSpace;

            double sheetAreaW = (canvasW - margin * 2) / cols;

            // Ensure minimum sizes
            if (canvasW < 600) canvasW = 600;
            if (canvasH < 400) canvasH = 400;

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

                int row = i / cols;
                int col = i % cols;

                double areaTop = margin + row * (sheetAreaH + headerSpace);
                double areaLeft = margin + col * sheetAreaW;

                double scale = Math.Min((sheetAreaW - margin * 2) / sheetW, (sheetAreaH - margin * 2) / sheetH) * 0.75 * _zoomLevel;
                if (scale < 0.015) scale = 0.015;

                double drawW = sheetW * scale;
                double drawH = sheetH * scale;
                double startX = areaLeft + (sheetAreaW - drawW) / 2;
                double startY = areaTop + headerSpace;

                // Sheet info at TOP
                TextBlock info = new TextBlock
                {
                    Text = $"#{idx + 1}: {currentResult.L:N0}×{currentResult.W:N0}mm U:{currentResult.Util:N1}% W:{currentResult.Waste:N1}%",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11))
                };
                Canvas.SetLeft(info, areaLeft + 5);
                Canvas.SetTop(info, areaTop + 2);
                LayoutCanvas.Children.Add(info);

                // Draw trim border (RED)
                Rectangle trimBorder = new Rectangle
                {
                    Width = drawW,
                    Height = drawH,
                    Fill = Brushes.Transparent,
                    Stroke = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                    StrokeThickness = 2
                };
                Canvas.SetLeft(trimBorder, startX);
                Canvas.SetTop(trimBorder, startY);
                LayoutCanvas.Children.Add(trimBorder);

                // Draw usable area
                double trimOffset = _lr * scale;
                Rectangle usableSheet = new Rectangle
                {
                    Width = drawW - trimOffset * 2,
                    Height = drawH - trimOffset * 2,
                    Fill = new SolidColorBrush(Color.FromRgb(13, 17, 23)),
                    Stroke = new SolidColorBrush(Color.FromRgb(100, 120, 140)),
                    StrokeThickness = 1
                };
                Canvas.SetLeft(usableSheet, startX + trimOffset);
                Canvas.SetTop(usableSheet, startY + trimOffset);
                LayoutCanvas.Children.Add(usableSheet);

                // Draw parts
                var partsOnSheet = _allPlacedParts
                    .Where(p => p.Sheet == currentResult.SheetRef && p.SheetNum == currentResult.SheetNum)
                    .ToList();

                var colorMap = new Dictionary<string, Color>();
                var partGroups = partsOnSheet.GroupBy(p => p.Ref).ToList();
                for (int c = 0; c < partGroups.Count; c++)
                    colorMap[partGroups[c].Key] = partColors[c % partColors.Length];

                foreach (var part in partsOnSheet)
                {
                    double px = startX + part.X * scale;
                    double py = startY + part.Y * scale;
                    double pw = part.L * scale;
                    double ph = part.W * scale;

                    var color = colorMap.ContainsKey(part.Ref) ? colorMap[part.Ref] : Color.FromRgb(128, 128, 128);

                    Border partBorder = new Border
                    {
                        Width = pw,
                        Height = ph,
                        Background = new SolidColorBrush(color),
                        BorderBrush = new SolidColorBrush(Colors.White),
                        BorderThickness = new Thickness(0.5)
                    };
                    Canvas.SetLeft(partBorder, px);
                    Canvas.SetTop(partBorder, py);
                    LayoutCanvas.Children.Add(partBorder);

                    if (pw > 20 && ph > 10)
                    {
                        TextBlock label = new TextBlock
                        {
                            Text = part.Ref,
                            FontSize = Math.Max(5, Math.Min(pw / 12, 9)),
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Colors.White)
                        };
                        Canvas.SetLeft(label, px + 2);
                        Canvas.SetTop(label, py + 2);
                        LayoutCanvas.Children.Add(label);
                    }
                }
            }

            if (txtCurrentSheet != null)
                txtCurrentSheet.Text = $"Layouts: {startIndex + 1}-{startIndex + sheetsToDraw} of {validResults.Count}";
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _zoomLevel = Math.Min(_zoomLevel + 0.1, 2.5);
            txtZoom.Text = $"{(_zoomLevel * 100):N0}%";
            DrawSingleSheetLayout(_currentIndex);
            // Reset scroll to see full image
            if (LayoutScrollViewer != null)
            {
                LayoutScrollViewer.ScrollToVerticalOffset(0);
                LayoutScrollViewer.ScrollToHorizontalOffset(0);
            }
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _zoomLevel = Math.Max(_zoomLevel - 0.1, 0.3);
            txtZoom.Text = $"{(_zoomLevel * 100):N0}%";
            DrawSingleSheetLayout(_currentIndex);
            // Reset scroll to see full image
            if (LayoutScrollViewer != null)
            {
                LayoutScrollViewer.ScrollToVerticalOffset(0);
                LayoutScrollViewer.ScrollToHorizontalOffset(0);
            }
        }

        private void SheetSelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSheetSelector.SelectedIndex >= 0 && _results.Count > 0)
            {
                int selected = cmbSheetSelector.SelectedIndex;
                _currentIndex = (selected / _sheetsPerPage) * _sheetsPerPage;
                DrawCurrentLayout(_currentIndex);
                DrawSingleSheetLayout(_currentIndex);  // Update center too
                UpdateLayoutCount();
            }
        }

        private void PrevLayout_Click(object sender, RoutedEventArgs e)
        {
            var validResults = _results.Where(r => r.Ref != "TOTAL").ToList();
            if (_currentIndex > 0)
            {
                _currentIndex = Math.Max(0, _currentIndex - _sheetsPerPage);
                cmbSheetSelector.SelectedIndex = _currentIndex;
                DrawCurrentLayout(_currentIndex);
                DrawSingleSheetLayout(_currentIndex);  // Update center
                UpdateLayoutCount();
            }
        }

        private void NextLayout_Click(object sender, RoutedEventArgs e)
        {
            var validResults = _results.Where(r => r.Ref != "TOTAL").ToList();
            if (_currentIndex < validResults.Count - 1)
            {
                _currentIndex = Math.Min(validResults.Count - 1, _currentIndex + _sheetsPerPage);
                cmbSheetSelector.SelectedIndex = _currentIndex;
                DrawCurrentLayout(_currentIndex);
                DrawSingleSheetLayout(_currentIndex);  // Update center
                UpdateLayoutCount();
            }
        }

        private void UpdateLayoutCount()
        {
            var validResults = _results.Where(r => r.Ref != "TOTAL").ToList();
            int totalSheets = validResults.Count;
            int page = (_currentIndex / _sheetsPerPage) + 1;
            int totalPages = (int)Math.Ceiling((double)totalSheets / _sheetsPerPage);
            if (totalPages < 1) totalPages = 1;
            txtLayoutNum.Text = $"Page {page}/{totalPages}";
            // Center shows page info, left shows full count
            txtCurrentLayoutStats.Text = $"Center: {page}/{totalPages} | View: {_sheetsPerPage}/page";
        }

        private void ClearOptimization_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Clear all data?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _stockSheets.Clear();
                _cutParts.Clear();
                _results.Clear();
                _allPlacedParts.Clear();
                _remnants.Clear();
                _cutOperations.Clear();
                txtSheetsUsed.Text = "0";
                txtSheetsRemaining.Text = "0";
                txtUtilization.Text = "0%";
                txtWaste.Text = "0%";
                txtTotalSheetsUsed.Text = "0";
                txtTotalPartsCut.Text = "0";
                txtAvgUtilization.Text = "0%";
                txtTotalStats.Text = "0 sheets, 0 parts";
                txtStockSummary.Text = "No stock added";
                txtPartsSummary.Text = "No parts added";
                txtCurrentLayoutStats.Text = "Select a layout to view";
                txtRemnants.Text = "0 remnants available";
                txtTotalCost.Text = "AED 0.00";
                _currentIndex = 0;
                PreviewCanvas.Children.Clear();
                LayoutCanvas.Children.Clear();
                pnlUnplaced.Visibility = Visibility.Collapsed;
            }
        }

        private void ExportResults_Click(object sender, RoutedEventArgs e)
        {
            if (_results.Count == 0)
            {
                MessageBox.Show("No results to export.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "CSV Files|*.csv", FileName = $"Optimization_{DateTime.Now:yyyyMMdd_HHmmss}.csv" };
                if (dialog.ShowDialog() == true)
                {
                    using (var writer = new System.IO.StreamWriter(dialog.FileName))
                    {
                        writer.WriteLine("Sheet Ref,Length mm,Width mm,Used Qty,Util %,Waste %,Area sqm");
                        foreach (var r in _results.Where(r => r.Ref != "TOTAL"))
                            writer.WriteLine($"{r.Ref},{r.L},{r.W},{r.Used},{r.Util:N2},{r.Waste:N2},{r.Area:N4}");
                        writer.WriteLine($"TOTAL,,,{_results.Count(r => r.Ref != "TOTAL")},,{_overallUtilization:N2},{_overallWastage:N2}");
                        writer.WriteLine();
                        writer.WriteLine("Part Ref,X,Y,Length,Width,Rotated,Sheet,Sheet #");
                        foreach (var p in _allPlacedParts.OrderBy(x => x.Sheet).ThenBy(x => x.SheetNum))
                            writer.WriteLine($"{p.Ref},{p.X:N1},{p.Y:N1},{p.L:N1},{p.W:N1},{(p.IsRotated ? "Yes" : "No")},{p.Sheet},{p.SheetNum}");
                    }
                    MessageBox.Show($"Exported:\n{dialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void PrintLayouts_Click(object sender, RoutedEventArgs e)
        {
            if (_results.Count == 0)
            {
                MessageBox.Show("No layouts to print.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var printWindow = new Window
            {
                Title = "Cutting Layouts - ProGlass",
                Width = 900,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = new SolidColorBrush(Colors.White)
            };
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock { Text = "Glass Cutting Layouts", FontSize = 22, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 20) });
            foreach (var r in _results.Where(r => r.Ref != "TOTAL"))
            {
                var border = new Border { BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new Thickness(1), Padding = new Thickness(15), Margin = new Thickness(0, 0, 0, 15), Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)) };
                border.Child = new TextBlock { Text = $"{r.Ref}: {r.L:N0} × {r.W:N0} mm\nUtilization: {r.Util:N2}%  |  Wastage: {r.Waste:N2}%", FontSize = 13 };
                stack.Children.Add(border);
            }
            var totalBorder = new Border { Background = new SolidColorBrush(Color.FromRgb(245, 158, 11)), Padding = new Thickness(15), Margin = new Thickness(0, 10, 0, 0) };
            totalBorder.Child = new TextBlock { Text = $"TOTAL: {_results.Count(r => r.Ref != "TOTAL")} sheets used", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Colors.White) };
            stack.Children.Add(totalBorder);
            scroll.Content = stack;
            printWindow.Content = scroll;
            printWindow.ShowDialog();
        }

        // These control CENTER 2D view only - don't touch left list
        private void SheetsPerPage4_Click(object sender, RoutedEventArgs e)
        {
            _sheetsPerPage = 4;
            cmbSheetSelector.SelectedIndex = 0;
            _currentIndex = 0;
            DrawSingleSheetLayout(_currentIndex);
            UpdateLayoutCount();
        }

        private void SheetsPerPage6_Click(object sender, RoutedEventArgs e)
        {
            _sheetsPerPage = 6;
            cmbSheetSelector.SelectedIndex = 0;
            _currentIndex = 0;
            DrawSingleSheetLayout(_currentIndex);
            UpdateLayoutCount();
        }

        private void SheetsPerPage8_Click(object sender, RoutedEventArgs e)
        {
            _sheetsPerPage = 8;
            cmbSheetSelector.SelectedIndex = 0;
            _currentIndex = 0;
            DrawSingleSheetLayout(_currentIndex);
            UpdateLayoutCount();
        }

        private void SheetsPerPage10_Click(object sender, RoutedEventArgs e)
        {
            _sheetsPerPage = 10;
            cmbSheetSelector.SelectedIndex = 0;
            _currentIndex = 0;
            DrawSingleSheetLayout(_currentIndex);
            UpdateLayoutCount();
        }

        // ================= NEW FEATURE METHODS =================

        private void CalculateCost()
        {
            if (_usedSQM > 0)
            {
                double pricePerSqm = 25;
                if (txtStockPrice != null)
                    double.TryParse(txtStockPrice.Text, out pricePerSqm);
                double totalCost = _usedSQM * pricePerSqm;

                if (txtTotalCost != null)
                {
                    string currency = "AED ";
                    if (cmbCurrency != null)
                    {
                        currency = cmbCurrency.SelectedIndex switch
                        {
                            0 => "AED ",
                            1 => "$",
                            2 => "€",
                            3 => "₹",
                            4 => "£",
                            _ => "AED "
                        };
                    }
                    txtTotalCost.Text = $"{currency}{totalCost:N2}";
                }
            }
        }

        private void txtStockPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateCost();
        }

        private void TrackRemnants()
        {
            if (txtRemnants != null)
            {
                var usableRemnants = _remnants.Where(r => r.L > 100 && r.W > 100).ToList();
                txtRemnants.Text = $"{usableRemnants.Count} remnants > 100x100mm";
            }
        }

        private void SaveJob_Click(object sender, RoutedEventArgs e)
        {
            string jobName = string.IsNullOrEmpty(txtJobName?.Text) ? $"Job_{DateTime.Now:yyyyMMdd_HHmmss}" : txtJobName.Text;

            var job = new OptimizationJob
            {
                Id = _savedJobs.Count + 1,
                Name = jobName,
                CreatedDate = DateTime.Now,
                SheetsUsed = _results.Count(r => r.Ref != "TOTAL"),
                Utilization = _overallUtilization
            };

            _savedJobs.Add(job);
            cmbSavedJobs.ItemsSource = _savedJobs;
            MessageBox.Show($"Job saved: {jobName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadJob_Selected(object sender, SelectionChangedEventArgs e) { }

        private void LoadJob_Click(object sender, RoutedEventArgs e)
        {
            if (cmbSavedJobs.SelectedItem is OptimizationJob job)
            {
                MessageBox.Show($"Loaded: {job.Name}\nSheets: {job.SheetsUsed}\nUtil: {job.Utilization:N2}%", "Job Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteJob_Click(object sender, RoutedEventArgs e)
        {
            if (cmbSavedJobs.SelectedItem is OptimizationJob job)
            {
                _savedJobs.Remove(job);
                cmbSavedJobs.ItemsSource = _savedJobs;
                MessageBox.Show("Job deleted", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportPDF_Click(object sender, RoutedEventArgs e)
        {
            if (_results.Count == 0)
            {
                MessageBox.Show("No results to export.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "PDF Files|*.pdf", FileName = $"Layouts_{DateTime.Now:yyyyMMdd_HHmmss}.pdf" };
                if (dialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(dialog.FileName.Replace(".pdf", ".txt"),
                        $"Glass Cutting Layouts\nGenerated: {DateTime.Now}\n\n" +
                        $"Total Sheets: {_results.Count(r => r.Ref != "TOTAL")}\n" +
                        $"Utilization: {_overallUtilization:N2}%\n\n" +
                        string.Join("\n", _results.Where(r => r.Ref != "TOTAL").Select(r => $"{r.Ref}: {r.L}x{r.W}mm - U:{r.Util:N2}%")));

                    MessageBox.Show($"Exported:\n{dialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void PrintLabels_Click(object sender, RoutedEventArgs e)
        {
            if (_allPlacedParts.Count == 0)
            {
                MessageBox.Show("No parts to print labels.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var printWindow = new Window
            {
                Title = "Print Labels - Glass Parts",
                Width = 400,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = new SolidColorBrush(Colors.White)
            };
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel { Margin = new Thickness(20) };

            stack.Children.Add(new TextBlock { Text = "Glass Part Labels", FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 15) });

            foreach (var part in _allPlacedParts.Take(50))
            {
                var border = new Border { BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new Thickness(1), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 10) };
                border.Child = new TextBlock { Text = $"ID: {part.Ref}\nSize: {part.L:N0} x {part.W:N0}mm\nSheet: {part.Sheet}-{part.SheetNum}", FontSize = 12 };
                stack.Children.Add(border);
            }

            if (_allPlacedParts.Count > 50)
                stack.Children.Add(new TextBlock { Text = $"... and {_allPlacedParts.Count - 50} more", FontSize = 10, Foreground = new SolidColorBrush(Colors.Gray) });

            scroll.Content = stack;
            printWindow.Content = scroll;
            printWindow.ShowDialog();
        }

        private bool _isSimulating = false;

        private void Simulate_Click(object sender, RoutedEventArgs e)
        {
            if (_allPlacedParts.Count == 0)
            {
                MessageBox.Show("No layout to simulate.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Button clickedBtn = sender as Button;

            if (_isSimulating)
            {
                // STOP simulation
                _simulateTimer.Stop();
                _isSimulating = false;
                _currentIndex = 0;
                DrawSingleSheetLayout(_currentIndex);

                if (clickedBtn != null && clickedBtn.Content != null)
                {
                    clickedBtn.Content = "▶ Simulate";
                    clickedBtn.Background = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                }

                MessageBox.Show("Simulation stopped!", "Stopped", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // START simulation
            _isSimulating = true;
            _currentIndex = 0;
            _simulateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
            _simulateTimer.Tick += (s, args) => NextSimulateStep();
            _simulateTimer.Start();

            if (clickedBtn != null && clickedBtn.Content != null)
            {
                clickedBtn.Content = "⏹ Stop";
                clickedBtn.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            }
        }

        private void NextSimulateStep()
        {
            var validResults = _results.Where(r => r.Ref != "TOTAL").ToList();
            if (validResults.Count == 0)
            {
                _simulateTimer.Stop();
                return;
            }

            // Draw current sheet on center panel
            DrawSingleSheetLayout(_currentIndex);

            // Move to next sheet
            _currentIndex++;

            // If reached end, stop simulation
            if (_currentIndex >= validResults.Count)
            {
                _simulateTimer.Stop();
                _isSimulating = false;
                _currentIndex = 0;

                // Reset button text
                var simulateBtn = FindName("btnSimulate") as Button;
                if (simulateBtn != null)
                {
                    simulateBtn.Content = "▶ Simulate";
                    simulateBtn.Background = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                }

                MessageBox.Show("Simulation complete!", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SetSheetsPerPage(int count)
        {
            _sheetsPerPage = count;
            DrawCurrentLayout(_currentIndex);
            UpdateLayoutCount();
        }

        public void ImportInvoiceItems(List<Models.InvoiceItemModel> parts)
        {
            _cutParts.Clear();
            if (parts == null) return;
            int count = 1;
            foreach (var p in parts)
            {
                if (p == null) continue;
                _cutParts.Add(new CutPart { Ref = !string.IsNullOrEmpty(p.GlassRef) ? p.GlassRef : $"P{count++}", L = p.Width1, W = p.Height1, Rot = true, Qty = p.Qty > 0 ? p.Qty : 1 });
            }
            UpdatePartsSummary();
            RunOptimizationFromInvoice();
        }

        public void SetStockSheet(double width, double height, int qty = 9999999)
        {
            _stockSheets.Clear();
            _stockSheets.Add(new StockSheet { Ref = "Stock 1", L = width, W = height, Qty = qty });
            UpdateStockSummary();
        }

        public void SetTrimSettings(double lm, double rm, double tm, double bm, double breakout, double kerf)
        {
            _lr = lm; _rm = rm; _tr = tm; _br = bm; _breakout = breakout; _kerf = kerf;
            if (txtLR != null) txtLR.Text = lm.ToString();
            if (txtRM != null) txtRM.Text = rm.ToString();
            if (txtTR != null) txtTR.Text = tm.ToString();
            if (txtBR != null) txtBR.Text = bm.ToString();
            if (txtBreakout != null) txtBreakout.Text = breakout.ToString();
            if (txtKerf != null) txtKerf.Text = kerf.ToString();
        }

        public double AverageUtilization => _overallUtilization;
        public int SheetsUsed => _results.Where(r => r.Ref != "TOTAL").Sum(r => r.Used);
        public double UsedSQM => _usedSQM;

        public void RunOptimizationFromInvoice()
        {
            double.TryParse(txtLR.Text, out _lr);
            double.TryParse(txtBR.Text, out _br);
            double.TryParse(txtTR.Text, out _tr);
            double.TryParse(txtRM.Text, out _rm);
            double.TryParse(txtKerf.Text, out _kerf);
            double.TryParse(txtBreakout.Text, out _breakout);

            if (_stockSheets.Count == 0 || _cutParts.Count == 0) return;

            try
            {
                if (cmbRotation != null)
                    _rotationPolicy = (RotationPolicy)cmbRotation.SelectedIndex;

                RunNestingAlgorithm(_kerf, _rotationPolicy);
                UpdateReportSection();
                ShowTab("Layouts");  // Show layouts tab after run
            }
            catch { }
        }
    }

    // ================= DATA CLASSES =================
    public class StockSheet
    {
        public string Ref { get; set; } = "";
        public double L { get; set; }
        public double W { get; set; }
        public int Qty { get; set; }
        public double Area => (L * W) / 1000000.0;
    }

    public class CutPart
    {
        public int Id { get; set; }
        public string Ref { get; set; } = "";
        public double L { get; set; }
        public double W { get; set; }
        public bool Rot { get; set; }
        public int Qty { get; set; }
        public double Area => (L * W * Qty) / 1000000.0;
        public bool IsPlaced { get; set; }
        public double PlacedX { get; set; }
        public double PlacedY { get; set; }
        public double PlacedW { get; set; }
        public double PlacedH { get; set; }
    }

    public class OptimizationResult
    {
        public string Ref { get; set; } = "";
        public string SheetRef { get; set; } = "";
        public int SheetNum { get; set; }
        public double L { get; set; }
        public double W { get; set; }
        public int Used { get; set; }
        public double Area { get; set; }
        public double Util { get; set; }
        public double Waste { get; set; }
    }

    public class PlacedPart
    {
        public string Ref { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public double L { get; set; }
        public double W { get; set; }
        public bool IsRotated { get; set; }
        public string Sheet { get; set; } = "";
        public int SheetNum { get; set; }
    }

    // ================= NEW FEATURES DATA CLASSES =================

    public class Remnant
    {
        public string Ref { get; set; } = "";
        public double L { get; set; }
        public double W { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public string FromSheet { get; set; }
        public int SheetNum { get; set; }
        public bool IsUsed { get; set; }
    }

    public class OptimizationJob
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
        public string StockJson { get; set; }
        public string PartsJson { get; set; }
        public int SheetsUsed { get; set; }
        public double Utilization { get; set; }
    }

    public class CostSettings
    {
        public double StockPricePerSQM { get; set; }
        public double KerfCostPerMm { get; set; }
        public double OperatingCostPerHour { get; set; }
    }

    public class CutOperation
    {
        public string PartRef { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsRotated { get; set; }
    }
}