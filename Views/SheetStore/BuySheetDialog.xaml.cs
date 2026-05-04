using System;
using System.Windows;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Views.SheetStore
{
    public partial class BuySheetDialog : Window
    {
        public BuySheetViewModel ViewModel { get; }

        public BuySheetDialog(Sheet sheet)
        {
            InitializeComponent();
            ViewModel = new BuySheetViewModel(sheet);
            DataContext = ViewModel;
            QtyTextBox.Focus();
        }

        private void Purchase_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Validate())
            {
                DialogResult = true;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class BuySheetViewModel
    {
        private readonly Sheet _sheet;

        // Display properties
        public string SheetInfo => $"{_sheet.Category} | {_sheet.Thickness} | {_sheet.Color}";
        public int CurrentStock => _sheet.TotalStock;
        public int UsedSheets => _sheet.UsedSheets;
        public int BalanceSheets => _sheet.TotalStock - _sheet.UsedSheets;

        // Input properties
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public string Supplier { get; set; } = "";
        public DateTime PurchasedOn { get; set; } = DateTime.Now;
        public string Notes { get; set; } = "";

        // Supplier list
        public string[] SupplierList => new[]
        {
            "Al Fanous Glass",
            "Al Rashid Glass",
            "Emirates Glass",
            "Saudi Glass Industries",
            "Bahrain Glass",
            "Qatar Glass",
            "Local Supplier",
            "Other"
        };

        public BuySheetViewModel(Sheet sheet)
        {
            _sheet = sheet;
            UnitPrice = sheet.PurchasePrice;
        }

        public bool Validate()
        {
            if (Quantity <= 0)
            {
                MessageBox.Show("Please enter a valid quantity (minimum 1)!",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (UnitPrice <= 0)
            {
                MessageBox.Show("Please enter a valid purchase price!",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public SheetPurchase ToPurchaseRecord()
        {
            return new SheetPurchase
            {
                SheetId = _sheet.Id,
                Quantity = Quantity,
                UnitPrice = UnitPrice,
                Supplier = Supplier ?? "",
                PurchasedOn = PurchasedOn,
                Notes = Notes ?? "",
                CreatedAt = DateTime.Now
            };
        }
    }
}