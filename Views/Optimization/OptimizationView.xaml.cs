using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ProGlassAutomation.Views.Optimization
{
    public partial class OptimizationView : UserControl
    {
        private OptimizationEngine _engine = new OptimizationEngine();
        private OptimizationServices _services = new OptimizationServices();

        private ObservableCollection<StockSheet> _stockSheets = new ObservableCollection<StockSheet>();
        private ObservableCollection<CutPart> _cutParts = new ObservableCollection<CutPart>();
        private ObservableCollection<SpecificationModel> _specifications = new ObservableCollection<SpecificationModel>();
        private ObservableCollection<CombinedSpecItem> _combinedItems = new ObservableCollection<CombinedSpecItem>();
        private List<SpecificationModel> _selectedSpecs = new List<SpecificationModel>();
        private ObservableCollection<OptimizationResult> _results = new ObservableCollection<OptimizationResult>();
        private List<PlacedPart> _allPlacedParts = new List<PlacedPart>();
        private ObservableCollection<OptimizationJob> _savedJobs = new ObservableCollection<OptimizationJob>();

        private double _zoomLevel = 1.5; // Increased from 1.0 - shows more detail
        private int _currentIndex = 0;
        private int _sheetsPerPage = 8; // Maximum 8 sheets per page
        private bool _isSimulating = false;
        private DispatcherTimer _simulateTimer;

        // Match engine defaults for proper coordinate mapping
        private double _lr = 15, _br = 15, _tr = 15, _rm = 15, _kerf = 4.0, _breakout = 4.0;
        private RotationPolicy _rotationPolicy = RotationPolicy.BestFit;

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

            // FIX: Configure engine at start + set rotation
            _engine.Configure(_lr, _rm, _tr, _br, _kerf, _breakout);
            _engine.SetEngineMode(EngineMode.IQ200V7);
            _engine.SetRotationPolicy(RotationPolicy.BestFit); // ADD THIS - default
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
            UpdateStockSummary();
        }

        public void AddStockSheet(double width, double height, int qty = 9999999)
        {
            int idx = _stockSheets.Count + 1;
            _stockSheets.Add(new StockSheet { Ref = $"S{idx}", L = width, W = height, Qty = qty });
            UpdateStockSummary();
        }

        public void ClearStockSheets()
        {
            _stockSheets.Clear();
            UpdateStockSummary();
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
                btn.Style = (Style)FindResource("TabActive");

                switch (tab)
                {
                    case "Stock": pnlStock.Visibility = Visibility.Visible; break;
                    case "Parts": pnlParts.Visibility = Visibility.Visible; break;
                    case "Settings": pnlSettings.Visibility = Visibility.Visible; break;
                    case "Layouts": pnlSummary.Visibility = Visibility.Visible; break;
                    case "Report": pnlReport.Visibility = Visibility.Visible; break;
                }
            }
        }

        private void ResetTabs()
        {
            btnStock.Style = (Style)FindResource("TabInactive");
            btnParts.Style = (Style)FindResource("TabInactive");
            btnSettings.Style = (Style)FindResource("TabInactive");
            btnSummary.Style = (Style)FindResource("TabInactive");
            btnReport.Style = (Style)FindResource("TabInactive");
            pnlStock.Visibility = Visibility.Collapsed;
            pnlParts.Visibility = Visibility.Collapsed;
            pnlSettings.Visibility = Visibility.Collapsed;
            pnlSummary.Visibility = Visibility.Collapsed;
            pnlReport.Visibility = Visibility.Collapsed;
        }

        private void ShowTab(string tab)
        {
            ResetTabs();
            switch (tab)
            {
                case "Stock": btnStock.Style = (Style)FindResource("TabActive"); pnlStock.Visibility = Visibility.Visible; break;
                case "Parts": btnParts.Style = (Style)FindResource("TabActive"); pnlParts.Visibility = Visibility.Visible; break;
                case "Settings": btnSettings.Style = (Style)FindResource("TabActive"); pnlSettings.Visibility = Visibility.Visible; break;
                case "Layouts": btnSummary.Style = (Style)FindResource("TabActive"); pnlSummary.Visibility = Visibility.Visible; break;
                case "Report": btnReport.Style = (Style)FindResource("TabActive"); pnlReport.Visibility = Visibility.Visible; break;
            }
        }

        private void AddStockSheet_Click(object sender, RoutedEventArgs e)
        {
            _stockSheets.Add(new StockSheet { Ref = $"S{_stockSheets.Count + 1}", L = 3300, W = 2433, Qty = 9999999 });
            UpdateStockSummary();
        }

        private void DeleteStockRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is StockSheet sheet)
                _stockSheets.Remove(sheet);
            UpdateStockSummary();
        }

        private void AddPart_Click(object sender, RoutedEventArgs e)
        {
            _cutParts.Add(new CutPart { Ref = $"P{_cutParts.Count + 1}", L = 1000, W = 1000, Rot = true, Qty = 1 });
            UpdatePartsSummary();
        }

        private void DeletePartRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CutPart part)
                _cutParts.Remove(part);
            UpdatePartsSummary();
        }

        private void DG_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) { }

        private void DG_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                string text = e.Data.GetData(DataFormats.Text) as string;
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

        private void UpdateStockSummary()
        {
            if (_stockSheets.Count == 0) { txtStockSummary.Text = "No stock added"; return; }
            var groups = _stockSheets.GroupBy(s => new { s.L, s.W })
                .Select(g => new { g.Key.L, g.Key.W, TotalQty = g.Sum(s => s.Qty), SQM = g.Sum(s => s.L * s.W * s.Qty) / 1000000.0 })
                .OrderByDescending(x => x.SQM).ToList();
            var lines = groups.Select(g => $"{g.L:N0}×{g.W:N0}mm = {g.TotalQty} ({g.SQM:N2}m²)").ToList();
            lines.Insert(0, $"Stock: {_stockSheets.Count} types, {_stockSheets.Sum(s => s.Qty)} total");
            txtStockSummary.Text = string.Join("\n", lines);
        }

        private void UpdatePartsSummary()
        {
            if (_cutParts.Count == 0) { txtPartsSummary.Text = "No parts added"; return; }
            var totalQty = _cutParts.Sum(p => p.Qty);
            var totalSQM = _cutParts.Sum(p => p.L * p.W * p.Qty) / 1000000.0;
            var groups = _cutParts.GroupBy(p => new { p.L, p.W, p.Ref })
                .Select(g => new { Ref = g.Key.Ref, L = g.Key.L, W = g.Key.W, Qty = g.Sum(x => x.Qty), SQM = g.Sum(x => x.L * x.W * x.Qty) / 1000000.0 })
                .OrderByDescending(x => x.SQM).ToList();
            var lines = groups.Select(g => $"{g.Ref}: {g.L}×{g.W}×{g.Qty} = {g.SQM:N2}m²").ToList();
            lines.Insert(0, $"Parts: {totalQty} total ({totalSQM:N2}m²)");
            txtPartsSummary.Text = string.Join("\n", lines);
        }

        private void RunOptimization_Click(object sender, RoutedEventArgs e)
        {
            if (_combinedItems.Count > 0)
            {
                UpdateOptimizableItems();
            }

            double.TryParse(txtLR.Text, out _lr);
            double.TryParse(txtBR.Text, out _br);
            double.TryParse(txtTR.Text, out _tr);
            double.TryParse(txtRM.Text, out _rm);
            double.TryParse(txtKerf.Text, out _kerf);
            double.TryParse(txtBreakout.Text, out _breakout);

            // FIX: Force BestFit rotation always (fixes rotation not applied)
            _rotationPolicy = RotationPolicy.BestFit;

            if (cmbRotation != null && cmbRotation.SelectedIndex >= 0)
            {
                _rotationPolicy = (RotationPolicy)cmbRotation.SelectedIndex;
            }

            _engine.Configure(_lr, _rm, _tr, _br, _kerf, _breakout);
            _engine.SetRotationPolicy(_rotationPolicy);

            if (_stockSheets.Count == 0 || _cutParts.Count == 0)
            {
                MessageBox.Show("Please add stock sheets and parts first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // DEBUG: Count parts before optimization
                int partsBefore = _cutParts.Sum(p => p.Qty);
                System.Diagnostics.Debug.WriteLine($"=== PARTS BEFORE: {partsBefore} ===");

                _engine.ExecuteNesting(_stockSheets.ToList(), _cutParts.ToList(), _results, _allPlacedParts, _savedJobs);
                UpdateReportSection();
                UpdateResultsGrouping();
                ShowTab("Layouts");

                // Force draw layouts - THIS WAS MISSING
                _currentIndex = 0;
                cmbSheetSelector.SelectedIndex = 0;
                DrawCurrentLayout(_currentIndex);
                DrawSingleSheetLayout(_currentIndex);
                UpdateLayoutCount();

                // DEBUG: Show detailed breakdown
                int placedCount = _allPlacedParts.Count;
                int unplacedCount = _engine.TotalPartsUnplaced;
                var partsPerSheetList = _allPlacedParts.GroupBy(p => p.Sheet).Select(g => $"{g.Key}: {g.Count()}").ToList();

                // Show unplaced parts details
                var unplacedList = _cutParts.Where(p => !p.IsPlaced).Take(5).Select(p => $"{p.Ref}: {p.L}x{p.W}").ToList();
                string unplacedInfo = unplacedList.Count > 0 ? "\n\nNOT PLACED:\n" + string.Join("\n", unplacedList) : "";

                MessageBox.Show(
                    $"=== COMPLETE ===\n\n" +
                    $"Parts Input: {partsBefore}\n" +
                    $"Parts Placed: {placedCount}\n" +
                    $"Parts NOT Placed: {unplacedCount}\n" +
                    $"Sheets Used: {_results.Count(r => r.Ref != "TOTAL")}\n{unplacedInfo}",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files|*.csv",
                    FileName = $"Optimization_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    _services.ExportResultsCsv(dialog.FileName, _results.ToList(), _allPlacedParts, _engine.OverallUtilization, _engine.OverallWastage);
                    MessageBox.Show($"Exported:\n{dialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF Files|*.pdf",
                    FileName = $"Layouts_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    _services.ExportPdf(dialog.FileName, _results.ToList(), _engine.OverallUtilization);
                    MessageBox.Show($"Exported:\n{dialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveJob_Click(object sender, RoutedEventArgs e)
        {
            string jobName = string.IsNullOrEmpty(txtJobName?.Text) ? $"Job_{DateTime.Now:yyyyMMdd_HHmmss}" : txtJobName.Text;

            var job = _services.CreateJob(jobName, _results.Count(r => r.Ref != "TOTAL"), _engine.OverallUtilization, _stockSheets.ToList(), _cutParts.ToList());
            job.Id = _savedJobs.Count + 1;

            _savedJobs.Add(job);
            cmbSavedJobs.ItemsSource = _savedJobs;

            MessageBox.Show($"Job saved: {jobName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadJob_Click(object sender, RoutedEventArgs e)
        {
            if (cmbSavedJobs.SelectedItem is OptimizationJob job)
            {
                // FIXED: Use out parameters (backward compatible)
                var loaded = _services.LoadJob(job);
                var stockSheets = loaded.stocks;
                var cutParts = loaded.parts;

                _stockSheets.Clear();
                _cutParts.Clear();

                foreach (var s in stockSheets) _stockSheets.Add(s);
                foreach (var p in cutParts) _cutParts.Add(p);

                UpdateStockSummary();
                UpdatePartsSummary();

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

        // =====================================================
        // DRAWING METHODS (Same as before)
        // =====================================================

        private void DrawCurrentLayout(int startIndex)
        {
            PreviewCanvas.Children.Clear();
            if (_results.Count == 0 || startIndex < 0) return;

            var validResults = _results.Where(r => r.Ref != "TOTAL").ToList();
            if (validResults.Count == 0) return;

            int sheetsToShow = Math.Min(validResults.Count, 100);
            PreviewCanvas.Width = 390;
            PreviewCanvas.Height = sheetsToShow * 26 + 10;

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

            txtCurrentLayoutStats.Text = $"Full List: {validResults.Count} sheets";
        }

        private void DrawSingleSheetLayout(int startIndex)
        {
            // ADD THIS: Clear canvas FIRST before any drawing
            LayoutCanvas.Children.Clear();

            if (!string.IsNullOrEmpty(txtFindPart?.Text) && txtFindPart.Text.Length > 0)
            {
                HighlightPartsOnLayout(txtFindPart.Text.Trim().ToUpper());
                return;
            }

            var validResults = _results.Where(r => r.Ref != "TOTAL").ToList();
            if (validResults.Count == 0 || startIndex < 0) return;

            int sheetsToDraw = Math.Min(_sheetsPerPage, validResults.Count - startIndex);
            if (sheetsToDraw <= 0)
            {
                sheetsToDraw = Math.Min(validResults.Count - startIndex, _sheetsPerPage);
                if (sheetsToDraw <= 0) sheetsToDraw = Math.Min(validResults.Count, _sheetsPerPage);
            }

            int cols = 2;
            // INCREASED: Bigger sheet areas for better visibility
            double canvasW = 850, margin = 15, headerSpace = 22;
            double sheetAreaH = 260, sheetAreaW = (canvasW - margin * 2) / cols;  // Bigger: 260 instead of 180
            double canvasH = ((_sheetsPerPage / cols) + 1) * sheetAreaH + margin * 2 + headerSpace;

            LayoutCanvas.Width = canvasW < 600 ? 600 : canvasW;
            LayoutCanvas.Height = canvasH < 400 ? 400 : canvasH;

            Color[] partColors = new Color[]
            {
        Color.FromRgb(59, 130,246), Color.FromRgb(16,185,129),
        Color.FromRgb(139,92,246), Color.FromRgb(245,158,11),
        Color.FromRgb(239,68,68), Color.FromRgb(6,182,212),
        Color.FromRgb(236,72,153), Color.FromRgb(34,197,94)
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

                // Scale to fit sheet in allocated area - BIGGER for better visibility
                double scaleX = (sheetAreaW - margin * 2) / sheetW;
                double scaleY = (sheetAreaH - margin * 2) / sheetH;
                double scale = Math.Min(scaleX, scaleY);

                // INCREASED: Bigger scale for better visibility (was 0.50)
                scale = scale * 0.80 * _zoomLevel;  // Bigger!
                if (scale < 0.05) scale = 0.05;  // Minimum 5%
                if (scale > 1.0) scale = 1.0;  // Cap at 100%

                // BIGGER: Full size drawing
                double drawW = sheetW * scale;
                double drawH = sheetH * scale;
                // Center in allocated area
                double startX = areaLeft + (sheetAreaW - drawW) / 2;
                double startY = areaTop + headerSpace;

                // Calculate usable area inside trim
                double trimOffset = _lr * scale;
                double usableW = drawW - trimOffset * 2;
                double usableH = drawH - trimOffset * 2;

                // Header
                TextBlock info = new TextBlock
                {
                    Text = $"#{idx + 1}: {currentResult.L:N0}×{currentResult.W:N0}mm U:{currentResult.Util:N1}%",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                };
                Canvas.SetLeft(info, areaLeft + 5);
                Canvas.SetTop(info, areaTop + 2);
                LayoutCanvas.Children.Add(info);

                // Sheet border (RED trim border)
                Rectangle trimBorder = new Rectangle
                {
                    Width = drawW,
                    Height = drawH,
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.Red,
                    StrokeThickness = 2
                };
                Canvas.SetLeft(trimBorder, startX);
                Canvas.SetTop(trimBorder, startY);
                LayoutCanvas.Children.Add(trimBorder);

                // Usable area (inner sheet)
                Rectangle usableSheet = new Rectangle
                {
                    Width = usableW,
                    Height = usableH,
                    Fill = new SolidColorBrush(Color.FromRgb(13, 17, 23)),
                    Stroke = new SolidColorBrush(Color.FromRgb(100, 120, 140)),
                    StrokeThickness = 1
                };
                Canvas.SetLeft(usableSheet, startX + trimOffset);
                Canvas.SetTop(usableSheet, startY + trimOffset);
                LayoutCanvas.Children.Add(usableSheet);

                // ===================== TRIM LABELS (L,R,T,B) =====================

                // LEFT
                TextBlock leftTrim = new TextBlock { Text = $"L:{_lr}", FontSize = 6, Foreground = Brushes.Red, FontWeight = FontWeights.Bold };
                Canvas.SetLeft(leftTrim, startX + 2); Canvas.SetTop(leftTrim, startY + trimOffset);
                LayoutCanvas.Children.Add(leftTrim);

                // RIGHT
                TextBlock rightTrim = new TextBlock { Text = $"R:{_rm}", FontSize = 6, Foreground = Brushes.Red, FontWeight = FontWeights.Bold };
                Canvas.SetLeft(rightTrim, startX + drawW - 20); Canvas.SetTop(rightTrim, startY + trimOffset);
                LayoutCanvas.Children.Add(rightTrim);

                // TOP
                TextBlock topTrim = new TextBlock { Text = $"T:{_tr}", FontSize = 6, Foreground = Brushes.Red, FontWeight = FontWeights.Bold };
                Canvas.SetLeft(topTrim, startX + trimOffset); Canvas.SetTop(topTrim, startY + 2);
                LayoutCanvas.Children.Add(topTrim);

                // BOTTOM
                TextBlock bottomTrim = new TextBlock { Text = $"B:{_br}", FontSize = 6, Foreground = Brushes.Red, FontWeight = FontWeights.Bold };
                Canvas.SetLeft(bottomTrim, startX + trimOffset); Canvas.SetTop(bottomTrim, startY + drawH - 12);
                LayoutCanvas.Children.Add(bottomTrim);

                // Sheet size in center
                TextBlock sheetSize = new TextBlock
                {
                    Text = $"{currentResult.L:N0}×{currentResult.W}",
                    FontSize = 8,
                    Foreground = Brushes.Cyan,
                    FontWeight = FontWeights.Bold
                };
                Canvas.SetLeft(sheetSize, startX + drawW / 2 - 35);
                Canvas.SetTop(sheetSize, startY + drawH / 2 - 10);
                LayoutCanvas.Children.Add(sheetSize);

                // Get parts for this sheet - NOTE: X,Y are already relative to trim origin!
                var partsOnSheet = _allPlacedParts
                    .Where(p => p.Sheet == currentResult.SheetRef && p.SheetNum == currentResult.SheetNum)
                    .ToList();

                var colorMap = new Dictionary<string, Color>();
                var partGroups = partsOnSheet.GroupBy(p => p.Ref).ToList();
                for (int c = 0; c < partGroups.Count; c++)
                    colorMap[partGroups[c].Key] = partColors[c % partColors.Length];

                foreach (var part in partsOnSheet)
                {
                    // Simply skip truly invalid coordinates
                    if (part.X < -500 || part.Y < -500 || part.L <= 0 || part.W <= 0)
                    {
                        continue;
                    }

                    // Calculate display position using part.X and part.Y directly
                    double px = startX + trimOffset + part.X * scale;
                    double py = startY + trimOffset + part.Y * scale;

                    // Use original dimensions - rotation already applied in placement
                    // The IsRotated flag indicates PART WAS ROTATED during placement
                    // so L and W already contain the final orientation
                    double pw = part.L * scale;
                    double ph = part.W * scale;

                    var color = colorMap.ContainsKey(part.Ref) ? colorMap[part.Ref] : Color.FromRgb(128, 128, 128);

                    // Draw part - MORE VISIBLE
                    Border partBorder = new Border
                    {
                        Width = pw,
                        Height = ph,
                        Background = new SolidColorBrush(color),
                        BorderBrush = part.IsRotated ? Brushes.Yellow : Brushes.White,
                        BorderThickness = new Thickness(1)  // Always visible border
                    };
                    Canvas.SetLeft(partBorder, px);
                    Canvas.SetTop(partBorder, py);
                    LayoutCanvas.Children.Add(partBorder);

                    // Draw label
                    if (pw > 25 && ph > 15)
                    {
                        string displayText = part.IsRotated
                            ? $"{part.Ref}\n{part.W:N0}×{part.L}\n⟳ ROT"
                            : $"{part.Ref}\n{part.L:N0}×{part.W}";

                        TextBlock label = new TextBlock
                        {
                            Text = displayText,
                            FontSize = Math.Max(4, Math.Min(pw / 14, 7)),
                            FontWeight = FontWeights.Bold,
                            Foreground = part.IsRotated ? Brushes.Black : Brushes.White,
                            TextAlignment = TextAlignment.Center
                        };
                        Canvas.SetLeft(label, px + 2);
                        Canvas.SetTop(label, py + (ph / 2) - 10);
                        LayoutCanvas.Children.Add(label);
                    }
                }
            }

            if (txtCurrentSheet != null)
                txtCurrentSheet.Text = $"Layouts: {startIndex + 1}-{startIndex + sheetsToDraw} of {validResults.Count}";
        }

        // =====================================================
        // HIGHLIGHT PARTS (Same structure)
        // =====================================================

        private void HighlightPartsOnLayout(string searchText)
        {
            LayoutCanvas.Children.Clear();
            var validResults = _results.Where(r => r.Ref != "TOTAL").ToList();
            if (validResults.Count == 0 || _currentIndex < 0) return;

            // Auto-adjust to show remaining sheets
            int actualStartIndex = _currentIndex;
            if (actualStartIndex >= validResults.Count) actualStartIndex = 0;
            int sheetsToDrawCount = Math.Min(_sheetsPerPage, validResults.Count - actualStartIndex);
            if (sheetsToDrawCount <= 0) sheetsToDrawCount = Math.Min(validResults.Count, _sheetsPerPage);

            if (sheetsToDrawCount  <= 0 || validResults.Count == 0)
            {
                txtCurrentSheet.Text = "No layouts to display";
                return;
            }

            int cols = 2;
            double canvasW = 800, margin = 15, headerSpace = 25;
            double sheetAreaH = 280, sheetAreaW = (canvasW - margin * 2) / cols;
            double canvasH = ((_sheetsPerPage / cols) + 1) * sheetAreaH + margin * 2 + headerSpace;

            LayoutCanvas.Width = canvasW < 600 ? 600 : canvasW;
            LayoutCanvas.Height = canvasH < 400 ? 400 : canvasH;

            Color[] partColors = new Color[]
            {
        Color.FromRgb(59,130,246), Color.FromRgb(16,185,129),
        Color.FromRgb(139,92,246), Color.FromRgb(245,158,11),
        Color.FromRgb(239,68,68), Color.FromRgb(6,182,212),
        Color.FromRgb(236,72,153), Color.FromRgb(34,197,94)
            };

            for (int i = 0; i < sheetsToDrawCount; i++)
            {
                int idx = _currentIndex + i;
                if (idx >= validResults.Count) break;

                var currentResult = validResults[idx];
                double sheetW = currentResult.L, sheetH = currentResult.W;
                int row = i / cols, col = i % cols;
                double areaTop = margin + row * (sheetAreaH + headerSpace);
                double areaLeft = margin + col * sheetAreaW;

                // FIX: Better scale calculation - ensure all parts are visible
                double scaleX = (sheetAreaW - margin * 2) / sheetW;
                double scaleY = (sheetAreaH - margin * 2) / sheetH;
                double scale = Math.Min(scaleX, scaleY) * 0.70 * _zoomLevel;

                // Minimum scale - ensure parts are visible but not too small
                if (scale < 0.01) scale = 0.01;
                if (scale > 0.5) scale = 0.5; // Maximum scale cap

                // DEBUG: Calculate parts count for this sheet
                var partsOnSheetDebug = _allPlacedParts.Where(p => p.Sheet == currentResult.SheetRef && p.SheetNum == currentResult.SheetNum).Count();
                System.Diagnostics.Debug.WriteLine($"Sheet {idx}: scale={scale:F4}, parts={partsOnSheetDebug}");

                double drawW = sheetW * scale, drawH = sheetH * scale;
                double startX = areaLeft + (sheetAreaW - drawW) / 2;
                double startY = areaTop + headerSpace;

                TextBlock info = new TextBlock
                {
                    Text = $"#{idx + 1}: {currentResult.L:N0}×{currentResult.W:N0}mm U:{currentResult.Util:N1}%",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                };
                Canvas.SetLeft(info, areaLeft + 5);
                Canvas.SetTop(info, areaTop + 2);
                LayoutCanvas.Children.Add(info);

                Rectangle trimBorder = new Rectangle { Width = drawW, Height = drawH, Fill = Brushes.Transparent, Stroke = Brushes.Red, StrokeThickness = 2 };
                Canvas.SetLeft(trimBorder, startX);
                Canvas.SetTop(trimBorder, startY);
                LayoutCanvas.Children.Add(trimBorder);

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

                // =====================================================
                // SHOW ALL 4 SIDES OF SHEET (Trim values)
                // =====================================================

                // LEFT trim
                TextBlock leftTrim = new TextBlock
                {
                    Text = $"L:{_lr}",
                    FontSize = 6,
                    Foreground = Brushes.Red,
                    FontWeight = FontWeights.Bold
                };
                Canvas.SetLeft(leftTrim, startX + 2);
                Canvas.SetTop(leftTrim, startY + trimOffset);
                LayoutCanvas.Children.Add(leftTrim);

                // RIGHT trim
                TextBlock rightTrim = new TextBlock
                {
                    Text = $"R:{_rm}",
                    FontSize = 6,
                    Foreground = Brushes.Red,
                    FontWeight = FontWeights.Bold
                };
                Canvas.SetLeft(rightTrim, startX + drawW - 18);
                Canvas.SetTop(rightTrim, startY + trimOffset);
                LayoutCanvas.Children.Add(rightTrim);

                // TOP trim
                TextBlock topTrim = new TextBlock
                {
                    Text = $"T:{_tr}",
                    FontSize = 6,
                    Foreground = Brushes.Red,
                    FontWeight = FontWeights.Bold
                };
                Canvas.SetLeft(topTrim, startX + trimOffset);
                Canvas.SetTop(topTrim, startY + 2);
                LayoutCanvas.Children.Add(topTrim);

                // BOTTOM trim
                TextBlock bottomTrim = new TextBlock
                {
                    Text = $"B:{_br}",
                    FontSize = 6,
                    Foreground = Brushes.Red,
                    FontWeight = FontWeights.Bold
                };
                Canvas.SetLeft(bottomTrim, startX + trimOffset);
                Canvas.SetTop(bottomTrim, startY + drawH - 12);
                LayoutCanvas.Children.Add(bottomTrim);

                // Show SHEET SIZE
                TextBlock sheetSize = new TextBlock
                {
                    Text = $"{currentResult.L:N0}×{currentResult.W}",
                    FontSize = 8,
                    Foreground = Brushes.Cyan,
                    FontWeight = FontWeights.Bold
                };
                Canvas.SetLeft(sheetSize, startX + drawW / 2 - 30);
                Canvas.SetTop(sheetSize, startY + drawH / 2 - 10);
                LayoutCanvas.Children.Add(sheetSize);

                var partsOnSheet = _allPlacedParts.Where(p => p.Sheet == currentResult.SheetRef && p.SheetNum == currentResult.SheetNum).ToList();

                var colorMap = new Dictionary<string, Color>();
                var partGroups = partsOnSheet.GroupBy(p => p.Ref).ToList();
                for (int c = 0; c < partGroups.Count; c++)
                    colorMap[partGroups[c].Key] = partColors[c % partColors.Length];

                bool foundOnThisSheet = false;

                foreach (var part in partsOnSheet)
                {
                    // Simply skip truly invalid coordinates (allow X=0, Y=0 as valid positions)
                    if (part.L <= 0 || part.W <= 0)
                    {
                        continue;
                    }

                    // Use original dimensions - rotation already applied in L/W
                    double px = startX + trimOffset + part.X * scale;
                    double py = startY + trimOffset + part.Y * scale;
                    double pw = part.L * scale;
                    double ph = part.W * scale;

                    bool isMatch = part.Ref.ToUpper().Contains(searchText);
                    if (isMatch) foundOnThisSheet = true;

                    var color = colorMap.ContainsKey(part.Ref) ? colorMap[part.Ref] : Color.FromRgb(128, 128, 128);
                    var fillColor = isMatch ? Color.FromRgb(255, 215, 0) : color;

                    Border partBorder = new Border
                    {
                        Width = pw,
                        Height = ph,
                        Background = new SolidColorBrush(fillColor),
                        BorderBrush = new SolidColorBrush(isMatch ? Colors.White : Colors.Black),
                        BorderThickness = new Thickness(isMatch ? 3 : 0.5)
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
                            Foreground = isMatch ? Brushes.Black : Brushes.White
                        };
                        Canvas.SetLeft(label, px + 2);
                        Canvas.SetTop(label, py + 2);
                        LayoutCanvas.Children.Add(label);
                    }
                }
            }

            if (txtCurrentSheet != null)
                txtCurrentSheet.Text = $"Sheet {actualStartIndex + 1}-{actualStartIndex + sheetsToDrawCount} of {validResults.Count} | Parts: {_allPlacedParts.Count}";

            if (_allPlacedParts.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Displaying {validResults.Count} sheets with {_allPlacedParts.Count} total parts");
            }
        }

        // =====================================================
        // ZOOM CONTROLS
        // =====================================================

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _zoomLevel = Math.Min(_zoomLevel + 0.1, 2.5);
            txtZoom.Text = $"{(_zoomLevel * 100):N0}%";
            DrawSingleSheetLayout(_currentIndex);
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
            if (LayoutScrollViewer != null)
            {
                LayoutScrollViewer.ScrollToVerticalOffset(0);
                LayoutScrollViewer.ScrollToHorizontalOffset(0);
            }
        }

        // =====================================================
        // LAYOUT NAVIGATION
        // =====================================================

        private void SheetSelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSheetSelector.SelectedIndex >= 0 && _results.Count > 0)
            {
                int selected = cmbSheetSelector.SelectedIndex;
                _currentIndex = (selected / _sheetsPerPage) * _sheetsPerPage;
                DrawCurrentLayout(_currentIndex);
                DrawSingleSheetLayout(_currentIndex);
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
                DrawSingleSheetLayout(_currentIndex);
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
                DrawSingleSheetLayout(_currentIndex);
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
            txtCurrentLayoutStats.Text = $"Center: {page}/{totalPages} | View: {_sheetsPerPage}/page";
        }

        private void SheetsPerPage4_Click(object sender, RoutedEventArgs e) { SetSheetsPerPage(4); }
        private void SheetsPerPage6_Click(object sender, RoutedEventArgs e) { SetSheetsPerPage(6); }
        private void SheetsPerPage8_Click(object sender, RoutedEventArgs e) { SetSheetsPerPage(8); }
        private void SheetsPerPage10_Click(object sender, RoutedEventArgs e) { SetSheetsPerPage(10); }

        private void SetSheetsPerPage(int count)
        {
            _sheetsPerPage = count;
            cmbSheetSelector.SelectedIndex = 0;
            _currentIndex = 0;
            DrawCurrentLayout(_currentIndex);
            DrawSingleSheetLayout(_currentIndex);
            UpdateLayoutCount();
        }

        // =====================================================
        // FIND FUNCTIONALITY
        // =====================================================

        private void txtFindPart_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtFindPart?.Text))
            {
                DrawSingleSheetLayout(_currentIndex);
                return;
            }
            string searchText = txtFindPart.Text.Trim().ToUpper();
            HighlightPartsOnLayout(searchText);
        }

        private void txtFindPart_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) btnFindNext_Click(sender, e);
        }

        private void btnFindNext_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtFindPart?.Text)) return;
            string searchText = txtFindPart.Text.Trim().ToUpper();

            var matchingParts = _allPlacedParts.Where(p => p.Ref.ToUpper().Contains(searchText)).ToList();
            if (matchingParts.Count == 0)
            {
                MessageBox.Show($"Part '{txtFindPart?.Text}' not found.", "Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            foreach (var part in matchingParts)
            {
                var result = _results.FirstOrDefault(r => r.SheetRef == part.Sheet && r.SheetNum == part.SheetNum);
                if (result != null)
                {
                    int index = _results.ToList().FindIndex(r => r.Ref == result.Ref);
                    if (index >= 0)
                    {
                        _currentIndex = (index / _sheetsPerPage) * _sheetsPerPage;
                        cmbSheetSelector.SelectedIndex = index;
                        DrawCurrentLayout(_currentIndex);
                        HighlightPartsOnLayout(searchText);
                        MessageBox.Show($"Found on {part.Sheet}-{part.SheetNum}\n{part.Ref}: {part.L:N0}×{part.W:N0}mm", "Found", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }
            }
        }

        // =====================================================
        // SPECIFICATION SELECTION
        // =====================================================

        public void LoadSpecificationsForOptimization(IEnumerable<SpecificationModel> specs)
        {
            _specifications.Clear();
            if (specs != null)
            {
                foreach (var s in specs) _specifications.Add(s);
            }
            if (lstSpecSelect != null)
            {
                lstSpecSelect.ItemsSource = _specifications;
                lstSpecSelect.DisplayMemberPath = "SpecificationName";
            }
            UpdateCombinedPreview();
        }

        private void UpdateCombinedPreview()
        {
            _combinedItems.Clear();
            _selectedSpecs.Clear();

            if (lstSpecSelect == null) { UpdateOptimizableItems(); return; }

            var selectedListBox = lstSpecSelect.SelectedItems;
            if (selectedListBox == null || selectedListBox.Count == 0)
            {
                if (txtSelectedSpecCount != null) txtSelectedSpecCount.Text = "0 selected";
                if (icCombinedPreview != null) icCombinedPreview.ItemsSource = null;
                UpdateOptimizableItems();
                return;
            }

            if (txtSelectedSpecCount != null) txtSelectedSpecCount.Text = $"{selectedListBox.Count} selected";

            bool useW1H1 = rbUseW1H1 != null && rbUseW1H1.IsChecked == true;

            foreach (SpecificationModel spec in selectedListBox)
            {
                if (spec?.Items == null) continue;
                _selectedSpecs.Add(spec);

                foreach (var item in spec.Items)
                {
                    double useWidth = useW1H1 ? item.Width1 : item.Width2;
                    double useHeight = useW1H1 ? item.Height1 : item.Height2;
                    if (useWidth <= 0 || useHeight <= 0) continue;

                    _combinedItems.Add(new CombinedSpecItem
                    {
                        GlassRef = !string.IsNullOrEmpty(item.GlassRef) ? item.GlassRef : spec.SpecificationName,
                        Width = useWidth,
                        Height = useHeight,
                        Qty = item.Qty,
                        SourceSpec = spec.SpecificationName
                    });
                }
            }

            if (icCombinedPreview != null) icCombinedPreview.ItemsSource = _combinedItems;
            UpdateOptimizableItems();
        }

        private void UpdateOptimizableItems()
        {
            _cutParts.Clear();
            if (_combinedItems.Count == 0) return;

            int idx = 1;
            foreach (var item in _combinedItems)
            {
                if (item.Width <= 0 || item.Height <= 0 || item.Qty <= 0) continue;
                _cutParts.Add(new CutPart
                {
                    Ref = item.GlassRef ?? $"P{idx++}",
                    L = item.Width,
                    W = item.Height,
                    Rot = true,
                    Qty = item.Qty
                });
            }
            UpdatePartsSummary();
        }

        private void lstSpecSelect_SelectionChanged(object sender, SelectionChangedEventArgs e) { UpdateCombinedPreview(); }
        private void rbUseW1H1_Checked(object sender, RoutedEventArgs e) { UpdateCombinedPreview(); }
        private void rbUseW2H2_Checked(object sender, RoutedEventArgs e) { UpdateCombinedPreview(); }

        // =====================================================
        // REPORT SECTION
        // =====================================================

        private void UpdateReportSection()
        {
            spReportDetails.Children.Clear();

            var header = new TextBlock { Text = "CUTTING REPORT", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11)), Margin = new Thickness(0, 0, 0, 15) };
            spReportDetails.Children.Add(header);

            var overallBorder = new Border { Background = new SolidColorBrush(Color.FromRgb(30, 59, 100)), Padding = new Thickness(15), CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 0, 0, 15) };
            var overallStack = new StackPanel();

            // FORCE REFRESH: Get fresh values from engine
            var totalSheets = _results.Count(r => r.Ref != "TOTAL");
            var totalParts = _engine.TotalPartsCut;
            var usedSQM = _engine.UsedSQM;
            var avgUtil = _engine.OverallUtilization;
            var waste = _engine.OverallWastage;

            overallStack.Children.Add(new TextBlock { Text = "OVERALL SUMMARY", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 10) });
            overallStack.Children.Add(new TextBlock { Text = $"Total Sheets Used: {totalSheets}", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11)) });
            overallStack.Children.Add(new TextBlock { Text = $"Total Parts Cut: {totalParts}", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246)) });
            overallStack.Children.Add(new TextBlock { Text = $"Glass Area Used: {usedSQM:N2} m²", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)) });
            overallStack.Children.Add(new TextBlock { Text = $"Average Utilization: {avgUtil:N2}%", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(139, 92, 246)) });
            overallStack.Children.Add(new TextBlock { Text = $"Wastage: {_engine.OverallWastage:N2}%", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)) });

            if (_engine.TotalPartsUnplaced > 0)
            {
                overallStack.Children.Add(new TextBlock { Text = $"⚠ Unplaced Parts: {_engine.TotalPartsUnplaced}", FontWeight = FontWeights.Bold, FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(252, 165, 165)), Margin = new Thickness(0, 10, 0, 0) });
            }

            overallBorder.Child = overallStack;
            spReportDetails.Children.Add(overallBorder);

            foreach (var result in _results.Where(r => r.Ref != "TOTAL").OrderBy(r => r.Ref))
            {
                var sheetBorder = new Border { Background = new SolidColorBrush(Color.FromRgb(26, 29, 35)), Padding = new Thickness(12), CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 0, 0, 8) };
                var sheetStack = new StackPanel();
                sheetStack.Children.Add(new TextBlock { Text = $"{result.Ref}: {result.L:N0} × {result.W:N0}mm", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246)) });
                sheetStack.Children.Add(new TextBlock { Text = $"Area: {result.Area:N3} m² | U: {result.Util:N2}% | W: {result.Waste:N2}%", Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)), FontSize = 11 });
                sheetBorder.Child = sheetStack;
                spReportDetails.Children.Add(sheetBorder);
            }

            var totalBorder = new Border { Background = new SolidColorBrush(Color.FromRgb(245, 158, 11)), Padding = new Thickness(15), CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 10, 0, 0) };
            totalBorder.Child = new TextBlock { Text = $"TOTAL: {_results.Count(r => r.Ref != "TOTAL")} sheets | {_engine.TotalPartsCut} parts | {_engine.UsedSQM:N2} m²", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
            spReportDetails.Children.Add(totalBorder);

            var totalCost = _services.CalculateCost(_engine.UsedSQM, _results.ToList(), _engine.GetRemnants());
            txtTotalCost.Text = $"AED {totalCost:N2}";
            txtRemnants.Text = $"{_engine.GetRemnants().Count} remnants available";
        }

        // =====================================================
        // RESULTS GROUPING
        // =====================================================

        private void UpdateResultsGrouping()
        {
            // Create temporary list instead of using Items while ItemsSource is set
            var groupedResults = new List<OptimizationResult>();

            var grouped = _results.Where(r => r.Ref != "TOTAL").GroupBy(r => new { r.L, r.W, r.SheetRef })
                .Select(g => new { Size = $"{g.Key.L:N0}×{g.Key.W:N0}", Ref = g.Key.SheetRef, Count = g.Count(), TotalArea = g.Sum(x => x.Area), AvgUtil = g.Average(x => x.Util) })
                .OrderByDescending(x => x.TotalArea).ToList();

            foreach (var g in grouped)
            {
                groupedResults.Add(new OptimizationResult { Ref = $"{g.Ref} ({g.Count}x)", L = 0, W = 0, Used = g.Count, Area = g.TotalArea, Util = g.AvgUtil, Waste = 100 - g.AvgUtil });
            }

            // Assign new list to ItemsSource
            icResults.ItemsSource = groupedResults;
        }

        // =====================================================
        // SETTINGS
        // =====================================================

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            double.TryParse(txtLR.Text, out _lr);
            double.TryParse(txtBR.Text, out _br);
            double.TryParse(txtTR.Text, out _tr);
            double.TryParse(txtRM.Text, out _rm);
            double.TryParse(txtKerf.Text, out _kerf);
            double.TryParse(txtBreakout.Text, out _breakout);

            // FIX: Validate rotation selection
            _rotationPolicy = RotationPolicy.BestFit; // default
            if (cmbRotation != null && cmbRotation.SelectedIndex >= 0)
            {
                _rotationPolicy = (RotationPolicy)cmbRotation.SelectedIndex;
            }

            _engine.Configure(_lr, _rm, _tr, _br, _kerf, _breakout);
            _engine.SetRotationPolicy(_rotationPolicy);
            MessageBox.Show("Settings saved.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // REMOVED: ResetSettings_Click (used txtBridgeWidth which doesn't exist in XAML)

        // =====================================================
        // KEYBOARD SHORTCUTS
        // =====================================================

        private void OptimizationView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5) RunOptimization_Click(sender, e);
            else if (e.Key == Key.Escape && _isSimulating) { _isSimulating = false; _simulateTimer?.Stop(); btnSimulate.Content = "Start Simulation"; }
            else if (e.Key == Key.Add && Keyboard.Modifiers == ModifierKeys.Control) ZoomIn_Click(sender, e);
            else if (e.Key == Key.Subtract && Keyboard.Modifiers == ModifierKeys.Control) ZoomOut_Click(sender, e);
        }

        private void LayoutCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                _zoomLevel = e.Delta > 0 ? Math.Min(_zoomLevel + 0.05, 2.5) : Math.Max(_zoomLevel - 0.05, 0.3);
                txtZoom.Text = $"{(_zoomLevel * 100):N0}%";
                DrawSingleSheetLayout(_currentIndex);
                e.Handled = true;
            }
        }

        // =====================================================
        // VALIDATION
        // =====================================================

        private bool ValidateInputs()
        {
            if (_stockSheets.Count == 0) { MessageBox.Show("Please add stock sheets.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
            if (_cutParts.Count == 0) { MessageBox.Show("Please add parts.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
            if (_stockSheets.Sum(s => s.Qty) < _cutParts.Sum(p => p.Qty)) MessageBox.Show("Warning: Not enough stock for all parts.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return true;
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private string FormatSize(double mm) => mm >= 1000 ? $"{mm / 1000:N1}m" : $"{mm:N0}mm";
        private string FormatArea(double sqm) => sqm >= 1 ? $"{sqm:N2} m²" : $"{sqm * 10000:N0} cm²";

        // =====================================================
        // PRINT LAYOUTS
        // =====================================================

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
                var border = new Border
                {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(15),
                    Margin = new Thickness(0, 0, 0, 15),
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252))
                };
                border.Child = new TextBlock
                {
                    Text = $"{r.Ref}: {r.L:N0} × {r.W:N0} mm\nUtilization: {r.Util:N2}%  |  Wastage: {r.Waste:N2}%",
                    FontSize = 13
                };
                stack.Children.Add(border);
            }

            var totalBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 10, 0, 0)
            };
            totalBorder.Child = new TextBlock
            {
                Text = $"TOTAL: {_results.Count(r => r.Ref != "TOTAL")} sheets used",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White)
            };
            stack.Children.Add(totalBorder);

            scroll.Content = stack;
            printWindow.Content = scroll;
            printWindow.ShowDialog();
        }

        // =====================================================
        // PRINT LABELS
        // =====================================================

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
                var border = new Border
                {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 10)
                };
                border.Child = new TextBlock
                {
                    Text = $"ID: {part.Ref}\nSize: {part.L:N0} x {part.W:N0}mm\nSheet: {part.Sheet}-{part.SheetNum}",
                    FontSize = 12
                };
                stack.Children.Add(border);
            }

            if (_allPlacedParts.Count > 50)
            {
                stack.Children.Add(new TextBlock { Text = $"... and {_allPlacedParts.Count - 50} more", FontSize = 10, Foreground = new SolidColorBrush(Colors.Gray) });
            }

            scroll.Content = stack;
            printWindow.Content = scroll;
            printWindow.ShowDialog();
        }

        // =====================================================
        // COST CALCULATION (Local - uses Services)
        // =====================================================

        public double CalculateCost(double usedSQM, List<OptimizationResult> results, List<RemnantPiece> remnants)
        {
            if (usedSQM <= 0) return 0;

            double stockCost = usedSQM * 250;

            double wasteArea = 0;
            foreach (var r in results.Where(x => x.Ref != "TOTAL"))
            {
                double sheetArea = r.L * r.W / 1000000.0;
                wasteArea += sheetArea - r.Area;
            }
            stockCost += wasteArea * 50 / 1000;

            double remnantCredit = 0;
            foreach (var rem in remnants.Where(x => !x.IsReused))
            {
                remnantCredit += (rem.Area / 1000000) * 25;
            }
            stockCost -= remnantCredit;

            return stockCost;
        }

        // =====================================================
        // SIMULATION MODE
        // =====================================================

        private void StartSimulation_Click(object sender, RoutedEventArgs e)
        {
            if (_isSimulating)
            {
                _isSimulating = false;
                _simulateTimer?.Stop();
                btnSimulate.Content = "Start Simulation";
                return;
            }

            if (_stockSheets.Count == 0 || _cutParts.Count == 0)
            {
                MessageBox.Show("Add stock and parts first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _isSimulating = true;
            btnSimulate.Content = "Stop Simulation";

            _simulateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _simulateTimer.Tick += SimulateTimer_Tick;
            _simulateTimer.Start();
        }

        private void SimulateTimer_Tick(object sender, EventArgs e)
        {
            if (!_isSimulating) return;

            var unplaced = _cutParts.Where(p => !p.IsPlaced).ToList();
            if (unplaced.Count == 0)
            {
                _simulateTimer?.Stop();
                _isSimulating = false;
                btnSimulate.Content = "Start Simulation";
                MessageBox.Show("Simulation complete!", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DrawSingleSheetLayout(_currentIndex);

            if (_currentIndex < _results.Count - 1)
                _currentIndex++;
            else
                _currentIndex = 0;
        }

        // =====================================================
        // INVOICE INTEGRATION METHODS - ADD THESE
        // =====================================================

        public int SheetsUsed => _results.Count(r => r.Ref != "TOTAL");

        public double AverageUtilization => _engine.OverallUtilization;

        public void SetStockSheet(StockSheet sheet)
        {
            _stockSheets.Add(sheet);
            UpdateStockSummary();
        }

        public void SetTrimSettings(double lr, double br, double tr, double rm, double kerf, double breakout)
        {
            _lr = lr;
            _br = br;
            _tr = tr;
            _rm = rm;
            _kerf = kerf;
            _breakout = breakout;

            _engine.Configure(_lr, _rm, _tr, _br, _kerf, _breakout);

            // ═══════════════════════════════════════════════════════════════════
            // UPDATE TEXTBOXES IN SETTINGS PANEL (When called from ProformaInvoice)
            // ═══════════════════════════════════════════════════════════════════
            try
            {
                if (txtLR != null) txtLR.Text = _lr.ToString();
                if (txtRM != null) txtRM.Text = _rm.ToString();
                if (txtTR != null) txtTR.Text = _tr.ToString();
                if (txtBR != null) txtBR.Text = _br.ToString();
                if (txtKerf != null) txtKerf.Text = _kerf.ToString();
                if (txtBreakout != null) txtBreakout.Text = _breakout.ToString();

                System.Diagnostics.Debug.WriteLine($"[OptimizationView] SetTrimSettings applied: L={_lr}, R={_rm}, T={_tr}, B={_br}, Kerf={_kerf}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OptimizationView] SetTrimSettings Error: {ex.Message}");
            }
        }

        public void ImportInvoiceItems(List<CutPart> items)
        {
            _cutParts.Clear();
            foreach (var item in items)
            {
                _cutParts.Add(item);
            }
            UpdatePartsSummary();
        }

        public void RunOptimizationFromInvoice()
        {
            if (_stockSheets.Count == 0 || _cutParts.Count == 0)
            {
                MessageBox.Show("Please add stock sheets and parts first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _engine.Configure(_lr, _rm, _tr, _br, _kerf, _breakout);
            _engine.SetRotationPolicy(_rotationPolicy);

            try
            {
                _engine.ExecuteNesting(_stockSheets.ToList(), _cutParts.ToList(), _results, _allPlacedParts, _savedJobs);
                UpdateReportSection();
                UpdateResultsGrouping();

                // ADDED: Draw layouts after optimization (THIS WAS MISSING!)
                ShowTab("Layouts");
                _currentIndex = 0;
                cmbSheetSelector.SelectedIndex = 0;
                DrawCurrentLayout(_currentIndex);
                DrawSingleSheetLayout(_currentIndex);
                UpdateLayoutCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =====================================================
        // INVOICE INTEGRATION - MISSING METHODS
        // =====================================================

        // For ProformaInvoice: SetStockSheet(width, height) - 2 arguments
        public void SetStockSheet(double width, double height)
        {
            int idx = _stockSheets.Count + 1;
            _stockSheets.Add(new StockSheet { Ref = $"S{idx}", L = width, W = height, Qty = 9999999 });
            UpdateStockSummary();
        }

        // For ProformaInvoice: ImportInvoiceItems(List<InvoiceItemModel>)  
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
            UpdatePartsSummary();
        }

        // =====================================================
        // CANVAS CONTEXT MENU
        // =====================================================

        private void LayoutCanvas_MouseRightClick(object sender, MouseButtonEventArgs e)
        {
            var contextMenu = new ContextMenu();

            var copyItem = new MenuItem { Header = "Copy Layout Image" };
            copyItem.Click += (s, args) => { /* Copy to clipboard logic */ };
            contextMenu.Items.Add(copyItem);

            var exportItem = new MenuItem { Header = "Export Layout PNG" };
            exportItem.Click += (s, args) => { /* Export PNG logic */ };
            contextMenu.Items.Add(exportItem);

            contextMenu.Items.Add(new Separator());

            var closeItem = new MenuItem { Header = "Close" };
            closeItem.Click += (s, args) => { };
            contextMenu.Items.Add(closeItem);

            contextMenu.IsOpen = true;
        }
    }
}