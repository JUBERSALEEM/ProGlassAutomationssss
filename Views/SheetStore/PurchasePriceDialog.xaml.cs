using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.Models;
using ProGlassAutomation.Services;

namespace ProGlassAutomation.Views.SheetStore
{
    public partial class PurchasePriceDialog : Window
    {
        public ObservableCollection<SheetItem> Sheets { get; set; }
        private Sheet? _selectedSheet;
        private string? _selectedSheetId;

        public PurchasePriceDialog(string? sheetId = null)
        {
            InitializeComponent();

            Sheets = new ObservableCollection<SheetItem>();
            _selectedSheetId = sheetId;

            var now = DateTime.Now;
            CurrentDateTimeText.Text = $"📅 {now:dd-MMM-yyyy}  🕐 {now:HH:mm}";
            TimeHintText.Text = $"Current: {now:HH:mm:ss}";

            LoadSheets();

            if (!string.IsNullOrEmpty(_selectedSheetId))
            {
                foreach (var item in Sheets)
                {
                    if (item.Id == _selectedSheetId)
                    {
                        SheetComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void LoadSheets()
        {
            Sheets.Clear();
            var sheets = SheetStoreService.Instance.GetAllSheets();
            foreach (var sheet in sheets)
            {
                Sheets.Add(new SheetItem
                {
                    Id = sheet.Id,
                    DisplayName = $"{sheet.Category} - {sheet.Thickness} {sheet.Color}",
                    PurchasePrice = sheet.PurchasePrice,
                    SellPrice = sheet.SellPrice,
                    SupplierName = sheet.SupplierName
                });
            }
            SheetComboBox.ItemsSource = Sheets;
        }

        private void SheetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SheetComboBox.SelectedItem is SheetItem selectedItem)
            {
                _selectedSheetId = selectedItem.Id;
                _selectedSheet = SheetStoreService.Instance.GetSheet(selectedItem.Id);

                if (_selectedSheet != null)
                {
                    SheetInfoText.Text = $"{_selectedSheet.Category} - {_selectedSheet.Thickness} {_selectedSheet.Color}";
                    PurchasePriceText.Text = _selectedSheet.PurchasePrice.ToString("F2");
                    SellPriceText.Text = _selectedSheet.SellPrice.ToString("F2");
                    SupplierText.Text = _selectedSheet.SupplierName;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_selectedSheetId) || _selectedSheet == null)
                {
                    MessageBox.Show("Please select a sheet first", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    SheetComboBox.Focus();
                    return;
                }

                if (!decimal.TryParse(PurchasePriceText.Text, out decimal purchasePrice) || purchasePrice <= 0)
                {
                    MessageBox.Show("Please enter a valid purchase price", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    PurchasePriceText.Focus();
                    return;
                }

                if (!decimal.TryParse(SellPriceText.Text, out decimal sellPrice) || sellPrice <= 0)
                {
                    MessageBox.Show("Please enter a valid sell price", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    SellPriceText.Focus();
                    return;
                }

                string supplier = SupplierText.Text.Trim();
                string notes = NotesText.Text.Trim();

                bool success = SheetStoreService.Instance.UpdateBothPrices(_selectedSheetId, purchasePrice, sellPrice, supplier, notes);

                if (success)
                {
                    var now = DateTime.Now;
                    MessageBox.Show(
                        $"✅ Purchase Recorded Successfully!\n\n" +
                        $"📅 Date: {now:dd-MMM-yyyy}\n" +
                        $"🕐 Time: {now:HH:mm:ss}\n" +
                        $"💵 Amount: AED {purchasePrice:N2}",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Failed to save price. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    // SheetItem class - unique to this file
    public class SheetItem
    {
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public decimal PurchasePrice { get; set; }
        public decimal SellPrice { get; set; }
        public string SupplierName { get; set; } = "";
    }
}