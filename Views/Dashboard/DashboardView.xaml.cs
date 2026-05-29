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
                _viewModel.LoadData();
                System.Diagnostics.Debug.WriteLine("[DashboardView] Loaded");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardView] Error: {ex.Message}");
            }
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                _viewModel.LoadData();
            }
            catch { }
        }

        private void SyncBalance_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.Sync();
                MessageBox.Show("Data synchronized!", "Sync", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("Dashboard refreshed!", "Refresh", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Refresh failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActivateNow_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Activate();
        }

        private void ShowSalesmen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dailyWorks = DbHelper.GetAllDailyWork();
                var deliveries = DbHelper.GetAllDeliveries();

                var dwSalesmen = dailyWorks.Where(d => !string.IsNullOrEmpty(d.Salesman))
                                           .Select(d => d.Salesman).Distinct().ToList();

                var delSalesmen = deliveries.Where(d => !string.IsNullOrEmpty(d.Salesman))
                                            .Select(d => d.Salesman).Distinct().ToList();

                var allSalesmen = dwSalesmen.Union(delSalesmen).OrderBy(s => s).ToList();

                var result = "👥 ALL SALESMEN\n\n";
                result += $"From DailyWork: {dwSalesmen.Count}\n";
                foreach (var s in dwSalesmen)
                {
                    var count = dailyWorks.Count(d => d.Salesman == s);
                    result += $"  • {s} ({count} records)\n";
                }

                result += $"\nFrom Deliveries: {delSalesmen.Count}\n";
                foreach (var s in delSalesmen)
                {
                    var count = deliveries.Count(d => d.Salesman == s);
                    result += $"  • {s} ({count} records)\n";
                }

                result += $"\n═══════════════════════════\n";
                result += $"Total Unique Salesmen: {allSalesmen.Count}\n\n";
                foreach (var s in allSalesmen)
                {
                    var dwCount = dailyWorks.Count(d => d.Salesman == s);
                    var delCount = deliveries.Count(d => d.Salesman == s);
                    result += $"  • {s}\n    DailyWork: {dwCount} | Deliveries: {delCount}\n";
                }

                MessageBox.Show(result, "Salesmen List", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}