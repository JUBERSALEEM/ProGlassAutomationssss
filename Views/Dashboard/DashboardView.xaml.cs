using System.Windows;
using System.Windows.Controls;

namespace ProGlassAutomation.Views.Dashboard
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            Loaded += DashboardView_Loaded;
        }

        private void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.DashboardViewModel vm)
            {
                vm.LoadData();
            }
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is ViewModels.DashboardViewModel vm && FilterComboBox.SelectedItem is ComboBoxItem item)
            {
                vm.SelectedFilter = item.Content.ToString();
                vm.LoadData();
            }
        }

        private void ProformaInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.DashboardViewModel vm)
                vm.Navigate("ProformaInvoice");
        }

        private void JobOrders_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.DashboardViewModel vm)
                vm.Navigate("JobOrders");
        }

        private void Deliveries_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.DashboardViewModel vm)
                vm.Navigate("Deliveries");
        }

        private void SheetStore_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.DashboardViewModel vm)
                vm.Navigate("SheetStore");
        }

        private void SyncBalance_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.DashboardViewModel vm)
                vm.Sync();
        }

        private void ViewBalanceReports_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.DashboardViewModel vm)
                vm.Navigate("BalanceReports");
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.DashboardViewModel vm)
                vm.LoadData();
        }

        private void ActivateNow_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.DashboardViewModel vm)
                vm.Activate();
        }
    }
}