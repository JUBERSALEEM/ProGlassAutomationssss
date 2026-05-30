using ProGlassAutomation.Data.Database;
using ProGlassAutomation.Models;

// Add this alias to disambiguate:
using DbDailyWork = ProGlassAutomation.Data.Database.DailyWork;
using InvoiceModel = ProGlassAutomation.Models.ProformaInvoiceModel;

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
        private ObservableCollection<DbDailyWork> _dailyWorks;
        private DbDailyWork _selectedWork;
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
        private string _filterCustomerReference = "";
        private DateTime? _filterStartDate;
        private DateTime? _filterEndDate;
        private bool _isEditing;
        private DbDailyWork _editingWork;
        private bool _isNewRecord;

        // OPTIONS - Empty collections, populated dynamically
        public ObservableCollection<string> TypeOfWorkOptions { get; } = new();
        public ObservableCollection<string> ProductionStatusOptions { get; } = new();
        public ObservableCollection<string> DailyReportStatusOptions { get; } = new();
        public ObservableCollection<string> StatusOptions { get; } = new();
        public ObservableCollection<string> ColorOptions { get; } = new();
        public ObservableCollection<string> SalesmanOptions { get; } = new();
        public ObservableCollection<string> CompanyOptions { get; } = new();
        public ObservableCollection<string> PINumberOptions { get; } = new();
        public ObservableCollection<string> CustomerReferenceOptions { get; } = new();
        public ObservableCollection<string> NotesOptions { get; } = new();

        // Proforma Invoice connection
        private ViewModels.ProformaInvoiceViewModel _proformaInvoiceVM;
        public ViewModels.ProformaInvoiceViewModel ProformaInvoiceVM
        {
            get => _proformaInvoiceVM;
            set => SetProperty(ref _proformaInvoiceVM, value);
        }

        // Navigation event to trigger view switch
        public event Action? RequestNavigateToInvoice;

        // Current selected values (for editing)
        private string _selectedTypeOfWork = "";
        private string _selectedProductionStatus = "";
        private string _selectedDailyReportStatus = "";
        private string _selectedStatus = "";
        private string _selectedColor = "";
        private string _selectedSalesman = "";
        private string _selectedCompany = "";

        // Current selected indices
        private int _typeOfWorkIndex = -1;
        private int _productionStatusIndex = -1;
        private int _dailyReportStatusIndex = -1;
        private int _statusIndex = -1;
        private int _colorIndex = -1;
        private int _salesmanIndex = -1;
        private int _companyIndex = -1;

        // Properties for Selected Values
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

        // Filter Properties
        public string FilterCustomerReference
        {
            get => _filterCustomerReference;
            set
            {
                if (SetProperty(ref _filterCustomerReference, value))
                    ApplyFilters();
            }
        }

        // Status message for user feedback
        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // Duplicate warning
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
            DailyWorks = new ObservableCollection<DbDailyWork>();

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
            LoadToInvoiceCommand = new RelayCommand(ExecuteLoadToInvoice);

            // PATCH: Load from database first, then merge JSON entries
            LoadFromDatabase();
            LoadFromJsonFile();
            LoadOptionsFromDatabase();
            CreateDataView();
        }

        // HELPER - Add to options if new
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

        // LOAD OPTIONS FROM DATABASE
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

                // Load from autocomplete tables
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

        // CHECK DUPLICATE
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
                    ? $"Duplicate: {EditingWork.CustomerReference} + {EditingWork.PINumber} already exists!"
                    : "";
            }
            else
            {
                IsDuplicateWarning = false;
                DuplicateMessage = "";
            }
        }

        private double ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;

            if (double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out double result))
                return result;

            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                return result;

            if (double.TryParse(value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                return result;

            return 0;
        }

        #region Properties

        public ObservableCollection<DbDailyWork> DailyWorks
        {
            get => _dailyWorks;
            set => SetProperty(ref _dailyWorks, value);
        }

        public DataView FilteredDataView
        {
            get => _filteredDataView;
            private set => SetProperty(ref _filteredDataView, value);
        }

        public DbDailyWork SelectedWork
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

        private List<int> _selectedIds = new List<int>();
        public List<DbDailyWork> SelectedRecordsData => DailyWorks?.Where(w => _selectedIds.Contains(w.Id)).ToList() ?? new List<DbDailyWork>();

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

        public DbDailyWork EditingWork
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
        public ICommand LoadToInvoiceCommand { get; }

        // Event handler for ProformaInvoice save event
        public void OnProformaInvoiceSaved(InvoiceModel invoice)
        {
            System.Diagnostics.Debug.WriteLine($"[DailyWork] ✅ Received InvoiceSaved event for: {invoice?.InvoiceNo}");
            if (invoice != null)
            {
                UpdateFromProformaInvoice(invoice);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[DailyWork] ❌ Invoice is NULL!");
            }
        }

        // Method to update DailyWorks from ProformaInvoice
        private void UpdateFromProformaInvoice(InvoiceModel invoice)
        {
            if (invoice == null)
            {
                System.Diagnostics.Debug.WriteLine("[DailyWork] ❌ UpdateFromProformaInvoice: invoice is NULL");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[DailyWork] Updating from ProformaInvoice: {invoice.InvoiceNo}");

            // Calculate totals from specifications
            double totalSQM = 0;
            int totalQty = 0;
            if (invoice.Specifications != null)
            {
                foreach (var spec in invoice.Specifications)
                {
                    totalSQM += spec.SpecTotalSQM;
                    totalQty += spec.SpecTotalQty;
                }
            }

            // Collect colors from all specifications
            var colorsFromSpecs = new List<string>();
            if (invoice.Specifications != null)
            {
                foreach (var spec in invoice.Specifications)
                {
                    var specColor = ExtractColorFromSpecification(spec);
                    if (!string.IsNullOrEmpty(specColor))
                    {
                        colorsFromSpecs.Add(specColor);
                    }
                }
            }

            string finalColor;
            if (colorsFromSpecs.Count > 0)
            {
                finalColor = string.Join(" | ", colorsFromSpecs);
            }
            else if (!string.IsNullOrEmpty(invoice.Color))
            {
                finalColor = invoice.Color;
            }
            else
            {
                finalColor = "";
            }

            // Find matching DailyWork by PINumber = ProjectNo OR InvoiceNo
            var matchingWork = DailyWorks?.FirstOrDefault(w =>
                (!string.IsNullOrEmpty(w.PINumber) && !string.IsNullOrEmpty(invoice.ProjectNo) &&
                 w.PINumber.Equals(invoice.ProjectNo, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(w.PINumber) && !string.IsNullOrEmpty(invoice.InvoiceNo) &&
                 w.PINumber.Equals(invoice.InvoiceNo, StringComparison.OrdinalIgnoreCase)));

            // If not found by ProjectNo/InvoiceNo, try by Customer Reference
            if (matchingWork == null && !string.IsNullOrEmpty(invoice.CustomerName))
            {
                matchingWork = DailyWorks?.FirstOrDefault(w =>
                    !string.IsNullOrEmpty(w.CustomerReference) &&
                    w.CustomerReference.Equals(invoice.CustomerName, StringComparison.OrdinalIgnoreCase));
            }

            bool isNewRecord = false;

            if (matchingWork != null)
            {
                System.Diagnostics.Debug.WriteLine($"[DailyWork] ✅ Found existing record ID: {matchingWork.Id}");

                // Show confirmation dialog with options
                var result = MessageBox.Show(
                    $"A Daily Work entry already exists for this invoice.\n\n" +
                    $"┌─────────────────────────────────────────────────────┐\n" +
                    $"│ EXISTING ENTRY                                     │\n" +
                    $"├─────────────────────────────────────────────────────┤\n" +
                    $"│ ID:              {matchingWork.Id,-30}│\n" +
                    $"│ PI Number:       {matchingWork.PINumber,-30}│\n" +
                    $"│ Customer Ref:    {matchingWork.CustomerReference,-30}│\n" +
                    $"│ Status:          {matchingWork.Status,-30}│\n" +
                    $"│ Type of Work:    {matchingWork.TypeOfWork,-30}│\n" +
                    $"│ Production:      {matchingWork.ProductionStatus,-30}│\n" +
                    $"│ Qty:             {matchingWork.Qty,-30}│\n" +
                    $"│ SQM:             {matchingWork.SQM:F2}{-28}│\n" +
                    $"└─────────────────────────────────────────────────────┘\n\n" +
                    $"What would you like to do?\n\n" +
                    $"[YES]     → Create NEW entry (keep existing)\n" +
                    $"[NO]      → UPDATE existing entry with invoice data\n" +
                    $"[CANCEL]  → Skip (don't save to Daily Work)",
                    "Daily Work Entry Exists",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                {
                    System.Diagnostics.Debug.WriteLine("[DailyWork] ⏭️ User cancelled - skipping Daily Work save");
                    StatusMessage = "⏭️ Daily Work save cancelled by user";
                    return;
                }
                else if (result == MessageBoxResult.Yes)
                {
                    // User wants to create NEW entry - keep existing and create new one
                    System.Diagnostics.Debug.WriteLine("[DailyWork] ➕ User chose to create NEW entry");
                    isNewRecord = true;
                    matchingWork = new DbDailyWork
                    {
                        Id = DailyWorks.Count > 0 ? DailyWorks.Max(w => w.Id) + 1 : 1,
                        Date = DateTime.Today,
                        CreatedDate = DateTime.Now,
                        Status = "Pending"  // Default status for new entry
                    };
                }
                else // MessageBoxResult.No
                {
                    // User wants to UPDATE existing entry
                    System.Diagnostics.Debug.WriteLine("[DailyWork] 🔄 User chose to UPDATE existing entry");
                    isNewRecord = false;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[DailyWork] No existing record found, creating new...");
                isNewRecord = true;
                matchingWork = new DbDailyWork
                {
                    Id = DailyWorks.Count > 0 ? DailyWorks.Max(w => w.Id) + 1 : 1,
                    Date = DateTime.Today,
                    CreatedDate = DateTime.Now,
                    Status = "Pending"
                };
            }

            // Update all fields from ProformaInvoice
            matchingWork.PINumber = invoice.InvoiceNo ?? matchingWork.PINumber ?? "";
            matchingWork.Company = invoice.CustomerName ?? matchingWork.Company ?? "";
            matchingWork.CustomerReference = invoice.ProjectName ?? matchingWork.CustomerReference ?? "";
            matchingWork.Salesman = invoice.Salesman ?? matchingWork.Salesman ?? "";
            matchingWork.Color = finalColor;
            matchingWork.UpdateDate = DateTime.Today;
            matchingWork.SQM = totalSQM;
            matchingWork.Qty = totalQty;

            // Build comprehensive notes from invoice
            var notesList = new List<string>();
            if (!string.IsNullOrEmpty(invoice.ProjectLocation))
                notesList.Add($"Location: {invoice.ProjectLocation}");
            if (!string.IsNullOrEmpty(invoice.LPONo))
                notesList.Add($"LPO: {invoice.LPONo}");
            if (!string.IsNullOrEmpty(invoice.AttentionName))
                notesList.Add($"Attention: {invoice.AttentionName}");
            if (!string.IsNullOrEmpty(invoice.ContactNo))
                notesList.Add($"Contact: {invoice.ContactNo}");
            if (!string.IsNullOrEmpty(invoice.CompanyName))
                notesList.Add($"Company: {invoice.CompanyName}");
            if (!string.IsNullOrEmpty(invoice.CompanyTRN))
                notesList.Add($"TRN: {invoice.CompanyTRN}");
            if (!string.IsNullOrEmpty(invoice.Notes))
                notesList.Add(invoice.Notes);
            matchingWork.Notes = string.Join(" | ", notesList);

            // Set TypeOfWork from first specification's ModuleType
            if (invoice.Specifications != null && invoice.Specifications.Count > 0)
            {
                var firstSpec = invoice.Specifications[0];
                matchingWork.TypeOfWork = firstSpec.ModuleType ?? "SGU";
            }
            else
            {
                matchingWork.TypeOfWork = matchingWork.TypeOfWork ?? "SGU";
            }

            // Set default statuses if new record
            if (isNewRecord)
            {
                matchingWork.ProductionStatus = matchingWork.ProductionStatus ?? "Pending";
                matchingWork.DailyReportStatus = matchingWork.DailyReportStatus ?? "Pending";
                matchingWork.Status = matchingWork.Status ?? "Pending";
            }

            // Add to options if new values
            AddToOptionsIfNew(PINumberOptions, matchingWork.PINumber);
            AddToOptionsIfNew(CompanyOptions, matchingWork.Company);
            AddToOptionsIfNew(SalesmanOptions, matchingWork.Salesman);
            AddToOptionsIfNew(ColorOptions, matchingWork.Color);
            AddToOptionsIfNew(CustomerReferenceOptions, matchingWork.CustomerReference);

            // Save to database
            if (isNewRecord)
            {
                DbHelper.SaveDailyWork(matchingWork);
                DailyWorks.Add(matchingWork);
                System.Diagnostics.Debug.WriteLine($"[DailyWork] ✅ Created NEW DailyWork ID: {matchingWork.Id}");
                StatusMessage = $"✅ Created NEW Daily Work: {matchingWork.PINumber}";
            }
            else
            {
                DbHelper.UpdateDailyWork(matchingWork);
                System.Diagnostics.Debug.WriteLine($"[DailyWork] ✅ Updated DailyWork ID: {matchingWork.Id}");
                StatusMessage = $"🔄 Updated Daily Work: {matchingWork.PINumber}";
            }

            // Refresh the DataGrid
            RefreshDataView();
            UpdateStatistics();
            OnPropertyChanged(nameof(FilteredDataView));
            System.Diagnostics.Debug.WriteLine("[DailyWork] ✅ Refresh complete");
        }

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

            var newView = dataTable.DefaultView;
            System.Diagnostics.Debug.WriteLine($"[DailyWork] CreateDataView - new view row count: {newView.Count}");
            FilteredDataView = newView;
            OnPropertyChanged(nameof(FilteredDataView));
        }

        private void RefreshDataView()
        {
            System.Diagnostics.Debug.WriteLine($"[DailyWork] RefreshDataView called. Current records: {DailyWorks?.Count ?? 0}");

            // Store current state
            string currentSort = FilteredDataView?.Sort ?? "";
            string currentFilter = FilteredDataView?.RowFilter ?? "";

            // Recreate DataView
            CreateDataView();

            // Restore state
            if (!string.IsNullOrEmpty(currentSort) && FilteredDataView != null)
                FilteredDataView.Sort = currentSort;
            if (!string.IsNullOrEmpty(currentFilter) && FilteredDataView != null)
                FilteredDataView.RowFilter = currentFilter;

            System.Diagnostics.Debug.WriteLine($"[DailyWork] DataView recreated. Filtered count: {FilteredDataView?.Count ?? 0}");

            UpdateStatistics();
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
                    bool alreadyExists = DailyWorks.Any(w =>
                        w.Id == work.Id ||

                        (!string.IsNullOrWhiteSpace(w.PINumber) &&
                         w.PINumber.Equals(work.PINumber,
                             StringComparison.OrdinalIgnoreCase)));

                    if (!alreadyExists)
                    {
                        DailyWorks.Add(work);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[DailyWork] Loaded {DailyWorks.Count} records from DB");
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DailyWork] Load error: {ex.Message}");
            }
        }

        // PATCH: Load Daily Works from local JSON files (one file per entry)
        // PATCH-OPTIMIZE: Load DB records once only
        private void LoadFromJsonFile()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string dataFolder = System.IO.Path.Combine(baseDir, "Data");

                if (!System.IO.Directory.Exists(dataFolder))
                {
                    StatusMessage = "⚠️ Data folder not found";
                    return;
                }

                // Get only PI-*.json files (DailyWorks files)
                var allJsonFiles = System.IO.Directory.GetFiles(dataFolder, "*.json");

                // Filter to only PI-*.json files (DailyWorks format)
                var jsonFiles = allJsonFiles.Where(f =>
                {
                    string fileName = System.IO.Path.GetFileName(f);
                    return fileName.StartsWith("PI-", StringComparison.OrdinalIgnoreCase);
                }).ToArray();

                if (jsonFiles.Length == 0)
                {
                    StatusMessage = "ℹ️ No PI-*.json files found in Data folder";
                    return;
                }

                var settings = new Newtonsoft.Json.JsonSerializerSettings
                {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                    DateFormatString = "yyyy-MM-ddTHH:mm:ss"
                };

                int addedCount = 0;
                int skippedCount = 0;
                int errorCount = 0;

                // PATCH-OPTIMIZE: Load DB records once only
                var dbRecords = DbHelper.GetAllDailyWork();

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

                        var work = Newtonsoft.Json.JsonConvert.DeserializeObject<DbDailyWork>(json, settings);

                        if (work == null)
                        {
                            errorCount++;
                            continue;
                        }

                        // PATCH: Skip invalid empty records
                        if (string.IsNullOrWhiteSpace(work.PINumber) &&
                            string.IsNullOrWhiteSpace(work.CustomerReference))
                        {
                            skippedCount++;
                            continue;
                        }

                        string jsonPINumber = (work.PINumber ?? "").Trim();
                        string jsonCustRef = (work.CustomerReference ?? "").Trim();

                        // Check in-memory DailyWorks
                        bool foundInMemory = DailyWorks.Any(w =>
                            (!string.IsNullOrEmpty(jsonPINumber) && jsonPINumber.Equals((w.PINumber ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(jsonCustRef) && jsonCustRef.Equals((w.CustomerReference ?? "").Trim(), StringComparison.OrdinalIgnoreCase)));

                        if (foundInMemory)
                        {
                            skippedCount++;
                            continue;
                        }

                        // PATCH-OPTIMIZE: Check cached database records (not calling DB inside loop)
                        bool foundInDB = dbRecords.Any(w =>
                            (!string.IsNullOrEmpty(jsonPINumber) &&
                             jsonPINumber.Equals((w.PINumber ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(jsonCustRef) &&
                             jsonCustRef.Equals((w.CustomerReference ?? "").Trim(), StringComparison.OrdinalIgnoreCase)));

                        if (foundInDB)
                        {
                            skippedCount++;
                            continue;
                        }

                        // PATCH: Generate stable ID if JSON has invalid ID
                        if (work.Id <= 0)
                        {
                            var existingByPI = DailyWorks.FirstOrDefault(w =>
                                !string.IsNullOrWhiteSpace(work.PINumber) &&
                                (w.PINumber ?? "").Trim()
                                    .Equals((work.PINumber ?? "").Trim(),
                                        StringComparison.OrdinalIgnoreCase));

                            if (existingByPI != null)
                            {
                                work.Id = existingByPI.Id;
                            }
                        }

                        // PATCH: Final duplicate protection
                        bool alreadyAdded = DailyWorks.Any(w =>
                            w.Id == work.Id ||
                            (!string.IsNullOrWhiteSpace(work.PINumber) &&
                             string.Equals((w.PINumber ?? "").Trim(), work.PINumber.Trim(), StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(work.CustomerReference) &&
                             string.Equals((w.CustomerReference ?? "").Trim(), work.CustomerReference.Trim(), StringComparison.OrdinalIgnoreCase)));

                        if (!alreadyAdded)
                        {
                            DailyWorks.Add(work);
                            addedCount++;
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DailyWork] Error reading {fileName}: {ex.Message}");
                        errorCount++;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[DailyWork] JSON load complete: {addedCount} added, {skippedCount} skipped, {errorCount} errors");
                StatusMessage = $"✅ Added {addedCount}, Skipped {skippedCount} duplicates";

                // PATCH: Removed automatic DB merge save
                // Imported JSON data should NOT be re-saved automatically
                // because it creates duplicate/random entries.
            }
            catch (UnauthorizedAccessException unauthEx)
            {
                StatusMessage = "❌ Access denied to Data folder";
            }
            catch (System.IO.IOException ioEx)
            {
                StatusMessage = "❌ File I/O error";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error loading JSON: {ex.Message}";
            }
        }

        // PATCH DISABLED: This method caused duplicate/random entries
        // during refresh/import cycle.
        private void SaveMergedToDatabase()
        {
            // PATCH DISABLED
            // This method caused duplicate/random entries
            // during refresh/import cycle.
            System.Diagnostics.Debug.WriteLine("[DailyWork] SaveMergedToDatabase() is DISABLED");
        }

        // PATCH: Update JSON file - remove deleted entries
        private void UpdateJsonFileAfterDelete(List<int> deletedIds)
        {
            try
            {
                string jsonPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Data",
                    "DailyWorks.json");

                if (!System.IO.File.Exists(jsonPath))
                {
                    jsonPath = System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "DailyWorks.json");
                }

                if (!System.IO.File.Exists(jsonPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[DailyWork] No JSON file to update");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[DailyWork] Updating JSON file after delete...");

                var json = System.IO.File.ReadAllText(jsonPath);
                var settings = new Newtonsoft.Json.JsonSerializerSettings
                {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                };

                var jsonWorks = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DbDailyWork>>(json, settings);

                if (jsonWorks != null && jsonWorks.Count > 0)
                {
                    var originalCount = jsonWorks.Count;
                    jsonWorks = jsonWorks.Where(w => !deletedIds.Contains(w.Id)).ToList();

                    if (jsonWorks.Count < originalCount)
                    {
                        var updatedJson = Newtonsoft.Json.JsonConvert.SerializeObject(jsonWorks, Newtonsoft.Json.Formatting.Indented, settings);
                        System.IO.File.WriteAllText(jsonPath, updatedJson);

                        System.Diagnostics.Debug.WriteLine($"[DailyWork] Removed {originalCount - jsonWorks.Count} entries from JSON");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DailyWork] UpdateJsonFileAfterDelete error: {ex.Message}");
            }
        }

        #endregion

        #region Filter Implementation

        // PATCH: Escape single quotes in filters to prevent crashes on names like O'Brien
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
                filterExpressions.Add($"Status = '{FilterStatus.Replace("'", "''")}'");

            if (!string.IsNullOrWhiteSpace(FilterProductionStatus))
                filterExpressions.Add($"ProductionStatus = '{FilterProductionStatus.Replace("'", "''")}'");

            if (!string.IsNullOrWhiteSpace(FilterTypeOfWork))
                filterExpressions.Add($"TypeOfWork = '{FilterTypeOfWork.Replace("'", "''")}'");

            if (!string.IsNullOrWhiteSpace(FilterSalesman))
                filterExpressions.Add($"Salesman = '{FilterSalesman.Replace("'", "''")}'");

            if (!string.IsNullOrWhiteSpace(FilterCompany))
                filterExpressions.Add($"Company = '{FilterCompany.Replace("'", "''")}'");

            if (!string.IsNullOrWhiteSpace(FilterColor))
                filterExpressions.Add($"Color = '{FilterColor.Replace("'", "''")}'");

            if (!string.IsNullOrWhiteSpace(FilterPINumber))
                filterExpressions.Add($"PINumber = '{FilterPINumber.Replace("'", "''")}'");

            if (!string.IsNullOrWhiteSpace(FilterCustomerReference))
                filterExpressions.Add($"CustomerReference = '{FilterCustomerReference.Replace("'", "''")}'");

            if (FilterStartDate.HasValue)
                filterExpressions.Add($"Date >= #{FilterStartDate.Value:yyyy-MM-dd}#");

            // PATCH: Fix end date to include the entire day
            if (FilterEndDate.HasValue)
                filterExpressions.Add($"Date < #{FilterEndDate.Value.AddDays(1):yyyy-MM-dd}#");

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
            EditingWork = new DbDailyWork
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

        // PATCH: Prevent null reference with DBNull checks
        private void ExecuteEdit(object parameter)
        {
            DbDailyWork workToEdit = null;

            if (SelectedDataRowView != null)
            {
                var dataRow = SelectedDataRowView;
                workToEdit = new DbDailyWork
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
                    // PATCH: Handle DBNull for Qty and SQM
                    Qty = dataRow["Qty"] != DBNull.Value
                        ? Convert.ToInt32(dataRow["Qty"])
                        : 0,
                    SQM = dataRow["SQM"] != DBNull.Value
                        ? Convert.ToDouble(dataRow["SQM"])
                        : 0,
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
            DbDailyWork workToDelete = null;

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
                    // PATCH: Store ID before deletion for JSON update
                    var deletedId = workToDelete.Id;

                    DbHelper.DeleteDailyWork(workToDelete.Id);
                    System.Diagnostics.Debug.WriteLine($"[DailyWork] Deleted ID: {workToDelete.Id}");

                    DailyWorks.Remove(workToDelete);

                    // PATCH: Also remove from JSON file
                    UpdateJsonFileAfterDelete(new List<int> { deletedId });

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

            if (DbHelper.IsDuplicateDailyWork(EditingWork.CustomerReference, EditingWork.PINumber, EditingWork.Id))
            {
                var result = MessageBox.Show(
                    $"Duplicate Entry!\n\nCustomer Reference: {EditingWork.CustomerReference}\nPI Number: {EditingWork.PINumber}\n\nThis combination already exists. Do you want to save anyway?",
                    "Duplicate Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    return;
            }

            if (_isNewRecord)
            {
                EditingWork.Id = DailyWorks.Count > 0 ? DailyWorks.Max(w => w.Id) + 1 : 1;
                EditingWork.CreatedDate = DateTime.Now;

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
            try
            {
                // PATCH: Prevent duplicate accumulation
                DailyWorks.Clear();

                // PATCH: Reset selections
                SelectedWork = null;
                SelectedDataRowView = null;

                System.Diagnostics.Debug.WriteLine("[DailyWork] Refresh started...");

                // Reload fresh data
                LoadFromDatabase();

                // PATCH: Load JSON for display only
                // DO NOT save imported records automatically
                LoadFromJsonFile();

                LoadOptionsFromDatabase();

                RefreshDataView();
                UpdateStatistics();

                System.Diagnostics.Debug.WriteLine(
                    $"[DailyWork] Refresh complete. Records: {DailyWorks.Count}");

                StatusMessage = "✅ Refresh completed";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[DailyWork] Refresh error: {ex.Message}");

                StatusMessage = "❌ Refresh failed";
            }
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

        // PATCH: Fix CSV export - explicitly select columns to avoid Id column
        private void ExportToCSV(string filePath)
        {
            var sb = new System.Text.StringBuilder();
            var headers = new[] { "Date", "Update Date", "Company", "PI Number", "Customer Ref", "Type of Work", "Production Status", "Daily Report Status", "Qty", "SQM", "Status", "Salesman", "Color", "Notes" };
            sb.AppendLine(string.Join(",", headers.Select(h => $"\"{h}\"")));

            foreach (DataRowView rowView in FilteredDataView)
            {
                // PATCH: Explicitly select columns to avoid internal Id column
                var fields = new[]
                {
                    rowView["Date"]?.ToString(),
                    rowView["UpdateDate"]?.ToString(),
                    rowView["Company"]?.ToString(),
                    rowView["PINumber"]?.ToString(),
                    rowView["CustomerReference"]?.ToString(),
                    rowView["TypeOfWork"]?.ToString(),
                    rowView["ProductionStatus"]?.ToString(),
                    rowView["DailyReportStatus"]?.ToString(),
                    rowView["Qty"]?.ToString(),
                    rowView["SQM"]?.ToString(),
                    rowView["Status"]?.ToString(),
                    rowView["Salesman"]?.ToString(),
                    rowView["Color"]?.ToString(),
                    rowView["Notes"]?.ToString()
                }
                .Select(f => $"\"{f?.Replace("\"", "\"\"")}\"");

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
            FilterCustomerReference = "";
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

        // PATCH: Fix duplicate COPY_/DUP_ prefix stacking
        private void ExecuteCopyRow(object parameter)
        {
            DbDailyWork sourceWork = null;

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

                // PATCH: Clean existing prefixes before adding new one
                var cleanPINumber = (copy.PINumber ?? "")
                    .Replace("COPY_", "")
                    .Replace("DUP_", "");
                copy.PINumber = $"COPY_{cleanPINumber}";

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

        // PATCH: Fix duplicate COPY_/DUP_ prefix stacking
        private void ExecuteDuplicateRow(object parameter)
        {
            DbDailyWork sourceWork = null;

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

                // PATCH: Clean existing prefixes before adding new one
                var cleanDuplicatePI = (duplicate.PINumber ?? "")
                    .Replace("COPY_", "")
                    .Replace("DUP_", "");
                duplicate.PINumber = $"DUP_{cleanDuplicatePI}";

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

        // PATCH: Fix print grid overlay layout with proper Grid.Row assignments
        private Grid CreatePrintVisual()
        {
            var grid = new Grid { Margin = new Thickness(20) };

            // PATCH: Define rows properly to prevent overlap
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

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

            // PATCH: Set header to row 0
            grid.Children.Add(headerPanel);
            Grid.SetRow(headerPanel, 0);

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

            // PATCH: Set DataGrid to row 1
            grid.Children.Add(dataGrid);
            Grid.SetRow(dataGrid, 1);

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
                    // PATCH: Save IDs before deletion for JSON update
                    var deletedIds = new List<int>(selectedIds);

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

                    // PATCH: Also remove from JSON file
                    UpdateJsonFileAfterDelete(deletedIds);

                    _selectedIds.Clear();
                    _selectedCount = 0;

                    // PATCH: Notify property changed after clearing selection
                    OnPropertyChanged(nameof(SelectedCount));

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

        private void ExecuteLoadToInvoice(object parameter)
        {
            System.Diagnostics.Debug.WriteLine($"[DailyWork] ExecuteLoadToInvoice called. SelectedDataRowView={(SelectedDataRowView != null)}");

            DbDailyWork workToLoad = null;

            // First try: Get from SelectedDataRowView (bound from XAML)
            if (SelectedDataRowView != null)
            {
                System.Diagnostics.Debug.WriteLine("[DailyWork] Using SelectedDataRowView");
                int id = Convert.ToInt32(SelectedDataRowView["Id"]);
                workToLoad = DailyWorks.FirstOrDefault(w => w.Id == id);
            }
            // Second try: Get from SelectedWork
            else if (SelectedWork != null)
            {
                System.Diagnostics.Debug.WriteLine("[DailyWork] Using SelectedWork");
                workToLoad = SelectedWork;
            }
            // Third try: Get from DataGrid parameter
            else if (parameter is System.Windows.Controls.DataGrid dg)
            {
                System.Diagnostics.Debug.WriteLine($"[DailyWork] DataGrid param - SelectedItem={(dg.SelectedItem != null)}");
                if (dg.SelectedItem is DataRowView drv)
                {
                    int id = Convert.ToInt32(drv["Id"]);
                    workToLoad = DailyWorks.FirstOrDefault(w => w.Id == id);
                }
            }

            if (workToLoad != null && ProformaInvoiceVM != null)
            {
                ProformaInvoiceVM.LoadFromDailyWork(workToLoad);
                System.Diagnostics.Debug.WriteLine($"[DailyWork] Loaded to Invoice: {workToLoad.PINumber}");

                // Trigger navigation to Proforma Invoice view
                RequestNavigateToInvoice?.Invoke();
            }
            else if (workToLoad != null)
            {
                System.Diagnostics.Debug.WriteLine($"[DailyWork] ProformaInvoiceVM is NULL");
                MessageBox.Show("ProformaInvoiceVM not connected.\nPlease set ProformaInvoiceVM in MainWindow.",
                    "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[DailyWork] No workToLoad found");
                MessageBox.Show("Please select a record first, or open Proforma Invoice page.",
                    "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool CanExecuteLoadToInvoice(object parameter)
        {
            // For testing - always return true (remove this after testing)
            return true;
        }

        // Helper method to extract colors from specification
        private string ExtractColorFromSpecification(SpecificationModel spec)
        {
            if (spec == null) return "";

            // Get specification name (contains the glass description)
            var specName = spec.SpecificationName ?? "";

            // Known color keywords (order matters - longer/more specific first)
            var knownColors = new[]
            {
                "HD Grey", "HD Blue", "HD Green", "HD Bronze", "HD Black", "HD White",
                "Grey", "Green", "Blue", "Bronze", "Black", "Brown", "Yellow", "Red", "Orange", "Clear", "White",
                "Teal", "Navy", "Rose", "Amber", "Violet", "Pink", "Purple", "Champagne", "Gold", "Silver"
            };

            // Split by "+" to handle DGU/LAM specs with multiple glass layers
            var layers = specName.Split('+');
            var colors = new List<string>();

            foreach (var layer in layers)
            {
                var layerTrimmed = layer.Trim();
                bool foundColor = false;

                // Find the first color match in this layer
                foreach (var color in knownColors)
                {
                    if (layerTrimmed.Contains(color, StringComparison.OrdinalIgnoreCase))
                    {
                        colors.Add(color);
                        foundColor = true;
                        break; // Only take first color per layer
                    }
                }

                // If no known color found, try to extract any word before "Glass"
                if (!foundColor)
                {
                    var colorPattern = ExtractColorFromPattern(layerTrimmed);
                    if (!string.IsNullOrEmpty(colorPattern))
                    {
                        colors.Add(colorPattern);
                    }
                }
            }

            if (colors.Count > 0)
            {
                return string.Join(" + ", colors);
            }

            return "";
        }

        // Helper to extract any color-like word from text
        private string ExtractColorFromPattern(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            // Remove common glass type words
            var cleanText = text
                .Replace("FT Glass", "")
                .Replace("Annealed", "")
                .Replace("Tempered", "")
                .Replace("Glass", "")
                .Replace("ASP", "");

            // Try to find any common color word pattern
            var words = cleanText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Known color patterns to look for
            var colorKeywords = new[] { "HD", "Grey", "Gray", "Green", "Blue", "Bronze", "Black", "Brown", "Yellow", "Red", "Orange", "Clear", "White", "Teal", "Navy", "Rose", "Amber", "Violet", "Pink", "Purple", "Champagne", "Gold", "Silver" };

            // Find consecutive color words
            var colorWords = new List<string>();
            bool lastWasColor = false;

            foreach (var word in words)
            {
                if (colorKeywords.Any(c => word.Equals(c, StringComparison.OrdinalIgnoreCase)))
                {
                    colorWords.Add(word);
                    lastWasColor = true;
                }
                else if (lastWasColor && IsColorDescriptor(word))
                {
                    // Keep the descriptor but don't add it separately
                    lastWasColor = false;
                }
                else
                {
                    lastWasColor = false;
                }
            }

            if (colorWords.Count > 0)
            {
                return string.Join(" ", colorWords);
            }

            return "";
        }

        // Check if word is a color descriptor
        private bool IsColorDescriptor(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            var descriptors = new[] { "Tinted", "Reflective", "Mirror", "LowE", "Solar" };
            return descriptors.Any(d => word.Contains(d, StringComparison.OrdinalIgnoreCase));
        }

        #endregion
    }
}