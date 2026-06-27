using System;
using System.Collections.Generic;
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
        // REENTRANCY GUARD
        // Prevents service DataChanged events from re-triggering
        // LoadData() while we're already mutating data.
        // ═══════════════════════════════════════════════════════
        private bool _suppressReload = false;

        // ═══════════════════════════════════════════════════════
        // SHEET DATA
        // ═══════════════════════════════════════════════════════
        private ObservableCollection<Sheet> _allSheets = new();
        public ObservableCollection<Sheet> AllSheets
        {
            get => _allSheets;
            set
            {
                _allSheets = value ?? new ObservableCollection<Sheet>();
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
                _filteredSheets = value ?? new ObservableCollection<Sheet>();

                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredCount));

                UpdateFilteredStats();
                UpdateSelectedStats();
            }
        }

        private Sheet? _selectedSheet;
        public Sheet? SelectedSheet
        {
            get => _selectedSheet;
            set
            {
                if (_selectedSheet == value)
                    return;

                _selectedSheet = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelection));

                UpdateSelectedStats();

                // Important for commands that depend on selected row
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // ═══════════════════════════════════════════════════════
        // MASTER DATA COUNTS FROM SHEET.CS
        // ═══════════════════════════════════════════════════════
        public int CategoryTotal => Sheet.Categories.Count;
        public int ThicknessTotal => Sheet.Thicknesses.Length;
        public int ColorTotal => Sheet.ColorItems.Count;
        public int SupplierTotal => Sheet.Suppliers.Count;

        // ═══════════════════════════════════════════════════════
        // FILTER OPTIONS
        // ═══════════════════════════════════════════════════════
        public ObservableCollection<string> Categories { get; } = new();
        public ObservableCollection<string> Thicknesses { get; } = new();
        public ObservableCollection<string> Colors { get; } = new();
        public ObservableCollection<string> Suppliers { get; } = new();

        private string _selectedCategory = "ALL";
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory == value)
                    return;

                _selectedCategory = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CategoryName));

                ApplyFilters();
                UpdateAllStats();
            }
        }

        private string _selectedThickness = "ALL";
        public string SelectedThickness
        {
            get => _selectedThickness;
            set
            {
                if (_selectedThickness == value)
                    return;

                _selectedThickness = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ThicknessName));

                ApplyFilters();
                UpdateAllStats();
            }
        }

        private string _selectedColor = "ALL";
        public string SelectedColor
        {
            get => _selectedColor;
            set
            {
                if (_selectedColor == value)
                    return;

                _selectedColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ColorName));

                ApplyFilters();
                UpdateAllStats();
            }
        }

        private string _selectedSupplier = "ALL";
        public string SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                if (_selectedSupplier == value)
                    return;

                _selectedSupplier = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SupplierName));

                ApplyFilters();
                UpdateAllStats();
            }
        }

        public string CategoryName =>
            string.IsNullOrEmpty(SelectedCategory) || SelectedCategory == "ALL"
                ? "ALL"
                : SelectedCategory;

        public string ThicknessName =>
            string.IsNullOrEmpty(SelectedThickness) || SelectedThickness == "ALL"
                ? "ALL"
                : SelectedThickness;

        public string ColorName =>
            string.IsNullOrEmpty(SelectedColor) || SelectedColor == "ALL"
                ? "ALL"
                : SelectedColor;

        public string SupplierName =>
            string.IsNullOrEmpty(SelectedSupplier) || SelectedSupplier == "ALL"
                ? "ALL"
                : SelectedSupplier;

        // ═══════════════════════════════════════════════════════
        // SEARCH
        // ═══════════════════════════════════════════════════════
        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                    return;

                _searchText = value ?? "";
                OnPropertyChanged();

                ApplyFilters();
            }
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
            "Supplier → Category → Color",
            "Stock High → Low",
            "Balance High → Low",
            "Value High → Low",
            "Name (A-Z)",
            "Last Update (Newest First)"
        };

        private string _selectedSortOption = "Category → Color → Thickness";
        public string SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                if (_selectedSortOption == value)
                    return;

                _selectedSortOption = value;
                OnPropertyChanged();

                ApplyFilters();
            }
        }

        // ═══════════════════════════════════════════════════════
        // SELECTED / MAIN SUMMARY STATS
        // These update when clicking a row.
        // If no row selected, they show current filtered totals.
        // ═══════════════════════════════════════════════════════
        public int MainStock { get; private set; }
        public int MainUsed { get; private set; }
        public int MainBalance { get; private set; }

        public double MainSQM { get; private set; }

        public string MainLastPurchase { get; private set; } = "-";
        public string MainLastUpdate { get; private set; } = "-";

        public decimal MainPurchaseValue { get; private set; }
        public decimal MainTotalValue { get; private set; }
        public decimal MainProfitValue { get; private set; }

        // ═══════════════════════════════════════════════════════
        // OVERALL STATS - ALL SHEETS
        // ═══════════════════════════════════════════════════════
        public int TotalSheets => AllSheets?.Count ?? 0;
        public int TotalStock => AllSheets?.Sum(s => s.TotalStock) ?? 0;
        public int TotalUsed => AllSheets?.Sum(s => s.UsedSheets) ?? 0;
        public int BalanceSheets => TotalStock - TotalUsed;

        public decimal TotalPurchaseAll => AllSheets?.Sum(s => s.TotalPurchaseValue) ?? 0;
        public decimal TotalAll => AllSheets?.Sum(s => s.TotalSellValue) ?? 0;
        public decimal TotalProfitAll => TotalAll - TotalPurchaseAll;

        public double TotalSQM => AllSheets?.Sum(s => s.TotalStockSQM) ?? 0;
        public double TotalUsedSQM => AllSheets?.Sum(s => s.UsedSQM) ?? 0;
        public double TotalBalanceSQM => AllSheets?.Sum(s => s.BalanceSQM) ?? 0;

        public int FilteredCount => FilteredSheets?.Count ?? 0;
        public bool HasSelection => SelectedSheet != null;

        // ═══════════════════════════════════════════════════════
        // FILTERED STATS - CURRENT RESULT ROWS
        // ═══════════════════════════════════════════════════════
        private int _filteredStock;
        public int FilteredStock
        {
            get => _filteredStock;
            set
            {
                _filteredStock = value;
                OnPropertyChanged();
            }
        }

        private int _filteredUsed;
        public int FilteredUsed
        {
            get => _filteredUsed;
            set
            {
                _filteredUsed = value;
                OnPropertyChanged();
            }
        }

        private int _filteredBalance;
        public int FilteredBalance
        {
            get => _filteredBalance;
            set
            {
                _filteredBalance = value;
                OnPropertyChanged();
            }
        }

        private decimal _filteredValue;
        public decimal FilteredValue
        {
            get => _filteredValue;
            set
            {
                _filteredValue = value;
                OnPropertyChanged();
            }
        }

        private double _filteredSQM;
        public double FilteredSQM
        {
            get => _filteredSQM;
            set
            {
                _filteredSQM = value;
                OnPropertyChanged();
            }
        }

        private double _filteredUsedSQM;
        public double FilteredUsedSQM
        {
            get => _filteredUsedSQM;
            set
            {
                _filteredUsedSQM = value;
                OnPropertyChanged();
            }
        }

        private double _filteredBalanceSQM;
        public double FilteredBalanceSQM
        {
            get => _filteredBalanceSQM;
            set
            {
                _filteredBalanceSQM = value;
                OnPropertyChanged();
            }
        }

        private int _filteredCategories;
        public int FilteredCategories
        {
            get => _filteredCategories;
            set
            {
                _filteredCategories = value;
                OnPropertyChanged();
            }
        }

        private int _filteredColors;
        public int FilteredColors
        {
            get => _filteredColors;
            set
            {
                _filteredColors = value;
                OnPropertyChanged();
            }
        }

        private int _filteredSuppliers;
        public int FilteredSuppliers
        {
            get => _filteredSuppliers;
            set
            {
                _filteredSuppliers = value;
                OnPropertyChanged();
            }
        }

        private int _filteredThicknesses;
        public int FilteredThicknesses
        {
            get => _filteredThicknesses;
            set
            {
                _filteredThicknesses = value;
                OnPropertyChanged();
            }
        }

        private decimal _filteredPurchaseTotal;
        public decimal FilteredPurchaseTotal
        {
            get => _filteredPurchaseTotal;
            set
            {
                _filteredPurchaseTotal = value;
                OnPropertyChanged();
            }
        }

        private decimal _filteredSellTotal;
        public decimal FilteredSellTotal
        {
            get => _filteredSellTotal;
            set
            {
                _filteredSellTotal = value;
                OnPropertyChanged();
            }
        }

        private decimal _filteredProfit;
        public decimal FilteredProfit
        {
            get => _filteredProfit;
            set
            {
                _filteredProfit = value;
                OnPropertyChanged();
            }
        }

        private int _selectedCount;
        public int SelectedCount
        {
            get => _selectedCount;
            set
            {
                _selectedCount = value;
                OnPropertyChanged();
            }
        }

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
        public ICommand AddSupplierCommand { get; }

        // ═══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════
        public SheetStoreViewModel()
        {
            SheetStoreService.Instance.DataChanged += OnServiceDataChanged;

            AddSheetCommand = new RelayCommand(AddSheet);

            // Keep these always executable.
            // Methods already check for null selection.
            // This prevents commands from staying disabled when row selection changes.
            EditSheetCommand = new RelayCommand(EditSheet);
            DeleteSheetCommand = new RelayCommand(DeleteSheet);
            BuySheetCommand = new RelayCommand(BuySheet);
            UseSheetsCommand = new RelayCommand(UseSheets);

            ExportExcelCommand = new RelayCommand(ExportExcel);
            ImportExcelCommand = new RelayCommand(ImportExcel);
            UpdatePurchasePriceCommand = new RelayCommand(UpdatePurchasePrice);
            ClearFilterCommand = new RelayCommand(ClearFilter);
            ViewHistoryCommand = new RelayCommand(ViewHistory);
            AddSupplierCommand = new RelayCommand(AddNewSupplier);

            LoadData();
        }

        public void Cleanup()
        {
            SheetStoreService.Instance.DataChanged -= OnServiceDataChanged;
        }

        private void OnServiceDataChanged(object sender, EventArgs e)
        {
            // Skip if we're already mutating data ourselves
            if (_suppressReload) return;

            if (Application.Current?.Dispatcher?.CheckAccess() == true)
                LoadData();
            else
                Application.Current?.Dispatcher?.BeginInvoke(new Action(LoadData));
        }

        // ═══════════════════════════════════════════════════════
        // LOAD DATA
        // ═══════════════════════════════════════════════════════
        private void LoadData()
        {
            try
            {
                int? selectedId = SelectedSheet?.Id;

                var sheets = SheetStoreService.Instance.GetAllActive();
                AllSheets = new ObservableCollection<Sheet>(sheets ?? Enumerable.Empty<Sheet>());

                LoadFilterLists();

                ApplyFilters();

                if (selectedId.HasValue)
                {
                    SelectedSheet = FilteredSheets.FirstOrDefault(s => s.Id == selectedId.Value);
                }
                else
                {
                    SelectedSheet = null;
                }

                UpdateAllStats();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadData Error: {ex.Message}");
            }
        }

        private void LoadFilterLists()
        {
            Categories.Clear();
            Categories.Add("ALL");
            foreach (var c in Sheet.Categories.Distinct().OrderBy(x => x))
                Categories.Add(c);

            Thicknesses.Clear();
            Thicknesses.Add("ALL");
            foreach (var t in Sheet.Thicknesses.Distinct())
                Thicknesses.Add(t);

            Colors.Clear();
            Colors.Add("ALL");
            foreach (var c in Sheet.ColorItems.Select(c => c.Name).Distinct().OrderBy(x => x))
                Colors.Add(c);

            Suppliers.Clear();
            Suppliers.Add("ALL");
            foreach (var s in Sheet.Suppliers.Distinct().OrderBy(x => x))
                Suppliers.Add(s);
        }

        // ═══════════════════════════════════════════════════════
        // FILTERING / SORTING
        // ═══════════════════════════════════════════════════════
        private void ApplyFilters()
        {
            if (AllSheets == null)
            {
                FilteredSheets = new ObservableCollection<Sheet>();
                SelectedSheet = null;
                return;
            }

            int? selectedId = SelectedSheet?.Id;

            IEnumerable<Sheet> filtered = AllSheets.AsEnumerable();

            if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != "ALL")
                filtered = filtered.Where(s => string.Equals(s.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(SelectedThickness) && SelectedThickness != "ALL")
                filtered = filtered.Where(s => string.Equals(s.Thickness, SelectedThickness, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(SelectedColor) && SelectedColor != "ALL")
                filtered = filtered.Where(s => string.Equals(s.Color, SelectedColor, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(SelectedSupplier) && SelectedSupplier != "ALL")
                filtered = filtered.Where(s => string.Equals(s.Supplier, SelectedSupplier, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string search = SearchText.Trim();

                filtered = filtered.Where(s =>
                    ContainsText(s.Category, search) ||
                    ContainsText(s.Color, search) ||
                    ContainsText(s.Thickness, search) ||
                    ContainsText(s.Supplier, search) ||
                    ContainsText(s.SupplierName, search) ||
                    ContainsText(s.Description, search) ||
                    s.Width.ToString().Contains(search) ||
                    s.Height.ToString().Contains(search));
            }

            filtered = ApplySorting(filtered);

            var sortedList = filtered.ToList();

            for (int i = 0; i < sortedList.Count; i++)
                sortedList[i].SrNo = i + 1;

            FilteredSheets = new ObservableCollection<Sheet>(sortedList);

            if (selectedId.HasValue)
            {
                var selectedInFiltered = FilteredSheets.FirstOrDefault(s => s.Id == selectedId.Value);
                SelectedSheet = selectedInFiltered;
            }
            else
            {
                SelectedSheet = null;
            }
        }

        private static bool ContainsText(string value, string search)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private IEnumerable<Sheet> ApplySorting(IEnumerable<Sheet> sheets)
        {
            return SelectedSortOption switch
            {
                "Category → Color → Thickness" =>
                    sheets.OrderBy(s => s.Category)
                          .ThenBy(s => s.Color)
                          .ThenBy(s => s.Thickness),

                "Color → Category → Thickness" =>
                    sheets.OrderBy(s => s.Color)
                          .ThenBy(s => s.Category)
                          .ThenBy(s => s.Thickness),

                "Thickness → Color → Category" =>
                    sheets.OrderBy(s => s.Thickness)
                          .ThenBy(s => s.Color)
                          .ThenBy(s => s.Category),

                "Category → Thickness → Color" =>
                    sheets.OrderBy(s => s.Category)
                          .ThenBy(s => s.Thickness)
                          .ThenBy(s => s.Color),

                "Supplier → Category → Color" =>
                    sheets.OrderBy(s => s.Supplier)
                          .ThenBy(s => s.Category)
                          .ThenBy(s => s.Color),

                "Stock High → Low" =>
                    sheets.OrderByDescending(s => s.TotalStock)
                          .ThenBy(s => s.Category),

                "Balance High → Low" =>
                    sheets.OrderByDescending(s => s.BalanceSheets)
                          .ThenBy(s => s.Category),

                "Value High → Low" =>
                    sheets.OrderByDescending(s => s.TotalSellValue)
                          .ThenBy(s => s.Category),

                "Name (A-Z)" =>
                    sheets.OrderBy(s => s.Category)
                          .ThenBy(s => s.Color),

                "Last Update (Newest First)" =>
                    sheets.OrderByDescending(s => s.CreatedDate),

                _ =>
                    sheets.OrderBy(s => s.Category)
                          .ThenBy(s => s.Color)
                          .ThenBy(s => s.Thickness)
            };
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

            OnPropertyChanged(nameof(TotalPurchaseAll));
            OnPropertyChanged(nameof(TotalAll));
            OnPropertyChanged(nameof(TotalProfitAll));

            OnPropertyChanged(nameof(TotalSQM));
            OnPropertyChanged(nameof(TotalUsedSQM));
            OnPropertyChanged(nameof(TotalBalanceSQM));

            OnPropertyChanged(nameof(CategoryTotal));
            OnPropertyChanged(nameof(ThicknessTotal));
            OnPropertyChanged(nameof(ColorTotal));
            OnPropertyChanged(nameof(SupplierTotal));

            UpdateFilteredStats();
            UpdateSelectedStats();
        }

        // ═══════════════════════════════════════════════════════
        // UPDATE FILTERED STATS
        // ═══════════════════════════════════════════════════════
        private void UpdateFilteredStats()
        {
            var sheets = FilteredSheets?.ToList() ?? new List<Sheet>();

            if (sheets.Count == 0)
            {
                FilteredStock = 0;
                FilteredUsed = 0;
                FilteredBalance = 0;

                FilteredValue = 0;
                FilteredPurchaseTotal = 0;
                FilteredSellTotal = 0;
                FilteredProfit = 0;

                FilteredSQM = 0;
                FilteredUsedSQM = 0;
                FilteredBalanceSQM = 0;

                FilteredCategories = 0;
                FilteredColors = 0;
                FilteredSuppliers = 0;
                FilteredThicknesses = 0;
                return;
            }

            FilteredStock = sheets.Sum(s => s.TotalStock);
            FilteredUsed = sheets.Sum(s => s.UsedSheets);
            FilteredBalance = FilteredStock - FilteredUsed;

            FilteredPurchaseTotal = sheets.Sum(s => s.TotalPurchaseValue);
            FilteredSellTotal = sheets.Sum(s => s.TotalSellValue);
            FilteredValue = FilteredSellTotal;
            FilteredProfit = FilteredSellTotal - FilteredPurchaseTotal;

            FilteredSQM = sheets.Sum(s => s.TotalStockSQM);
            FilteredUsedSQM = sheets.Sum(s => s.UsedSQM);
            FilteredBalanceSQM = sheets.Sum(s => s.BalanceSQM);

            FilteredCategories = sheets.Where(s => !string.IsNullOrWhiteSpace(s.Category))
                                       .Select(s => s.Category)
                                       .Distinct(StringComparer.OrdinalIgnoreCase)
                                       .Count();

            FilteredColors = sheets.Where(s => !string.IsNullOrWhiteSpace(s.Color))
                                   .Select(s => s.Color)
                                   .Distinct(StringComparer.OrdinalIgnoreCase)
                                   .Count();

            FilteredSuppliers = sheets.Where(s => !string.IsNullOrWhiteSpace(s.Supplier))
                                      .Select(s => s.Supplier)
                                      .Distinct(StringComparer.OrdinalIgnoreCase)
                                      .Count();

            FilteredThicknesses = sheets.Where(s => !string.IsNullOrWhiteSpace(s.Thickness))
                                        .Select(s => s.Thickness)
                                        .Distinct(StringComparer.OrdinalIgnoreCase)
                                        .Count();
        }

        // ═══════════════════════════════════════════════════════
        // UPDATE SELECTED ROW / FALLBACK FILTERED STATS
        // ═══════════════════════════════════════════════════════
        private void UpdateSelectedStats()
        {
            if (SelectedSheet != null)
            {
                MainStock = SelectedSheet.TotalStock;
                MainUsed = SelectedSheet.UsedSheets;
                MainBalance = SelectedSheet.BalanceSheets;

                MainSQM = SelectedSheet.TotalStockSQM;

                MainPurchaseValue = SelectedSheet.TotalPurchaseValue;
                MainTotalValue = SelectedSheet.TotalSellValue;
                MainProfitValue = SelectedSheet.TotalProfitValue;

                MainLastPurchase = SelectedSheet.LatestPurchaseDate.HasValue
                    ? SelectedSheet.LatestPurchaseDate.Value.ToString("dd/MM/yyyy")
                    : "-";

                MainLastUpdate = SelectedSheet.DisplayDateTime ?? "-";
            }
            else
            {
                var sheets = FilteredSheets?.ToList() ?? new List<Sheet>();

                MainStock = sheets.Sum(s => s.TotalStock);
                MainUsed = sheets.Sum(s => s.UsedSheets);
                MainBalance = MainStock - MainUsed;

                MainSQM = sheets.Sum(s => s.TotalStockSQM);

                MainPurchaseValue = sheets.Sum(s => s.TotalPurchaseValue);
                MainTotalValue = sheets.Sum(s => s.TotalSellValue);
                MainProfitValue = MainTotalValue - MainPurchaseValue;

                var lastPurchaseSheet = sheets.Where(s => s.LatestPurchaseDate.HasValue)
                                              .OrderByDescending(s => s.LatestPurchaseDate)
                                              .FirstOrDefault();

                MainLastPurchase = lastPurchaseSheet != null
                    ? $"{lastPurchaseSheet.Thickness} {lastPurchaseSheet.Color}"
                    : "-";

                var lastUpdateSheet = sheets.OrderByDescending(s => s.CreatedDate).FirstOrDefault();
                MainLastUpdate = lastUpdateSheet?.DisplayDateTime ?? "-";
            }

            OnPropertyChanged(nameof(CategoryName));
            OnPropertyChanged(nameof(ThicknessName));
            OnPropertyChanged(nameof(ColorName));
            OnPropertyChanged(nameof(SupplierName));

            OnPropertyChanged(nameof(MainStock));
            OnPropertyChanged(nameof(MainUsed));
            OnPropertyChanged(nameof(MainBalance));
            OnPropertyChanged(nameof(MainSQM));

            OnPropertyChanged(nameof(MainPurchaseValue));
            OnPropertyChanged(nameof(MainTotalValue));
            OnPropertyChanged(nameof(MainProfitValue));

            OnPropertyChanged(nameof(MainLastPurchase));
            OnPropertyChanged(nameof(MainLastUpdate));
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

                if (sheet == null)
                {
                    MessageBox.Show("Please select a sheet first!", "No Selection");
                    return;
                }

                var dialog = new Views.SheetStore.SheetDialog(sheet);
                if (dialog.ShowDialog() == true)
                    SheetStoreService.Instance.UpdateSheet(dialog.NewSheet);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error editing sheet: {ex.Message}", "Error");
            }
        }

        // ═══════════════════════════════════════════════════════
        // DELETE SHEET — FIXED
        // ═══════════════════════════════════════════════════════
        public void DeleteSheet(object parameter = null)
        {
            try
            {
                Sheet? sheet = parameter as Sheet ?? SelectedSheet;

                if (sheet == null)
                {
                    MessageBox.Show("Please select a sheet first!", "No Selection");
                    return;
                }

                var result = MessageBox.Show(
                    $"Delete '{sheet.Category} - {sheet.Thickness} {sheet.Color}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                int targetId = sheet.Id;
                System.Diagnostics.Debug.WriteLine($"DeleteSheet (VM): Begin delete for ID={targetId}");

                // 1) Detach selection FIRST so binding doesn't hold the row
                if (SelectedSheet?.Id == targetId)
                    SelectedSheet = null;

                // 2) Optimistic in-memory removal (instant UI feedback)
                //    Suppress reload so DataChanged event doesn't bounce back mid-delete.
                _suppressReload = true;
                try
                {
                    var inAll = AllSheets.FirstOrDefault(s => s.Id == targetId);
                    if (inAll != null) AllSheets.Remove(inAll);

                    var inFiltered = FilteredSheets.FirstOrDefault(s => s.Id == targetId);
                    if (inFiltered != null) FilteredSheets.Remove(inFiltered);

                    // 3) Persist deletion in service / XML
                    SheetStoreService.Instance.DeleteSheet(targetId);
                }
                finally
                {
                    _suppressReload = false;
                }

                // 4) Reload from source of truth so any inconsistency self-heals
                LoadData();

                // 5) Refresh stats
                UpdateAllStats();

                System.Diagnostics.Debug.WriteLine($"DeleteSheet (VM): Completed delete for ID={targetId}");
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

                    LoadData();
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

                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "CSV Files|*.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    int count = SheetStoreService.Instance.ImportFromExcel(dialog.FileName);
                    MessageBox.Show($"Imported {count} sheets!", "Success");

                    LoadData();
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

                LoadData();
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
            SelectedSupplier = "ALL";
            SelectedSortOption = "Category → Color → Thickness";
            SearchText = "";
            SelectedSheet = null;

            ApplyFilters();
            UpdateAllStats();
        }

        public void AddNewSupplier()
        {
            try
            {
                var inputDialog = new Views.SheetStore.AddSupplierDialog();

                if (inputDialog.ShowDialog() == true &&
                    !string.IsNullOrWhiteSpace(inputDialog.SupplierName))
                {
                    string newSupplier = inputDialog.SupplierName.Trim();

                    if (Sheet.SupplierExists(newSupplier))
                    {
                        MessageBox.Show(
                            $"Supplier '{newSupplier}' already exists!",
                            "Duplicate",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    Sheet.AddSupplier(newSupplier);

                    Suppliers.Clear();
                    Suppliers.Add("ALL");
                    foreach (var s in Sheet.Suppliers.OrderBy(x => x))
                        Suppliers.Add(s);

                    SelectedSupplier = newSupplier;

                    MessageBox.Show(
                        $"Supplier '{newSupplier}' added successfully!",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding supplier: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
