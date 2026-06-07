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
    // =====================================================
    // ROTATION POLICIES
    // =====================================================

    public enum RotationPolicy
    {
        None = 0,
        Rotate90 = 1,
        BestFit = 2,
        FirstFit = 3,
        StripFill = 4,
        ColumnFill = 5,
        RowFill = 6
    }

    // =====================================================
    // NESTING STRATEGIES (PATCH 2)
    // =====================================================

    public enum NestingStrategy
    {
        BestArea,        // BSSF - Best Short Side Fit
        ShortSideFit,    // SSF
        LongSideFit,     // LSF  
        Guillotine,      // Guillotine split
        Skyline,        // Skyline algorithm
        BottomLeft,      // BL - Bottom Left
        BestPerimeter   // BPF - Best Perimeter Fit
    }

    public partial class OptimizationView : UserControl
    {
        // =====================================================
        // MAIN DATA COLLECTIONS
        // =====================================================

        private ObservableCollection<StockSheet> _stockSheets = new ObservableCollection<StockSheet>();
        private ObservableCollection<CutPart> _cutParts = new ObservableCollection<CutPart>();
        private ObservableCollection<OptimizationResult> _results = new ObservableCollection<OptimizationResult>();
        private List<PlacedPart> _allPlacedParts = new List<PlacedPart>();

        // =====================================================
        // UI STATE
        // =====================================================

        private double _zoomLevel = 1.0;
        private int _currentIndex = 0;
        private int _sheetsPerPage = 6;

        // =====================================================
        // TRIM & CUT SETTINGS
        // =====================================================

        private double _lr = 15, _br = 15, _tr = 15, _rm = 15, _kerf = 4.0, _breakout = 4.0;
        private RotationPolicy _rotationPolicy = RotationPolicy.BestFit;
        private double _bridgeWidth = 15;

        // =====================================================
        // OPTIMIZATION RESULTS
        // =====================================================

        private double _overallUtilization = 0;
        private double _overallWastage = 0;
        private int _totalPartsCut = 0;
        private int _totalPartsUnplaced = 0;
        private double _usedSQM = 0;

        // =====================================================
        // ADVANCED FEATURES (PATCHES 1-10)
        // =====================================================

        private List<MaxRect> _freeRects = new List<MaxRect>();           // PATCH 1: MaxRects engine
        private List<RemnantPiece> _remnants = new List<RemnantPiece>(); // PATCH 5,9: Remnants
        private ObservableCollection<OptimizationJob> _savedJobs = new ObservableCollection<OptimizationJob>();
        private CostModel _costModel = new CostModel();                      // PATCH 6: Cost model
        private PlacementConstraint _constraints = new PlacementConstraint(); // PATCH 8: Constraints

        private NestingStrategy _nestingStrategy = NestingStrategy.BestArea; // PATCH 2: Strategy
        private bool _multiStrategyMode = false;                            // PATCH 2: Multi-strategy
        private bool _lookaheadEnabled = true;                             // PATCH 3: Lookahead
        private bool _twoPassEnabled = true;                               // PATCH 4: Two-pass
        private bool _remnantReuseEnabled = true;                         // PATCH 5: Remnant reuse

        private List<CutSequence> _cutSequences = new List<CutSequence>(); // PATCH 10: Digital twin
        private Dictionary<string, double> _stockPrices = new Dictionary<string, double>();
        private int _highPriorityParts = 0;
        private bool _multiStockMode = false;

        private DispatcherTimer _simulateTimer;
        private int _simulateStep = 0;
        private List<CutOperation> _cutOperations = new List<CutOperation>();

        // =====================================================
        // CONSTRUCTOR
        // =====================================================

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

            // Initialize default cost settings (PATCH 6)
            _costModel.StockPricePerSQM = 250;
            _costModel.WastePenaltyPerSQM = 50;
            _costModel.RemnantCreditPerSQM = 25;
            _costModel.KerfCostPerMm = 0.01;
            _costModel.OperatingCostPerHour = 150;
            _costModel.SetupCostPerJob = 50;

            // Initialize default constraints (PATCH 8)
            _constraints.MinPartSize = 50;
            _constraints.SafetyMarginX = 0;
            _constraints.SafetyMarginY = 0;
            _constraints.KerfCompensation = 4.0;
            _constraints.MinRemnantSize = 100;
            _constraints.AllowRotation = true;
            _constraints.MaxAspectRatio = 10.0;
        }

        // =====================================================
        // TAB HANDLERS
        // =====================================================

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

        // =====================================================
        // STOCK MANAGEMENT
        // =====================================================

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

        // =====================================================
        // PARTS MANAGEMENT
        // =====================================================

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

        // =====================================================
        // OPTIMIZATION ENTRY POINT
        // =====================================================

        private void RunOptimization_Click(object sender, RoutedEventArgs e)
        {
            // Load settings from UI
            double.TryParse(txtLR.Text, out _lr); double.TryParse(txtBR.Text, out _br);
            double.TryParse(txtTR.Text, out _tr); double.TryParse(txtRM.Text, out _rm);
            double.TryParse(txtKerf.Text, out _kerf); double.TryParse(txtBreakout.Text, out _breakout);

            if (cmbRotation != null)
                _rotationPolicy = (RotationPolicy)cmbRotation.SelectedIndex;

            // Validate input
            if (_stockSheets.Count == 0 || _cutParts.Count == 0)
            {
                MessageBox.Show("Please add stock sheets and parts first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Run the advanced nesting algorithm with all patches
                RunAdvancedNesting(_kerf, _rotationPolicy);

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

        // =====================================================
        // PATCH 1-10: ADVANCED NESTING ALGORITHM
        // =====================================================

        private void RunAdvancedNesting(double kerf, RotationPolicy rotationPolicy)
        {
            // Clear all results
            _results.Clear();
            _allPlacedParts.Clear();
            _remnants.Clear();
            _cutOperations.Clear();
            _cutSequences.Clear();
            _freeRects.Clear();

            // Expand parts by quantity
            var allParts = new List<CutPart>();
            int partId = 1;
            foreach (var p in _cutParts)
            {
                for (int i = 0; i < p.Qty; i++)
                {
                    allParts.Add(new CutPart
                    {
                        Id = partId++,
                        Ref = p.Ref,
                        L = p.L,
                        W = p.W,
                        Rot = p.Rot,
                        Qty = 1,
                        IsPlaced = false
                    });
                }
            }

            // Sort parts by area (largest first) for optimal nesting
            allParts = allParts.OrderByDescending(x => x.L * x.W)
                               .ThenByDescending(x => Math.Max(x.L, x.W))
                               .ToList();

            var sortedStock = _stockSheets.OrderByDescending(s => s.L * s.W).ToList();

            // Statistics
            double totalUsedAreaAll = 0, totalAreaUsedSheets = 0;
            int totalSheetsUsed = 0;
            _totalPartsCut = 0;
            _totalPartsUnplaced = 0;
            _usedSQM = 0;

            // PATCH 2: Multi-Strategy Selection
            // Test strategies and select the best one
            if (_multiStrategyMode)
            {
                _nestingStrategy = SelectBestStrategy(allParts, sortedStock[0], kerf);
            }

            // Process each stock type
            foreach (var stock in sortedStock)
            {
                double oneSheetArea = stock.L * stock.W / 1000000.0;
                int availableQty = stock.Qty;
                if (availableQty <= 0) continue;

                // Calculate usable area after trim margins
                double usableW = stock.L - _lr - _rm;
                double usableH = stock.W - _tr - _br;

                if (usableW < _breakout || usableH < _breakout) continue;

                int sheetsUsedForThisStock = 0;

                // Process each sheet
                for (int sheetNum = 0; sheetNum < availableQty; sheetNum++)
                {
                    var remaining = allParts.Where(p => !p.IsPlaced).ToList();
                    if (remaining.Count == 0) break;

                    // PATCH 1: Initialize MaxRects instead of wasteRects
                    _freeRects.Clear();
                    _freeRects.Add(new MaxRect(0, 0, usableW, usableH));

                    double usedAreaThisSheet = 0;
                    int placedOnThisSheet = 0;
                    var placedOnThisSheetList = new List<PlacedPart>();
                    var sheetCuts = new List<CutOperation>();

                    // PATCH 10: Create cut sequence for digital twin
                    var cutSequence = new CutSequence
                    {
                        SheetId = $"{stock.Ref}-{sheetNum + 1}"
                    };

                    // PATCH 3: Place parts with lookahead scoring
                    foreach (var part in remaining)
                    {
                        if (part.IsPlaced) continue;

                        // PATCH 8: Validate constraints before placement
                        if (!_constraints.ValidatePlacement(part.L, part.W))
                            continue;

                        // PATCH 7: Hybrid rotation - evaluate both orientations
                        var placement = EvaluatePlacementWithLookahead(part, kerf);

                        if (placement == null) continue;

                        // Commit placement
                        part.IsPlaced = true;
                        part.PlacedX = placement.X;
                        part.PlacedY = placement.Y;
                        part.PlacedW = placement.IsRotated ? part.W : part.L;
                        part.PlacedH = placement.IsRotated ? part.L : part.W;

                        placedOnThisSheetList.Add(new PlacedPart
                        {
                            Ref = part.Ref,
                            X = part.PlacedX + _lr,
                            Y = part.PlacedY + _tr,
                            L = part.PlacedW,
                            W = part.PlacedH,
                            IsRotated = placement.IsRotated,
                            Sheet = stock.Ref,
                            SheetNum = sheetNum + 1
                        });

                        // In the placement loop: stop accumulating usedAreaThisSheet per-part (compute per-sheet later)
                        double partArea = (part.PlacedW * part.PlacedH) / 1000000.0;
                        _usedSQM += partArea;
                        placedOnThisSheet++;
                        _totalPartsCut++;

                        // Update free rects (PATCH 1: Guillotine split)
                        UpdateFreeRectsWithGuillotine(placement, part.PlacedW + kerf, part.PlacedH + kerf);

                        // PATCH 10: Generate toolpath operations
                        GenerateToolpathForPart(part, cutSequence, placement.X + _lr, placement.Y + _tr, kerf);
                    }

                    // PATCH 5: Track usable remnants for reuse
                    if (_remnantReuseEnabled)
                    {
                        CollectRemnants(stock.Ref, sheetNum + 1);
                    }

                    // Record sheet result
                    if (placedOnThisSheet > 0)
                    {
                        // Recalculate used area from placed parts to avoid accumulation errors
                        usedAreaThisSheet = placedOnThisSheetList.Sum(p => (p.L * p.W) / 1000000.0);

                        sheetsUsedForThisStock++;
                        totalSheetsUsed++;
                        totalAreaUsedSheets += oneSheetArea;

                        // Calculate utilization for this sheet (used area vs full sheet area)
                        double thisSheetUtil = oneSheetArea > 0 ? (usedAreaThisSheet / oneSheetArea) * 100.0 : 0.0;

                        // Detect overlapping placed parts (simple axis-aligned check)
                        bool overlapDetected = false;
                        for (int a = 0; a < placedOnThisSheetList.Count; a++)
                        {
                            var pa = placedOnThisSheetList[a];
                            var ax1 = pa.X; var ay1 = pa.Y; var ax2 = pa.X + pa.L; var ay2 = pa.Y + pa.W;
                            for (int b = a + 1; b < placedOnThisSheetList.Count; b++)
                            {
                                var pb = placedOnThisSheetList[b];
                                var bx1 = pb.X; var by1 = pb.Y; var bx2 = pb.X + pb.L; var by2 = pb.Y + pb.W;
                                bool intersects = !(ax2 <= bx1 || bx2 <= ax1 || ay2 <= by1 || by2 <= ay1);
                                if (intersects)
                                {
                                    overlapDetected = true;
                                    System.Diagnostics.Debug.WriteLine($"OVERLAP detected on sheet {stock.Ref}-{sheetsUsedForThisStock}: {pa.Ref}@({pa.X},{pa.Y},{pa.L},{pa.W}) vs {pb.Ref}@({pb.X},{pb.Y},{pb.L},{pb.W})");
                                }
                            }
                        }

                        // If used area exceeds sheet area due to overlapping or rounding, clamp and warn
                        if (thisSheetUtil > 100.0 || overlapDetected)
                        {
                            System.Diagnostics.Debug.WriteLine($"WARNING: Sheet utilization >100% or overlap on {stock.Ref}-{sheetsUsedForThisStock}. Clamping values.");
                            // Clamp the used area to the full sheet area
                            usedAreaThisSheet = Math.Min(usedAreaThisSheet, oneSheetArea);
                            thisSheetUtil = oneSheetArea > 0 ? (usedAreaThisSheet / oneSheetArea) * 100.0 : 0.0;
                        }

                        // DEBUG: Show actual values
                        System.Diagnostics.Debug.WriteLine($"Sheet {sheetsUsedForThisStock}: Used={usedAreaThisSheet.ToString("F4")} m², Sheet={oneSheetArea.ToString("F4")} m², Util={thisSheetUtil.ToString("F2")}%");

                        _results.Add(new OptimizationResult
                        {
                            Ref = $"{stock.Ref}-{sheetsUsedForThisStock}",
                            SheetRef = stock.Ref,
                            SheetNum = sheetsUsedForThisStock,
                            L = stock.L,
                            W = stock.W,
                            Used = 1,
                            Area = usedAreaThisSheet,
                            Util = thisSheetUtil,
                            Waste = 100 - thisSheetUtil
                        });

                        // Add used area to totals AFTER clamping
                        totalUsedAreaAll += usedAreaThisSheet;

                        _allPlacedParts.AddRange(placedOnThisSheetList);
                        _cutOperations.AddRange(sheetCuts);

                        // Add cut sequence
                        cutSequence.TotalKerfLength = cutSequence.Operations.Sum(o => o.Length);
                        cutSequence.EstimatedTime = cutSequence.TotalKerfLength / 5000; // Rough time estimate
                        _cutSequences.Add(cutSequence);
                    }

                    // Continue to next sheet if no parts placed this sheet
                    if (placedOnThisSheet == 0) break;
                }
            }

            // PATCH 4: Second pass - just for placing additional parts, not for utilization calculation
            // (parts on remnants don't add new sheet area, utilization already calculated per-sheet above)
            double areaFromRemnants = 0;
            if (_twoPassEnabled && _remnants.Count > 0)
            {
                var unplacedParts = allParts.Where(p => !p.IsPlaced).ToList();
                if (unplacedParts.Count > 0)
                {
                    areaFromRemnants = RunSecondPassNesting(unplacedParts);
                }
            }

            // Update statistics
            _totalPartsUnplaced = allParts.Count(p => !p.IsPlaced);

            // Calculate utilization - SUM each sheet's FULL area individually (handles different stock sizes)
            var sheetResults = _results.Where(r => r.Ref != "TOTAL").ToList();
            if (sheetResults.Count > 0)
            {
                double totalPartArea = sheetResults.Sum(r => r.Area);

                // FIX: Calculate each sheet area and SUM them (handles mixed stock sizes correctly)
                double totalFullArea = sheetResults.Sum(r => (r.L * r.W) / 1000000.0);

                // DEBUG - Show actual values
                System.Diagnostics.Debug.WriteLine($"DEBUG: {sheetResults.Count} sheets, PartArea={totalPartArea:F4} m², FullArea={totalFullArea:F4} m²");

                _overallUtilization = totalFullArea > 0 ? (totalPartArea / totalFullArea) * 100 : 0;
                _overallWastage = 100 - _overallUtilization;

                // Cap utilization to valid range
                if (_overallUtilization > 100) _overallUtilization = 100;
                if (_overallUtilization < 0) _overallUtilization = 0;
                _overallWastage = 100 - _overallUtilization;
            }
            else
            {
                _overallUtilization = 0;
                _overallWastage = 100;
            }

            // DEBUG: Show unplaced parts info
            var unplaced = allParts.Where(p => !p.IsPlaced).ToList();
            if (unplaced.Count > 0)
            {
                string unplacedInfo = string.Join(", ", unplaced.Take(10).Select(p => $"{p.L}x{p.W}"));
                System.Diagnostics.Debug.WriteLine($"UNPLACED ({unplaced.Count}): {unplacedInfo}");
            }

            int totalAvailable = _stockSheets.Sum(s => s.Qty);

            // FIX: Final clamp before showing results
            if (_overallUtilization > 100) _overallUtilization = 100;
            if (_overallUtilization < 0) _overallUtilization = 0;
            _overallWastage = 100 - _overallUtilization;

            _results.Add(new OptimizationResult { Ref = "TOTAL", L = 0, W = 0, Used = totalSheetsUsed, Area = totalUsedAreaAll, Util = _overallUtilization, Waste = _overallWastage });

            // PATCH 4: Third pass - DISABLED
            // Don't force place parts that don't fit - leave them as unplaced
            // This maintains accurate utilization calculation
            var stillUnplaced = allParts.Where(p => !p.IsPlaced).ToList();
            if (stillUnplaced.Count > 0)
            {
                // Just mark remaining as unplaced - DO NOT force place
                // Utilization should only count properly placed parts
                System.Diagnostics.Debug.WriteLine($"Third pass SKIPPED: {stillUnplaced.Count} parts left unplaced");
            }

            _totalPartsUnplaced = allParts.Count(p => !p.IsPlaced);

            // Don't group - show all individual sheets
            icResults.ItemsSource = _results.Where(r => r.Ref != "TOTAL").ToList();

            // Update UI
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

        // =====================================================
        // PATCH 2: STRATEGY SELECTION
        // =====================================================

        private NestingStrategy SelectBestStrategy(List<CutPart> parts, StockSheet stock, double kerf)
        {
            var strategies = new[]
            {
                NestingStrategy.BestArea,
                NestingStrategy.Guillotine,
                NestingStrategy.ShortSideFit,
                NestingStrategy.BottomLeft
            };

            NestingStrategy bestStrategy = NestingStrategy.BestArea;
            double bestUtil = 0;

            // Test with subset of parts
            var testParts = parts.Take(15).ToList();

            foreach (var strat in strategies)
            {
                var testRects = new List<MaxRect>();
                testRects.Add(new MaxRect(0, 0, stock.L - _lr - _rm, stock.W - _tr - _br));

                double testUsed = 0;
                var testPartsCopy = testParts.Select(p => new CutPart
                {
                    Id = p.Id,
                    Ref = p.Ref,
                    L = p.L,
                    W = p.W,
                    Rot = p.Rot
                }).ToList();

                foreach (var p in testPartsCopy)
                {
                    var placement = FindBestMaxRect(p.L + kerf, p.W + kerf, p.Rot, testRects, strat);
                    if (placement != null)
                    {
                        testUsed += p.L * p.W;
                        // Update test rects
                        UpdateFreeRectsTest(placement, p.L + kerf, p.W + kerf, testRects);
                    }
                }

                double util = (testUsed / (stock.L * stock.W)) * 100;
                if (util > bestUtil)
                {
                    bestUtil = util;
                    bestStrategy = strat;
                }
            }

            return bestStrategy;
        }

        // =====================================================
        // PATCH 3: LOOKAHEAD PLACEMENT EVALUATION
        // =====================================================

        private PlacementNode EvaluatePlacementWithLookahead(CutPart part, double kerf)
        {
            double pW = part.L + kerf;
            double pH = part.W + kerf;
            double pWRot = part.W + kerf;
            double pHRot = part.L + kerf;

            // Find any available space - try normal first
            var normalCandidates = FindMatchingMaxRects(pW, pH, _freeRects);

            if (normalCandidates.Count > 0)
            {
                // Take the first available spot
                var rect = normalCandidates.First();
                return new PlacementNode
                {
                    X = rect.X,
                    Y = rect.Y,
                    Width = part.L,
                    Height = part.W,
                    IsRotated = false,
                    Score = 0
                };
            }

            // Try rotated if allowed
            if (part.Rot)
            {
                var rotatedCandidates = FindMatchingMaxRects(pWRot, pHRot, _freeRects);
                if (rotatedCandidates.Count > 0)
                {
                    var rect = rotatedCandidates.First();
                    return new PlacementNode
                    {
                        X = rect.X,
                        Y = rect.Y,
                        Width = part.W,
                        Height = part.L,
                        IsRotated = true,
                        Score = 0
                    };
                }
            }

            return null;
        }

        private double ScoreLookahead(List<MaxRect> freeRects, CutPart currentPart)
        {
            // Simple heuristic: prefer placements that leave larger usable areas
            return freeRects.Sum(r => r.Area);
        }

        // =====================================================
        // PATCH 1: MAXRECTS CORE OPERATIONS
        // =====================================================

        private List<MaxRect> FindMatchingMaxRects(double w, double h, List<MaxRect> freeRects)
        {
            return freeRects.Where(r => r.Fits(w, h)).OrderBy(r => r.Area).ToList();
        }

        private MaxRect FindBestMaxRect(double w, double h, bool canRotate, List<MaxRect> freeRects, NestingStrategy strategy)
        {
            var matches = FindMatchingMaxRects(w, h, freeRects);
            if (matches.Count > 0)
            {
                return matches.OrderBy(r => ScorePlacement(r, w, h, strategy)).First();
            }
            return null;
        }

        private double ScorePlacement(MaxRect rect, double w, double h, NestingStrategy strategy)
        {
            double score = 0;

            switch (strategy)
            {
                case NestingStrategy.BestArea:
                case NestingStrategy.ShortSideFit:
                    score = rect.Area - (w * h);
                    double shortSide = Math.Min(rect.Width - w, rect.Height - h);
                    score += shortSide * 0.1;
                    break;

                case NestingStrategy.LongSideFit:
                    double leftover = Math.Max(rect.Width - w, rect.Height - h);
                    score = leftover * (rect.Width + rect.Height);
                    break;

                case NestingStrategy.BestPerimeter:
                    score = (rect.Width + rect.Height) - (w + h);
                    break;

                case NestingStrategy.BottomLeft:
                    score = (rect.Y + rect.X) * 1000;
                    break;

                case NestingStrategy.Skyline:
                    score = rect.Y * 1000 + rect.X;
                    break;

                case NestingStrategy.Guillotine:
                    double vertSplit = Math.Abs(rect.Width - w);
                    double horizSplit = Math.Abs(rect.Height - h);
                    score = Math.Min(vertSplit, horizSplit);
                    break;
            }

            return score;
        }

        private void UpdateFreeRectsWithGuillotine(PlacementNode placement, double w, double h)
        {
            // Find the free rect that contains the placement area (allow small epsilon)
            const double eps = 0.01;
            var usedRect = _freeRects.FirstOrDefault(r =>
                placement.X + eps >= r.X && placement.Y + eps >= r.Y &&
                placement.X + w <= r.X + r.Width + eps && placement.Y + h <= r.Y + r.Height + eps);

            if (usedRect == null) return;

            _freeRects.Remove(usedRect);

            double rightW = usedRect.Width - w;
            double bottomH = usedRect.Height - h;

            // Create remaining rectangles (guillotine split)
            if (rightW > _constraints.MinRemnantSize)
            {
                _freeRects.Add(new MaxRect(placement.X + w, placement.Y, rightW, usedRect.Height));
            }

            if (bottomH > _constraints.MinRemnantSize)
            {
                // bottom remaining rect spans the full width of the usedRect
                _freeRects.Add(new MaxRect(placement.X, placement.Y + h, usedRect.Width, bottomH));
            }

            // Merge adjacent free rects and prune any contained rects
            MergeFreeRects();
            PruneContainedFreeRects(_freeRects);
        }

        private void UpdateFreeRectsTest(MaxRect placement, double w, double h, List<MaxRect> freeRects)
        {
            const double eps = 0.01;
            var usedRect = freeRects.FirstOrDefault(r =>
                placement.X + eps >= r.X && placement.Y + eps >= r.Y &&
                placement.X + w <= r.X + r.Width + eps && placement.Y + h <= r.Y + r.Height + eps);

            if (usedRect == null) return;

            freeRects.Remove(usedRect);

            double rightW = usedRect.Width - w;
            double bottomH = usedRect.Height - h;

            if (rightW > _constraints.MinRemnantSize)
                freeRects.Add(new MaxRect(placement.X + w, placement.Y, rightW, usedRect.Height));

            if (bottomH > _constraints.MinRemnantSize)
                freeRects.Add(new MaxRect(placement.X, placement.Y + h, usedRect.Width, bottomH));

            // Do not add overlapping bottom-right rect; right and bottom rects cover remaining space
            PruneContainedFreeRects(freeRects);
        }

        private void SimulatePlacement(MaxRect rect, double w, double h, bool rotated, List<MaxRect> freeRects)
        {
            const double eps = 0.01;
            var used = freeRects.FirstOrDefault(r =>
                rect.X + eps >= r.X && rect.Y + eps >= r.Y &&
                rect.X + w <= r.X + r.Width + eps && rect.Y + h <= r.Y + r.Height + eps);

            if (used == null) return;

            freeRects.Remove(used);

            double rightW = used.Width - w;
            double bottomH = used.Height - h;

            if (rightW > _constraints.MinRemnantSize)
                freeRects.Add(new MaxRect(rect.X + w, rect.Y, rightW, used.Height));

            if (bottomH > _constraints.MinRemnantSize)
                freeRects.Add(new MaxRect(rect.X, rect.Y + h, used.Width, bottomH));

            // Do not add overlapping bottom-right rect; right and bottom rects cover remaining space
            PruneContainedFreeRects(freeRects);
        }

        private void MergeFreeRects()
        {
            bool merged;
            do
            {
                merged = false;
                for (int i = 0; i < _freeRects.Count && !merged; i++)
                {
                    for (int j = i + 1; j < _freeRects.Count && !merged; j++)
                    {
                        var a = _freeRects[i];
                        var b = _freeRects[j];

                        // Horizontal merge
                        if (Math.Abs(a.Y - b.Y) < 0.01 && Math.Abs(a.Height - b.Height) < 0.01)
                        {
                            if (Math.Abs(a.X + a.Width - b.X) < 0.01)
                            {
                                _freeRects[i] = new MaxRect(a.X, a.Y, a.Width + b.Width, a.Height);
                                _freeRects.RemoveAt(j);
                                merged = true;
                            }
                            else if (Math.Abs(b.X + b.Width - a.X) < 0.01)
                            {
                                _freeRects[i] = new MaxRect(b.X, b.Y, a.Width + b.Width, b.Height);
                                _freeRects.RemoveAt(j);
                                merged = true;
                            }
                        }
                        // Vertical merge
                        else if (Math.Abs(a.X - b.X) < 0.01 && Math.Abs(a.Width - b.Width) < 0.01)
                        {
                            if (Math.Abs(a.Y + a.Height - b.Y) < 0.01)
                            {
                                _freeRects[i] = new MaxRect(a.X, a.Y, a.Width, a.Height + b.Height);
                                _freeRects.RemoveAt(j);
                                merged = true;
                            }
                            else if (Math.Abs(b.Y + b.Height - a.Y) < 0.01)
                            {
                                _freeRects[i] = new MaxRect(b.X, b.Y, b.Width, a.Height + b.Height);
                                _freeRects.RemoveAt(j);
                                merged = true;
                            }
                        }
                    }
                }
            } while (merged);
            // After merging, prune contained rectangles
            PruneContainedFreeRects(_freeRects);
        }

        private void PruneContainedFreeRects(List<MaxRect> list)
        {
            bool removed;
            do
            {
                removed = false;
                for (int i = 0; i < list.Count; i++)
                {
                    for (int j = 0; j < list.Count; j++)
                    {
                        if (i == j) continue;
                        var a = list[i];
                        var b = list[j];
                        // if a is contained within b remove a
                        if (a.X + 0.01 >= b.X && a.Y + 0.01 >= b.Y && a.X + a.Width <= b.X + b.Width + 0.01 && a.Y + a.Height <= b.Y + b.Height + 0.01)
                        {
                            list.RemoveAt(i);
                            removed = true;
                            break;
                        }
                    }
                    if (removed) break;
                }
            } while (removed);
        }

        // =====================================================
        // PATCH 5 & 9: REMNANT COLLECTION & SCORING
        // =====================================================

        private void CollectRemnants(string sheetRef, int sheetNum)
        {
            foreach (var rect in _freeRects)
            {
                if (rect.Area > 100000 && Math.Min(rect.Width, rect.Height) > _constraints.MinRemnantSize)
                {
                    var remnant = new RemnantPiece
                    {
                        Id = $"R{_remnants.Count + 1}",
                        X = rect.X,
                        Y = rect.Y,
                        Width = rect.Width,
                        Height = rect.Height,
                        SourceSheet = sheetRef,
                        SheetNumber = sheetNum,
                        CreatedAt = DateTime.Now
                    };

                    // PATCH 9: Calculate value score
                    remnant.ValueScore = remnant.UsabilityScore;

                    if (remnant.ValueScore > 0.3) // Only keep useful remnants
                    {
                        _remnants.Add(remnant);
                    }
                }
            }
        }

        // =====================================================
        // PATCH 4: SECOND PASS NESTING - FIXED
        // =====================================================

        private double RunSecondPassNesting(List<CutPart> unplacedParts)
        {
            if (!_twoPassEnabled || unplacedParts.Count == 0) return 0;
            if (_remnants.Count == 0) return 0;

            // Get ALL usable remnants
            var usableRemnants = _remnants
                .Where(r => !r.IsReused && r.ValueScore > 0.2)
                .OrderByDescending(r => r.Area)
                .ToList();

            if (usableRemnants.Count == 0) return 0;

            double partsAreaOnRemnants = 0;

            // Try to place parts on each remnant
            foreach (var remnant in usableRemnants)
            {
                // Track pieces placed on THIS remnant so we can mark it fully used when no more fit
                var partsOnThisRemnant = new List<CutPart>();

                // Find all parts that fit this remnant
                foreach (var part in unplacedParts)
                {
                    if (part.IsPlaced) continue;

                    double pW = part.L + _kerf;
                    double pH = part.W + _kerf;

                    // Check normal orientation
                    bool canFitNormal = pW <= remnant.Width && pH <= remnant.Height;

                    // Check rotated orientation  
                    bool canFitRotated = false;
                    if (part.Rot)
                    {
                        canFitRotated = pH <= remnant.Width && pW <= remnant.Height;
                    }

                    if (!canFitNormal && !canFitRotated) continue;

                    // Place part
                    bool rotated = canFitRotated && !canFitNormal;

                    part.IsPlaced = true;
                    part.PlacedX = remnant.X + _kerf;
                    part.PlacedY = remnant.Y + _kerf;
                    part.PlacedW = rotated ? part.W : part.L;
                    part.PlacedH = rotated ? part.L : part.W;

                    // Track area
                    double partArea = (part.L * part.W) / 1000000.0;
                    partsAreaOnRemnants += partArea;
                    _usedSQM += partArea;
                    _totalPartsCut++;

                    partsOnThisRemnant.Add(part);
                }

                // Only mark remnant as used if we placed at least one part on it
                if (partsOnThisRemnant.Count > 0)
                {
                    remnant.IsReused = true;
                }
            }

            _totalPartsUnplaced = unplacedParts.Count(p => !p.IsPlaced);

            // Return area of parts placed on remnants (for tracking only, not used in util calc)
            return partsAreaOnRemnants;
        }

        // =====================================================
        // PATCH 10: DIGITAL TWIN TOOLPATH
        // =====================================================

        private void GenerateToolpathForPart(CutPart part, CutSequence sequence, double offsetX, double offsetY, double kerf)
        {
            double x = part.PlacedX;
            double y = part.PlacedY;
            double w = part.PlacedW;
            double h = part.PlacedH;

            // 4 cuts for rectangular part
            sequence.Operations.Add(new ToolpathOperation
            {
                PartId = part.Ref,
                Sequence = sequence.Operations.Count + 1,
                StartX = x + offsetX,
                StartY = y + offsetY,
                EndX = x + w + offsetX,
                EndY = y + offsetY,
                ToolType = "Cut"
            });

            sequence.Operations.Add(new ToolpathOperation
            {
                PartId = part.Ref,
                Sequence = sequence.Operations.Count + 1,
                StartX = x + w + offsetX,
                StartY = y + offsetY,
                EndX = x + w + offsetX,
                EndY = y + h + offsetY,
                ToolType = "Cut"
            });

            sequence.Operations.Add(new ToolpathOperation
            {
                PartId = part.Ref,
                Sequence = sequence.Operations.Count + 1,
                StartX = x + w + offsetX,
                StartY = y + h + offsetY,
                EndX = x + offsetX,
                EndY = y + h + offsetY,
                ToolType = "Cut"
            });

            sequence.Operations.Add(new ToolpathOperation
            {
                PartId = part.Ref,
                Sequence = sequence.Operations.Count + 1,
                StartX = x + offsetX,
                StartY = y + h + offsetY,
                EndX = x + offsetX,
                EndY = y + offsetY,
                ToolType = "Cut"
            });

            // Add bridge cuts if enabled
            if (_bridgeWidth > 0)
            {
                AddBridgeCuts(part, sequence, offsetX, offsetY);
            }
        }

        private void AddBridgeCuts(CutPart part, CutSequence sequence, double offsetX, double offsetY)
        {
            double x = part.PlacedX;
            double y = part.PlacedY;
            double w = part.PlacedW;
            double h = part.PlacedH;

            // Calculate bridge positions
            int bridgeCount = (int)Math.Floor(h / (_bridgeWidth * 4));
            if (bridgeCount < 1) bridgeCount = 1;

            double bridgeSpacing = h / (bridgeCount + 1);

            for (int i = 1; i <= bridgeCount; i++)
            {
                double bridgeY = y + (bridgeSpacing * i);

                sequence.Operations.Add(new ToolpathOperation
                {
                    PartId = part.Ref,
                    Sequence = sequence.Operations.Count + 1,
                    StartX = x + offsetX,
                    StartY = bridgeY + offsetY,
                    EndX = x + w + offsetX,
                    EndY = bridgeY + offsetY,
                    ToolType = "BridgeCut",
                    IsBridgeCut = true
                });

                sequence.BridgeCount++;
            }
        }

        // =====================================================
        // PATCH 6: COST CALCULATION
        // =====================================================

        private void CalculateCost()
        {
            if (_usedSQM > 0)
            {
                double pricePerSqm = 25;
                if (txtStockPrice != null)
                    double.TryParse(txtStockPrice.Text, out pricePerSqm);

                double totalCost = _usedSQM * pricePerSqm;

                // Add waste penalty
                double wasteArea = 0;
                foreach (var result in _results.Where(r => r.Ref != "TOTAL"))
                {
                    double sheetArea = result.L * result.W / 1000000.0;
                    wasteArea += sheetArea - result.Area;
                }

                totalCost += wasteArea * _costModel.WastePenaltyPerSQM / 1000;

                // Add remnant credit
                double remnantCredit = 0;
                foreach (var rem in _remnants.Where(r => !r.IsReused))
                {
                    remnantCredit += _costModel.CalculateRemnantCredit(rem.Area);
                }
                totalCost -= remnantCredit;

                // Add kerf cost
                double totalKerf = 0;
                foreach (var seq in _cutSequences)
                {
                    totalKerf += seq.TotalKerfLength;
                }
                totalCost += totalKerf * _costModel.KerfCostPerMm;

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

        // ======== ADD THIS MISSING METHOD ========
        private void txtStockPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateCost();
        }

        // =====================================================
        // REMNANT TRACKING
        // =====================================================

        private void TrackRemnants()
        {
            if (txtRemnants != null)
            {
                var usableRemnants = _remnants.Where(r => r.ValueScore > 0.5).ToList();
                var totalArea = usableRemnants.Sum(r => r.Area) / 1000000.0;
                txtRemnants.Text = $"{usableRemnants.Count} usable ({totalArea:N2}m²)";
            }
        }

        // =====================================================
        // OLD COMPATIBILITY METHODS
        // =====================================================

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

        // =====================================================
        // UI UPDATE METHODS
        // =====================================================

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

            overallStack.Children.Add(new TextBlock
            {
                Text = $"Strategy: {_nestingStrategy}",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                Margin = new Thickness(0, 5, 0, 0)
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

            // Show individual sheet results - no grouping needed
            var sheetResults = _results.Where(r => r.Ref != "TOTAL").OrderBy(r => r.Ref).ToList();

            foreach (var result in sheetResults)
            {
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
                    Text = $"{result.Ref}: {result.L:N0} × {result.W:N0}mm",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246))
                });

                sheetStack.Children.Add(new TextBlock
                {
                    Text = $"Area: {result.Area:N3} m² | U: {result.Util:N2}% | W: {result.Waste:N2}%",
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

            totalStack.Children.Add(new TextBlock
            {
                Text = $"Est. Cost: {txtTotalCost?.Text ?? "N/A"}",
                FontSize = 14,
                Foreground = new SolidColorBrush(Colors.White)
            });

            totalBorder.Child = totalStack;
            spReportDetails.Children.Add(totalBorder);
        }

        private void UpdateResultsGrouping() { }

        // =====================================================
        // DRAWING METHODS
        // =====================================================

        private void DrawCurrentLayout(int startIndex)
        {
            PreviewCanvas.Children.Clear();

            if (_results.Count == 0 || startIndex < 0) return;
            var validResults = _results.Where(r => r.Ref != "TOTAL").ToList();
            if (validResults.Count == 0) return;

            int sheetsToShow = validResults.Count;
            if (sheetsToShow > 100) sheetsToShow = 100;

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
            LayoutCanvas.Children.Clear();

            var validResults = _results.Where(r => r.Ref != "TOTAL").ToList();
            if (validResults.Count == 0 || startIndex < 0) return;

            int sheetsToDraw = Math.Min(_sheetsPerPage, validResults.Count - startIndex);
            if (sheetsToDraw <= 0) return;

            int cols = 2;
            int rows = (int)Math.Ceiling((double)_sheetsPerPage / cols);

            double canvasW = 800;
            double headerSpace = 25;
            double margin = 15;

            double sheetAreaH = 280;
            double canvasH = rows * sheetAreaH + margin * 2 + headerSpace;
            double sheetAreaW = (canvasW - margin * 2) / cols;

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

                TextBlock info = new TextBlock
                {
                    Text = $"#{idx + 1}: {currentResult.L:N0}×{currentResult.W:N0}mm U:{currentResult.Util:N1}% W:{currentResult.Waste:N1}%",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.White)
                };
                Canvas.SetLeft(info, areaLeft + 5);
                Canvas.SetTop(info, areaTop + 2);
                LayoutCanvas.Children.Add(info);

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
        // DATA MANAGEMENT
        // =====================================================

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
                _cutSequences.Clear();
                _freeRects.Clear();

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

        // =====================================================
        // JOB SAVE/LOAD
        // =====================================================

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

        // =====================================================
        // EXPORT METHODS
        // =====================================================

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

        // =====================================================
        // SIMULATION
        // =====================================================

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

            DrawSingleSheetLayout(_currentIndex);
            _currentIndex++;

            if (_currentIndex >= validResults.Count)
            {
                _simulateTimer.Stop();
                _isSimulating = false;
                _currentIndex = 0;

                var simulateBtn = FindName("btnSimulate") as Button;
                if (simulateBtn != null)
                {
                    simulateBtn.Content = "▶ Simulate";
                    simulateBtn.Background = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                }

                MessageBox.Show("Simulation complete!", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // =====================================================
        // PUBLIC API METHODS
        // =====================================================

        public void ImportInvoiceItems(List<Models.InvoiceItemModel> parts)
        {
            _cutParts.Clear();
            if (parts == null) return;
            int count = 1;
            foreach (var p in parts)
            {
                if (p == null) continue;
                _cutParts.Add(new CutPart
                {
                    Ref = !string.IsNullOrEmpty(p.GlassRef) ? p.GlassRef : $"P{count++}",
                    L = p.Width1,
                    W = p.Height1,
                    Rot = true,
                    Qty = p.Qty > 0 ? p.Qty : 1
                });
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

                RunAdvancedNesting(_kerf, _rotationPolicy);
                UpdateReportSection();
                ShowTab("Layouts");
            }
            catch { }
        }
    }

    // =====================================================
    // DATA CLASSES
    // =====================================================

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

    // =====================================================
    // PATCH 1: MAXRECTS CLASSES
    // =====================================================

    public class MaxRect
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public double Area => Width * Height;
        public double ShortSide => Math.Min(Width, Height);
        public double LongSide => Math.Max(Width, Height);
        public double AspectRatio => Width > 0 ? LongSide / ShortSide : double.MaxValue;

        public MaxRect() { }

        public MaxRect(double x, double y, double w, double h)
        {
            X = x; Y = y; Width = w; Height = h;
        }

        public bool Fits(double w, double h) => Width >= w && Height >= h;

        public MaxRect Clone() => new MaxRect(X, Y, Width, Height);
    }

    // =====================================================
    // PATCH 3: PLACEMENT NODE
    // =====================================================

    public class PlacementNode
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsRotated { get; set; }
        public double Score { get; set; }
    }

    // =====================================================
    // PATCH 5 & 9: REMNANT CLASSES
    // =====================================================

    public class RemnantPiece
    {
        public string Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string SourceSheet { get; set; }
        public int SheetNumber { get; set; }
        public bool IsReused { get; set; }
        public double ValueScore { get; set; }
        public DateTime CreatedAt { get; set; }

        public double Area => Width * Height;

        public double UsabilityScore
        {
            get
            {
                double aspectScore = Math.Min(Width, Height) / Math.Max(Width, Height);
                double sizeScore = Math.Min(Width, Height) > 200 ? 1.0 :
                                  Math.Min(Width, Height) > 100 ? 0.7 : 0.3;
                double areaScore = Area > 500000 ? 1.0 : Area > 200000 ? 0.6 : 0.2;
                return (aspectScore * 0.4 + sizeScore * 0.3 + areaScore * 0.3);
            }
        }
    }

    // =====================================================
    // PATCH 8: CONSTRAINT CLASSES
    // =====================================================

    public class PlacementConstraint
    {
        public double MinPartSize { get; set; } = 50;
        public double SafetyMarginX { get; set; } = 0;
        public double SafetyMarginY { get; set; } = 0;
        public double KerfCompensation { get; set; } = 4.0;
        public double MinRemnantSize { get; set; } = 20;
        public bool AllowRotation { get; set; } = true;
        public double MaxAspectRatio { get; set; } = 10.0;

        public bool ValidatePlacement(double w, double h)
        {
            // Only reject if too small
            if (w < 20 || h < 20) return false;
            return true;
        }
    }

    // =====================================================
    // PATCH 6: COST MODEL
    // =====================================================

    public class CostModel
    {
        public double StockPricePerSQM { get; set; } = 250;
        public double WastePenaltyPerSQM { get; set; } = 50;
        public double RemnantCreditPerSQM { get; set; } = 25;
        public double KerfCostPerMm { get; set; } = 0.01;
        public double OperatingCostPerHour { get; set; } = 150;
        public double SetupCostPerJob { get; set; } = 50;

        public double CalculateSheetCost(double usedArea, double sheetArea, double kerfLength)
        {
            double stockCost = (sheetArea / 1000000) * StockPricePerSQM;
            double wasteCost = ((sheetArea - usedArea) / 1000000) * WastePenaltyPerSQM;
            double kerfCost = kerfLength * KerfCostPerMm;
            return stockCost + wasteCost + kerfCost;
        }

        public double CalculateRemnantCredit(double area)
        {
            return (area / 1000000) * RemnantCreditPerSQM;
        }

        public double CalculateTotalCost(double usedArea, double totalSheetArea, double kerfLength, double remnantArea)
        {
            double cost = CalculateSheetCost(usedArea, totalSheetArea, kerfLength);
            cost -= CalculateRemnantCredit(remnantArea);
            return cost;
        }
    }

    // =====================================================
    // PATCH 10: DIGITAL TWIN CLASSES
    // =====================================================

    public class ToolpathOperation
    {
        public string PartId { get; set; }
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        public double Depth { get; set; }
        public double Speed { get; set; }
        public int Sequence { get; set; }
        public string ToolType { get; set; } = "StraightCut";
        public bool IsBridgeCut { get; set; }

        public double Length => Math.Sqrt(Math.Pow(EndX - StartX, 2) + Math.Pow(EndY - StartY, 2));
    }

    public class CutSequence
    {
        public string SheetId { get; set; }
        public List<ToolpathOperation> Operations { get; set; } = new List<ToolpathOperation>();
        public double TotalKerfLength { get; set; }
        public double EstimatedTime { get; set; }
        public double BridgeCount { get; set; }

        public CutSequence()
        {
            Operations = new List<ToolpathOperation>();
        }

        public List<Point> GetToolpathPoints()
        {
            var points = new List<Point>();
            foreach (var op in Operations.OrderBy(o => o.Sequence))
            {
                points.Add(new Point(op.StartX, op.StartY));
                points.Add(new Point(op.EndX, op.EndY));
            }
            return points;
        }

        public double CalculateTotalCutLength()
        {
            return Operations.Sum(o => o.Length);
        }

        public double EstimateProcessingTime(double speedMmPerSec = 5000)
        {
            return CalculateTotalCutLength() / speedMmPerSec;
        }
    }
}