using ProGlassAutomation.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ProGlassAutomation.Views.ProformaInvoice
{
    public partial class ProformaInvoiceMainView : UserControl
    {
        public ProformaInvoiceMainView()
        {
            InitializeComponent();

            // Subscribe to window mouse down to close popups when clicking outside
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.PreviewMouseLeftButtonDown += Window_PreviewMouseLeftButtonDown;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.PreviewMouseLeftButtonDown -= Window_PreviewMouseLeftButtonDown;
            }
        }

        private void StatusBadge_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Prevent DataGrid row selection
            e.Handled = true;

            if (sender is Border border)
            {
                var grid = border.Parent as Grid;
                if (grid != null)
                {
                    foreach (var child in grid.Children)
                    {
                        if (child is Popup popup)
                        {
                            // Close all other popups first
                            CloseAllOtherPopups(popup);

                            // Toggle this popup - SINGLE CLICK
                            popup.IsOpen = !popup.IsOpen;
                            return;
                        }
                    }
                }
            }
        }

        private void StatusOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var newStatus = button.Tag?.ToString();
                if (string.IsNullOrEmpty(newStatus)) return;

                var invoice = button.DataContext as ProformaInvoiceModel;
                if (invoice == null) return;

                // Close all popups
                CloseAllPopups();

                // Call ViewModel with Tuple
                if (DataContext is ViewModels.ProformaInvoiceMainViewModel vm)
                {
                    var param = new System.Tuple<ProformaInvoiceModel, string>(invoice, newStatus);
                    vm.ChangeStatusCommand.Execute(param);
                }
            }
        }

        private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Close all popups when clicking outside
            var element = e.OriginalSource as DependencyObject;

            // Check if click is on any StatusBadge or inside a StatusPopup
            var statusBadge = FindAncestorByName<Border>(element, "StatusBadge");
            var statusPopup = FindAncestor<Popup>(element);

            // Only close if clicking outside
            if (statusBadge == null && statusPopup == null)
            {
                CloseAllPopups();
            }
        }

        private void CloseAllOtherPopups(Popup keepOpen)
        {
            var dataGrid = FindChild<DataGrid>(this);
            if (dataGrid != null)
            {
                var allPopups = FindChildren<Popup>(dataGrid);
                foreach (var popup in allPopups)
                {
                    if (popup != keepOpen)
                    {
                        popup.IsOpen = false;
                    }
                }
            }
        }

        private void CloseAllPopups()
        {
            var dataGrid = FindChild<DataGrid>(this);
            if (dataGrid != null)
            {
                var allPopups = FindChildren<Popup>(dataGrid);
                foreach (var popup in allPopups)
                {
                    popup.IsOpen = false;
                }
            }
        }

        // Helper: Find ancestor by name
        private static T FindAncestorByName<T>(DependencyObject child, string name) where T : DependencyObject
        {
            var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T t && (parent as FrameworkElement)?.Name == name)
                    return t;
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        // Helper: Find ancestor of type
        private static T FindAncestor<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T)
                    return (T)parent;
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        // Helper: Find first child of type
        private static T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T found)
                    return found;
                var result = FindChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        // Helper: Find all children of type
        private static IEnumerable<T> FindChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T found)
                    yield return found;
                foreach (var result in FindChildren<T>(child))
                    yield return result;
            }
        }
    }
}