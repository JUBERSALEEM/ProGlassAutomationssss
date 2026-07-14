using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace ProGlassAutomation.Views.Optimization
{
    public partial class ReportDialog : Window
    {
        private readonly OptimizationViewModel _vm;

        public ReportDialog(OptimizationViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = _vm;  // ✅ FIX: bind to VM so {Binding Layouts} works
            Loaded += ReportDialog_Loaded;
        }

        private void ReportDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                PopulateHeader();
                PopulateFullTextReport();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ReportDialog] Load: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════
        // HEADER STATS
        // ══════════════════════════════════════════════════════
        private void PopulateHeader()
        {
            if (_vm.LastResult == null) return;

            int sheets = _vm.LastSheets?.Count ?? 0;
            int placed = _vm.LastResult.PlacedParts?.Count ?? 0;
            double util = _vm.Utilization;
            double waste = _vm.Waste;
            long runtime = _vm.LastResult.RuntimeMs;

            txtSheets.Text = sheets.ToString();
            txtPlaced.Text = placed.ToString();
            txtUtil.Text = $"{util:0.0}%";
            txtWaste.Text = $"{waste:0.0}%";
            txtSubtitle.Text = $"Generated {DateTime.Now:yyyy-MM-dd HH:mm:ss} · {placed} pieces placed across {sheets} sheets · Runtime {runtime} ms";
        }

        // ══════════════════════════════════════════════════════
        // TAB 2: Full Text Report
        // ══════════════════════════════════════════════════════
        private void PopulateFullTextReport()
        {
            var sb = new StringBuilder();

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("  MaxNest Optimization Report");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"  Generated:        {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"  Runtime:          {_vm.LastResult?.RuntimeMs ?? 0} ms");
            sb.AppendLine();

            sb.AppendLine("─── SUMMARY ────────────────────────────────────────────────");
            sb.AppendLine();
            int totalSheets = _vm.LastSheets?.Count ?? 0;
            int totalPlaced = _vm.LastResult?.PlacedParts?.Count ?? 0;
            double totalUsedArea = _vm.LastSheets?.Sum(s => s.UsedArea) ?? 0;
            double totalSheetArea = totalSheets * (_vm.LastSheets?.FirstOrDefault()?.StockWidth ?? 0) * (_vm.LastSheets?.FirstOrDefault()?.StockHeight ?? 0);

            sb.AppendLine($"  Total sheets used:       {totalSheets}");
            sb.AppendLine($"  Total pieces placed:     {totalPlaced}");
            sb.AppendLine($"  Overall utilization:     {_vm.Utilization:0.00}%");
            sb.AppendLine($"  Overall waste:           {_vm.Waste:0.00}%");
            sb.AppendLine($"  Total used area:         {totalUsedArea / 1_000_000.0:0.000} m²");
            sb.AppendLine($"  Total sheet area:        {totalSheetArea / 1_000_000.0:0.000} m²");
            sb.AppendLine($"  Average utilization:     {_vm.AverageUtilization:0.00}%");
            sb.AppendLine();

            sb.AppendLine("─── STOCK SHEETS ───────────────────────────────────────────");
            sb.AppendLine();
            if (_vm.StockSheets != null && _vm.StockSheets.Count > 0)
            {
                foreach (var s in _vm.StockSheets)
                {
                    sb.AppendLine($"  [{s.Index}] {s.Name}");
                    sb.AppendLine($"       Size:    {s.L:N0} × {s.W:N0} mm  ({(s.L * s.W / 1_000_000.0):0.000} m²)");
                    sb.AppendLine($"       Qty:     {s.Qty:N0}");
                    sb.AppendLine($"       Price:   AED {s.PricePerM2:0.00} /m²  → Unit: AED {s.UnitPrice:0.00}");
                    sb.AppendLine($"       Trim:    LM={s.LM} RM={s.RM} TM={s.TM} BM={s.BM} mm");
                    sb.AppendLine();
                }
            }
            else
            {
                sb.AppendLine("  (no stock sheets)");
                sb.AppendLine();
            }

            sb.AppendLine("─── PER-SHEET BREAKDOWN ────────────────────────────────────");
            sb.AppendLine();
            if (_vm.LastSheets != null && _vm.LastSheets.Count > 0)
            {
                foreach (var s in _vm.LastSheets)
                {
                    int glassCount = s.PlacedParts?.Count ?? 0;
                    int rotated = s.PlacedParts?.Count(p => p.Rotated) ?? 0;
                    sb.AppendLine($"  Sheet #{s.SheetNum}");
                    sb.AppendLine($"    Stock:           {s.StockWidth:N0} × {s.StockHeight:N0} mm  ({(s.StockWidth * s.StockHeight / 1_000_000.0):0.000} m²)");
                    sb.AppendLine($"    Pieces placed:   {glassCount}");
                    sb.AppendLine($"    Rotated 90°:     {rotated}");
                    sb.AppendLine($"    Utilization:     {s.Utilization:0.00}%");
                    sb.AppendLine($"    Used area:       {s.UsedArea / 1_000_000.0:0.000} m²");
                    sb.AppendLine($"    Waste area:      {s.WasteArea / 1_000_000.0:0.000} m²");

                    if (s.PlacedParts != null && s.PlacedParts.Count > 0)
                    {
                        sb.AppendLine($"    Parts:");
                        int idx = 1;
                        foreach (var p in s.PlacedParts)
                        {
                            string label = string.IsNullOrEmpty(p.Label) ? $"Part {idx}" : p.Label;
                            string rot = p.Rotated ? " (rotated 90°)" : "";
                            sb.AppendLine($"      {idx,2}. {label,-20} {p.L,5:N0} × {p.W,5:N0} mm at ({p.X,4:N0}, {p.Y,4:N0}){rot}");
                            idx++;
                        }
                    }
                    sb.AppendLine();
                }
            }
            else
            {
                sb.AppendLine("  (no sheets)");
                sb.AppendLine();
            }

            sb.AppendLine("─── PARTS TO CUT ────────────────────────────────────────────");
            sb.AppendLine();
            if (_vm.DemandParts != null && _vm.DemandParts.Count > 0)
            {
                sb.AppendLine($"  Total part types:        {_vm.DemandParts.Count}");
                sb.AppendLine($"  Total pieces:            {_vm.DemandParts.Sum(p => p.Qty)}");
                sb.AppendLine($"  Total area required:     {_vm.DemandParts.Sum(p => p.L * p.W * p.Qty) / 1_000_000.0:0.000} m²");
                sb.AppendLine();
                sb.AppendLine("  Sr  Label                  L × W (mm)          Qty");
                sb.AppendLine("  ──  ────────────────────   ─────────────────   ────");
                foreach (var p in _vm.DemandParts)
                {
                    string label = string.IsNullOrEmpty(p.Label) ? "(no label)" : p.Label;
                    sb.AppendLine($"  {p.SrNo,2}  {label,-20}  {p.L,5:N0} × {p.W,5:N0}      {p.Qty,4}");
                }
                sb.AppendLine();

                var max = _vm.DemandParts.OrderByDescending(p => p.L * p.W).FirstOrDefault();
                if (max != null)
                {
                    sb.AppendLine($"  Largest piece: {max.Label} — {max.L:N0}×{max.W:N0} mm");
                }
            }
            else
            {
                sb.AppendLine("  (no parts)");
            }
            sb.AppendLine();

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("  End of report");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");

            txtFullReport.Text = sb.ToString();
        }

        // ══════════════════════════════════════════════════════
        // ACTIONS
        // ══════════════════════════════════════════════════════
        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(txtFullReport.Text))
                {
                    Clipboard.SetText(txtFullReport.Text);
                    txtFooterInfo.Text = "✓ Report copied to clipboard.";
                }
                else
                {
                    var sb = new StringBuilder();
                    foreach (var row in _vm.Layouts)
                    {
                        sb.AppendLine($"Sheet #{row.SheetNum} | {row.Dimensions} | {row.GlassCount} pieces | {row.YieldText} | Used: {row.UsedNet:F3} m² | Waste: {row.ScrapWaste:F3} m²");
                    }
                    Clipboard.SetText(sb.ToString());
                    txtFooterInfo.Text = "✓ Per-sheet breakdown copied to clipboard.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Copy failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog
                {
                    Filter = "Text Files (*.txt)|*.txt|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    FileName = $"MaxNest_Report_{DateTime.Now:yyyyMMdd_HHmmss}",
                    DefaultExt = ".txt",
                    Title = "Save Report"
                };

                if (dlg.ShowDialog(this) == true)
                {
                    string content = dlg.FilterIndex == 2 ? BuildCsv() : txtFullReport.Text;
                    File.WriteAllText(dlg.FileName, content, Encoding.UTF8);
                    txtFooterInfo.Text = $"✓ Report saved to: {Path.GetFileName(dlg.FileName)}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string BuildCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Sheet #,Dimensions,Glass Count,Rotated,Yield %,Used (m²),Waste (m²)");
            foreach (var row in _vm.Layouts)
            {
                sb.AppendLine($"{row.SheetNum},{row.Dimensions},{row.GlassCount},{row.Rotated90},{row.YieldPct:0.00},{row.UsedNet:0.000},{row.ScrapWaste:0.000}");
            }
            return sb.ToString();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}