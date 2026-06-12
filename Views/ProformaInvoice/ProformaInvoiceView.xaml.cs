using System;
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
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

// Aliases to avoid ambiguity
using ModelInvoice = ProGlassAutomation.Models.InvoiceItemModel;
using ModelSpec = ProGlassAutomation.Models.SpecificationModel;
using Color = System.Windows.Media.Color;
using Border = System.Windows.Controls.Border;
using CheckBox = System.Windows.Controls.CheckBox;

namespace ProGlassAutomation.Views.ProformaInvoice
{
    /// <summary>
    /// Code-behind for ProformaInvoiceView.xaml
    /// Handles UI interactions, clipboard paste, print, and navigation.
    /// </summary>
    /// <remarks>
    /// PATCH 18: Fixed memory leak issues
    /// - Simplified Unloaded cleanup
    /// - Use BulkUpdateScope for paste
    /// - Better error handling
    /// - Use Dispatcher instead of Task.ContinueWith
    /// </remarks>
    public partial class ProformaInvoiceView : UserControl
    {
        private ProformaInvoiceViewModel _viewModel;
        private const double SCROLL_SPEED = 0.3;

        // ═══════════════════════════════════════════════════════════════
        // MISSING COLLECTIONS - ADD THESE
        // ═══════════════════════════════════════════════════════════════
        public ObservableCollection<StockSheetItem> AdditionalSheets { get; set; } = new();
        public ObservableCollection<SheetResultItem> SheetResults { get; set; } = new();

        // ═══════════════════════════════════════════════════════════════
        // MISSING CLASSES - ADD THESE
        // ═══════════════════════════════════════════════════════════════

        public class StockSheetItem : INotifyPropertyChanged
        {
            private string _sheetLabel = "";
            private string _width = "";
            private string _height = "";
            private string _qty = "";

