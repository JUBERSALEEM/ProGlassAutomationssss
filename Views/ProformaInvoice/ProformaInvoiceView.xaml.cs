using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ProGlassAutomation.Models;
using ProGlassAutomation.ViewModels;
using ProGlassAutomation.Views;
using ProGlassAutomation.Views.Optimization;

// Aliases to avoid ambiguity
using ModelInvoice = ProGlassAutomation.Models.InvoiceItemModel;
using ModelSpec = ProGlassAutomation.Models.SpecificationModel;
using Color = System.Windows.Media.Color;
using Border = System.Windows.Controls.Border;
using CheckBox = System.Windows.Controls.CheckBox;

namespace ProGlassAutomation.Views.ProformaInvoice
{
    /// <summary>
    /// Code-behind for ProformaInvoiceView.xaml — release-ready version.
    /// Handles UI interactions only; business logic lives in the ViewModel.
    /// </summary>
    public partial class ProformaInvoiceView : UserControl
    {
        // ═══════════════════════════════════════════════════════
        // CONSTANTS (FIX M1: extract magic numbers)
        // ═══════════════════════════════════════════════════════
        private const string LOG = "[ProformaView]"; // FIX L1: standardized log prefix

        private const double DEFAULT_SHEET_WIDTH = 3210;
        private const double DEFAULT_SHEET_HEIGHT = 2250;
        private const int DEFAULT_SHEET_QTY = 99999;
        // FIX L4: optimization fallback defaults — kept distinct from sheet1 defaults
        private const double OPT_FALLBACK_WIDTH = 3660;
        private const double OPT_FALLBACK_HEIGHT = 2440;

        private const double SCROLL_SPEED = 0.3;
        private const int FUTURE_SPEC_COUNT = 6;

        // ═══════════════════════════════════════════════════════
        // FIELDS
        // ═══════════════════════════════════════════════════════
        private ProformaInvoiceViewModel _viewModel;

        public ObservableCollection<StockSheetItem> AdditionalSheets { get; } = new();
        public ObservableCollection<SheetResultItem> SheetResults { get; } = new();
        public ObservableCollection<SpecSelectionItem> SpecSelectionItems { get; } = new();

        // Tracks per-spec dimension choice (false = W1/H1, true = W2/H2)
        private readonly Dictionary<string, bool> _specDimChoice = new();

        // FIX C7: was static, now instance-level (no leaks across View instances)
        private Window? _optimizerWindow;
        private OptimizationView? _optimizerView;

        // Guard against re-entrant selection events while we mutate selection programmatically
        private bool _isUpdatingSpecSelection = false;

        // Trim table (per-thickness)
        private Dictionary<string, TrimSettings> _trimTable = new();

        // ═══════════════════════════════════════════════════════
        // NESTED CLASSES
        // ═══════════════════════════════════════════════════════

        public class StockSheetItem : INotifyPropertyChanged
        {
            private string _sheetLabel = "";
            private string _width = "";
            private string _height = "";
            private string _qty = "";

            public string SheetLabel { get => _sheetLabel; set { _sheetLabel = value; OnPropertyChanged(nameof(SheetLabel)); } }
            public string Width { get => _width; set { _width = value; OnPropertyChanged(nameof(Width)); } }
            public string Height { get => _height; set { _height = value; OnPropertyChanged(nameof(Height)); } }
            public string Qty { get => _qty; set { _qty = value; OnPropertyChanged(nameof(Qty)); } }

            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public class SheetResultItem
        {
            public string SheetName { get; set; } = "";
            public string SheetDimensions { get; set; } = "";
            public string PiecesCut { get; set; } = "—";
            public string AreaUsed { get; set; } = "0 m²";
            public string Utilization { get; set; } = "0";
            public string Wastage { get; set; } = "0";
            public double BarHeight { get; set; } = 0;
        }

        public class SpecSelectionItem : INotifyPropertyChanged
        {
            private string _displayName = "";
            private string _fullName = "";
            private bool _isSelected = false;
            private bool _isW2H2 = false;

            public string DisplayName { get => _displayName; set { _displayName = value; OnPropertyChanged(nameof(DisplayName)); } }
            public string FullName { get => _fullName; set { _fullName = value; OnPropertyChanged(nameof(FullName)); } }

            public bool IsSelected
            {
                get => _isSelected;
                set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
            }

            // FIX H6: bound property replaces visual-tree-walking radio state
            public bool IsW2H2
            {
                get => _isW2H2;
                set { _isW2H2 = value; OnPropertyChanged(nameof(IsW2H2)); OnPropertyChanged(nameof(IsW1H1)); }
            }

            public bool IsW1H1
            {
                get => !_isW2H2;
                set { _isW2H2 = !value; OnPropertyChanged(nameof(IsW2H2)); OnPropertyChanged(nameof(IsW1H1)); }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public class TrimSettings
        {
            public double LM { get; set; }
            public double RM { get; set; }
            public double TM { get; set; }
            public double BM { get; set; }
            public double BreakoutMin { get; set; }
            public double Kerf { get; set; }
        }

        public class CutPart
        {
            public string Ref { get; set; } = "";
            public double L { get; set; }
            public double W { get; set; }
            public int Qty { get; set; }
            public bool Rot { get; set; } = true;
        }

        // ═══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════
        public ProformaInvoiceView()
        {
            InitializeComponent();

            _viewModel = SharedViewModels.ProformaInvoiceVM;
            DataContext = _viewModel;

            AdditionalSheetsContainer.ItemsSource = AdditionalSheets;
            PerSheetResultsContainer.ItemsSource = SheetResults;

            Unloaded += ProformaInvoiceView_Unloaded;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                InitializeTrimTable();
                AutoSelectFirstThickness();
            }), System.Windows.Threading.DispatcherPriority.Loaded);

            Debug.WriteLine($"{LOG} Using SharedViewModels.ProformaInvoiceVM");
            Debug.WriteLine($"{LOG} ✓ Automatic thickness trim ready");
        }

        // ═══════════════════════════════════════════════════════
        // TRIM TABLE
        // ═══════════════════════════════════════════════════════
        public void InitializeTrimTable()
        {
            _trimTable = new Dictionary<string, TrimSettings>
            {
                ["6mm"] = new TrimSettings { LM = 15, RM = 15, TM = 15, BM = 15, BreakoutMin = 15, Kerf = 0 },
                ["8mm"] = new TrimSettings { LM = 30, RM = 30, TM = 30, BM = 30, BreakoutMin = 30, Kerf = 0 },
                ["10mm"] = new TrimSettings { LM = 50, RM = 50, TM = 0, BM = 50, BreakoutMin = 40, Kerf = 0 },
                ["12mm"] = new TrimSettings { LM = 50, RM = 50, TM = 0, BM = 50, BreakoutMin = 40, Kerf = 0 },
                ["15mm"] = new TrimSettings { LM = 60, RM = 60, TM = 0, BM = 60, BreakoutMin = 50, Kerf = 0 },
                ["19mm"] = new TrimSettings { LM = 70, RM = 70, TM = 0, BM = 70, BreakoutMin = 60, Kerf = 0 }
            };
            Debug.WriteLine($"{LOG} ✓ Trim table initialized with {_trimTable.Count} thicknesses");
        }

