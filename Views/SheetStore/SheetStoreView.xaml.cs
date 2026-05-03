using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.Models;
using ProGlassAutomation.Services;

namespace ProGlassAutomation.Views.SheetStore
{
    public partial class SheetStoreView : UserControl
    {
        // ==================== PROPERTIES ====================
        public ObservableCollection<Sheet> AllSheets { get; private set; }
        public ObservableCollection<Sheet> FilteredSheets { get; private set; }
        public ObservableCollection<string> Categories { get; private set; }

        // ==================== CACHE TRACKING ====================
        private string _currentSearch = "";
        private string _currentCategory = "";

        // ═══════════════════════════════════════════════════════
        // FIX #1: CancellationToken instead of System.Timers.Timer
        // ═══════════════════════════════════════════════════════
        private CancellationTokenSource _debounceCts;

        // ==================== INIT ====================
        public SheetStoreView()
        {
            InitializeComponent();

            // Subscribe to service events with correct EventHandler signature
            SheetStoreService.Instance.DataChanged += OnDataChanged;

            LoadData();
        }

        // ═══════════════════════════════════════════════════════
        // FIX #7: EventHandler signature
        // ═══════════════════════════════════════════════════════
        private void OnDataChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(LoadData);
        }

        // ==================== CLEANUP ====================
        private void Cleanup()
        {
            SheetStoreService.Instance.DataChanged -= OnDataChanged;
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
        }

        // ==================== LOAD DATA ====================
        private void LoadData()
        {
            try
            {
                AllSheets = SheetStoreService.Instance.GetAllActive();
                Categories = SheetStoreService.Instance.GetCategories();
                ApplyFilters();
                UpdateStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading data: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== APPLY FILTERS ====================
        private void ApplyFilters()
        {
            if (AllSheets == null) return;

            var filtered = AllSheets.AsEnumerable();

            // Apply search filter - use prebuilt Search index
            if (!string.IsNullOrWhiteSpace(_currentSearch))
            {
                // ═══════════════════════════════════════════════════════
                // FIX #4: Use prebuilt search index
                // ═══════════════════════════════════════════════════════
                var searchResults = SheetStoreService.Instance.Search(_currentSearch);
                var searchSet = new HashSet<int>(searchResults.Select(s => s.Id));
                filtered = filtered.Where(s => searchSet.Contains(s.Id));
            }

            // Apply category filter - use prebuilt Category lookup
            if (!string.IsNullOrWhiteSpace(_currentCategory))
            {
                var categorySheets = SheetStoreService.Instance.GetByCategory(_currentCategory);
                var categorySet = new HashSet<int>(categorySheets.Select(s => s.Id));
                filtered = filtered.Where(s => categorySet.Contains(s.Id));
            }

            FilteredSheets = new ObservableCollection<Sheet>(filtered);
            SheetGrid.ItemsSource = FilteredSheets;
            UpdateStats();
        }

        // ==================== UPDATE STATS ====================
        private void UpdateStats()
        {
            if (FilteredSheets == null || AllSheets == null) return;

            TotalSheetsText.Text = FilteredSheets.Count.ToString();
            TotalStockText.Text = FilteredSheets.Sum(s => s.TotalStock).ToString();
            UsedSheetsText.Text = FilteredSheets.Sum(s => s.UsedSheets).ToString();
            BalanceSheetsText.Text = FilteredSheets.Sum(s => s.BalanceSheets).ToString();
            ShowingCountText.Text = FilteredSheets.Count.ToString();
            TotalEntriesText.Text = AllSheets.Count.ToString();
            TotalAllText.Text = AllSheets.Count.ToString();

            // O(1) lookup for last purchase
            var lastPurchase = FilteredSheets
                .Where(s => s.LatestPurchaseDate.HasValue)
                .OrderByDescending(s => s.LatestPurchaseDate)
                .FirstOrDefault();
            LastPurchaseText.Text = lastPurchase != null ? lastPurchase.Thickness + " " + lastPurchase.Color : "-";

            var lastUpdate = FilteredSheets.OrderByDescending(s => s.CreatedDate).FirstOrDefault();
            LastUpdateText.Text = lastUpdate?.DisplayDateTime ?? "-";
        }

        // ═══════════════════════════════════════════════════════
        // FIX #1: Task.Delay + CancellationToken debounce
        // ═══════════════════════════════════════════════════════
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentSearch = SearchBox.Text ?? "";
            ScheduleSearch();
        }

        private async void ScheduleSearch()
        {
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            try
            {
                await Task.Delay(300, _debounceCts.Token);
                ApplyFilters();
            }
            catch (TaskCanceledException) { }
        }

        // ==================== CATEGORY FILTER ====================
        private void CategoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentCategory = CategoryListBox.SelectedItem?.ToString() ?? "";
            ApplyFilters();
        }

        private void ClearCategoryFilter_Click(object sender, RoutedEventArgs e)
        {
            CategoryListBox.SelectedItem = null;
            _currentCategory = "";
            ApplyFilters();
        }

        // ==================== CRUD OPERATIONS ====================
        private void AddSheet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SheetDialog(null) { Owner = Window.GetWindow(this) };
                if (dialog.ShowDialog() == true)
                {
                    SheetStoreService.Instance.AddSheet(dialog.NewSheet);
                    // NO LoadData() needed - DataChanged event handles it!
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
                    // O(1) lookup
                    var sheet = SheetStoreService.Instance.GetById(id);
                    if (sheet != null)
                    {
                        var dialog = new SheetDialog(sheet) { Owner = Window.GetWindow(this) };
                        if (dialog.ShowDialog() == true)
                        {
                            SheetStoreService.Instance.UpdateSheet(dialog.NewSheet);
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
                    // O(1) lookup
                    var sheet = SheetStoreService.Instance.GetById(id);
                    if (sheet != null)
                    {
                        sheet.TotalStock++;
                        sheet.LatestPurchaseDate = DateTime.Now;
                        SheetStoreService.Instance.UpdateSheet(sheet);
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
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== OTHER ACTIONS ====================
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
                if (dialog.ShowDialog() == true)
                {
                    // DataChanged event will trigger refresh automatically
                }
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