using System;
using System.Windows;
using System.Windows.Controls;

namespace ProGlassAutomation.Views.SheetStore
{
    public partial class UseSheetsDialog : Window
    {
        private readonly int _availableBalance;

        public UseSheetsDialog(int availableBalance)
        {
            InitializeComponent();
            _availableBalance = availableBalance;

            // ✅ Set balance display
            BalanceText.Text = availableBalance.ToString();

            // Set default date
            DatePicker.SelectedDate = DateTime.Today;

            // Initialize hour combo
            for (int i = 0; i < 24; i++)
            {
                HourCombo.Items.Add(i.ToString("00"));
            }
            HourCombo.SelectedIndex = DateTime.Now.Hour;
        }

        // ✅ Public properties for ViewModel to access
        public int Quantity => int.TryParse(QtyTextBox.Text, out int qty) ? qty : 0;
        public string Reason => ReasonTextBox.Text.Trim();
        public DateTime UsedOn
        {
            get
            {
                var date = DatePicker.SelectedDate ?? DateTime.Today;
                var hour = int.TryParse(HourCombo.SelectedItem?.ToString(), out int h) ? h : 0;
                return date.Date.AddHours(hour);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Use_Click(object sender, RoutedEventArgs e)
        {
            // Validate quantity
            if (!int.TryParse(QtyTextBox.Text, out int qty) || qty <= 0)
            {
                MessageBox.Show("Please enter a valid quantity.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check balance
            if (qty > _availableBalance)
            {
                MessageBox.Show($"Not enough sheets. Available: {_availableBalance}",
                    "Insufficient Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate reason
            if (string.IsNullOrWhiteSpace(ReasonTextBox.Text))
            {
                MessageBox.Show("Please enter a reason.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ✅ Success
            DialogResult = true;
            Close();
        }
    }
}