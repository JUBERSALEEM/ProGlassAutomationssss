using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Data;
using ProGlassAutomation.Models;
using ProGlassAutomation.Data.Database;

namespace ProGlassAutomation.ViewModels
{
    public class DeliveryViewModel : ViewModelBase
    {
        private ObservableCollection<Delivery> _deliveryOrders;
        private ObservableCollection<DailyWork> _sourceOrders;
        private Delivery _selectedOrder;
        private DeliveryItem _selectedDeliveryItem;
        private DataRowView _selectedDataRowView;
        private DataView _filteredDataView;
        private string _searchText = "";
        private string _sortColumn = "";
        private ListSortDirection _sortDirection = ListSortDirection.Ascending;
        private string _filterStatus = "";
        private string _filterTypeOfWork = "";
        private string _filterSalesman = "";
        private string _filterCompany = "";
        private DateTime? _filterStartDate;
        private DateTime? _filterEndDate;
        private bool _isEditing;
        private bool _isAddingDelivery;
        private bool _isViewingDetails;
        private Delivery _editingOrder;
        private DeliveryItem _editingDeliveryItem;
        private bool _isNewRecord;

        // Options collections
        public ObservableCollection<string> TypeOfWorkOptions { get; private set; }
        public ObservableCollection<string> StatusOptions { get; private set; }
        public ObservableCollection<string> SalesmanOptions { get; private set; }
        public ObservableCollection<string> CompanyOptions { get; private set; }
        public ObservableCollection<string> DriverOptions { get; private set; }
        public ObservableCollection<string> VehicleOptions { get; private set; }

        public DeliveryViewModel()
        {
            DeliveryOrders = new ObservableCollection<Delivery>();
            SourceOrders = new ObservableCollection<DailyWork>();
            InitializeOptions();

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

            LoadDataFromDatabase();
            CreateDataView();
        }

        #region Properties

        public ObservableCollection<Delivery> DeliveryOrders
        {
            get => _deliveryOrders;
            set => SetProperty(ref _deliveryOrders, value);
        }

        public ObservableCollection<DailyWork> SourceOrders
        {
            get => _sourceOrders;
            set => SetProperty(ref _sourceOrders, value);
        }

        public DataView FilteredDataView
        {
            get => _filteredDataView;
            private set => SetProperty(ref _filteredDataView, value);
        }

        public Delivery SelectedOrder
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

        public DeliveryItem SelectedDeliveryItem
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
                    CommandManager.InvalidateRequerySuggested();
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

        public Delivery EditingOrder
        {
            get => _editingOrder;
            set => SetProperty(ref _editingOrder, value);
        }

