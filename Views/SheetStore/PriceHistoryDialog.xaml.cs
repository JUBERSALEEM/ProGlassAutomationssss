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
        public ObservableCollection<Sheet> AllSheets { get; set; }
        public ObservableCollection<Sheet> FilteredSheets { get; set; }

        private string _currentCategory = "";
        private string _currentDateRange = "";
        private string _currentSearch = "";

        // ═══════════════════════════════════════════════════════
        // FIX #1: CancellationToken instead of System.Timers.Timer
        // ═══════════════════════════════════════════════════════
        private CancellationTokenSource _debounceCts;

        public PriceHistoryDialog()
        {
            InitializeComponent();

            // Subscribe to auto-refresh with correct EventHandler signature
            SheetStoreService.Instance.DataChanged += OnDataChanged;

            InitializeFilters();
            LoadData();
        }

        // ═══════════════════════════════════════════════════════
        // FIX #7: EventHandler signature
        // ═══════════════════════════════════════════════════════
        private void OnDataChanged(object sender, EventArgs e)
        {
            if (this.IsActive || this.IsVisible)
            {
                Dispatcher.Invoke(LoadData);
            }
        }

        // ==================== CLEANUP ====================
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
                CategoryFilterCombo.Items.Clear();
                CategoryFilterCombo.Items.Add(new ComboBoxItem { Content = "All Categories", IsSelected = true });

                // ═══════════════════════════════════════════════════════
                // FIX #4: Use prebuilt category index from cache
                // ═══════════════════════════════════════════════════════
                var categories = SheetStoreService.Instance.GetCategories();
                foreach (string cat in categories)
                {
                    CategoryFilterCombo.Items.Add(new ComboBoxItem { Content = cat });
                }

                CategoryFilterCombo.SelectedIndex = 0;

                DateFilterCombo.Items.Clear();
                DateFilterCombo.Items.Add(new ComboBoxItem { Content = "All Time", IsSelected = true });
                DateFilterCombo.Items.Add(new ComboBoxItem { Content = "Today" });
                DateFilterCombo.Items.Add(new ComboBoxItem { Content = "This Week" });
                DateFilterCombo.Items.Add(new ComboBoxItem { Content = "This Month" });
                DateFilterCombo.Items.Add(new ComboBoxItem { Content = "This Year" });
                DateFilterCombo.SelectedIndex = 0;
            }
            catch
            {
                // Silently handle filter initialization errors
            }
        }

        private void LoadData()
        {
            try
            {
                AllSheets = SheetStoreService.Instance.GetAllActive();
                ApplyFilters();
            }
            catch
            {
                AllSheets = new ObservableCollection<Sheet>();
                FilteredSheets = new ObservableCollection<Sheet>();
                PriceHistoryGrid.ItemsSource = FilteredSheets;
                UpdateSummary();
            }
        }

        private void UpdateSummary()
        {
            try
            {
                TotalCountText.Text = FilteredSheets?.Count.ToString() ?? "0";
                TotalPurchaseText.Text = "AED " + (FilteredSheets?.Sum(s => s.PurchasePrice) ?? 0).ToString("N2");
                TotalSellText.Text = "AED " + (FilteredSheets?.Sum(s => s.SellPrice) ?? 0).ToString("N2");
            }
            catch
            {
                TotalCountText.Text = "0";
                TotalPurchaseText.Text = "AED 0.00";
                TotalSellText.Text = "AED 0.00";
            }
        }

        private void ApplyFilters()
        {
            if (AllSheets == null) return;

            try
            {
                var filtered = AllSheets.ToList();

                // ═══════════════════════════════════════════════════════
                // FIX #9: Use prebuilt category lookup instead of LINQ
                // ═══════════════════════════════════════════════════════
                if (!string.IsNullOrEmpty(_currentCategory) && _currentCategory != "All Categories")
                {
                    // Use cached category lookup
                    var byCategory = SheetStoreService.Instance.GetByCategory(_currentCategory);
                    filtered = byCategory.ToList();
                }

                // Date filter (keep as-is, no prebuilt index needed)
                var now = DateTime.Now;
                switch (_currentDateRange)
                {
                    case "Today":
                        filtered = filtered.Where(s => s.LatestPurchaseDate?.Date == now.Date).ToList();
                        break;
                    case "This Week":
                        filtered = filtered.Where(s => s.LatestPurchaseDate >= now.AddDays(-7)).ToList();
                        break;
                    case "This Month":
                        filtered = filtered.Where(s => s.LatestPurchaseDate >= now.AddMonths(-1)).ToList();
                        break;
                    case "This Year":
                        filtered = filtered.Where(s => s.LatestPurchaseDate >= now.AddYears(-1)).ToList();
                        break;
                }

                // Search filter - use prebuilt search index
                if (!string.IsNullOrWhiteSpace(_currentSearch))
                {
                    var searchResults = SheetStoreService.Instance.Search(_currentSearch);
                    if (filtered.Count == AllSheets.Count)
                    {
                        // No other filters, use search results directly
                        filtered = searchResults.ToList();
                    }
                    else
                    {
                        // Combine with existing filters
                        var searchSet = new HashSet<int>(searchResults.Select(s => s.Id));
                        filtered = filtered.Where(s => searchSet.Contains(s.Id)).ToList();
                    }
                }

                FilteredSheets = new ObservableCollection<Sheet>(filtered);
                PriceHistoryGrid.ItemsSource = FilteredSheets;
                UpdateSummary();
            }
            catch
            {
                // Silently handle filter errors
            }
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

        // ═══════════════════════════════════════════════════════
        // FIX #1: Task.Delay + CancellationToken debounce
        // ═══════════════════════════════════════════════════════
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
                    FileName = "PriceHistory_" + DateTime.Now.ToString("yyyyMMdd")
                };

                if (dialog.ShowDialog() == true)
                {
                    var lines = new System.Collections.Generic.List<string>
                    {
                        "SrNo,Category,Thickness,Color,Purchase,Sell,Supplier,Date"
                    };

                    foreach (var s in FilteredSheets)
                    {
                        string date = s.LatestPurchaseDate.HasValue ? s.LatestPurchaseDate.Value.ToString("dd-MMM-yyyy") : "-";
                        lines.Add($"{s.SrNo},{s.Category},{s.Thickness},{s.Color},{s.PurchasePrice},{s.SellPrice},{s.Supplier},{date}");
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
}