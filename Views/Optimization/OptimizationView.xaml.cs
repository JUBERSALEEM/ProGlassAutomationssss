using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Views.Optimization
{
    public partial class OptimizationView : UserControl
    {
        private readonly OptimizationViewModel _vm;

        public OptimizationView()
        {
            InitializeComponent();

            _vm = new OptimizationViewModel();
            DataContext = _vm;

            dgStock.ItemsSource = _vm.StockSheets;
            dgParts.ItemsSource = _vm.Parts;
        }

        private void RunOptimization_Click(object sender, RoutedEventArgs e)
        {
            _vm.RunOptimizationCommand.Execute(null);

            txtUtilization.Text = _vm.Utilization + "%";
            txtWaste.Text = _vm.Waste + "%";

            ShowTab("Layout");
            UpdateReport();
        }

        private void TabStock_Click(object sender, RoutedEventArgs e) => ShowTab("Stock");
        private void TabParts_Click(object sender, RoutedEventArgs e) => ShowTab("Parts");
        private void TabLayout_Click(object sender, RoutedEventArgs e) => ShowTab("Layout");
        private void TabReport_Click(object sender, RoutedEventArgs e) => ShowTab("Report");

        private void ShowTab(string tab)
        {
            pnlStockTab.Visibility = Visibility.Collapsed;
            pnlPartsTab.Visibility = Visibility.Collapsed;
            pnlLayoutTab.Visibility = Visibility.Collapsed;
            pnlReportTab.Visibility = Visibility.Collapsed;

            btnTabStock.Style = (Style)Resources["SegmentTabInactive"];
            btnTabParts.Style = (Style)Resources["SegmentTabInactive"];
            btnTabLayout.Style = (Style)Resources["SegmentTabInactive"];
            btnTabReport.Style = (Style)Resources["SegmentTabInactive"];

            switch (tab)
            {
                case "Stock":
                    pnlStockTab.Visibility = Visibility.Visible;
                    btnTabStock.Style = (Style)Resources["SegmentTabActive"];
                    break;
                case "Parts":
                    pnlPartsTab.Visibility = Visibility.Visible;
                    btnTabParts.Style = (Style)Resources["SegmentTabActive"];
                    break;
                case "Layout":
                    pnlLayoutTab.Visibility = Visibility.Visible;
                    btnTabLayout.Style = (Style)Resources["SegmentTabActive"];
                    break;
                case "Report":
                    pnlReportTab.Visibility = Visibility.Visible;
                    btnTabReport.Style = (Style)Resources["SegmentTabActive"];
                    break;
            }
        }

        private void UpdateReport()
        {
            if (_vm.LastResult == null)
            {
                txtReport.Text = "Run optimization to generate report";
                return;
            }

            var r = _vm.LastResult;

            txtReport.Text =
                $"Sheet Reference: {r.Ref}\n" +
                $"Sheet Size: {r.L} x {r.W} mm\n" +
                $"Area Used: {r.Area:N0} mm²\n" +
                $"Utilization: {r.Util}%\n" +
                $"Waste: {r.Waste}%\n" +
                $"Parts Placed: {r.PlacedParts?.Count ?? 0}";
        }

        public int SheetsUsed => _vm.SheetsUsed;
        public double AverageUtilization => _vm.AverageUtilization;
        public List<OptimizationResult> GetResultsList() => _vm.GetResultsList();
        public void ImportInvoiceItems(List<InvoiceItemModel> items) => _vm.ImportInvoiceItems(items);
        public void RunOptimizationFromInvoice() { _vm.RunOptimizationFromInvoice(); ShowTab("Layout"); UpdateReport(); }
        public void SetStockSheet(double width, double height) => _vm.SetStockSheet(width, height);
        public void SetTrimSettings(double lr, double br, double tr, double rm, double kerf, double breakout)
            => _vm.SetTrimSettings(lr, br, tr, rm, kerf, breakout);
    }
}