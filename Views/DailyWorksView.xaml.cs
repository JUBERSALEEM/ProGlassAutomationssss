using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data;
using ProGlassAutomation.ViewModels;

namespace ProGlassAutomation.Views
{
    public partial class DailyWorksView : UserControl
    {
        private bool _isSelectingAll = false;

        public DailyWorksView()
        {
            InitializeComponent();

            // Use shared ProformaInvoiceViewModel
            var dailyWorksVM = new DailyWorksViewModel();
            dailyWorksVM.ProformaInvoiceVM = SharedViewModels.ProformaInvoiceVM;

            // Subscribe to save event for auto-update
            SharedViewModels.ProformaInvoiceVM.InvoiceSaved += (invoice) => dailyWorksVM.OnProformaInvoiceSaved(invoice);

            // Connect navigation event
            dailyWorksVM.RequestNavigateToInvoice += OnNavigateToInvoice;

            // Set DataContext
            DataContext = dailyWorksVM;

            System.Diagnostics.Debug.WriteLine("[DailyWorksView] ViewModels initialized and connected");
        }

        // Event to notify MainWindow to navigate
        public event Action? NavigateToInvoice;

        private void OnNavigateToInvoice()
        {
            System.Diagnostics.Debug.WriteLine("[DailyWorksView] Navigate to Invoice requested");
            NavigateToInvoice?.Invoke();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DailyWorksViewModel vm && vm.SelectedDataRowView != null)
            {
                vm.EditCommand.Execute(vm.SelectedDataRowView);
            }
        }

        private void HeaderSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && MainDataGrid != null)
            {
                _isSelectingAll = true;

                if (checkBox.IsChecked == true)
                {
                    MainDataGrid.SelectAll();
                }
                else
                {
                    MainDataGrid.UnselectAll();
                }

                _isSelectingAll = false;
                UpdateSelectedIds();
            }
        }

        private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isSelectingAll)
            {
                UpdateSelectedIds();
            }
        }

        private void UpdateSelectedIds()
        {
            if (DataContext is DailyWorksViewModel vm && MainDataGrid != null)
            {
                var selectedIds = new List<int>();
                DataRowView? firstSelected = null;

                foreach (var item in MainDataGrid.SelectedItems)
                {
                    if (item is DataRowView rowView)
                    {
                        selectedIds.Add(Convert.ToInt32(rowView["Id"]));
                        if (firstSelected == null)
                            firstSelected = rowView;
                    }
                }

                vm.UpdateSelectedIds(selectedIds);

                // Set SelectedDataRowView for command CanExecute checks
                if (firstSelected != null)
                {
                    vm.SelectedDataRowView = firstSelected;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // SELECT ALL ON CLICK - TextBox
        // ═══════════════════════════════════════════════════════════

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        private void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox textBox && !textBox.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                textBox.Focus();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // SELECT ALL ON CLICK - ComboBox (Editable)
        // ═══════════════════════════════════════════════════════════

        private void ComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.IsEditable)
            {
                comboBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var textBox = comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;
                    textBox?.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        private void ComboBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ComboBox comboBox && !comboBox.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                comboBox.Focus();
            }
        }

        private void LoadToInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DailyWorksViewModel vm)
            {
                // Get selected row from DataGrid
                DataRowView? selectedRow = null;
                if (MainDataGrid?.SelectedItem is DataRowView drv)
                {
                    selectedRow = drv;
                    vm.SelectedDataRowView = drv;
                }

                // Execute the command
                vm.LoadToInvoiceCommand.Execute(MainDataGrid);

                System.Diagnostics.Debug.WriteLine($"[DailyWork] LoadToInvoice clicked. SelectedRow={(selectedRow != null)}");
            }
        }
    }
}