            public string SheetLabel
            {
                get => _sheetLabel;
                set { _sheetLabel = value; OnPropertyChanged("SheetLabel"); }
            }
            public string Width
            {
                get => _width;
                set { _width = value; OnPropertyChanged("Width"); }
            }
            public string Height
            {
                get => _height;
                set { _height = value; OnPropertyChanged("Height"); }
            }
            public string Qty
            {
                get => _qty;
                set { _qty = value; OnPropertyChanged("Qty"); }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public class SheetResultItem
        {
            public string SheetName { get; set; } = "";
            public string SheetDimensions { get; set; } = "";
            public string PiecesCut { get; set; } = "0";
            public string AreaUsed { get; set; } = "0 m²";
            public string Utilization { get; set; } = "0";
            public string Wastage { get; set; } = "0";
            public double BarHeight { get; set; } = 0;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SPEC SELECTION WRAPPER - Shows simple names like "Spec 1"
        // ═══════════════════════════════════════════════════════════════
        public class SpecSelectionItem : INotifyPropertyChanged
        {
            private string _displayName = "";
            private string _fullName = "";
            private bool _isSelected = false;

            public string DisplayName
            {
                get => _displayName;
                set { _displayName = value; OnPropertyChanged("DisplayName"); }
            }
            public string FullName
            {
                get => _fullName;
                set { _fullName = value; OnPropertyChanged("FullName"); }
            }
            public bool IsSelected
            {
                get => _isSelected;
                set { _isSelected = value; OnPropertyChanged("IsSelected"); }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SPEC SELECTION COLLECTION
        // ═══════════════════════════════════════════════════════════════
        public ObservableCollection<SpecSelectionItem> SpecSelectionItems { get; set; } = new();

        // ═══════════════════════════════════════════════════════════════
        // TRIM TABLE
        // ═══════════════════════════════════════════════════════════════
        private Dictionary<string, TrimSettings> _trimTable = new()
        {
            ["6mm"] = new TrimSettings { LM = 15, RM = 15, TM = 15, BM = 15, BreakoutMin = 15, Kerf = 0 },
            ["8mm"] = new TrimSettings { LM = 30, RM = 30, TM = 30, BM = 30, BreakoutMin = 30, Kerf = 0 },
            ["10mm"] = new TrimSettings { LM = 50, RM = 50, TM = 0, BM = 50, BreakoutMin = 40, Kerf = 0 },
            ["12mm"] = new TrimSettings { LM = 50, RM = 50, TM = 0, BM = 50, BreakoutMin = 40, Kerf = 0 }
        };

        public class TrimSettings
        {
            public double LM { get; set; }
            public double RM { get; set; }
            public double TM { get; set; }
            public double BM { get; set; }
            public double BreakoutMin { get; set; }
            public double Kerf { get; set; }
        }

        // ═══════════════════════════════════════════════════════════════
        // CUTPART FOR OPTIMIZER
        // ═══════════════════════════════════════════════════════════════
        public class CutPart
        {
            public string Ref { get; set; } = "";
            public double L { get; set; }
            public double W { get; set; }
            public int Qty { get; set; }
            public bool Rot { get; set; } = true;
        }

        // Track which dimension EACH spec uses (false = W1/H1, true = W2/H2)
        private Dictionary<string, bool> _specDimChoice = new Dictionary<string, bool>();

        // Static references
        private static Window? _optimizerWindow;
        private static OptimizationView? _optimizerView;

        // Flag to block selection changed handler during programmatic updates
        private bool _isUpdatingSpecSelection = false;

        // ═══════════════════════════════════════════════════════════════
        // CONSTRUCTOR - ADD MISSING BINDINGS
        // ═══════════════════════════════════════════════════════════════
        public ProformaInvoiceView()
        {
            InitializeComponent();

            // Use shared ProformaInvoiceViewModel (SAME instance as DailyWorks)
            _viewModel = SharedViewModels.ProformaInvoiceVM;
            DataContext = _viewModel;

            // IMPORTANT: Set this UserControl as temporary DataContext for list binding
            // or use RelativeSource in XAML
            var tempDataContext = this;

            // MISSING BINDINGS - ADD THESE
            AdditionalSheetsContainer.ItemsSource = AdditionalSheets;
            PerSheetResultsContainer.ItemsSource = SheetResults;

            // Note: Don't call RefreshSpecSelectionItems() here
            // It will be called in Loaded event after specs are ready

            // PATCH 18 - Register for cleanup to prevent memory leaks
            Unloaded += ProformaInvoiceView_Unloaded;

            System.Diagnostics.Debug.WriteLine("[ProformaInvoiceView] Using SharedViewModels.ProformaInvoiceVM");
        }

        // ==================== LOADED - Auto-create specs for testing ====================

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Always refresh when UserControl loads to ensure we have latest specs
            RefreshSpecSelectionItems();
            System.Diagnostics.Debug.WriteLine($"[UserControl_Loaded] Specs refreshed, count: {SpecSelectionItems.Count}");
        }

        private void lstSpecSelect_Loaded(object sender, RoutedEventArgs e)
        {
            // Also refresh when ListBox loads
            RefreshSpecSelectionItems();
            System.Diagnostics.Debug.WriteLine($"[lstSpecSelect_Loaded] Items count: {SpecSelectionItems.Count}");
        }

        // ==================== ADD SPECIFICATION - FIX DUPLICATE ====================
        private void AddSpecification_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int countBefore = _viewModel?.Invoice?.Specifications?.Count ?? 0;

                if (_viewModel?.AddSpecificationCommand?.CanExecute(null) == true)
                {
                    _viewModel.AddSpecificationCommand.Execute(null);
                }

                int countAfter = _viewModel?.Invoice?.Specifications?.Count ?? 0;

                System.Diagnostics.Debug.WriteLine($"[AddSpecification_Click] Before: {countBefore}, After: {countAfter}");

                // FIX: If 2 specs were added instead of 1, remove the duplicate
                if (countAfter == countBefore + 2 && countAfter > 0)
                {
                    var specToRemove = _viewModel.Invoice.Specifications[countAfter - 1];
                    _viewModel.Invoice.Specifications.RemoveAt(countAfter - 1);
                    System.Diagnostics.Debug.WriteLine($"[AddSpecification_Click] Removed duplicate spec");
                    countAfter--;
                }

                // Refresh spec selection list
                RefreshSpecSelectionItems();

                // Auto-select the newly added spec
                SelectLastSpec();

                System.Diagnostics.Debug.WriteLine($"[AddSpecification_Click] ✓ Spec added, Total: {SpecSelectionItems.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AddSpecification_Click] ERROR: {ex.Message}");
                MessageBox.Show($"Error adding specification: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveSpecification_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Execute the command from ViewModel
                if (_viewModel?.RemoveSpecificationCommand?.CanExecute(null) == true)
                {
                    _viewModel.RemoveSpecificationCommand.Execute(null);
                }

                // AUTO-REFRESH: Update spec selection list immediately after removing
                RefreshSpecSelectionItems();

                System.Diagnostics.Debug.WriteLine($"[RemoveSpecification_Click] ✓ Spec removed, Total specs: {SpecSelectionItems.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RemoveSpecification_Click] ERROR: {ex.Message}");
            }
        }

        private void SelectLastSpec()
        {
            // Auto-select the last added spec in the ListBox
            if (SpecSelectionItems.Count > 0)
            {
                var lastWrapper = SpecSelectionItems[SpecSelectionItems.Count - 1];
                lstSpecSelect.SelectedItem = lastWrapper;
            }
        }

        // ==================== PATCH 18 - CLEANUP ON UNLOAD ====================

        private void ProformaInvoiceView_Unloaded(object sender, RoutedEventArgs e)
        {
            // PATCH 18 FIX - Proper cleanup to prevent memory leaks
            Unloaded -= ProformaInvoiceView_Unloaded;

            // PATCH 18 FIX: Don't clear DataContext - shared VM handles its own cleanup
            System.Diagnostics.Debug.WriteLine("[ProformaInvoiceView] Unloaded - Cleanup complete");
        }

        // ═══════════════════════════════════════════════════════════════
        // REFRESH SPEC SELECTION ITEMS - Create simple names from specs
        // ═══════════════════════════════════════════════════════════════

        public void RefreshSpecSelectionItems()
        {
            SpecSelectionItems.Clear();

            int specCount = _viewModel?.Invoice?.Specifications?.Count ?? 0;
            System.Diagnostics.Debug.WriteLine($"[RefreshSpecSelectionItems] START - Invoice has {specCount} specs");

            if (_viewModel?.Invoice?.Specifications != null)
            {
                for (int i = 0; i < _viewModel.Invoice.Specifications.Count; i++)
                {
                    var spec = _viewModel.Invoice.Specifications[i];
                    if (spec != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[RefreshSpecSelectionItems] Adding Spec {i + 1}: {spec.SpecificationName}");

                        SpecSelectionItems.Add(new SpecSelectionItem
                        {
                            DisplayName = $"Spec {i + 1}",
                            FullName = spec.SpecificationName,
                            IsSelected = false
                        });
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[RefreshSpecSelectionItems] DONE - Created {SpecSelectionItems.Count} items");
        }

        // ═══════════════════════════════════════════════════════════════
        // MISSING BUTTON HANDLERS - ADD THESE
        // ═══════════════════════════════════════════════════════════════

        private void BtnAddStockSheet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int sheetNum = AdditionalSheets.Count + 2;
                AdditionalSheets.Add(new StockSheetItem
                {
                    SheetLabel = $"Sheet {sheetNum}",
                    Width = "3210",
                    Height = "2250",
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
                txtSheetWidth.Text = "3210";
                txtSheetHeight.Text = "2250";
                txtSheetQty.Text = "99999";
                AdditionalSheets.Clear();
                txtOptStatus.Text = "Default sheets loaded";
            }
            catch (Exception) { }
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
            catch (Exception) { }
        }

        private void btnSelectAllSpecs_Click(object sender, RoutedEventArgs e)
        {
            lstSpecSelect.SelectAll();
        }

        private void btnClearSpecSelection_Click(object sender, RoutedEventArgs e)
        {
            lstSpecSelect.SelectedItems.Clear();
            _specDimChoice.Clear();
            txtOptStatus.Text = "Select specs and choose dimensions";

            // Also clear UI selections
            UpdateSpecSelectUIRadioButtons();
        }

        // ═══════════════════════════════════════════════════════════════
        // EXISTING EXPANDABLE PANEL CLICK HANDLERS - PRESERVE THESE
        // ═══════════════════════════════════════════════════════════════

        private void ToggleFilePanel_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.IsFilePanelOpen = !_viewModel.IsFilePanelOpen;
                FileContent.Visibility = _viewModel.IsFilePanelOpen ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ToggleSGUPanel_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.IsSGUSelected = !_viewModel.IsSGUSelected;
            }
        }

        private void ToggleDGUPanel_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.IsDGUSelected = !_viewModel.IsDGUSelected;
            }
        }

        private void ToggleLAMPanel_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.IsLAMSelected = !_viewModel.IsLAMSelected;
            }
        }

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
            RecentInvoicesContent.Visibility = RecentInvoicesContent.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        // ==================== OPEN OPTIMIZER WINDOW ====================

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
            optWindow.ContentRendered += (s, args) =>
            {
                _optimizerView = optWindow.Content as OptimizationView;
            };

