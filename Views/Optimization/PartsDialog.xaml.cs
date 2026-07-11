using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Views.Optimization
{
    public partial class PartsDialog : Window
    {
        private readonly OptimizationViewModel _vm;

        public PartsDialog(OptimizationViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = _vm;
            icParts.ItemsSource = _vm.DemandParts;
            UpdateStats();
        }

        #region === EXCEL-STYLE CELL NAVIGATION ===

        // Tab = Select all text (works reliably)
        private void Cell_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.Dispatcher.BeginInvoke(new Action(() => tb.SelectAll()),
                    System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        // Arrow keys: Left/Right move between columns, Up/Down move between rows
        // Enter = move DOWN to same column in next row
        // Tab = select all (handled by GotFocus)
        private void Cell_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb) return;

            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                MoveFocusVertical(tb, forward: true);
            }
            else if (e.Key == Key.Down)
            {
                e.Handled = true;
                MoveFocusVertical(tb, forward: true);
            }
            else if (e.Key == Key.Up)
            {
                e.Handled = true;
                MoveFocusVertical(tb, forward: false);
            }
            else if (e.Key == Key.Right)
            {
                // If cursor is at end, move to next cell
                if (tb.SelectionStart == tb.Text.Length && tb.SelectionLength == 0)
                {
                    e.Handled = true;
                    MoveFocusHorizontal(tb, forward: true);
                }
            }
            else if (e.Key == Key.Left)
            {
                // If cursor is at start, move to previous cell
                if (tb.SelectionStart == 0 && tb.SelectionLength == 0)
                {
                    e.Handled = true;
                    MoveFocusHorizontal(tb, forward: false);
                }
            }
        }

        private void MoveFocusVertical(TextBox current, bool forward)
        {
            var itemsControl = FindVisualParent<ItemsControl>(current);
            if (itemsControl == null) return;

            var row = FindVisualParent<ContentPresenter>(current);
            if (row == null) return;

            int currentIndex = itemsControl.ItemContainerGenerator.IndexFromContainer(row);
            int nextIndex = forward ? currentIndex + 1 : currentIndex - 1;

            if (nextIndex >= itemsControl.Items.Count)
            {
                // Auto-add empty row when going down past last row
                AddEmptyRow();
                nextIndex = itemsControl.Items.Count - 1;
            }

            if (nextIndex < 0) return;

            var nextRow = itemsControl.ItemContainerGenerator.ContainerFromIndex(nextIndex) as FrameworkElement;
            if (nextRow == null) return;

            var nextTb = FindVisualChildByName<TextBox>(nextRow, current.Name);
            if (nextTb != null)
            {
                nextTb.Focus();
                nextTb.SelectAll();
            }
        }

        private void MoveFocusHorizontal(TextBox current, bool forward)
        {
            // Column order: tbLabel, tbL, tbW, tbQty
            var columnOrder = new[] { "tbLabel", "tbL", "tbW", "tbQty" };
            int currentCol = Array.IndexOf(columnOrder, current.Name);
            if (currentCol < 0) return;

            int nextCol = forward ? currentCol + 1 : currentCol - 1;
            if (nextCol < 0 || nextCol >= columnOrder.Length) return;

            var row = FindVisualParent<ContentPresenter>(current);
            if (row == null) return;

            var nextTb = FindVisualChildByName<TextBox>(row, columnOrder[nextCol]);
            if (nextTb != null)
            {
                nextTb.Focus();
                nextTb.SelectAll();
            }
        }

        #endregion

        #region === BUTTON HANDLERS ===

        private void AddPart_Click(object sender, RoutedEventArgs e)
        {
            AddEmptyRow();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DemandPart part)
            {
                _vm.DemandParts.Remove(part);
                UpdateStats();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        #endregion

        #region === DOWNLOAD TEMPLATE (EXPORTS CURRENT DATA AS CSV/EXCEL) ===

        private void DownloadTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    FileName = $"PartsOfGlass_Template_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                    Title = "Save Parts Template / Export"
                };

                if (dlg.ShowDialog() != true) return;

                var sb = new StringBuilder();
                // Header row
                sb.AppendLine("ID,Label,Width(mm),Height(mm),Qty");

                if (_vm.DemandParts != null && _vm.DemandParts.Count > 0)
                {
                    // Export CURRENT entries from the table
                    foreach (var p in _vm.DemandParts)
                    {
                        sb.AppendLine($"{p.Id},\"{EscapeCsv(p.Label)}\",{p.L.ToString(CultureInfo.InvariantCulture)},{p.W.ToString(CultureInfo.InvariantCulture)},{p.Qty}");
                    }
                }
                else
                {
                    // Export sample template when table is empty
                    sb.AppendLine("1,\"Sample Glass 1\",1000,800,2");
                    sb.AppendLine("2,\"Sample Glass 2\",1250,1650,5");
                    sb.AppendLine("3,\"Sample Glass 3\",750,900,3");
                }

                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);

                MessageBox.Show(
                    $"Template exported successfully!\n\nFile: {dlg.FileName}\nRows: {(_vm.DemandParts?.Count ?? 3)}",
                    "Template Exported", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        #endregion

        #region === IMPORT CSV/EXCEL ===

        private void ImportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    Filter = "CSV/Excel Files (*.csv;*.xlsx;*.xls)|*.csv;*.xlsx;*.xls|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    Title = "Import Parts from CSV/Excel"
                };

                if (dlg.ShowDialog() != true) return;

                var importedCount = 0;
                var skippedCount = 0;

                // For now we support .csv (use StreamReader)
                // For .xlsx you would need a library like ClosedXML, but
                // we read the file as text which works for many Excel saves
                using (var reader = new StreamReader(dlg.FileName, Encoding.UTF8))
                {
                    var isFirstRow = true;
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var cells = ParseCsvLine(line);
                        if (cells.Count < 4) { skippedCount++; continue; }

                        // Skip header row (first row containing "ID" or "Label")
                        if (isFirstRow)
                        {
                            isFirstRow = false;
                            if (cells[0].Trim().Equals("ID", StringComparison.OrdinalIgnoreCase) ||
                                cells[0].Trim().Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                                cells[1].Trim().Equals("Label", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                        }

                        // Try parse ID, Label, Width, Height, Qty
                        if (!double.TryParse(cells[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double w) || w <= 0 ||
                            !double.TryParse(cells[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double h) || h <= 0)
                        {
                            skippedCount++;
                            continue;
                        }

                        int qty = 1;
                        if (cells.Count > 4)
                            int.TryParse(cells[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out qty);
                        if (qty <= 0) qty = 1;

                        string label = cells.Count > 1 ? cells[1].Trim() : "";

                        _vm.DemandParts.Add(new DemandPart
                        {
                            Label = label,
                            L = w,
                            W = h,
                            Qty = qty
                        });
                        importedCount++;
                    }
                }

                UpdateStats();

                MessageBox.Show(
                    $"Import complete!\n\n✓ Imported: {importedCount}\n⊘ Skipped: {skippedCount}",
                    "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else
                {
                    if (c == ',')
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }
                    else if (c == '"' && current.Length == 0)
                    {
                        inQuotes = true;
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
            }
            result.Add(current.ToString());
            return result;
        }

        #endregion

        #region === HELPERS ===

        private void AddEmptyRow()
        {
            _vm.DemandParts.Add(new DemandPart
            {
                Label = "",
                L = 0,
                W = 0,
                Qty = 0
            });
            UpdateStats();
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            var current = child;
            while (current != null)
            {
                if (current is T t) return t;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private static T? FindVisualChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t && (t as FrameworkElement)?.Name == name)
                    return t;
                var found = FindVisualChildByName<T>(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void UpdateStats()
        {
            if (txtPartTypes != null) txtPartTypes.Text = _vm.DemandParts.Count.ToString();
            if (txtTotalPieces != null) txtTotalPieces.Text = _vm.DemandParts.Sum(p => p.Qty).ToString();
            if (txtTotalArea != null)
                txtTotalArea.Text = $"{_vm.DemandParts.Sum(p => p.L * p.W * p.Qty) / 1_000_000.0:N3} m²";
            if (txtUniqueDims != null)
                txtUniqueDims.Text = $"{_vm.DemandParts.Select(p => $"{p.L}x{p.W}").Distinct().Count()} sizes";
            if (txtLargest != null)
            {
                if (_vm.DemandParts.Count > 0)
                {
                    var max = _vm.DemandParts.OrderByDescending(p => p.L * p.W).First();
                    txtLargest.Text = $"{max.Label} ({max.L:N0}×{max.W:N0} mm)";
                }
                else
                {
                    txtLargest.Text = "—";
                }
            }
        }

        #endregion
    }
}