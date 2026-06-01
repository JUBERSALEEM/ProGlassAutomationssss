using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProGlassAutomation.Models;
using ProGlassAutomation.ViewModels;

namespace ProGlassAutomation.Views
{
    public partial class DailyWorksView : UserControl
    {
        private bool _isBulkSelecting = false;

        public DailyWorksView()
        {
            InitializeComponent();

            if (DataContext is DailyWorksViewModel vm)
            {
                vm.RequestNavigateToInvoice += () => NavigateToInvoice?.Invoke();
            }
        }

        public event Action? NavigateToInvoice;

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DailyWorksViewModel vm && vm.SelectedItem != null)
            {
                vm.EditCommand?.Execute(null);
            }
        }

        private void HeaderSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && MainDataGrid != null)
            {
                _isBulkSelecting = true;
                MainDataGrid.Dispatcher.Invoke(() =>
                {
                    if (checkBox.IsChecked == true)
                        MainDataGrid.SelectAll();
                    else
                        MainDataGrid.UnselectAll();
                    MainDataGrid.UpdateLayout();
                });
                _isBulkSelecting = false;
                UpdateSelectedIds();
            }
        }

        private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isBulkSelecting) return;
            UpdateSelectedIds();
        }

        private void UpdateSelectedIds()
        {
            if (DataContext is DailyWorksViewModel vm && MainDataGrid != null)
            {
                var selectedIds = new List<int>();
                DailyWorkModel? firstSelected = null;

                foreach (var item in MainDataGrid.SelectedItems)
                {
                    if (item is DailyWorkModel work)
                    {
                        selectedIds.Add(work.Id);
                        if (firstSelected == null)
                            firstSelected = work;
                    }
                }

                vm.UpdateSelectedIds(selectedIds);
                if (firstSelected != null)
                    vm.SelectedItem = firstSelected;
            }
        }

        private void LoadToInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DailyWorksViewModel vm)
            {
                if (MainDataGrid?.SelectedItem is DailyWorkModel work)
                    vm.SelectedItem = work;
                vm.EditCommand?.Execute(null);
            }
        }
    }
}