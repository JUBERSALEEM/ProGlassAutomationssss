using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.Models;
using ProGlassAutomation.Services;

namespace ProGlassAutomation.Views.SheetStore
{
    public partial class PurchasePriceDialog : Window
    {
        private string _selectedCategory = "All Categories";

        public PurchasePriceDialog()
        {
            InitializeComponent();
            LoadCategories();
            UpdatePreview();
        }

        private void LoadCategories()
        {
            CategoryFilterCombo.Items.Clear();
            CategoryFilterCombo.Items.Add(new ComboBoxItem { Content = "All Categories", IsSelected = true });

            foreach (string cat in Sheet.Categories)
                CategoryFilterCombo.Items.Add(new ComboBoxItem { Content = cat });

            CategoryFilterCombo.SelectedIndex = 0;
        }

        private int GetFilteredCount()
        {
            var allSheets = SheetStoreService.Instance.GetAllActive().ToList();
            if (_selectedCategory == "All Categories")
                return allSheets.Count;
            return allSheets.Count(s => s.Category == _selectedCategory);
        }

        private void UpdatePreview()
        {
            ItemsCountText.Text = GetFilteredCount().ToString();

            if (decimal.TryParse(NewPurchasePriceText.Text, out decimal price))
                PreviewPriceText.Text = "AED " + price.ToString("N2");
            else
                PreviewPriceText.Text = "AED 0.00";
        }

        private void CategoryFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = CategoryFilterCombo.SelectedItem as ComboBoxItem;
            _selectedCategory = item?.Content?.ToString() ?? "All Categories";
            UpdatePreview();
        }

        private void DecreasePercent_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(PercentText.Text, out int percent) && percent > 0)
            {
                PercentText.Text = (percent - 5).ToString();
                ApplyPercentage();
            }
        }

        private void IncreasePercent_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(PercentText.Text, out int percent))
            {
                PercentText.Text = (percent + 5).ToString();
                ApplyPercentage();
            }
        }

        private void ApplyPercentage()
        {
            if (!int.TryParse(PercentText.Text, out int percent)) return;

            var sheets = SheetStoreService.Instance.GetAllActive().ToList();
            if (sheets.Count == 0) return;

            var filtered = _selectedCategory == "All Categories"
                ? sheets
                : sheets.Where(s => s.Category == _selectedCategory).ToList();

            if (filtered.Count == 0) return;

            decimal avgPrice = filtered.Average(s => s.PurchasePrice);
            decimal newPrice = avgPrice + (avgPrice * percent / 100);
            NewPurchasePriceText.Text = newPrice.ToString("N2");
            UpdatePreview();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!decimal.TryParse(NewPurchasePriceText.Text, out decimal newPrice))
                {
                    MessageBox.Show("Please enter a valid price!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var allSheets = SheetStoreService.Instance.GetAllActive().ToList();

                var sheetsToUpdate = _selectedCategory == "All Categories"
                    ? allSheets
                    : allSheets.Where(s => s.Category == _selectedCategory).ToList();

                if (sheetsToUpdate.Count == 0)
                {
                    MessageBox.Show("No sheets found to update!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string unit = (PriceUnitCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Sheet";
                int updated = 0;

                foreach (var sheet in sheetsToUpdate)
                {
                    decimal finalPrice = newPrice;

                    if (unit == "SQM" && sheet.SquareMeter > 0)
                        finalPrice = newPrice * (decimal)sheet.SquareMeter;
                    else if (unit == "Sqft" && sheet.SquareMeter > 0)
                        finalPrice = newPrice * (decimal)(sheet.SquareMeter * 10.764);

                    sheet.PurchasePrice = Math.Round(finalPrice, 2);
                    SheetStoreService.Instance.UpdateSheet(sheet);
                    updated++;
                }

                MessageBox.Show($"Successfully updated {updated} sheets!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}