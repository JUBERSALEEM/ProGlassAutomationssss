using System.Windows.Controls;
using ProGlassAutomation.ViewModels;
using DbJobOrder = ProGlassAutomation.Data.Database.JobOrderModel;

namespace ProGlassAutomation.Views.JobOrder
{
    public partial class JobOrderListView : UserControl
    {
        // Track updating status to prevent re-entry loops
        private bool _isUpdatingStatus = false;
        private bool _isInitialized = false;

        public JobOrderListView()
        {
            InitializeComponent();

            // Set initialized after first render
            Loaded += (s, e) => _isInitialized = true;
        }

        private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is JobOrderListViewModel vm && vm.SelectedJobOrder != null)
            {
                // Open in view mode on double-click
                vm.ViewJobOrderCommand.Execute(vm.SelectedJobOrder);
            }
        }

        private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Guard: Prevent re-entry if already updating
            if (_isUpdatingStatus) return;

            // Guard: Not fully initialized yet
            if (!_isInitialized) return;

            // Guard: No actual change
            if (e.AddedItems.Count == 0) return;

            if (sender is ComboBox comboBox &&
                comboBox.DataContext is DbJobOrder jobOrder &&
                comboBox.SelectedItem != null)
            {
                var newStatus = comboBox.SelectedItem?.ToString();

                if (!string.IsNullOrEmpty(newStatus) &&
                    DataContext is JobOrderListViewModel vm)
                {
                    // Only update if status actually changed
                    if (jobOrder.Status != newStatus)
                    {
                        _isUpdatingStatus = true;

                        // Update status - will refresh UI after DB success
                        vm.UpdateStatus(jobOrder, newStatus);

                        _isUpdatingStatus = false;
                    }
                }
            }
        }
    }
}