            optWindow.Closed += (s, args) =>
            {
                _optimizerWindow = null;
                _optimizerView = null;
            };

            optWindow.Show();
            optWindow.WindowState = WindowState.Maximized;
        }

        // ==================== RUN OPTIMIZATION (QUICK RESULT) ====================

        private void RunOptimization_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Default sheet size if empty
                if (string.IsNullOrWhiteSpace(txtSheetWidth.Text)) txtSheetWidth.Text = "3660";
                if (string.IsNullOrWhiteSpace(txtSheetHeight.Text)) txtSheetHeight.Text = "2440";

                // Get sheet size
                if (!double.TryParse(txtSheetWidth.Text, out double sheetWidth) || sheetWidth <= 0)
                {
                    MessageBox.Show("Please enter valid sheet width!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!double.TryParse(txtSheetHeight.Text, out double sheetHeight) || sheetHeight <= 0)
                {
                    MessageBox.Show("Please enter valid sheet height!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get thickness with null safe check
                string thickness = (cmbThickness.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "6mm";

                if (!_trimTable.ContainsKey(thickness))
                {
                    thickness = "6mm";
                }
                TrimSettings trim = _trimTable[thickness];

                // Get all items from all specifications
                var items = new List<ModelInvoice>();
                if (_viewModel?.Invoice?.Specifications != null)
                {
                    foreach (var spec in _viewModel.Invoice.Specifications)
                    {
                        if (spec?.Items != null)
                        {
                            foreach (var item in spec.Items)
                            {
                                if (item != null)
                                    items.Add(item);
                            }
                        }
                    }
                }

                if (items.Count == 0)
                {
                    MessageBox.Show("No items to optimize!", "Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Determine which mode we're in
                bool globalUseAlt = (rbUseW2H2 != null && rbUseW2H2.IsChecked == true);
                bool useCustom = (rbUseCustom != null && rbUseCustom.IsChecked == true);

                // Also check checkbox in results area
                if (!globalUseAlt && rbDimsW2H2 != null && rbDimsW2H2.IsChecked == true)
                    globalUseAlt = true;

                System.Diagnostics.Debug.WriteLine($"[RunOptimization] START - globalUseAlt={globalUseAlt}, useCustom={useCustom}, trackedSpecs={_specDimChoice.Count}");

                // Get all items with their spec info
                var allItems = new List<(Models.SpecificationModel spec, Models.InvoiceItemModel item)>();

                if (_viewModel?.Invoice?.Specifications != null)
                {
                    foreach (var spec in _viewModel.Invoice.Specifications)
                    {
                        if (spec?.Items == null) continue;
                        foreach (var item in spec.Items)
                        {
                            if (item != null)
                                allItems.Add((spec, item));
                        }
                    }
                }

                if (allItems.Count == 0)
                {
                    MessageBox.Show("No items to optimize!", "Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Build parts list using appropriate dimensions
                var parts = new List<CutPart>();

                foreach (var (spec, item) in allItems)
                {
                    bool useAlt = false;

                    if (useCustom && _specDimChoice.ContainsKey(spec.SpecificationName))
                    {
                        // USE PER-SPEC SELECTION from dictionary
                        useAlt = _specDimChoice[spec.SpecificationName];
                        System.Diagnostics.Debug.WriteLine($"[RunOptimization] Per-spec: {spec.SpecificationName} → {(useAlt ? "W2/H2" : "W1/H1")}");
                    }
                    else
                    {
                        // USE GLOBAL SELECTION
                        useAlt = globalUseAlt;
                        System.Diagnostics.Debug.WriteLine($"[RunOptimization] Global: {spec.SpecificationName} → {(useAlt ? "W2/H2" : "W1/H1")}");
                    }

                    double width = useAlt
                        ? (item.Width2 > 0 ? item.Width2 : item.Width1)
                        : (item.Width1 > 0 ? item.Width1 : item.Width2);
                    double height = useAlt
                        ? (item.Height2 > 0 ? item.Height2 : item.Height1)
                        : (item.Height1 > 0 ? item.Height1 : item.Height2);

                    parts.Add(new CutPart
                    {
                        Ref = item.GlassRef ?? "P",
                        L = width,
                        W = height,
                        Qty = item.Qty > 0 ? item.Qty : 1,
                        Rot = true
                    });
                }

                // Build status message
                string dimStatus;
                if (useCustom)
                {
                    var altCount = _specDimChoice.Count(kvp => kvp.Value);
                    if (altCount == 0)
                        dimStatus = "W1/H1 (all)";
                    else if (altCount == _specDimChoice.Count)
                        dimStatus = "W2/H2 (all)";
                    else
                        dimStatus = $"Mixed ({altCount} specs W2/H2)";
                }
                else if (globalUseAlt)
                    dimStatus = "W2/H2";
                else
                    dimStatus = "W1/H1";

                System.Diagnostics.Debug.WriteLine($"[RunOptimization] Total items: {parts.Count}, Mode: {dimStatus}");

                // Logging: how many items actually have alternate dims and radio states
                int altAvailable = items.Count(i => (i.Width2 > 0 || i.Height2 > 0));
                System.Diagnostics.Debug.WriteLine($"[RunOptimization] AltAvailable={altAvailable}/{items.Count}, rbUseW2H2={(rbUseW2H2?.IsChecked == true)}, rbDimsW2H2={(rbDimsW2H2?.IsChecked == true)}");

                // If user selected W2/H2 but no items contain W2/H2, warn and fall back to W1/H1
                if (globalUseAlt && altAvailable == 0)
                {
                    MessageBox.Show("W2/H2 selected but no alternate dimensions found in items. Falling back to W1/H1.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    globalUseAlt = false;
                }

                // Create optimizer view (don't show - just run for results)
                var optView = new OptimizationView();

                // Convert original items using appropriate dimensions per item
                var invoiceItems = new List<Models.InvoiceItemModel>();

                foreach (var i in items)
                {
                    bool useAltForItem = globalUseAlt;

                    // Check per-spec selection if custom mode
                    if (useCustom && FindSpecForItem(i, out string sn) && _specDimChoice.ContainsKey(sn))
                    {
                        useAltForItem = _specDimChoice[sn];
                    }

                    invoiceItems.Add(new Models.InvoiceItemModel
                    {
                        GlassRef = i.GlassRef,
                        Width1 = useAltForItem ? (i.Width2 > 0 ? i.Width2 : i.Width1) : (i.Width1 > 0 ? i.Width1 : i.Width2),
                        Height1 = useAltForItem ? (i.Height2 > 0 ? i.Height2 : i.Height1) : (i.Height1 > 0 ? i.Height1 : i.Height2),
                        Qty = i.Qty > 0 ? i.Qty : 1
                    });
                }

                // Set data
                optView.ImportInvoiceItems(invoiceItems);
                optView.SetStockSheet(sheetWidth, sheetHeight);
                optView.SetTrimSettings(trim.LM, trim.RM, trim.TM, trim.BM, trim.BreakoutMin, trim.Kerf);

                // Run optimization
                optView.RunOptimizationFromInvoice();

                // Get results - update center section only
                double utilization = optView.AverageUtilization;
                int sheetsUsed = optView.SheetsUsed;

                txtUtilization.Text = $"{utilization:N1}%";
                txtSheetsUsed.Text = sheetsUsed.ToString();
                txtWastage.Text = $"{(100 - utilization):N1}%";
                txtOptStatus.Text = $"✓ Optimized ({txtSheetWidth.Text}×{txtSheetHeight.Text}mm) — {dimStatus}";

                // Increment run count
                int currentCount = 0;
                if (int.TryParse(txtOptRunCount.Text, out int c)) currentCount = c;
                txtOptRunCount.Text = (currentCount + 1).ToString();

                // Update per-sheet results
                UpdatePerSheetResults(optView, sheetWidth, sheetHeight);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== VIEW FULL LAYOUTS ====================

        private void ViewOptimizationLayouts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Default sheet size if empty
                if (string.IsNullOrWhiteSpace(txtSheetWidth.Text)) txtSheetWidth.Text = "3660";
                if (string.IsNullOrWhiteSpace(txtSheetHeight.Text)) txtSheetHeight.Text = "2440";

                // Get sheet size
                if (!double.TryParse(txtSheetWidth.Text, out double sheetWidth) || sheetWidth <= 0)
                {
                    MessageBox.Show("Please enter valid sheet width!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!double.TryParse(txtSheetHeight.Text, out double sheetHeight) || sheetHeight <= 0)
                {
                    MessageBox.Show("Please enter valid sheet height!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get thickness with null safe check
                string thickness = (cmbThickness.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "6mm";

                if (!_trimTable.ContainsKey(thickness))
                {
                    thickness = "6mm";
                }
                TrimSettings trim = _trimTable[thickness];

                // Get selected specifications from ListBox (wrappers) or all specs
                var selectedSpecNames = new List<string>();

                if (chkSpecWise.IsChecked == true && lstSpecSelect.SelectedItems.Count > 0)
                {
                    foreach (SpecSelectionItem s in lstSpecSelect.SelectedItems)
                        if (s != null && !string.IsNullOrEmpty(s.FullName))
                            selectedSpecNames.Add(s.FullName);
                }

                // If nothing selected or checkbox unchecked, use all specs
                if (selectedSpecNames.Count == 0 && _viewModel?.Invoice?.Specifications != null)
                {
                    selectedSpecNames.AddRange(_viewModel.Invoice.Specifications
                        .Where(s => s != null)
                        .Select(s => s.SpecificationName));
                }

                // Now get the items from those specs
                var items = new List<Models.InvoiceItemModel>();
                foreach (var specName in selectedSpecNames)
                {
                    var spec = _viewModel.Invoice.Specifications
                        .FirstOrDefault(s => s?.SpecificationName == specName);
                    if (spec?.Items == null) continue;
                    foreach (var item in spec.Items)
                        if (item != null) items.Add(item);
                }

                if (items.Count == 0)
                {
                    MessageBox.Show("No items to optimize!", "Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Determine mode
                bool globalUseAlt = (rbUseW2H2 != null && rbUseW2H2.IsChecked == true);
                bool useCustom = (rbUseCustom != null && rbUseCustom.IsChecked == true);

                if (!globalUseAlt && rbDimsW2H2 != null && rbDimsW2H2.IsChecked == true)
                    globalUseAlt = true;

                System.Diagnostics.Debug.WriteLine($"[ViewOptimizationLayouts] START - globalUseAlt={globalUseAlt}, useCustom={useCustom}");

                // Convert using appropriate dims
                var invoiceItems = items.Select(i =>
                {
                    bool useAltForItem = globalUseAlt;
                    if (FindSpecForItem(i, out string sn) && useCustom && _specDimChoice.ContainsKey(sn))
                        useAltForItem = _specDimChoice[sn];

                    return new Models.InvoiceItemModel
                    {
                        GlassRef = i.GlassRef,
                        Width1 = useAltForItem ? (i.Width2 > 0 ? i.Width2 : i.Width1) : (i.Width1 > 0 ? i.Width1 : i.Width2),
                        Height1 = useAltForItem ? (i.Height2 > 0 ? i.Height2 : i.Height1) : (i.Height1 > 0 ? i.Height1 : i.Height2),
                        Qty = i.Qty > 0 ? i.Qty : 1
                    };
                }).ToList();

                // Build status message
                string dimStatus;
                if (useCustom)
                {
                    var altCount = _specDimChoice.Count(kvp => kvp.Value);
                    if (altCount == 0)
                        dimStatus = "W1/H1 (all)";
                    else if (altCount == _specDimChoice.Count)
                        dimStatus = "W2/H2 (all)";
                    else
                        dimStatus = $"Mixed ({altCount} specs W2/H2)";
                }
                else if (globalUseAlt)
                    dimStatus = "W2/H2";
                else
                    dimStatus = "W1/H1";

                // Create and show FULL optimization window
                var optWindow = new Window
                {
                    Title = "Glass Cut Optimizer - ProGlass Automation",
                    Content = new OptimizationView(),
                    Width = 1400,
                    Height = 900,
                    Background = new SolidColorBrush(Color.FromRgb(30, 39, 46)),
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                var optView = optWindow.Content as OptimizationView;

                if (optView != null)
                {
                    optView.ImportInvoiceItems(invoiceItems);
                    optView.SetStockSheet(sheetWidth, sheetHeight);
                    optView.SetTrimSettings(trim.LM, trim.RM, trim.TM, trim.BM, trim.BreakoutMin, trim.Kerf);
                    optView.RunOptimizationFromInvoice();

                    optWindow.Show();

                    // Also update center section
                    double utilization = optView.AverageUtilization;
                    int sheetsUsed = optView.SheetsUsed;

                    txtUtilization.Text = $"{utilization:N1}%";
                    txtSheetsUsed.Text = sheetsUsed.ToString();
                    txtWastage.Text = $"{(100 - utilization):N1}%";
                    txtOptStatus.Text = $"✓ Optimized ({txtSheetWidth.Text}×{txtSheetHeight.Text}mm) — {dimStatus}";

                    // Update per-sheet results
                    UpdatePerSheetResults(optView, sheetWidth, sheetHeight);
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

        // ==================== HELPER METHODS ====================

        private bool FindSpecForItem(Models.InvoiceItemModel item, out string specName)
        {
            specName = "";
            var spec = FindSpecification(item);
            if (spec != null)
            {
                specName = spec.SpecificationName;
                return true;
            }
            return false;
        }

        private void UpdatePerSheetResults(OptimizationView optView, double sheetWidth, double sheetHeight)
        {
            try
            {
                SheetResults.Clear();

                // Get ACTUAL per-sheet results from optimizer
                var results = optView.GetResultsList();

                if (results == null || results.Count == 0)
                {
                    // Fallback: Single sheet size = show ONE consolidated result
                    int totalSheets = optView.SheetsUsed;
                    double areaTotal = sheetWidth * sheetHeight / 1_000_000;
                    int totalPieces = (int)(optView.AverageUtilization * 10);
                    double util = optView.AverageUtilization;
                    double wastage = 100 - util;
                    double areaUsed = areaTotal * (util / 100);

                    SheetResults.Add(new SheetResultItem
                    {
                        SheetName = $"{sheetWidth:N0} × {sheetHeight:N0} mm",
                        SheetDimensions = $"{sheetWidth:N0} × {sheetHeight:N0} mm",
                        PiecesCut = totalPieces.ToString(),
                        AreaUsed = $"{areaUsed * totalSheets:N2} m²",
                        Utilization = $"{util:N1}",
                        Wastage = $"{wastage:N1}",
                        BarHeight = util * 0.8
                    });
                }
                else
                {
                    // CONSOLIDATE by sheet dimensions (same size = single entry)
                    // Use reflection or safe property access to get available values
                    var groupedResults = results
                        .GroupBy(r => new { r.L, r.W })
                        .Select(g => new
                        {
                            SheetDimensions = $"{g.Key.L:N0} × {g.Key.W:N0} mm",
                            // Count sheets in this group instead of Qty
                            SheetCount = g.Count(),
                            TotalArea = g.Sum(x => x.Area),
                            AvgUtil = g.Average(x => x.Util)
                        })
                        .OrderByDescending(g => g.AvgUtil)
                        .ToList();

                    int sheetIndex = 1;
                    foreach (var group in groupedResults)
                    {
                        double wastage = 100 - group.AvgUtil;

                        // Calculate pieces from area (approx) or use sheet count
                        // Since we don't have direct piece count, use sheet count as proxy
                        int piecesEst = group.SheetCount; // This is actually sheet count

                        SheetResults.Add(new SheetResultItem
                        {
                            SheetName = group.SheetDimensions,
                            SheetDimensions = group.SheetDimensions,
                            PiecesCut = group.SheetCount.ToString(), // Show sheet count
                            AreaUsed = $"{group.TotalArea:N2} m²",
                            Utilization = $"{group.AvgUtil:N1}",
                            Wastage = $"{wastage:N1}",
                            BarHeight = group.AvgUtil * 0.8
                        });

                        sheetIndex++;
                    }
                }

                // Update sheets used count (show unique sheet sizes, not count)
                int uniqueSizes = SheetResults.Select(r => r.SheetDimensions).Distinct().Count();
                int totalSheetsUsed = optView.SheetsUsed;

                if (uniqueSizes == 1)
                    txtSheetsUsedCount.Text = $"{totalSheetsUsed} sheets ({SheetResults.First().SheetDimensions})";
                else
                    txtSheetsUsedCount.Text = $"{totalSheetsUsed} sheets ({uniqueSizes} sizes)";

                // Show/hide empty state
                EmptyPerSheetResults.Visibility = SheetResults.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdatePerSheetResults] Error: {ex.Message}");

                // FALLBACK: Show simple single result on error
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

        // ==================== TEXT SELECTION ON FOCUS ====================

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

        // ==================== PHONE NUMBER VALIDATION ====================

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

        // ==================== SMOOTH SCROLLING ====================

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
            if (obj is ScrollViewer scrollViewer) return scrollViewer;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }

        // ==================== EXISTING HANDLERS ====================

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

        // ==================== PASTE FROM EXCEL ====================

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

                // PATCH 18: Null check for ViewModel
                if (_viewModel == null || _viewModel.Invoice == null)
                {
                    MessageBox.Show("Invoice not loaded. Please create or open an invoice first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var spec = _viewModel.SelectedTargetSpecification;
                if (spec == null)
                {
                    if (_viewModel.Invoice.Specifications.Count == 0)
                        _viewModel.AddSpecificationCommand.Execute(null);

                    if (_viewModel.Invoice.Specifications.Count == 0)
                    {
                        MessageBox.Show("Could not create specification. Please add a specification manually.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                // PATCH 18 FIX: Use BulkUpdateScope() - returns IDisposable
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

                MessageBox.Show($"✅ Pasted {itemsAdded} items from Excel!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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

        // ==================== PRINT HANDLERS ====================

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
                        previewWindow.LayoutTransform = new System.Windows.Media.ScaleTransform(scale, scale);
                    }

                    printDialog.PrintVisual(previewWindow, "ProForma Invoice");
                    previewWindow.LayoutTransform = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing invoice: {ex.Message}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Error opening print preview: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== KEY HANDLERS ====================

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
            catch { }
        }

        private void HandleEnterKey(DataGrid dataGrid)
        {
            try
            {
                if (dataGrid == null || !dataGrid.CurrentCell.IsValid) return;

                var currentItem = dataGrid.CurrentCell.Item as Models.InvoiceItemModel;
                if (currentItem == null) return;

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
                            System.Diagnostics.Debug.WriteLine($"[HandleEnterKey] Error: {ex.Message}");
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HandleEnterKey] Error: {ex.Message}");
            }
        }

        private void HandleTabKey(DataGrid dataGrid)
        {
            try
            {
                var currentItem = dataGrid.CurrentCell.Item as Models.InvoiceItemModel;
                if (currentItem == null) return;

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
                            System.Diagnostics.Debug.WriteLine($"[HandleTabKey] Error: {ex.Message}");
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HandleTabKey] Error: {ex.Message}");
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

        // ==================== OTHER CHARGES HANDLERS ====================

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

        private void SpecSelectorBorder_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
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
                MessageBox.Show("No specifications available.\nAdd at least one specification first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    IsChecked = charge.SpecIndexList.Contains(i)
                };
                checkBox.Tag = specIndex;
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

        // ==================== PER-SPEC DIMENSION SELECTION ====================

        private void OptSpecDim_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string dimTag)
            {
                try
                {
                    string specName = rb.GroupName;

                    if (string.IsNullOrEmpty(specName))
                    {
                        System.Diagnostics.Debug.WriteLine("[OptSpecDim_Click] WARN: Empty GroupName, skipping");
                        return;
                    }

                    bool useAlt = (dimTag == "W2/H2");

                    _specDimChoice[specName] = useAlt;

                    if (_viewModel?.Invoice?.Specifications != null)
                    {
                        var spec = _viewModel.Invoice.Specifications.FirstOrDefault(s => s?.SpecificationName == specName);
                        if (spec != null && !lstSpecSelect.SelectedItems.Contains(spec))
                        {
                            lstSpecSelect.SelectedItems.Add(spec);
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[OptSpecDim_Click] ✓ Spec='{specName}', Dim={dimTag}, UseAlt={useAlt}");

                    UpdateListBoxRadioButtons(lstSpecSelect);
                    UpdateOptStatus();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[OptSpecDim_Click] ERROR: {ex.Message}");
                }
            }
        }

        private void UpdateOptStatus()
        {
            if (_specDimChoice.Count == 0)
            {
                txtOptStatus.Text = "Select specs and choose dimensions";
                return;
            }

            var w2h2Count = _specDimChoice.Count(kvp => kvp.Value);
            var w1h1Count = _specDimChoice.Count - w2h2Count;

            if (w2h2Count == 0)
                txtOptStatus.Text = "All specs: W1/H1";
            else if (w1h1Count == 0)
                txtOptStatus.Text = "All specs: W2/H2";
            else
                txtOptStatus.Text = $"W1/H1: {w1h1Count} specs, W2/H2: {w2h2Count} specs";
        }

        // ==================== FALLBACK DIMENSION CLICK ====================

        private void FallbackDim_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                string fallbackType = rb.Name;
                bool useAltW2H2 = (fallbackType == "rbUseW2H2");

                System.Diagnostics.Debug.WriteLine($"[FallbackDim_Click] Fallback changed: {fallbackType}, useAltW2H2={useAltW2H2}");

                if (fallbackType == "rbUseCustom")
                {
                    txtOptStatus.Text = "Select specs and choose dimensions";
                    return;
                }

                if (_viewModel?.Invoice?.Specifications != null)
                {
                    foreach (var spec in _viewModel.Invoice.Specifications)
                    {
                        if (spec != null && !string.IsNullOrEmpty(spec.SpecificationName))
                        {
                            _specDimChoice[spec.SpecificationName] = useAltW2H2;
                        }
                    }
                }

                // Clear and re-select all specs using wrappers
                lstSpecSelect.SelectedItems.Clear();
                RefreshSpecSelectionItems(); // Refresh to get latest wrappers

                // Select all wrappers
                foreach (var wrapper in SpecSelectionItems)
                {
                    lstSpecSelect.SelectedItems.Add(wrapper);
                }

                txtOptStatus.Text = useAltW2H2 ? "All specs: W2/H2" : "All specs: W1/H1";

                // Force UI refresh
                UpdateSpecSelectUIRadioButtons();

                // FIX: Now update RadioButton visual states to show W1/H1 or W2/H2 as selected
                UpdateListBoxRadioButtons(lstSpecSelect);

                System.Diagnostics.Debug.WriteLine($"[FallbackDim_Click] Updated {_specDimChoice.Count} specs to {(useAltW2H2 ? "W2/H2" : "W1/H1")}");
            }
        }

        private void UpdateSpecSelectUIRadioButtons()
        {
            try
            {
                // Set flag to prevent selection changed from resetting dictionary
                _isUpdatingSpecSelection = true;

                // Force visual update - dispatch to ensure ListBox items are rendered
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
                {
                    try
                    {
                        // Save current selection (now using SpecSelectionItem wrappers)
                        var selectedWrappers = lstSpecSelect.SelectedItems.Cast<SpecSelectionItem>().ToList();
                        var selectedSpecNames = selectedWrappers
                            .Where(w => w != null)
                            .Select(w => w.FullName)
                            .ToList();

                        // Clear and refresh ListBox items to force re-render
                        var items = lstSpecSelect.ItemsSource;
                        lstSpecSelect.ItemsSource = null;
                        lstSpecSelect.ItemsSource = items;

                        // Clear dictionary and rebuild from scratch using ALL specs from VM
                        _specDimChoice.Clear();
                        if (_viewModel?.Invoice?.Specifications != null)
                        {
                            foreach (var spec in _viewModel.Invoice.Specifications)
                            {
                                if (spec != null && !string.IsNullOrEmpty(spec.SpecificationName))
                                {
                                    // Default to W1/H1 (false), unless user explicitly chose W2/H2 before
                                    _specDimChoice[spec.SpecificationName] = false;
                                }
                            }
                        }

                        // Restore selection - find wrapper by name
                        lstSpecSelect.SelectedItems.Clear();
                        foreach (var specName in selectedSpecNames)
                        {
                            var wrapper = SpecSelectionItems.FirstOrDefault(w => w.FullName == specName);
                            if (wrapper != null)
                                lstSpecSelect.SelectedItems.Add(wrapper);
                        }

                        // Force update RadioButtons with delay to ensure visual tree is ready
                        System.Threading.Thread.Sleep(50);
                        UpdateListBoxRadioButtons(lstSpecSelect);

                        // Update status
                        UpdateOptStatus();

                        System.Diagnostics.Debug.WriteLine($"[UpdateSpecSelectUIRadioButtons] UI refreshed");
                    }
                    catch (Exception ex2)
                    {
                        System.Diagnostics.Debug.WriteLine($"[UpdateSpecSelectUIRadioButtons] Inner ERROR: {ex2.Message}");
                    }
                    finally
                    {
                        _isUpdatingSpecSelection = false;
                    }
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateSpecSelectUIRadioButtons] ERROR: {ex.Message}");
                _isUpdatingSpecSelection = false;
            }
        }

        private void UpdateListBoxRadioButtons(ListBox listBox)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateListBoxRadioButtons] Processing {listBox.Items.Count} items...");

                // Iterate through ListBox items and find RadioButtons
                for (int i = 0; i < listBox.Items.Count; i++)
                {
                    var container = listBox.ItemContainerGenerator.ContainerFromIndex(i);
                    if (container == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[UpdateListBoxRadioButtons] Container {i} is NULL");
                        continue;
                    }

                    // Find RadioButtons in this container
                    var radios = FindVisualChildren<RadioButton>(container);
                    foreach (var rb in radios)
                    {
                        string specName = rb.GroupName;
                        string tag = rb.Tag?.ToString() ?? "";

                        System.Diagnostics.Debug.WriteLine($"[UpdateListBoxRadioButtons] Found RB: Group={specName}, Tag={tag}");

                        if (string.IsNullOrEmpty(specName) || !_specDimChoice.ContainsKey(specName))
                            continue;

                        bool useAlt = _specDimChoice[specName];

                        // FIX: Use correct tags with forward slash!
                        if (tag == "W1/H1")
                        {
                            rb.IsChecked = !useAlt;
                            System.Diagnostics.Debug.WriteLine($"[UpdateListBoxRadioButtons] W1/H1 IsChecked={!useAlt}");
                        }
                        else if (tag == "W2/H2")
                        {
                            rb.IsChecked = useAlt;
                            System.Diagnostics.Debug.WriteLine($"[UpdateListBoxRadioButtons] W2/H2 IsChecked={useAlt}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateListBoxRadioButtons] ERROR: {ex.Message}");
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t)
                    yield return t;

                foreach (var childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }

        // Add these methods to the code-behind file (after the other button handlers)

        private void BtnSelectAllSpecs_Click(object sender, RoutedEventArgs e)
        {
            lstSpecSelect.SelectAll();
        }

        private void BtnClearSpecSelection_Click(object sender, RoutedEventArgs e)
        {
            lstSpecSelect.SelectedItems.Clear();
            _specDimChoice.Clear();
            txtOptStatus.Text = "Select specs and choose dimensions";
            UpdateSpecSelectUIRadioButtons();
        }

        private void RbSpecW1H1_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string specName)
            {
                try
                {
                    if (!string.IsNullOrEmpty(specName))
                    {
                        _specDimChoice[specName] = false; // W1/H1 = false
                        System.Diagnostics.Debug.WriteLine($"[RbSpecW1H1_Click] Spec='{specName}', Dim=W1/H1");

                        // Auto-select spec if not selected
                        // Auto-select wrapper if not selected
                        var wrapper = SpecSelectionItems.FirstOrDefault(w => w.FullName == specName);
                        if (wrapper != null && !lstSpecSelect.SelectedItems.Contains(wrapper))
                        {
                            lstSpecSelect.SelectedItems.Add(wrapper);
                        }

                        UpdateListBoxRadioButtons(lstSpecSelect);
                        UpdateOptStatus();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[RbSpecW1H1_Click] ERROR: {ex.Message}");
                }
            }
        }

        private void RbSpecW2H2_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string specName)
            {
                try
                {
                    if (!string.IsNullOrEmpty(specName))
                    {
                        _specDimChoice[specName] = true; // W2/H2 = true
                        System.Diagnostics.Debug.WriteLine($"[RbSpecW2H2_Click] Spec='{specName}', Dim=W2/H2");

                        // Auto-select wrapper if not selected
                        var wrapper = SpecSelectionItems.FirstOrDefault(w => w.FullName == specName);
                        if (wrapper != null && !lstSpecSelect.SelectedItems.Contains(wrapper))
                        {
                            lstSpecSelect.SelectedItems.Add(wrapper);
                        }

                        UpdateListBoxRadioButtons(lstSpecSelect);
                        UpdateOptStatus();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[RbSpecW2H2_Click] ERROR: {ex.Message}");
                }
            }
        }

        // ==================== SPEC LIST SELECTION CHANGED ====================

        private void lstSpecSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Skip if we're doing programmatic update
            if (_isUpdatingSpecSelection) return;

            try
            {
                // FIX: The ListBox.SelectedItems contains the actual selected wrapper objects
                // We need to check which wrappers are in SelectedItems, NOT wrapper.IsSelected
                var selectedWrappers = lstSpecSelect.SelectedItems
                    .Cast<SpecSelectionItem>()
                    .ToHashSet();

                // Add newly selected specs to tracking (default to W1/H1 = false)
                foreach (var wrapper in selectedWrappers)
                {
                    if (!string.IsNullOrEmpty(wrapper.FullName))
                    {
                        if (!_specDimChoice.ContainsKey(wrapper.FullName))
                        {
                            _specDimChoice[wrapper.FullName] = false; // Default W1/H1
                            System.Diagnostics.Debug.WriteLine($"[lstSpecSelect_SelectionChanged] Added: {wrapper.FullName}");
                        }
                    }
                }

                // Remove unselected specs from tracking
                var selectedSpecNames = selectedWrappers
                    .Where(w => !string.IsNullOrEmpty(w.FullName))
                    .Select(w => w.FullName)
                    .ToHashSet();
                var toRemove = _specDimChoice.Keys.Where(k => !selectedSpecNames.Contains(k)).ToList();
                foreach (var key in toRemove)
                {
                    _specDimChoice.Remove(key);
                    System.Diagnostics.Debug.WriteLine($"[lstSpecSelect_SelectionChanged] Removed: {key}");
                }

                System.Diagnostics.Debug.WriteLine($"[lstSpecSelect_SelectionChanged] Selected wrappers: {selectedWrappers.Count}, Tracked specs: {_specDimChoice.Count}");
                UpdateOptStatus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[lstSpecSelect_SelectionChanged] ERROR: {ex.Message}");
            }
        }
    }

        // ==================== SCROLL BEHAVIOR HELPER ====================
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