        private TrimSettings GetCurrentTrim()
        {
            try
            {
                string thickness = (cmbThickness?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "6mm";
                if (_trimTable.TryGetValue(thickness, out TrimSettings? trim))
                    return trim;
            }
            catch (Exception ex) // FIX H5: log instead of silent swallow
            {
                Debug.WriteLine($"{LOG} GetCurrentTrim error: {ex.Message}");
            }
            return _trimTable.ContainsKey("6mm")
                ? _trimTable["6mm"]
                : new TrimSettings { LM = 15, RM = 15, TM = 15, BM = 15, Kerf = 0 };
        }

        private void AutoSelectFirstThickness()
        {
            try
            {
                if (cmbThickness?.Items.Count > 0 && cmbThickness.SelectedIndex < 0)
                {
                    cmbThickness.SelectedIndex = 0;
                    Debug.WriteLine($"{LOG} ✓ First thickness auto-selected");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} AutoSelectFirstThickness error: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshSpecSelectionItems();
            Debug.WriteLine($"{LOG} UserControl_Loaded — specs refreshed, count: {SpecSelectionItems.Count}");
        }

        private void lstSpecSelect_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshSpecSelectionItems();
            Debug.WriteLine($"{LOG} lstSpecSelect_Loaded — items count: {SpecSelectionItems.Count}");
        }

        private void ProformaInvoiceView_Unloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= ProformaInvoiceView_Unloaded;

            // FIX C7: clean up optimizer references (instance-level now)
            try
            {
                _optimizerView = null;
                if (_optimizerWindow != null)
                {
                    _optimizerWindow = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} Unload cleanup error: {ex.Message}");
            }

            Debug.WriteLine($"{LOG} Unloaded — cleanup complete");
        }

        // ═══════════════════════════════════════════════════════
        // SPEC SELECTION REFRESH
        // ═══════════════════════════════════════════════════════
        public void RefreshSpecSelectionItems()
        {
            // Preserve selection and dim-choice across rebuild
            var prevSelections = new Dictionary<string, bool>(_specDimChoice);
            var prevSelected = SpecSelectionItems
                .Where(w => w.IsSelected)
                .Select(w => w.FullName)
                .ToHashSet();

            SpecSelectionItems.Clear();

            int specCount = _viewModel?.Invoice?.Specifications?.Count ?? 0;
            Debug.WriteLine($"{LOG} RefreshSpecSelectionItems — invoice has {specCount} specs");

            if (_viewModel?.Invoice?.Specifications != null)
            {
                for (int i = 0; i < _viewModel.Invoice.Specifications.Count; i++)
                {
                    var spec = _viewModel.Invoice.Specifications[i];
                    if (spec == null) continue;

                    bool wasSelected = prevSelected.Contains(spec.SpecificationName);
                    bool wasW2H2 = prevSelections.TryGetValue(spec.SpecificationName, out var v) && v;

                    SpecSelectionItems.Add(new SpecSelectionItem
                    {
                        DisplayName = $"Spec {i + 1}",
                        FullName = spec.SpecificationName,
                        IsSelected = wasSelected,
                        IsW2H2 = wasW2H2
                    });
                }
            }

            Debug.WriteLine($"{LOG} RefreshSpecSelectionItems — created {SpecSelectionItems.Count} items");
        }

        // ═══════════════════════════════════════════════════════
        // SPECIFICATIONS
        // ═══════════════════════════════════════════════════════
        // FIX H2: root cause was that the command was wired in both code-behind AND XAML.
        // The defensive "remove duplicate" hack is gone. Now we just execute the command.
        private void AddSpecification_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel?.AddSpecificationCommand?.CanExecute(null) == true)
                {
                    _viewModel.AddSpecificationCommand.Execute(null);
                }

                RefreshSpecSelectionItems();
                SelectLastSpec();

