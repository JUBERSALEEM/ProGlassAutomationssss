using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.ViewModels;
using ProGlassAutomation.Data.Database;
using ProGlassAutomation.Models;

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

        // ✅ Sync button now uses the command from ViewModel (with IsSyncing state)
        // No more separate SyncBalance_Click and Refresh_Click handlers - they all use SyncCommand

        private void ShowSalesmen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var piList = SharedViewModels.ProformaInvoiceListVM;
                var joList = SharedViewModels.JobOrderListVM;
                var dwList = SharedViewModels.DailyWorksVM;

                var piSalesmen = piList?.AllInvoices?
                    .Where(i => !string.IsNullOrEmpty(i.Salesman))
                    .Select(i => i.Salesman).Distinct().ToList() ?? new System.Collections.Generic.List<string>();

                var joSalesmen = joList?.JobOrders?
                    .Where(j => !string.IsNullOrEmpty(j.Salesman))
                    .Select(j => j.Salesman).Distinct().ToList() ?? new System.Collections.Generic.List<string>();

                var dwSalesmen = dwList?.DailyWorks?
                    .Where(d => !string.IsNullOrEmpty(d.Salesman))
                    .Select(d => d.Salesman).Distinct().ToList() ?? new System.Collections.Generic.List<string>();

                var allSalesmen = piSalesmen.Union(joSalesmen).Union(dwSalesmen).Distinct().OrderBy(s => s).ToList();

                var result = "SALES TEAM SUMMARY\n===============================\n\n";
                int idx = 1;
                foreach (var s in allSalesmen)
                {
                    var piCount = piList?.AllInvoices?.Count(i => i.Salesman == s && i.Status == "Confirmed") ?? 0;
                    var piTotal = piList?.AllInvoices?
                        .Where(i => i.Salesman == s && i.Status == "Confirmed")
                        .Sum(i => (decimal)i.NetTotal) ?? 0;
                    var joCount = joList?.JobOrders?.Count(j => j.Salesman == s) ?? 0;
                    var dwCount = dwList?.DailyWorks?.Count(d => d.Salesman == s) ?? 0;

                    result += $"{idx}. {s}\n";
                    result += $"   PIs: {piCount} ({piTotal:N0} AED) | JOs: {joCount} | DWs: {dwCount}\n\n";
                    idx++;
                }
                result += "===============================\nTotal Active: " + allSalesmen.Count;

                MessageBox.Show(result, "Sales Team", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
}