using System;
using System.Collections.ObjectModel;
using System.Linq;
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

        public PriceHistoryDialog()
        {
            InitializeComponent();
            InitializeFilters();
            LoadData();
        }

        private void InitializeFilters()
        {
            try
            {
                // Load Categories
                CategoryFilterCombo.Items.Clear();
                CategoryFilterCombo.Items.Add(new ComboBoxItem { Content = "All Categories", IsSelected = true });

                // Check if Sheet.Categories exists and has items
                if (Sheet.Categories != null && Sheet.Categories.Count > 0)
                {
                    foreach (string cat in Sheet.Categories)
                    {
                        CategoryFilterCombo.Items.Add(new ComboBoxItem { Content = cat });
                    }
                }

                CategoryFilterCombo.SelectedIndex = 0;

                // Load Date Ranges
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
                FilteredSheets = new ObservableCollection<Sheet>(AllSheets);
                PriceHistoryGrid.ItemsSource = FilteredSheets;
                UpdateSummary();
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
                TotalCountText.Text = FilteredSheets.Count.ToString();
                TotalPurchaseText.Text = "AED " + FilteredSheets.Sum(s => s.PurchasePrice).ToString("N2");
                TotalSellText.Text = "AED " + FilteredSheets.Sum(s => s.SellPrice).ToString("N2");
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
            try
            {
                // Get selected category safely
                string category = "All Categories";
                if (CategoryFilterCombo.SelectedItem is ComboBoxItem catItem)
                {
                    category = catItem.Content?.ToString() ?? "All Categories";
                }

                // Get selected date range safely
                string dateRange = "All Time";
                if (DateFilterCombo.SelectedItem is ComboBoxItem dateItem)
                {
                    dateRange = dateItem.Content?.ToString() ?? "All Time";
                }

                // Get search text safely
                string search = SearchBox?.Text?.ToLower() ?? "";

                // Start filtering
                var filtered = AllSheets.ToList();

                // Category filter
                if (category != "All Categories")
                {
                    filtered = filtered.Where(s => s.Category == category).ToList();
                }

                // Date filter
                var now = DateTime.Now;
                switch (dateRange)
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

                // Search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    filtered = filtered.Where(s =>
                        (s.Category ?? "").ToLower().Contains(search) ||
                        (s.Thickness ?? "").ToLower().Contains(search) ||
                        (s.Color ?? "").ToLower().Contains(search) ||
                        (s.Supplier ?? "").ToLower().Contains(search)).ToList();
                }

                FilteredSheets = new ObservableCollection<Sheet>(filtered);
                PriceHistoryGrid.ItemsSource = FilteredSheets;
                UpdateSummary();
            }
            catch
            {
                // Silently handle filter errors - don't show popup
            }
        }

        private void CategoryFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DateFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
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