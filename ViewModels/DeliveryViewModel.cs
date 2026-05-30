using ProGlassAutomation.Data.Database;
using ProGlassAutomation.Models;

// Add these aliases to disambiguate:
using DbDelivery = ProGlassAutomation.Data.Database.Delivery;
using DbDeliveryItem = ProGlassAutomation.Data.Database.DeliveryItem;
using DbDailyWork = ProGlassAutomation.Data.Database.DailyWork;
using JobOrder = ProGlassAutomation.Models.JobOrder;

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ProGlassAutomation.ViewModels
{
    public class DeliveryViewModel : ViewModelBase
    {
        private ObservableCollection<DbDelivery> _deliveryOrders;
        private ObservableCollection<DbDailyWork> _sourceOrders;
        private DbDelivery _selectedOrder;
        private DbDeliveryItem _selectedDeliveryItem;
        private DataRowView _selectedDataRowView;
        private DataView _filteredDataView;
        private string _searchText = "";
        private string _sortColumn = "";
        private ListSortDirection _sortDirection = ListSortDirection.Ascending;
        private string _filterStatus = "";
        private string _filterTypeOfWork = "";
        private string _filterSalesman = "";
        private string _filterCompany = "";
        private string _filterColor = "";
        private DateTime? _filterStartDate;
        private DateTime? _filterEndDate;
        private bool _isEditing;
        private bool _isAddingDelivery;
        private bool _isViewingDetails;
        private DbDelivery _editingOrder;
        private DbDeliveryItem _editingDeliveryItem;
        private bool _isNewRecord;
        private bool _isInitialized;

        // Notes View Popup
        private bool _isViewingNotes = false;
        private string _viewNotesContent = "";

        // Delete Delivery Item Confirmation
        private bool _isDeletingDeliveryItem = false;
        private DbDeliveryItem _confirmDeleteItem;

        // Edit Delivery Item
        private bool _isEditingDeliveryItem = false;
        private DbDeliveryItem _editingDeliveryItemFromDb;

        // Import Log - Complete History
        private bool _isViewingImportLog = false;
        private ObservableCollection<ImportSession> _importHistory = new ObservableCollection<ImportSession>();
        private ImportSession _currentSession;

        // Selected IDs tracking
        private List<int> _selectedIds = new List<int>();

        // Options collections - Loaded from database
        public ObservableCollection<string> TypeOfWorkOptions { get; private set; }
        public ObservableCollection<string> StatusOptions { get; private set; }
        public ObservableCollection<string> SalesmanOptions { get; private set; }
        public ObservableCollection<string> CompanyOptions { get; private set; }
        public ObservableCollection<string> ColorOptions { get; private set; }

        // Driver and Vehicle - Dynamic collections (loaded from database)
        public ObservableCollection<string> DriverOptions { get; set; }
        public ObservableCollection<string> VehicleOptions { get; set; }

        public DeliveryViewModel()
        {
            // Initialize database FIRST
            InitializeDatabase();

            DeliveryOrders = new ObservableCollection<DbDelivery>();
            SourceOrders = new ObservableCollection<DbDailyWork>();
            TypeOfWorkOptions = new ObservableCollection<string>();
            StatusOptions = new ObservableCollection<string>();
            SalesmanOptions = new ObservableCollection<string>();
            CompanyOptions = new ObservableCollection<string>();
            ColorOptions = new ObservableCollection<string>();
            DriverOptions = new ObservableCollection<string>();
            VehicleOptions = new ObservableCollection<string>();

            AddNewCommand = new RelayCommand(ExecuteAddNew);
            EditCommand = new RelayCommand(ExecuteEdit, CanExecuteEdit);
            DeleteCommand = new RelayCommand(ExecuteDelete, CanExecuteDelete);
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);
            RefreshCommand = new RelayCommand(ExecuteRefresh);
            ImportConfirmedCommand = new RelayCommand(ExecuteImportConfirmed);
            ExportCommand = new RelayCommand(ExecuteExport);
            ClearFiltersCommand = new RelayCommand(ExecuteClearFilters);
            PrintCommand = new RelayCommand(ExecutePrint);
            AddDeliveryCommand = new RelayCommand(ExecuteAddDelivery, CanExecuteAddDelivery);
            SaveDeliveryCommand = new RelayCommand(ExecuteSaveDelivery);
            DeleteDeliveryItemCommand = new RelayCommand(ExecuteDeleteDeliveryItem, CanExecuteDeleteDeliveryItem);
            ViewDetailsCommand = new RelayCommand(ExecuteViewDetails, CanExecuteViewDetails);
            DeleteSelectedCommand = new RelayCommand(ExecuteDeleteSelected, CanExecuteDeleteSelected);

            // Notes View and Delete Confirmation Commands
            ViewNotesCommand = new RelayCommand(ExecuteViewNotes);
            CloseNotesCommand = new RelayCommand(ExecuteCloseNotes);
            CancelDeleteDeliveryItemCommand = new RelayCommand(ExecuteCancelDeleteDeliveryItem);
            ConfirmDeleteDeliveryItemCommand = new RelayCommand(ExecuteConfirmDeleteDeliveryItem);

            // Edit Delivery Item Commands
            EditDeliveryItemCommand = new RelayCommand(ExecuteEditDeliveryItem, CanExecuteEditDeliveryItem);
            SaveEditDeliveryItemCommand = new RelayCommand(ExecuteSaveEditDeliveryItem, CanExecuteSaveEditDeliveryItem);
            CancelEditDeliveryItemCommand = new RelayCommand(ExecuteCancelEditDeliveryItem);

            // Import Log Commands
            ViewImportLogCommand = new RelayCommand(ExecuteViewImportLog, CanExecuteViewImportLog);
            CloseImportLogCommand = new RelayCommand(ExecuteCloseImportLog);

            LoadDataFromDatabase();
            LoadOptionsFromDatabase();
            LoadSourceOrdersFromJson();
            CreateDataView();
        }

        #region Database Initialization

        private void InitializeDatabase()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[DeliveryViewModel] Initializing database...");
                DbHelper.Init();
                System.Diagnostics.Debug.WriteLine("[DeliveryViewModel] Database initialized successfully!");
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeliveryViewModel] Database initialization failed: {ex.Message}");
                MessageBox.Show(
                    $"Database initialization failed:\n\n{ex.Message}\n\n" +
                    $"Please check if the database file exists and is accessible.",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                _isInitialized = false;
            }
        }

        #endregion

        #region Properties

        public ObservableCollection<DbDelivery> DeliveryOrders
        {
            get => _deliveryOrders;
            set => SetProperty(ref _deliveryOrders, value);
        }

        public ObservableCollection<DbDailyWork> SourceOrders
        {
            get => _sourceOrders;
            set => SetProperty(ref _sourceOrders, value);
        }

        public DataView FilteredDataView
        {
            get => _filteredDataView;
            private set => SetProperty(ref _filteredDataView, value);
        }

        public DbDelivery SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                if (SetProperty(ref _selectedOrder, value))
                {
                    if (value != null)
                        LoadDeliveryItems(value);
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public DbDeliveryItem SelectedDeliveryItem
        {
            get => _selectedDeliveryItem;
            set
            {
                if (SetProperty(ref _selectedDeliveryItem, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        public DataRowView SelectedDataRowView
        {
            get => _selectedDataRowView;
            set
            {
                if (SetProperty(ref _selectedDataRowView, value))
                {
                    if (value != null)
                    {
                        int id = Convert.ToInt32(value["Id"]);
                        var order = DeliveryOrders.FirstOrDefault(w => w.Id == id);
                        if (order != null)
                        {
                            _selectedOrder = order;
                            OnPropertyChanged(nameof(SelectedOrder));
                            LoadDeliveryItems(order);
                            System.Diagnostics.Debug.WriteLine($"[DeliveryViewModel] Row selected: Id={id}, Company={order.Company}, Balance={order.Balance}");
                        }
                    }
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    ApplyFilters();
            }
        }

        public string FilterStatus
        {
            get => _filterStatus;
            set
            {
                if (SetProperty(ref _filterStatus, value))
                    ApplyFilters();
            }
        }

        public string FilterTypeOfWork
        {
            get => _filterTypeOfWork;
            set
            {
                if (SetProperty(ref _filterTypeOfWork, value))
                    ApplyFilters();
            }
        }

        public string FilterSalesman
        {
            get => _filterSalesman;
            set
            {
                if (SetProperty(ref _filterSalesman, value))
                    ApplyFilters();
            }
        }

        public string FilterCompany
        {
            get => _filterCompany;
            set
            {
                if (SetProperty(ref _filterCompany, value))
                    ApplyFilters();
            }
        }

        public string FilterColor
        {
            get => _filterColor;
            set
            {
                if (SetProperty(ref _filterColor, value))
                    ApplyFilters();
            }
        }

        public DateTime? FilterStartDate
        {
            get => _filterStartDate;
            set
            {
                if (SetProperty(ref _filterStartDate, value))
                    ApplyFilters();
            }
        }

        public DateTime? FilterEndDate
        {
            get => _filterEndDate;
            set
            {
                if (SetProperty(ref _filterEndDate, value))
                    ApplyFilters();
            }
        }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public bool IsAddingDelivery
        {
            get => _isAddingDelivery;
            set => SetProperty(ref _isAddingDelivery, value);
        }

        public bool IsViewingDetails
        {
            get => _isViewingDetails;
            set => SetProperty(ref _isViewingDetails, value);
        }

        public DbDelivery EditingOrder
        {
            get => _editingOrder;
            set => SetProperty(ref _editingOrder, value);
        }

        public DbDeliveryItem EditingDeliveryItem
        {
            get => _editingDeliveryItem;
            set => SetProperty(ref _editingDeliveryItem, value);
        }

        // Notes View Popup Properties
        public bool IsViewingNotes
        {
            get => _isViewingNotes;
            set => SetProperty(ref _isViewingNotes, value);
        }

        public string ViewNotesContent
        {
            get => _viewNotesContent;
            set => SetProperty(ref _viewNotesContent, value);
        }

        // Delete Delivery Item Confirmation Properties
        public bool IsDeletingDeliveryItem
        {
            get => _isDeletingDeliveryItem;
            set => SetProperty(ref _isDeletingDeliveryItem, value);
        }

        public DbDeliveryItem ConfirmDeleteItem
        {
            get => _confirmDeleteItem;
            set => SetProperty(ref _confirmDeleteItem, value);
        }

        // Edit Delivery Item Properties
        public bool IsEditingDeliveryItem
        {
            get => _isEditingDeliveryItem;
            set => SetProperty(ref _isEditingDeliveryItem, value);
        }

        public DbDeliveryItem EditingDeliveryItemFromDb
        {
            get => _editingDeliveryItemFromDb;
            set => SetProperty(ref _editingDeliveryItemFromDb, value);
        }

        // Import Log Properties
        public bool IsViewingImportLog
        {
            get => _isViewingImportLog;
            set => SetProperty(ref _isViewingImportLog, value);
        }

        public ObservableCollection<ImportSession> ImportHistory
        {
            get => _importHistory;
            set => SetProperty(ref _importHistory, value);
        }

        public ImportSession CurrentSession
        {
            get => _currentSession;
            set => SetProperty(ref _currentSession, value);
        }

        public string SortColumn
        {
            get => _sortColumn;
            set => SetProperty(ref _sortColumn, value);
        }

        public ListSortDirection SortDirection
        {
            get => _sortDirection;
            set => SetProperty(ref _sortDirection, value);
        }

        // Statistics
        private int _selectedCount;

        public int TotalRecords => DeliveryOrders?.Count ?? 0;
        public int TotalOrderQty => DeliveryOrders?.Sum(w => w.OrderQty) ?? 0;
        public int TotalDelivered => DeliveryOrders?.Sum(w => w.TotalDelivered) ?? 0;
        public int TotalReturned => DeliveryOrders?.Sum(w => w.TotalReturned) ?? 0;
        public int TotalBalance => DeliveryOrders?.Sum(w => w.Balance) ?? 0;
        public double TotalOrderSQM => DeliveryOrders?.Sum(w => w.OrderSQM) ?? 0;

        public int FilteredRecords => FilteredDataView?.Count ?? 0;
        public int FilteredOrderQty => FilteredDataView?.Cast<DataRowView>().Sum(x => Convert.ToInt32(x["OrderQty"])) ?? 0;
        public int FilteredDelivered => FilteredDataView?.Cast<DataRowView>().Sum(x => Convert.ToInt32(x["TotalDelivered"])) ?? 0;
        public int FilteredReturned => FilteredDataView?.Cast<DataRowView>().Sum(x => Convert.ToInt32(x["TotalReturned"])) ?? 0;
        public int FilteredBalance => FilteredDataView?.Cast<DataRowView>().Sum(x => Convert.ToInt32(x["Balance"])) ?? 0;
        public double FilteredOrderSQM => FilteredDataView?.Cast<DataRowView>().Sum(x => Convert.ToDouble(x["OrderSQM"])) ?? 0;

        public int SelectedCount
        {
            get => _selectedCount;
            private set
            {
                if (_selectedCount != value)
                {
                    _selectedCount = value;
                    OnPropertyChanged(nameof(SelectedCount));
                }
            }
        }

        public int SelectedRecords => _selectedCount == 0 ? TotalRecords : _selectedCount;
        public int SelectedOrderQty => _selectedCount == 0 ? TotalOrderQty : GetSelectedSum(w => w.OrderQty);
        public int SelectedDelivered => _selectedCount == 0 ? TotalDelivered : GetSelectedSum(w => w.TotalDelivered);
        public int SelectedReturned => _selectedCount == 0 ? TotalReturned : GetSelectedSum(w => w.TotalReturned);
        public int SelectedBalance => _selectedCount == 0 ? TotalBalance : GetSelectedSum(w => w.Balance);
        public double SelectedOrderSQM => _selectedCount == 0 ? TotalOrderSQM : GetSelectedDoubleSum(w => w.OrderSQM);

        private int GetSelectedSum(Func<DbDelivery, int> selector)
        {
            if (_selectedCount == 0) return TotalOrderQty;
            return DeliveryOrders?.Where(w => _selectedIds.Contains(w.Id)).Sum(selector) ?? 0;
        }

        private double GetSelectedDoubleSum(Func<DbDelivery, double> selector)
        {
            if (_selectedCount == 0) return TotalOrderSQM;
            return DeliveryOrders?.Where(w => _selectedIds.Contains(w.Id)).Sum(selector) ?? 0;
        }

        public void SetSelectedCount(int count)
        {
            SelectedCount = count;
            RefreshSelectedStats();
        }

        public void UpdateSelectedIds(List<int> ids)
        {
            _selectedIds = ids;
            SelectedCount = ids.Count;
            RefreshSelectedStats();
        }

        private void RefreshSelectedStats()
        {
            RefreshStatisticsProperties();
        }

        private void UpdateAllStats()
        {
            RefreshSelectedStats();
        }

        #endregion

        #region Commands

        public ICommand AddNewCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ImportConfirmedCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand AddDeliveryCommand { get; }
        public ICommand SaveDeliveryCommand { get; }
        public ICommand DeleteDeliveryItemCommand { get; }
        public ICommand ViewDetailsCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        public ICommand ViewNotesCommand { get; }
        public ICommand CloseNotesCommand { get; }
        public ICommand CancelDeleteDeliveryItemCommand { get; }
        public ICommand ConfirmDeleteDeliveryItemCommand { get; }
        public ICommand EditDeliveryItemCommand { get; }
        public ICommand SaveEditDeliveryItemCommand { get; }
        public ICommand CancelEditDeliveryItemCommand { get; }
        public ICommand ViewImportLogCommand { get; }
        public ICommand CloseImportLogCommand { get; }

        #endregion

        #region Helper Methods

        // PATCH-04: Centralize Order Selection
        private DbDelivery GetCurrentOrder()
        {
            if (SelectedDataRowView != null)
            {
                int id = Convert.ToInt32(SelectedDataRowView["Id"]);
                return DeliveryOrders.FirstOrDefault(x => x.Id == id);
            }
            return SelectedOrder;
        }

        // PATCH-07: Centralize Statistics Property Refresh
        private void RefreshStatisticsProperties()
        {
            OnPropertyChanged(nameof(TotalRecords));
            OnPropertyChanged(nameof(TotalOrderQty));
            OnPropertyChanged(nameof(TotalDelivered));
            OnPropertyChanged(nameof(TotalReturned));
            OnPropertyChanged(nameof(TotalBalance));
            OnPropertyChanged(nameof(TotalOrderSQM));

            OnPropertyChanged(nameof(FilteredRecords));
            OnPropertyChanged(nameof(FilteredOrderQty));
            OnPropertyChanged(nameof(FilteredDelivered));
            OnPropertyChanged(nameof(FilteredReturned));
            OnPropertyChanged(nameof(FilteredBalance));
            OnPropertyChanged(nameof(FilteredOrderSQM));

            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(SelectedRecords));
            OnPropertyChanged(nameof(SelectedOrderQty));
            OnPropertyChanged(nameof(SelectedDelivered));
            OnPropertyChanged(nameof(SelectedReturned));
            OnPropertyChanged(nameof(SelectedBalance));
            OnPropertyChanged(nameof(SelectedOrderSQM));
        }

        // PATCH-10: Safe Database Operation Wrapper
        private bool ExecuteDbOperation(Action action, string operationName)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{operationName} failed:\n\n{ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        // PATCH-06: CSV Injection Prevention
        private string SanitizeCsv(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            if (value.StartsWith("=") ||
                value.StartsWith("+") ||
                value.StartsWith("-") ||
                value.StartsWith("@"))
            {
                return "'" + value;
            }

            return value;
        }

        // PATCH-11: Centralize Option Updates
        private void UpdateLookupOptions(DbDelivery order)
        {
            AddToOptionsIfNew(TypeOfWorkOptions, order.TypeOfWork);
            AddToOptionsIfNew(StatusOptions, order.Status);
            AddToOptionsIfNew(SalesmanOptions, order.Salesman);
            AddToOptionsIfNew(CompanyOptions, order.Company);
            AddToOptionsIfNew(ColorOptions, order.Color);
        }

        private void AddToOptionsIfNew(ObservableCollection<string> collection, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (!collection.Contains(value))
            {
                collection.Add(value);
            }
        }

        #endregion

        #region Load Options From Database

        private void LoadOptionsFromDatabase()
        {
            try
            {
                TypeOfWorkOptions.Clear();
                StatusOptions.Clear();
                SalesmanOptions.Clear();
                CompanyOptions.Clear();
                ColorOptions.Clear();
                DriverOptions.Clear();
                VehicleOptions.Clear();

                var typeOfWorkOptions = DbHelper.GetAllTypeOfWorkOptions();
                foreach (var option in typeOfWorkOptions)
                {
                    if (!string.IsNullOrWhiteSpace(option))
                        TypeOfWorkOptions.Add(option);
                }

                var statusOptions = DbHelper.GetAllDeliveryStatusOptions();
                foreach (var option in statusOptions)
                {
                    if (!string.IsNullOrWhiteSpace(option))
                        StatusOptions.Add(option);
                }

                var salesmanOptions = DbHelper.GetAllSalesmanOptions();
                foreach (var option in salesmanOptions)
                {
                    if (!string.IsNullOrWhiteSpace(option))
                        SalesmanOptions.Add(option);
                }

                var companyOptions = DbHelper.GetAllCompanyOptions();
                foreach (var option in companyOptions)
                {
                    if (!string.IsNullOrWhiteSpace(option))
                        CompanyOptions.Add(option);
                }

                var colorOptions = DbHelper.GetAllColorOptions();
                foreach (var option in colorOptions)
                {
                    if (!string.IsNullOrWhiteSpace(option))
                        ColorOptions.Add(option);
                }

                var driverOptions = DbHelper.GetAllDriverOptions();
                foreach (var option in driverOptions)
                {
                    if (!string.IsNullOrWhiteSpace(option))
                        DriverOptions.Add(option);
                }

                var vehicleOptions = DbHelper.GetAllVehicleOptions();
                foreach (var option in vehicleOptions)
                {
                    if (!string.IsNullOrWhiteSpace(option))
                        VehicleOptions.Add(option);
                }

                System.Diagnostics.Debug.WriteLine($"[DeliveryViewModel] Loaded options: TypeOfWork={TypeOfWorkOptions.Count}, Status={StatusOptions.Count}, Salesman={SalesmanOptions.Count}, Company={CompanyOptions.Count}, Color={ColorOptions.Count}, Driver={DriverOptions.Count}, Vehicle={VehicleOptions.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeliveryViewModel] Load options error: {ex.Message}");
            }
        }

        #endregion

        #region Load Data From Database

        private void LoadDataFromDatabase()
        {
            if (!_isInitialized)
            {
                System.Diagnostics.Debug.WriteLine("[DeliveryViewModel] Database not initialized, skipping load.");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("[DeliveryViewModel] Loading data from database...");

                if (!DbHelper.TestConnection())
                {
                    MessageBox.Show("Cannot connect to database. Please restart the application.",
                        "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dbDeliveries = DbHelper.GetAllDeliveries();
                System.Diagnostics.Debug.WriteLine($"[DeliveryViewModel] Loaded {dbDeliveries.Count} deliveries");

                DeliveryOrders.Clear();
                foreach (var delivery in dbDeliveries)
                {
                    var items = DbHelper.GetDeliveryItems(delivery.Id);
                    var itemCollection = new ObservableCollection<DbDeliveryItem>(items);
                    delivery.DeliveryItems = itemCollection;
                    UpdateOrderStatus(delivery);
                    DeliveryOrders.Add(delivery);
                }

                LoadUniqueDriversAndVehicles();

                System.Diagnostics.Debug.WriteLine("[DeliveryViewModel] Data loading complete!");
                UpdateAllStats();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeliveryViewModel] Error loading data: {ex.Message}");
                MessageBox.Show($"Error loading data from database:\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // PATCH-08 & PATCH-09: Load Source Orders from JSON files with HashSet and no debug spam
        private void LoadSourceOrdersFromJson()
        {
            System.Diagnostics.Debug.WriteLine("[DeliveryViewModel] === LoadSourceOrdersFromJson START ===");

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string dataFolder = System.IO.Path.Combine(baseDir, "Data");

                if (!System.IO.Directory.Exists(dataFolder))
                {
                    System.Diagnostics.Debug.WriteLine("[DeliveryViewModel] Data folder does not exist for JSON loading");
                    return;
                }

                // Get only PI-*.json files
                var allJsonFiles = System.IO.Directory.GetFiles(dataFolder, "*.json");
                var jsonFiles = allJsonFiles.Where(f =>
                {
                    string fileName = System.IO.Path.GetFileName(f);
                    return fileName.StartsWith("PI-", StringComparison.OrdinalIgnoreCase);
                }).ToArray();

                System.Diagnostics.Debug.WriteLine($"[DeliveryViewModel] Found {jsonFiles.Length} JSON files for SourceOrders");

                if (jsonFiles.Length == 0)
                {
                    return;
                }

                // IMPORTANT: Clear existing SourceOrders to prevent duplicates on refresh
                SourceOrders.Clear();

                // PATCH-08: Use HashSet for O(1) duplicate detection
                var existingPiNumbers = new HashSet<string>(
                    SourceOrders.Select(x => x.PINumber?.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x)),
                    StringComparer.OrdinalIgnoreCase);

                var jsonSettings = new Newtonsoft.Json.JsonSerializerSettings
                {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                    DateFormatString = "yyyy-MM-ddTHH:mm:ss"
                };

                int addedCount = 0;
                int skippedCount = 0;

                foreach (var jsonPath in jsonFiles)
                {
                    string fileName = System.IO.Path.GetFileName(jsonPath);

                    try
                    {
                        var json = System.IO.File.ReadAllText(jsonPath);

                        if (string.IsNullOrWhiteSpace(json))
                        {
                            skippedCount++;
                            continue;
                        }

                        var work = Newtonsoft.Json.JsonConvert.DeserializeObject<DbDailyWork>(json, jsonSettings);

                        if (work == null)
                        {
                            skippedCount++;
                            continue;
                        }

                        string jsonPINumber = (work.PINumber ?? "").Trim();
                        string jsonCustRef = (work.CustomerReference ?? "").Trim();

                        // PATCH-08: Use HashSet for faster lookup
                        if (existingPiNumbers.Contains(jsonPINumber))
                        {
                            skippedCount++;
                            continue;
                        }

                        // NEW entry - add it
                        SourceOrders.Add(work);
                        existingPiNumbers.Add(jsonPINumber); // Add to HashSet
                        addedCount++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DeliveryViewModel] Error reading {fileName}: {ex.Message}");
                        skippedCount++;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[DeliveryViewModel] SourceOrders JSON load complete: {addedCount} added, {skippedCount} skipped");
                System.Diagnostics.Debug.WriteLine($"[DeliveryViewModel] SourceOrders Total Count: {SourceOrders.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeliveryViewModel] LoadSourceOrdersFromJson error: {ex.Message}");
            }

            System.Diagnostics.Debug.WriteLine("[DeliveryViewModel] === LoadSourceOrdersFromJson END ===");
        }

        private void LoadDeliveryItems(DbDelivery order)
        {
            if (order?.DeliveryItems == null) return;

            try
            {
                var items = DbHelper.GetDeliveryItems(order.Id);
                order.DeliveryItems.Clear();
                foreach (var item in items)
                {
                    order.DeliveryItems.Add(item);
                }
                UpdateOrderStatus(order);
                System.Diagnostics.Debug.WriteLine($"[DeliveryViewModel] Loaded {items.Count} delivery items for order {order.Id}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading delivery items: {ex.Message}",
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadUniqueDriversAndVehicles()
        {
            try
            {
                var allDeliveryItems = new List<DbDeliveryItem>();
                foreach (var order in DeliveryOrders)
                {
                    foreach (var item in order.DeliveryItems)
                    {
                        allDeliveryItems.Add(item);
                    }
                }

                var uniqueDrivers = allDeliveryItems
                    .Where(x => !string.IsNullOrWhiteSpace(x.Driver))
                    .Select(x => x.Driver.Trim())
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                foreach (var driver in uniqueDrivers)
                {
                    if (!DriverOptions.Contains(driver))
                        DriverOptions.Add(driver);
                }

                var uniqueVehicles = allDeliveryItems
                    .Where(x => !string.IsNullOrWhiteSpace(x.Vehicle))
                    .Select(x => x.Vehicle.Trim())
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                foreach (var vehicle in uniqueVehicles)
                {
                    if (!VehicleOptions.Contains(vehicle))
                        VehicleOptions.Add(vehicle);
                }

                System.Diagnostics.Debug.WriteLine($"[DeliveryViewModel] Loaded {DriverOptions.Count} drivers and {VehicleOptions.Count} vehicles");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeliveryViewModel] Error loading drivers/vehicles: {ex.Message}");
            }
        }

        #endregion

        #region Import from Confirmed Orders

        private void ExecuteImportConfirmed(object parameter)
        {
            try
            {
                var confirmedOrders = SourceOrders
                    .Where(w => w.Status == "Confirmed")
                    .ToList();

                if (confirmedOrders.Count == 0)
                {
                    MessageBox.Show("No confirmed orders found in Daily Works to import.",
                        "Import", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int importedCount = 0;
                int updatedCount = 0;
                int skippedCount = 0;

                var session = new ImportSession
                {
                    SessionDateTime = DateTime.Now,
                    Notes = "",
                    Entries = new ObservableCollection<ImportLog>()
                };

                foreach (var sourceOrder in confirmedOrders)
                {
                    var existingDelivery = DeliveryOrders.FirstOrDefault(d =>
                        d.PINumber == sourceOrder.PINumber &&
                        !string.IsNullOrEmpty(sourceOrder.PINumber));

                    if (existingDelivery != null)
                    {
                        bool hasChanges =
                            existingDelivery.OrderQty != sourceOrder.Qty ||
                            existingDelivery.OrderSQM != sourceOrder.SQM ||
                            existingDelivery.Notes != sourceOrder.Notes ||
                            existingDelivery.Company != sourceOrder.Company ||
                            existingDelivery.TypeOfWork != sourceOrder.TypeOfWork ||
                            existingDelivery.Salesman != sourceOrder.Salesman ||
                            existingDelivery.Color != sourceOrder.Color;

                        if (hasChanges)
                        {
                            var changes = new ObservableCollection<ImportLogItem>();

                            if (existingDelivery.OrderQty != sourceOrder.Qty)
                                changes.Add(new ImportLogItem { FieldName = "Order Qty", OldValue = existingDelivery.OrderQty.ToString(), NewValue = sourceOrder.Qty.ToString() });
                            if (existingDelivery.OrderSQM != sourceOrder.SQM)
                                changes.Add(new ImportLogItem { FieldName = "Order SQM", OldValue = existingDelivery.OrderSQM.ToString("N2"), NewValue = sourceOrder.SQM.ToString("N2") });
                            if (existingDelivery.Company != sourceOrder.Company)
                                changes.Add(new ImportLogItem { FieldName = "Company", OldValue = existingDelivery.Company, NewValue = sourceOrder.Company ?? "" });
                            if (existingDelivery.TypeOfWork != sourceOrder.TypeOfWork)
                                changes.Add(new ImportLogItem { FieldName = "Type of Work", OldValue = existingDelivery.TypeOfWork, NewValue = sourceOrder.TypeOfWork ?? "" });
                            if (existingDelivery.Salesman != sourceOrder.Salesman)
                                changes.Add(new ImportLogItem { FieldName = "Salesman", OldValue = existingDelivery.Salesman, NewValue = sourceOrder.Salesman ?? "" });
                            if (existingDelivery.Color != sourceOrder.Color)
                                changes.Add(new ImportLogItem { FieldName = "Color", OldValue = existingDelivery.Color ?? "", NewValue = sourceOrder.Color ?? "" });
                            if (existingDelivery.Notes != sourceOrder.Notes)
                                changes.Add(new ImportLogItem { FieldName = "Notes", OldValue = existingDelivery.Notes ?? "", NewValue = sourceOrder.Notes ?? "" });

                            session.Entries.Add(new ImportLog
                            {
                                ImportDateTime = DateTime.Now,
                                PINumber = existingDelivery.PINumber,
                                Company = existingDelivery.Company,
                                Changes = changes
                            });

                            existingDelivery.OrderQty = sourceOrder.Qty;
                            existingDelivery.OrderSQM = sourceOrder.SQM;
                            existingDelivery.Company = sourceOrder.Company ?? "";
                            existingDelivery.TypeOfWork = sourceOrder.TypeOfWork ?? "";
                            existingDelivery.Salesman = sourceOrder.Salesman ?? "";
                            existingDelivery.Color = sourceOrder.Color ?? "";
                            existingDelivery.Notes = sourceOrder.Notes ?? "";
                            existingDelivery.UpdatedDate = DateTime.Today;
                            UpdateOrderStatus(existingDelivery);

                            DbHelper.UpdateDelivery(existingDelivery);
                            updatedCount++;
                        }
                        else
                        {
                            skippedCount++;
                        }
                        continue;
                    }

                    // PATCH-02: Remove manual ID generation - database will use AUTOINCREMENT
                    var newDelivery = new DbDelivery
                    {
                        SourceId = sourceOrder.Id,
                        Date = sourceOrder.Date,
                        Company = sourceOrder.Company ?? "",
                        PINumber = sourceOrder.PINumber ?? "",
                        CustomerReference = sourceOrder.CustomerReference ?? "",
                        TypeOfWork = sourceOrder.TypeOfWork ?? "",
                        OrderQty = sourceOrder.Qty,
                        OrderSQM = sourceOrder.SQM,
                        Salesman = sourceOrder.Salesman ?? "",
                        Color = sourceOrder.Color ?? "",
                        ProductionStatus = sourceOrder.ProductionStatus ?? "",
                        Status = "Pending",
                        Notes = sourceOrder.Notes ?? "",
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now,
                        DeliveryItems = new ObservableCollection<DbDeliveryItem>()
                    };

                    DbHelper.SaveDelivery(newDelivery);
                    DeliveryOrders.Add(newDelivery);
                    importedCount++;
                }

                if (session.Entries.Count > 0 || importedCount > 0)
                {
                    ImportHistory.Insert(0, session);
                    CurrentSession = session;
                }

                RefreshDataView();
                UpdateAllStats();

                string message = $"Import Complete!\n\n";
                message += $"New: {importedCount} orders\n";
                message += $"Updated: {updatedCount} orders\n";
                message += $"Skipped (no changes): {skippedCount} orders";

                MessageBox.Show(message, "Import", MessageBoxButton.OK, MessageBoxImage.Information);

                if (updatedCount > 0)
                {
                    IsViewingImportLog = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing orders: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Filter Implementation

        private void ApplyFilters()
        {
            if (FilteredDataView == null) return;
            var filterExpressions = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.Replace("'", "''");
                filterExpressions.Add($"(Company LIKE '%{searchLower}%' OR PINumber LIKE '%{searchLower}%' OR Salesman LIKE '%{searchLower}%' OR Notes LIKE '%{searchLower}%')");
            }

            if (!string.IsNullOrWhiteSpace(FilterStatus))
                filterExpressions.Add($"Status = '{FilterStatus}'");

            if (!string.IsNullOrWhiteSpace(FilterTypeOfWork))
                filterExpressions.Add($"TypeOfWork = '{FilterTypeOfWork}'");

            if (!string.IsNullOrWhiteSpace(FilterSalesman))
                filterExpressions.Add($"Salesman = '{FilterSalesman}'");

            if (!string.IsNullOrWhiteSpace(FilterCompany))
                filterExpressions.Add($"Company = '{FilterCompany}'");

            if (!string.IsNullOrWhiteSpace(FilterColor))
                filterExpressions.Add($"Color = '{FilterColor}'");

            if (FilterStartDate.HasValue)
                filterExpressions.Add($"Date >= #{FilterStartDate.Value:yyyy-MM-dd}#");

            if (FilterEndDate.HasValue)
                filterExpressions.Add($"Date <= #{FilterEndDate.Value:yyyy-MM-dd}#");

            FilteredDataView.RowFilter = filterExpressions.Count > 0 ? string.Join(" AND ", filterExpressions) : "";

            if (!string.IsNullOrEmpty(SortColumn))
                FilteredDataView.Sort = $"{SortColumn} {(SortDirection == ListSortDirection.Ascending ? "ASC" : "DESC")}";

            UpdateAllStats();
        }

        #endregion

        #region DataView

        private void CreateDataView()
        {
            var dataTable = new DataTable("Deliveries");
            dataTable.Columns.Add("Id", typeof(int));
            dataTable.Columns.Add("Date", typeof(DateTime));
            dataTable.Columns.Add("Company", typeof(string));
            dataTable.Columns.Add("PINumber", typeof(string));
            dataTable.Columns.Add("TypeOfWork", typeof(string));
            dataTable.Columns.Add("Color", typeof(string));
            dataTable.Columns.Add("OrderQty", typeof(int));
            dataTable.Columns.Add("TotalDelivered", typeof(int));
            dataTable.Columns.Add("TotalReturned", typeof(int));
            dataTable.Columns.Add("Balance", typeof(int));
            dataTable.Columns.Add("OrderSQM", typeof(double));
            dataTable.Columns.Add("Salesman", typeof(string));
            dataTable.Columns.Add("Status", typeof(string));
            dataTable.Columns.Add("Notes", typeof(string));

            foreach (var order in DeliveryOrders)
            {
                var row = dataTable.NewRow();
                row["Id"] = order.Id;
                row["Date"] = order.Date;
                row["Company"] = order.Company ?? "";
                row["PINumber"] = order.PINumber ?? "";
                row["TypeOfWork"] = order.TypeOfWork ?? "";
                row["Color"] = order.Color ?? "";
                row["OrderQty"] = order.OrderQty;
                row["TotalDelivered"] = order.TotalDelivered;
                row["TotalReturned"] = order.TotalReturned;
                row["Balance"] = order.Balance;
                row["OrderSQM"] = order.OrderSQM;
                row["Salesman"] = order.Salesman ?? "";
                row["Status"] = order.Status ?? "";
                row["Notes"] = order.Notes ?? "";
                dataTable.Rows.Add(row);
            }

            FilteredDataView = dataTable.DefaultView;
        }

        private void RefreshDataView()
        {
            if (FilteredDataView == null) return;
            var currentSort = FilteredDataView.Sort;
            var currentFilter = FilteredDataView.RowFilter;
            CreateDataView();
            if (!string.IsNullOrEmpty(currentSort)) FilteredDataView.Sort = currentSort;
            if (!string.IsNullOrEmpty(currentFilter)) FilteredDataView.RowFilter = currentFilter;
        }

        private void UpdateDataTableRow(DbDelivery order)
        {
            if (FilteredDataView == null) return;

            try
            {
                var dataTable = FilteredDataView.Table;
                if (dataTable == null) return;

                foreach (DataRow row in dataTable.Rows)
                {
                    if (Convert.ToInt32(row["Id"]) == order.Id)
                    {
                        row.BeginEdit();
                        row["Date"] = order.Date;
                        row["Company"] = order.Company ?? "";
                        row["PINumber"] = order.PINumber ?? "";
                        row["TypeOfWork"] = order.TypeOfWork ?? "";
                        row["Color"] = order.Color ?? "";
                        row["OrderQty"] = order.OrderQty;
                        row["TotalDelivered"] = order.TotalDelivered;
                        row["TotalReturned"] = order.TotalReturned;
                        row["Balance"] = order.Balance;
                        row["OrderSQM"] = order.OrderSQM;
                        row["Salesman"] = order.Salesman ?? "";
                        row["Status"] = order.Status ?? "";
                        row["Notes"] = order.Notes ?? "";
                        row.EndEdit();

                        RefreshStatisticsProperties();
                        System.Diagnostics.Debug.WriteLine($"[DeliveryViewModel] DataTable row updated for order {order.Id}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeliveryViewModel] UpdateDataTableRow error: {ex.Message}");
                RefreshDataView();
            }
        }

        private void UpdateOrderStatus(DbDelivery order)
        {
            if (order.Balance <= 0)
                order.Status = "Completed";
            else if (order.TotalDelivered > 0)
                order.Status = "Partially Delivered";
            else
                order.Status = "Pending";
        }

        #endregion

        #region Command Implementations

        private void ExecuteAddNew(object parameter)
        {
            _isNewRecord = true;
            EditingOrder = new DbDelivery
            {
                Date = DateTime.Today,
                Status = "Pending",
                OrderQty = 0,
                OrderSQM = 0,
                DeliveryItems = new ObservableCollection<DbDeliveryItem>()
            };
            IsEditing = true;
        }

        // PATCH-01 & PATCH-04: Fixed shared collection reference, use GetCurrentOrder()
        private void ExecuteEdit(object parameter)
        {
            var orderToEdit = GetCurrentOrder();

            if (orderToEdit != null)
            {
                _isNewRecord = false;
                // PATCH-01: Create new collection instead of sharing reference
                EditingOrder = new DbDelivery
                {
                    Id = orderToEdit.Id,
                    SourceId = orderToEdit.SourceId,
                    Date = orderToEdit.Date,
                    Company = orderToEdit.Company,
                    PINumber = orderToEdit.PINumber,
                    CustomerReference = orderToEdit.CustomerReference,
                    TypeOfWork = orderToEdit.TypeOfWork,
                    OrderQty = orderToEdit.OrderQty,
                    OrderSQM = orderToEdit.OrderSQM,
                    Salesman = orderToEdit.Salesman,
                    Color = orderToEdit.Color,
                    Status = orderToEdit.Status,
                    Notes = orderToEdit.Notes,
                    CreatedDate = orderToEdit.CreatedDate,
                    UpdatedDate = DateTime.Now,
                    DeliveryItems = new ObservableCollection<DbDeliveryItem>(
                        orderToEdit.DeliveryItems.Select(x => new DbDeliveryItem
                        {
                            Id = x.Id,
                            OrderId = x.OrderId,
                            DeliveryDate = x.DeliveryDate,
                            DeliveredQty = x.DeliveredQty,
                            ReturnedQty = x.ReturnedQty,
                            DeliveredSQM = x.DeliveredSQM,
                            ReturnedSQM = x.ReturnedSQM,
                            Driver = x.Driver,
                            Vehicle = x.Vehicle,
                            Notes = x.Notes,
                            CreatedDate = x.CreatedDate
                        }))
                };
                IsEditing = true;
            }
        }

        private bool CanExecuteEdit(object parameter)
        {
            return parameter != null || SelectedDataRowView != null || SelectedOrder != null;
        }

        // PATCH-04: Use GetCurrentOrder()
        private void ExecuteDelete(object parameter)
        {
            var orderToDelete = GetCurrentOrder();

            if (orderToDelete != null)
            {
                var result = MessageBox.Show(
                    $"Delete delivery order for {orderToDelete.Company}?\nPI: {orderToDelete.PINumber}\n\nThis will also delete all delivery items.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    if (ExecuteDbOperation(() => DbHelper.DeleteDelivery(orderToDelete.Id), "Delete Delivery"))
                    {
                        DeliveryOrders.Remove(orderToDelete);
                        RefreshDataView();
                        UpdateAllStats();
                        SelectedOrder = null;
                        SelectedDataRowView = null;
                    }
                }
            }
        }

        private bool CanExecuteDelete(object parameter)
        {
            return parameter != null || SelectedDataRowView != null || SelectedOrder != null;
        }

        private void ExecuteSave(object parameter)
        {
            if (EditingOrder == null) return;

            if (string.IsNullOrWhiteSpace(EditingOrder.Company))
            {
                MessageBox.Show("Company name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_isNewRecord)
            {
                EditingOrder.CreatedDate = DateTime.Today;
                EditingOrder.UpdatedDate = DateTime.Today;

                DbHelper.SaveDelivery(EditingOrder);

                // PATCH-11: Use centralized option update
                UpdateLookupOptions(EditingOrder);

                DeliveryOrders.Add(EditingOrder);
            }
            else
            {
                var existing = DeliveryOrders.FirstOrDefault(w => w.Id == EditingOrder.Id);
                if (existing != null)
                {
                    existing.Company = EditingOrder.Company;
                    existing.PINumber = EditingOrder.PINumber;
                    existing.CustomerReference = EditingOrder.CustomerReference;
                    existing.TypeOfWork = EditingOrder.TypeOfWork;
                    existing.OrderQty = EditingOrder.OrderQty;
                    existing.OrderSQM = EditingOrder.OrderSQM;
                    existing.Salesman = EditingOrder.Salesman;
                    existing.Color = EditingOrder.Color;
                    existing.Status = EditingOrder.Status;
                    existing.Notes = EditingOrder.Notes;
                    existing.UpdatedDate = DateTime.Today;
                    UpdateOrderStatus(existing);

                    DbHelper.UpdateDelivery(existing);

                    // PATCH-11: Use centralized option update
                    UpdateLookupOptions(EditingOrder);
                }
            }

            IsEditing = false;
            EditingOrder = null;
            RefreshDataView();
            UpdateAllStats();
        }

        private bool CanExecuteSave(object parameter) => EditingOrder != null;

        private void ExecuteCancel(object parameter)
        {
            IsEditing = false;
            IsAddingDelivery = false;
            IsViewingDetails = false;
            IsViewingNotes = false;
            IsDeletingDeliveryItem = false;
            IsEditingDeliveryItem = false;
            IsViewingImportLog = false;
            EditingOrder = null;
            EditingDeliveryItem = null;
            EditingDeliveryItemFromDb = null;
            ViewNotesContent = "";
            ConfirmDeleteItem = null;
        }

        private void ExecuteRefresh(object parameter)
        {
            LoadDataFromDatabase();
            LoadOptionsFromDatabase();
            LoadSourceOrdersFromJson();
            RefreshDataView();
            UpdateAllStats();
        }

        // PATCH-04: Use GetCurrentOrder()
        private void ExecuteViewDetails(object parameter)
        {
            var orderToView = GetCurrentOrder();

            if (orderToView != null)
            {
                SelectedOrder = orderToView;
                IsViewingDetails = true;
            }
        }

        private bool CanExecuteViewDetails(object parameter)
        {
            return parameter != null || SelectedDataRowView != null || SelectedOrder != null;
        }

        private void ExecuteAddDelivery(object parameter)
        {
            DbDelivery order = SelectedOrder;

            if (order != null && order.Balance > 0)
            {
                EditingDeliveryItem = new DbDeliveryItem
                {
                    OrderId = order.Id,
                    DeliveryDate = DateTime.Today,
                    DeliveredQty = 0,
                    ReturnedQty = 0,
                    DeliveredSQM = 0,
                    ReturnedSQM = 0,
                    Driver = "",
                    Vehicle = ""
                };
                IsAddingDelivery = true;
            }
            else
            {
                MessageBox.Show("No balance remaining. Order is fully delivered.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool CanExecuteAddDelivery(object parameter)
        {
            return SelectedOrder != null && SelectedOrder.Balance > 0;
        }

        // PATCH-03: Divide by zero protection, PATCH-12: Negative value check, PATCH-13: Null protection
        private void ExecuteSaveDelivery(object parameter)
        {
            if (EditingDeliveryItem == null || SelectedOrder == null) return;

            if (SelectedOrder?.DeliveryItems == null)
            {
                MessageBox.Show("Order delivery items not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (EditingDeliveryItem.DeliveredQty <= 0)
            {
                MessageBox.Show("Delivered quantity must be greater than 0.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // PATCH-12: Check for negative returned quantity
            if (EditingDeliveryItem.ReturnedQty < 0)
            {
                MessageBox.Show("Returned quantity cannot be negative.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // PATCH-03: Divide by zero protection
            if (SelectedOrder.OrderQty <= 0)
            {
                MessageBox.Show("Order Quantity must be greater than zero.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(EditingDeliveryItem.Driver))
            {
                var driverTrimmed = EditingDeliveryItem.Driver.Trim();
                if (!DriverOptions.Contains(driverTrimmed))
                    DriverOptions.Add(driverTrimmed);
            }

            if (!string.IsNullOrWhiteSpace(EditingDeliveryItem.Vehicle))
            {
                var vehicleTrimmed = EditingDeliveryItem.Vehicle.Trim();
                if (!VehicleOptions.Contains(vehicleTrimmed))
                    VehicleOptions.Add(vehicleTrimmed);
            }

            var totalAfterDelivery = SelectedOrder.TotalDelivered + EditingDeliveryItem.DeliveredQty - SelectedOrder.TotalReturned;
            var balance = SelectedOrder.OrderQty - totalAfterDelivery;

            if (balance < 0)
            {
                var result = MessageBox.Show(
                    $"Delivered quantity ({EditingDeliveryItem.DeliveredQty}) will exceed order quantity.\n\n" +
                    $"Current Balance: {SelectedOrder.Balance}\n" +
                    $"After Delivery: {balance} (negative)\n\n" +
                    "Do you want to continue?",
                    "Warning",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;
            }

            // PATCH-03: Safe SQM calculation with divide by zero check
            EditingDeliveryItem.DeliveredSQM = SelectedOrder.OrderQty > 0
                ? Math.Round((double)EditingDeliveryItem.DeliveredQty / SelectedOrder.OrderQty * SelectedOrder.OrderSQM, 2)
                : 0;
            EditingDeliveryItem.ReturnedSQM = SelectedOrder.OrderQty > 0
                ? Math.Round((double)EditingDeliveryItem.ReturnedQty / SelectedOrder.OrderQty * SelectedOrder.OrderSQM, 2)
                : 0;
            EditingDeliveryItem.CreatedDate = DateTime.Today;

            DbHelper.SaveDeliveryItem(EditingDeliveryItem);

            SelectedOrder.DeliveryItems.Add(EditingDeliveryItem);
            SelectedOrder.UpdatedDate = DateTime.Today;
            UpdateOrderStatus(SelectedOrder);

            DbHelper.UpdateDelivery(SelectedOrder);

            IsAddingDelivery = false;
            EditingDeliveryItem = null;
            RefreshDataView();
            UpdateAllStats();

            MessageBox.Show("Delivery recorded successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecuteDeleteDeliveryItem(object parameter)
        {
            if (parameter is DbDeliveryItem item)
            {
                ConfirmDeleteItem = item;
                IsDeletingDeliveryItem = true;
            }
            else if (SelectedDeliveryItem != null && SelectedOrder != null)
            {
                ConfirmDeleteItem = SelectedDeliveryItem;
                IsDeletingDeliveryItem = true;
            }
        }

        private bool CanExecuteDeleteDeliveryItem(object parameter)
        {
            return parameter != null || SelectedDeliveryItem != null;
        }

        private void ExecuteEditDeliveryItem(object parameter)
        {
            if (parameter is DbDeliveryItem item)
            {
                EditingDeliveryItemFromDb = new DbDeliveryItem
                {
                    Id = item.Id,
                    OrderId = item.OrderId,
                    DeliveryDate = item.DeliveryDate,
                    DeliveredQty = item.DeliveredQty,
                    ReturnedQty = item.ReturnedQty,
                    DeliveredSQM = item.DeliveredSQM,
                    ReturnedSQM = item.ReturnedSQM,
                    Driver = item.Driver,
                    Vehicle = item.Vehicle,
                    Notes = item.Notes
                };
                IsEditingDeliveryItem = true;
            }
        }

        private bool CanExecuteEditDeliveryItem(object parameter)
        {
            return parameter is DbDeliveryItem;
        }

        private bool CanExecuteSaveEditDeliveryItem(object parameter)
        {
            return EditingDeliveryItemFromDb != null;
        }

        // PATCH-03: Divide by zero protection, PATCH-12: Negative value check, PATCH-13: Null protection, PATCH-05: Fix Item Refresh
        private void ExecuteSaveEditDeliveryItem(object parameter)
        {
            if (EditingDeliveryItemFromDb == null || SelectedOrder == null) return;

            if (SelectedOrder?.DeliveryItems == null)
            {
                MessageBox.Show("Order delivery items not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (EditingDeliveryItemFromDb.DeliveredQty < 0)
            {
                MessageBox.Show("Delivered quantity cannot be negative.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // PATCH-12: Check for negative returned quantity
            if (EditingDeliveryItemFromDb.ReturnedQty < 0)
            {
                MessageBox.Show("Returned quantity cannot be negative.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // PATCH-03: Divide by zero protection
            if (SelectedOrder.OrderQty <= 0)
            {
                MessageBox.Show("Order Quantity must be greater than zero.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var originalItem = SelectedOrder.DeliveryItems.FirstOrDefault(x => x.Id == EditingDeliveryItemFromDb.Id);
            if (originalItem == null)
            {
                MessageBox.Show("Original delivery item not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!string.IsNullOrWhiteSpace(EditingDeliveryItemFromDb.Driver))
            {
                var driverTrimmed = EditingDeliveryItemFromDb.Driver.Trim();
                if (!DriverOptions.Contains(driverTrimmed))
                    DriverOptions.Add(driverTrimmed);
            }

            if (!string.IsNullOrWhiteSpace(EditingDeliveryItemFromDb.Vehicle))
            {
                var vehicleTrimmed = EditingDeliveryItemFromDb.Vehicle.Trim();
                if (!VehicleOptions.Contains(vehicleTrimmed))
                    VehicleOptions.Add(vehicleTrimmed);
            }

            originalItem.DeliveryDate = EditingDeliveryItemFromDb.DeliveryDate;
            originalItem.DeliveredQty = EditingDeliveryItemFromDb.DeliveredQty;
            originalItem.ReturnedQty = EditingDeliveryItemFromDb.ReturnedQty;
            originalItem.Driver = EditingDeliveryItemFromDb.Driver;
            originalItem.Vehicle = EditingDeliveryItemFromDb.Vehicle;
            originalItem.Notes = EditingDeliveryItemFromDb.Notes;

            // PATCH-03: Safe SQM calculation with divide by zero check
            originalItem.DeliveredSQM = SelectedOrder.OrderQty > 0
                ? Math.Round((double)originalItem.DeliveredQty / SelectedOrder.OrderQty * SelectedOrder.OrderSQM, 2)
                : 0;
            originalItem.ReturnedSQM = SelectedOrder.OrderQty > 0
                ? Math.Round((double)originalItem.ReturnedQty / SelectedOrder.OrderQty * SelectedOrder.OrderSQM, 2)
                : 0;

            DbHelper.UpdateDeliveryItem(originalItem);

            // PATCH-05: Use new ObservableCollection instead of Clear/Add
            var refreshedOrder = DbHelper.GetDeliveryById(SelectedOrder.Id);
            if (refreshedOrder != null)
            {
                SelectedOrder.DeliveryItems = new ObservableCollection<DbDeliveryItem>(refreshedOrder.DeliveryItems);
            }

            SelectedOrder.UpdatedDate = DateTime.Today;
            UpdateOrderStatus(SelectedOrder);

            DbHelper.UpdateDelivery(SelectedOrder);

            UpdateDataTableRow(SelectedOrder);

            IsEditingDeliveryItem = false;
            EditingDeliveryItemFromDb = null;
            UpdateAllStats();

            MessageBox.Show("Delivery item updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecuteCancelEditDeliveryItem(object parameter)
        {
            IsEditingDeliveryItem = false;
            EditingDeliveryItemFromDb = null;
        }

        private void ExecuteViewImportLog(object parameter)
        {
            if (ImportHistory != null && ImportHistory.Count > 0)
            {
                IsViewingImportLog = true;
            }
            else
            {
                MessageBox.Show("No import history available.", "Import History", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool CanExecuteViewImportLog(object parameter)
        {
            return ImportHistory != null && ImportHistory.Count > 0;
        }

        private void ExecuteCloseImportLog(object parameter)
        {
            IsViewingImportLog = false;
        }

        #endregion

        #region Notes View

        private void ExecuteViewNotes(object parameter)
        {
            if (parameter is string notes)
            {
                ViewNotesContent = string.IsNullOrWhiteSpace(notes) ? "No notes available." : notes;
                IsViewingNotes = true;
            }
        }

        private void ExecuteCloseNotes(object parameter)
        {
            IsViewingNotes = false;
            ViewNotesContent = "";
        }

        #endregion

        #region Delete Delivery Item with Confirmation

        // PATCH-13: Null protection
        private void ExecuteCancelDeleteDeliveryItem(object parameter)
        {
            IsDeletingDeliveryItem = false;
            ConfirmDeleteItem = null;
        }

        // PATCH-13: Null protection
        private void ExecuteConfirmDeleteDeliveryItem(object parameter)
        {
            if (ConfirmDeleteItem == null || SelectedOrder == null) return;

            if (SelectedOrder?.DeliveryItems == null)
            {
                MessageBox.Show("Order delivery items not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DbHelper.DeleteDeliveryItem(ConfirmDeleteItem.Id);

            SelectedOrder.DeliveryItems.Remove(ConfirmDeleteItem);
            SelectedOrder.UpdatedDate = DateTime.Today;
            UpdateOrderStatus(SelectedOrder);

            DbHelper.UpdateDelivery(SelectedOrder);

            UpdateDataTableRow(SelectedOrder);

            IsDeletingDeliveryItem = false;
            ConfirmDeleteItem = null;
            SelectedDeliveryItem = null;
            UpdateAllStats();
        }

        #endregion

        #region Export

        // PATCH-06: CSV Injection Prevention
        private void ExecuteExport(object parameter)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    DefaultExt = ".csv",
                    FileName = $"Deliveries_Export_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    ExportToCSV(dialog.FileName);
                    MessageBox.Show($"Export completed!\n{dialog.FileName}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // PATCH-06: CSV Injection Prevention
        private void ExportToCSV(string filePath)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Date,Company,PI Number,Type of Work,Color,Order Qty,Delivered,Returned,Balance,SQM,Salesman,Status,Notes");

            foreach (var order in DeliveryOrders)
            {
                sb.AppendLine($"\"{order.Date:dd-MM-yyyy}\"," +
                    $"\"{SanitizeCsv(order.Company)}\"," +
                    $"\"{SanitizeCsv(order.PINumber)}\"," +
                    $"\"{SanitizeCsv(order.TypeOfWork)}\"," +
                    $"\"{SanitizeCsv(order.Color)}\"," +
                    $"{order.OrderQty}," +
                    $"{order.TotalDelivered}," +
                    $"{order.TotalReturned}," +
                    $"{order.Balance}," +
                    $"{order.OrderSQM:N2}," +
                    $"\"{SanitizeCsv(order.Salesman)}\"," +
                    $"\"{SanitizeCsv(order.Status)}\"," +
                    $"\"{SanitizeCsv(order.Notes)}\"");
            }

            System.IO.File.WriteAllText(filePath, sb.ToString(), System.Text.Encoding.UTF8);
        }

        #endregion

        #region Clear Filters

        private void ExecuteClearFilters(object parameter)
        {
            SearchText = "";
            FilterStatus = "";
            FilterTypeOfWork = "";
            FilterSalesman = "";
            FilterCompany = "";
            FilterColor = "";
            FilterStartDate = null;
            FilterEndDate = null;
            SortColumn = "";
            SortDirection = ListSortDirection.Ascending;
            ApplyFilters();
        }

        #endregion

        #region Print

        private void ExecutePrint(object parameter)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    var printVisual = CreatePrintVisual();
                    if (printVisual != null)
                    {
                        printDialog.PrintVisual(printVisual, "Delivery Report");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Print failed: {ex.Message}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Grid CreatePrintVisual()
        {
            var grid = new Grid { Margin = new Thickness(20) };

            var headerPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };
            headerPanel.Children.Add(new TextBlock
            {
                Text = "DELIVERY REPORT",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            headerPanel.Children.Add(new TextBlock
            {
                Text = $"Generated: {DateTime.Now:dd-MM-yyyy HH:mm}",
                FontSize = 10,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0)
            });
            headerPanel.Children.Add(new TextBlock
            {
                Text = $"Total Orders: {TotalRecords} | Order Qty: {TotalOrderQty} | Delivered: {TotalDelivered} | Balance: {TotalBalance}",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0)
            });
            grid.Children.Add(headerPanel);

            var dataGrid = new DataGrid
            {
                ItemsSource = FilteredDataView,
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                IsReadOnly = true,
                FontSize = 10,
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                HorizontalGridLinesBrush = Brushes.LightGray,
                VerticalGridLinesBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Black,
                Margin = new Thickness(0, 10, 0, 0)
            };

            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Date", Binding = new System.Windows.Data.Binding("Date") { StringFormat = "dd-MM-yyyy" }, Width = 80 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Company", Binding = new System.Windows.Data.Binding("Company"), Width = 120 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "PI No", Binding = new System.Windows.Data.Binding("PINumber"), Width = 100 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Type", Binding = new System.Windows.Data.Binding("TypeOfWork"), Width = 100 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Color", Binding = new System.Windows.Data.Binding("Color"), Width = 80 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Order Qty", Binding = new System.Windows.Data.Binding("OrderQty"), Width = 70 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Delivered", Binding = new System.Windows.Data.Binding("TotalDelivered"), Width = 70 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Returned", Binding = new System.Windows.Data.Binding("TotalReturned"), Width = 70 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Balance", Binding = new System.Windows.Data.Binding("Balance"), Width = 60 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "SQM", Binding = new System.Windows.Data.Binding("OrderSQM") { StringFormat = "N2" }, Width = 60 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Salesman", Binding = new System.Windows.Data.Binding("Salesman"), Width = 90 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Status", Binding = new System.Windows.Data.Binding("Status"), Width = 80 });

            grid.Children.Add(dataGrid);

            return grid;
        }

        #endregion

        #region Delete Selected

        private void ExecuteDeleteSelected(object parameter)
        {
            if (parameter is System.Windows.Controls.DataGrid dataGrid)
            {
                var selectedIds = new List<int>();

                foreach (var item in dataGrid.SelectedItems)
                {
                    if (item is DataRowView rowView)
                    {
                        selectedIds.Add(Convert.ToInt32(rowView["Id"]));
                    }
                }

                if (selectedIds.Count == 0)
                {
                    MessageBox.Show("Please select rows to delete.", "No Selection",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"Delete {selectedIds.Count} selected order(s)?\n\nThis will also delete all delivery items.\n\nThis action cannot be undone.",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    foreach (var id in selectedIds)
                    {
                        if (ExecuteDbOperation(() => DbHelper.DeleteDelivery(id), "Delete Delivery"))
                        {
                            var item = DeliveryOrders.FirstOrDefault(w => w.Id == id);
                            if (item != null)
                            {
                                DeliveryOrders.Remove(item);
                            }
                        }
                    }

                    _selectedIds.Clear();
                    _selectedCount = 0;

                    RefreshDataView();
                    UpdateAllStats();
                    SelectedOrder = null;
                    SelectedDataRowView = null;
                }
            }
        }

        private bool CanExecuteDeleteSelected(object parameter)
        {
            return _selectedCount > 0;
        }

        #endregion

        #region Row Selection Handler

        public void OnDataGridSelectionChanged(DataRowView rowView)
        {
            SelectedDataRowView = rowView;
        }

        public void OnDataGridSelectionChanged(int selectedCount, List<int> selectedIds)
        {
            SetSelectedCount(selectedCount);
            UpdateSelectedIds(selectedIds);
        }

        #endregion

        #region Selection Tracking

        private void TrackDataGridSelection(System.Windows.Controls.DataGrid dataGrid)
        {
            if (dataGrid == null) return;

            var selectedIds = new List<int>();
            foreach (var item in dataGrid.SelectedItems)
            {
                if (item is DataRowView rowView)
                {
                    selectedIds.Add(Convert.ToInt32(rowView["Id"]));
                }
            }

            SetSelectedCount(selectedIds.Count);
            UpdateSelectedIds(selectedIds);
        }

        #endregion

        #region Create from JobOrder

        public void CreateFromJobOrder(JobOrder jobOrder)
        {
            if (jobOrder == null) return;

            try
            {
                // PATCH-02: Remove manual ID generation - database will use AUTOINCREMENT
                var newDelivery = new DbDelivery
                {
                    SourceId = jobOrder.Id,
                    Date = DateTime.Today,
                    Company = jobOrder.ClientName ?? "",
                    PINumber = jobOrder.JobNumber ?? "",
                    TypeOfWork = "Glass",  // Default value
                    OrderQty = jobOrder.TotalQty,
                    OrderSQM = jobOrder.TotalSQM,
                    Salesman = jobOrder.Salesman ?? "",
                    Color = jobOrder.Color ?? "",
                    Status = "Pending",
                    Notes = $"Created from Job Order: {jobOrder.JobNumber}\n{jobOrder.Notes ?? ""}",
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now,
                    DeliveryItems = new ObservableCollection<DbDeliveryItem>()
                };

                // Copy items from job order specifications
                int itemId = 1;
                foreach (var spec in jobOrder.Specifications)
                {
                    foreach (var item in spec.Items)
                    {
                        var deliveryItem = new DbDeliveryItem
                        {
                            OrderId = newDelivery.Id,
                            DeliveredQty = 0,
                            ReturnedQty = 0,
                            DeliveredSQM = 0,
                            ReturnedSQM = 0,
                            Driver = "",
                            Vehicle = "",
                            DeliveryDate = DateTime.Today,
                            CreatedDate = DateTime.Today
                        };
                        newDelivery.DeliveryItems.Add(deliveryItem);
                        itemId++;
                    }
                }

                // Save to database
                DbHelper.SaveDelivery(newDelivery);

                // Add to collection
                DeliveryOrders.Add(newDelivery);

                // PATCH-11: Use centralized option update
                AddToOptionsIfNew(CompanyOptions, newDelivery.Company);
                AddToOptionsIfNew(SalesmanOptions, newDelivery.Salesman);
                AddToOptionsIfNew(TypeOfWorkOptions, newDelivery.TypeOfWork);
                AddToOptionsIfNew(ColorOptions, newDelivery.Color);

                // Refresh views
                RefreshDataView();
                UpdateAllStats();

                // Select the new delivery
                SelectedOrder = newDelivery;

                System.Diagnostics.Debug.WriteLine($"[DeliveryViewModel] Created from Job Order: {jobOrder.JobNumber} -> DO Id: {newDelivery.Id}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeliveryViewModel] CreateFromJobOrder error: {ex.Message}");
                MessageBox.Show($"Failed to create delivery from Job Order: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}