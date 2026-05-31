using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProGlassAutomation.ViewModels;

namespace ProGlassAutomation.Views.ProformaInvoice
{
    public partial class ProformaInvoiceListView : UserControl
    {
        private ProformaInvoiceListViewModel _viewModel;

        public ProformaInvoiceListView()
        {
            InitializeComponent();

            // Get ViewModel from SharedViewModels
            _viewModel = SharedViewModels.ProformaInvoiceListVM;
            DataContext = _viewModel;

            System.Diagnostics.Debug.WriteLine("[ProformaInvoiceListView] Initialized");
        }

        // ==================== SEARCH TEXTBOX ====================

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.SelectAll();
            }
        }

        // ==================== DATAGRID DOUBLE CLICK ====================

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel?.SelectedInvoice != null)
            {
                _viewModel.EditInvoiceCommand.Execute(_viewModel.SelectedInvoice);
            }
        }

        // ==================== KEYBOARD NAVIGATION ====================

        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is DataGrid)
            {
                if (e.Key == Key.Enter && _viewModel?.SelectedInvoice != null)
                {
                    e.Handled = true;
                    _viewModel.EditInvoiceCommand.Execute(_viewModel.SelectedInvoice);
                    return;
                }

                if (e.Key == Key.Delete && _viewModel?.SelectedInvoice != null)
                {
                    e.Handled = true;
                    var result = MessageBox.Show(
                        $"Are you sure you want to delete invoice {_viewModel.SelectedInvoice.InvoiceNo}?",
                        "Confirm Delete",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        _viewModel.DeleteInvoiceCommand.Execute(_viewModel.SelectedInvoice);
                    }
                    return;
                }

                if (e.Key == Key.J && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    // Ctrl+J - Create Job Order
                    if (_viewModel?.SelectedInvoice != null && !_viewModel.SelectedInvoice.IsConvertedToJobOrder)
                    {
                        e.Handled = true;
                        _viewModel.CreateJobOrderCommand.Execute(_viewModel.SelectedInvoice);
                    }
                }

                if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    // Ctrl+N - New Invoice
                    e.Handled = true;
                    _viewModel.NewInvoiceCommand.Execute(null);
                }

                if (e.Key == Key.F5)
                {
                    // F5 - Refresh
                    e.Handled = true;
                    _viewModel.RefreshCommand.Execute(null);
                }
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}