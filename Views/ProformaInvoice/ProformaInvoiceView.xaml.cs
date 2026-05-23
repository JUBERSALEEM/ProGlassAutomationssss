using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProGlassAutomation.Models;
using ProGlassAutomation.ViewModels;

namespace ProGlassAutomation.Views.ProformaInvoice
{
    public partial class ProformaInvoiceView : UserControl
    {
        private ProformaInvoiceViewModel _viewModel;

        public ProformaInvoiceView()
        {
            InitializeComponent();
            _viewModel = new ProformaInvoiceViewModel();
            DataContext = _viewModel;
        }

        // ==================== TEXT SELECTION ON FOCUS ====================

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.SelectAll();
            }
        }

        private void TextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (!tb.IsFocused)
                {
                    tb.Focus();
                    e.Handled = true;
                }
            }
        }

        private void ComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox cb)
            {
                cb.IsDropDownOpen = true;
            }
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
            if (sender is TextBox tb && tb.Text == "+971-")
            {
                tb.Text = "";
            }
        }

        // ==================== EXISTING HANDLERS ====================

        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SpecificationModel spec)
            {
                _viewModel.AddItemWithPrice(spec);
            }
        }

        private void ToggleLM_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProformaInvoiceViewModel vm)
            {
                vm.IsLMVisible = !vm.IsLMVisible;
            }
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is InvoiceItemModel item)
            {
                _viewModel.RemoveItem(item);
            }
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

                // Get selected specification
                var spec = _viewModel.SelectedTargetSpecification;
                if (spec == null)
                {
                    if (_viewModel.Invoice.Specifications.Count == 0)
                        _viewModel.AddSpecificationCommand.Execute(null);
                    spec = _viewModel.SelectedTargetSpecification ?? _viewModel.Invoice.Specifications[0];
                }

                // Split clipboard by rows and columns
                var rows = clipboardText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (rows.Length == 0)
                {
                    MessageBox.Show("No data to paste!", "Paste", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check if first row is header (contains GlassRef, W1, H1, etc.)
                bool skipHeader = false;
                var firstRowLower = rows[0].ToLower();
                if (firstRowLower.Contains("glass") || firstRowLower.Contains("width") ||
                    firstRowLower.Contains("height") || firstRowLower.Contains("qty") ||
                    firstRowLower.Contains("ref"))
                {
                    skipHeader = true;
                }

                int startRow = spec.Items.Count > 0 ? spec.Items.Count : 0;
                int itemsAdded = 0;
                int startIndex = skipHeader ? 1 : 0;

                // Get default values from first item if exists
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

                for (int i = startIndex; i < rows.Length; i++)
                {
                    var row = rows[i];
                    var columns = row.Split('\t');

                    if (columns.Length == 0 || string.IsNullOrWhiteSpace(string.Join("", columns).Replace("\t", "")))
                        continue;

                    var item = new InvoiceItemModel
                    {
                        SrNo = startRow + itemsAdded + 1
                    };

                    // Column 0: Glass Reference
                    if (columns.Length > 0)
                        item.GlassRef = columns[0].Trim();

                    // Column 1: Width 1
                    if (columns.Length > 1 && double.TryParse(columns[1].Trim().Replace(",", ""), out double w1))
                        item.Width1 = w1;
                    else
                        item.Width1 = defaultWidth1;

                    // Column 2: Height 1
                    if (columns.Length > 2 && double.TryParse(columns[2].Trim().Replace(",", ""), out double h1))
                        item.Height1 = h1;
                    else
                        item.Height1 = defaultHeight1;

                    // Column 3: Width 2 (optional)
                    if (columns.Length > 3 && double.TryParse(columns[3].Trim().Replace(",", ""), out double w2))
                        item.Width2 = w2;
                    else
                        item.Width2 = defaultWidth2;

                    // Column 4: Height 2 (optional)
                    if (columns.Length > 4 && double.TryParse(columns[4].Trim().Replace(",", ""), out double h2))
                        item.Height2 = h2;
                    else
                        item.Height2 = defaultHeight2;

                    // Column 5: Quantity
                    if (columns.Length > 5 && int.TryParse(columns[5].Trim().Replace(",", ""), out int qty))
                        item.Qty = qty;
                    else
                        item.Qty = 1;

                    // Column 6: Price (optional)
                    if (columns.Length > 6 && double.TryParse(columns[6].Trim().Replace(",", ""), out double price))
                        item.Price = price;
                    else
                        item.Price = defaultPrice;

                    // Column 7: Surcharge % (optional)
                    if (columns.Length > 7 && double.TryParse(columns[7].Trim().Replace(",", "").Replace("%", ""), out double surcharge))
                        item.SurchargePercent = surcharge;
                    else
                        item.SurchargePercent = defaultSurcharge;

                    spec.Items.Add(item);
                    itemsAdded++;
                }

                // Recalculate totals
                spec.CalculateTotals();
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
                var printPreview = new ProformaInvoicePrintPreviewView
                {
                    DataContext = DataContext
                };

                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    var content = printPreview.Content as FrameworkElement;
                    if (content != null)
                    {
                        var pageWidth = printDialog.PrintableAreaWidth;
                        var pageHeight = printDialog.PrintableAreaHeight;
                        var contentWidth = content.ActualWidth;
                        var contentHeight = content.ActualHeight;

                        if (contentWidth > 0 && contentHeight > 0)
                        {
                            var scaleX = pageWidth / contentWidth;
                            var scaleY = pageHeight / contentHeight;
                            var scale = Math.Min(scaleX, scaleY);

                            content.LayoutTransform = new System.Windows.Media.ScaleTransform(scale, scale);
                        }

                        printDialog.PrintVisual(content, "ProForma Invoice");
                        content.LayoutTransform = null;
                    }
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
                var printPreview = new ProformaInvoicePrintPreviewView
                {
                    DataContext = DataContext
                };

                var scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = printPreview
                };

                var window = new Window
                {
                    Content = scrollViewer,
                    Title = "Print Preview - ProForma Invoice",
                    Width = 1100,
                    Height = 800,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                window.Show();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error opening print preview: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // KEY HANDLER - Handle Enter, Tab, and Arrow keys
        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not DataGrid dataGrid) return;

            try
            {
                var cell = dataGrid.CurrentCell;
                if (!cell.IsValid || cell.Column == null) return;

                // ENTER KEY - Move to next row or create new row if on last row
                if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                    HandleEnterKey(dataGrid);
                    return;
                }

                // TAB KEY - Move to next row when at last columns
                if (e.Key == Key.Tab && Keyboard.Modifiers != ModifierKeys.Shift)
                {
                    int currentColumnIndex = cell.Column.DisplayIndex;
                    int totalColumns = dataGrid.Columns.Count;

                    if (currentColumnIndex >= totalColumns - 3)
                    {
                        e.Handled = true;
                        HandleTabKey(dataGrid);
                        return;
                    }
                }
            }
            catch { }
        }

        private void HandleEnterKey(DataGrid dataGrid)
        {
            try
            {
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
                    {
                        dataGrid.CurrentCell = new DataGridCellInfo(nextItem, dataGrid.Columns[currentColIndex]);
                    }
                    dataGrid.BeginEdit();
                }
                else
                {
                    _viewModel.AddItemWithPrice(spec);

                    System.Threading.Tasks.Task.Delay(100).ContinueWith(_ =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            int newIndex = spec.Items.Count - 1;
                            if (newIndex >= 0)
                            {
                                var newItem = spec.Items[newIndex];
                                dataGrid.SelectedItem = newItem;
                                dataGrid.ScrollIntoView(newItem);

                                if (dataGrid.Columns.Count > 1)
                                {
                                    dataGrid.CurrentCell = new DataGridCellInfo(newItem, dataGrid.Columns[1]);
                                }
                                dataGrid.BeginEdit();
                            }
                        });
                    });
                }
            }
            catch { }
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
                    {
                        dataGrid.CurrentCell = new DataGridCellInfo(nextItem, dataGrid.Columns[1]);
                    }
                    dataGrid.BeginEdit();
                }
                else
                {
                    _viewModel.AddItemWithPrice(spec);

                    System.Threading.Tasks.Task.Delay(100).ContinueWith(_ =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            int newIndex = spec.Items.Count - 1;
                            if (newIndex >= 0)
                            {
                                var newItem = spec.Items[newIndex];
                                dataGrid.SelectedItem = newItem;
                                dataGrid.ScrollIntoView(newItem);

                                if (dataGrid.Columns.Count > 1)
                                {
                                    dataGrid.CurrentCell = new DataGridCellInfo(newItem, dataGrid.Columns[1]);
                                }
                                dataGrid.BeginEdit();
                            }
                        });
                    });
                }
            }
            catch { }
        }

        private SpecificationModel FindSpecification(InvoiceItemModel item)
        {
            if (_viewModel?.Invoice == null) return null;

            foreach (var spec in _viewModel.Invoice.Specifications)
            {
                if (spec?.Items?.Contains(item) == true)
                {
                    return spec;
                }
            }
            return null;
        }
    }
}