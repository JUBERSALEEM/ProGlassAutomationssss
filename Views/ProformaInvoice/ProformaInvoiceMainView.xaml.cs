using ProGlassAutomation.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq;

namespace ProGlassAutomation.Views.ProformaInvoice
{
    public partial class ProformaInvoiceMainView : UserControl
    {
        private Popup _currentStatusPopup = null;

        public ProformaInvoiceMainView()
        {
            InitializeComponent();
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

        // ==================== STATUS DROPDOWN HANDLER ====================
        private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                // Get the current invoice from DataContext
                var invoice = comboBox.DataContext as ProformaInvoiceModel;
                if (invoice == null) return;

                // Get the new status
                var newStatus = selectedItem.Content?.ToString();
                if (string.IsNullOrEmpty(newStatus)) return;

                // Don't update if status is the same
                if (invoice.Status == newStatus) return;

                // Update the model
                invoice.Status = newStatus;

                // Call ViewModel command
                if (DataContext is ViewModels.ProformaInvoiceMainViewModel vm)
                {
                    var param = new System.Tuple<ProformaInvoiceModel, string>(invoice, newStatus);
                    vm.ChangeStatusCommand.Execute(param);
                }
            }
        }

        // ==================== WINDOW CLICK HANDLER ====================
        private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var element = e.OriginalSource as DependencyObject;
            var statusBadge = FindAncestorByName<Border>(element, "StatusBadge");
            var statusPopup = FindAncestor<Popup>(element);

            if (statusBadge == null && statusPopup == null)
            {
                CloseAllPopups();
            }
        }

        // ==================== POPUP HELPERS ====================
        private void CloseAllPopups()
        {
            if (_currentStatusPopup != null)
            {
                _currentStatusPopup.IsOpen = false;
                _currentStatusPopup = null;
            }
        }

        private static T FindAncestorByName<T>(DependencyObject child, string name) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T t && (parent as FrameworkElement)?.Name == name)
                    return t;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private static T FindAncestor<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T)
                    return (T)parent;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private static T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T found)
                    return found;
                var result = FindChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private static IEnumerable<T> FindChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T found)
                    yield return found;
                foreach (var result in FindChildren<T>(child))
                    yield return result;
            }
        }
    }
}