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
        private bool _isUpdatingStatus = false;
        private bool _isInitialized = false;

        public ProformaInvoiceListView()
        {
            InitializeComponent();
            _viewModel = DataContext as ProformaInvoiceListViewModel;

            if (_viewModel != null)
            {
                // Subscribe to debug all property changes
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;

                System.Diagnostics.Debug.WriteLine($"[CTOR] DataContext set. DraftCount={_viewModel.DraftCount}");
            }

            Loaded += (s, e) =>
            {
                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine($"[LOADED] _isInitialized=true");
            };
        }

        // 🔴 DEBUG ALL PROPERTY CHANGES
        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_viewModel == null) return;

            var propName = e.PropertyName;
            var value = _viewModel.GetType().GetProperty(propName)?.GetValue(_viewModel);
            System.Diagnostics.Debug.WriteLine($"[PROPCHANGED] {propName} = {value}");
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

        // ==================== STATUS COMBOBOX CHANGED ====================
        private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingStatus) return;
            if (!_isInitialized) return;
            if (e.AddedItems.Count == 0) return;

            if (sender is ComboBox cb)
            {
                string? newStatus = cb.SelectedItem as string;
                var invoice = cb.DataContext as ProformaInvoiceModel;

                if (newStatus != null && invoice != null && _viewModel != null && invoice.Status != newStatus)
                {
                    _isUpdatingStatus = true;

                    // Update status
                    _viewModel.UpdateStatus(invoice, newStatus);

                    // 🔴 FORCE STATS REFRESH MANUALLY
                    RefreshStatsManual();

                    _isUpdatingStatus = false;
                }
            }
        }

        // 🔴 HELPER METHOD - Force stats refresh
        private void RefreshStatsManual()
        {
            if (_viewModel == null) return;

            // Recalculate all stats manually
            _viewModel.DraftCount = _viewModel.FilteredInvoices.Count(i => i.Status == "Draft");
            _viewModel.SentCount = _viewModel.FilteredInvoices.Count(i => i.Status == "Sent");
            _viewModel.ConfirmedCount = _viewModel.FilteredInvoices.Count(i => i.Status == "Confirmed");
            _viewModel.HoldCount = _viewModel.FilteredInvoices.Count(i => i.Status == "Hold");
            _viewModel.CompletedCount = _viewModel.FilteredInvoices.Count(i => i.Status == "Completed");
            _viewModel.TotalInvoiceCount = _viewModel.AllInvoices.Count;

            // Force notification
            _viewModel.OnPropertyChanged(nameof(_viewModel.DraftCount));
            _viewModel.OnPropertyChanged(nameof(_viewModel.SentCount));
            _viewModel.OnPropertyChanged(nameof(_viewModel.ConfirmedCount));
            _viewModel.OnPropertyChanged(nameof(_viewModel.HoldCount));
            _viewModel.OnPropertyChanged(nameof(_viewModel.CompletedCount));
            _viewModel.OnPropertyChanged(nameof(_viewModel.TotalInvoiceCount));
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
                }
                else if (e.Key == Key.Delete && _viewModel?.SelectedInvoice != null)
                {
                    _viewModel.DeleteInvoiceCommand.Execute(_viewModel.SelectedInvoice);
                }
                else if (e.Key == Key.J && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (_viewModel?.SelectedInvoice != null && !_viewModel.SelectedInvoice.IsConvertedToJobOrder)
                    {
                        e.Handled = true;
                        _viewModel.CreateJobOrderCommand.Execute(_viewModel.SelectedInvoice);
                    }
                }
                else if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    e.Handled = true;
                    _viewModel?.NewInvoiceCommand.Execute(null);
                }
                else if (e.Key == Key.F5)
                {
                    e.Handled = true;
                    _viewModel?.RefreshCommand.Execute(null);
                }
                else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    e.Handled = true;
                    _viewModel?.OpenFolderCommand.Execute(null);
                }
                else if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    e.Handled = true;
                    _viewModel?.ExportAllCommand.Execute(null);
                }
                else if (e.Key == Key.Escape)
                {
                    if (_viewModel != null) _viewModel.SelectedInvoice = null;
                }
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    }
}