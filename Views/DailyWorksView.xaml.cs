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
                foreach (var item in MainDataGrid.SelectedItems)
                {
                    if (item is DataRowView rowView)
                    {
                        selectedIds.Add(Convert.ToInt32(rowView["Id"]));
                    }
                }
                vm.UpdateSelectedIds(selectedIds);
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
    }
}