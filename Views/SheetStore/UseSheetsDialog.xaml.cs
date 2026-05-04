using System;
using System.Windows;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Views.SheetStore
{
    public partial class UseSheetsDialog : Window
    {
        public UseSheetsViewModel ViewModel { get; }

        public UseSheetsDialog(Sheet sheet)
        {
            InitializeComponent();
            ViewModel = new UseSheetsViewModel(sheet);
            DataContext = ViewModel;
            QtyTextBox.Focus();
        }

        private void Use_Click(object sender, RoutedEventArgs e)
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

    public class UseSheetsViewModel
    {
        private readonly Sheet _sheet;

        // Display properties
        public string SheetInfo => $"{_sheet.Category} | {_sheet.Thickness} | {_sheet.Color}";
        public int BalanceSheets => _sheet.TotalStock - _sheet.UsedSheets;

        // Input properties
        public int Quantity { get; set; } = 1;
        public string Reason { get; set; } = "";
        public DateTime UsedOn { get; set; } = DateTime.Now;

        // Common reasons
        public string[] CommonReasons => new[]
        {
            "Used for project",
            "Used for DGU Lamination",
            "Used for SGU",
            "Used for mirror cutting",
            "Damaged/Broken",
            "Returned to supplier",
            "Sample/Display",
            "Other"
        };

        public UseSheetsViewModel(Sheet sheet)
        {
            _sheet = sheet;
        }

        public bool Validate()
        {
            if (Quantity <= 0)
            {
                MessageBox.Show("Please enter a valid quantity (minimum 1)!",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (Quantity > BalanceSheets)
            {
                MessageBox.Show($"Only {BalanceSheets} sheets available!\n\nPlease enter a smaller quantity.",
                    "Insufficient Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(Reason))
            {
                var result = MessageBox.Show(
                    "Are you sure you want to use sheets without specifying a reason?",
                    "No Reason Provided",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return false;
            }

            return true;
        }

        public SheetUsage ToUsageRecord()
        {
            return new SheetUsage
            {
                SheetId = _sheet.Id,
                Quantity = Quantity,
                Reason = Reason ?? "",
                UsedOn = UsedOn,
                CreatedAt = DateTime.Now
            };
        }
    }
}