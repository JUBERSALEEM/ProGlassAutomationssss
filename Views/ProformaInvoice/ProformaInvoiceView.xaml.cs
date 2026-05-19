using System;
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
                _viewModel.AddItem(spec);
            }
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is InvoiceItemModel item)
            {
                _viewModel.RemoveItem(item);
            }
        }

        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Tab) return;
            if (Keyboard.Modifiers == ModifierKeys.Shift) return;
            if (sender is not DataGrid dataGrid) return;

            try
            {
                var cell = dataGrid.CurrentCell;
                if (!cell.IsValid || cell.Column == null) return;

                int currentColumnIndex = cell.Column.DisplayIndex;
                int totalColumns = dataGrid.Columns.Count;

                // Check if on Price column (index 11) or after - create new row
                // This intercepts Tab BEFORE going to TotalAED
                if (currentColumnIndex >= totalColumns - 3)
                {
                    e.Handled = true;

                    var currentItem = cell.Item as InvoiceItemModel;
                    if (currentItem == null) return;

                    var spec = FindSpecification(currentItem);
                    if (spec == null) return;

                    int itemIndex = spec.Items.IndexOf(currentItem);

                    // If not last row, move to next row
                    if (itemIndex < spec.Items.Count - 1)
                    {
                        System.Threading.Tasks.Task.Delay(50).ContinueWith(_ =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                var nextItem = spec.Items[itemIndex + 1];
                                dataGrid.SelectedItem = nextItem;
                                dataGrid.ScrollIntoView(nextItem);
                                if (dataGrid.Columns.Count > 1)
                                {
                                    dataGrid.CurrentCell = new DataGridCellInfo(nextItem, dataGrid.Columns[1]);
                                }
                                dataGrid.BeginEdit();
                            });
                        });
                    }
                    else
                    {
                        // Create new row
                        _viewModel.AddItem(spec);

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