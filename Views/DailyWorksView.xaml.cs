using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
                MainDataGrid.Dispatcher.BeginInvoke(() =>
                {
                    if (checkBox.IsChecked == true)
                        MainDataGrid.SelectAll();
                    else
                        MainDataGrid.UnselectAll();
                    MainDataGrid.UpdateLayout();
                    _suppressSelectionChanged = false;
                });
                UpdateSelectedIds();
            }
        }

        private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelectionChanged) return;
            UpdateSelectedIds();
        }

        // PATCH 137: Copy cell value on right-click
        private void MainDataGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (MainDataGrid?.CurrentCell.Column is DataGridColumn column &&
                    MainDataGrid.SelectedItem is DailyWorkModel work)
                {
                    var propertyName = column.SortMemberPath;
                    if (!string.IsNullOrEmpty(propertyName))
                    {
                        var prop = typeof(DailyWorkModel).GetProperty(propertyName);
                        if (prop != null)
                        {
                            var value = prop.GetValue(work)?.ToString();
                            if (!string.IsNullOrEmpty(value))
                            {
                                Clipboard.SetText(value);
                                if (DataContext is DailyWorksViewModel vm)
                                {
                                    vm.StatusMessage = $"Copied: {value}";
                                }
                            }
                        }
                    }
                }
            }
            catch { /* Ignore copy errors */ }
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

                if (firstSelected != null && vm.SelectedItem?.Id != firstSelected.Id)
                    vm.SelectedItem = firstSelected;
            }
        }

        // PATCH 108: Keyboard shortcuts
        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is not DailyWorksViewModel vm) return;

            if (e.Key == Key.Escape && vm.IsEditing)
            {
                vm.CancelCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter && vm.IsEditing)
            {
                vm.SaveCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SearchTextBox?.Focus();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control && vm.IsEditing)
            {
                vm.SaveCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Delete && !vm.IsEditing && vm.SelectedCount > 0)
            {
                vm.DeleteSelectedCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
            {
                MainDataGrid?.SelectAll();
                e.Handled = true;
                return;
            }

            // PATCH 130: Arrow key navigation
            if (e.Key == Key.Down && MainDataGrid != null)
            {
                var currentIndex = MainDataGrid.SelectedIndex;
                if (currentIndex < MainDataGrid.Items.Count - 1)
                {
                    MainDataGrid.SelectedIndex = currentIndex + 1;
                    MainDataGrid.ScrollIntoView(MainDataGrid.SelectedItem);
                }
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Up && MainDataGrid != null)
            {
                var currentIndex = MainDataGrid.SelectedIndex;
                if (currentIndex > 0)
                {
                    MainDataGrid.SelectedIndex = currentIndex - 1;
                    MainDataGrid.ScrollIntoView(MainDataGrid.SelectedItem);
                }
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter && !vm.IsEditing && vm.SelectedItem != null)
            {
                vm.EditCommand.Execute(null);
                e.Handled = true;
                return;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SearchTextBox?.Focus();
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

        // PATCH 129: Search box key handler
        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is DailyWorksViewModel vm)
            {
                vm.RefreshCommand.Execute(null);
                e.Handled = true;
            }
        }

        // PATCH 125, 126: Toggle dark mode click
        private void ToggleDarkMode_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DailyWorksViewModel vm)
            {
                vm.ToggleDarkModeCommand.Execute(null);
                ApplyDarkModeColors(vm.IsDarkMode);
            }
        }

        // PATCH 126: Apply dark mode colors
        private void ApplyDarkModeColors(bool isDarkMode)
        {
            try
            {
                var bg = isDarkMode ? "#1F2937" : "#F1F5F9";
                var cardBg = isDarkMode ? "#374151" : "#FFFFFF";
                var headerBg = isDarkMode ? "#0F172A" : "#1E40AF";

                this.Background = new BrushConverter().ConvertFromString(bg) as Brush;

                var grid = this.Content as Grid;
                if (grid?.Children[0] is Border headerBorder)
                {
                    headerBorder.Background = new BrushConverter().ConvertFromString(headerBg) as Brush;
                }

                if (grid?.Children[1] is Border filterBorder)
                {
                    filterBorder.Background = new BrushConverter().ConvertFromString(isDarkMode ? "#1F2937" : "#DBEAFE") as Brush;
                }

                if (grid?.Children[2] is Border dataBorder)
                {
                    dataBorder.Background = new BrushConverter().ConvertFromString(cardBg) as Brush;
                }

                if (grid?.Children[3] is Border statsBorder)
                {
                    statsBorder.Background = new BrushConverter().ConvertFromString(cardBg) as Brush;
                }
            }
            catch { /* Ignore errors */ }
        }

        // PATCH 135: Toggle column visibility
        private void ToggleColumn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && DataContext is DailyWorksViewModel vm)
            {
                var header = menuItem.Header?.ToString();
                if (string.IsNullOrEmpty(header) || MainDataGrid == null) return;

                var col = MainDataGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == header);
                if (col != null)
                {
                    col.Visibility = menuItem.IsChecked ? Visibility.Visible : Visibility.Collapsed;
                }
            }
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

        // PATCH 145: Print preview - FIXED
        private void PrintPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            var previewWindow = new Window
            {
                Title = "Print Preview - Daily Works",
                Width = 900,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = Brushes.White
            };

            var stack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock
            {
                Text = "DAILY WORKS REPORT",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            if (MainDataGrid?.ItemsSource != null)
            {
                foreach (DailyWorkModel w in MainDataGrid.ItemsSource)
                {
                    var row = new StackPanel { Orientation = Orientation.Horizontal };
                    var dateStr = w.Date == default ? "" : w.Date.ToString("dd-MM-yyyy");
                    row.Children.Add(new TextBlock { Text = dateStr, Width = 80 });
                    row.Children.Add(new TextBlock { Text = w.Company ?? "", Width = 100 });
                    row.Children.Add(new TextBlock { Text = w.PiNumber ?? "", Width = 80 });
                    row.Children.Add(new TextBlock { Text = w.Qty.ToString(), Width = 50 });
                    row.Children.Add(new TextBlock { Text = w.Sqm.ToString("N2"), Width = 60 });
                    stack.Children.Add(row);
                }
            }

            scrollViewer.Content = stack;

            var toolBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10) };

            var printBtn = new Button { Content = "🖨️ Print", Margin = new Thickness(0, 0, 10, 0) };
            printBtn.Click += (s, args) => { previewWindow.Close(); DoPrint(); };
            toolBar.Children.Add(printBtn);

            var closeBtn = new Button { Content = "❌ Close" };
            closeBtn.Click += (s, args) => previewWindow.Close();
            toolBar.Children.Add(closeBtn);

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.Children.Add(toolBar);
            mainGrid.Children.Add(scrollViewer);
            Grid.SetRow(toolBar, 0);
            Grid.SetRow(scrollViewer, 1);
            previewWindow.Content = mainGrid;
            previewWindow.Show();
        }

        private void DoPrint()
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
            catch (Exception ex)
            {
                MessageBox.Show("Print failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // PATCH 150: Quick actions
        private void ScrollToTop_Click(object sender, RoutedEventArgs e)
        {
            if (MainDataGrid?.Items.Count > 0)
            {
                MainDataGrid.ScrollIntoView(MainDataGrid.Items[0]);
                MainDataGrid.SelectedIndex = 0;
            }
        }

        private void ScrollToBottom_Click(object sender, RoutedEventArgs e)
        {
            if (MainDataGrid?.Items.Count > 0)
            {
                var last = MainDataGrid.Items.Count - 1;
                MainDataGrid.ScrollIntoView(MainDataGrid.Items[last]);
                MainDataGrid.SelectedIndex = last;
            }
        }

        private void ShowStats_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DailyWorksViewModel vm)
            {
                MessageBox.Show(
                    $"Records: {vm.FilteredRecords}\n" +
                    $"QTY: {vm.FilteredQty}\n" +
                    $"SQM: {vm.FilteredSQM:N2}\n" +
                    $"Done: {vm.CompletedCount}\n" +
                    $"Pending: {vm.PendingCount}",
                    "Statistics",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void ShowHistory_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DailyWorksViewModel vm)
            {
                var logs = vm.ActivityLogs;
                if (logs == null || logs.Count == 0)
                {
                    MessageBox.Show("No activity history yet.", "History", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var history = string.Join("\n", logs.Take(10).Select(l => $"{l.Timestamp:HH:mm:ss} - {l.Action}: {l.Details}"));
                MessageBox.Show(history, "Activity History", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // PATCH 157: Record detail popup
        private void ShowDetailPopup(DailyWorkModel work)
        {
            var popup = new Window
            {
                Title = $"Details - {work.PiNumber}",
                Width = 500,
                Height = 450,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = Brushes.White
            };

            var stack = new StackPanel { Margin = new Thickness(20) };

            stack.Children.Add(new TextBlock
            {
                Text = work.PiNumber,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 64, 175)),
                Margin = new Thickness(0, 0, 0, 15)
            });

            stack.Children.Add(new TextBlock { Text = $"Company: {work.Company}", Margin = new Thickness(0, 0, 0, 5) });
            stack.Children.Add(new TextBlock { Text = $"Date: {work.Date:dd-MM-yyyy}", Margin = new Thickness(0, 0, 0, 5) });
            stack.Children.Add(new TextBlock { Text = $"Qty: {work.Qty} | SQM: {work.Sqm:N2}", Margin = new Thickness(0, 0, 0, 5) });
            stack.Children.Add(new TextBlock { Text = $"Production: {work.ProductionStatus}", Margin = new Thickness(0, 0, 0, 5) });
            stack.Children.Add(new TextBlock { Text = $"Status: {work.Status}", Margin = new Thickness(0, 0, 0, 5) });
            stack.Children.Add(new TextBlock { Text = $"Notes: {work.Notes}", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) });

            var closeBtn = new Button { Content = "Close", HorizontalAlignment = HorizontalAlignment.Right };
            closeBtn.Click += (s, e) => popup.Close();
            stack.Children.Add(closeBtn);

            popup.Content = stack;
            popup.ShowDialog();
        }

        // PATCH 158: Quick search dropdown
        // Note: Requires XAML changes - add IsEditable="True" to SearchTextBox
        // <ComboBox IsEditable="True" ItemsSource="{Binding RecentSearches}" Text="{Binding SearchText}"/>

        // REMOVED: PATCH 94 - Duplicate of PATCH 108 UserControl_KeyDown
        // This override conflicts with the existing KeyDown handler
    }
}