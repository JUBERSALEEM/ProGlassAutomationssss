using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ProGlassAutomation.Models;
using ProGlassAutomation.Services;

namespace ProGlassAutomation.ViewModels
{
    public class SheetStoreViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // ═══════════════════════════════════════════════════════
        // SHEET DATA
        // ═══════════════════════════════════════════════════════
        private ObservableCollection<Sheet> _allSheets = new();
        public ObservableCollection<Sheet> AllSheets
        {
            get => _allSheets;
            set
            {
                _allSheets = value;
                OnPropertyChanged();
                UpdateAllStats();
            }
        }

        private ObservableCollection<Sheet> _filteredSheets = new();
        public ObservableCollection<Sheet> FilteredSheets
        {
            get => _filteredSheets;
            set
            {
                _filteredSheets = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredCount));
                UpdateSelectedStats();
            }
        }

        private Sheet _selectedSheet;
        public Sheet? SelectedSheet
        {
            get => _selectedSheet;
            set
            {
                _selectedSheet = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelection));
                UpdateSelectedStats();
            }
        }

        // ═══════════════════════════════════════════════════════
        // CATEGORIES
        // ═══════════════════════════════════════════════════════
        public ObservableCollection<string> Categories { get; } = new();

        private string _selectedCategory = "ALL";
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CategoryName));
                ApplyFilters();
                UpdateAllStats();
            }
        }

        // ═══════════════════════════════════════════════════════
        // THICKNESS
        // ═══════════════════════════════════════════════════════
        public ObservableCollection<string> Thicknesses { get; } = new();

        private string _selectedThickness = "ALL";
        public string SelectedThickness
        {
            get => _selectedThickness;
            set
            {
                _selectedThickness = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ThicknessName));
                ApplyFilters();
                UpdateAllStats();
            }
        }

        // ═══════════════════════════════════════════════════════
        // COLORS
        // ═══════════════════════════════════════════════════════
        public ObservableCollection<string> Colors { get; } = new();

        private string _selectedColor = "ALL";
        public string SelectedColor
        {
            get => _selectedColor;
            set
            {
                _selectedColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ColorName));
                ApplyFilters();
                UpdateAllStats();
            }
        }

        // ═══════════════════════════════════════════════════════
        // SEARCH
        // ═══════════════════════════════════════════════════════
        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ApplyFilters(); }
        }

        // ═══════════════════════════════════════════════════════
        // SORT OPTIONS
        // ═══════════════════════════════════════════════════════
        public ObservableCollection<string> SortOptions { get; } = new()
        {
            "Category → Color → Thickness",
            "Color → Category → Thickness",
            "Thickness → Color → Category",
            "Category → Thickness → Color",
            "Name (A-Z)",
            "Last Update (Newest First)"
        };

        private string _selectedSortOption = "Category → Color → Thickness";
        public string SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                _selectedSortOption = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        // ═══════════════════════════════════════════════════════
        // MAIN STATS
        // ═══════════════════════════════════════════════════════
        public string CategoryName => string.IsNullOrEmpty(SelectedCategory) || SelectedCategory == "ALL" ? "ALL" : SelectedCategory;
        public string ThicknessName => string.IsNullOrEmpty(SelectedThickness) || SelectedThickness == "ALL" ? "ALL" : SelectedThickness;
        public string ColorName => string.IsNullOrEmpty(SelectedColor) || SelectedColor == "ALL" ? "ALL" : SelectedColor;

        public int MainStock { get; private set; }
        public int MainUsed { get; private set; }
        public int MainBalance { get; private set; }
        public string MainLastPurchase { get; private set; } = "-";
        public string MainLastUpdate { get; private set; } = "-";
        public decimal MainTotalValue { get; private set; }

        // ═══════════════════════════════════════════════════════
        // OVERALL STATS
        // ═══════════════════════════════════════════════════════
        public int TotalSheets => AllSheets?.Count ?? 0;
        public int TotalStock => AllSheets?.Sum(s => s.TotalStock) ?? 0;
        public int TotalUsed => AllSheets?.Sum(s => s.UsedSheets) ?? 0;
        public int BalanceSheets => TotalStock - TotalUsed;
        public decimal TotalAll => AllSheets?.Sum(s => s.TotalStock * s.SellPrice) ?? 0;
        public int FilteredCount => FilteredSheets?.Count ?? 0;
        public bool HasSelection => SelectedSheet != null;

        // ═══════════════════════════════════════════════════════
        // COMMANDS
        // ═══════════════════════════════════════════════════════
        public ICommand AddSheetCommand { get; }
        public ICommand EditSheetCommand { get; }
        public ICommand DeleteSheetCommand { get; }
        public ICommand BuySheetCommand { get; }
        public ICommand UseSheetsCommand { get; }
        public ICommand ExportExcelCommand { get; }
        public ICommand ImportExcelCommand { get; }
        public ICommand UpdatePurchasePriceCommand { get; }
        public ICommand ClearFilterCommand { get; }
        public ICommand ViewHistoryCommand { get; }

        // ═══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════
        public SheetStoreViewModel()
        {
            SheetStoreService.Instance.DataChanged += OnServiceDataChanged;

            AddSheetCommand = new RelayCommand(AddSheet);
            EditSheetCommand = new RelayCommand(EditSheet, CanEditOrDelete);
            DeleteSheetCommand = new RelayCommand(DeleteSheet, CanEditOrDelete);
            BuySheetCommand = new RelayCommand(BuySheet, CanEditOrDelete);
            UseSheetsCommand = new RelayCommand(UseSheets, CanEditOrDelete);
            ExportExcelCommand = new RelayCommand(ExportExcel);
            ImportExcelCommand = new RelayCommand(ImportExcel);
            UpdatePurchasePriceCommand = new RelayCommand(UpdatePurchasePrice);
            ClearFilterCommand = new RelayCommand(ClearFilter);
            ViewHistoryCommand = new RelayCommand(ViewHistory);

            LoadData();
        }

        public void Cleanup()
        {
            SheetStoreService.Instance.DataChanged -= OnServiceDataChanged;
        }

        private void OnServiceDataChanged(object sender, EventArgs e)
        {
            if (Application.Current?.Dispatcher?.CheckAccess() == true)
                LoadData();
            else
                Application.Current?.Dispatcher?.BeginInvoke(new Action(LoadData));
        }

        private void LoadData()
        {
            try
            {
                var sheets = SheetStoreService.Instance.GetAllActive();
                AllSheets = new ObservableCollection<Sheet>(sheets);

                // Load Categories
                Categories.Clear();
                Categories.Add("ALL");
                foreach (var c in SheetStoreService.Instance.GetCategories().OrderBy(x => x))
                    Categories.Add(c);

                // Load Thicknesses
                Thicknesses.Clear();
                Thicknesses.Add("ALL");
                var thicknesses = sheets.Select(s => s.Thickness).Distinct().OrderBy(t => t);
                foreach (var t in thicknesses)
                    Thicknesses.Add(t);

                // Load Colors
                Colors.Clear();
                Colors.Add("ALL");
                var colors = sheets.Select(s => s.Color).Distinct().OrderBy(c => c);
                foreach (var c in colors)
                    Colors.Add(c);

                ApplyFilters();
                UpdateAllStats();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadData Error: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            if (AllSheets == null)
            {
                FilteredSheets = new ObservableCollection<Sheet>();
                return;
            }

            var filtered = AllSheets.AsEnumerable();

            // Filter by Category
            if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != "ALL")
                filtered = filtered.Where(s => s.Category == SelectedCategory);

            // Filter by Thickness
            if (!string.IsNullOrEmpty(SelectedThickness) && SelectedThickness != "ALL")
                filtered = filtered.Where(s => s.Thickness == SelectedThickness);

            // Filter by Color
            if (!string.IsNullOrEmpty(SelectedColor) && SelectedColor != "ALL")
                filtered = filtered.Where(s => s.Color == SelectedColor);

            // Filter by Search
            if (!string.IsNullOrEmpty(SearchText))
            {
                var search = SearchText.ToLower();
                filtered = filtered.Where(s =>
                    s.Category.ToLower().Contains(search) ||
                    s.Color.ToLower().Contains(search) ||
                    s.Thickness.ToLower().Contains(search) ||
                    (s.Supplier?.ToLower().Contains(search) ?? false));
            }

            // Apply Sorting
            filtered = ApplySorting(filtered);

            // Convert to list and assign SrNo
            var sortedList = filtered.ToList();
            for (int i = 0; i < sortedList.Count; i++)
            {
                sortedList[i].SrNo = i + 1;
            }

            FilteredSheets = new ObservableCollection<Sheet>(sortedList);
        }

        private IEnumerable<Sheet> ApplySorting(IEnumerable<Sheet> sheets)
        {
            switch (SelectedSortOption)
            {
                case "Category → Color → Thickness":
                    return sheets.OrderBy(s => s.Category)
                                 .ThenBy(s => s.Color)
                                 .ThenBy(s => s.Thickness);

                case "Color → Category → Thickness":
                    return sheets.OrderBy(s => s.Color)
                                 .ThenBy(s => s.Category)
                                 .ThenBy(s => s.Thickness);

                case "Thickness → Color → Category":
                    return sheets.OrderBy(s => s.Thickness)
                                 .ThenBy(s => s.Color)
                                 .ThenBy(s => s.Category);

                case "Category → Thickness → Color":
                    return sheets.OrderBy(s => s.Category)
                                 .ThenBy(s => s.Thickness)
                                 .ThenBy(s => s.Color);

                case "Name (A-Z)":
                    return sheets.OrderBy(s => s.Category)
                                 .ThenBy(s => s.Color);

                case "Last Update (Newest First)":
                    return sheets.OrderByDescending(s => s.CreatedDate);

                default:
                    return sheets.OrderBy(s => s.Category)
                                 .ThenBy(s => s.Color)
                                 .ThenBy(s => s.Thickness);
            }
        }

        // ═══════════════════════════════════════════════════════
        // UPDATE ALL STATS
        // ═══════════════════════════════════════════════════════
        private void UpdateAllStats()
        {
            OnPropertyChanged(nameof(TotalSheets));
            OnPropertyChanged(nameof(TotalStock));
            OnPropertyChanged(nameof(TotalUsed));
            OnPropertyChanged(nameof(BalanceSheets));
            OnPropertyChanged(nameof(TotalAll));
            UpdateSelectedStats();
        }

        // ═══════════════════════════════════════════════════════
        // UPDATE MAIN STATS
        // ═══════════════════════════════════════════════════════
        private void UpdateSelectedStats()
        {
            var sheets = FilteredSheets?.ToList();

            MainStock = sheets?.Sum(s => s.TotalStock) ?? 0;
            MainUsed = sheets?.Sum(s => s.UsedSheets) ?? 0;
            MainBalance = MainStock - MainUsed;
            MainTotalValue = sheets?.Sum(s => s.TotalStock * s.SellPrice) ?? 0;

            // Last Purchase
            var lastPurchaseSheet = sheets?.Where(s => s.LatestPurchaseDate.HasValue)
                                            .OrderByDescending(s => s.LatestPurchaseDate)
                                            .FirstOrDefault();
            MainLastPurchase = lastPurchaseSheet != null ? $"{lastPurchaseSheet.Thickness} {lastPurchaseSheet.Color}" : "-";

            // Last Update
            var lastUpdateSheet = sheets?.OrderByDescending(s => s.CreatedDate).FirstOrDefault();
            MainLastUpdate = lastUpdateSheet?.DisplayDateTime ?? "-";

            OnPropertyChanged(nameof(CategoryName));
            OnPropertyChanged(nameof(ThicknessName));
            OnPropertyChanged(nameof(ColorName));
            OnPropertyChanged(nameof(MainStock));
            OnPropertyChanged(nameof(MainUsed));
            OnPropertyChanged(nameof(MainBalance));
            OnPropertyChanged(nameof(MainLastPurchase));
            OnPropertyChanged(nameof(MainLastUpdate));
            OnPropertyChanged(nameof(MainTotalValue));
        }

        // ═══════════════════════════════════════════════════════
        // CRUD OPERATIONS
        // ═══════════════════════════════════════════════════════
        public void AddSheet()
        {
            try
            {
                var dialog = new Views.SheetStore.SheetDialog(null);
                if (dialog.ShowDialog() == true)
                    SheetStoreService.Instance.AddSheet(dialog.NewSheet);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding sheet: {ex.Message}", "Error");
            }
        }

        public void EditSheet(object parameter = null)
        {
            try
            {
                Sheet? sheet = parameter as Sheet ?? SelectedSheet;
                if (sheet == null) return;

                var dialog = new Views.SheetStore.SheetDialog(sheet);
                if (dialog.ShowDialog() == true)
                    SheetStoreService.Instance.UpdateSheet(dialog.NewSheet);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error editing sheet: {ex.Message}", "Error");
            }
        }

        public void DeleteSheet(object parameter = null)
        {
            try
            {
                Sheet? sheet = parameter as Sheet ?? SelectedSheet;
                if (sheet == null) return;

                var result = MessageBox.Show(
                    $"Delete '{sheet.Category} - {sheet.Thickness}mm {sheet.Color}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                    SheetStoreService.Instance.DeleteSheet(sheet.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting sheet: {ex.Message}", "Error");
            }
        }

        public void BuySheet(object parameter = null)
        {
            try
            {
                Sheet? sheet = parameter as Sheet ?? SelectedSheet;
                if (sheet == null)
                {
                    MessageBox.Show("Please select a sheet first!", "No Selection");
                    return;
                }

                var dialog = new Views.SheetStore.BuySheetDialog(sheet);
                if (dialog.ShowDialog() == true)
                {
                    var record = new SheetPurchase
                    {
                        SheetId = sheet.Id,
                        Quantity = dialog.Quantity,
                        UnitPrice = dialog.UnitPrice,
                        Supplier = dialog.Supplier,
                        PurchasedOn = dialog.PurchasedOn,
                        CreatedAt = DateTime.Now
                    };

                    SheetStoreService.Instance.AddPurchaseRecord(record);
                    MessageBox.Show($"Purchased {record.Quantity} sheets at {record.UnitPrice} AED each!", "Success");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void UseSheets(object parameter = null)
        {
            try
            {
                Sheet? sheet = parameter as Sheet ?? SelectedSheet;
                if (sheet == null)
                {
                    MessageBox.Show("Please select a sheet first!", "No Selection");
                    return;
                }

                int balance = sheet.TotalStock - sheet.UsedSheets;
                if (balance <= 0)
                {
                    MessageBox.Show("No sheets available to use!", "Insufficient Stock");
                    return;
                }

                var dialog = new Views.SheetStore.UseSheetsDialog(balance);
                if (dialog.ShowDialog() == true)
                {
                    var record = new SheetUsage
                    {
                        SheetId = sheet.Id,
                        Quantity = dialog.Quantity,
                        Reason = dialog.Reason,
                        UsedOn = dialog.UsedOn,
                        CreatedAt = DateTime.Now
                    };

                    SheetStoreService.Instance.AddUsageRecord(record);
                    MessageBox.Show($"Used {record.Quantity} sheets!", "Success");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanEditOrDelete(object parameter = null)
        {
            if (parameter is Sheet) return true;
            return SelectedSheet != null;
        }

        public void ExportExcel()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files|*.csv",
                    FileName = $"SheetInventory_{DateTime.Now:yyyyMMdd}"
                };

                if (dialog.ShowDialog() == true)
                {
                    SheetStoreService.Instance.ExportToExcel(dialog.FileName);
                    MessageBox.Show("Export completed!", "Success");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting: {ex.Message}", "Error");
            }
        }

        public void ImportExcel()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "CSV Files|*.csv" };
                if (dialog.ShowDialog() == true)
                {
                    int count = SheetStoreService.Instance.ImportFromExcel(dialog.FileName);
                    MessageBox.Show($"Imported {count} sheets!", "Success");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing: {ex.Message}", "Error");
            }
        }

        public void UpdatePurchasePrice()
        {
            try
            {
                var dialog = new Views.SheetStore.PurchasePriceDialog();
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating prices: {ex.Message}", "Error");
            }
        }

        public void ClearFilter()
        {
            SelectedCategory = "ALL";
            SelectedThickness = "ALL";
            SelectedColor = "ALL";
            SearchText = "";
            SelectedSheet = null;
        }

        public void ViewHistory()
        {
            try
            {
                var dialog = new Views.SheetStore.PriceHistoryDialog();
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening history: {ex.Message}", "Error");
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}