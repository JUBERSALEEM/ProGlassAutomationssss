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

// Aliases to avoid ambiguity
using ModelInvoice = ProGlassAutomation.Models.InvoiceItemModel;

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

        public ProformaInvoiceView()
        {
            InitializeComponent();

            // Use shared ProformaInvoiceViewModel (SAME instance as DailyWorks)
            _viewModel = SharedViewModels.ProformaInvoiceVM;
            DataContext = _viewModel;

            // PATCH 18 - Register for cleanup to prevent memory leaks
            Unloaded += ProformaInvoiceView_Unloaded;

            System.Diagnostics.Debug.WriteLine("[ProformaInvoiceView] Using SharedViewModels.ProformaInvoiceVM");
        }

        // ==================== PATCH 18 - CLEANUP ON UNLOAD ====================

        private void ProformaInvoiceView_Unloaded(object sender, RoutedEventArgs e)
        {
            // PATCH 18 FIX - Proper cleanup to prevent memory leaks
            Unloaded -= ProformaInvoiceView_Unloaded;

            // PATCH 18 FIX: Don't clear DataContext - shared VM handles its own cleanup
            System.Diagnostics.Debug.WriteLine("[ProformaInvoiceView] Unloaded - Cleanup complete");
        }

        // ==================== EXPANDABLE PANEL CLICK HANDLERS ====================

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
                // Just toggle - same as File/Opt/Summary
                _viewModel.IsSGUSelected = !_viewModel.IsSGUSelected;
            }
        }

        private void ToggleDGUPanel_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                // Just toggle - same as File/Opt/Summary
                _viewModel.IsDGUSelected = !_viewModel.IsDGUSelected;
            }
        }

        private void ToggleLAMPanel_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                // Just toggle - same as File/Opt/Summary
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

        // ==================== OPTIMIZATION POPUP ====================

        // ==================== TRIM TABLE ====================
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

        // Static references
        private static Window? _optimizerWindow;
        private static OptimizationView? _optimizerView;

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

        // ==================== CUTPART FOR OPTIMIZER ====================

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
                var items = new List<Models.InvoiceItemModel>();
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
                txtOptStatus.Text = $"✓ Optimized ({txtSheetWidth.Text}×{txtSheetHeight.Text}mm) — {dimStatus}";
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

                // Get selected specifications or all
                var items = new List<Models.InvoiceItemModel>();
                var selectedSpecs = new List<Models.SpecificationModel>();
                if (chkSpecWise.IsChecked == true && lstSpecSelect.SelectedItems.Count > 0)
                {
                    foreach (Models.SpecificationModel s in lstSpecSelect.SelectedItems)
                        selectedSpecs.Add(s);
                }
                else
                {
                    if (_viewModel?.Invoice?.Specifications != null)
                        selectedSpecs.AddRange(_viewModel.Invoice.Specifications);
                }

                foreach (var spec in selectedSpecs)
                {
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

                // Build parts using appropriate dimensions (reuse existing items list)
                var parts = new List<CutPart>();

                foreach (var item in items)
                {
                    bool useAlt = false;

                    // Find which spec this item belongs to
                    if (FindSpecForItem(item, out string specName))
                    {
                        if (useCustom && _specDimChoice.ContainsKey(specName))
                        {
                            // Use per-spec selection
                            useAlt = _specDimChoice[specName];
                        }
                        else
                        {
                            // Use global selection
                            useAlt = globalUseAlt;
                        }
                    }
                    else
                    {
                        useAlt = globalUseAlt;
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
                    txtOptStatus.Text = $"✓ Optimized ({txtSheetWidth.Text}×{txtSheetHeight.Text}mm) — {dimStatus}";
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
            Regex regex = new Regex(@"^[0-9\+\-\s\$\$]+$");
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
            catch (System.Exception ex)
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
            catch (System.Exception ex)
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
                // FIX: DataGridCellInfo is a struct - use IsValid property
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
                // FIX: Check IsValid instead of nullable check
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
                        catch (System.Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[HandleEnterKey] Error: {ex.Message}");
                        }
                    }));
                }
            }
            catch (System.Exception ex)
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
                        catch (System.Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[HandleTabKey] Error: {ex.Message}");
                        }
                    }));
                }
            }
            catch (System.Exception ex)
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

        private void lstSpecSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Track ONLY newly selected specs - don't clear existing choices
                foreach (Models.SpecificationModel spec in lstSpecSelect.SelectedItems)
                {
                    if (spec != null && !_specDimChoice.ContainsKey(spec.SpecificationName))
                    {
                        _specDimChoice[spec.SpecificationName] = false; // Default to W1/H1
                    }
                }

                // Remove any unselected specs from tracking
                var selectedSpecs = lstSpecSelect.SelectedItems.Cast<Models.SpecificationModel>()
                    .Select(s => s.SpecificationName).ToHashSet();
                var toRemove = _specDimChoice.Keys.Where(k => !selectedSpecs.Contains(k)).ToList();
                foreach (var key in toRemove)
                {
                    _specDimChoice.Remove(key);
                }

                System.Diagnostics.Debug.WriteLine($"[lstSpecSelect_SelectionChanged] Tracked specs: {_specDimChoice.Count}");
                UpdateOptStatus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[lstSpecSelect_SelectionChanged] ERROR: {ex.Message}");
            }
        }

        private void OptSpecDim_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string dimTag)
            {
                try
                {
                    // Get spec name from the RadioButton's GroupName (set in XAML binding)
                    string specName = rb.GroupName;

                    // Validate spec name
                    if (string.IsNullOrEmpty(specName))
                    {
                        System.Diagnostics.Debug.WriteLine("[OptSpecDim_Click] WARN: Empty GroupName, skipping");
                        return;
                    }

                    bool useAlt = (dimTag == "W2H2");

                    // Store the choice
                    _specDimChoice[specName] = useAlt;

                    // Auto-select this spec in the ListBox if not already selected
                    if (_viewModel?.Invoice?.Specifications != null)
                    {
                        var spec = _viewModel.Invoice.Specifications.FirstOrDefault(s => s?.SpecificationName == specName);
                        if (spec != null && !lstSpecSelect.SelectedItems.Contains(spec))
                        {
                            lstSpecSelect.SelectedItems.Add(spec);
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[OptSpecDim_Click] ✓ Spec='{specName}', Dim={dimTag}, UseAlt={useAlt}");

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

        private void btnSelectAllSpecs_Click(object sender, RoutedEventArgs e)
        {
            lstSpecSelect.SelectAll();
        }

        private void btnClearSpecSelection_Click(object sender, RoutedEventArgs e)
        {
            lstSpecSelect.SelectedItems.Clear();
            _specDimChoice.Clear();
            txtOptStatus.Text = "Select specs and choose dimensions";
        }

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