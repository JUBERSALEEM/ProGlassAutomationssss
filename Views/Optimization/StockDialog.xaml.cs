using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ProGlassAutomation.Views.Optimization
{
    public partial class StockDialog : Window
    {
        private readonly OptimizationViewModel _vm;
        private StockSheetViewModel? _editing;

        public StockDialog(OptimizationViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = _vm;  // ✅ FIX: so {Binding StockSheets} resolves
            dgStock.ItemsSource = _vm.StockSheets;

            _vm.StockSheets.CollectionChanged += (s, e) => UpdateSummary();
            UpdateSummary();
        }

        private void ApplyMargins_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new StockMarginDialog { Owner = Window.GetWindow(this) };
                if (dlg.ShowDialog() == true)
                {
                    txtLM.Text = dlg.LM.ToString("0", CultureInfo.InvariantCulture);
                    txtRM.Text = dlg.RM.ToString("0", CultureInfo.InvariantCulture);
                    txtTM.Text = dlg.TM.ToString("0", CultureInfo.InvariantCulture);
                    txtBM.Text = dlg.BM.ToString("0", CultureInfo.InvariantCulture);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StockDialog] ApplyMargins: {ex.Message}");
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Please enter a sheet name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus(); return;
            }
            if (!double.TryParse(txtWidth.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double w) || w <= 0)
            { MessageBox.Show("Enter a valid width.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); txtWidth.Focus(); return; }
            if (!double.TryParse(txtHeight.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double h) || h <= 0)
            { MessageBox.Show("Enter a valid height.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); txtHeight.Focus(); return; }
            if (!int.TryParse(txtQty.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int qty) || qty < 0)
            { MessageBox.Show("Enter a valid quantity.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); txtQty.Focus(); return; }

            double price = 0;
            if (!string.IsNullOrWhiteSpace(txtPrice.Text))
            {
                if (!double.TryParse(txtPrice.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out price) || price < 0)
                { MessageBox.Show("Enter a valid price.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); txtPrice.Focus(); return; }
            }

            double lm = ParseOr(txtLM.Text, 15);
            double rm = ParseOr(txtRM.Text, 15);
            double tm = ParseOr(txtTM.Text, 15);
            double bm = ParseOr(txtBM.Text, 15);

            if (_editing == null)
            {
                _vm.StockSheets.Add(new StockSheetViewModel
                {
                    Index = _vm.StockSheets.Count + 1,
                    Name = txtName.Text.Trim(),
                    L = w,
                    W = h,
                    Qty = qty,
                    PricePerM2 = price,
                    LM = lm,
                    RM = rm,
                    TM = tm,
                    BM = bm
                });
            }
            else
            {
                _editing.Name = txtName.Text.Trim();
                _editing.L = w; _editing.W = h; _editing.Qty = qty; _editing.PricePerM2 = price;
                _editing.LM = lm; _editing.RM = rm; _editing.TM = tm; _editing.BM = bm;
                _editing = null;
                btnAdd.Content = "+ Add Stock";
                btnCancel.Visibility = Visibility.Collapsed;
            }

            ClearForm();
            Reindex();
            UpdateSummary();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is StockSheetViewModel stock)
            {
                _editing = stock;
                txtName.Text = stock.Name;
                txtWidth.Text = stock.L.ToString(CultureInfo.InvariantCulture);
                txtHeight.Text = stock.W.ToString(CultureInfo.InvariantCulture);
                txtQty.Text = stock.Qty.ToString(CultureInfo.InvariantCulture);
                txtPrice.Text = stock.PricePerM2 > 0 ? stock.PricePerM2.ToString(CultureInfo.InvariantCulture) : "";
                txtLM.Text = stock.LM.ToString(CultureInfo.InvariantCulture);
                txtRM.Text = stock.RM.ToString(CultureInfo.InvariantCulture);
                txtTM.Text = stock.TM.ToString(CultureInfo.InvariantCulture);
                txtBM.Text = stock.BM.ToString(CultureInfo.InvariantCulture);
                btnAdd.Content = "✓ Update";
                btnCancel.Visibility = Visibility.Visible;
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is StockSheetViewModel stock)
            {
                var r = MessageBox.Show($"Delete '{stock.Name}' (#{stock.Index})?", "Confirm Delete",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    if (_editing == stock)
                    {
                        _editing = null;
                        btnAdd.Content = "+ Add Stock";
                        btnCancel.Visibility = Visibility.Collapsed;
                    }
                    _vm.StockSheets.Remove(stock);
                    Reindex();
                    UpdateSummary();
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            btnAdd.Content = "+ Add Stock";
            btnCancel.Visibility = Visibility.Collapsed;
            ClearForm();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void ClearForm()
        {
            txtName.Text = "Standard Sheet";
            txtWidth.Text = "3210";
            txtHeight.Text = "2250";
            txtQty.Text = "9999";
            txtPrice.Text = "25";
            txtLM.Text = "15";
            txtRM.Text = "15";
            txtTM.Text = "15";
            txtBM.Text = "15";
        }

        private void Reindex()
        {
            for (int i = 0; i < _vm.StockSheets.Count; i++)
            {
                if (_vm.StockSheets[i].Index != i + 1)
                    _vm.StockSheets[i].Index = i + 1;
            }
        }

        private void UpdateSummary()
        {
            if (!IsLoaded) return;
            int totalSheets = _vm.StockSheets.Sum(s => s.Qty);
            double totalArea = _vm.StockSheets.Sum(s => s.Area * s.Qty);
            double avgPrice = _vm.StockSheets.Count > 0
                ? _vm.StockSheets.Where(s => s.PricePerM2 > 0).DefaultIfEmpty().Average(s => s?.PricePerM2 ?? 0)
                : 0;
            double totalValue = _vm.StockSheets.Sum(s => s.UnitPrice * s.Qty);

            txtTotalSheets.Text = totalSheets.ToString("N0");
            txtTotalArea.Text = totalArea.ToString("F3");
            txtAvgPrice.Text = avgPrice.ToString("N2");
            txtTotalValue.Text = totalValue.ToString("N2");
        }

        private static double ParseOr(string text, double fallback)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : fallback;
        }
    }
}