using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.Models;
using ProGlassAutomation.Services;

namespace ProGlassAutomation.Views.SheetStore
{
    public partial class SheetStoreView : UserControl
    {
        public SheetStoreView()
        {
            InitializeComponent();
            LoadSheets();
            UpdateStats();
        }

        private void LoadSheets()
        {
            var sheets = SheetStoreService.Instance.GetAllSheets();
            SheetItemsControl.ItemsSource = sheets;
        }

        private void UpdateStats()
        {
            var sheets = SheetStoreService.Instance.GetAllSheets();

            TotalSheetsText.Text = sheets.Count.ToString();
            CategoriesCountText.Text = SheetStoreService.Instance.GetAllCategories().Count.ToString();
            ThicknessCountText.Text = SheetStoreService.Instance.GetAllThicknesses().Count.ToString();
            ColorsCountText.Text = SheetStoreService.Instance.GetAllColors().Count.ToString();

            // Find the most recent purchase
            var lastPurchase = sheets
                .Where(s => s.LastPurchaseDate > DateTime.MinValue)
                .OrderByDescending(s => s.LastPurchaseDate)
                .ThenByDescending(s => s.LastPurchaseTime)
                .FirstOrDefault();

            if (lastPurchase != null && lastPurchase.LastPurchaseDate > DateTime.MinValue)
            {
                LastUpdateText.Text = $"{lastPurchase.LastPurchaseDate:dd-MMM}\n{lastPurchase.LastPurchaseTime:HH:mm}";
            }
            else
            {
                LastUpdateText.Text = "-";
            }
        }

        private void AddSheet_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SheetDialog(null);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                LoadSheets();
                UpdateStats();
            }
        }

        private void EditSheet_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string id)
            {
                var sheet = SheetStoreService.Instance.GetSheet(id);
                if (sheet != null)
                {
                    var dialog = new SheetDialog(sheet);
                    dialog.Owner = Window.GetWindow(this);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadSheets();
                    }
                }
            }
        }

        private void DeleteSheet_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string id)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to delete this sheet?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (SheetStoreService.Instance.DeleteSheet(id))
                    {
                        LoadSheets();
                        UpdateStats();
                        MessageBox.Show("Sheet deleted successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void UpdatePurchasePrice_Click(object sender, RoutedEventArgs e)
        {
            string? sheetId = null;
            if (sender is Button btn)
            {
                sheetId = btn.Tag as string;
            }

            var dialog = new PurchasePriceDialog(sheetId);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                LoadSheets();
                UpdateStats();
            }
        }

        private void ViewHistory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PriceHistoryDialog();
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export - Coming soon!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Import - Coming soon!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}