        public DeliveryItem EditingDeliveryItem
        {
            get => _editingDeliveryItem;
            set => SetProperty(ref _editingDeliveryItem, value);
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
        public int TotalRecords => DeliveryOrders?.Count ?? 0;
        public int FilteredRecords => FilteredDataView?.Count ?? 0;
        public int TotalOrderQty => DeliveryOrders?.Sum(w => w.OrderQty) ?? 0;
        public int TotalDelivered => DeliveryOrders?.Sum(w => w.TotalDelivered) ?? 0;
        public int TotalReturned => DeliveryOrders?.Sum(w => w.TotalReturned) ?? 0;
        public int TotalBalance => DeliveryOrders?.Sum(w => w.Balance) ?? 0;
        public double TotalOrderSQM => DeliveryOrders?.Sum(w => w.OrderSQM) ?? 0;
        public int PendingCount => DeliveryOrders?.Count(w => w.Status == "Pending") ?? 0;
        public int PartialCount => DeliveryOrders?.Count(w => w.Status == "Partially Delivered") ?? 0;
        public int CompletedCount => DeliveryOrders?.Count(w => w.Status == "Completed") ?? 0;
        public int SelectedCount => _selectedCount;
        private int _selectedCount;

        public void SetSelectedCount(int count)
        {
            _selectedCount = count;
            OnPropertyChanged(nameof(SelectedCount));
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

        #endregion

        #region Initialization

        private void InitializeOptions()
        {
            TypeOfWorkOptions = new ObservableCollection<string>
            {
                "Single Unit (SGU)", "Double Unit (DGU)", "Lamination Unit",
                "Single + Double Unit", "SGU + DGU", "SGU + Lamination",
                "DGU + Lamination", "SGU + DGU + Lamination", "Tempered",
                "Tempered + Lamination", "Other"
            };

            StatusOptions = new ObservableCollection<string>
            {
                "Pending", "Partially Delivered", "Completed", "Cancelled"
            };

            SalesmanOptions = new ObservableCollection<string>
            {
                "Ahmed Khan", "Muhammad Ali", "Hassan Ahmed", "Usman Malik",
                "Bilal Shah", "Ali Raza", "Faisal Mahmood", "Imran Hussain"
            };

            CompanyOptions = new ObservableCollection<string>
            {
                "ABC Construction", "XYZ Windows", "Secure Buildings Ltd",
                "Modern Glass Works", "Elite Glazing Co", "Premium Windows Inc"
            };

            DriverOptions = new ObservableCollection<string>
            {
                "Driver 1", "Driver 2", "Driver 3", "Driver 4",
                "Driver 5", "Driver 6", "External 1", "External 2"
            };

            VehicleOptions = new ObservableCollection<string>
            {
                "Van A1", "Van A2", "Van B1", "Van B2",
                "Truck C1", "Truck C2", "Pickup 1", "Pickup 2"
            };
        }

        private void CreateDataView()
        {
            var dataTable = new DataTable("Deliveries");
            dataTable.Columns.Add("Id", typeof(int));
            dataTable.Columns.Add("Date", typeof(DateTime));
            dataTable.Columns.Add("Company", typeof(string));
            dataTable.Columns.Add("PINumber", typeof(string));
            dataTable.Columns.Add("TypeOfWork", typeof(string));
            dataTable.Columns.Add("OrderQty", typeof(int));
            dataTable.Columns.Add("TotalDelivered", typeof(int));
            dataTable.Columns.Add("TotalReturned", typeof(int));
            dataTable.Columns.Add("Balance", typeof(int));
            dataTable.Columns.Add("OrderSQM", typeof(double));
            dataTable.Columns.Add("Salesman", typeof(string));
            dataTable.Columns.Add("Status", typeof(string));
            dataTable.Columns.Add("Notes", typeof(string));
            dataTable.Columns.Add("IsSelected", typeof(bool));

            foreach (var order in DeliveryOrders)
            {
                var row = dataTable.NewRow();
                row["Id"] = order.Id;
                row["Date"] = order.Date;
                row["Company"] = order.Company ?? "";
                row["PINumber"] = order.PINumber ?? "";
                row["TypeOfWork"] = order.TypeOfWork ?? "";
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

        private void UpdateOrderStatus(Delivery order)
        {
            if (order.Balance <= 0)
                order.Status = "Completed";
            else if (order.TotalDelivered > 0)
                order.Status = "Partially Delivered";
            else
                order.Status = "Pending";
        }

        #endregion

        #region Load From Database

        private void LoadDataFromDatabase()
        {
            try
            {
                // Load DailyWork (Source Orders) from database
                var dbDailyWork = DbHelper.GetAllDailyWork();
                SourceOrders = new ObservableCollection<DailyWork>(dbDailyWork);

                // Load Deliveries from database
                var dbDeliveries = DbHelper.GetAllDeliveries();

                DeliveryOrders.Clear();
                foreach (var delivery in dbDeliveries)
                {
                    // Load delivery items for each delivery
                    var items = DbHelper.GetDeliveryItems(delivery.Id);
                    delivery.DeliveryItems = new ObservableCollection<DeliveryItem>(items);

                    // Update status based on delivery items
                    UpdateOrderStatus(delivery);

                    DeliveryOrders.Add(delivery);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data from database: {ex.Message}",
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadDeliveryItems(Delivery order)
        {
            try
            {
                var items = DbHelper.GetDeliveryItems(order.Id);
                order.DeliveryItems = new ObservableCollection<DeliveryItem>(items);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading delivery items: {ex.Message}",
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Import from Confirmed Orders

        private void ExecuteImportConfirmed(object parameter)
        {
            try
            {
                // Get only CONFIRMED orders from SourceOrders (from database)
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
                int skippedCount = 0;

                foreach (var sourceOrder in confirmedOrders)
                {
                    // CHECK IF ALREADY IMPORTED (by PI Number)
                    bool alreadyExists = DeliveryOrders.Any(d =>
                        d.PINumber == sourceOrder.PINumber &&
                        !string.IsNullOrEmpty(sourceOrder.PINumber));

                    if (alreadyExists)
                    {
                        skippedCount++;
                        continue;
                    }

                    // CREATE NEW DELIVERY ORDER
                    var newDelivery = new Delivery
                    {
                        Id = DeliveryOrders.Count > 0 ? DeliveryOrders.Max(d => d.Id) + 1 : 1,
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
                        DeliveryItems = new ObservableCollection<DeliveryItem>()
                    };

                    // SAVE TO DATABASE
                    DbHelper.SaveDelivery(newDelivery);

                    DeliveryOrders.Add(newDelivery);
                    importedCount++;
                }

                RefreshDataView();
                UpdateStatistics();

                string message = $"Import Complete!\n\n";
                message += $"Imported: {importedCount} orders\n";
                message += $"Skipped (already imported): {skippedCount} orders";

                MessageBox.Show(message, "Import", MessageBoxButton.OK, MessageBoxImage.Information);
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

            if (FilterStartDate.HasValue)
                filterExpressions.Add($"Date >= #{FilterStartDate.Value:yyyy-MM-dd}#");

            if (FilterEndDate.HasValue)
                filterExpressions.Add($"Date <= #{FilterEndDate.Value:yyyy-MM-dd}#");

            FilteredDataView.RowFilter = filterExpressions.Count > 0 ? string.Join(" AND ", filterExpressions) : "";

            if (!string.IsNullOrEmpty(SortColumn))
                FilteredDataView.Sort = $"{SortColumn} {(SortDirection == ListSortDirection.Ascending ? "ASC" : "DESC")}";

            UpdateStatistics();
        }

        #endregion

        #region Command Implementations

        private void ExecuteAddNew(object parameter)
        {
            _isNewRecord = true;
            EditingOrder = new Delivery
            {
                Id = 0,
                Date = DateTime.Today,
                Status = "Pending",
                OrderQty = 0,
                OrderSQM = 0,
                DeliveryItems = new ObservableCollection<DeliveryItem>()
            };
            IsEditing = true;
        }

        private void ExecuteEdit(object parameter)
        {
            Delivery orderToEdit = null;

            if (SelectedDataRowView != null)
            {
                int id = Convert.ToInt32(SelectedDataRowView["Id"]);
                orderToEdit = DeliveryOrders.FirstOrDefault(w => w.Id == id);
            }
            else if (SelectedOrder != null)
            {
                orderToEdit = SelectedOrder;
            }

            if (orderToEdit != null)
            {
                _isNewRecord = false;
                EditingOrder = orderToEdit.Clone();
                IsEditing = true;
            }
        }

        private bool CanExecuteEdit(object parameter)
        {
            return parameter != null || SelectedDataRowView != null || SelectedOrder != null;
        }

        private void ExecuteDelete(object parameter)
        {
            Delivery orderToDelete = null;

            if (SelectedDataRowView != null)
            {
                int id = Convert.ToInt32(SelectedDataRowView["Id"]);
                orderToDelete = DeliveryOrders.FirstOrDefault(w => w.Id == id);
            }
            else if (SelectedOrder != null)
            {
                orderToDelete = SelectedOrder;
            }

            if (orderToDelete != null)
            {
                var result = MessageBox.Show(
                    $"Delete delivery order for {orderToDelete.Company}?\nPI: {orderToDelete.PINumber}\n\nThis will also delete all delivery items.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // DELETE FROM DATABASE
                    DbHelper.DeleteDelivery(orderToDelete.Id);

                    DeliveryOrders.Remove(orderToDelete);
                    RefreshDataView();
                    UpdateStatistics();
                    SelectedOrder = null;
                    SelectedDataRowView = null;
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
                EditingOrder.Id = DeliveryOrders.Count > 0 ? DeliveryOrders.Max(w => w.Id) + 1 : 1;
                EditingOrder.CreatedDate = DateTime.Today;
                EditingOrder.UpdatedDate = DateTime.Today;

                // SAVE TO DATABASE
                DbHelper.SaveDelivery(EditingOrder);

                DeliveryOrders.Add(EditingOrder);
            }
            else
            {
                var existing = DeliveryOrders.FirstOrDefault(w => w.Id == EditingOrder.Id);
                if (existing != null)
                {
                    existing.Date = EditingOrder.Date;
                    existing.UpdatedDate = DateTime.Today;
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
                    UpdateOrderStatus(existing);

                    // UPDATE IN DATABASE
                    DbHelper.UpdateDelivery(existing);
                }
            }

            IsEditing = false;
            EditingOrder = null;
            RefreshDataView();
            UpdateStatistics();
        }

        private bool CanExecuteSave(object parameter) => EditingOrder != null;

        private void ExecuteCancel(object parameter)
        {
            IsEditing = false;
            IsAddingDelivery = false;
            IsViewingDetails = false;
            EditingOrder = null;
            EditingDeliveryItem = null;
        }

        private void ExecuteRefresh(object parameter)
        {
            LoadDataFromDatabase();
            RefreshDataView();
            UpdateStatistics();
        }

        private void ExecuteViewDetails(object parameter)
        {
            Delivery orderToView = null;

            if (SelectedDataRowView != null)
            {
                int id = Convert.ToInt32(SelectedDataRowView["Id"]);
                orderToView = DeliveryOrders.FirstOrDefault(w => w.Id == id);
            }
            else if (SelectedOrder != null)
            {
                orderToView = SelectedOrder;
            }

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
            Delivery order = SelectedOrder;

            if (order != null && order.Balance > 0)
            {
                EditingDeliveryItem = new DeliveryItem
                {
                    Id = 0,
                    OrderId = order.Id,
                    DeliveryDate = DateTime.Today,
                    DeliveredQty = 0,
                    ReturnedQty = 0,
                    DeliveredSQM = 0,
                    ReturnedSQM = 0
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

        private void ExecuteSaveDelivery(object parameter)
        {
            if (EditingDeliveryItem == null || SelectedOrder == null) return;

            if (EditingDeliveryItem.DeliveredQty <= 0)
            {
                MessageBox.Show("Delivered quantity must be greater than 0.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
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

            EditingDeliveryItem.Id = SelectedOrder.DeliveryItems.Count > 0
                ? SelectedOrder.DeliveryItems.Max(x => x.Id) + 1
                : 1;
            EditingDeliveryItem.DeliveredSQM = Math.Round((double)EditingDeliveryItem.DeliveredQty / SelectedOrder.OrderQty * SelectedOrder.OrderSQM, 2);
            EditingDeliveryItem.ReturnedSQM = Math.Round((double)EditingDeliveryItem.ReturnedQty / SelectedOrder.OrderQty * SelectedOrder.OrderSQM, 2);
            EditingDeliveryItem.CreatedDate = DateTime.Today;

            // SAVE TO DATABASE
            DbHelper.SaveDeliveryItem(EditingDeliveryItem);

            SelectedOrder.DeliveryItems.Add(EditingDeliveryItem);
            SelectedOrder.UpdatedDate = DateTime.Today;
            UpdateOrderStatus(SelectedOrder);

            // UPDATE DELIVERY STATUS IN DATABASE
            DbHelper.UpdateDelivery(SelectedOrder);

            IsAddingDelivery = false;
            EditingDeliveryItem = null;
            RefreshDataView();
            UpdateStatistics();

            MessageBox.Show("Delivery recorded successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecuteDeleteDeliveryItem(object parameter)
        {
            if (SelectedDeliveryItem == null || SelectedOrder == null) return;

            var result = MessageBox.Show(
                $"Delete this delivery record?\n\n" +
                $"Delivered: {SelectedDeliveryItem.DeliveredQty} pcs\n" +
                $"Returned: {SelectedDeliveryItem.ReturnedQty} pcs\n" +
                $"Date: {SelectedDeliveryItem.DeliveryDate:dd-MM-yyyy}",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // DELETE FROM DATABASE
                DbHelper.DeleteDeliveryItem(SelectedDeliveryItem.Id);

                SelectedOrder.DeliveryItems.Remove(SelectedDeliveryItem);
                SelectedOrder.UpdatedDate = DateTime.Today;
                UpdateOrderStatus(SelectedOrder);

                // UPDATE DELIVERY STATUS IN DATABASE
                DbHelper.UpdateDelivery(SelectedOrder);

                SelectedDeliveryItem = null;
                RefreshDataView();
                UpdateStatistics();
            }
        }

        private bool CanExecuteDeleteDeliveryItem(object parameter)
        {
            return SelectedDeliveryItem != null;
        }

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

        private void ExportToCSV(string filePath)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Date,Company,PI Number,Type of Work,Order Qty,Delivered,Returned,Balance,SQM,Salesman,Status,Notes");

            foreach (var order in DeliveryOrders)
            {
                sb.AppendLine($"\"{order.Date:dd-MM-yyyy}\",\"{order.Company}\",\"{order.PINumber}\",\"{order.TypeOfWork}\",{order.OrderQty},{order.TotalDelivered},{order.TotalReturned},{order.Balance},{order.OrderSQM:N2},\"{order.Salesman}\",\"{order.Status}\",\"{order.Notes}\"");
            }

            System.IO.File.WriteAllText(filePath, sb.ToString(), System.Text.Encoding.UTF8);
        }

        private void ExecuteClearFilters(object parameter)
        {
            SearchText = "";
            FilterStatus = "";
            FilterTypeOfWork = "";
            FilterSalesman = "";
            FilterCompany = "";
            FilterStartDate = null;
            FilterEndDate = null;
            SortColumn = "";
            SortDirection = ListSortDirection.Ascending;
            ApplyFilters();
        }

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

            // Header
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
                Text = $"Total Orders: {FilteredRecords} | Order Qty: {TotalOrderQty} | Delivered: {TotalDelivered} | Balance: {TotalBalance}",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0)
            });
            grid.Children.Add(headerPanel);

            // DataGrid for printing
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
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Order Qty", Binding = new System.Windows.Data.Binding("OrderQty"), Width = 70 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Delivered", Binding = new System.Windows.Data.Binding("TotalDelivered"), Width = 70 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Returned", Binding = new System.Windows.Data.Binding("TotalReturned"), Width = 70 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Balance", Binding = new System.Windows.Data.Binding("Balance"), Width = 70 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "SQM", Binding = new System.Windows.Data.Binding("OrderSQM") { StringFormat = "N2" }, Width = 70 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Salesman", Binding = new System.Windows.Data.Binding("Salesman"), Width = 90 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Status", Binding = new System.Windows.Data.Binding("Status"), Width = 80 });

            grid.Children.Add(dataGrid);

            return grid;
        }

        private void UpdateStatistics()
        {
            OnPropertyChanged(nameof(TotalRecords));
            OnPropertyChanged(nameof(FilteredRecords));
            OnPropertyChanged(nameof(TotalOrderQty));
            OnPropertyChanged(nameof(TotalDelivered));
            OnPropertyChanged(nameof(TotalReturned));
            OnPropertyChanged(nameof(TotalBalance));
            OnPropertyChanged(nameof(TotalOrderSQM));
            OnPropertyChanged(nameof(PendingCount));
            OnPropertyChanged(nameof(PartialCount));
            OnPropertyChanged(nameof(CompletedCount));
            OnPropertyChanged(nameof(SelectedCount));
        }

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
                    $"Delete {selectedIds.Count} selected record(s)?\n\nThis will also delete all delivery items.\nThis action cannot be undone.",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // DELETE FROM DATABASE
                    foreach (var id in selectedIds)
                    {
                        DbHelper.DeleteDelivery(id);
                    }

                    var toDelete = DeliveryOrders.Where(d => selectedIds.Contains(d.Id)).ToList();
                    foreach (var item in toDelete)
                    {
                        DeliveryOrders.Remove(item);
                    }
                    RefreshDataView();
                    UpdateStatistics();
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
    }
}