using System;
using System.Windows;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Views.SheetStore
{
    public partial class BuySheetDialog : Window
    {
        private readonly Sheet _sheet;

        public int Quantity { get; private set; }
        public decimal UnitPrice { get; private set; }
        public string Supplier { get; private set; }
        public DateTime PurchasedOn { get; private set; }
        public string Notes { get; private set; }

        public BuySheetDialog(Sheet sheet)
        {
            InitializeComponent();
            _sheet = sheet;

            // Set current date
            DatePicker.SelectedDate = DateTime.Now;

            // Set current hour
            for (int i = 0; i < 24; i++)
            {
                HourCombo.Items.Add(i.ToString("D2") + ":00");
                HourCombo.Items.Add(i.ToString("D2") + ":30");
            }
            HourCombo.SelectedIndex = DateTime.Now.Hour * 2;

            PriceTextBox.Text = sheet.PurchasePrice.ToString();
            QtyTextBox.Focus();
        }

        private void Purchase_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(QtyTextBox.Text, out int qty) || qty <= 0)
            {
                MessageBox.Show("Please enter a valid quantity!", "Error");
                return;
            }

            if (!decimal.TryParse(PriceTextBox.Text, out decimal price) || price <= 0)
            {
                MessageBox.Show("Please enter a valid price!", "Error");
                return;
            }

            Quantity = qty;
            UnitPrice = price;
            Supplier = SupplierTextBox.Text ?? "";
            Notes = NotesTextBox.Text ?? "";

            // Combine date + time
            DateTime selectedDate = DatePicker.SelectedDate ?? DateTime.Now;
            string selectedHour = HourCombo.SelectedItem?.ToString() ?? "00:00";
            string[] parts = selectedHour.Split(':');
            int hour = int.Parse(parts[0]);
            int minute = int.Parse(parts[1]);
            PurchasedOn = new DateTime(selectedDate.Year, selectedDate.Month, selectedDate.Day, hour, minute, 0);

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}