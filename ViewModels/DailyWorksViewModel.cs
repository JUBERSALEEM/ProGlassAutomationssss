using ProGlassAutomation.Models;
using ProGlassAutomation.Data.Database;
using System;
using System.Collections.ObjectModel;
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
    public class DailyWorksViewModel : ViewModelBase
    {
        private ObservableCollection<DailyWork> _dailyWorks;
        private DailyWork _selectedWork;
        private DataRowView _selectedDataRowView;
        private DataView _filteredDataView;
        private string _searchText = "";
        private string _sortColumn = "";
        private ListSortDirection _sortDirection = ListSortDirection.Ascending;
        private string _filterStatus = "";
        private string _filterProductionStatus = "";
        private string _filterTypeOfWork = "";
        private string _filterSalesman = "";
        private string _filterCompany = "";
        private string _filterColor = "";
        private string _filterPINumber = "";
        private DateTime? _filterStartDate;
        private DateTime? _filterEndDate;
        private bool _isEditing;
        private DailyWork _editingWork;
        private bool _isNewRecord;

        // ✅ OPTIONS - Empty collections, populated dynamically
        public ObservableCollection<string> TypeOfWorkOptions { get; } = new();
        public ObservableCollection<string> ProductionStatusOptions { get; } = new();
        public ObservableCollection<string> DailyReportStatusOptions { get; } = new();
        public ObservableCollection<string> StatusOptions { get; } = new();
        public ObservableCollection<string> ColorOptions { get; } = new();
        public ObservableCollection<string> SalesmanOptions { get; } = new();
        public ObservableCollection<string> CompanyOptions { get; } = new();
        public ObservableCollection<string> PINumberOptions { get; } = new();

        // ✅ NEW: Customer Reference and Notes options for autocomplete
        public ObservableCollection<string> CustomerReferenceOptions { get; } = new();
        public ObservableCollection<string> NotesOptions { get; } = new();

        // ✅ Current selected values (for editing)
        private string _selectedTypeOfWork = "";
        private string _selectedProductionStatus = "";
        private string _selectedDailyReportStatus = "";
        private string _selectedStatus = "";
        private string _selectedColor = "";
        private string _selectedSalesman = "";
        private string _selectedCompany = "";

        // ✅ Current selected indices
        private int _typeOfWorkIndex = -1;
        private int _productionStatusIndex = -1;
        private int _dailyReportStatusIndex = -1;
        private int _statusIndex = -1;
        private int _colorIndex = -1;
        private int _salesmanIndex = -1;
        private int _companyIndex = -1;

        // ✅ Properties for Selected Values
        public string SelectedTypeOfWork
        {
            get => _selectedTypeOfWork;
            set => SetProperty(ref _selectedTypeOfWork, value);
        }

        public int TypeOfWorkIndex
        {
            get => _typeOfWorkIndex;
            set => SetProperty(ref _typeOfWorkIndex, value);
        }

        public string SelectedProductionStatus
        {
            get => _selectedProductionStatus;
            set => SetProperty(ref _selectedProductionStatus, value);
        }

        public int ProductionStatusIndex
        {
            get => _productionStatusIndex;
            set => SetProperty(ref _productionStatusIndex, value);
        }

        public string SelectedDailyReportStatus
        {
            get => _selectedDailyReportStatus;
            set => SetProperty(ref _selectedDailyReportStatus, value);
        }

        public int DailyReportStatusIndex
        {
            get => _dailyReportStatusIndex;
            set => SetProperty(ref _dailyReportStatusIndex, value);
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set => SetProperty(ref _selectedStatus, value);
        }

        public int StatusIndex
        {
            get => _statusIndex;
            set => SetProperty(ref _statusIndex, value);
        }

        public string SelectedColor
        {
            get => _selectedColor;
            set => SetProperty(ref _selectedColor, value);
        }

        public int ColorIndex
        {
            get => _colorIndex;
            set => SetProperty(ref _colorIndex, value);
        }

        public string SelectedSalesman
        {
            get => _selectedSalesman;
            set => SetProperty(ref _selectedSalesman, value);
        }

        public int SalesmanIndex
        {
            get => _salesmanIndex;
            set => SetProperty(ref _salesmanIndex, value);
        }

        public string SelectedCompany
        {
            get => _selectedCompany;
            set => SetProperty(ref _selectedCompany, value);
        }

        public int CompanyIndex
        {
            get => _companyIndex;
            set => SetProperty(ref _companyIndex, value);
        }

        // ✅ Duplicate warning
        private bool _isDuplicateWarning;
        public bool IsDuplicateWarning
        {
            get => _isDuplicateWarning;
            set => SetProperty(ref _isDuplicateWarning, value);
        }

        private string _duplicateMessage = "";
        public string DuplicateMessage
        {
            get => _duplicateMessage;
            set => SetProperty(ref _duplicateMessage, value);
        }

        private string _editingSqmText = "";
        public string EditingSqmText
        {
            get => _editingSqmText;
            set
            {
                if (SetProperty(ref _editingSqmText, value))
                {
                    if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                    {
                        if (EditingWork != null)
                            EditingWork.SQM = result;
                    }
                }
            }
        }

        private string _editingQtyText = "";
        public string EditingQtyText
        {
            get => _editingQtyText;
            set
            {
                if (SetProperty(ref _editingQtyText, value))
                {
                    if (int.TryParse(value, out int result))
                    {
                        if (EditingWork != null)
                            EditingWork.Qty = result;
                    }
                }
            }
        }

        public DailyWorksViewModel()
        {
            DailyWorks = new ObservableCollection<DailyWork>();

            AddNewCommand = new RelayCommand(ExecuteAddNew);
            EditCommand = new RelayCommand(ExecuteEdit, CanExecuteEdit);
            DeleteCommand = new RelayCommand(ExecuteDelete, CanExecuteDelete);
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);
            RefreshCommand = new RelayCommand(ExecuteRefresh);
            ExportCommand = new RelayCommand(ExecuteExport);
            ClearFiltersCommand = new RelayCommand(ExecuteClearFilters);
            SortCommand = new RelayCommand(ExecuteSort);
            CopyRowCommand = new RelayCommand(ExecuteCopyRow, CanExecuteCopyRow);
            DuplicateRowCommand = new RelayCommand(ExecuteDuplicateRow, CanExecuteDuplicateRow);
            DeleteSelectedCommand = new RelayCommand(ExecuteDeleteSelected, CanExecuteDeleteSelected);
            PrintCommand = new RelayCommand(ExecutePrint);

            LoadFromDatabase();
            LoadOptionsFromDatabase();
            CreateDataView();
        }

        // ═══════════════════════════════════════════════════════════
        // HELPER - Add to options if new
        // ═══════════════════════════════════════════════════════════

        private void AddToOptionsIfNew(ObservableCollection<string> collection, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (!collection.Contains(value))
            {
                collection.Add(value);
            }
        }

        private void SetSelectedValue(string value, ObservableCollection<string> collection, Action<int> setIndex, Action<string> setSelected)
        {
            var index = collection.IndexOf(value);
            if (index >= 0)
            {
                setIndex(index);
            }
            else if (!string.IsNullOrWhiteSpace(value))
            {
                AddToOptionsIfNew(collection, value);
                setIndex(collection.Count - 1);
            }
            else
            {
                setIndex(-1);
            }
            setSelected(value);
        }

        // ═══════════════════════════════════════════════════════════
        // LOAD OPTIONS FROM DATABASE
        // ═══════════════════════════════════════════════════════════

        private void LoadOptionsFromDatabase()
        {
            try
            {
                var dbData = DbHelper.GetAllDailyWork();

                foreach (var work in dbData)
                {
                    AddToOptionsIfNew(TypeOfWorkOptions, work.TypeOfWork);
                    AddToOptionsIfNew(ProductionStatusOptions, work.ProductionStatus);
                    AddToOptionsIfNew(DailyReportStatusOptions, work.DailyReportStatus);
                    AddToOptionsIfNew(StatusOptions, work.Status);
                    AddToOptionsIfNew(ColorOptions, work.Color);
                    AddToOptionsIfNew(SalesmanOptions, work.Salesman);
                    AddToOptionsIfNew(CompanyOptions, work.Company);
                    AddToOptionsIfNew(PINumberOptions, work.PINumber);
                    AddToOptionsIfNew(CustomerReferenceOptions, work.CustomerReference);
                    AddToOptionsIfNew(NotesOptions, work.Notes);
                }

                // ✅ Load from autocomplete tables
                var customerRefs = DbHelper.GetAllCustomerReferences();
                foreach (var cr in customerRefs)
                {
                    AddToOptionsIfNew(CustomerReferenceOptions, cr.CustomerReference);
                }

                var notes = DbHelper.GetAllNotes();
                foreach (var note in notes)
                {
                    AddToOptionsIfNew(NotesOptions, note);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DailyWork] Load options error: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // CHECK DUPLICATE
        // ═══════════════════════════════════════════════════════════

        private void CheckForDuplicate()
        {
            if (EditingWork == null) return;

            if (!string.IsNullOrWhiteSpace(EditingWork.CustomerReference) &&
                !string.IsNullOrWhiteSpace(EditingWork.PINumber))
            {
                bool isDuplicate = DbHelper.IsDuplicateDailyWork(
                    EditingWork.CustomerReference,
                    EditingWork.PINumber,
                    EditingWork.Id);

                IsDuplicateWarning = isDuplicate;
                DuplicateMessage = isDuplicate
                    ? $"⚠️ Duplicate: {EditingWork.CustomerReference} + {EditingWork.PINumber} already exists!"
                    : "";
            }
            else
            {
                IsDuplicateWarning = false;
                DuplicateMessage = "";
            }
        }

        // ═══════════════════════════════════════════════════════════
        // PARSE DECIMAL (handles both . and , as decimal separator)
        // ═══════════════════════════════════════════════════════════

        private double ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;

            // Try current culture first
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out double result))
                return result;

            // Try invariant culture (US format with .)
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                return result;

            // Try replacing comma with period
            if (double.TryParse(value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                return result;

            return 0;
        }

        #region Properties

        public ObservableCollection<DailyWork> DailyWorks
        {
            get => _dailyWorks;
            set => SetProperty(ref _dailyWorks, value);
        }

        public DataView FilteredDataView
        {
            get => _filteredDataView;
            private set => SetProperty(ref _filteredDataView, value);
        }

        public DailyWork SelectedWork
        {
            get => _selectedWork;
            set
            {
                if (SetProperty(ref _selectedWork, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool SelectAll
        {
            get => _selectAll;
            set
            {
                if (SetProperty(ref _selectAll, value))
                {
                    if (FilteredDataView != null)
                    {
                        foreach (var row in FilteredDataView.Cast<DataRowView>())
                        {
                            if (row.Row.Table.Columns.Contains("IsSelected"))
                            {
                                row["IsSelected"] = value;
                            }
                        }
                    }
                }
            }
        }

        private bool _selectAll;

        public int SelectedCount => _selectedCount;
        private int _selectedCount;

        public void SetSelectedCount(int count)
        {
            _selectedCount = count;
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(SelectedRecords));
            OnPropertyChanged(nameof(SelectedQty));
            OnPropertyChanged(nameof(SelectedSQM));
        }

        private DataGrid _mainDataGrid;
        public DataGrid MainDataGrid
        {
            get => _mainDataGrid;
            set => SetProperty(ref _mainDataGrid, value);
        }

        // ✅ Selected IDs tracking
        private List<int> _selectedIds = new List<int>();
        public List<DailyWork> SelectedRecordsData => DailyWorks?.Where(w => _selectedIds.Contains(w.Id)).ToList() ?? new List<DailyWork>();

        public void UpdateSelectedIds(List<int> ids)
        {
            _selectedIds = ids;
            _selectedCount = ids.Count;
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(SelectedRecords));
            OnPropertyChanged(nameof(SelectedQty));
            OnPropertyChanged(nameof(SelectedSQM));
        }

        private int GetSelectedCount()
        {
            return FilteredDataView?.Cast<DataRowView>()
                .Count(r => r.Row.Table.Columns.Contains("IsSelected") &&
                            r["IsSelected"] is bool b && b) ?? 0;
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

        public string FilterProductionStatus
        {
            get => _filterProductionStatus;
            set
            {
                if (SetProperty(ref _filterProductionStatus, value))
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

        public string FilterPINumber
        {
            get => _filterPINumber;
            set
            {
                if (SetProperty(ref _filterPINumber, value))
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

        public DailyWork EditingWork
        {
            get => _editingWork;
            set => SetProperty(ref _editingWork, value);
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
        public int TotalRecords => DailyWorks?.Count ?? 0;
        public int FilteredRecords => FilteredDataView?.Count ?? 0;
        public double TotalSQM => DailyWorks?.Sum(w => w.SQM) ?? 0;
        public int TotalQty => DailyWorks?.Sum(w => w.Qty) ?? 0;
        public double FilteredSQM => FilteredDataView?.Cast<DataRowView>().Sum(r => Convert.ToDouble(r["SQM"])) ?? 0;
        public int FilteredQty => FilteredDataView?.Cast<DataRowView>().Sum(r => Convert.ToInt32(r["Qty"])) ?? 0;

        // ✅ Selected stats (for footer display)
        public int SelectedRecords => _selectedCount;
        public double SelectedSQM => GetSelectedSQM();
        public int SelectedQty => GetSelectedQty();

        private double GetSelectedSQM()
        {
            if (_selectedCount == 0) return TotalSQM;
            return DailyWorks?.Where(w => _selectedIds.Contains(w.Id)).Sum(w => w.SQM) ?? 0;
        }

        private int GetSelectedQty()
        {
            if (_selectedCount == 0) return TotalQty;
            return DailyWorks?.Where(w => _selectedIds.Contains(w.Id)).Sum(w => w.Qty) ?? 0;
        }

        #endregion

        #region Commands

        public ICommand AddNewCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand SortCommand { get; }
        public ICommand CopyRowCommand { get; }
        public ICommand DuplicateRowCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        public ICommand PrintCommand { get; }

        #endregion

        #region DataView

        private void CreateDataView()
        {
            var dataTable = new DataTable("DailyWorks");
            dataTable.Columns.Add("Id", typeof(int));
            dataTable.Columns.Add("Date", typeof(DateTime));
            dataTable.Columns.Add("UpdateDate", typeof(DateTime));
            dataTable.Columns.Add("Company", typeof(string));
            dataTable.Columns.Add("PINumber", typeof(string));
            dataTable.Columns.Add("CustomerReference", typeof(string));
            dataTable.Columns.Add("TypeOfWork", typeof(string));
            dataTable.Columns.Add("ProductionStatus", typeof(string));
            dataTable.Columns.Add("DailyReportStatus", typeof(string));
            dataTable.Columns.Add("Qty", typeof(int));
            dataTable.Columns.Add("SQM", typeof(double));
            dataTable.Columns.Add("Status", typeof(string));
            dataTable.Columns.Add("Salesman", typeof(string));
            dataTable.Columns.Add("Color", typeof(string));
            dataTable.Columns.Add("Notes", typeof(string));

            foreach (var work in DailyWorks)
            {
                var row = dataTable.NewRow();
                row["Id"] = work.Id;
                row["Date"] = work.Date;
                row["UpdateDate"] = work.UpdateDate;
                row["Company"] = work.Company ?? "";
                row["PINumber"] = work.PINumber ?? "";
                row["CustomerReference"] = work.CustomerReference ?? "";
                row["TypeOfWork"] = work.TypeOfWork ?? "";
                row["ProductionStatus"] = work.ProductionStatus ?? "";
                row["DailyReportStatus"] = work.DailyReportStatus ?? "";
                row["Qty"] = work.Qty;
                row["SQM"] = work.SQM;
                row["Status"] = work.Status ?? "";
                row["Salesman"] = work.Salesman ?? "";
                row["Color"] = work.Color ?? "";
                row["Notes"] = work.Notes ?? "";
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

        #endregion

        #region Load Data from Database

        private void LoadFromDatabase()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[DailyWork] Loading from database...");

                var dbData = DbHelper.GetAllDailyWork();

                DailyWorks.Clear();
                foreach (var work in dbData)
                {
                    DailyWorks.Add(work);
                }

                System.Diagnostics.Debug.WriteLine($"[DailyWork] Loaded {DailyWorks.Count} records");
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DailyWork] Load error: {ex.Message}");
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
                filterExpressions.Add($"(Company LIKE '%{searchLower}%' OR PINumber LIKE '%{searchLower}%' OR CustomerReference LIKE '%{searchLower}%' OR Salesman LIKE '%{searchLower}%' OR Notes LIKE '%{searchLower}%')");
            }

            if (!string.IsNullOrWhiteSpace(FilterStatus))
                filterExpressions.Add($"Status = '{FilterStatus}'");

            if (!string.IsNullOrWhiteSpace(FilterProductionStatus))
                filterExpressions.Add($"ProductionStatus = '{FilterProductionStatus}'");

            if (!string.IsNullOrWhiteSpace(FilterTypeOfWork))
                filterExpressions.Add($"TypeOfWork = '{FilterTypeOfWork}'");

            if (!string.IsNullOrWhiteSpace(FilterSalesman))
                filterExpressions.Add($"Salesman = '{FilterSalesman}'");

            if (!string.IsNullOrWhiteSpace(FilterCompany))
                filterExpressions.Add($"Company = '{FilterCompany}'");

            if (!string.IsNullOrWhiteSpace(FilterColor))
                filterExpressions.Add($"Color = '{FilterColor}'");

            // ✅ ADD PI Number filter
            if (!string.IsNullOrWhiteSpace(FilterPINumber))
                filterExpressions.Add($"PINumber = '{FilterPINumber}'");

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
            EditingWork = new DailyWork
            {
                Id = 0,
                Date = DateTime.Today,
                UpdateDate = DateTime.Today,
                CreatedDate = DateTime.Now
            };
            IsEditing = true;
            IsDuplicateWarning = false;
            DuplicateMessage = "";
            EditingSqmText = "";
            EditingQtyText = "";

            // Reset all selection indices
            _typeOfWorkIndex = -1;
            _productionStatusIndex = -1;
            _dailyReportStatusIndex = -1;
            _statusIndex = -1;
            _colorIndex = -1;
            _salesmanIndex = -1;
            _companyIndex = -1;
            _selectedTypeOfWork = "";
            _selectedProductionStatus = "";
            _selectedDailyReportStatus = "";
            _selectedStatus = "";
            _selectedColor = "";
            _selectedSalesman = "";
            _selectedCompany = "";
        }

        private void ExecuteEdit(object parameter)
        {
            DailyWork workToEdit = null;

            if (SelectedDataRowView != null)
            {
                var dataRow = SelectedDataRowView;
                workToEdit = new DailyWork
                {
                    Id = Convert.ToInt32(dataRow["Id"]),
                    Date = Convert.ToDateTime(dataRow["Date"]),
                    UpdateDate = Convert.ToDateTime(dataRow["UpdateDate"]),
                    Company = dataRow["Company"]?.ToString() ?? "",
                    PINumber = dataRow["PINumber"]?.ToString() ?? "",
                    CustomerReference = dataRow["CustomerReference"]?.ToString() ?? "",
                    TypeOfWork = dataRow["TypeOfWork"]?.ToString() ?? "",
                    ProductionStatus = dataRow["ProductionStatus"]?.ToString() ?? "",
                    DailyReportStatus = dataRow["DailyReportStatus"]?.ToString() ?? "",
                    Qty = Convert.ToInt32(dataRow["Qty"]),
                    SQM = Convert.ToDouble(dataRow["SQM"]),
                    Status = dataRow["Status"]?.ToString() ?? "",
                    Salesman = dataRow["Salesman"]?.ToString() ?? "",
                    Color = dataRow["Color"]?.ToString() ?? "",
                    Notes = dataRow["Notes"]?.ToString() ?? ""
                };
            }
            else if (SelectedWork != null)
            {
                workToEdit = SelectedWork;
            }

            if (workToEdit != null)
            {
                _isNewRecord = false;
                EditingWork = workToEdit.Clone();
                IsEditing = true;
                IsDuplicateWarning = false;
                DuplicateMessage = "";
                EditingSqmText = EditingWork.SQM.ToString(CultureInfo.InvariantCulture);
                EditingQtyText = EditingWork.Qty.ToString();

                // Set dropdown selections
                SetSelectedValue(workToEdit.TypeOfWork, TypeOfWorkOptions, i => _typeOfWorkIndex = i, v => _selectedTypeOfWork = v);
                SetSelectedValue(workToEdit.ProductionStatus, ProductionStatusOptions, i => _productionStatusIndex = i, v => _selectedProductionStatus = v);
                SetSelectedValue(workToEdit.DailyReportStatus, DailyReportStatusOptions, i => _dailyReportStatusIndex = i, v => _selectedDailyReportStatus = v);
                SetSelectedValue(workToEdit.Status, StatusOptions, i => _statusIndex = i, v => _selectedStatus = v);
                SetSelectedValue(workToEdit.Color, ColorOptions, i => _colorIndex = i, v => _selectedColor = v);
                SetSelectedValue(workToEdit.Salesman, SalesmanOptions, i => _salesmanIndex = i, v => _selectedSalesman = v);
                SetSelectedValue(workToEdit.Company, CompanyOptions, i => _companyIndex = i, v => _selectedCompany = v);

                OnPropertyChanged(nameof(TypeOfWorkIndex));
                OnPropertyChanged(nameof(ProductionStatusIndex));
                OnPropertyChanged(nameof(DailyReportStatusIndex));
                OnPropertyChanged(nameof(StatusIndex));
                OnPropertyChanged(nameof(ColorIndex));
                OnPropertyChanged(nameof(SalesmanIndex));
                OnPropertyChanged(nameof(CompanyIndex));
                OnPropertyChanged(nameof(SelectedTypeOfWork));
                OnPropertyChanged(nameof(SelectedProductionStatus));
                OnPropertyChanged(nameof(SelectedDailyReportStatus));
                OnPropertyChanged(nameof(SelectedStatus));
                OnPropertyChanged(nameof(SelectedColor));
                OnPropertyChanged(nameof(SelectedSalesman));
                OnPropertyChanged(nameof(SelectedCompany));
            }
        }

        private bool CanExecuteEdit(object parameter)
        {
            return parameter != null || SelectedDataRowView != null || SelectedWork != null;
        }

        private void ExecuteDelete(object parameter)
        {
            DailyWork workToDelete = null;

            if (SelectedDataRowView != null)
            {
                int id = Convert.ToInt32(SelectedDataRowView["Id"]);
                workToDelete = DailyWorks.FirstOrDefault(w => w.Id == id);
            }
            else if (SelectedWork != null)
            {
                workToDelete = SelectedWork;
            }

            if (workToDelete != null)
            {
                var result = MessageBox.Show(
                    $"Delete record for {workToDelete.Company}?\nPI: {workToDelete.PINumber}",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    DbHelper.DeleteDailyWork(workToDelete.Id);
                    System.Diagnostics.Debug.WriteLine($"[DailyWork] Deleted ID: {workToDelete.Id}");

                    DailyWorks.Remove(workToDelete);
                    RefreshDataView();
                    UpdateStatistics();
                    SelectedWork = null;
                    SelectedDataRowView = null;
                }
            }
        }

        private bool CanExecuteDelete(object parameter)
        {
            return parameter != null || SelectedDataRowView != null || SelectedWork != null;
        }

        private void ExecuteSave(object parameter)
        {
            if (EditingWork == null) return;

            if (string.IsNullOrWhiteSpace(EditingWork.Company))
            {
                MessageBox.Show("Company name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ✅ Check for duplicate before saving
            if (DbHelper.IsDuplicateDailyWork(EditingWork.CustomerReference, EditingWork.PINumber, EditingWork.Id))
            {
                var result = MessageBox.Show(
                    $"⚠️ Duplicate Entry!\n\nCustomer Reference: {EditingWork.CustomerReference}\nPI Number: {EditingWork.PINumber}\n\nThis combination already exists. Do you want to save anyway?",
                    "Duplicate Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    return;
            }

            if (_isNewRecord)
            {
                EditingWork.Id = DailyWorks.Count > 0 ? DailyWorks.Max(w => w.Id) + 1 : 1;
                EditingWork.CreatedDate = DateTime.Now;

                // Add new values to dropdown options
                AddToOptionsIfNew(TypeOfWorkOptions, EditingWork.TypeOfWork);
                AddToOptionsIfNew(ProductionStatusOptions, EditingWork.ProductionStatus);
                AddToOptionsIfNew(DailyReportStatusOptions, EditingWork.DailyReportStatus);
                AddToOptionsIfNew(StatusOptions, EditingWork.Status);
                AddToOptionsIfNew(ColorOptions, EditingWork.Color);
                AddToOptionsIfNew(SalesmanOptions, EditingWork.Salesman);
                AddToOptionsIfNew(CompanyOptions, EditingWork.Company);
                AddToOptionsIfNew(PINumberOptions, EditingWork.PINumber);
                AddToOptionsIfNew(CustomerReferenceOptions, EditingWork.CustomerReference);
                AddToOptionsIfNew(NotesOptions, EditingWork.Notes);

                // ✅ Save to autocomplete tables
                DbHelper.SaveCustomerReference(EditingWork.CustomerReference, EditingWork.Company);
                DbHelper.SaveNoteSuggestion(EditingWork.Notes);

                DbHelper.SaveDailyWork(EditingWork);
                System.Diagnostics.Debug.WriteLine($"[DailyWork] Saved new ID: {EditingWork.Id}");

                DailyWorks.Add(EditingWork);
            }
            else
            {
                var existing = DailyWorks.FirstOrDefault(w => w.Id == EditingWork.Id);
                if (existing != null)
                {
                    existing.Date = EditingWork.Date;
                    existing.UpdateDate = DateTime.Today;
                    existing.Company = EditingWork.Company;
                    existing.PINumber = EditingWork.PINumber;
                    existing.CustomerReference = EditingWork.CustomerReference;
                    existing.TypeOfWork = EditingWork.TypeOfWork;
                    existing.ProductionStatus = EditingWork.ProductionStatus;
                    existing.DailyReportStatus = EditingWork.DailyReportStatus;
                    existing.Qty = EditingWork.Qty;
                    existing.SQM = EditingWork.SQM;
                    existing.Status = EditingWork.Status;
                    existing.Salesman = EditingWork.Salesman;
                    existing.Color = EditingWork.Color;
                    existing.Notes = EditingWork.Notes;

                    // Add new values to dropdown options
                    AddToOptionsIfNew(TypeOfWorkOptions, EditingWork.TypeOfWork);
                    AddToOptionsIfNew(ProductionStatusOptions, EditingWork.ProductionStatus);
                    AddToOptionsIfNew(DailyReportStatusOptions, EditingWork.DailyReportStatus);
                    AddToOptionsIfNew(StatusOptions, EditingWork.Status);
                    AddToOptionsIfNew(ColorOptions, EditingWork.Color);
                    AddToOptionsIfNew(SalesmanOptions, EditingWork.Salesman);
                    AddToOptionsIfNew(CompanyOptions, EditingWork.Company);
                    AddToOptionsIfNew(PINumberOptions, EditingWork.PINumber);
                    AddToOptionsIfNew(CustomerReferenceOptions, EditingWork.CustomerReference);
                    AddToOptionsIfNew(NotesOptions, EditingWork.Notes);

                    // ✅ Save to autocomplete tables
                    DbHelper.SaveCustomerReference(EditingWork.CustomerReference, EditingWork.Company);
                    DbHelper.SaveNoteSuggestion(EditingWork.Notes);

                    DbHelper.UpdateDailyWork(existing);
                    System.Diagnostics.Debug.WriteLine($"[DailyWork] Updated ID: {existing.Id}");
                }
            }

            IsEditing = false;
            EditingWork = null;
            IsDuplicateWarning = false;
            DuplicateMessage = "";
            RefreshDataView();
            UpdateStatistics();
        }

        private bool CanExecuteSave(object parameter) => EditingWork != null;

        private void ExecuteCancel(object parameter)
        {
            IsEditing = false;
            EditingWork = null;
            IsDuplicateWarning = false;
            DuplicateMessage = "";
        }

        private void ExecuteRefresh(object parameter)
        {
            LoadFromDatabase();
            LoadOptionsFromDatabase();
            RefreshDataView();
            UpdateStatistics();
        }

        private void ExecuteExport(object parameter)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    DefaultExt = ".csv",
                    FileName = $"DailyWorks_Export_{DateTime.Now:yyyyMMdd_HHmmss}"
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
            var headers = new[] { "Date", "Update Date", "Company", "PI Number", "Customer Ref", "Type of Work", "Production Status", "Daily Report Status", "Qty", "SQM", "Status", "Salesman", "Color", "Notes" };
            sb.AppendLine(string.Join(",", headers));

            foreach (DataRowView rowView in FilteredDataView)
            {
                var fields = rowView.Row.ItemArray.Select(f => $"\"{f?.ToString()?.Replace("\"", "\"\"")}\"");
                sb.AppendLine(string.Join(",", fields));
            }

            System.IO.File.WriteAllText(filePath, sb.ToString(), System.Text.Encoding.UTF8);
        }

        private void ExecuteClearFilters(object parameter)
        {
            SearchText = "";
            FilterStatus = "";
            FilterProductionStatus = "";
            FilterTypeOfWork = "";
            FilterSalesman = "";
            FilterCompany = "";
            FilterColor = "";
            FilterPINumber = "";
            FilterStartDate = null;
            FilterEndDate = null;
            SortColumn = "";
            SortDirection = ListSortDirection.Ascending;
            ApplyFilters();
        }

        private void ExecuteSort(object parameter)
        {
            if (parameter is string columnName)
            {
                if (SortColumn == columnName)
                {
                    SortDirection = SortDirection == ListSortDirection.Ascending
                        ? ListSortDirection.Descending
                        : ListSortDirection.Ascending;
                }
                else
                {
                    SortColumn = columnName;
                    SortDirection = ListSortDirection.Ascending;
                }
                ApplyFilters();
            }
        }

        private void ExecuteCopyRow(object parameter)
        {
            DailyWork sourceWork = null;

            if (SelectedDataRowView != null)
            {
                int id = Convert.ToInt32(SelectedDataRowView["Id"]);
                sourceWork = DailyWorks.FirstOrDefault(w => w.Id == id);
            }
            else if (SelectedWork != null)
            {
                sourceWork = SelectedWork;
            }

            if (sourceWork != null)
            {
                var copy = sourceWork.Clone();
                copy.Id = 0;
                copy.PINumber = $"COPY_{copy.PINumber}";
                copy.Date = DateTime.Today;
                copy.UpdateDate = DateTime.Today;
                copy.CreatedDate = DateTime.Now;

                DbHelper.SaveDailyWork(copy);

                DailyWorks.Add(copy);
                RefreshDataView();
                UpdateStatistics();
            }
        }

        private bool CanExecuteCopyRow(object parameter)
        {
            return SelectedDataRowView != null || SelectedWork != null;
        }

        private void ExecuteDuplicateRow(object parameter)
        {
            DailyWork sourceWork = null;

            if (SelectedDataRowView != null)
            {
                int id = Convert.ToInt32(SelectedDataRowView["Id"]);
                sourceWork = DailyWorks.FirstOrDefault(w => w.Id == id);
            }
            else if (SelectedWork != null)
            {
                sourceWork = SelectedWork;
            }

            if (sourceWork != null)
            {
                var duplicate = sourceWork.Clone();
                duplicate.Id = DailyWorks.Count > 0 ? DailyWorks.Max(w => w.Id) + 1 : 1;
                duplicate.PINumber = $"DUP_{duplicate.PINumber}";
                duplicate.Date = DateTime.Today;
                duplicate.UpdateDate = DateTime.Today;
                duplicate.CreatedDate = DateTime.Now;

                DbHelper.SaveDailyWork(duplicate);

                DailyWorks.Add(duplicate);
                RefreshDataView();
                UpdateStatistics();
            }
        }

        private bool CanExecuteDuplicateRow(object parameter)
        {
            return SelectedDataRowView != null || SelectedWork != null;
        }

        private void UpdateStatistics()
        {
            OnPropertyChanged(nameof(TotalRecords));
            OnPropertyChanged(nameof(FilteredRecords));
            OnPropertyChanged(nameof(TotalSQM));
            OnPropertyChanged(nameof(TotalQty));
            OnPropertyChanged(nameof(FilteredSQM));
            OnPropertyChanged(nameof(FilteredQty));
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(SelectedRecords));
            OnPropertyChanged(nameof(SelectedQty));
            OnPropertyChanged(nameof(SelectedSQM));
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
                        printDialog.PrintVisual(printVisual, "Daily Works Report");
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
                Text = "DAILY WORKS REPORT",
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
                Text = $"Records: {FilteredRecords} | Total Qty: {TotalQty} | Total SQM: {TotalSQM:N2}",
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
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Production", Binding = new System.Windows.Data.Binding("ProductionStatus"), Width = 90 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Qty", Binding = new System.Windows.Data.Binding("Qty"), Width = 50 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "SQM", Binding = new System.Windows.Data.Binding("SQM") { StringFormat = "N2" }, Width = 60 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Status", Binding = new System.Windows.Data.Binding("Status"), Width = 70 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Salesman", Binding = new System.Windows.Data.Binding("Salesman"), Width = 90 });

            grid.Children.Add(dataGrid);

            return grid;
        }

        private void ExecuteDeleteSelected(object parameter)
        {
            if (parameter is DataGrid dataGrid)
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
                    $"Delete {selectedIds.Count} selected record(s)?\n\nThis action cannot be undone.",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    foreach (var id in selectedIds)
                    {
                        DbHelper.DeleteDailyWork(id);
                        var item = DailyWorks.FirstOrDefault(w => w.Id == id);
                        if (item != null)
                        {
                            DailyWorks.Remove(item);
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[DailyWork] Deleted {selectedIds.Count} records");

                    // ✅ Clear selection after delete
                    _selectedIds.Clear();
                    _selectedCount = 0;

                    RefreshDataView();
                    UpdateStatistics();
                    SelectedWork = null;
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