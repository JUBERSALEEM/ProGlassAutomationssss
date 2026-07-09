using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Views.Optimization
{
    public partial class OptimizationView : UserControl
    {
        private readonly OptimizationViewModel _viewModel;

        public OptimizationView()
        {
            InitializeComponent();
            _viewModel = new OptimizationViewModel();
            DataContext = _viewModel;
        }

        private void RunOptimization_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(txtKerf.Text, out double kerf))
            {
                MessageBox.Show("Invalid Kerf value.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(txtTrim.Text, out double trim))
            {
                MessageBox.Show("Invalid Trim value.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _viewModel.SetTrimSettings(trim, trim, trim, trim, kerf, 0);

            _viewModel.RunOptimizationCommand.Execute(null);

            txtUtilization.Text = _viewModel.Utilization + "%";
            txtWaste.Text = _viewModel.Waste + "%";
            txtCost.Text = _viewModel.Cost.ToString("F2");

            LayoutCanvas.Children.Clear();
        }

        // =====================================================
        // Compatibility Surface for ProformaInvoice
        // =====================================================

        public int SheetsUsed => _viewModel.SheetsUsed;

        public double AverageUtilization => _viewModel.AverageUtilization;

        public List<OptimizationResult> GetResultsList()
            => _viewModel.GetResultsList();

        public void ImportInvoiceItems(List<InvoiceItemModel> items)
            => _viewModel.ImportInvoiceItems(items);

        public void RunOptimizationFromInvoice()
            => _viewModel.RunOptimizationFromInvoice();

        public void SetStockSheet(double width, double height)
            => _viewModel.SetStockSheet(width, height);

        public void SetTrimSettings(double lr, double br, double tr, double rm, double kerf, double breakout)
            => _viewModel.SetTrimSettings(lr, br, tr, rm, kerf, breakout);
    }
}