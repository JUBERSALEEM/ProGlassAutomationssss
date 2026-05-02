using System;
using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.Models;
using ProGlassAutomation.Services;

namespace ProGlassAutomation.Views.SheetStore
{
    public partial class PurchasePriceDialog : Window
    {
        private string? _preselectedId;

        public PurchasePriceDialog(string? sheetId = null)
        {
            InitializeComponent();
            _preselectedId = sheetId;
            LoadSheets();
        }

        private void LoadSheets()
        {
            var sheets = SheetStoreService.Instance.GetAllSheets();
            SheetComboBox.ItemsSource = sheets;
            SheetComboBox.SelectedIndex = 0;

            if (!string.IsNullOrEmpty(_preselectedId))
            {
                for (int i = 0; i < sheets.Count; i++)
                {
                    if (sheets[i].Id == _preselectedId)
                    {
                        SheetComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void SheetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SheetComboBox.SelectedItem is Sheet sheet)
            {
                PurchasePriceTextBox.Text = sheet.PurchasePrice.ToString("F2");
                SellPriceTextBox.Text = sheet.SellPrice.ToString("F2");
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SheetComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Select a sheet", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(PurchasePriceTextBox.Text, out decimal purchasePrice) || purchasePrice < 0)
                {
                    MessageBox.Show("Enter valid purchase price", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    PurchasePriceTextBox.Focus();
                    return;
                }

                if (!decimal.TryParse(SellPriceTextBox.Text, out decimal sellPrice) || sellPrice <= 0)
                {
                    MessageBox.Show("Enter valid sell price", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    SellPriceTextBox.Focus();
                    return;
                }

                var sheet = SheetComboBox.SelectedItem as Sheet;
                if (sheet == null) return;

                var result = SheetStoreService.Instance.UpdateBothPrices(
                    sheet.Id,
                    purchasePrice,
                    sellPrice,
                    SupplierTextBox.Text.Trim(),
                    NotesTextBox.Text.Trim()
                );

                if (result.Success)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show(result.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}