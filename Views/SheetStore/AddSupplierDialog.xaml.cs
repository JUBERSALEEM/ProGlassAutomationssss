using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Views.SheetStore
{
    public partial class AddSupplierDialog : Window
    {
        public string SupplierName => SupplierTextBox.Text.Trim();
        private bool _isDuplicate = false;

        public AddSupplierDialog()
        {
            InitializeComponent();

            // Load existing suppliers
            ExistingSuppliersList.ItemsSource = Sheet.Suppliers.OrderBy(s => s);

            SupplierTextBox.Focus();
        }

        private void SupplierTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string input = SupplierTextBox.Text.Trim();

            if (string.IsNullOrEmpty(input))
            {
                DuplicateWarning.Visibility = Visibility.Collapsed;
                AddButton.IsEnabled = false;
                _isDuplicate = false;
                return;
            }

            // Check for duplicate
            bool exists = Sheet.SupplierExists(input);

            if (exists)
            {
                DuplicateWarning.Visibility = Visibility.Visible;
                AddButton.IsEnabled = false;
                AddButton.Content = "Already Exists";
                _isDuplicate = true;
            }
            else
            {
                DuplicateWarning.Visibility = Visibility.Collapsed;
                AddButton.IsEnabled = true;
                AddButton.Content = "Add Supplier";
                _isDuplicate = false;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SupplierTextBox.Text))
            {
                MessageBox.Show("Please enter a supplier name!", "Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_isDuplicate)
            {
                MessageBox.Show("This supplier already exists!", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}