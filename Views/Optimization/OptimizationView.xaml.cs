using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace ProGlassAutomation.Views.Optimization
{
    public partial class OptimizationView : UserControl
    {
        private const int UnlimitedQtySentinel = 9999999;

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

        private double _zoomLevel = 1.5;
        private int _currentIndex = 0;
        private int _sheetsPerPage = 8;
        private bool _isSimulating = false;
        private DispatcherTimer _simulateTimer;

        private double _lr = 15, _br = 15, _tr = 15, _rm = 15, _kerf = 4.0, _breakout = 4.0;
        private RotationPolicy _rotationPolicy = RotationPolicy.BestFit;

        // Drag helper state (fixes empty drag-start handler & enables internal drag-drop text export)
        private Point _dragStartPoint;

        // Find-next state (fixes “always returns first match” UX bug)
        private string _lastFindText = string.Empty;
        private int _lastFindMatchOrderIndex = -1;

        public OptimizationView()
        {
            InitializeComponent();
            DataContext = this;

            dgStock.ItemsSource = _stockSheets;
            dgParts.ItemsSource = _cutParts;
            icResults.ItemsSource = _results;

            if (cmbSheetSelector != null)
                cmbSheetSelector.ItemsSource = _results;

            if (cmbSavedJobs != null)
                cmbSavedJobs.ItemsSource = _savedJobs;

            if (PreviewCanvas != null)
            {
                PreviewCanvas.Width = 900;
                PreviewCanvas.Height = 650;
            }

            // Initialize timer once (fix: avoid recreating timer and re-attaching tick multiple times)
            _simulateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _simulateTimer.Tick += SimulateTimer_Tick;

            // Ensure key handling works (XAML sets Focusable="True", but we also focus on load)
            Loaded += (_, __) =>
            {
                try { Keyboard.Focus(this); } catch { /* ignore */ }
            };

            // Ensure timer is stopped when view is unloaded (fix: avoid timer firing on disposed view)
            Unloaded += (_, __) =>
            {
                try
                {
                    _isSimulating = false;
                    _simulateTimer.Stop();
                }
                catch { /* ignore */ }
            };

            // Wire internal drag detection (fix: DG_PreviewMouseLeftButtonDown was empty)
            if (dgStock != null) dgStock.PreviewMouseMove += DG_PreviewMouseMove;
            if (dgParts != null) dgParts.PreviewMouseMove += DG_PreviewMouseMove;

            _engine.Configure(_lr, _rm, _tr, _br, _kerf, _breakout);
            _engine.SetEngineMode(EngineMode.IQ200V7);
            _engine.SetRotationPolicy(RotationPolicy.BestFit);
        }

        public void SetStockSheets(IEnumerable<StockSheet> sheets)
        {
            _stockSheets.Clear();
            if (sheets == null) return;

            int idx = 1;
            foreach (var s in sheets)
            {
                var copy = new StockSheet
                {
                    Ref = string.IsNullOrEmpty(s.Ref) ? $"S{idx++}" : s.Ref,
                    L = s.L,
                    W = s.W,
                    Qty = s.Qty
                };
                _stockSheets.Add(copy);
            }
            UpdateStockSummary();
        }

        public void AddStockSheet(double width, double height, int qty = UnlimitedQtySentinel)
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

        public OptimizationResult? GetTotalResult()
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
                string tab = btn.Tag.ToString() ?? string.Empty;
                ResetTabs();

                SafeApplyStyle(btn, "TabActive");

                switch (tab)
                {
                    case "Stock": if (pnlStock != null) pnlStock.Visibility = Visibility.Visible; break;
                    case "Parts": if (pnlParts != null) pnlParts.Visibility = Visibility.Visible; break;
                    case "Settings": if (pnlSettings != null) pnlSettings.Visibility = Visibility.Visible; break;
                    case "Layouts": if (pnlSummary != null) pnlSummary.Visibility = Visibility.Visible; break;
                    case "Report": if (pnlReport != null) pnlReport.Visibility = Visibility.Visible; break;
                }
            }
        }

        private void ResetTabs()
        {
            if (btnStock != null) SafeApplyStyle(btnStock, "TabInactive");
            if (btnParts != null) SafeApplyStyle(btnParts, "TabInactive");
            if (btnSettings != null) SafeApplyStyle(btnSettings, "TabInactive");
            if (btnSummary != null) SafeApplyStyle(btnSummary, "TabInactive");
            if (btnReport != null) SafeApplyStyle(btnReport, "TabInactive");

            if (pnlStock != null) pnlStock.Visibility = Visibility.Collapsed;
            if (pnlParts != null) pnlParts.Visibility = Visibility.Collapsed;
            if (pnlSettings != null) pnlSettings.Visibility = Visibility.Collapsed;
            if (pnlSummary != null) pnlSummary.Visibility = Visibility.Collapsed;
            if (pnlReport != null) pnlReport.Visibility = Visibility.Collapsed;
        }

        private void ShowTab(string tab)
        {
            ResetTabs();
            switch (tab)
            {
                case "Stock":
                    if (btnStock != null) SafeApplyStyle(btnStock, "TabActive");
                    if (pnlStock != null) pnlStock.Visibility = Visibility.Visible;
                    break;

                case "Parts":
                    if (btnParts != null) SafeApplyStyle(btnParts, "TabActive");
                    if (pnlParts != null) pnlParts.Visibility = Visibility.Visible;
                    break;

                case "Settings":
                    if (btnSettings != null) SafeApplyStyle(btnSettings, "TabActive");
                    if (pnlSettings != null) pnlSettings.Visibility = Visibility.Visible;
                    break;

                case "Layouts":
                    if (btnSummary != null) SafeApplyStyle(btnSummary, "TabActive");
                    if (pnlSummary != null) pnlSummary.Visibility = Visibility.Visible;
                    break;

                case "Report":
                    if (btnReport != null) SafeApplyStyle(btnReport, "TabActive");
                    if (pnlReport != null) pnlReport.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void SafeApplyStyle(Button button, string resourceKey)
        {
            try
            {
                var style = TryFindResource(resourceKey) as Style;
                if (style != null)
                    button.Style = style;
                else
                    Debug.WriteLine($"[OptimizationView] Style resource '{resourceKey}' not found.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OptimizationView] Failed to apply style '{resourceKey}': {ex.Message}");
            }
        }

        private void AddStockSheet_Click(object sender, RoutedEventArgs e)
        {
            _stockSheets.Add(new StockSheet { Ref = $"S{_stockSheets.Count + 1}", L = 3300, W = 2433, Qty = UnlimitedQtySentinel });
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

        // Fix: previously empty; now commits edits and captures drag start point
        private void DG_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);

            if (sender is DataGrid dg)
            {
                try
                {
                    dg.Focus();
                    dg.CommitEdit(DataGridEditingUnit.Cell, true);
                    dg.CommitEdit(DataGridEditingUnit.Row, true);
                }
                catch { /* ignore */ }
            }
        }

        // Fix: allow internal drag of selected rows as text (compatible with DG_Drop parser)
        private void DG_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            var position = e.GetPosition(null);
            var diff = _dragStartPoint - position;

            if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            if (sender is not DataGrid dg) return;
            if (dg.SelectedItems == null || dg.SelectedItems.Count == 0) return;

            try
            {
                string text = BuildDragTextForGrid(dg);
                if (string.IsNullOrWhiteSpace(text)) return;

                var data = new DataObject();
                data.SetData(DataFormats.Text, text);
                data.SetData(DataFormats.UnicodeText, text);

                DragDrop.DoDragDrop(dg, data, DragDropEffects.Copy);
                e.Handled = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OptimizationView] Drag export error: {ex.Message}");
            }
        }

        private string BuildDragTextForGrid(DataGrid dg)
        {
            // We intentionally produce the same format that ParseStockData/ParsePartsData expects: "L W Qty"
            var lines = new List<string>();

            if (dg == dgStock)
            {
                foreach (var item in dg.SelectedItems.OfType<StockSheet>())
                {
                    lines.Add($"{item.L.ToString(CultureInfo.InvariantCulture)}\t{item.W.ToString(CultureInfo.InvariantCulture)}\t{item.Qty}");
                }
            }
            else if (dg == dgParts)
            {
                foreach (var item in dg.SelectedItems.OfType<CutPart>())
                {
                    lines.Add($"{item.L.ToString(CultureInfo.InvariantCulture)}\t{item.W.ToString(CultureInfo.InvariantCulture)}\t{item.Qty}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private void DG_Drop(object sender, DragEventArgs e)
        {
            try
            {
                string? text = null;

                // Fix: accept UnicodeText as well as Text (Excel/other sources often provide UnicodeText)
                if (e.Data.GetDataPresent(DataFormats.UnicodeText))
                    text = e.Data.GetData(DataFormats.UnicodeText) as string;
                else if (e.Data.GetDataPresent(DataFormats.Text))
                    text = e.Data.GetData(DataFormats.Text) as string;

                if (string.IsNullOrWhiteSpace(text)) return;

                if (sender == dgStock)
                {
                    var sheets = _services.ParseStockData(text, _stockSheets.Count + 1);
                    foreach (var s in sheets) _stockSheets.Add(s);
                    UpdateStockSummary();
                }
                else if (sender == dgParts)
                {
                    var parts = _services.ParsePartsData(text, _cutParts.Count + 1);
                    foreach (var p in parts) _cutParts.Add(p);
                    UpdatePartsSummary();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to parse dropped raw layout values: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStockSummary()
        {
            if (txtStockSummary == null) return;

            if (_stockSheets.Count == 0)
            {
                txtStockSummary.Text = "No stock added";
                return;
            }

            bool hasUnlimited = _stockSheets.Any(s => s.Qty >= UnlimitedQtySentinel);

            // group by size
            var groups = _stockSheets.GroupBy(s => new { s.L, s.W })
                .Select(g =>
                {
                    bool groupUnlimited = g.Any(x => x.Qty >= UnlimitedQtySentinel);
                    int totalQty = groupUnlimited ? UnlimitedQtySentinel : g.Sum(x => x.Qty);
                    double sqm = groupUnlimited
                        ? (g.Key.L * g.Key.W) / 1000000.0 // show per-sheet sqm if unlimited
                        : g.Sum(x => x.L * x.W * x.Qty) / 1000000.0;

                    return new
                    {
                        g.Key.L,
                        g.Key.W,
                        IsUnlimited = groupUnlimited,
                        TotalQty = totalQty,
                        SQM = sqm
                    };
                })
                .OrderByDescending(x => x.SQM)
                .ToList();

            var lines = new List<string>();

            if (hasUnlimited)
                lines.Add($"Stock: {_stockSheets.Count} types, Unlimited qty");
            else
                lines.Add($"Stock: {_stockSheets.Count} types, {_stockSheets.Sum(s => s.Qty)} total");

            foreach (var g in groups)
            {
                string qtyText = g.IsUnlimited ? "∞" : g.TotalQty.ToString(CultureInfo.InvariantCulture);
                string sqmText = g.IsUnlimited ? $"{g.SQM:N2}m² / sheet" : $"{g.SQM:N2}m²";
                lines.Add($"{g.L:N0}×{g.W:N0}mm = {qtyText} ({sqmText})");
            }

            txtStockSummary.Text = string.Join("\n", lines);
        }

        private void UpdatePartsSummary()
        {
            if (txtPartsSummary == null) return;

            if (_cutParts.Count == 0)
            {
                txtPartsSummary.Text = "No parts added";
                return;
            }

            var totalQty = _cutParts.Sum(p => p.Qty);
            var totalSQM = _cutParts.Sum(p => p.L * p.W * p.Qty) / 1000000.0;

            var groups = _cutParts.GroupBy(p => new { p.L, p.W, p.Ref })
                .Select(g => new
                {
                    Ref = g.Key.Ref,
                    L = g.Key.L,
                    W = g.Key.W,
                    Qty = g.Sum(x => x.Qty),
                    SQM = g.Sum(x => x.L * x.W * x.Qty) / 1000000.0
                })
                .OrderByDescending(x => x.SQM)
                .ToList();

            var lines = groups.Select(g => $"{g.Ref}: {g.L}×{g.W}×{g.Qty} = {g.SQM:N2}m²").ToList();
            lines.Insert(0, $"Parts: {totalQty} total ({totalSQM:N2}m²)");
            txtPartsSummary.Text = string.Join("\n", lines);
        }

        private RotationPolicy GetRotationPolicyFromUI()
        {
            // Fix: SelectedIndex-to-enum cast was wrong because the UI items are not in enum order.
            // XAML: 0=Rotate90, 1=None, 2=BestFit
            if (cmbRotation == null || cmbRotation.SelectedIndex < 0)
                return RotationPolicy.BestFit;

            return cmbRotation.SelectedIndex switch
            {
                0 => RotationPolicy.Rotate90,
                1 => RotationPolicy.None,
                2 => RotationPolicy.BestFit,
                _ => RotationPolicy.BestFit
            };
        }

        private bool TryParseDoubleFromTextBox(TextBox? tb, out double value)
        {
            value = 0;
            if (tb == null) return false;

            var text = tb.Text?.Trim() ?? string.Empty;

            // Try current culture first, then invariant (fix: robust parsing without breaking existing user locale behavior)
            return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
                   || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private async void RunOptimization_Click(object sender, RoutedEventArgs e)
        {
            // Keep existing combined-items workflow
            if (_combinedItems.Count > 0)
            {
                UpdateOptimizableItems();
            }

            TryParseDoubleFromTextBox(txtLR, out _lr);
            TryParseDoubleFromTextBox(txtBR, out _br);
            TryParseDoubleFromTextBox(txtTR, out _tr);
            TryParseDoubleFromTextBox(txtRM, out _rm);
            TryParseDoubleFromTextBox(txtKerf, out _kerf);
            TryParseDoubleFromTextBox(txtBreakout, out _breakout);

            _rotationPolicy = GetRotationPolicyFromUI();

            _engine.Configure(_lr, _rm, _tr, _br, _kerf, _breakout);
            _engine.SetRotationPolicy(_rotationPolicy);

            if (!ValidateInputs())
                return;

            try
            {
                int partsBefore = _cutParts.Sum(p => p.Qty);

                if (txtEngineStatus != null) txtEngineStatus.Text = "Computing... Please wait.";
                if (txtSheetInfoFooter != null) txtSheetInfoFooter.Text = "Computing optimization...";

                // Fix: run on background thread to avoid UI freeze
                await Task.Run(() =>
                    _engine.ExecuteNesting(_stockSheets.ToList(), _cutParts.ToList(), _results, _allPlacedParts, _savedJobs)
                );

                UpdateReportSection();
                UpdateResultsGrouping();
                ShowTab("Layouts");

                // KPI updates
                if (txtSheetsUsed != null)
                    txtSheetsUsed.Text = _results.Count(r => r.Ref != "TOTAL").ToString(CultureInfo.InvariantCulture);

                // Fix: display “∞” if stock has an unlimited sentinel
                if (txtSheetsRemaining != null)
                {
                    bool hasUnlimited = _stockSheets.Any(s => s.Qty >= UnlimitedQtySentinel);
                    if (hasUnlimited)
                    {
                        txtSheetsRemaining.Text = "∞";
                    }
                    else
                    {
                        int remaining = Math.Max(0, _stockSheets.Sum(s => s.Qty) - _results.Count(r => r.Ref != "TOTAL"));
                        txtSheetsRemaining.Text = remaining.ToString(CultureInfo.InvariantCulture);
                    }
                }

                if (txtUtilization != null)
                    txtUtilization.Text = $"{_engine.OverallUtilization:N1}%";

                if (txtWaste != null)
                    txtWaste.Text = $"{_engine.OverallWastage:N1}%";

                if (txtTotalPartsCut != null)
                    txtTotalPartsCut.Text = _engine.TotalPartsCut.ToString(CultureInfo.InvariantCulture);

                if (txtEngineStatus != null) txtEngineStatus.Text = "Complete — Layouts ready.";
                if (txtSheetInfoFooter != null) txtSheetInfoFooter.Text = "Complete — Use Prev/Next or Sheet Selector to browse layouts.";

                _currentIndex = 0;
                if (cmbSheetSelector != null && _results.Count > 0)
                    cmbSheetSelector.SelectedIndex = 0;

                DrawCurrentLayout(_currentIndex);
                DrawSingleSheetLayout(_currentIndex);
                UpdateLayoutCount();

                int placedCount = _allPlacedParts.Count;
                int unplacedCount = _engine.TotalPartsUnplaced;

                MessageBox.Show(
                    $"=== COMPLETE ===\n\n" +
                    $"Parts Input: {partsBefore}\n" +
                    $"Parts Placed: {placedCount}\n" +
                    $"Parts NOT Placed: {unplacedCount}\n" +
                    $"Sheets Used: {_results.Count(r => r.Ref != "TOTAL")}",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                if (txtEngineStatus != null) txtEngineStatus.Text = "Error — see message.";
                if (txtSheetInfoFooter != null) txtSheetInfoFooter.Text = "Error during optimization.";
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
                if (txtSheetsRemaining != null) txtSheetsRemaining.Text = "0";
                if (txtUtilization != null) txtUtilization.Text = "0%";
                if (txtWaste != null) txtWaste.Text = "0%";
                if (txtTotalSheetsUsed != null) txtTotalSheetsUsed.Text = "0";
                if (txtTotalPartsCut != null) txtTotalPartsCut.Text = "0";
                if (txtAvgUtilization != null) txtAvgUtilization.Text = "0%";
                if (txtTotalStats != null) txtTotalStats.Text = "0 sheets, 0 parts";

                if (txtStockSummary != null) txtStockSummary.Text = "No stock added";
                if (txtPartsSummary != null) txtPartsSummary.Text = "No parts added";
                if (txtCurrentLayoutStats != null) txtCurrentLayoutStats.Text = "Select a layout to view";
                if (txtRemnants != null) txtRemnants.Text = "0 remnants available";
                if (txtTotalCost != null) txtTotalCost.Text = "AED 0.00";
                if (txtEngineStatus != null) txtEngineStatus.Text = "Ready — Configure and press COMPUTE.";
                if (txtSheetInfoFooter != null) txtSheetInfoFooter.Text = "Ready — Add stock and parts, then press COMPUTE OPTIMIZATION";

                _currentIndex = 0;

                if (PreviewCanvas != null) PreviewCanvas.Children.Clear();
                if (LayoutCanvas != null) LayoutCanvas.Children.Clear();
                if (spReportDetails != null) spReportDetails.Children.Clear();
                if (pnlUnplaced != null) pnlUnplaced.Visibility = Visibility.Collapsed;

                _isSimulating = false;
                _simulateTimer.Stop();
                if (btnSimulate != null) btnSimulate.Content = "Start Simulation";
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
                var dialog = new SaveFileDialog
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
                var dialog = new SaveFileDialog
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
            if (cmbSavedJobs != null)
                cmbSavedJobs.ItemsSource = _savedJobs;

            MessageBox.Show($"Job saved: {jobName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadJob_Click(object sender, RoutedEventArgs e)
        {
            if (cmbSavedJobs != null && cmbSavedJobs.SelectedItem is OptimizationJob job)
            {
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
            if (cmbSavedJobs != null && cmbSavedJobs.SelectedItem is OptimizationJob job)
            {
                _savedJobs.Remove(job);
                cmbSavedJobs.ItemsSource = _savedJobs;
                MessageBox.Show("Job deleted", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // =====================================================
        // DRAWING METHODS
        // =====================================================

        private void DrawCurrentLayout(int startIndex)
        {
            if (PreviewCanvas == null) return;

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
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
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
                    Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Canvas.SetLeft(info, 10);
                Canvas.SetTop(info, 5 + i * 26 + 3);
                PreviewCanvas.Children.Add(info);
            }

            if (txtCurrentLayoutStats != null)
                txtCurrentLayoutStats.Text = $"Full List: {validResults.Count} sheets";
        }

        private void DrawSingleSheetLayout(int startIndex)
        {
            DrawSingleSheetLayout(startIndex, string.Empty);
        }

        private void DrawSingleSheetLayout(int startIndex, string searchText)
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

            // Fix: previous height calc used integer division and _sheetsPerPage instead of actual sheetsToDraw
            int rows = (int)Math.Ceiling(sheetsToDraw / (double)cols);
            double canvasH = rows * (sheetAreaH + headerSpace) + margin * 2 + headerSpace;

            LayoutCanvas.Width = canvasW;
            LayoutCanvas.Height = canvasH;

            Color[] partColors = new Color[]
            {
                Color.FromRgb(59, 130, 246), Color.FromRgb(16, 185, 129),
                Color.FromRgb(139, 92, 246), Color.FromRgb(245, 158, 11),
                Color.FromRgb(239, 68, 68), Color.FromRgb(6, 182, 212),
                Color.FromRgb(236, 72, 153), Color.FromRgb(34, 197, 94)
            };

            bool hasSearch = !string.IsNullOrEmpty(searchText);

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

                double leftOffset = _lr * scale;
                double rightOffset = _rm * scale;
                double topOffset = _tr * scale;
                double bottomOffset = _br * scale;

                double usableW = drawW - leftOffset - rightOffset;
                double usableH = drawH - topOffset - bottomOffset;

                TextBlock info = new TextBlock
                {
                    Text = $"#{idx + 1}: {currentResult.L:N0}×{currentResult.W:N0}mm U:{currentResult.Util:N1}%",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42))
                };
                Canvas.SetLeft(info, areaLeft + 5);
                Canvas.SetTop(info, areaTop + 2);
                LayoutCanvas.Children.Add(info);

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

                    bool isMatch = hasSearch && part.Ref.ToUpperInvariant().Contains(searchText);
                    var color = colorMap.ContainsKey(part.Ref) ? colorMap[part.Ref] : Color.FromRgb(128, 128, 128);
                    var fillColor = isMatch ? Color.FromRgb(255, 215, 0) : color;

                    Border partBorder = new Border
                    {
                        Width = pw,
                        Height = ph,
                        Background = new SolidColorBrush(fillColor),
                        BorderBrush = isMatch ? Brushes.White : (part.IsRotated ? Brushes.Yellow : Brushes.White),
                        BorderThickness = new Thickness(isMatch ? 3 : 1)
                    };
                    Canvas.SetLeft(partBorder, px);
                    Canvas.SetTop(partBorder, py);
                    LayoutCanvas.Children.Add(partBorder);
                }
            }

            if (txtCurrentSheet != null)
                txtCurrentSheet.Text = $"Layouts: {startIndex + 1}-{startIndex + sheetsToDraw} of {validResults.Count}";
        }

        private void HighlightPartsOnLayout(string searchText)
        {
            DrawSingleSheetLayout(_currentIndex, searchText);
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

        private void SheetSelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSheetSelector != null && cmbSheetSelector.SelectedIndex >= 0 && _results.Count > 0)
            {
                var selectedResult = cmbSheetSelector.SelectedItem as OptimizationResult;
                if (selectedResult != null && selectedResult.Ref == "TOTAL") return;

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
            if (validResults.Count == 0) return;

            if (_currentIndex > 0)
            {
                _currentIndex = Math.Max(0, _currentIndex - _sheetsPerPage);
                if (cmbSheetSelector != null && _currentIndex < cmbSheetSelector.Items.Count)
                    cmbSheetSelector.SelectedIndex = _currentIndex;

                DrawCurrentLayout(_currentIndex);
                DrawSingleSheetLayout(_currentIndex);
                UpdateLayoutCount();
            }
        }

        private void NextLayout_Click(object sender, RoutedEventArgs e)
        {
            var validResults = _results.Where(r => r.Ref != "TOTAL").ToList();
            if (validResults.Count == 0) return;

            if (_currentIndex < validResults.Count - _sheetsPerPage)
            {
                _currentIndex = Math.Min(validResults.Count - 1, _currentIndex + _sheetsPerPage);
                if (cmbSheetSelector != null && _currentIndex < cmbSheetSelector.Items.Count)
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
            int totalPages = (int)Math.Ceiling((double)Math.Max(1, totalSheets) / _sheetsPerPage);
            if (totalPages < 1) totalPages = 1;

            if (txtLayoutNum != null) txtLayoutNum.Text = $"Page {page}/{totalPages}";
            if (txtCurrentLayoutStats != null) txtCurrentLayoutStats.Text = $"Center: {page}/{totalPages} | View: {_sheetsPerPage}/page";
        }

        private void SheetsPerPage4_Click(object sender, RoutedEventArgs e) { SetSheetsPerPage(4); }
        private void SheetsPerPage6_Click(object sender, RoutedEventArgs e) { SetSheetsPerPage(6); }
        private void SheetsPerPage8_Click(object sender, RoutedEventArgs e) { SetSheetsPerPage(8); }
        private void SheetsPerPage10_Click(object sender, RoutedEventArgs e) { SetSheetsPerPage(10); }

        private void SetSheetsPerPage(int count)
        {
            _sheetsPerPage = count;
            if (cmbSheetSelector != null) cmbSheetSelector.SelectedIndex = 0;
            _currentIndex = 0;
            DrawCurrentLayout(_currentIndex);
            DrawSingleSheetLayout(_currentIndex);
            UpdateLayoutCount();
        }

        private void txtFindPart_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Fix: reset FindNext state when search text changes
            var current = txtFindPart?.Text?.Trim() ?? string.Empty;
            if (!string.Equals(current, _lastFindText, StringComparison.OrdinalIgnoreCase))
            {
                _lastFindText = current;
                _lastFindMatchOrderIndex = -1;
            }

            if (string.IsNullOrEmpty(current))
            {
                DrawSingleSheetLayout(_currentIndex);
                return;
            }

            string searchText = current.ToUpperInvariant();
            HighlightPartsOnLayout(searchText);
        }

        private void txtFindPart_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) btnFindNext_Click(sender, e);
        }

        private void btnFindNext_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtFindPart?.Text)) return;

            string searchText = txtFindPart.Text.Trim();
            if (string.IsNullOrEmpty(searchText)) return;

            string upperSearch = searchText.ToUpperInvariant();

            var validResults = _results.Where(r => r.Ref != "TOTAL").ToList();
            if (validResults.Count == 0)
            {
                MessageBox.Show("No layouts available. Run optimization first.", "Not Ready", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Map result positions for deterministic “next”
            var posMap = validResults
                .Select((r, idx) => new { r.SheetRef, r.SheetNum, idx })
                .ToDictionary(k => (k.SheetRef, k.SheetNum), v => v.idx);

            var orderedMatches = _allPlacedParts
                .Where(p => p.Ref != null && p.Ref.ToUpperInvariant().Contains(upperSearch))
                .Select(p =>
                {
                    int pos = posMap.TryGetValue((p.Sheet, p.SheetNum), out var idx) ? idx : int.MaxValue;
                    return new { Part = p, Pos = pos };
                })
                .Where(x => x.Pos != int.MaxValue)
                .OrderBy(x => x.Pos)
                .ThenBy(x => x.Part.Ref)
                .ToList();

            if (orderedMatches.Count == 0)
            {
                MessageBox.Show($"Part '{searchText}' not found.", "Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Fix: cycle through matches instead of always returning the first
            _lastFindText = searchText;

            int nextOrder = _lastFindMatchOrderIndex + 1;
            if (nextOrder >= orderedMatches.Count) nextOrder = 0;
            _lastFindMatchOrderIndex = nextOrder;

            var match = orderedMatches[nextOrder];
            int index = match.Pos;

            _currentIndex = (index / _sheetsPerPage) * _sheetsPerPage;
            if (cmbSheetSelector != null) cmbSheetSelector.SelectedIndex = index;

            DrawCurrentLayout(_currentIndex);
            HighlightPartsOnLayout(upperSearch);

            MessageBox.Show(
                $"Found ({nextOrder + 1}/{orderedMatches.Count}) on {match.Part.Sheet}-{match.Part.SheetNum}\n{match.Part.Ref}: {match.Part.L:N0}×{match.Part.W:N0}mm",
                "Found", MessageBoxButton.OK, MessageBoxImage.Information);
        }

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

        private void UpdateReportSection()
        {
            if (spReportDetails == null) return;
            spReportDetails.Children.Clear();

            var header = new TextBlock { Text = "CUTTING REPORT", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)), Margin = new Thickness(0, 0, 0, 15) };
            spReportDetails.Children.Add(header);

            var overallBorder = new Border { Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)), Padding = new Thickness(15), CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 0, 0, 15) };
            var overallStack = new StackPanel();

            var totalSheets = _results.Count(r => r.Ref != "TOTAL");
            var totalParts = _engine.TotalPartsCut;
            var usedSQM = _engine.UsedSQM;
            var avgUtil = _engine.OverallUtilization;

            overallStack.Children.Add(new TextBlock { Text = "OVERALL SUMMARY", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)), Margin = new Thickness(0, 0, 0, 10) });
            overallStack.Children.Add(new TextBlock { Text = $"Total Sheets Used: {totalSheets}", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(217, 119, 6)) });
            overallStack.Children.Add(new TextBlock { Text = $"Total Parts Cut: {totalParts}", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)) });
            overallStack.Children.Add(new TextBlock { Text = $"Glass Area Used: {usedSQM:N2} m²", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)) });
            overallStack.Children.Add(new TextBlock { Text = $"Average Utilization: {avgUtil:N2}%", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)) });
            overallStack.Children.Add(new TextBlock { Text = $"Wastage: {_engine.OverallWastage:N2}%", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)) });

            if (_engine.TotalPartsUnplaced > 0)
            {
                overallStack.Children.Add(new TextBlock { Text = $"⚠ Unplaced Parts: {_engine.TotalPartsUnplaced}", FontWeight = FontWeights.Bold, FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28)), Margin = new Thickness(0, 10, 0, 0) });

                if (pnlUnplaced != null) pnlUnplaced.Visibility = Visibility.Visible;
                if (txtUnplaced != null) txtUnplaced.Text = $"Unplaced: {_engine.TotalPartsUnplaced}";
            }
            else
            {
                if (pnlUnplaced != null) pnlUnplaced.Visibility = Visibility.Collapsed;
            }

            overallBorder.Child = overallStack;
            spReportDetails.Children.Add(overallBorder);

            foreach (var result in _results.Where(r => r.Ref != "TOTAL").OrderBy(r => r.Ref))
            {
                var sheetBorder = new Border { Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)), Padding = new Thickness(12), CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 0, 0, 8), BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)) };
                var sheetStack = new StackPanel();
                sheetStack.Children.Add(new TextBlock { Text = $"{result.Ref}: {result.L:N0} × {result.W:N0}mm", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)) });
                sheetStack.Children.Add(new TextBlock { Text = $"Area: {result.Area:N3} m² | U: {result.Util:N2}% | W: {result.Waste:N2}%", Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)), FontSize = 11 });
                sheetBorder.Child = sheetStack;
                spReportDetails.Children.Add(sheetBorder);
            }

            var totalBorder = new Border { Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)), Padding = new Thickness(15), CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 10, 0, 0) };
            totalBorder.Child = new TextBlock { Text = $"TOTAL: {_results.Count(r => r.Ref != "TOTAL")} sheets used | {_engine.TotalPartsCut} parts | {_engine.UsedSQM:N2} m²", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
            spReportDetails.Children.Add(totalBorder);

            var totalCost = _services.CalculateCost(_engine.UsedSQM, _results.ToList(), _engine.GetRemnants());
            if (txtTotalCost != null) txtTotalCost.Text = $"AED {Math.Max(0, totalCost):N2}";
            if (txtRemnants != null) txtRemnants.Text = $"{_engine.GetRemnants().Count} remnants available";

            if (txtTotalSheetsUsed != null) txtTotalSheetsUsed.Text = totalSheets.ToString(CultureInfo.InvariantCulture);
            if (txtAvgUtilization != null) txtAvgUtilization.Text = $"{avgUtil:N2}%";
            if (txtTotalStats != null) txtTotalStats.Text = $"{totalSheets} sheets, {totalParts} parts";
        }

        private void UpdateResultsGrouping()
        {
            // Fix: previously grouped results set L/W=0 and Waste=100-AvgUtil, breaking the report columns.
            var groupedResults = new List<OptimizationResult>();

            var grouped = _results
                .Where(r => r.Ref != "TOTAL")
                .GroupBy(r => new { r.L, r.W, r.SheetRef })
                .Select(g => new
                {
                    g.Key.L,
                    g.Key.W,
                    Ref = g.Key.SheetRef,
                    Count = g.Count(),
                    TotalArea = g.Sum(x => x.Area),
                    AvgUtil = g.Average(x => x.Util),
                    AvgWaste = g.Average(x => x.Waste)
                })
                .OrderByDescending(x => x.TotalArea)
                .ToList();

            foreach (var g in grouped)
            {
                groupedResults.Add(new OptimizationResult
                {
                    Ref = $"{g.Ref} ({g.Count}x)",
                    SheetRef = g.Ref,
                    SheetNum = 0,
                    L = g.L,
                    W = g.W,
                    Used = g.Count,
                    Area = g.TotalArea,
                    Util = g.AvgUtil,
                    Waste = g.AvgWaste
                });
            }

            if (icResults != null)
                icResults.ItemsSource = groupedResults;
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            TryParseDoubleFromTextBox(txtLR, out _lr);
            TryParseDoubleFromTextBox(txtBR, out _br);
            TryParseDoubleFromTextBox(txtTR, out _tr);
            TryParseDoubleFromTextBox(txtRM, out _rm);
            TryParseDoubleFromTextBox(txtKerf, out _kerf);
            TryParseDoubleFromTextBox(txtBreakout, out _breakout);

            _rotationPolicy = GetRotationPolicyFromUI();

            _engine.Configure(_lr, _rm, _tr, _br, _kerf, _breakout);
            _engine.SetRotationPolicy(_rotationPolicy);

            MessageBox.Show("Settings saved.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OptimizationView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5) RunOptimization_Click(sender, e);
            else if (e.Key == Key.Escape && _isSimulating)
            {
                _isSimulating = false;
                _simulateTimer.Stop();
                if (btnSimulate != null) btnSimulate.Content = "Start Simulation";
            }
            else if (e.Key == Key.Add && Keyboard.Modifiers == ModifierKeys.Control) ZoomIn_Click(sender, e);
            else if (e.Key == Key.Subtract && Keyboard.Modifiers == ModifierKeys.Control) ZoomOut_Click(sender, e);
        }

        private void LayoutCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                _zoomLevel = e.Delta > 0 ? Math.Min(_zoomLevel + 0.05, 2.5) : Math.Max(_zoomLevel - 0.05, 0.3);
                if (txtZoom != null) txtZoom.Text = $"{(_zoomLevel * 100):N0}%";
                DrawSingleSheetLayout(_currentIndex);
                e.Handled = true;
            }
        }

        private bool ValidateInputs()
        {
            if (_stockSheets.Count == 0)
            {
                MessageBox.Show("Please add stock sheets.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (_cutParts.Count == 0)
            {
                MessageBox.Show("Please add parts.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Keep existing warning behavior, but guard “unlimited” sentinel
            bool hasUnlimited = _stockSheets.Any(s => s.Qty >= UnlimitedQtySentinel);
            if (!hasUnlimited && _stockSheets.Sum(s => s.Qty) < _cutParts.Sum(p => p.Qty))
                MessageBox.Show("Warning: Not enough stock for all parts.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

            return true;
        }

        private string FormatSize(double mm) => mm >= 1000 ? $"{mm / 1000:N1}m" : $"{mm:N0}mm";
        private string FormatArea(double sqm) => sqm >= 1 ? $"{sqm:N2} m²" : $"{sqm * 10000:N0} cm²";

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
                Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
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

        public double CalculateCost(double usedSQM, List<OptimizationResult> results, List<RemnantPiece> remnants)
        {
            // Fix: keep method but delegate to service to avoid divergence (preserves workflow & signature)
            return _services.CalculateCost(usedSQM, results, remnants);
        }

        private void StartSimulation_Click(object sender, RoutedEventArgs e)
        {
            if (_isSimulating)
            {
                _isSimulating = false;
                _simulateTimer.Stop();
                if (btnSimulate != null) btnSimulate.Content = "Start Simulation";
                return;
            }

            if (_stockSheets.Count == 0 || _cutParts.Count == 0)
            {
                MessageBox.Show("Add stock and parts first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _isSimulating = true;
            if (btnSimulate != null) btnSimulate.Content = "Stop Simulation";

            _simulateTimer.Start();
        }

        private void SimulateTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isSimulating) return;

            var validResults = _results.Where(r => r.Ref != "TOTAL").ToList();
            if (validResults.Count == 0)
            {
                _simulateTimer.Stop();
                _isSimulating = false;
                if (btnSimulate != null) btnSimulate.Content = "Start Simulation";
                return;
            }

            DrawSingleSheetLayout(_currentIndex);

            if (_currentIndex < validResults.Count - 1)
                _currentIndex++;
            else
                _currentIndex = 0;
        }

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

            try
            {
                if (txtLR != null) txtLR.Text = _lr.ToString(CultureInfo.CurrentCulture);
                if (txtRM != null) txtRM.Text = _rm.ToString(CultureInfo.CurrentCulture);
                if (txtTR != null) txtTR.Text = _tr.ToString(CultureInfo.CurrentCulture);
                if (txtBR != null) txtBR.Text = _br.ToString(CultureInfo.CurrentCulture);
                if (txtKerf != null) txtKerf.Text = _kerf.ToString(CultureInfo.CurrentCulture);
                if (txtBreakout != null) txtBreakout.Text = _breakout.ToString(CultureInfo.CurrentCulture);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OptimizationView] SetTrimSettings Error: {ex.Message}");
            }
        }

        public void ImportInvoiceItems(List<CutPart> items)
        {
            _cutParts.Clear();
            if (items == null) return;

            foreach (var item in items)
            {
                _cutParts.Add(item);
            }
            UpdatePartsSummary();

            // --- SYNC FIX: Auto-run optimization if stock is available ---
            if (_stockSheets.Count > 0 && _cutParts.Count > 0)
            {
                RunOptimizationFromInvoice();
            }
        }

        public void RunOptimizationFromInvoice()
        {
            if (_stockSheets.Count == 0 || _cutParts.Count == 0)
            {
                return;
            }

            _engine.Configure(_lr, _rm, _tr, _br, _kerf, _breakout);
            _engine.SetRotationPolicy(_rotationPolicy);

            try
            {
                _engine.ExecuteNesting(_stockSheets.ToList(), _cutParts.ToList(), _results, _allPlacedParts, _savedJobs);
                UpdateReportSection();
                UpdateResultsGrouping();

                ShowTab("Layouts");
                _currentIndex = 0;
                if (cmbSheetSelector != null) cmbSheetSelector.SelectedIndex = 0;
                DrawCurrentLayout(_currentIndex);
                DrawSingleSheetLayout(_currentIndex);
                UpdateLayoutCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SetStockSheet(double width, double height)
        {
            int idx = _stockSheets.Count + 1;
            _stockSheets.Add(new StockSheet { Ref = $"S{idx}", L = width, W = height, Qty = UnlimitedQtySentinel });
            UpdateStockSummary();
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
            UpdatePartsSummary();

            // --- SYNC FIX: Auto-run optimization if stock is available ---
            if (_stockSheets.Count > 0 && _cutParts.Count > 0)
            {
                RunOptimizationFromInvoice();
            }
        }

        private void LayoutCanvas_MouseRightClick(object sender, MouseButtonEventArgs e)
        {
            // Fix: previously created a menu with no click handlers & no placement target.
            if (LayoutCanvas == null) return;

            var contextMenu = new ContextMenu
            {
                PlacementTarget = LayoutCanvas,
                Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint
            };

            var copyItem = new MenuItem { Header = "Copy Layout Image" };
            copyItem.Click += (_, __) =>
            {
                try
                {
                    var bmp = RenderLayoutToBitmap();
                    if (bmp != null)
                    {
                        Clipboard.SetImage(bmp);
                        MessageBox.Show("Layout image copied to clipboard.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Copy failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            contextMenu.Items.Add(copyItem);

            var exportItem = new MenuItem { Header = "Export Layout PNG" };
            exportItem.Click += (_, __) =>
            {
                try
                {
                    var bmp = RenderLayoutToBitmap();
                    if (bmp == null) return;

                    var dlg = new SaveFileDialog
                    {
                        Filter = "PNG Image|*.png",
                        FileName = $"Layout_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                    };

                    if (dlg.ShowDialog() == true)
                    {
                        using var fs = new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write);
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bmp));
                        encoder.Save(fs);

                        MessageBox.Show($"Exported:\n{dlg.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            contextMenu.Items.Add(exportItem);

            contextMenu.Items.Add(new Separator());

            var closeItem = new MenuItem { Header = "Close" };
            closeItem.Click += (_, __) => contextMenu.IsOpen = false;
            contextMenu.Items.Add(closeItem);

            contextMenu.IsOpen = true;
            e.Handled = true;
        }

        private BitmapSource? RenderLayoutToBitmap()
        {
            if (LayoutCanvas == null) return null;

            // Render the visible LayoutCanvas content
            var bounds = VisualTreeHelper.GetDescendantBounds(LayoutCanvas);
            if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0) return null;

            int width = (int)Math.Ceiling(bounds.Width);
            int height = (int)Math.Ceiling(bounds.Height);

            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

            var dv = new DrawingVisual();
            using (var ctx = dv.RenderOpen())
            {
                var vb = new VisualBrush(LayoutCanvas);
                ctx.DrawRectangle(vb, null, new Rect(new Point(0, 0), new Size(width, height)));
            }

            rtb.Render(dv);
            return rtb;
        }
    }
}