using System;
using System.Collections.Generic;
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

            for (int i = 0; i < CategoryComboBox.Items.Count; i++)
            {
                if (CategoryComboBox.Items[i] is ComboBoxItem item &&
                    item.Content?.ToString() == Sheet.Category)
                {
                    CategoryComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        private List<string> ValidateForm()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ThicknessText.Text))
                errors.Add("• Thickness is required");

            if (string.IsNullOrWhiteSpace(ColorText.Text))
                errors.Add("• Color is required");

            if (CategoryComboBox.SelectedItem == null)
                errors.Add("• Series is required");

            if (!decimal.TryParse(WidthText.Text, out decimal width) || width <= 0)
                errors.Add("• Invalid width");

            if (!decimal.TryParse(HeightText.Text, out decimal height) || height <= 0)
                errors.Add("• Invalid height");

            if (!decimal.TryParse(PurchasePriceText.Text, out decimal purchasePrice) || purchasePrice < 0)
                errors.Add("• Invalid purchase price");

            if (!decimal.TryParse(SellPriceText.Text, out decimal sellPrice) || sellPrice < 0)
                errors.Add("• Invalid sell price");

            return errors;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var errors = ValidateForm();
            if (errors.Count > 0)
            {
                MessageBox.Show(string.Join("\n", errors), "Validation Errors",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string category = "";
                if (CategoryComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    category = selectedItem.Content?.ToString() ?? "";
                }

                decimal width = decimal.Parse(WidthText.Text.Trim());
                decimal height = decimal.Parse(HeightText.Text.Trim());
                decimal purchasePrice = decimal.Parse(PurchasePriceText.Text.Trim());
                decimal sellPrice = decimal.Parse(SellPriceText.Text.Trim());

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

                (bool Success, string Message) result;

                if (_isEdit)
                {
                    result = SheetStoreService.Instance.UpdateSheet(Sheet);
                }
                else
                {
                    result = SheetStoreService.Instance.AddSheet(Sheet);
                }

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