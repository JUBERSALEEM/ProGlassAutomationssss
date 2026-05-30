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

            // NOTE: DataContext is set by MainViewModel.CreateDailyWorksView()
            // We only set up event handlers here

            // Connect navigation event
            if (DataContext is DailyWorksViewModel vm)
            {
                vm.RequestNavigateToInvoice += OnNavigateToInvoice;
                System.Diagnostics.Debug.WriteLine("[DailyWorksView] Navigation event connected");
            }

            System.Diagnostics.Debug.WriteLine("[DailyWorksView] Initialized");
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

                if (firstSelected != null)
                {
                    vm.SelectedDataRowView = firstSelected;
                }
            }
        }

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
                DataRowView? selectedRow = null;
                if (MainDataGrid?.SelectedItem is DataRowView drv)
                {
                    selectedRow = drv;
                    vm.SelectedDataRowView = drv;
                }

                vm.LoadToInvoiceCommand.Execute(MainDataGrid);

                System.Diagnostics.Debug.WriteLine($"[DailyWork] LoadToInvoice clicked. SelectedRow={(selectedRow != null)}");
            }
        }
    }
}