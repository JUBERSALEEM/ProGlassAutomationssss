using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.ViewModels;
using ProGlassAutomation.Data.Database;

namespace ProGlassAutomation.Views.Dashboard
{
    public partial class DashboardView : UserControl
    {
        private DashboardViewModel _viewModel;
        private bool _isLoaded;

        public DashboardView()
        {
            InitializeComponent();
            _viewModel = new DashboardViewModel();
            DataContext = _viewModel;
            Loaded += DashboardView_Loaded;
        }

        private void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isLoaded) return;
                _isLoaded = true;
                _viewModel.LoadData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardView] Error: {ex.Message}");
            }
        }

        private void SyncBalance_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.Sync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowSalesmen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dailyWorks = DbHelper.GetAllDailyWork();
                var deliveries = DbHelper.GetAllDeliveries();
                var pis = DbHelper.GetAllProformaInvoices();

                var dwSalesmen = dailyWorks.Where(d => !string.IsNullOrEmpty(d.Salesman))
                                       .Select(d => d.Salesman).Distinct().ToList();
                var delSalesmen = deliveries.Where(d => !string.IsNullOrEmpty(d.Salesman))
                                    .Select(d => d.Salesman).Distinct().ToList();
                var piSalesmen = pis.Where(p => !string.IsNullOrEmpty(p.Salesman))
                                 .Select(p => p.Salesman).Distinct().ToList();

                var allSalesmen = dwSalesmen.Union(delSalesmen).Union(piSalesmen).Distinct().OrderBy(s => s).ToList();

                var result = "SALESMEN LIST\n";
                result += "===============================\n\n";

                int index = 1;
                foreach (var s in allSalesmen)
                {
                    var dwCount = dailyWorks.Count(d => d.Salesman == s);
                    var delCount = deliveries.Count(d => d.Salesman == s);
                    var piCount = pis.Count(p => p.Salesman == s && p.Status == "Confirmed");
                    var piTotal = pis.Where(p => p.Salesman == s && p.Status == "Confirmed").Sum(p => p.NetAmount);

                    result += $"{index}. {s}\n";
                    result += $"   DailyWork: {dwCount} | Deliveries: {delCount}\n";
                    result += $"   Confirmed PIs: {piCount} | Total: AED {piTotal:N0}\n\n";
                    index++;
                }

                result += "===============================\n";
                result += $"Total Salesmen: {allSalesmen.Count}";

                MessageBox.Show(result, "Salesmen List", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}