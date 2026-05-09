using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ProGlassAutomation.Views.GlassOptimization
{
    public partial class GlassOptimizationView : UserControl
    {
        public GlassOptimizationView()
        {
            InitializeComponent();
        }

        private void InputBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    textBox.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        private void InputBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (!textBox.IsFocused)
                {
                    e.Handled = true;
                    textBox.Focus();
                }
                else
                {
                    textBox.SelectAll();
                }
            }
        }

        private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            var viewModel = DataContext as GlassOptimizationViewModel;
            if (viewModel == null)
                return;

            if (e.Key == Key.Enter)
            {
                MoveToNext(textBox);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Tab && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                int column = GetParentColumn(textBox);

                if (column == 2)
                {
                    viewModel.AddCommand.Execute(null);
                    e.Handled = true;

                    Dispatcher.BeginInvoke(new System.Action(() =>
                    {
                        FocusFirstFieldOfLastSheet();
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }
        }

        private int GetParentColumn(TextBox textBox)
        {
            var parent = VisualTreeHelper.GetParent(textBox);
            while (parent != null)
            {
                if (parent is StackPanel sp)
                    return Grid.GetColumn(sp);
                parent = VisualTreeHelper.GetParent(parent);
            }
            return -1;
        }

        private void MoveToNext(TextBox currentBox)
        {
            var presenter = FindParent<ContentPresenter>(currentBox);
            if (presenter == null)
                return;

            var grid = FindInputGrid(presenter);
            if (grid == null)
                return;

            var textBoxes = GetTextBoxesInOrder(grid);
            int idx = textBoxes.IndexOf(currentBox);

            if (idx >= 0 && idx < textBoxes.Count - 1)
            {
                textBoxes[idx + 1].Focus();
            }
        }

        private List<TextBox> GetTextBoxesInOrder(Grid grid)
        {
            var result = new List<TextBox>();

            for (int col = 0; col <= 2; col++)
            {
                foreach (var child in grid.Children)
                {
                    if (child is StackPanel sp && Grid.GetColumn(sp) == col)
                    {
                        var tb = GetTextBox(sp);
                        if (tb != null)
                        {
                            result.Add(tb);
                            break;
                        }
                    }
                }
            }

            return result;
        }

        private void FocusFirstFieldOfLastSheet()
        {
            var itemsControl = FindVisualChild<ItemsControl>(this);
            if (itemsControl == null || itemsControl.Items.Count == 0)
                return;

            var lastItem = itemsControl.Items[itemsControl.Items.Count - 1];
            var container = itemsControl.ItemContainerGenerator.ContainerFromItem(lastItem) as ContentPresenter;
            if (container == null)
                return;

            var grid = FindInputGrid(container);
            if (grid == null)
                return;

            foreach (var child in grid.Children)
            {
                if (child is StackPanel sp && Grid.GetColumn(sp) == 0)
                {
                    var tb = GetTextBox(sp);
                    if (tb != null)
                    {
                        tb.Focus();
                        Dispatcher.BeginInvoke(new System.Action(() => tb.SelectAll()),
                            System.Windows.Threading.DispatcherPriority.Input);
                        return;
                    }
                }
            }
        }

        private TextBox? GetTextBox(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is TextBox tb)
                    return tb;
            }
            return null;
        }

        private T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T result)
                    return result;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private Grid? FindInputGrid(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is Grid g && g.ColumnDefinitions.Count == 4)
                    return g;

                var found = FindInputGrid(child);
                if (found != null)
                    return found;
            }
            return null;
        }

        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var found = FindVisualChild<T>(child);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}