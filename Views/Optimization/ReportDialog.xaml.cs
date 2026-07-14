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

            // KPI cards
            txtGrossYield.Text = $"{_vm.Utilization:0.##}%";
            txtScrap.Text = $"{_vm.Waste:0.##}%";
            txtSheetsUsed.Text = _vm.LastSheets.Count.ToString();
            txtRuntime.Text = $"{_vm.LastResult.RuntimeMs} ms";
            txtReportSubtitle.Text = $"Generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            // Per-sheet DataGrid
            dgLayouts.ItemsSource = _vm.Layouts;

            // Build full text report
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine("  MaxNest Optimization Report");
            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine($"  Generated:    {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"  Runtime:      {_vm.LastResult.RuntimeMs} ms");
            sb.AppendLine();
            sb.AppendLine("───────────────────────────────────────────────────────");
            sb.AppendLine("  OVERALL METRICS");
            sb.AppendLine("───────────────────────────────────────────────────────");
            sb.AppendLine($"  Sheets Used:   {_vm.LastSheets.Count}");
            sb.AppendLine($"  Utilization:   {_vm.Utilization:0.##}%");
            sb.AppendLine($"  Waste:         {_vm.Waste:0.##}%");
            sb.AppendLine($"  Parts Placed:  {_vm.LastResult.PlacedParts?.Count ?? 0}");
            sb.AppendLine();
            sb.AppendLine("───────────────────────────────────────────────────────");
            sb.AppendLine("  PER-SHEET BREAKDOWN");
            sb.AppendLine("───────────────────────────────────────────────────────");
            foreach (var s in _vm.LastSheets)
            {
                int count = s.PlacedParts?.Count ?? 0;
                int rot = s.PlacedParts?.Count(p => p.Rotated) ?? 0;
                sb.AppendLine($"  Sheet #{s.SheetNum}:");
                sb.AppendLine($"    Dimensions:  {s.StockWidth:N0} × {s.StockHeight:N0} mm");
                sb.AppendLine($"    Pieces:      {count}");
                sb.AppendLine($"    Rotated:     {rot} (90°)");
                sb.AppendLine($"    Yield:       {s.Utilization:0.0}%");
                sb.AppendLine($"    Used:        {s.UsedArea / 1_000_000.0:0.000} m²");
                sb.AppendLine($"    Scrap:       {s.WasteArea / 1_000_000.0:0.000} m²");
                sb.AppendLine();
            }
            sb.AppendLine("───────────────────────────────────────────────────────");
            sb.AppendLine("  STOCK INVENTORY");
            sb.AppendLine("───────────────────────────────────────────────────────");
            foreach (var stock in _vm.StockSheets)
            {
                sb.AppendLine($"  #{stock.Index} {stock.Name}");
                sb.AppendLine($"    Dimensions:  {stock.L:N0} × {stock.W:N0} mm");
                sb.AppendLine($"    Quantity:    {stock.Qty:N0}");
                sb.AppendLine($"    LM/RM/TM/BM: {stock.LM}/{stock.RM}/{stock.TM}/{stock.BM} mm");
                sb.AppendLine($"    Price/m²:    {stock.PricePerM2:N2}");
                sb.AppendLine($"    Unit Price:  {stock.UnitPrice:N2}");
                sb.AppendLine();
            }
            sb.AppendLine("───────────────────────────────────────────────────────");
            sb.AppendLine("  PARTS INVENTORY");
            sb.AppendLine("───────────────────────────────────────────────────────");
            int idx = 1;
            foreach (var part in _vm.Parts)
            {
                sb.AppendLine($"  {idx++}. {part.L:N0} × {part.W:N0} mm  |  Qty: {part.Qty}");
            }
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine("  End of report");
            sb.AppendLine("═══════════════════════════════════════════════════════");

            txtFullReport.Text = sb.ToString();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(txtFullReport.Text);
                MessageBox.Show("Report copied to clipboard.",
                    "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Copy error: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show("Report saved.",
                        "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save error: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}