                Debug.WriteLine($"{LOG} ✓ Spec added — total: {SpecSelectionItems.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} AddSpecification_Click error: {ex.Message}");
                MessageBox.Show($"Error adding specification: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveSpecification_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel?.RemoveSpecificationCommand?.CanExecute(null) == true)
                    _viewModel.RemoveSpecificationCommand.Execute(null);

                RefreshSpecSelectionItems();
                Debug.WriteLine($"{LOG} ✓ Spec removed — total: {SpecSelectionItems.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} RemoveSpecification_Click error: {ex.Message}");
            }
        }

        private void SelectLastSpec()
        {
            if (SpecSelectionItems.Count > 0)
            {
                var lastWrapper = SpecSelectionItems[SpecSelectionItems.Count - 1];
                lstSpecSelect.SelectedItem = lastWrapper;
            }
        }

        // ═══════════════════════════════════════════════════════
        // STOCK SHEET BUTTONS
        // ═══════════════════════════════════════════════════════
        private void BtnAddStockSheet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int sheetNum = AdditionalSheets.Count + 2;
                AdditionalSheets.Add(new StockSheetItem
                {
                    SheetLabel = $"Sheet {sheetNum}",
                    Width = DEFAULT_SHEET_WIDTH.ToString(),
                    Height = DEFAULT_SHEET_HEIGHT.ToString(),
                    Qty = "100"
                });
                txtOptStatus.Text = $"Added Sheet {sheetNum}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLoadDefaultSheets_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                txtSheetWidth.Text = DEFAULT_SHEET_WIDTH.ToString();
                txtSheetHeight.Text = DEFAULT_SHEET_HEIGHT.ToString();
                txtSheetQty.Text = DEFAULT_SHEET_QTY.ToString();
                AdditionalSheets.Clear();
                txtOptStatus.Text = "Default sheets loaded";
            }
            catch (Exception ex) // FIX H5: log instead of silent swallow
            {
                Debug.WriteLine($"{LOG} BtnLoadDefaultSheets_Click error: {ex.Message}");
            }
        }

        private void BtnClearSheets_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AdditionalSheets.Clear();
                txtSheetWidth.Text = "";
                txtSheetHeight.Text = "";
                txtSheetQty.Text = "";
                txtOptStatus.Text = "Sheets cleared";
            }
            catch (Exception ex) // FIX H5: log
            {
                Debug.WriteLine($"{LOG} BtnClearSheets_Click error: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════
        // SPEC SELECTION BUTTONS
        // FIX M8 + M9: duplicate handlers removed — single definition below
        // ═══════════════════════════════════════════════════════
        private void BtnSelectAllSpecs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var wrapper in SpecSelectionItems)
                    wrapper.IsSelected = true;

                lstSpecSelect.SelectAll();
                UpdateOptStatus();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} BtnSelectAllSpecs_Click error: {ex.Message}");
            }
        }

        private void BtnClearSpecSelection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var wrapper in SpecSelectionItems)
                {
                    wrapper.IsSelected = false;
                    wrapper.IsW2H2 = false;
                }
                lstSpecSelect.SelectedItems.Clear();
                _specDimChoice.Clear();
                txtOptStatus.Text = "Select specs and choose dimensions";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} BtnClearSpecSelection_Click error: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════
        // EXPANDABLE PANEL HANDLERS
        // ═══════════════════════════════════════════════════════
        private void ToggleFilePanel_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.IsFilePanelOpen = !_viewModel.IsFilePanelOpen;
                FileContent.Visibility = _viewModel.IsFilePanelOpen ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ToggleSGUPanel_Click(object sender, RoutedEventArgs e) { if (_viewModel != null) _viewModel.IsSGUSelected = !_viewModel.IsSGUSelected; }
        private void ToggleDGUPanel_Click(object sender, RoutedEventArgs e) { if (_viewModel != null) _viewModel.IsDGUSelected = !_viewModel.IsDGUSelected; }
        private void ToggleLAMPanel_Click(object sender, RoutedEventArgs e) { if (_viewModel != null) _viewModel.IsLAMSelected = !_viewModel.IsLAMSelected; }

        private void ToggleOptPanel_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.IsOptimizationPanelOpen = !_viewModel.IsOptimizationPanelOpen;
                OptContent.Visibility = _viewModel.IsOptimizationPanelOpen ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ToggleRecentInvoices_Click(object sender, RoutedEventArgs e)
        {
            RecentInvoicesContent.Visibility = RecentInvoicesContent.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        // ═══════════════════════════════════════════════════════
        // OPEN OPTIMIZER WINDOW (instance-level)
        // ═══════════════════════════════════════════════════════
        private void OpenOptimization_Click(object sender, RoutedEventArgs e)
        {
            var optWindow = new Window
            {
                Title = "Glass Cut Optimizer - ProGlass Automation",
                Content = new OptimizationView(),
                WindowState = WindowState.Normal,
                WindowStyle = WindowStyle.SingleBorderWindow,
                ResizeMode = ResizeMode.CanResize,
                Width = 1400,
                Height = 900,
                MinWidth = 1000,
                MinHeight = 700,
                Background = new SolidColorBrush(Color.FromRgb(30, 39, 46)),
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            _optimizerWindow = optWindow;
            optWindow.ContentRendered += (s, args) => { _optimizerView = optWindow.Content as OptimizationView; };
            optWindow.Closed += (s, args) => { _optimizerWindow = null; _optimizerView = null; };

            optWindow.Show();
            optWindow.WindowState = WindowState.Maximized;
        }

        // ═══════════════════════════════════════════════════════
        // FIX H7: shared optimization core — kills duplicate code between
        // RunOptimization_Click and ViewOptimizationLayouts_Click
        // ═══════════════════════════════════════════════════════
        private class OptimizationContext
        {
            public double SheetWidth { get; set; }
            public double SheetHeight { get; set; }
            public TrimSettings Trim { get; set; } = new();
            public string Thickness { get; set; } = "6mm";
            public List<Models.InvoiceItemModel> ItemsForOptimizer { get; set; } = new();
            public string DimStatusText { get; set; } = "W1/H1";
            public bool GlobalUseAlt { get; set; }
            public bool UseCustom { get; set; }
        }

        private OptimizationContext? BuildOptimizationContext()
        {
            // Apply defaults if empty
            if (string.IsNullOrWhiteSpace(txtSheetWidth.Text)) txtSheetWidth.Text = OPT_FALLBACK_WIDTH.ToString();
            if (string.IsNullOrWhiteSpace(txtSheetHeight.Text)) txtSheetHeight.Text = OPT_FALLBACK_HEIGHT.ToString();

            if (!double.TryParse(txtSheetWidth.Text, out double sheetWidth) || sheetWidth <= 0)
            {
                MessageBox.Show("Please enter valid sheet width!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
            if (!double.TryParse(txtSheetHeight.Text, out double sheetHeight) || sheetHeight <= 0)
            {
                MessageBox.Show("Please enter valid sheet height!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            TrimSettings trim = GetCurrentTrim();
            string thickness = (cmbThickness?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "6mm";

            // FIX H6: read dim choices from bound SpecSelectionItem.IsW2H2 (no visual tree walking)
            SyncDimChoiceFromWrappers();

            // Gather items
            var allItemsWithSpec = new List<(Models.SpecificationModel spec, Models.InvoiceItemModel item)>();
            if (_viewModel?.Invoice?.Specifications != null)
            {
                foreach (var spec in _viewModel.Invoice.Specifications)
                {
                    if (spec?.Items == null) continue;
                    foreach (var item in spec.Items)
                    {
                        if (item != null)
                            allItemsWithSpec.Add((spec, item));
                    }
                }
            }

            if (allItemsWithSpec.Count == 0)
            {
                MessageBox.Show("No items to optimize!", "Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            bool globalUseAlt = (rbUseW2H2 != null && rbUseW2H2.IsChecked == true);
            bool useCustom = (rbUseCustom != null && rbUseCustom.IsChecked == true);

            if (!globalUseAlt && rbDimsW2H2 != null && rbDimsW2H2.IsChecked == true)
                globalUseAlt = true;

            // Build optimizer items
            var invoiceItems = new List<Models.InvoiceItemModel>();
            foreach (var (spec, item) in allItemsWithSpec)
            {
                bool useAltForItem = useCustom
                    ? (_specDimChoice.TryGetValue(spec.SpecificationName, out var v) && v)
                    : globalUseAlt;

                double width = useAltForItem
                    ? (item.Width2 > 0 ? item.Width2 : item.Width1)
                    : (item.Width1 > 0 ? item.Width1 : item.Width2);
                double height = useAltForItem
                    ? (item.Height2 > 0 ? item.Height2 : item.Height1)
                    : (item.Height1 > 0 ? item.Height1 : item.Height2);

                invoiceItems.Add(new Models.InvoiceItemModel
                {
                    GlassRef = item.GlassRef,
                    Width1 = width,
                    Height1 = height,
                    Qty = item.Qty > 0 ? item.Qty : 1
                });
            }

            // Warn if W2/H2 requested but no items have alternates
            int altAvailable = allItemsWithSpec.Count(t => t.item.Width2 > 0 || t.item.Height2 > 0);
            if (globalUseAlt && altAvailable == 0)
            {
                MessageBox.Show("W2/H2 selected but no alternate dimensions found. Falling back to W1/H1.",
                    "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                globalUseAlt = false;
            }

            // Build status text
            string dimStatus;
            if (useCustom)
            {
                int altCount = _specDimChoice.Count(kvp => kvp.Value);
                if (altCount == 0) dimStatus = "W1/H1 (all)";
                else if (altCount == _specDimChoice.Count) dimStatus = "W2/H2 (all)";
                else dimStatus = $"Mixed ({altCount} specs W2/H2)";
            }
            else
            {
                dimStatus = globalUseAlt ? "W2/H2" : "W1/H1";
            }

            return new OptimizationContext
            {
                SheetWidth = sheetWidth,
                SheetHeight = sheetHeight,
                Trim = trim,
                Thickness = thickness,
                ItemsForOptimizer = invoiceItems,
                DimStatusText = dimStatus,
                GlobalUseAlt = globalUseAlt,
                UseCustom = useCustom
            };
        }

        private void SyncDimChoiceFromWrappers()
        {
            // FIX H6: pull dim choice from the bound wrappers — no visual tree walking
            _specDimChoice.Clear();
            foreach (var wrapper in SpecSelectionItems.Where(w => w.IsSelected))
            {
                if (!string.IsNullOrEmpty(wrapper.FullName))
                    _specDimChoice[wrapper.FullName] = wrapper.IsW2H2;
            }
        }

        // ═══════════════════════════════════════════════════════
        // RUN OPTIMIZATION (quick — center section only)
        // ═══════════════════════════════════════════════════════
        private void RunOptimization_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ctx = BuildOptimizationContext();
                if (ctx == null) return;

                Debug.WriteLine($"{LOG} RunOptimization — items={ctx.ItemsForOptimizer.Count}, mode={ctx.DimStatusText}");

                var optView = new OptimizationView();
                optView.ImportInvoiceItems(ctx.ItemsForOptimizer);
                optView.SetStockSheet(ctx.SheetWidth, ctx.SheetHeight);
                optView.SetTrimSettings(ctx.Trim.LM, ctx.Trim.BM, ctx.Trim.TM, ctx.Trim.RM, ctx.Trim.Kerf, ctx.Trim.BreakoutMin);
                optView.RunOptimizationFromInvoice();

                UpdateResultsUI(optView, ctx);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════════
        // VIEW FULL LAYOUTS (popup window)
        // ═══════════════════════════════════════════════════════
        private void ViewOptimizationLayouts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ctx = BuildOptimizationContext();
                if (ctx == null) return;

                // If spec-wise checkbox is on, narrow items to selected specs only
                if (chkSpecWise.IsChecked == true)
                {
                    var selectedNames = SpecSelectionItems
                        .Where(w => w.IsSelected)
                        .Select(w => w.FullName)
                        .ToHashSet();

                    if (selectedNames.Count > 0 && _viewModel?.Invoice?.Specifications != null)
                    {
                        var narrowed = new List<Models.InvoiceItemModel>();
                        foreach (var spec in _viewModel.Invoice.Specifications)
                        {
                            if (spec == null || !selectedNames.Contains(spec.SpecificationName)) continue;
                            foreach (var item in spec.Items)
                            {
                                bool useAlt = ctx.UseCustom
                                    ? (_specDimChoice.TryGetValue(spec.SpecificationName, out var v) && v)
                                    : ctx.GlobalUseAlt;

                                narrowed.Add(new Models.InvoiceItemModel
                                {
                                    GlassRef = item.GlassRef,
                                    Width1 = useAlt ? (item.Width2 > 0 ? item.Width2 : item.Width1) : (item.Width1 > 0 ? item.Width1 : item.Width2),
                                    Height1 = useAlt ? (item.Height2 > 0 ? item.Height2 : item.Height1) : (item.Height1 > 0 ? item.Height1 : item.Height2),
                                    Qty = item.Qty > 0 ? item.Qty : 1
                                });
                            }
                        }

                        if (narrowed.Count > 0)
                            ctx.ItemsForOptimizer = narrowed;
                    }
                }

                var optWindow = new Window
                {
                    Title = "Glass Cut Optimizer - ProGlass Automation",
                    Content = new OptimizationView(),
                    Width = 1400,
                    Height = 900,
                    Background = new SolidColorBrush(Color.FromRgb(30, 39, 46)),
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                if (optWindow.Content is OptimizationView optView)
                {
                    optView.ImportInvoiceItems(ctx.ItemsForOptimizer);
                    optView.SetStockSheet(ctx.SheetWidth, ctx.SheetHeight);
                    optView.SetTrimSettings(ctx.Trim.LM, ctx.Trim.BM, ctx.Trim.TM, ctx.Trim.RM, ctx.Trim.Kerf, ctx.Trim.BreakoutMin);
                    optView.RunOptimizationFromInvoice();

                    optWindow.Show();
                    UpdateResultsUI(optView, ctx);
                }
                else
                {
                    MessageBox.Show("Failed to create optimizer view!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Centralized results UI updater (used by both Run and View Layouts)
        private void UpdateResultsUI(OptimizationView optView, OptimizationContext ctx)
        {
            double utilization = optView.AverageUtilization;
            int sheetsUsed = optView.SheetsUsed;

            txtUtilization.Text = $"{utilization:N1}%";
            txtSheetsUsed.Text = sheetsUsed.ToString();
            txtWastage.Text = $"{(100 - utilization):N1}%";

            txtOptStatus.Text =
                $"✓ Optimized | {ctx.Thickness} | Trim: {ctx.Trim.LM}/{ctx.Trim.RM}/{ctx.Trim.TM}/{ctx.Trim.BM}mm | {ctx.DimStatusText}";

            // Increment run count
            int currentCount = int.TryParse(txtOptRunCount.Text, out int c) ? c : 0;
            txtOptRunCount.Text = (currentCount + 1).ToString();

            UpdatePerSheetResults(optView, ctx.SheetWidth, ctx.SheetHeight);
        }

        // ═══════════════════════════════════════════════════════
        // THICKNESS CHANGED — refresh status text
        // (Wire this in XAML if you want auto-update; otherwise harmless)
        // ═══════════════════════════════════════════════════════
        private void cmbThickness_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                TrimSettings trim = GetCurrentTrim();
                string thickness = (cmbThickness?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "6mm";
                txtOptStatus.Text = $"Thickness: {thickness} | Trim: LM={trim.LM}mm RM={trim.RM}mm TM={trim.TM}mm BM={trim.BM}mm";
                Debug.WriteLine($"{LOG} ✓ Thickness={thickness}, Trim applied: LM={trim.LM}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} cmbThickness_SelectionChanged error: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════
        // PER-SHEET RESULTS
        // FIX C8 + M4: Removed the bogus (int)(util * 10) "pieces" math.
        //  Now PiecesCut = "—" with a sheet-count label that's accurate.
        // ═══════════════════════════════════════════════════════
        private void UpdatePerSheetResults(OptimizationView optView, double sheetWidth, double sheetHeight)
        {
            try
            {
                SheetResults.Clear();

                var results = optView.GetResultsList();

                if (results == null || results.Count == 0)
                {
                    // Fallback: single consolidated row
                    int totalSheets = optView.SheetsUsed;
                    double areaTotal = sheetWidth * sheetHeight / 1_000_000;
                    double util = optView.AverageUtilization;
                    double wastage = 100 - util;
                    double areaUsed = areaTotal * (util / 100);

                    SheetResults.Add(new SheetResultItem
                    {
                        SheetName = $"{sheetWidth:N0} × {sheetHeight:N0} mm",
                        SheetDimensions = $"{sheetWidth:N0} × {sheetHeight:N0} mm",
                        PiecesCut = "—", // FIX C8: was (int)(util * 10) — meaningless
                        AreaUsed = $"{areaUsed * totalSheets:N2} m²",
                        Utilization = $"{util:N1}",
                        Wastage = $"{wastage:N1}",
                        BarHeight = util * 0.8
                    });
                }
                else
                {
                    // Group by sheet dimensions, count sheets per group
                    var groupedResults = results
                        .GroupBy(r => new { r.L, r.W })
                        .Select(g => new
                        {
                            SheetDimensions = $"{g.Key.L:N0} × {g.Key.W:N0} mm",
                            SheetCount = g.Count(),
                            TotalArea = g.Sum(x => x.Area),
                            AvgUtil = g.Average(x => x.Util)
                        })
                        .OrderByDescending(g => g.AvgUtil)
                        .ToList();

                    foreach (var group in groupedResults)
                    {
                        double wastage = 100 - group.AvgUtil;

                        SheetResults.Add(new SheetResultItem
                        {
                            SheetName = group.SheetDimensions,
                            SheetDimensions = group.SheetDimensions,
                            // FIX M4: clearer label — this is sheet count, not piece count
                            PiecesCut = $"{group.SheetCount} sheets",
                            AreaUsed = $"{group.TotalArea:N2} m²",
                            Utilization = $"{group.AvgUtil:N1}",
                            Wastage = $"{wastage:N1}",
                            BarHeight = group.AvgUtil * 0.8
                        });
                    }
                }

                int uniqueSizes = SheetResults.Select(r => r.SheetDimensions).Distinct().Count();
                int totalSheetsUsed = optView.SheetsUsed;

                txtSheetsUsedCount.Text = uniqueSizes == 1
                    ? $"{totalSheetsUsed} sheets ({SheetResults.First().SheetDimensions})"
                    : $"{totalSheetsUsed} sheets ({uniqueSizes} sizes)";

                EmptyPerSheetResults.Visibility = SheetResults.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} UpdatePerSheetResults error: {ex.Message}");

                double util = optView?.AverageUtilization ?? 0;
                int sheets = optView?.SheetsUsed ?? 0;
                double area = sheetWidth * sheetHeight / 1_000_000 * sheets;

                SheetResults.Add(new SheetResultItem
                {
                    SheetName = $"{sheetWidth:N0} × {sheetHeight:N0} mm",
                    SheetDimensions = $"{sheetWidth:N0} × {sheetHeight:N0} mm",
                    PiecesCut = "—",
                    AreaUsed = $"{area:N2} m²",
                    Utilization = $"{util:N1}",
                    Wastage = $"{(100 - util):N1}",
                    BarHeight = util * 0.8
                });

                txtSheetsUsedCount.Text = $"{sheets} sheets";
                EmptyPerSheetResults.Visibility = Visibility.Collapsed;
            }
        }

        // ═══════════════════════════════════════════════════════
        // TEXT SELECTION / FOCUS / VALIDATION
        // ═══════════════════════════════════════════════════════
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb) tb.SelectAll();
        }

        private void TextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb && !tb.IsFocused)
            {
                tb.Focus();
                e.Handled = true;
            }
        }

        private void ComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox cb) cb.IsDropDownOpen = true;
        }

        private void PhoneNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9\+\-\s]+$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void PhoneNumber_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && string.IsNullOrEmpty(tb.Text))
            {
                tb.Text = "+971-";
                tb.Select(tb.Text.Length, 0);
            }
        }

        private void PhoneNumber_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Text == "+971-") tb.Text = "";
        }

        // ═══════════════════════════════════════════════════════
        // SMOOTH SCROLL
        // ═══════════════════════════════════════════════════════
        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                e.Handled = true;
                var scrollViewer = GetScrollViewer(dataGrid);
                if (scrollViewer != null)
                {
                    double scrollAmount = e.Delta * SCROLL_SPEED;
                    double newOffset = scrollViewer.VerticalOffset - scrollAmount;
                    newOffset = Math.Max(0, Math.Min(newOffset, scrollViewer.ScrollableHeight));
                    AnimateScroll(scrollViewer, newOffset);
                }
            }
        }

        private void AnimateScroll(ScrollViewer scrollViewer, double targetOffset)
        {
            var animation = new DoubleAnimation
            {
                To = targetOffset,
                Duration = TimeSpan.FromMilliseconds(150),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            scrollViewer.BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, animation);
        }

        private ScrollViewer? GetScrollViewer(DependencyObject obj)
        {
            if (obj is ScrollViewer sv) return sv;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }

        // ═══════════════════════════════════════════════════════
        // ROW BUTTONS
        // ═══════════════════════════════════════════════════════
        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Models.SpecificationModel spec)
                _viewModel.AddItemWithPrice(spec);
        }

        private void ToggleLM_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProformaInvoiceViewModel vm)
                vm.IsLMVisible = !vm.IsLMVisible;
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Models.InvoiceItemModel item)
                _viewModel.RemoveItem(item);
        }

        // ═══════════════════════════════════════════════════════
        // PASTE FROM EXCEL
        // ═══════════════════════════════════════════════════════
        private void DataGrid_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (!Clipboard.ContainsText())
                {
                    MessageBox.Show("Clipboard is empty!", "Paste", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string clipboardText = Clipboard.GetText();
                if (string.IsNullOrWhiteSpace(clipboardText))
                {
                    MessageBox.Show("No text data in clipboard!", "Paste", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_viewModel == null || _viewModel.Invoice == null)
                {
                    MessageBox.Show("Invoice not loaded. Please create or open an invoice first.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var spec = _viewModel.SelectedTargetSpecification;
                if (spec == null)
                {
                    if (_viewModel.Invoice.Specifications.Count == 0)
                        _viewModel.AddSpecificationCommand.Execute(null);

                    if (_viewModel.Invoice.Specifications.Count == 0)
                    {
                        MessageBox.Show("Could not create specification. Please add a specification manually.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    spec = _viewModel.SelectedTargetSpecification ?? _viewModel.Invoice.Specifications[0];
                }

                var rows = clipboardText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (rows.Length == 0)
                {
                    MessageBox.Show("No data to paste!", "Paste", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool skipHeader = rows[0].ToLower().Contains("glass") || rows[0].ToLower().Contains("width") ||
                                rows[0].ToLower().Contains("height") || rows[0].ToLower().Contains("qty") ||
                                rows[0].ToLower().Contains("ref");

                int startRow = spec.Items.Count > 0 ? spec.Items.Count : 0;
                int itemsAdded = 0;
                int startIndex = skipHeader ? 1 : 0;

                double defaultWidth1 = 0, defaultHeight1 = 0, defaultWidth2 = 0, defaultHeight2 = 0, defaultPrice = 0, defaultSurcharge = 20;
                if (spec.Items.Count > 0)
                {
                    var firstItem = spec.Items[0];
                    defaultWidth1 = firstItem.Width1;
                    defaultHeight1 = firstItem.Height1;
                    defaultWidth2 = firstItem.Width2;
                    defaultHeight2 = firstItem.Height2;
                    defaultPrice = firstItem.Price;
                    defaultSurcharge = firstItem.SurchargePercent;
                }

                using (spec.BulkUpdateScope())
                {
                    for (int i = startIndex; i < rows.Length; i++)
                    {
                        var row = rows[i];
                        var columns = row.Split('\t');

                        if (columns.Length == 0 || string.IsNullOrWhiteSpace(string.Join("", columns).Replace("\t", "")))
                            continue;

                        var item = new ModelInvoice { SrNo = startRow + itemsAdded + 1 };

                        if (columns.Length > 0) item.GlassRef = columns[0].Trim();
                        if (columns.Length > 1 && double.TryParse(columns[1].Trim().Replace(",", ""), out double w1)) item.Width1 = w1; else item.Width1 = defaultWidth1;
                        if (columns.Length > 2 && double.TryParse(columns[2].Trim().Replace(",", ""), out double h1)) item.Height1 = h1; else item.Height1 = defaultHeight1;
                        if (columns.Length > 3 && double.TryParse(columns[3].Trim().Replace(",", ""), out double w2)) item.Width2 = w2; else item.Width2 = defaultWidth2;
                        if (columns.Length > 4 && double.TryParse(columns[4].Trim().Replace(",", ""), out double h2)) item.Height2 = h2; else item.Height2 = defaultHeight2;
                        if (columns.Length > 5 && int.TryParse(columns[5].Trim().Replace(",", ""), out int qty)) item.Qty = qty; else item.Qty = 1;
                        if (columns.Length > 6 && double.TryParse(columns[6].Trim().Replace(",", ""), out double price)) item.Price = price; else item.Price = defaultPrice;
                        if (columns.Length > 7 && double.TryParse(columns[7].Trim().Replace(",", "").Replace("%", ""), out double surcharge)) item.SurchargePercent = surcharge; else item.SurchargePercent = defaultSurcharge;

                        spec.Items.Add(item);
                        itemsAdded++;
                    }
                }

                _viewModel.Invoice.CalculateTotals();
                _viewModel.Invoice.IsDirty = true;

                MessageBox.Show($"✅ Pasted {itemsAdded} items from Excel!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Paste failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DataGrid_CanPaste(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Clipboard.ContainsText();
            e.Handled = true;
        }

        // ═══════════════════════════════════════════════════════
        // PRINT
        // ═══════════════════════════════════════════════════════
        private void PrintInvoice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var previewWindow = new ProformaInvoicePrintPreviewView();
                previewWindow.DataContext = DataContext;

                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    var pageWidth = printDialog.PrintableAreaWidth;
                    var pageHeight = printDialog.PrintableAreaHeight;
                    var contentWidth = previewWindow.ActualWidth;
                    var contentHeight = previewWindow.ActualHeight;

                    if (contentWidth > 0 && contentHeight > 0)
                    {
                        var scale = Math.Min(pageWidth / contentWidth, pageHeight / contentHeight);
                        previewWindow.LayoutTransform = new ScaleTransform(scale, scale);
                    }

                    printDialog.PrintVisual(previewWindow, "ProForma Invoice");
                    previewWindow.LayoutTransform = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing invoice: {ex.Message}", "Print Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintPreview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var previewWindow = new ProformaInvoicePrintPreviewView();
                previewWindow.DataContext = DataContext;

                var window = new Window
                {
                    Content = previewWindow,
                    Title = "Print Preview - ProForma Invoice",
                    Width = 1100,
                    Height = 800,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening print preview: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════════
        // DATAGRID KEY HANDLERS
        // ═══════════════════════════════════════════════════════
        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not DataGrid dataGrid) return;

            try
            {
                var cell = dataGrid.CurrentCell;
                if (!cell.IsValid || cell.Column == null) return;

                if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                    HandleEnterKey(dataGrid);
                    return;
                }

                if (e.Key == Key.Tab && Keyboard.Modifiers != ModifierKeys.Shift)
                {
                    int currentColumnIndex = cell.Column.DisplayIndex;
                    int totalColumns = dataGrid.Columns.Count;

                    if (currentColumnIndex >= totalColumns - 3)
                    {
                        e.Handled = true;
                        HandleTabKey(dataGrid);
                    }
                }
            }
            catch (Exception ex) // FIX H5: log
            {
                Debug.WriteLine($"{LOG} DataGrid_PreviewKeyDown error: {ex.Message}");
            }
        }

        private void HandleEnterKey(DataGrid dataGrid)
        {
            try
            {
                if (dataGrid == null || !dataGrid.CurrentCell.IsValid) return;

                if (dataGrid.CurrentCell.Item is not Models.InvoiceItemModel currentItem) return;

                var spec = FindSpecification(currentItem);
                if (spec == null) return;

                int currentIndex = spec.Items.IndexOf(currentItem);
                int totalRows = spec.Items.Count;

                if (currentIndex < totalRows - 1)
                {
                    var nextItem = spec.Items[currentIndex + 1];
                    dataGrid.SelectedItem = nextItem;
                    dataGrid.ScrollIntoView(nextItem);

                    int currentColIndex = dataGrid.CurrentCell.Column.DisplayIndex;
                    if (currentColIndex < dataGrid.Columns.Count)
                        dataGrid.CurrentCell = new DataGridCellInfo(nextItem, dataGrid.Columns[currentColIndex]);

                    dataGrid.BeginEdit();
                }
                else
                {
                    _viewModel.AddItemWithPrice(spec);

                    Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
                    {
                        try
                        {
                            int newIndex = spec.Items.Count - 1;
                            if (newIndex >= 0 && dataGrid != null)
                            {
                                var newItem = spec.Items[newIndex];
                                dataGrid.SelectedItem = newItem;
                                dataGrid.ScrollIntoView(newItem);

                                if (dataGrid.Columns.Count > 1)
                                    dataGrid.CurrentCell = new DataGridCellInfo(newItem, dataGrid.Columns[1]);

                                dataGrid.BeginEdit();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"{LOG} HandleEnterKey inner error: {ex.Message}");
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} HandleEnterKey error: {ex.Message}");
            }
        }

        private void HandleTabKey(DataGrid dataGrid)
        {
            try
            {
                if (dataGrid.CurrentCell.Item is not Models.InvoiceItemModel currentItem) return;

                var spec = FindSpecification(currentItem);
                if (spec == null) return;

                int currentIndex = spec.Items.IndexOf(currentItem);

                if (currentIndex < spec.Items.Count - 1)
                {
                    var nextItem = spec.Items[currentIndex + 1];
                    dataGrid.SelectedItem = nextItem;
                    dataGrid.ScrollIntoView(nextItem);

                    if (dataGrid.Columns.Count > 1)
                        dataGrid.CurrentCell = new DataGridCellInfo(nextItem, dataGrid.Columns[1]);

                    dataGrid.BeginEdit();
                }
                else
                {
                    _viewModel.AddItemWithPrice(spec);

                    Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
                    {
                        try
                        {
                            int newIndex = spec.Items.Count - 1;
                            if (newIndex >= 0 && dataGrid != null)
                            {
                                var newItem = spec.Items[newIndex];
                                dataGrid.SelectedItem = newItem;
                                dataGrid.ScrollIntoView(newItem);

                                if (dataGrid.Columns.Count > 1)
                                    dataGrid.CurrentCell = new DataGridCellInfo(newItem, dataGrid.Columns[1]);

                                dataGrid.BeginEdit();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"{LOG} HandleTabKey inner error: {ex.Message}");
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} HandleTabKey error: {ex.Message}");
            }
        }

        private Models.SpecificationModel? FindSpecification(Models.InvoiceItemModel item)
        {
            if (_viewModel?.Invoice == null) return null;

            foreach (var spec in _viewModel.Invoice.Specifications)
            {
                if (spec?.Items?.Contains(item) == true)
                    return spec;
            }
            return null;
        }

        // ═══════════════════════════════════════════════════════
        // OTHER CHARGES HANDLERS
        // ═══════════════════════════════════════════════════════
        private void SpecCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProformaInvoiceViewModel vm)
                vm.RefreshAllChargeAutoValues();
        }

        private void SpecDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is ProformaInvoiceViewModel vm)
                vm.RefreshAllChargeAutoValues();
        }

        private void ChargeSpecToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle)
            {
                var charge = FindChargeFromToggle(toggle);
                if (charge != null)
                {
                    charge.TargetsAllSpecs = toggle.IsChecked == true;
                    if (toggle.IsChecked == true)
                        charge.SetAllSpecs();

                    if (DataContext is ProformaInvoiceViewModel vm)
                        vm.RefreshAllChargeAutoValues();
                }
            }
        }

        private void OpenSpecSelector_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var charge = FindChargeFromButton(button);
                if (charge != null && DataContext is ProformaInvoiceViewModel vm)
                    ShowSpecSelectionDialog(charge, vm);
            }
        }

        private void SpecSelectorBorder_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is OtherChargeModel charge)
            {
                if (DataContext is ProformaInvoiceViewModel vm)
                {
                    if (e.OriginalSource is System.Windows.Controls.Primitives.ToggleButton)
                        return;

                    charge.TargetsAllSpecs = false;
                    ShowSpecSelectionDialog(charge, vm);
                }
            }
        }

        private void ShowSpecSelectionDialog(OtherChargeModel charge, ProformaInvoiceViewModel vm)
        {
            var specs = vm.Invoice.Specifications;
            if (specs.Count == 0)
            {
                MessageBox.Show("No specifications available.\nAdd at least one specification first.",
                    "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Window
            {
                Title = "Select Specifications for " + charge.Name,
                Width = 400,
                Height = Math.Min(specs.Count * 40 + 120, 500),
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Colors.White),
                ShowInTaskbar = true,
                Topmost = false
            };

            var mainPanel = new StackPanel { Margin = new Thickness(15) };

            var headerText = new TextBlock
            {
                Text = "Select which specifications this charge applies to:",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainPanel.Children.Add(headerText);

            var checkBoxPanel = new StackPanel();

            for (int i = 0; i < specs.Count; i++)
            {
                var spec = specs[i];
                var specIndex = i;
                var checkBox = new CheckBox
                {
                    Content = $"Spec {i + 1}: {spec.SpecificationName}",
                    Margin = new Thickness(0, 4, 0, 4),
                    FontSize = 11,
                    IsChecked = charge.SpecIndexList.Contains(i),
                    Tag = specIndex
                };
                checkBox.Checked += (s, ev) => charge.AddSpec(specIndex);
                checkBox.Unchecked += (s, ev) => charge.RemoveSpec(specIndex);
                checkBoxPanel.Children.Add(checkBox);
            }
            mainPanel.Children.Add(checkBoxPanel);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 15, 0, 0)
            };

            var selectAllButton = new Button
            {
                Content = "All",
                Width = 60,
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(5, 3, 5, 3)
            };
            selectAllButton.Click += (s, ev) =>
            {
                charge.SetAllSpecs();
                charge.TargetsAllSpecs = true;
                dialog.Close();
                if (DataContext is ProformaInvoiceViewModel vm2)
                    vm2.RefreshAllChargeAutoValues();
            };
            buttonPanel.Children.Add(selectAllButton);

            var doneButton = new Button
            {
                Content = "Done",
                Width = 80,
                Padding = new Thickness(5, 3, 5, 3)
            };
            doneButton.Click += (s, ev) =>
            {
                dialog.Close();
                if (DataContext is ProformaInvoiceViewModel vm2)
                    vm2.RefreshAllChargeAutoValues();
            };
            buttonPanel.Children.Add(doneButton);

            mainPanel.Children.Add(buttonPanel);
            dialog.Content = mainPanel;
            dialog.ShowDialog();
        }

        private OtherChargeModel? FindChargeFromToggle(System.Windows.Controls.Primitives.ToggleButton toggle)
        {
            var parent = toggle.Parent;
            while (parent != null)
            {
                if (parent is FrameworkElement fe && fe.DataContext is OtherChargeModel charge)
                    return charge;
                if (parent is FrameworkElement f)
                    parent = f.Parent;
                else
                    break;
            }
            return null;
        }

        private OtherChargeModel? FindChargeFromButton(Button button)
        {
            var parent = button.Parent;
            while (parent != null)
            {
                if (parent is FrameworkElement fe && fe.DataContext is OtherChargeModel charge)
                    return charge;
                if (parent is FrameworkElement f)
                    parent = f.Parent;
                else
                    break;
            }
            return null;
        }

        // ═══════════════════════════════════════════════════════
        // PER-SPEC DIMENSION SELECTION
        // FIX H6: drives the bound SpecSelectionItem.IsW2H2 — no visual tree walking
        // ═══════════════════════════════════════════════════════
        private void OptSpecDim_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string dimTag)
            {
                try
                {
                    string specName = rb.GroupName;
                    if (string.IsNullOrEmpty(specName))
                    {
                        Debug.WriteLine($"{LOG} OptSpecDim_Click — empty GroupName, skipping");
                        return;
                    }

                    bool useAlt = (dimTag == "W2/H2");
                    _specDimChoice[specName] = useAlt;

                    var wrapper = SpecSelectionItems.FirstOrDefault(w => w.FullName == specName);
                    if (wrapper != null)
                    {
                        wrapper.IsSelected = true;
                        wrapper.IsW2H2 = useAlt;
                    }

                    UpdateOptStatus();
                    Debug.WriteLine($"{LOG} ✓ OptSpecDim — Spec='{specName}', Dim={dimTag}, UseAlt={useAlt}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{LOG} OptSpecDim_Click error: {ex.Message}");
                }
            }
        }

        private void UpdateOptStatus()
        {
            SyncDimChoiceFromWrappers();

            if (_specDimChoice.Count == 0)
            {
                txtOptStatus.Text = "Select specs and choose dimensions";
                return;
            }

            int w2h2Count = _specDimChoice.Count(kvp => kvp.Value);
            int w1h1Count = _specDimChoice.Count - w2h2Count;

            if (w2h2Count == 0) txtOptStatus.Text = "All specs: W1/H1";
            else if (w1h1Count == 0) txtOptStatus.Text = "All specs: W2/H2";
            else txtOptStatus.Text = $"W1/H1: {w1h1Count} specs, W2/H2: {w2h2Count} specs";
        }

        // ═══════════════════════════════════════════════════════
        // FALLBACK DIMENSION RADIO
        // FIX H6 + M6: no more visual tree walking, no more _isUpdatingSpecSelection gymnastics
        // ═══════════════════════════════════════════════════════
        private void FallbackDim_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                string fallbackType = rb.Name;
                Debug.WriteLine($"{LOG} FallbackDim_Click: {fallbackType}");

                if (fallbackType == "rbUseCustom")
                {
                    txtOptStatus.Text = "Select specs and choose dimensions";
                    return;
                }

                bool useAltW2H2 = (fallbackType == "rbUseW2H2");

                // Update VM-side dictionary
                _specDimChoice.Clear();
                if (_viewModel?.Invoice?.Specifications != null)
                {
                    foreach (var spec in _viewModel.Invoice.Specifications)
                    {
                        if (spec != null && !string.IsNullOrEmpty(spec.SpecificationName))
                            _specDimChoice[spec.SpecificationName] = useAltW2H2;
                    }
                }

                // Update wrappers (drives radio buttons via bindings)
                foreach (var wrapper in SpecSelectionItems)
                {
                    wrapper.IsSelected = true;
                    wrapper.IsW2H2 = useAltW2H2;
                }

                lstSpecSelect.SelectAll();
                txtOptStatus.Text = useAltW2H2 ? "All specs: W2/H2" : "All specs: W1/H1";

                Debug.WriteLine($"{LOG} Updated {_specDimChoice.Count} specs to {(useAltW2H2 ? "W2/H2" : "W1/H1")}");
            }
        }

        // ═══════════════════════════════════════════════════════
        // SPEC ROW RADIO HANDLERS (bound via SpecSelectionItem.IsW2H2)
        // ═══════════════════════════════════════════════════════
        private void RbSpecW1H1_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string specName)
            {
                try
                {
                    if (string.IsNullOrEmpty(specName)) return;

                    _specDimChoice[specName] = false;

                    var wrapper = SpecSelectionItems.FirstOrDefault(w => w.FullName == specName);
                    if (wrapper != null)
                    {
                        wrapper.IsSelected = true;
                        wrapper.IsW2H2 = false;
                    }

                    UpdateOptStatus();
                    Debug.WriteLine($"{LOG} RbSpecW1H1 — Spec='{specName}'");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{LOG} RbSpecW1H1_Click error: {ex.Message}");
                }
            }
        }

        private void RbSpecW2H2_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string specName)
            {
                try
                {
                    if (string.IsNullOrEmpty(specName)) return;

                    _specDimChoice[specName] = true;

                    var wrapper = SpecSelectionItems.FirstOrDefault(w => w.FullName == specName);
                    if (wrapper != null)
                    {
                        wrapper.IsSelected = true;
                        wrapper.IsW2H2 = true;
                    }

                    UpdateOptStatus();
                    Debug.WriteLine($"{LOG} RbSpecW2H2 — Spec='{specName}'");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{LOG} RbSpecW2H2_Click error: {ex.Message}");
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        // SPEC LIST SELECTION CHANGED
        // FIX H1: duplicate _specDimChoice.Remove(key) removed
        // ═══════════════════════════════════════════════════════
        private void lstSpecSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingSpecSelection) return;

            try
            {
                _isUpdatingSpecSelection = true;

                // Sync wrapper IsSelected with ListBox SelectedItems
                var selectedSet = new HashSet<SpecSelectionItem>(lstSpecSelect.SelectedItems.OfType<SpecSelectionItem>());
                foreach (var wrapper in SpecSelectionItems)
                    wrapper.IsSelected = selectedSet.Contains(wrapper);

                // Rebuild dim choice dictionary from selection
                var selectedNames = selectedSet
                    .Where(w => !string.IsNullOrEmpty(w.FullName))
                    .Select(w => w.FullName)
                    .ToHashSet();

                // Add newcomers (default W1/H1)
                foreach (var w in selectedSet)
                {
                    if (!string.IsNullOrEmpty(w.FullName) && !_specDimChoice.ContainsKey(w.FullName))
                        _specDimChoice[w.FullName] = w.IsW2H2;
                }

                // Remove deselected
                var toRemove = _specDimChoice.Keys.Where(k => !selectedNames.Contains(k)).ToList();
                foreach (var key in toRemove)
                {
                    _specDimChoice.Remove(key); // FIX H1: was called twice
                    Debug.WriteLine($"{LOG} Removed from tracking: {key}");
                }

                Debug.WriteLine($"{LOG} Selection sync — selected={selectedSet.Count}, tracked={_specDimChoice.Count}");
                UpdateOptStatus();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} lstSpecSelect_SelectionChanged error: {ex.Message}");
            }
            finally
            {
                _isUpdatingSpecSelection = false;
            }
        }
    }

    // ═══════════════════════════════════════════════════════
    // SCROLL BEHAVIOR HELPER
    // ═══════════════════════════════════════════════════════
    public static class ScrollViewerBehavior
    {
        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(ScrollViewerBehavior),
                new FrameworkPropertyMetadata(0.0, OnVerticalOffsetChanged));

        public static void SetVerticalOffset(DependencyObject target, double value) => target.SetValue(VerticalOffsetProperty, value);
        public static double GetVerticalOffset(DependencyObject target) => (double)target.GetValue(VerticalOffsetProperty);

        private static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer)
                scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
        }
    }
}