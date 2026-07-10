using System;
using System.Linq;
using System.Text;
using System.Windows;

namespace ProGlassAutomation.Views.Optimization
{
    public partial class ReportDialog : Window
    {
        private readonly OptimizationViewModel _vm;
        private readonly string _fullReport;

        public ReportDialog(OptimizationViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            LoadData();
            _fullReport = _vm.BuildReport();
        }

        private void LoadData()
        {
            if (_vm.LastResult == null) return;

            txtGrossYield.Text = $"{_vm.Utilization:0.##}%";
            txtScrap.Text = $"{_vm.Waste:0.##}%";
            txtSheetsUsed.Text = _vm.LastSheets.Count.ToString();
            txtRuntime.Text = $"{_vm.LastResult.RuntimeMs} ms";
            txtReportSubtitle.Text = $"Generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            dgLayouts.ItemsSource = _vm.Layouts;

            var sb = new StringBuilder();
            sb.AppendLine("=== MaxNest Optimization Report ===");
            sb.AppendLine($"Generated: {DateTime.Now}");
            sb.AppendLine($"Runtime: {_vm.LastResult.RuntimeMs} ms");
            sb.AppendLine();
            sb.AppendLine("=== OVERALL METRICS ===");
            sb.AppendLine($"Sheets Used:     {_vm.LastSheets.Count}");
            sb.AppendLine($"Utilization:     {_vm.Utilization:0.##}%");
            sb.AppendLine($"Waste:           {_vm.Waste:0.##}%");
            sb.AppendLine($"Parts Placed:    {_vm.LastResult.PlacedParts?.Count ?? 0}");
            sb.AppendLine();
            sb.AppendLine("=== PER-SHEET BREAKDOWN ===");
            foreach (var s in _vm.LastSheets)
            {
                int count = s.PlacedParts?.Count ?? 0;
                int rot = s.PlacedParts?.Count(p => p.Rotated) ?? 0;
                sb.AppendLine($"  Sheet #{s.SheetNum}: {s.StockWidth:N0}x{s.StockHeight:N0} mm | Pieces: {count} | Rotated: {rot} | Yield: {s.Utilization:0.0}% | Scrap: {(s.WasteArea / 1_000_000.0):0.000} m²");
            }
            sb.AppendLine();
            sb.AppendLine("=== STOCK INVENTORY ===");
            foreach (var stock in _vm.StockSheets)
            {
                sb.AppendLine($"  {stock.Index}. {stock.L:N0}x{stock.W:N0} mm | Qty: {stock.Qty:N0} | ${stock.PricePerM2:N2}/m² | Total: ${stock.UnitPrice:N2}");
            }
            sb.AppendLine();
            sb.AppendLine("=== PARTS INVENTORY ===");
            int idx = 1;
            foreach (var part in _vm.Parts)
            {
                sb.AppendLine($"  {idx++}. {part.L:N0}x{part.W:N0} mm | Qty: {part.Qty}");
            }
            txtFullReport.Text = sb.ToString();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(_fullReport);
                MessageBox.Show("Report copied to clipboard.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Copy error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files|*.csv",
                    FileName = $"MaxNest_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };
                if (dlg.ShowDialog() == true)
                {
                    _vm.ExportCsv(dlg.FileName);
                    MessageBox.Show("Report saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}