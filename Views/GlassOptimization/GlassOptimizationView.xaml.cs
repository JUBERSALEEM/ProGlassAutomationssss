using System.Collections.Generic;
using System.Linq;
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

        // Select all when field gets focus
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

        // Select all on mouse click
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

        // Handle Enter and Tab keys
        private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            if (e.Key == Key.Enter)
            {
                UpdateValue(textBox);
                MoveToNextField(textBox);
                e.Handled = true;
            }
            else if (e.Key == Key.Tab && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                UpdateValue(textBox);

                if (IsLastField(textBox))
                {
                    var viewModel = DataContext as GlassOptimizationViewModel;
                    viewModel?.AddCommand.Execute(null);

                    Dispatcher.BeginInvoke(new System.Action(() =>
                    {
                        FocusFirstFieldOfLastSheet();
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
                else
                {
                    MoveToNextField(textBox);
                }
            }
        }

        private void UpdateValue(TextBox textBox)
        {
            var binding = textBox.GetBindingExpression(TextBox.TextProperty);
            binding?.UpdateSource();
        }

        private bool IsLastField(TextBox textBox)
        {
            var textBoxes = GetAllInputTextBoxesInSheet(textBox);
            return textBoxes.Count > 0 && textBoxes[textBoxes.Count - 1] == textBox;
        }

        private List<TextBox> GetAllInputTextBoxesInSheet(TextBox currentBox)
        {
            var textBoxes = new List<TextBox>();

            // Find the parent ItemsControl.Item
            var itemContainer = FindParentContentPresenter(currentBox);
            if (itemContainer == null)
                return textBoxes;

            // Find the inner Grid with the 4 columns
            var innerGrid = FindInputGrid(itemContainer);
            if (innerGrid == null)
                return textBoxes;

            // Get StackPanels and their TextBoxes in order
            var columnTextBoxes = new Dictionary<int, TextBox>();

            foreach (var child in innerGrid.Children)
            {
                if (child is FrameworkElement fe)
                {
                    var column = Grid.GetColumn(fe);
                    if (column >= 0 && column <= 2) // Only input columns
                    {
                        var textBox = FindTextBoxInChild(fe);
                        if (textBox != null)
                        {
                            columnTextBoxes[column] = textBox;
                        }
                    }
                }
            }

            // Return in column order
            textBoxes = columnTextBoxes.OrderBy(x => x.Key).Select(x => x.Value).ToList();
            return textBoxes;
        }

        private ContentPresenter? FindParentContentPresenter(DependencyObject child)
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is ContentPresenter cp)
                    return cp;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private Grid? FindInputGrid(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is Grid grid && Grid.GetColumn(grid) == -1)
                {
                    // Check if this grid has 4 columns
                    if (grid.ColumnDefinitions.Count == 4)
                        return grid;
                }

                var found = FindInputGrid(child);
                if (found != null)
                    return found;
            }
            return null;
        }

        private TextBox? FindTextBoxInChild(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is TextBox tb)
                    return tb;
            }
            return null;
        }

        private void MoveToNextField(TextBox currentBox)
        {
            var textBoxes = GetAllInputTextBoxesInSheet(currentBox);
            var currentIndex = textBoxes.IndexOf(currentBox);

            if (currentIndex >= 0 && currentIndex < textBoxes.Count - 1)
            {
                var nextBox = textBoxes[currentIndex + 1];
                nextBox.Focus();
            }
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

            var innerGrid = FindInputGrid(container);
            if (innerGrid == null)
                return;

            var columnTextBoxes = new Dictionary<int, TextBox>();

            foreach (var child in innerGrid.Children)
            {
                if (child is FrameworkElement fe)
                {
                    var column = Grid.GetColumn(fe);
                    if (column >= 0 && column <= 2)
                    {
                        var textBox = FindTextBoxInChild(fe);
                        if (textBox != null)
                        {
                            columnTextBoxes[column] = textBox;
                        }
                    }
                }
            }

            var textBoxes = columnTextBoxes.OrderBy(x => x.Key).Select(x => x.Value).ToList();

            if (textBoxes.Count > 0)
            {
                textBoxes[0].Focus();
                Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    textBoxes[0].SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
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