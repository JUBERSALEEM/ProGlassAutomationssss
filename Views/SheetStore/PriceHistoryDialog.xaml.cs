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
    public partial class PriceHistoryDialog : Window
    {
        // Combined history items for display
        public ObservableCollection<HistoryItem> AllHistoryItems { get; set; }
        public ObservableCollection<HistoryItem> FilteredHistoryItems { get; set; }

        private string _currentCategory = "";
        private string _currentDateRange = "";
        private string _currentSearch = "";
        private string _currentHistoryType = "All";

        private CancellationTokenSource _debounceCts;

        public PriceHistoryDialog()
        {
            InitializeComponent();

            AllHistoryItems = new ObservableCollection<HistoryItem>();
            FilteredHistoryItems = new ObservableCollection<HistoryItem>();

            SheetStoreService.Instance.DataChanged += OnDataChanged;

            InitializeFilters();
            LoadData();
        }

        private void OnDataChanged(object sender, EventArgs e)
        {
            if (this.IsActive || this.IsVisible)
            {
                Dispatcher.Invoke(LoadData);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            SheetStoreService.Instance.DataChanged -= OnDataChanged;
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
        }

        private void InitializeFilters()
        {
            try
            {
                // Category filter
                CategoryFilterCombo.Items.Clear();
                CategoryFilterCombo.Items.Add(new ComboBoxItem { Content = "All Categories", IsSelected = true });
                var categories = SheetStoreService.Instance.GetCategories();
                foreach (string cat in categories)
                {
                    CategoryFilterCombo.Items.Add(new ComboBoxItem { Content = cat });
                }
                CategoryFilterCombo.SelectedIndex = 0;

                // Date filter
                DateFilterCombo.Items.Clear();
                DateFilterCombo.Items.Add(new ComboBoxItem { Content = "All Time", IsSelected = true });
                DateFilterCombo.Items.Add(new ComboBoxItem { Content = "Today" });
                DateFilterCombo.Items.Add(new ComboBoxItem { Content = "This Week" });
                DateFilterCombo.Items.Add(new ComboBoxItem { Content = "This Month" });
                DateFilterCombo.Items.Add(new ComboBoxItem { Content = "This Year" });
                DateFilterCombo.SelectedIndex = 0;

                // History type filter
                HistoryTypeCombo.Items.Clear();
                HistoryTypeCombo.Items.Add(new ComboBoxItem { Content = "All", IsSelected = true });
                HistoryTypeCombo.Items.Add(new ComboBoxItem { Content = "Purchase Only" });
                HistoryTypeCombo.Items.Add(new ComboBoxItem { Content = "Usage Only" });
                HistoryTypeCombo.SelectedIndex = 0;
            }
            catch { }
        }

        private void LoadData()
        {
            try
            {
                var sheets = SheetStoreService.Instance.GetAllActive();
                var historyItems = new System.Collections.Generic.List<HistoryItem>();

                foreach (var sheet in sheets)
                {
                    // Add purchase history records
                    if (sheet.PurchaseHistory != null)
                    {
                        foreach (var purchase in sheet.PurchaseHistory)
                        {
                            historyItems.Add(new HistoryItem
                            {
                                SrNo = historyItems.Count + 1,
                                SheetId = sheet.Id,
                                Category = sheet.Category,
                                Thickness = sheet.Thickness,
                                Color = sheet.Color,
                                HistoryType = "Purchase",
                                Quantity = purchase.Quantity,
                                UnitPrice = purchase.UnitPrice,
                                TotalAmount = purchase.TotalAmt,
                                Supplier = purchase.Supplier,
                                Reason = purchase.Notes,
                                HistoryDate = purchase.PurchasedOn,
                                CreatedAt = purchase.CreatedAt
                            });
                        }
                    }

                    // Add usage history records
                    if (sheet.UseHistory != null)
                    {
                        foreach (var usage in sheet.UseHistory)
                        {
                            historyItems.Add(new HistoryItem
                            {
                                SrNo = historyItems.Count + 1,
                                SheetId = sheet.Id,
                                Category = sheet.Category,
                                Thickness = sheet.Thickness,
                                Color = sheet.Color,
                                HistoryType = "Usage",
                                Quantity = usage.Quantity,
                                UnitPrice = 0,
                                TotalAmount = 0,
                                Supplier = "",
                                Reason = usage.Reason,
                                HistoryDate = usage.UsedOn,
                                CreatedAt = usage.CreatedAt
                            });
                        }
                    }
                }

                // Sort by date descending
                historyItems = historyItems.OrderByDescending(h => h.HistoryDate).ToList();
                for (int i = 0; i < historyItems.Count; i++)
                {
                    historyItems[i].SrNo = i + 1;
                }

                AllHistoryItems = new ObservableCollection<HistoryItem>(historyItems);
                ApplyFilters();
            }
            catch
            {
                AllHistoryItems = new ObservableCollection<HistoryItem>();
                FilteredHistoryItems = new ObservableCollection<HistoryItem>();
                PriceHistoryGrid.ItemsSource = FilteredHistoryItems;
                UpdateSummary();
            }
        }

        private void UpdateSummary()
        {
            try
            {
                var items = FilteredHistoryItems?.ToList() ?? new System.Collections.Generic.List<HistoryItem>();

                TotalCountText.Text = items.Count.ToString();

                var purchases = items.Where(i => i.HistoryType == "Purchase").ToList();
                var totalPurchase = purchases.Sum(i => i.TotalAmount);
                TotalPurchaseText.Text = "AED " + totalPurchase.ToString("N2");

                var totalQty = items.Sum(i => i.Quantity);
                TotalSellText.Text = items.Count + " records";
            }
            catch
            {
                TotalCountText.Text = "0";
                TotalPurchaseText.Text = "AED 0.00";
                TotalSellText.Text = "0 records";
            }
        }

        private void ApplyFilters()
        {
            if (AllHistoryItems == null) return;

            try
            {
                var filtered = AllHistoryItems.ToList();

                // Category filter
                if (!string.IsNullOrEmpty(_currentCategory) && _currentCategory != "All Categories")
                {
                    filtered = filtered.Where(h => h.Category == _currentCategory).ToList();
                }

                // History type filter
                if (_currentHistoryType == "Purchase Only")
                {
                    filtered = filtered.Where(h => h.HistoryType == "Purchase").ToList();
                }
                else if (_currentHistoryType == "Usage Only")
                {
                    filtered = filtered.Where(h => h.HistoryType == "Usage").ToList();
                }

                // Date filter
                var now = DateTime.Now;
                switch (_currentDateRange)
                {
                    case "Today":
                        filtered = filtered.Where(h => h.HistoryDate.Date == now.Date).ToList();
                        break;
                    case "This Week":
                        filtered = filtered.Where(h => h.HistoryDate >= now.AddDays(-7)).ToList();
                        break;
                    case "This Month":
                        filtered = filtered.Where(h => h.HistoryDate >= now.AddMonths(-1)).ToList();
                        break;
                    case "This Year":
                        filtered = filtered.Where(h => h.HistoryDate >= now.AddYears(-1)).ToList();
                        break;
                }

                // Search filter
                if (!string.IsNullOrWhiteSpace(_currentSearch))
                {
                    var search = _currentSearch.ToLower();
                    filtered = filtered.Where(h =>
                        h.Category.ToLower().Contains(search) ||
                        h.Thickness.ToLower().Contains(search) ||
                        h.Color.ToLower().Contains(search) ||
                        h.Supplier.ToLower().Contains(search) ||
                        h.Reason.ToLower().Contains(search)).ToList();
                }

                FilteredHistoryItems = new ObservableCollection<HistoryItem>(filtered);
                PriceHistoryGrid.ItemsSource = FilteredHistoryItems;
                UpdateSummary();
            }
            catch { }
        }

        private void CategoryFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentCategory = (CategoryFilterCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            ApplyFilters();
        }

        private void DateFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentDateRange = (DateFilterCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            ApplyFilters();
        }

        private void HistoryTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentHistoryType = (HistoryTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
            ApplyFilters();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentSearch = SearchBox?.Text ?? "";
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

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV|*.csv",
                    FileName = "History_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")
                };

                if (dialog.ShowDialog() == true)
                {
                    var lines = new System.Collections.Generic.List<string>
                    {
                        "SrNo,Type,Category,Thickness,Color,Quantity,UnitPrice,Total,Supplier,Date,Notes"
                    };

                    foreach (var h in FilteredHistoryItems)
                    {
                        lines.Add($"{h.SrNo},{h.HistoryType},{h.Category},{h.Thickness},{h.Color},{h.Quantity},{h.UnitPrice},{h.TotalAmount},{h.Supplier},{h.HistoryDate:dd-MMM-yyyy},{h.Reason}");
                    }

                    System.IO.File.WriteAllLines(dialog.FileName, lines);
                    MessageBox.Show("Export completed!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    // ═══════════════════════════════════════════════════════
    // HISTORY ITEM CLASS
    // ═══════════════════════════════════════════════════════
    public class HistoryItem
    {
        public int SrNo { get; set; }
        public int SheetId { get; set; }
        public string Category { get; set; } = "";
        public string Thickness { get; set; } = "";
        public string Color { get; set; } = "";
        public string HistoryType { get; set; } = ""; // "Purchase" or "Usage"
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public string Supplier { get; set; } = "";
        public string Reason { get; set; } = "";
        public DateTime HistoryDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}