using ProGlassAutomation.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

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
        }

        private void OpenStockDialog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new StockDialog(_vm) { Owner = Window.GetWindow(this) };
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OptimizationView] OpenStockDialog: {ex.Message}");
            }
        }

        private void OpenPartsDialog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ✅ FIX: Use the DemandParts collection from the VM
                var dlg = new PartsDialog(_vm) { Owner = Window.GetWindow(this) };
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OptimizationView] OpenPartsDialog: {ex.Message}");
            }
        }

        private void OpenLayoutDialog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_vm.LastSheets == null || _vm.LastSheets.Count == 0)
                {
                    MessageBox.Show("Run optimization first to view 2D layout.",
                        "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                var dlg = new LayoutDialog(_vm) { Owner = Window.GetWindow(this) };
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OptimizationView] OpenLayoutDialog: {ex.Message}");
            }
        }

        private void OpenReportDialog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_vm.LastResult == null)
                {
                    MessageBox.Show("Run optimization first to view report.",
                        "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                var dlg = new ReportDialog(_vm) { Owner = Window.GetWindow(this) };
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OptimizationView] OpenReportDialog: {ex.Message}");
            }
        }

        private void RunOptimization_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.StockSheets.Count == 0 || _vm.Parts.Count == 0)
            {
                MessageBox.Show("Please add stock sheets and parts first.",
                    "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                int rotation = rbRotComplex != null && rbRotComplex.IsChecked == true ? 2
                              : rbRot90 != null && rbRot90.IsChecked == true ? 1 : 0;

                bool success = _vm.RunOptimizationSync(
                    GetDouble(txtKerf),
                    GetDouble(txtTrim),
                    GetDouble(txtBreakLeft),
                    GetDouble(txtBreakRight),
                    GetDouble(txtBreakTop),
                    GetDouble(txtBreakBottom),
                    GetDouble(txtBreakMin),
                    rotation,
                    95);

                if (success)
                {
                    int placedCount = _vm.LastResult?.PlacedParts?.Count ?? 0;
                    int sheetCount = _vm.LastSheets?.Count ?? 0;
                    MessageBox.Show(
                        $"Optimization complete!\n\nTotal Parts Placed: {placedCount}\nSheets Used: {sheetCount}\nUtilization: {_vm.Utilization:0.##}%\nWaste: {_vm.Waste:0.##}%\n\nClick 'View 2D Layout' or 'View Report' to see results.",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Optimization error: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Rotation_Changed(object sender, RoutedEventArgs e) { }

        private void ImportFromProforma_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _vm.RunOptimizationFromInvoice();
                MessageBox.Show("Imported from ProformaInvoice successfully.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import error: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════ PROFORMA INVOICE COMPATIBILITY ═══════════
        public int SheetsUsed => _vm.SheetsUsed;
        public double AverageUtilization => _vm.AverageUtilization;
        public List<OptimizationResult> GetResultsList() => _vm.GetResultsList();

        public void ImportInvoiceItems(List<InvoiceItemModel> items)
        {
            try { _vm.ImportInvoiceItems(items); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[OptimizationView] ImportInvoiceItems: {ex.Message}"); }
        }

        public void RunOptimizationFromInvoice()
        {
            try { _vm.RunOptimizationFromInvoice(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[OptimizationView] RunOptimizationFromInvoice: {ex.Message}"); }
        }

        public void SetStockSheet(double width, double height)
        {
            try { _vm.SetStockSheet(width, height); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[OptimizationView] SetStockSheet: {ex.Message}"); }
        }

        public void SetTrimSettings(double lr, double br, double tr, double rm, double kerf, double breakout)
        {
            try { _vm.SetTrimSettings(lr, br, tr, rm, kerf, breakout); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[OptimizationView] SetTrimSettings: {ex.Message}"); }
        }

        private double GetDouble(TextBox? tb)
        {
            if (tb == null) return 0;
            return double.TryParse(tb.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0;
        }
    }
}