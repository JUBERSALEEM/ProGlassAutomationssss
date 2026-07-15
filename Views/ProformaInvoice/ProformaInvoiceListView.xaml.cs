using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProGlassAutomation.ViewModels;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Views.ProformaInvoice
{
    public partial class ProformaInvoiceListView : UserControl
    {
        private ProformaInvoiceListViewModel? _viewModel;
        private bool _isInitialized = false;

        public ProformaInvoiceListView()
        {
            InitializeComponent();

            _viewModel = DataContext as ProformaInvoiceListViewModel;
            Loaded += ProformaInvoiceListView_Loaded;
            DataContextChanged += ProformaInvoiceListView_DataContextChanged;
        }

        private void ProformaInvoiceListView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Unsubscribe from old VM
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            // Subscribe to new VM
            _viewModel = e.NewValue as ProformaInvoiceListViewModel;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ProformaInvoiceListView_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitialized = true;

            // ✅ FIX: Call LoadData() on first load (if data not yet loaded)
            if (_viewModel != null && _viewModel.AllInvoices.Count == 0)
            {
                _viewModel.LoadData();
            }
        }

        // Debug logging (DEBUG only, minimal)
        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
#if DEBUG
            if (e.PropertyName == "HasDateRangeError" && _viewModel?.HasDateRangeError == true)
            {
                System.Diagnostics.Debug.WriteLine($"[PI] Date range error detected");
            }
#endif
        }

        // ==================== SEARCH TEXTBOX ====================
        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb) tb.SelectAll();
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
            if (sender is not DataGrid) return;
            if (_viewModel == null) return;

            if (e.Key == Key.Enter && _viewModel.SelectedInvoice != null)
            {
                e.Handled = true;
                _viewModel.EditInvoiceCommand.Execute(_viewModel.SelectedInvoice);
            }
            else if (e.Key == Key.Delete && _viewModel.SelectedInvoice != null)
            {
                e.Handled = true;
                _viewModel.DeleteInvoiceCommand.Execute(_viewModel.SelectedInvoice);
            }
            else if (e.Key == Key.J && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (_viewModel.SelectedInvoice != null && !_viewModel.SelectedInvoice.IsConvertedToJobOrder)
                {
                    e.Handled = true;
                    _viewModel.CreateJobOrderCommand.Execute(_viewModel.SelectedInvoice);
                }
            }
            else if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                _viewModel.NewInvoiceCommand?.Execute(null);
            }
            else if (e.Key == Key.F5)
            {
                e.Handled = true;
                _viewModel.RefreshCommand?.Execute(null);
            }
            else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                _viewModel.OpenFolderCommand?.Execute(null);
            }
            else if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                _viewModel.ExportAllCommand?.Execute(null);
            }
            else if (e.Key == Key.Escape)
            {
                _viewModel.SelectedInvoice = null;
            }
        }
    }
}