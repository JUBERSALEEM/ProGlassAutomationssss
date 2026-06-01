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
        // PATCH 25: Remove _isBulkSelecting flag - use change suppression scope
        private bool _suppressSelectionChanged = false;

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
                _suppressSelectionChanged = true;
                // PATCH 14: Use BeginInvoke for non-blocking UI update
                MainDataGrid.Dispatcher.BeginInvoke(() =>
                {
                    if (checkBox.IsChecked == true)
                        MainDataGrid.SelectAll();
                    else
                        MainDataGrid.UnselectAll();
                    MainDataGrid.UpdateLayout();
                    _suppressSelectionChanged = false; // PATCH 25: Reset AFTER operation
                });
                UpdateSelectedIds();
            }
        }

        private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelectionChanged) return;
            UpdateSelectedIds();
        }

        // PATCH 27: Prevent re-raising SelectionChanged loops
        private void UpdateSelectedIds()
        {
            if (_suppressSelectionChanged) return;

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

                // PATCH 27: Only update if different to avoid loops
                if (firstSelected != null && vm.SelectedItem?.Id != firstSelected.Id)
                    vm.SelectedItem = firstSelected;
            }
        }

        // PATCH 108: Keyboard shortcuts
        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is not DailyWorksViewModel vm) return;

            // Escape = Close edit popup
            if (e.Key == Key.Escape && vm.IsEditing)
            {
                vm.CancelCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Enter in edit mode = Save
            if (e.Key == Key.Enter && vm.IsEditing)
            {
                vm.SaveCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Ctrl+F = Focus search
            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SearchTextBox?.Focus();
                e.Handled = true;
                return;
            }

            // Ctrl+S = Save (when editing)
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control && vm.IsEditing)
            {
                vm.SaveCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Delete = Delete selected
            if (e.Key == Key.Delete && !vm.IsEditing && vm.SelectedCount > 0)
            {
                vm.DeleteSelectedCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Ctrl+A = Select all
            if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
            {
                MainDataGrid?.SelectAll();
                e.Handled = true;
                return;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Focus search on load
            SearchTextBox?.Focus();

            // PATCH 110: Load saved column widths
            LoadColumnWidths();
        }

        // PATCH 92: Print button handler
        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true && MainDataGrid != null)
                {
                    printDialog.PrintVisual(MainDataGrid, "Daily Works Report");
                    if (DataContext is DailyWorksViewModel vm)
                    {
                        vm.StatusMessage = "Printing complete!";
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Print failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // PATCH 93: Load to Invoice handler
        private void LoadToInvoice_Click(object sender, RoutedEventArgs e)
        {
            // Raise navigation event to navigate to invoice page
            NavigateToInvoice?.Invoke();
        }

        // PATCH 110: Column widths persistence
        private readonly string _columnWidthsKey = "DailyWorksColumnWidths";

        private void MainDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            LoadColumnWidths();
        }

        private void MainDataGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SaveColumnWidths();
        }

        private void SaveColumnWidths()
        {
            try
            {
                if (MainDataGrid == null) return;

                var widths = new Dictionary<string, double>();
                foreach (var col in MainDataGrid.Columns)
                {
                    if (col.Header != null && col.Width.IsAbsolute)
                        widths[col.Header.ToString()!] = col.Width.Value;
                }

                var json = System.Text.Json.JsonSerializer.Serialize(widths);
                System.IO.File.WriteAllText(_columnWidthsKey + ".json", json);
            }
            catch { /* Ignore errors */ }
        }

        private void LoadColumnWidths()
        {
            try
            {
                var path = _columnWidthsKey + ".json";
                if (!System.IO.File.Exists(path)) return;

                var json = System.IO.File.ReadAllText(path);
                var widths = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, double>>(json);

                if (widths == null || MainDataGrid == null) return;

                foreach (var col in MainDataGrid.Columns)
                {
                    if (col.Header != null && widths.TryGetValue(col.Header.ToString()!, out var w))
                        col.Width = new DataGridLength(w);
                }
            }
            catch { /* Ignore errors */ }
        }

        // PATCH 94: Keyboard shortcuts
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (DataContext is not DailyWorksViewModel vm) return;

            switch (e.Key)
            {
                case Key.F5: // Refresh
                    vm.RefreshCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.Insert: // Add new record
                    vm.AddNewCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.Delete: // Delete selected
                    if (vm.SelectedCount > 0)
                        vm.DeleteSelectedCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.Escape: // Cancel/Close popup
                    vm.CancelCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.S when Keyboard.Modifiers == ModifierKeys.Control: // Ctrl+S - Save
                    if (vm.IsEditing)
                        vm.SaveCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.F when Keyboard.Modifiers == ModifierKeys.Control: // Ctrl+F - Focus search
                    // Focus search box - requires x:Name on search TextBox in XAML
                    e.Handled = true;
                    break;
            }
        }
    }
}