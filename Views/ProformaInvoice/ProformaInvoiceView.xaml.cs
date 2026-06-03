using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ProGlassAutomation.Models;
using ProGlassAutomation.ViewModels;

namespace ProGlassAutomation.Views.ProformaInvoice
{
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
            if (sender is Button button && button.Tag is SpecificationModel spec)
                _viewModel.AddItemWithPrice(spec);
        }

        private void ToggleLM_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProformaInvoiceViewModel vm)
                vm.IsLMVisible = !vm.IsLMVisible;
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is InvoiceItemModel item)
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

                        var item = new InvoiceItemModel { SrNo = startRow + itemsAdded + 1 };

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

                var currentItem = dataGrid.CurrentCell.Item as InvoiceItemModel;
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
                var currentItem = dataGrid.CurrentCell.Item as InvoiceItemModel;
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

        private SpecificationModel? FindSpecification(InvoiceItemModel item)
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