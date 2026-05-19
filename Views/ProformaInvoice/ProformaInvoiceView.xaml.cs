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

        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SpecificationModel spec)
            {
                // Use AddItemWithPrice to copy base price from first row
                _viewModel.AddItemWithPrice(spec);
            }
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is InvoiceItemModel item)
            {
                _viewModel.RemoveItem(item);
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

        // ENTER KEY - Move to next row, or create new row with price copied from first row
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

                // If not on last row, move to next row
                if (currentIndex < totalRows - 1)
                {
                    var nextItem = spec.Items[currentIndex + 1];
                    dataGrid.SelectedItem = nextItem;
                    dataGrid.ScrollIntoView(nextItem);

                    // Stay in same column
                    int currentColIndex = dataGrid.CurrentCell.Column.DisplayIndex;
                    if (currentColIndex < dataGrid.Columns.Count)
                    {
                        dataGrid.CurrentCell = new DataGridCellInfo(nextItem, dataGrid.Columns[currentColIndex]);
                    }
                    dataGrid.BeginEdit();
                }
                else
                {
                    // On last row - create new row with price from first row
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

                                // Move to GlassRef column (column index 1)
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

        // TAB KEY - Move to next row or create new row
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
                    // Move to next row
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
                    // Create new row with price from first row
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
    }
}