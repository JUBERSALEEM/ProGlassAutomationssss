using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProGlassAutomation.ViewModels;

namespace ProGlassAutomation.Views
{
    public partial class DailyWorksView : UserControl
    {
        private bool _isSelectingAll = false;

        public DailyWorksView()
        {
            InitializeComponent();
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
                UpdateSelectedCount();
            }
        }

        private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isSelectingAll)
            {
                UpdateSelectedCount();
            }
        }

        private void UpdateSelectedCount()
        {
            if (DataContext is DailyWorksViewModel vm)
            {
                vm.SetSelectedCount(MainDataGrid?.SelectedItems?.Count ?? 0);
            }
        }
    }
}