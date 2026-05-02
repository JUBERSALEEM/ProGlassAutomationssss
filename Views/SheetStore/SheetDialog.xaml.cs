using System;
using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.Models;
using ProGlassAutomation.Services;

namespace ProGlassAutomation.Views.SheetStore
{
    public partial class SheetDialog : Window
    {
        public Sheet Sheet { get; private set; }
        private readonly bool _isEdit;

        public SheetDialog(Sheet? sheet = null)
        {
            InitializeComponent();

            _isEdit = sheet != null;
            Sheet = sheet ?? new Sheet();

            if (_isEdit)
            {
                HeaderText.Text = "EDIT SHEET";
                LoadSheetData();
            }
            else
            {
                HeaderText.Text = "ADD NEW SHEET";
            }

            DataContext = this;
        }

        private void LoadSheetData()
        {
            ThicknessText.Text = Sheet.Thickness;
            ColorText.Text = Sheet.Color;
            WidthText.Text = Sheet.Width.ToString();
            HeightText.Text = Sheet.Height.ToString();
            PurchasePriceText.Text = Sheet.PurchasePrice.ToString("F2");
            SellPriceText.Text = Sheet.SellPrice.ToString("F2");
            SupplierText.Text = Sheet.SupplierName;
            DescriptionText.Text = Sheet.Description;

            // Set category combo box
            for (int i = 0; i < CategoryComboBox.Items.Count; i++)
            {
                if (CategoryComboBox.Items[i] is ComboBoxItem item && item.Content.ToString() == Sheet.Category)
                {
                    CategoryComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(ThicknessText.Text))
                {
                    MessageBox.Show("Thickness is required", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ThicknessText.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(ColorText.Text))
                {
                    MessageBox.Show("Color is required", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ColorText.Focus();
                    return;
                }

                if (CategoryComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Series is required", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(WidthText.Text, out decimal width))
                {
                    MessageBox.Show("Invalid width", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    WidthText.Focus();
                    return;
                }

                if (!decimal.TryParse(HeightText.Text, out decimal height))
                {
                    MessageBox.Show("Invalid height", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    HeightText.Focus();
                    return;
                }

                if (!decimal.TryParse(PurchasePriceText.Text, out decimal purchasePrice))
                {
                    MessageBox.Show("Invalid purchase price", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    PurchasePriceText.Focus();
                    return;
                }

                if (!decimal.TryParse(SellPriceText.Text, out decimal sellPrice))
                {
                    MessageBox.Show("Invalid sell price", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    SellPriceText.Focus();
                    return;
                }

                // Get category from combo box
                string category = "";
                if (CategoryComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    category = selectedItem.Content.ToString() ?? "";
                }

                Sheet.Thickness = ThicknessText.Text.Trim();
                Sheet.Color = ColorText.Text.Trim();
                Sheet.Category = category;
                Sheet.Width = width;
                Sheet.Height = height;
                Sheet.PurchasePrice = purchasePrice;
                Sheet.SellPrice = sellPrice;
                Sheet.SupplierName = SupplierText.Text.Trim();
                Sheet.Description = DescriptionText.Text.Trim();
                Sheet.PricePerSqft = sellPrice;
                Sheet.PricePerSqmeter = sellPrice * 10.764m;

                bool success;
                if (_isEdit)
                {
                    success = SheetStoreService.Instance.UpdateSheet(Sheet);
                }
                else
                {
                    success = SheetStoreService.Instance.AddSheet(Sheet);
                }

                if (success)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Failed to save sheet", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
}