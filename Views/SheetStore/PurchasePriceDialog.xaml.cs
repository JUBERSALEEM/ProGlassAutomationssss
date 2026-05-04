using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        // ═══════════════════════════════════════════════════════
        // CACHE PARSED NUMERIC VALUES
        // ═══════════════════════════════════════════════════════
        private decimal _cachedPrice;
        private int _cachedPercent;
        private string _lastPriceText = "";
        private string _lastPercentText = "";

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

            var categories = SheetStoreService.Instance.GetCategories();
            foreach (string cat in categories)
                CategoryFilterCombo.Items.Add(new ComboBoxItem { Content = cat });

            CategoryFilterCombo.SelectedIndex = 0;
        }

        // ═══════════════════════════════════════════════════════
        // GET FILTERED COUNT
        // ═══════════════════════════════════════════════════════
        private int GetFilteredCount()
        {
            if (_selectedCategory == "All Categories")
            {
                return SheetStoreService.Instance.GetAllActive().Count;
            }

            return SheetStoreService.Instance.GetByCategory(_selectedCategory).Count;
        }

        // ═══════════════════════════════════════════════════════
        // UPDATE PREVIEW
        // ═══════════════════════════════════════════════════════
        private void UpdatePreview()
        {
            ItemsCountText.Text = GetFilteredCount().ToString();

            // Parse price once
            if (NewPurchasePriceText.Text != _lastPriceText)
            {
                _lastPriceText = NewPurchasePriceText.Text;
                decimal.TryParse(NewPurchasePriceText.Text, out _cachedPrice);
            }

            PreviewPriceText.Text = "AED " + _cachedPrice.ToString("N2");
        }

        private void CategoryFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = CategoryFilterCombo.SelectedItem as ComboBoxItem;
            _selectedCategory = item?.Content?.ToString() ?? "All Categories";
            UpdatePreview();
        }

        private void DecreasePercent_Click(object sender, RoutedEventArgs e)
        {
            // Parse percent once
            if (PercentText.Text != _lastPercentText)
            {
                _lastPercentText = PercentText.Text;
                int.TryParse(PercentText.Text, out _cachedPercent);
            }

            if (_cachedPercent > 0)
            {
                _cachedPercent -= 5;
                PercentText.Text = _cachedPercent.ToString();
                ApplyPercentage();
            }
        }

        private void IncreasePercent_Click(object sender, RoutedEventArgs e)
        {
            // Parse percent once
            if (PercentText.Text != _lastPercentText)
            {
                _lastPercentText = PercentText.Text;
                int.TryParse(PercentText.Text, out _cachedPercent);
            }

            _cachedPercent += 5;
            PercentText.Text = _cachedPercent.ToString();
            ApplyPercentage();
        }

        private void ApplyPercentage()
        {
            if (_cachedPercent == 0) return;

            var sheets = _selectedCategory == "All Categories"
                ? SheetStoreService.Instance.GetAllActive()
                : SheetStoreService.Instance.GetByCategory(_selectedCategory);

            if (sheets.Count == 0) return;

            decimal avgPrice = (decimal)sheets.Average(s => s.PurchasePrice);
            decimal newPrice = avgPrice + (avgPrice * _cachedPercent / 100);
            NewPurchasePriceText.Text = Math.Round(newPrice, 2).ToString("N2");
            UpdatePreview();
        }

        // ═══════════════════════════════════════════════════════
        // FIX: Use UpdateSheet for each sheet individually
        // ═══════════════════════════════════════════════════════
        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Parse price once
                if (NewPurchasePriceText.Text != _lastPriceText)
                {
                    _lastPriceText = NewPurchasePriceText.Text;
                    decimal.TryParse(NewPurchasePriceText.Text, out _cachedPrice);
                }

                if (_cachedPrice <= 0)
                {
                    MessageBox.Show("Please enter a valid price!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var sheetsToUpdate = _selectedCategory == "All Categories"
                    ? SheetStoreService.Instance.GetAllActive()
                    : SheetStoreService.Instance.GetByCategory(_selectedCategory);

                if (sheetsToUpdate.Count == 0)
                {
                    MessageBox.Show("No sheets found to update!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string unit = (PriceUnitCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Sheet";
                int updated = 0;

                foreach (var sheet in sheetsToUpdate)
                {
                    decimal finalPrice = _cachedPrice;

                    if (unit == "SQM" && sheet.SquareMeter > 0)
                        finalPrice = _cachedPrice * (decimal)sheet.SquareMeter;
                    else if (unit == "Sqft" && sheet.SquareMeter > 0)
                        finalPrice = _cachedPrice * (decimal)(sheet.SquareMeter * 10.764);

                    sheet.PurchasePrice = Math.Round(finalPrice, 2);

                    // ═══════════════════════════════════════════════════════
                    // FIX: Update each sheet individually instead of BulkUpdatePrices
                    // ═══════════════════════════════════════════════════════
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