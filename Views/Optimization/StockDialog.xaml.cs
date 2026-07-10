using System;
using System.Globalization;
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
            dgStock.ItemsSource = _vm.StockSheets;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(txtWidth.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double w) || w <= 0)
            {
                MessageBox.Show("Enter valid width.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!double.TryParse(txtHeight.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double h) || h <= 0)
            {
                MessageBox.Show("Enter valid height.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(txtQty.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int qty) || qty < 0)
            {
                MessageBox.Show("Enter valid quantity.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!double.TryParse(txtPrice.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double price) || price < 0)
            {
                MessageBox.Show("Enter valid price.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_editing == null)
            {
                _vm.StockSheets.Add(new StockSheetViewModel
                {
                    Index = _vm.StockSheets.Count + 1,
                    L = w,
                    W = h,
                    Qty = qty,
                    PricePerM2 = price
                });
            }
            else
            {
                _editing.L = w;
                _editing.W = h;
                _editing.Qty = qty;
                _editing.PricePerM2 = price;
                _editing = null;
                btnAdd.Content = "+ Add Stock";
            }

            ClearForm();
            Reindex();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is StockSheetViewModel stock)
            {
                _editing = stock;
                txtWidth.Text = stock.L.ToString();
                txtHeight.Text = stock.W.ToString();
                txtQty.Text = stock.Qty.ToString();
                txtPrice.Text = stock.PricePerM2.ToString();
                btnAdd.Content = "✓ Update";
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is StockSheetViewModel stock)
            {
                var r = MessageBox.Show($"Delete stock sheet #{stock.Index}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    _vm.StockSheets.Remove(stock);
                    Reindex();
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void ClearForm()
        {
            txtWidth.Text = "3210";
            txtHeight.Text = "2250";
            txtQty.Text = "9999";
            txtPrice.Text = "1559.86";
        }

        private void Reindex()
        {
            for (int i = 0; i < _vm.StockSheets.Count; i++)
            {
                if (_vm.StockSheets[i].Index != i + 1)
                    _vm.StockSheets[i].Index = i + 1;
            }
        }
    }
}