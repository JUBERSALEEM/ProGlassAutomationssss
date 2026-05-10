using ProGlassAutomation.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ProGlassAutomation.Views.Delivery
{
    public partial class DeliveryView : UserControl
    {
        public DeliveryView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DeliveryViewModel vm)
            {
                vm.ViewDetailsCommand.Execute(null);
            }
        }

        private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is DeliveryViewModel vm)
            {
                vm.SetSelectedCount(MainDataGrid?.SelectedItems?.Count ?? 0);
            }
        }

        private void HeaderSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && MainDataGrid != null)
            {
                if (checkBox.IsChecked == true)
                {
                    MainDataGrid.SelectAll();
                }
                else
                {
                    MainDataGrid.UnselectAll();
                }
            }
        }

        // ✅ NEW: Select all text when TextBox gets focus
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                textBox.SelectAll();
            }
        }
    }
}