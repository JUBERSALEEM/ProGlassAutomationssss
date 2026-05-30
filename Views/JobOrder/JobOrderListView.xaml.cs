using ProGlassAutomation.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using DbJobOrder = ProGlassAutomation.Data.Database.JobOrderModel;

namespace ProGlassAutomation.Views.JobOrder
{
    public partial class JobOrderListView : UserControl
    {
        public JobOrderListView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is JobOrderListViewModel vm && vm.SelectedJobOrder != null)
            {
                vm.ViewJobOrderCommand.Execute(vm.SelectedJobOrder);
            }
        }

        private void StatusBadge_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Parent is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is Popup popup)
                    {
                        popup.IsOpen = !popup.IsOpen;
                        break;
                    }
                }
            }
            e.Handled = true;
        }

        private void StatusOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string newStatus)
            {
                // Close popup
                var popup = FindParent<Popup>(btn);
                if (popup != null)
                    popup.IsOpen = false;

                // Find JobOrder from row
                var jobOrder = FindDataContext<DbJobOrder>(btn);
                if (jobOrder != null && DataContext is JobOrderListViewModel vm)
                {
                    vm.UpdateStatus(jobOrder, newStatus);
                }
            }
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T typedParent)
                    return typedParent;
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private static T? FindDataContext<T>(DependencyObject element) where T : class
        {
            while (element != null)
            {
                if (element is FrameworkElement fe && fe.DataContext is T result)
                    return result;
                element = System.Windows.Media.VisualTreeHelper.GetParent(element);
            }
            return null;
        }
    }
}