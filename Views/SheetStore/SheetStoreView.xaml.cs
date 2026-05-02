using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
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
            SheetGrid.ItemsSource = sheets;
        }

        private void UpdateStats()
        {
            TotalSheetsText.Text = SheetStoreService.Instance.GetAllSheets().Count.ToString();
            ThicknessCountText.Text = SheetStoreService.Instance.GetAllThicknesses().Count.ToString();
            ColorsCountText.Text = SheetStoreService.Instance.GetAllColors().Count.ToString();

            var lastUpdate = SheetStoreService.Instance.GetAllSheets()
                .Where(s => s.LatestPurchaseDate > DateTime.MinValue)
                .OrderByDescending(s => s.LatestPurchaseDate)
                .FirstOrDefault();

            LastUpdateText.Text = lastUpdate?.LatestPurchaseDate.ToString("dd-MMM") ?? "None";
        }

        private void AddSheet_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SheetDialog(null);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                var result = SheetStoreService.Instance.AddSheet(dialog.Sheet);
                if (result.Success)
                {
                    LoadSheets();
                    UpdateStats();
                }
                else
                {
                    MessageBox.Show(result.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                        var result = SheetStoreService.Instance.UpdateSheet(dialog.Sheet);
                        if (result.Success)
                        {
                            LoadSheets();
                        }
                        else
                        {
                            MessageBox.Show(result.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        private void DeleteSheet_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string id)
            {
                var result = MessageBox.Show("Delete this sheet?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    var deleteResult = SheetStoreService.Instance.DeleteSheet(id);
                    if (deleteResult.Success)
                    {
                        LoadSheets();
                        UpdateStats();
                    }
                    else
                    {
                        MessageBox.Show(deleteResult.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void UpdatePurchasePrice_Click(object sender, RoutedEventArgs e)
        {
            string? sheetId = null;
            if (sender is Button btn && btn.Tag is string tagId)
            {
                sheetId = tagId;
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

        private async void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                DefaultExt = "csv",
                FileName = $"sheets_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                var result = await SheetStoreService.Instance.ExportToExcelAsync(dialog.FileName);
                MessageBox.Show(result.Message, result.Success ? "Success" : "Error", MessageBoxButton.OK,
                    result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);
            }
        }

        private async void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                DefaultExt = "csv"
            };

            if (dialog.ShowDialog() == true)
            {
                var result = await SheetStoreService.Instance.ImportFromExcelAsync(dialog.FileName);
                MessageBox.Show(result.Message, result.Success ? "Success" : "Error", MessageBoxButton.OK,
                    result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

                if (result.Success)
                {
                    LoadSheets();
                    UpdateStats();
                }
            }
        }
    }
}