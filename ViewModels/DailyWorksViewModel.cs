using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProGlassAutomation.Data;
using ProGlassAutomation.Data.Database;
using ProGlassAutomation.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ProGlassAutomation.ViewModels
{
    /// <summary>
    /// ViewModel - Manual properties, uses RelayCommand from toolkit
    /// </summary>
    // PATCH 54: Implement IDisposable for proper cleanup
    public partial class DailyWorksViewModel : ObservableObject, IDisposable
    {
        private readonly IDailyWorkRepository _repository;

        // Main collection
        private ObservableCollection<DailyWorkModel> _dailyWorks = new();
        public ObservableCollection<DailyWorkModel> DailyWorks
        {
            get => _dailyWorks;
            set => SetProperty(ref _dailyWorks, value);
        }

        // Filtered view
        private ICollectionView? _filteredView;
        public ICollectionView FilteredDataView
        {
            get
            {
                if (_filteredView == null && DailyWorks.Count > 0)
                {
                    _filteredView = CollectionViewSource.GetDefaultView(_dailyWorks);
                    _filteredView.Filter = FilterPredicate;
                }
                return _filteredView!;
            }
        }

        private bool FilterPredicate(object obj)
        {
            if (obj is not DailyWorkModel work) return false;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLowerInvariant();
                if (!work.Company.ToLowerInvariant().Contains(search) &&
                    !work.PiNumber.ToLowerInvariant().Contains(search) &&
                    !work.CustomerReference.ToLowerInvariant().Contains(search) &&
                    !work.Salesman.ToLowerInvariant().Contains(search) &&
                    !work.Notes.ToLowerInvariant().Contains(search))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(FilterStatus) && work.Status != FilterStatus) return false;
            if (!string.IsNullOrWhiteSpace(FilterProductionStatus) && work.ProductionStatus != FilterProductionStatus) return false;
            if (!string.IsNullOrWhiteSpace(FilterTypeOfWork) && work.TypeOfWork != FilterTypeOfWork) return false;
            if (!string.IsNullOrWhiteSpace(FilterSalesman) && work.Salesman != FilterSalesman) return false;
            if (!string.IsNullOrWhiteSpace(FilterCompany) && work.Company != FilterCompany) return false;
            if (!string.IsNullOrWhiteSpace(FilterColor) && work.Color != FilterColor) return false;
            if (!string.IsNullOrWhiteSpace(FilterPINumber) && work.PiNumber != FilterPINumber) return false;
            if (!string.IsNullOrWhiteSpace(FilterCustomerReference) && work.CustomerReference != FilterCustomerReference) return false;
            if (FilterStartDate.HasValue && work.Date < FilterStartDate) return false;
            if (FilterEndDate.HasValue && work.Date > FilterEndDate) return false;

            return true;
        }

        // Selection - PATCH 63: Manual property (source generator had issues)
        private DailyWorkModel? _selectedItem;
        public DailyWorkModel? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        // PATCH 20: Selection tracking with HashSet for O(1) lookups
        private HashSet<int> _selectedIds = new();
        private int _selectedCount;
        public int SelectedCount => _selectedCount;

        public void UpdateSelectedIds(IEnumerable<int> ids)
        {
            _selectedIds = new HashSet<int>(ids);
            _selectedCount = _selectedIds.Count;
            OnPropertyChanged(nameof(SelectedCount));
        }

        // PATCH 35: Add debounce timer for search
        private System.Timers.Timer? _searchDebounceTimer;

        // PATCH 112: Quick instant search toggle
        private bool _instantSearch = true;
        public bool InstantSearch
        {
            get => _instantSearch;
            set => SetProperty(ref _instantSearch, value);
        }

        // Note: SearchText needs custom setter for debounce, so keep manual
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    // PATCH 112: Check if instant search is enabled
                    if (_instantSearch)
                    {
                        // Instant search - no debounce
                        RefreshFilteredView();
                    }
                    else
                    {
                        // PATCH 112: Reuse timer to prevent memory leak
                        if (_searchDebounceTimer == null)
                        {
                            _searchDebounceTimer = new System.Timers.Timer(300);
                            _searchDebounceTimer.Elapsed += (s, e) =>
                            {
                                _searchDebounceTimer?.Stop();
                                Application.Current?.Dispatcher.BeginInvoke(() => RefreshFilteredView());
                            };
                        }
                        _searchDebounceTimer.Stop();
                        _searchDebounceTimer.Start();
                    }
                }
            }
        }

        private string _filterStatus = string.Empty;
        public string FilterStatus
        {
            get => _filterStatus;
            set { if (SetProperty(ref _filterStatus, value)) RefreshFilteredView(); }
        }

        private string _filterProductionStatus = string.Empty;
        public string FilterProductionStatus
        {
            get => _filterProductionStatus;
            set { if (SetProperty(ref _filterProductionStatus, value)) RefreshFilteredView(); }
        }

        private string _filterTypeOfWork = string.Empty;
        public string FilterTypeOfWork
        {
            get => _filterTypeOfWork;
            set { if (SetProperty(ref _filterTypeOfWork, value)) RefreshFilteredView(); }
        }

        private string _filterSalesman = string.Empty;
        public string FilterSalesman
        {
            get => _filterSalesman;
            set { if (SetProperty(ref _filterSalesman, value)) RefreshFilteredView(); }
        }

        private string _filterCompany = string.Empty;
        public string FilterCompany
        {
            get => _filterCompany;
            set { if (SetProperty(ref _filterCompany, value)) RefreshFilteredView(); }
        }

        private string _filterColor = string.Empty;
        public string FilterColor
        {
            get => _filterColor;
            set { if (SetProperty(ref _filterColor, value)) RefreshFilteredView(); }
        }

        private string _filterPINumber = string.Empty;
        public string FilterPINumber
        {
            get => _filterPINumber;
            set { if (SetProperty(ref _filterPINumber, value)) RefreshFilteredView(); }
        }

        private string _filterCustomerReference = string.Empty;
        public string FilterCustomerReference
        {
            get => _filterCustomerReference;
            set { if (SetProperty(ref _filterCustomerReference, value)) RefreshFilteredView(); }
        }

        private DateTime? _filterStartDate;
        public DateTime? FilterStartDate
        {
            get => _filterStartDate;
            set { if (SetProperty(ref _filterStartDate, value)) RefreshFilteredView(); }
        }

        private DateTime? _filterEndDate;
        public DateTime? FilterEndDate
        {
            get => _filterEndDate;
            set { if (SetProperty(ref _filterEndDate, value)) RefreshFilteredView(); }
        }

        // Editing state - MANUAL
        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        private DailyWorkModel? _editingWork;
        public DailyWorkModel? EditingWork
        {
            get => _editingWork;
            set => SetProperty(ref _editingWork, value);
        }

        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isDuplicateWarning;
        public bool IsDuplicateWarning
        {
            get => _isDuplicateWarning;
            set => SetProperty(ref _isDuplicateWarning, value);
        }

        private string _duplicateMessage = string.Empty;
        public string DuplicateMessage
        {
            get => _duplicateMessage;
            set => SetProperty(ref _duplicateMessage, value);
        }

        [RelayCommand]
        private async Task DeleteSelectedAsync(object? parameter)
        {
            // PATCH 55: Prevent command re-entry
            if (IsBusy) return;

            // Get selected IDs from the tracking list
            var selectedIds = _selectedIds?.ToList() ?? new List<int>();

            System.Diagnostics.Debug.WriteLine($"[DailyWorksVM] DeleteSelected called. Count: {selectedIds.Count}");

            // If empty, try to get from DataGrid parameter
            if (selectedIds.Count == 0 && parameter is DataGrid dg)
            {
                selectedIds.Clear();
                foreach (var item in dg.SelectedItems)
                {
                    if (item is DailyWorkModel work)
                    {
                        selectedIds.Add(work.Id);
                        System.Diagnostics.Debug.WriteLine($"[DailyWorksVM] Adding selected Id: {work.Id}");
                    }
                }
            }

            if (selectedIds.Count == 0)
            {
                StatusMessage = "No items selected! Please select items first.";
                MessageBox.Show("Please select items to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Delete {selectedIds.Count} selected records?",
                "Confirm Bulk Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // PATCH 01, 04: Use repository instead of direct DbHelper call
                foreach (var id in selectedIds)
                {
                    await _repository.DeleteAsync(id);
                    System.Diagnostics.Debug.WriteLine($"[DailyWorksVM] Deleted Id: {id}");
                }

                await LoadDataAsync();
                _selectedIds.Clear(); // PATCH 20: Clear HashSet
                SelectedItem = null;
                StatusMessage = $"Deleted {selectedIds.Count} records successfully!";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DailyWorksVM] DeleteSelected error: {ex.Message}");
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        // Busy state
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        // Options
        public ObservableCollection<string> CompanyOptions { get; } = new();
        public ObservableCollection<string> PINumberOptions { get; } = new();
        public ObservableCollection<string> CustomerReferenceOptions { get; } = new();
        public ObservableCollection<string> TypeOfWorkOptions { get; } = new();
        public ObservableCollection<string> StatusOptions { get; } = new();
        public ObservableCollection<string> ProductionStatusOptions { get; } = new();
        public ObservableCollection<string> SalesmanOptions { get; } = new();
        public ObservableCollection<string> ColorOptions { get; } = new();

        // Commands - Source generators auto-create commands from [RelayCommand]
        [RelayCommand]
        private async Task AddNewAsync()
        {
            EditingWork = DailyWorkModel.CreateNew();
            IsEditing = true;
            IsDuplicateWarning = false;
            DuplicateMessage = "";
        }

        [RelayCommand]
        private async Task EditAsync()
        {
            if (SelectedItem == null) return;
            EditingWork = SelectedItem.Clone();
            IsEditing = true;
            IsDuplicateWarning = false;
            DuplicateMessage = "";
        }

        [RelayCommand]
        private async Task DeleteAsync()
        {
            if (SelectedItem == null) return;
            var result = MessageBox.Show($"Delete {SelectedItem.Company}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                await _repository.DeleteAsync(SelectedItem.Id);
                DailyWorks.Remove(SelectedItem);
                RefreshFilteredView();
                SelectedItem = null;
                UpdateStatistics();
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (EditingWork == null) return;
            if (string.IsNullOrWhiteSpace(EditingWork.Company))
            {
                MessageBox.Show("Company required!", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var dbEntity = DbDailyWork.FromUiModel(EditingWork);
            if (EditingWork.Id == 0)
            {
                EditingWork.Id = DailyWorks.Count > 0 ? DailyWorks.Max(w => w.Id) + 1 : 1;
                await _repository.InsertAsync(dbEntity);
                DailyWorks.Add(EditingWork);
            }
            else
            {
                EditingWork.UpdateDate = DateTime.Today;
                await _repository.UpdateAsync(dbEntity);
                var existing = DailyWorks.FirstOrDefault(w => w.Id == EditingWork.Id);
                if (existing != null)
                {
                    var index = DailyWorks.IndexOf(existing);
                    DailyWorks[index] = EditingWork;
                }
            }
            IsEditing = false;
            EditingWork = null;
            RefreshFilteredView();
            UpdateStatistics();
        }

        [RelayCommand]
        private void Cancel()
        {
            IsEditing = false;
            EditingWork = null;
            IsDuplicateWarning = false;
            DuplicateMessage = "";
        }

        [RelayCommand]
        private async Task RefreshAsync() => await LoadDataAsync();

        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = string.Empty;
            FilterStatus = string.Empty;
            FilterProductionStatus = string.Empty;
            FilterTypeOfWork = string.Empty;
            FilterSalesman = string.Empty;
            FilterCompany = string.Empty;
            FilterColor = string.Empty;
            FilterPINumber = string.Empty;
            FilterCustomerReference = string.Empty;
            FilterStartDate = null;
            FilterEndDate = null;
            RefreshFilteredView();
        }

        // PATCH 41-44: Quick filter presets
        [RelayCommand]
        private void QuickFilterThisWeek()
        {
            FilterStartDate = DateTime.Today.AddDays(-7);
            FilterEndDate = DateTime.Today;
            RefreshFilteredView();
        }

        [RelayCommand]
        private void QuickFilterThisMonth()
        {
            FilterStartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            FilterEndDate = DateTime.Today;
            RefreshFilteredView();
        }

        [RelayCommand]
        private void QuickFilterAllTime()
        {
            FilterStartDate = null;
            FilterEndDate = null;
            RefreshFilteredView();
        }

        // PATCH 91: Export to CSV - Export FILTERED records
        [RelayCommand]
        private async Task ExportToCsvAsync()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    DefaultExt = ".csv",
                    FileName = $"DailyWorks_Export_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    IsBusy = true;
                    StatusMessage = "Exporting...";

                    var lines = new List<string>
            {
                "Date,Company,PI Number,Color,Cust Ref,Type of Work,Production,QTY,SQM,Status,Salesman,Notes"
            };

                    // PATCH 91 FIX: Export FILTERED records, not all records
                    var filteredList = FilteredDataView?.Cast<DailyWorkModel>().ToList() ?? DailyWorks.ToList();

                    foreach (var work in filteredList)
                    {
                        var line = $"\"{work.Date:dd-MM-yyyy}\",\"{work.Company}\",\"{work.PiNumber}\",\"{work.Color}\",\"{work.CustomerReference}\",\"{work.TypeOfWork}\",\"{work.ProductionStatus}\",{work.Qty},{work.Sqm:N2},\"{work.Status}\",\"{work.Salesman}\",\"{work.Notes}\"";
                        lines.Add(line);
                    }

                    await System.IO.File.WriteAllLinesAsync(dialog.FileName, lines);
                    // PATCH 111: Add export timestamp
                    StatusMessage = $"Exported {filteredList.Count} records at {DateTime.Now:HH:mm:ss}!";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Export failed: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // PATCH 92: Print
        [RelayCommand]
        private void Print()
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Will be handled in code-behind with DataGrid reference
                    StatusMessage = "Printing...";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Print failed: " + ex.Message;
            }
        }

        // PATCH 95: Auto-refresh timer
        private System.Timers.Timer? _autoRefreshTimer;

        private bool _autoRefreshEnabled;
        public bool AutoRefreshEnabled
        {
            get => _autoRefreshEnabled;
            set
            {
                if (SetProperty(ref _autoRefreshEnabled, value))
                {
                    UpdateAutoRefresh();
                }
            }
        }

        private int _autoRefreshSeconds = 60;
        public int AutoRefreshSeconds
        {
            get => _autoRefreshSeconds;
            set
            {
                if (SetProperty(ref _autoRefreshSeconds, value))
                {
                    UpdateAutoRefresh();
                }
            }
        }

        private void UpdateAutoRefresh()
        {
            _autoRefreshTimer?.Stop();
            _autoRefreshTimer?.Dispose();
            _autoRefreshTimer = null;

            if (AutoRefreshEnabled && AutoRefreshSeconds > 0)
            {
                _autoRefreshTimer = new System.Timers.Timer(AutoRefreshSeconds * 1000);
                _autoRefreshTimer.Elapsed += async (s, e) =>
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await LoadDataAsync();
                    });
                };
                _autoRefreshTimer.Start();
            }
        }

        // Invoice handling
        public event Action? RequestNavigateToInvoice;

        public void OnProformaInvoiceSaved(Models.ProformaInvoiceModel invoice)
        {
            _ = LoadDataAsync();
        }

        // PATCH 17: Cached statistics
        private int _totalRecords;
        public int TotalRecords
        {
            get => _totalRecords;
            private set => SetProperty(ref _totalRecords, value);
        }

        private double _totalSQM;
        public double TotalSQM
        {
            get => _totalSQM;
            private set => SetProperty(ref _totalSQM, value);
        }

        private int _totalQty;
        public int TotalQty
        {
            get => _totalQty;
            private set => SetProperty(ref _totalQty, value);
        }

        private int _filteredRecords;
        public int FilteredRecords
        {
            get => _filteredRecords;
            private set => SetProperty(ref _filteredRecords, value);
        }

        private double _filteredSQM;
        public double FilteredSQM
        {
            get => _filteredSQM;
            private set => SetProperty(ref _filteredSQM, value);
        }

        private int _filteredQty;
        public int FilteredQty
        {
            get => _filteredQty;
            private set => SetProperty(ref _filteredQty, value);
        }

        public bool HasRecords => FilteredRecords > 0;

        private void RefreshFilteredView()
        {
            _filteredView?.Refresh();
            UpdateStatistics();
        }

        // PATCH 17, 70: Update cached statistics including unique counts
        private void UpdateStatistics()
        {
            TotalRecords = DailyWorks.Count;
            TotalSQM = DailyWorks.Sum(w => w.Sqm);
            TotalQty = DailyWorks.Sum(w => w.Qty);

            // PATCH 70: Add unique counts for stats display
            TotalCompanies = DailyWorks.Select(w => w.Company).Distinct().Count();
            TotalPINumbers = DailyWorks.Select(w => w.PiNumber).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().Count();

            FilteredRecords = FilteredDataView?.Cast<object>().Count() ?? 0;
            FilteredSQM = FilteredDataView?.Cast<DailyWorkModel>().Sum(w => w.Sqm) ?? 0;
            FilteredQty = FilteredDataView?.Cast<DailyWorkModel>().Sum(w => w.Qty) ?? 0;

            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasRecords));
        }

        // Add these fields and properties
        private int _totalCompanies;
        public int TotalCompanies
        {
            get => _totalCompanies;
            private set => SetProperty(ref _totalCompanies, value);
        }

        private int _totalPINumbers;
        public int TotalPINumbers
        {
            get => _totalPINumbers;
            private set => SetProperty(ref _totalPINumbers, value);
        }

        // PATCH 107: Bulk production status
        private string _bulkProductionStatus = string.Empty;
        public string BulkProductionStatus
        {
            get => _bulkProductionStatus;
            set => SetProperty(ref _bulkProductionStatus, value);
        }

        [RelayCommand]
        private async Task BulkUpdateProductionStatusAsync()
        {
            if (string.IsNullOrWhiteSpace(BulkProductionStatus)) return;

            var selectedIds = _selectedIds?.ToList() ?? new List<int>();
            if (selectedIds.Count == 0)
            {
                // Try from grid - PATCH 107
                var grid = Application.Current.MainWindow?.FindName("MainDataGrid") as DataGrid;
                if (grid != null)
                {
                    foreach (var item in grid.SelectedItems)
                    {
                        if (item is DailyWorkModel work)
                            selectedIds.Add(work.Id);
                    }
                }
            }

            if (selectedIds.Count == 0)
            {
                StatusMessage = "No items selected!";
                return;
            }

            var result = MessageBox.Show(
                $"Update Production Status to '{BulkProductionStatus}' for {selectedIds.Count} records?",
                "Confirm Bulk Update", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                foreach (var id in selectedIds)
                {
                    var work = DailyWorks.FirstOrDefault(w => w.Id == id);
                    if (work != null)
                    {
                        work.ProductionStatus = BulkProductionStatus;
                        work.UpdateDate = DateTime.Today;
                        await _repository.UpdateAsync(DbDailyWork.FromUiModel(work));
                    }
                }

                RefreshFilteredView();
                UpdateStatistics();
                _selectedIds.Clear();
                OnPropertyChanged(nameof(SelectedCount));
                StatusMessage = $"Updated {selectedIds.Count} records to '{BulkProductionStatus}'!";
            }
            catch (Exception ex)
            {
                StatusMessage = "Update failed: " + ex.Message;
            }
        }

        // PATCH 12: Prevent UI thread blocking with parallel loading
        public async Task LoadDataAsync()
        {
            if (IsBusy) return; // PATCH 104: Prevent concurrent LoadDataAsync

            IsBusy = true;
            try
            {
                // PATCH 13: Use Task.WhenAll for parallel loading
                var loadTask = Task.Run(async () =>
                {
                    var dbItems = await _repository.GetAllAsync();
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        DailyWorks.Clear();
                        foreach (var item in dbItems)
                        {
                            var piNum = item.PINumber?.Trim() ?? "";
                            var custRef = item.CustomerReference?.Trim() ?? "";
                            if (!string.IsNullOrWhiteSpace(piNum) || !string.IsNullOrWhiteSpace(custRef))
                            {
                                DailyWorks.Add(item.ToUiModel());
                            }
                        }
                        _filteredView = null;
                        OnPropertyChanged(nameof(FilteredDataView));
                        LoadFilterOptions();
                        UpdateStatistics();
                    });
                });

                await loadTask;
                StatusMessage = "Ready";
            }
            catch (Exception ex)
            {
                StatusMessage = "Load failed: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void LoadFilterOptions()
        {
            CompanyOptions.Clear();
            PINumberOptions.Clear();
            CustomerReferenceOptions.Clear();
            TypeOfWorkOptions.Clear();
            StatusOptions.Clear();
            ProductionStatusOptions.Clear();
            SalesmanOptions.Clear();
            ColorOptions.Clear();

            foreach (var company in DailyWorks.Select(w => w.Company).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().OrderBy(c => c))
                CompanyOptions.Add(company);
            foreach (var pi in DailyWorks.Select(w => w.PiNumber).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().OrderBy(p => p))
                PINumberOptions.Add(pi);
            foreach (var custRef in DailyWorks.Select(w => w.CustomerReference).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().OrderBy(c => c))
                CustomerReferenceOptions.Add(custRef);
            foreach (var type in DailyWorks.Select(w => w.TypeOfWork).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().OrderBy(t => t))
                TypeOfWorkOptions.Add(type);
            foreach (var status in DailyWorks.Select(w => w.Status).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s))
                StatusOptions.Add(status);
            foreach (var prodStatus in DailyWorks.Select(w => w.ProductionStatus).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().OrderBy(p => p))
                ProductionStatusOptions.Add(prodStatus);
            foreach (var salesman in DailyWorks.Select(w => w.Salesman).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s))
                SalesmanOptions.Add(salesman);
            foreach (var color in DailyWorks.Select(w => w.Color).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().OrderBy(c => c))
                ColorOptions.Add(color);
        }

        // Constructor
        // PATCH 56-60: Proper disposal
        public DailyWorksViewModel()
        {
            _isBusy = false;
            _repository = new DailyWorkRepository();
            _ = LoadDataAsync();
        }

        // PATCH 54: Implement IDisposable for proper cleanup
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _searchDebounceTimer?.Stop();
                _searchDebounceTimer?.Dispose();
                _searchDebounceTimer = null;

                // PATCH 95: Auto-refresh timer cleanup
                _autoRefreshTimer?.Stop();
                _autoRefreshTimer?.Dispose();
                _autoRefreshTimer = null;
            }
        }
    }
}