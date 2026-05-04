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
                OnPropertyChanged(nameof(TotalSheets));
                OnPropertyChanged(nameof(TotalStock));
                OnPropertyChanged(nameof(TotalUsed));
                OnPropertyChanged(nameof(BalanceSheets));
                OnPropertyChanged(nameof(TotalAll));
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
            }
        }

        private Sheet _selectedSheet;
        public Sheet? SelectedSheet
        {
            get => _selectedSheet;
            set { _selectedSheet = value; OnPropertyChanged(); }
        }

        // ═══════════════════════════════════════════════════════
        // CATEGORIES
        // ═══════════════════════════════════════════════════════
        public ObservableCollection<string> Categories { get; } = new();

        private string _selectedCategory = "";
        public string SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); ApplyFilters(); }
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
        // STATS PROPERTIES
        // ═══════════════════════════════════════════════════════
        public int TotalSheets => AllSheets?.Count ?? 0;
        public int TotalStock => AllSheets?.Sum(s => s.TotalStock) ?? 0;
        public int TotalUsed => AllSheets?.Sum(s => s.UsedSheets) ?? 0;
        public int BalanceSheets => TotalStock - TotalUsed;
        public decimal TotalAll => AllSheets?.Sum(s => s.TotalStock * s.SellPrice) ?? 0;
        public string LastPurchase => GetLastPurchase();
        public string LastUpdate => GetLastUpdate();
        public int FilteredCount => FilteredSheets?.Count ?? 0;

        // ═══════════════════════════════════════════════════════
        // COMMANDS
        // ═══════════════════════════════════════════════════════
        public ICommand AddSheetCommand { get; }
        public ICommand EditSheetCommand { get; }
        public ICommand DeleteSheetCommand { get; }
        public ICommand BuySheetCommand { get; }
        public ICommand SaveCommand { get; }
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
            System.Diagnostics.Debug.WriteLine("SheetStoreViewModel: Constructor started");

            // Subscribe to DataChanged event
            SheetStoreService.Instance.DataChanged += OnServiceDataChanged;
            System.Diagnostics.Debug.WriteLine("SheetStoreViewModel: DataChanged event subscribed");

            AddSheetCommand = new RelayCommand(AddSheet);
            EditSheetCommand = new RelayCommand(EditSheet, CanEditOrDelete);
            DeleteSheetCommand = new RelayCommand(DeleteSheet, CanEditOrDelete);
            BuySheetCommand = new RelayCommand(BuySheet, CanEditOrDelete);
            SaveCommand = new RelayCommand(Save);
            ExportExcelCommand = new RelayCommand(ExportExcel);
            ImportExcelCommand = new RelayCommand(ImportExcel);
            UpdatePurchasePriceCommand = new RelayCommand(UpdatePurchasePrice);
            ClearFilterCommand = new RelayCommand(ClearFilter);
            ViewHistoryCommand = new RelayCommand(ViewHistory);

            LoadData();
            System.Diagnostics.Debug.WriteLine("SheetStoreViewModel: Constructor completed");
        }

        // ═══════════════════════════════════════════════════════
        // CLEANUP
        // ═══════════════════════════════════════════════════════
        public void Cleanup()
        {
            System.Diagnostics.Debug.WriteLine("SheetStoreViewModel: Cleanup - unsubscribing from DataChanged");
            SheetStoreService.Instance.DataChanged -= OnServiceDataChanged;
        }

        // ═══════════════════════════════════════════════════════
        // EVENT HANDLER
        // ═══════════════════════════════════════════════════════
        private void OnServiceDataChanged(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("SheetStoreViewModel: OnServiceDataChanged fired");

            if (Application.Current == null)
            {
                System.Diagnostics.Debug.WriteLine("SheetStoreViewModel: Application.Current is null, loading directly");
                LoadData();
                return;
            }

            if (Application.Current.Dispatcher.CheckAccess())
            {
                System.Diagnostics.Debug.WriteLine("SheetStoreViewModel: Loading on UI thread");
                LoadData();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("SheetStoreViewModel: Loading on dispatcher thread");
                Application.Current.Dispatcher.BeginInvoke(new Action(LoadData));
            }
        }

        // ═══════════════════════════════════════════════════════
        // LOAD DATA
        // ═══════════════════════════════════════════════════════
        private void LoadData()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("SheetStoreViewModel: LoadData started");

                var sheets = SheetStoreService.Instance.GetAllActive();
                AllSheets = new ObservableCollection<Sheet>(sheets);
                System.Diagnostics.Debug.WriteLine($"SheetStoreViewModel: Loaded {sheets.Count} sheets");

                Categories.Clear();
                foreach (var c in SheetStoreService.Instance.GetCategories())
                    Categories.Add(c);
                System.Diagnostics.Debug.WriteLine($"SheetStoreViewModel: Loaded {Categories.Count} categories");

                ApplyFilters();
                UpdateStats();

                System.Diagnostics.Debug.WriteLine("SheetStoreViewModel: LoadData completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SheetStoreViewModel: Error in LoadData: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════
        // APPLY FILTERS
        // ═══════════════════════════════════════════════════════
        private void ApplyFilters()
        {
            if (AllSheets == null)
            {
                FilteredSheets = new ObservableCollection<Sheet>();
                return;
            }

            var filtered = AllSheets.AsEnumerable();

            if (!string.IsNullOrEmpty(SelectedCategory))
            {
                filtered = filtered.Where(s => s.Category == SelectedCategory);
            }

            if (!string.IsNullOrEmpty(SearchText))
            {
                var search = SearchText.ToLower();
                filtered = filtered.Where(s =>
                    s.Category.ToLower().Contains(search) ||
                    s.Color.ToLower().Contains(search) ||
                    s.Thickness.ToLower().Contains(search) ||
                    (s.Supplier?.ToLower().Contains(search) ?? false));
            }

            FilteredSheets = new ObservableCollection<Sheet>(filtered);
        }

        // ═══════════════════════════════════════════════════════
        // UPDATE STATS
        // ═══════════════════════════════════════════════════════
        private void UpdateStats()
        {
            OnPropertyChanged(nameof(TotalSheets));
            OnPropertyChanged(nameof(TotalStock));
            OnPropertyChanged(nameof(TotalUsed));
            OnPropertyChanged(nameof(BalanceSheets));
            OnPropertyChanged(nameof(TotalAll));
            OnPropertyChanged(nameof(LastPurchase));
            OnPropertyChanged(nameof(LastUpdate));
            OnPropertyChanged(nameof(FilteredCount));
        }

        private string GetLastPurchase()
        {
            var sheet = AllSheets?.Where(s => s.LatestPurchaseDate.HasValue)
                                   .OrderByDescending(s => s.LatestPurchaseDate)
                                   .FirstOrDefault();
            return sheet != null ? $"{sheet.Thickness} {sheet.Color}" : "-";
        }

        private string GetLastUpdate()
        {
            var sheet = AllSheets?.OrderByDescending(s => s.CreatedDate).FirstOrDefault();
            return sheet?.DisplayDateTime ?? "-";
        }

        // ═══════════════════════════════════════════════════════
        // CRUD OPERATIONS
        // ═══════════════════════════════════════════════════════
        public void AddSheet()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("SheetStoreViewModel: AddSheet started");

                var dialog = new Views.SheetStore.SheetDialog(null);
                if (dialog.ShowDialog() == true)
                {
                    System.Diagnostics.Debug.WriteLine($"SheetStoreViewModel: Dialog returned, NewSheet={dialog.NewSheet?.Category}");
                    SheetStoreService.Instance.AddSheet(dialog.NewSheet);
                    System.Diagnostics.Debug.WriteLine("SheetStoreViewModel: AddSheet completed");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SheetStoreViewModel: AddSheet cancelled");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SheetStoreViewModel: Error adding sheet: {ex.Message}");
                MessageBox.Show($"Error adding sheet: {ex.Message}", "Error");
            }
        }

        public void EditSheet(object parameter = null)
        {
            try
            {
                Sheet? sheet = parameter as Sheet;
                if (sheet == null)
                    sheet = SelectedSheet;

                if (sheet == null)
                {
                    System.Diagnostics.Debug.WriteLine("SheetStoreViewModel: EditSheet - no sheet selected");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"SheetStoreViewModel: EditSheet started for ID={sheet.Id}");

                var dialog = new Views.SheetStore.SheetDialog(sheet);
                if (dialog.ShowDialog() == true)
                {
                    System.Diagnostics.Debug.WriteLine($"SheetStoreViewModel: Dialog returned, updating ID={dialog.NewSheet?.Id}");
                    SheetStoreService.Instance.UpdateSheet(dialog.NewSheet);
                    System.Diagnostics.Debug.WriteLine("SheetStoreViewModel: EditSheet completed");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SheetStoreViewModel: Error editing sheet: {ex.Message}");
                MessageBox.Show($"Error editing sheet: {ex.Message}", "Error");
            }
        }

        public void DeleteSheet(object parameter = null)
        {
            try
            {
                Sheet? sheet = parameter as Sheet;
                if (sheet == null)
                    sheet = SelectedSheet;

                if (sheet == null) return;

                var result = MessageBox.Show(
                    $"Delete '{sheet.Category} - {sheet.Thickness}mm {sheet.Color}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Debug.WriteLine($"SheetStoreViewModel: Deleting sheet ID={sheet.Id}");
                    SheetStoreService.Instance.DeleteSheet(sheet.Id);
                }
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
                Sheet? sheet = parameter as Sheet;
                if (sheet == null)
                    sheet = SelectedSheet;

                if (sheet == null) return;

                sheet.TotalStock++;
                sheet.LatestPurchaseDate = DateTime.Now;
                SheetStoreService.Instance.UpdateSheet(sheet);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error buying sheet: {ex.Message}", "Error");
            }
        }

        private bool CanEditOrDelete(object parameter = null)
        {
            if (parameter is Sheet) return true;
            return SelectedSheet != null;
        }

        public void Save()
        {
            UpdateStats();
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
                if (dialog.ShowDialog() == true)
                {
                    // DataChanged event will trigger LoadData()
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating prices: {ex.Message}", "Error");
            }
        }

        public void ClearFilter()
        {
            SelectedCategory = "";
            SearchText = "";
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

        // ═══════════════════════════════════════════════════════
        // PROPERTY NOTIFICATION
        // ═══════════════════════════════════════════════════════
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}