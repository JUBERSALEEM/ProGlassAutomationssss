using ProGlassAutomation.Data.Database;
using ProGlassAutomation.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
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

        // ✅ NEW: Toggle selection on direct click
        private void DataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Find the row that was clicked
            var row = GetDataGridRowFromEvent(e);
            if (row != null && row.DataContext != null)
            {
                // Toggle selection
                if (row.IsSelected)
                {
                    MainDataGrid.SelectedItems.Remove(row.DataContext);
                }
                else
                {
                    if (!MainDataGrid.SelectedItems.Contains(row.DataContext))
                    {
                        MainDataGrid.SelectedItems.Add(row.DataContext);
                    }
                }
            }
        }

        private DataGridRow GetDataGridRowFromEvent(MouseButtonEventArgs e)
        {
            var dep = e.OriginalSource as DependencyObject;
            while (dep != null && !(dep is DataGridRow))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }
            return dep as DataGridRow;
        }

        private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is DeliveryViewModel vm && MainDataGrid != null)
            {
                int selectedCount = MainDataGrid.SelectedItems?.Count ?? 0;
                vm.SetSelectedCount(selectedCount);

                var selectedIds = new List<int>();
                foreach (var item in MainDataGrid.SelectedItems)
                {
                    if (item is System.Data.DataRowView rowView)
                    {
                        selectedIds.Add(Convert.ToInt32(rowView["Id"]));
                    }
                }
                vm.UpdateSelectedIds(selectedIds);

                if (MainDataGrid.SelectedItem is System.Data.DataRowView selectedRow)
                {
                    int id = Convert.ToInt32(selectedRow["Id"]);
                    var order = vm.DeliveryOrders?.FirstOrDefault(w => w.Id == id);
                    if (order != null)
                    {
                        if (order.DeliveryItems == null || order.DeliveryItems.Count == 0)
                        {
                            var items = DbHelper.GetDeliveryItems(order.Id);
                            order.DeliveryItems = new System.Collections.ObjectModel.ObservableCollection<Models.DeliveryItem>(items);
                        }
                        vm.SelectedOrder = order;
                    }
                }
                else if (selectedCount == 0)
                {
                    vm.SelectedOrder = null;
                }
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

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        private void CheckBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}