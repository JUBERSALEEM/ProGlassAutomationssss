using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

            // Hook into the ItemsControl's loaded event to ensure
            // event handlers are attached AFTER the visual tree is built
            icParts.Loaded += (s, e) => AttachExcelHandlers();

            UpdateStats();
        }

        // Attach GotFocus/KeyDown handlers to ALL existing and future
        // ExcelCell TextBoxes inside the ItemsControl via AddHandler
        private void AttachExcelHandlers()
        {
            // Walk the visual tree to find all TextBoxes named tbLabel, tbL, tbW, tbQty
            AttachHandlersToChildren(icParts);
        }

        private void AttachHandlersToChildren(DependencyObject parent)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is TextBox tb && !string.IsNullOrEmpty(tb.Name))
                {
                    if (tb.Name == "tbLabel" || tb.Name == "tbL" || tb.Name == "tbW" || tb.Name == "tbQty")
                    {
                        // AddHandler ensures the handler is called
                        tb.AddHandler(TextBox.GotFocusEvent, new RoutedEventHandler(Cell_GotFocus));
                        tb.AddHandler(TextBox.PreviewKeyDownEvent, new KeyEventHandler(Cell_KeyDown));
                    }
                }
                AttachHandlersToChildren(child);
            }
        }

        // ✅ FIX: Tab = Select All via GotFocus (works reliably with Tab and click)
        private void Cell_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                // Use Dispatcher.BeginInvoke to ensure selection happens
                // AFTER focus is fully set, avoiding race conditions
                tb.Dispatcher.BeginInvoke(new Action(() =>
                {
                    tb.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        // Excel-style key navigation
        // Enter = move down to same column
        // Tab = handled by GotFocus select all
        private void Cell_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (sender is not TextBox tb) return;

            e.Handled = true;

            var itemsControl = FindVisualParent<ItemsControl>(tb);
            if (itemsControl == null) return;

            var row = FindVisualParent<ContentPresenter>(tb);
            if (row == null) return;

            int currentIndex = itemsControl.ItemContainerGenerator.IndexFromContainer(row);
            int nextIndex = currentIndex + 1;

            // Auto-add empty row if at the last row
            if (nextIndex >= itemsControl.Items.Count)
            {
                AddEmptyRow();
                nextIndex = itemsControl.Items.Count - 1;
            }

            var nextRow = itemsControl.ItemContainerGenerator.ContainerFromIndex(nextIndex) as FrameworkElement;
            if (nextRow == null) return;

            var nextTb = FindVisualChildByName<TextBox>(nextRow, tb.Name);
            if (nextTb != null)
            {
                nextTb.Focus();
                nextTb.SelectAll();
            }
        }

        // Add an empty row (no fake data)
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

        // Helper: find visual parent by type
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

        // Helper: find visual child by name and type
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

        // BUTTON HANDLERS use _vm.DemandParts
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

        // STATS UPDATER
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
    }
}