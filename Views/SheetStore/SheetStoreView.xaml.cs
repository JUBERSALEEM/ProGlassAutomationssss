using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.Models;
using ProGlassAutomation.Services;

namespace ProGlassAutomation.Views.SheetStore
{
    public partial class SheetStoreView : UserControl
    {
        public ObservableCollection<Sheet> AllSheets { get; set; }
        public ObservableCollection<Sheet> FilteredSheets { get; set; }
        public ObservableCollection<string> Categories { get; set; }

        public SheetStoreView()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                AllSheets = SheetStoreService.Instance.GetAllActive();
                FilteredSheets = new ObservableCollection<Sheet>(AllSheets);
                Categories = SheetStoreService.Instance.GetCategories();
                SheetGrid.ItemsSource = FilteredSheets;
                CategoryListBox.ItemsSource = Categories;
                UpdateStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading data: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStats()
        {
            TotalSheetsText.Text = FilteredSheets.Count.ToString();
            TotalStockText.Text = FilteredSheets.Sum(s => s.TotalStock).ToString();
            UsedSheetsText.Text = FilteredSheets.Sum(s => s.UsedSheets).ToString();
            BalanceSheetsText.Text = FilteredSheets.Sum(s => s.BalanceSheets).ToString();
            ShowingCountText.Text = FilteredSheets.Count.ToString();
            TotalEntriesText.Text = AllSheets.Count.ToString();
            TotalAllText.Text = AllSheets.Count.ToString();

            var lastPurchase = FilteredSheets.Where(s => s.LatestPurchaseDate.HasValue).OrderByDescending(s => s.LatestPurchaseDate).FirstOrDefault();
            LastPurchaseText.Text = lastPurchase != null ? lastPurchase.Thickness + " " + lastPurchase.Color : "-";

            var lastUpdate = FilteredSheets.OrderByDescending(s => s.CreatedDate).FirstOrDefault();
            LastUpdateText.Text = lastUpdate?.DisplayDateTime ?? "-";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string search = SearchBox.Text?.ToLower() ?? "";
                FilteredSheets = string.IsNullOrWhiteSpace(search)
                    ? new ObservableCollection<Sheet>(AllSheets)
                    : new ObservableCollection<Sheet>(AllSheets.Where(s =>
                        s.Thickness.ToLower().Contains(search) ||
                        s.Color.ToLower().Contains(search) ||
                        s.Category.ToLower().Contains(search) ||
                        s.Supplier.ToLower().Contains(search)));
                SheetGrid.ItemsSource = FilteredSheets;
                UpdateStats();
            }
            catch { }
        }

        private void CategoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string cat = CategoryListBox.SelectedItem?.ToString() ?? "";
                if (string.IsNullOrEmpty(cat)) return;
                FilteredSheets = new ObservableCollection<Sheet>(AllSheets.Where(s => s.Category == cat));
                SheetGrid.ItemsSource = FilteredSheets;
                UpdateStats();
            }
            catch { }
        }

        private void ClearCategoryFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CategoryListBox.SelectedItem = null;
                FilteredSheets = new ObservableCollection<Sheet>(AllSheets);
                SheetGrid.ItemsSource = FilteredSheets;
                UpdateStats();
            }
            catch { }
        }

        private void AddSheet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SheetDialog(null) { Owner = Window.GetWindow(this) };
                if (dialog.ShowDialog() == true)
                {
                    SheetStoreService.Instance.AddSheet(dialog.NewSheet);
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditSheet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((sender as Button)?.Tag is int id)
                {
                    var sheet = SheetStoreService.Instance.GetById(id);
                    if (sheet != null)
                    {
                        var dialog = new SheetDialog(sheet) { Owner = Window.GetWindow(this) };
                        if (dialog.ShowDialog() == true)
                        {
                            SheetStoreService.Instance.UpdateSheet(dialog.NewSheet);
                            LoadData();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BuySheet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((sender as Button)?.Tag is int id)
                {
                    var sheet = SheetStoreService.Instance.GetById(id);
                    if (sheet != null)
                    {
                        sheet.TotalStock++;
                        sheet.LatestPurchaseDate = DateTime.Now;
                        SheetStoreService.Instance.UpdateSheet(sheet);
                        LoadData();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteSheet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((sender as Button)?.Tag is int id)
                {
                    var result = MessageBox.Show("Delete this sheet?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        SheetStoreService.Instance.DeleteSheet(id);
                        LoadData();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var historyDialog = new PriceHistoryDialog();
                historyDialog.Owner = Window.GetWindow(this);
                historyDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening history: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdatePurchasePrice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new PurchasePriceDialog { Owner = Window.GetWindow(this) };
                if (dialog.ShowDialog() == true) LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "CSV|*.csv", FileName = "SheetInventory_" + DateTime.Now.ToString("yyyyMMdd") };
                if (dialog.ShowDialog() == true)
                {
                    SheetStoreService.Instance.ExportToExcel(dialog.FileName);
                    MessageBox.Show("Export completed!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "CSV|*.csv" };
                if (dialog.ShowDialog() == true)
                {
                    int count = SheetStoreService.Instance.ImportFromExcel(dialog.FileName);
                    LoadData();
                    MessageBox.Show($"Imported {count} sheets!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PreviousPage_Click(object sender, RoutedEventArgs e) { }
        private void NextPage_Click(object sender, RoutedEventArgs e) { }
    }
}