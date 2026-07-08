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
    /// Code-behind for ProformaInvoiceView.xaml — release-ready version with Undo/Redo.
    /// Handles UI interactions only; business logic lives in the ViewModel.
    /// </summary>
    public partial class ProformaInvoiceView : UserControl
    {
        // ═══════════════════════════════════════════════════════
        // CONSTANTS
        // ═══════════════════════════════════════════════════════
        private const string LOG = "[ProformaView]";

        private const double DEFAULT_SHEET_WIDTH = 3210;
        private const double DEFAULT_SHEET_HEIGHT = 2250;
        private const int DEFAULT_SHEET_QTY = 99999;
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

        private readonly Dictionary<string, bool> _specDimChoice = new();

        private Window? _optimizerWindow;
        private OptimizationView? _optimizerView;

        private bool _isUpdatingSpecSelection = false;

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

            PreviewKeyDown += ProformaInvoiceView_PreviewKeyDown;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                InitializeTrimTable();
                AutoSelectFirstThickness();
            }), System.Windows.Threading.DispatcherPriority.Loaded);

            Debug.WriteLine($"{LOG} Using SharedViewModels.ProformaInvoiceVM");
            Debug.WriteLine($"{LOG} ✓ Automatic thickness trim ready");
            Debug.WriteLine($"{LOG} ✓ Undo/Redo (Ctrl+Z / Ctrl+Y) wired");
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
            catch (Exception ex)
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

            // FIX: Unsubscribe key handler on unload to avoid stale handler references
            PreviewKeyDown -= ProformaInvoiceView_PreviewKeyDown;

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
            catch (Exception ex)
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
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} BtnClearSheets_Click error: {ex.Message}");
            }
        }

        // FIX: Handler added because XAML now wires Presets button to this method.
        // Preserves UI behavior without introducing incomplete preset logic.
        private void BtnPresets_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                txtOptStatus.Text = "Sheet presets coming soon";
                MessageBox.Show("Sheet presets module coming soon.", "Presets", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} BtnPresets_Click error: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════
        // SPEC SELECTION BUTTONS
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
        // OPEN OPTIMIZER WINDOW
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
        // SHARED OPTIMIZATION CORE
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

            SyncDimChoiceFromWrappers();

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

            int altAvailable = allItemsWithSpec.Count(t => t.item.Width2 > 0 || t.item.Height2 > 0);
            if (globalUseAlt && altAvailable == 0)
            {
                MessageBox.Show("W2/H2 selected but no alternate dimensions found. Falling back to W1/H1.",
                    "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                globalUseAlt = false;
            }

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
            _specDimChoice.Clear();
            foreach (var wrapper in SpecSelectionItems.Where(w => w.IsSelected))
            {
                if (!string.IsNullOrEmpty(wrapper.FullName))
                    _specDimChoice[wrapper.FullName] = wrapper.IsW2H2;
            }
        }

        // ═══════════════════════════════════════════════════════
        // RUN OPTIMIZATION
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
        // VIEW FULL LAYOUTS
        // ═══════════════════════════════════════════════════════
        private void ViewOptimizationLayouts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ctx = BuildOptimizationContext();
                if (ctx == null) return;

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

        private void UpdateResultsUI(OptimizationView optView, OptimizationContext ctx)
        {
            double utilization = optView.AverageUtilization;
            int sheetsUsed = optView.SheetsUsed;

            txtUtilization.Text = $"{utilization:N1}%";
            txtSheetsUsed.Text = sheetsUsed.ToString();
            txtWastage.Text = $"{(100 - utilization):N1}%";

            txtOptStatus.Text =
                $"✓ Optimized | {ctx.Thickness} | Trim: {ctx.Trim.LM}/{ctx.Trim.RM}/{ctx.Trim.TM}/{ctx.Trim.BM}mm | {ctx.DimStatusText}";

            int currentCount = int.TryParse(txtOptRunCount.Text, out int c) ? c : 0;
            txtOptRunCount.Text = (currentCount + 1).ToString();

            UpdatePerSheetResults(optView, ctx.SheetWidth, ctx.SheetHeight);
        }

        // ═══════════════════════════════════════════════════════
        // THICKNESS CHANGED
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
        // ═══════════════════════════════════════════════════════
        private void UpdatePerSheetResults(OptimizationView optView, double sheetWidth, double sheetHeight)
        {
            try
            {
                SheetResults.Clear();

                var results = optView.GetResultsList();

                if (results == null || results.Count == 0)
                {
                    int totalSheets = optView.SheetsUsed;
                    double areaTotal = sheetWidth * sheetHeight / 1_000_000;
                    double util = optView.AverageUtilization;
                    double wastage = 100 - util;
                    double areaUsed = areaTotal * (util / 100);

                    SheetResults.Add(new SheetResultItem
                    {
                        SheetName = $"{sheetWidth:N0} × {sheetHeight:N0} mm",
                        SheetDimensions = $"{sheetWidth:N0} × {sheetHeight:N0} mm",
                        PiecesCut = "—",
                        AreaUsed = $"{areaUsed * totalSheets:N2} m²",
                        Utilization = $"{util:N1}",
                        Wastage = $"{wastage:N1}",
                        BarHeight = util * 0.8
                    });
                }
                else
                {
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
        // SMART PASTE FROM CONTEXT MENU
        // ═══════════════════════════════════════════════════════
        private void SmartPaste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not MenuItem menuItem) return;

                if (menuItem.Parent is not ContextMenu contextMenu) return;
                if (contextMenu.PlacementTarget is not DataGridCell clickedCell)
                {
                    Debug.WriteLine($"{LOG} SmartPaste: PlacementTarget is not a DataGridCell");
                    return;
                }

                var clickedColumn = clickedCell.Column;
                if (clickedColumn == null)
                {
                    Debug.WriteLine($"{LOG} SmartPaste: no column");
                    return;
                }

                string clickedHeader = clickedColumn.Header?.ToString() ?? "";
                int anchorColIndex = GetDimensionColumnIndex(clickedHeader);
                if (anchorColIndex < 0)
                {
                    MessageBox.Show("Right-click paste only works on W1, H1, W2, H2, or Qty columns.",
                        "Paste", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (clickedCell.DataContext is not Models.InvoiceItemModel anchorItem)
                {
                    Debug.WriteLine($"{LOG} SmartPaste: cell DataContext is not InvoiceItemModel");
                    return;
                }

                var spec = FindSpecification(anchorItem);
                if (spec == null)
                {
                    Debug.WriteLine($"{LOG} SmartPaste: cannot find owning spec");
                    return;
                }

                int anchorRowIndex = spec.Items.IndexOf(anchorItem);
                if (anchorRowIndex < 0)
                {
                    Debug.WriteLine($"{LOG} SmartPaste: anchor item not in spec.Items");
                    return;
                }

                if (!Clipboard.ContainsText())
                {
                    MessageBox.Show("Clipboard is empty.", "Paste",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string clipboardText = Clipboard.GetText();
                if (string.IsNullOrWhiteSpace(clipboardText))
                {
                    MessageBox.Show("No text in clipboard.", "Paste",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var rawRows = clipboardText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (rawRows.Length == 0)
                {
                    MessageBox.Show("No data to paste.", "Paste",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _viewModel?.TakeSnapshot($"Paste {rawRows.Length} rows into '{spec.SpecificationName}'");

                var defaults = CaptureRowDefaults(spec);

                int rowsAdded = 0;
                int rowsUpdated = 0;

                using (spec.BulkUpdateScope())
                {
                    for (int i = 0; i < rawRows.Length; i++)
                    {
                        var cells = rawRows[i].Split('\t');
                        if (cells.Length == 0) continue;
                        if (string.IsNullOrWhiteSpace(string.Join("", cells))) continue;

                        int targetRowIndex = anchorRowIndex + i;

                        Models.InvoiceItemModel targetItem;
                        if (targetRowIndex < spec.Items.Count)
                        {
                            targetItem = spec.Items[targetRowIndex];
                            rowsUpdated++;
                        }
                        else
                        {
                            targetItem = CreateRowFromDefaults(spec, defaults, targetRowIndex + 1);
                            spec.Items.Add(targetItem);
                            rowsAdded++;
                        }

                        for (int c = 0; c < cells.Length; c++)
                        {
                            int targetColIndex = anchorColIndex + c;
                            if (targetColIndex > 4) break;

                            ApplyCellValue(targetItem, targetColIndex, cells[c]);
                        }
                    }
                }

                _viewModel.RenumberAllSrNumbers();
                spec.Recalculate();
                _viewModel.Invoice.CalculateTotals();
                _viewModel.Invoice.IsDirty = true;

                Debug.WriteLine($"{LOG} SmartPaste: {rowsUpdated} updated + {rowsAdded} added in spec '{spec.SpecificationName}'");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} SmartPaste error: {ex.Message}");
                MessageBox.Show($"Paste failed: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetDimensionColumnIndex(string header)
        {
            return header switch
            {
                "W1" => 0,
                "H1" => 1,
                "W2" => 2,
                "H2" => 3,
                "Qty" => 4,
                _ => -1
            };
        }

        private void ApplyCellValue(Models.InvoiceItemModel item, int colIndex, string rawValue)
        {
            string trimmed = rawValue?.Trim() ?? "";
            if (string.IsNullOrEmpty(trimmed)) return;

            string cleaned = trimmed.Replace(",", "");

            switch (colIndex)
            {
                case 0:
                    if (double.TryParse(cleaned, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double w1))
                        item.Width1 = Math.Max(0, w1);
                    break;

                case 1:
                    if (double.TryParse(cleaned, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double h1))
                        item.Height1 = Math.Max(0, h1);
                    break;

                case 2:
                    if (double.TryParse(cleaned, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double w2))
                        item.Width2 = Math.Max(0, w2);
                    break;

                case 3:
                    if (double.TryParse(cleaned, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double h2))
                        item.Height2 = Math.Max(0, h2);
                    break;

                case 4:
                    if (int.TryParse(cleaned, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out int qty))
                        item.Qty = Math.Max(1, qty);
                    break;
            }
        }

        private RowDefaults CaptureRowDefaults(Models.SpecificationModel spec)
        {
            var d = new RowDefaults();
            if (spec.Items.Count > 0)
            {
                var first = spec.Items[0];
                d.GlassRef = first.GlassRef ?? "";
                d.Width1 = first.Width1;
                d.Height1 = first.Height1;
                d.Width2 = first.Width2;
                d.Height2 = first.Height2;
                d.Qty = first.Qty > 0 ? first.Qty : 1;
                d.Price = first.Price;
                d.SurchargePercent = first.SurchargePercent;
            }
            else
            {
                d.Qty = 1;
                d.SurchargePercent = spec.SurchargePercent;
                d.Price = spec.BasePrice;
            }
            return d;
        }

        private Models.InvoiceItemModel CreateRowFromDefaults(Models.SpecificationModel spec, RowDefaults d, int srNo)
        {
            return new Models.InvoiceItemModel
            {
                SrNo = srNo,
                GlassRef = d.GlassRef,
                Width1 = d.Width1,
                Height1 = d.Height1,
                Width2 = d.Width2,
                Height2 = d.Height2,
                Qty = d.Qty,
                Price = d.Price,
                SurchargePercent = d.SurchargePercent,
                Specification = spec
            };
        }

        private class RowDefaults
        {
            public string GlassRef { get; set; } = "";
            public double Width1 { get; set; }
            public double Height1 { get; set; }
            public double Width2 { get; set; }
            public double Height2 { get; set; }
            public int Qty { get; set; } = 1;
            public double Price { get; set; }
            public double SurchargePercent { get; set; } = 20;
        }

        // ═══════════════════════════════════════════════════════
        // PASTE FROM EXCEL (Ctrl+V — legacy)
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

                _viewModel?.TakeSnapshot($"Paste {rows.Length} rows into '{spec.SpecificationName}'");

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
        // ✏️ UNDO/REDO — UserControl-level keyboard handler
        // ═══════════════════════════════════════════════════════
        private void ProformaInvoiceView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                bool isCtrlZ = (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control);
                bool isCtrlY = (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control);

                if (!isCtrlZ && !isCtrlY) return;

                if (IsCellInEditMode())
                {
                    Debug.WriteLine($"{LOG} Ctrl+{(isCtrlZ ? "Z" : "Y")} — cell in edit mode, letting WPF handle it");
                    return;
                }

                if (isCtrlZ)
                {
                    if (_viewModel?.CanUndo == true)
                    {
                        _viewModel.Undo();
                        e.Handled = true;
                        Debug.WriteLine($"{LOG} Ctrl+Z fired → Undo");
                    }
                    else
                    {
                        Debug.WriteLine($"{LOG} Ctrl+Z fired but CanUndo=false");
                    }
                }
                else if (isCtrlY)
                {
                    if (_viewModel?.CanRedo == true)
                    {
                        _viewModel.Redo();
                        e.Handled = true;
                        Debug.WriteLine($"{LOG} Ctrl+Y fired → Redo");
                    }
                    else
                    {
                        Debug.WriteLine($"{LOG} Ctrl+Y fired but CanRedo=false");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} PreviewKeyDown error: {ex.Message}");
            }
        }

        private bool IsCellInEditMode()
        {
            try
            {
                var focused = Keyboard.FocusedElement as DependencyObject;
                if (focused == null) return false;

                while (focused != null)
                {
                    if (focused is DataGridCell cell && cell.IsEditing)
                        return true;

                    if (focused is System.Windows.Controls.Primitives.DataGridCellsPresenter ||
                        focused is DataGrid)
                    {
                        return false;
                    }

                    focused = VisualTreeHelper.GetParent(focused);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════
        // ✏️ UNDO — Cell-edit tracking with pre-edit value memory
        // ═══════════════════════════════════════════════════════
        private object? _preEditValue;
        private Models.InvoiceItemModel? _preEditItem;
        private string _preEditColumn = "";

        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            try
            {
                if (e.Row?.Item is not Models.InvoiceItemModel item) return;

                string columnHeader = e.Column?.Header?.ToString() ?? "cell";

                _preEditItem = item;
                _preEditColumn = columnHeader;
                _preEditValue = GetCellValueForUndo(item, columnHeader);

                Debug.WriteLine($"{LOG} BeginningEdit: captured pre-edit value for {columnHeader} = '{_preEditValue}'");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} DataGrid_BeginningEdit error: {ex.Message}");
                _preEditValue = null;
                _preEditItem = null;
            }
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                if (e.EditAction != DataGridEditAction.Commit)
                {
                    _preEditValue = null;
                    _preEditItem = null;
                    return;
                }

                if (_preEditItem == null) return;
                var item = _preEditItem;
                var column = _preEditColumn;
                var originalValue = _preEditValue;
                _preEditItem = null;
                _preEditValue = null;

                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
                {
                    try
                    {
                        object? newValue = GetCellValueForUndo(item, column);

                        if (Equals(originalValue, newValue))
                        {
                            Debug.WriteLine($"{LOG} CellEditEnding: no change in {column} (was '{originalValue}', still '{newValue}') — no snapshot");
                            return;
                        }

                        var spec = FindSpecification(item);
                        if (spec == null) return;

                        SetCellValueForUndo(item, column, originalValue);
                        _viewModel?.TakeSnapshot($"Edit {column} in '{spec.SpecificationName}'");
                        SetCellValueForUndo(item, column, newValue);

                        Debug.WriteLine($"{LOG} CellEditEnding snapshot: {column} '{originalValue}' → '{newValue}'");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"{LOG} CellEditEnding deferred error: {ex.Message}");
                    }
                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} DataGrid_CellEditEnding error: {ex.Message}");
                _preEditValue = null;
                _preEditItem = null;
            }
        }

        private object? GetCellValueForUndo(Models.InvoiceItemModel item, string columnHeader)
        {
            return columnHeader switch
            {
                "Glass Ref" => item.GlassRef,
                "W1" => item.Width1,
                "H1" => item.Height1,
                "W2" => item.Width2,
                "H2" => item.Height2,
                "Qty" => item.Qty,
                "Price" => item.Price,
                _ => null
            };
        }

        private void SetCellValueForUndo(Models.InvoiceItemModel item, string columnHeader, object? value)
        {
            try
            {
                switch (columnHeader)
                {
                    case "Glass Ref": item.GlassRef = value?.ToString() ?? ""; break;
                    case "W1": if (value is double w1) item.Width1 = w1; break;
                    case "H1": if (value is double h1) item.Height1 = h1; break;
                    case "W2": if (value is double w2) item.Width2 = w2; break;
                    case "H2": if (value is double h2) item.Height2 = h2; break;
                    case "Qty": if (value is int qty) item.Qty = qty; break;
                    case "Price": if (value is double p) item.Price = p; break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} SetCellValueForUndo error: {ex.Message}");
            }
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
            catch (Exception ex)
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

                _specDimChoice.Clear();
                if (_viewModel?.Invoice?.Specifications != null)
                {
                    foreach (var spec in _viewModel.Invoice.Specifications)
                    {
                        if (spec != null && !string.IsNullOrEmpty(spec.SpecificationName))
                            _specDimChoice[spec.SpecificationName] = useAltW2H2;
                    }
                }

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
        // SPEC ROW RADIO HANDLERS
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
        // ═══════════════════════════════════════════════════════
        private void lstSpecSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingSpecSelection) return;

            try
            {
                _isUpdatingSpecSelection = true;

                var selectedSet = new HashSet<SpecSelectionItem>(lstSpecSelect.SelectedItems.OfType<SpecSelectionItem>());
                foreach (var wrapper in SpecSelectionItems)
                    wrapper.IsSelected = selectedSet.Contains(wrapper);

                var selectedNames = selectedSet
                    .Where(w => !string.IsNullOrEmpty(w.FullName))
                    .Select(w => w.FullName)
                    .ToHashSet();

                foreach (var w in selectedSet)
                {
                    if (!string.IsNullOrEmpty(w.FullName) && !_specDimChoice.ContainsKey(w.FullName))
                        _specDimChoice[w.FullName] = w.IsW2H2;
                }

                var toRemove = _specDimChoice.Keys.Where(k => !selectedNames.Contains(k)).ToList();
                foreach (var key in toRemove)
                {
                    _specDimChoice.Remove(key);
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