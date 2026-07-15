using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ProGlassAutomation.Models;
using Newtonsoft.Json;

namespace ProGlassAutomation.ViewModels
{
    public class ProformaInvoiceListViewModel : INotifyPropertyChanged
    {
        private readonly string _defaultFolderPath;
        private System.Threading.Timer? _searchTimer;
        private int _bulkUpdateCount;
        private bool _suppressStatsUpdate;
        private bool _isLoadingFromFolder;

        public ObservableCollection<string> EditableStatusOptions { get; } = new ObservableCollection<string>
        {
            "Draft", "Sent", "Confirmed", "Hold", "Revised", "Completed", "Cancelled"
        };

        public ProformaInvoiceListViewModel()
        {
            _defaultFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            AllInvoices = new ObservableCollection<ProformaInvoiceModel>();
            FilteredInvoices = new ObservableCollection<ProformaInvoiceModel>();

            StatusOptions = new ObservableCollection<StatusOption>
            {
                new StatusOption { Label = "All", Value = "" },
                new StatusOption { Label = "Draft", Value = "Draft" },
                new StatusOption { Label = "Sent", Value = "Sent" },
                new StatusOption { Label = "Confirmed", Value = "Confirmed" },
                new StatusOption { Label = "Hold", Value = "Hold" },
                new StatusOption { Label = "Revised", Value = "Revised" },
                new StatusOption { Label = "Completed", Value = "Completed" },
                new StatusOption { Label = "Cancelled", Value = "Cancelled" }
            };

            SalesmanOptions = new ObservableCollection<SalesmanOption>();
            CustomerOptions = new ObservableCollection<CustomerOption>();
            CustRefOptions = new ObservableCollection<CustRefOption>();

            InitializeCommands();
            // ✅ Don't load in constructor - let view's Loaded event trigger LoadData()
        }

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        private bool _hasNoInvoices = true;
        public bool HasNoInvoices { get => _hasNoInvoices; set => SetProperty(ref _hasNoInvoices, value); }

        public ObservableCollection<ProformaInvoiceModel> AllInvoices { get; }
        public ObservableCollection<ProformaInvoiceModel> FilteredInvoices { get; }
        public ObservableCollection<StatusOption> StatusOptions { get; }
        public ObservableCollection<SalesmanOption> SalesmanOptions { get; }
        public ObservableCollection<CustomerOption> CustomerOptions { get; }
        public ObservableCollection<CustRefOption> CustRefOptions { get; }

        private ProformaInvoiceModel? _selectedInvoice;
        public ProformaInvoiceModel? SelectedInvoice { get => _selectedInvoice; set => SetProperty(ref _selectedInvoice, value); }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _searchTimer?.Dispose();
                    _searchTimer = new System.Threading.Timer(
                        _ => Application.Current.Dispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Background,
                            (Action)(() => ApplyFilters())),
                        null, 300, Timeout.Infinite);
                }
            }
        }

        private string? _selectedStatus;
        public string? SelectedStatus { get => _selectedStatus; set { if (SetProperty(ref _selectedStatus, value)) ApplyFilters(); } }
        private string? _selectedSalesman;
        public string? SelectedSalesman { get => _selectedSalesman; set { if (SetProperty(ref _selectedSalesman, value)) ApplyFilters(); } }
        private string? _selectedCustomer;
        public string? SelectedCustomer { get => _selectedCustomer; set { if (SetProperty(ref _selectedCustomer, value)) ApplyFilters(); } }
        private string? _selectedCustRef;
        public string? SelectedCustRef { get => _selectedCustRef; set { if (SetProperty(ref _selectedCustRef, value)) ApplyFilters(); } }

        // ==================== DATE RANGE (With Validation) ====================
        private DateTime? _dateFrom;
        public DateTime? DateFrom
        {
            get => _dateFrom;
            set
            {
                if (SetProperty(ref _dateFrom, value))
                {
                    ValidateDateRange();
                    ApplyFilters();
                }
            }
        }

        private DateTime? _dateTo;
        public DateTime? DateTo
        {
            get => _dateTo;
            set
            {
                if (SetProperty(ref _dateTo, value))
                {
                    ValidateDateRange();
                    ApplyFilters();
                }
            }
        }

        private bool _hasDateRangeError;
        public bool HasDateRangeError
        {
            get => _hasDateRangeError;
            set
            {
                if (SetProperty(ref _hasDateRangeError, value))
                    OnPropertyChanged(nameof(DateRangeErrorMessage));
            }
        }

        public string DateRangeErrorMessage =>
            _hasDateRangeError ? "⚠ 'From' date must be before 'To' date" : "";

        private void ValidateDateRange()
        {
            HasDateRangeError = _dateFrom.HasValue && _dateTo.HasValue && _dateFrom > _dateTo;
        }

        public bool IsDateFilterActive => _dateFrom.HasValue || _dateTo.HasValue;

        // ==================== FILTER CHECKBOXES ====================
        private bool _filterDraft = true;
        public bool FilterDraft { get => _filterDraft; set { if (SetProperty(ref _filterDraft, value)) ApplyFilters(); } }
        private bool _filterSent = true;
        public bool FilterSent { get => _filterSent; set { if (SetProperty(ref _filterSent, value)) ApplyFilters(); } }
        private bool _filterConfirmed = true;
        public bool FilterConfirmed { get => _filterConfirmed; set { if (SetProperty(ref _filterConfirmed, value)) ApplyFilters(); } }
        private bool _filterHold = true;
        public bool FilterHold { get => _filterHold; set { if (SetProperty(ref _filterHold, value)) ApplyFilters(); } }
        private bool _filterRevised = true;
        public bool FilterRevised { get => _filterRevised; set { if (SetProperty(ref _filterRevised, value)) ApplyFilters(); } }
        private bool _filterCompleted = true;
        public bool FilterCompleted { get => _filterCompleted; set { if (SetProperty(ref _filterCompleted, value)) ApplyFilters(); } }
        private bool _filterCancelled = false;
        public bool FilterCancelled { get => _filterCancelled; set { if (SetProperty(ref _filterCancelled, value)) ApplyFilters(); } }
        private bool _filterConvertedToJO = false;
        public bool FilterConvertedToJO { get => _filterConvertedToJO; set { if (SetProperty(ref _filterConvertedToJO, value)) ApplyFilters(); } }

        private int _totalInvoiceCount;
        public int TotalInvoiceCount { get => _totalInvoiceCount; set => SetProperty(ref _totalInvoiceCount, value); }
        private int _convertedToJOCount;
        public int ConvertedToJOCount { get => _convertedToJOCount; set => SetProperty(ref _convertedToJOCount, value); }
        private int _draftCount;
        public int DraftCount { get => _draftCount; set => SetProperty(ref _draftCount, value); }
        private int _sentCount;
        public int SentCount { get => _sentCount; set => SetProperty(ref _sentCount, value); }
        private int _confirmedCount;
        public int ConfirmedCount { get => _confirmedCount; set => SetProperty(ref _confirmedCount, value); }
        private int _holdCount;
        public int HoldCount { get => _holdCount; set => SetProperty(ref _holdCount, value); }
        private int _completedCount;
        public int CompletedCount { get => _completedCount; set => SetProperty(ref _completedCount, value); }

        public ICommand? NewInvoiceCommand { get; private set; }
        public ICommand? EditInvoiceCommand { get; private set; }
        public ICommand? ViewInvoiceCommand { get; private set; }
        public ICommand? DeleteInvoiceCommand { get; private set; }
        public ICommand? CreateJobOrderCommand { get; private set; }
        public ICommand? OpenFolderCommand { get; private set; }
        public ICommand? ExportAllCommand { get; private set; }
        public ICommand? RefreshCommand { get; private set; }
        public ICommand? ClearFiltersCommand { get; private set; }
        public ICommand? QuickFilterThisWeekCommand { get; private set; }
        public ICommand? QuickFilterThisMonthCommand { get; private set; }
        public ICommand? QuickFilterTodayCommand { get; private set; }
        public ICommand? QuickFilterThisYearCommand { get; private set; }
        public ICommand? QuickFilterAllTimeCommand { get; private set; }
        public ICommand? ResetDateRangeCommand { get; private set; }

        private void InitializeCommands()
        {
            NewInvoiceCommand = new RelayCommand(_ => ExecuteNewInvoice(_));
            EditInvoiceCommand = new RelayCommand<ProformaInvoiceModel>(inv => ExecuteEditInvoice(inv));
            ViewInvoiceCommand = new RelayCommand<ProformaInvoiceModel>(inv => ExecuteViewInvoice(inv));
            DeleteInvoiceCommand = new RelayCommand<ProformaInvoiceModel>(inv => ExecuteDeleteInvoice(inv));
            CreateJobOrderCommand = new RelayCommand<ProformaInvoiceModel>(inv => ExecuteCreateJobOrder(inv));
            OpenFolderCommand = new RelayCommand(_ => ExecuteOpenFolder(_));
            ExportAllCommand = new RelayCommand(_ => ExecuteExportAll(_));
            RefreshCommand = new RelayCommand(_ => ExecuteRefresh(_));
            ClearFiltersCommand = new RelayCommand(_ => ExecuteClearFilters(_));

            QuickFilterThisWeekCommand = new RelayCommand(_ => { DateFrom = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek); DateTo = DateTime.Today; });
            QuickFilterThisMonthCommand = new RelayCommand(_ => { DateFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); DateTo = DateTime.Today; });
            QuickFilterTodayCommand = new RelayCommand(_ => { DateFrom = DateTime.Today; DateTo = DateTime.Today; });
            QuickFilterThisYearCommand = new RelayCommand(_ => { DateFrom = new DateTime(DateTime.Today.Year, 1, 1); DateTo = DateTime.Today; });
            QuickFilterAllTimeCommand = new RelayCommand(_ => { DateFrom = null; DateTo = null; });
            ResetDateRangeCommand = new RelayCommand(_ => { DateFrom = null; DateTo = null; });
        }

        // ==================== PUBLIC LOAD DATA (Called from view) ====================
        public void LoadData()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[PI] LoadData() called. Folder: {_defaultFolderPath}");
                IsLoading = true;
                LoadInvoicesFromFolderInternal(_defaultFolderPath);
                ApplyFilters();
                CalculateStatistics();
                System.Diagnostics.Debug.WriteLine($"[PI] LoadData() complete. AllInvoices: {AllInvoices.Count}, Filtered: {FilteredInvoices.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PI] LoadData error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ==================== EXECUTE METHODS ====================
        private void ExecuteNewInvoice(object? parameter)
        {
            try
            {
                SharedViewModels.ProformaInvoiceVM.CreateNewInvoice();
                var editorView = new ProGlassAutomation.Views.ProformaInvoice.ProformaInvoiceView
                { DataContext = SharedViewModels.ProformaInvoiceVM };
                (Application.Current.MainWindow as MainWindow)?.SetContent(editorView);
            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ExecuteEditInvoice(ProformaInvoiceModel? invoice)
        {
            if (invoice == null) return;
            try
            {
                SharedViewModels.ProformaInvoiceVM.LoadFromExistingInvoice(invoice);
                var editorView = new ProGlassAutomation.Views.ProformaInvoice.ProformaInvoiceView
                { DataContext = SharedViewModels.ProformaInvoiceVM };
                (Application.Current.MainWindow as MainWindow)?.SetContent(editorView);
            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ExecuteViewInvoice(ProformaInvoiceModel? invoice) => ExecuteEditInvoice(invoice);

        private void ExecuteDeleteInvoice(ProformaInvoiceModel? invoice)
        {
            if (invoice == null) return;
            var result = MessageBox.Show($"Delete invoice {invoice.InvoiceNo}?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
            try
            {
                var filePath = GetInvoiceFilePath(invoice.InvoiceNo);
                if (File.Exists(filePath))
                {
                    var backupFolder = Path.Combine(_defaultFolderPath, "Deleted");
                    if (!Directory.Exists(backupFolder)) Directory.CreateDirectory(backupFolder);
                    File.Move(filePath, Path.Combine(backupFolder, $"{invoice.InvoiceNo}_{DateTime.Now:yyyyMMddHHmmss}.json"));
                }
                AllInvoices.Remove(invoice);
                ApplyFilters();
            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ExecuteCreateJobOrder(ProformaInvoiceModel? invoice)
        {
            if (invoice == null) return;
            var result = MessageBox.Show(
                $"Create Job Order from {invoice.InvoiceNo}?\nCustomer: {invoice.CustomerName}\nAmount: {invoice.NetTotal:N0}",
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
            try
            {
                var jobOrderVM = SharedViewModels.JobOrderVM;
                jobOrderVM.ClearForNewJobOrder();
                jobOrderVM.LoadFromProformaInvoice(invoice);
                var jobOrderView = new ProGlassAutomation.Views.JobOrder.JobOrderView
                { DataContext = jobOrderVM };
                (Application.Current.MainWindow as MainWindow)?.SetContent(jobOrderView);
            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ExecuteOpenFolder(object? parameter)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog { InitialDirectory = _defaultFolderPath };
            if (dialog.ShowDialog() == true) LoadInvoicesFromFolder(dialog.FolderName);
        }

        private void ExecuteExportAll(object? parameter)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            { Filter = "Excel|*.xlsx", FileName = $"Invoices_{DateTime.Now:yyyyMMdd}" };
            if (dialog.ShowDialog() == true)
                MessageBox.Show($"Exported {FilteredInvoices.Count} invoices to {dialog.FileName}");
        }

        private void ExecuteRefresh(object? parameter)
        {
            try
            {
                IsLoading = true;

                var savedSearch = _searchText;
                var savedStatus = _selectedStatus;
                var savedSalesman = _selectedSalesman;
                var savedCustomer = _selectedCustomer;
                var savedCustRef = _selectedCustRef;
                var savedDateFrom = _dateFrom;
                var savedDateTo = _dateTo;
                var savedFilterDraft = _filterDraft;
                var savedFilterSent = _filterSent;
                var savedFilterConfirmed = _filterConfirmed;
                var savedFilterHold = _filterHold;
                var savedFilterRevised = _filterRevised;
                var savedFilterCompleted = _filterCompleted;
                var savedFilterCancelled = _filterCancelled;
                var savedFilterConvertedToJO = _filterConvertedToJO;
                var savedSelectedInvoice = SelectedInvoice;

                _isLoadingFromFolder = true;
                _suppressStatsUpdate = true;
                _bulkUpdateCount++;

                try
                {
                    LoadInvoicesFromFolderInternal(_defaultFolderPath);

                    _searchText = savedSearch;
                    _selectedStatus = savedStatus;
                    _selectedSalesman = savedSalesman;
                    _selectedCustomer = savedCustomer;
                    _selectedCustRef = savedCustRef;
                    _dateFrom = savedDateFrom;
                    _dateTo = savedDateTo;
                    _filterDraft = savedFilterDraft;
                    _filterSent = savedFilterSent;
                    _filterConfirmed = savedFilterConfirmed;
                    _filterHold = savedFilterHold;
                    _filterRevised = savedFilterRevised;
                    _filterCompleted = savedFilterCompleted;
                    _filterCancelled = savedFilterCancelled;
                    _filterConvertedToJO = savedFilterConvertedToJO;

                    OnPropertyChanged(nameof(SearchText));
                    OnPropertyChanged(nameof(SelectedStatus));
                    OnPropertyChanged(nameof(SelectedSalesman));
                    OnPropertyChanged(nameof(SelectedCustomer));
                    OnPropertyChanged(nameof(SelectedCustRef));
                    OnPropertyChanged(nameof(DateFrom));
                    OnPropertyChanged(nameof(DateTo));
                    OnPropertyChanged(nameof(FilterDraft));
                    OnPropertyChanged(nameof(FilterSent));
                    OnPropertyChanged(nameof(FilterConfirmed));
                    OnPropertyChanged(nameof(FilterHold));
                    OnPropertyChanged(nameof(FilterRevised));
                    OnPropertyChanged(nameof(FilterCompleted));
                    OnPropertyChanged(nameof(FilterCancelled));
                    OnPropertyChanged(nameof(FilterConvertedToJO));
                    OnPropertyChanged(nameof(IsDateFilterActive));
                    ValidateDateRange();

                    ApplyFilters();

                    if (savedSelectedInvoice != null)
                    {
                        var match = FilteredInvoices.FirstOrDefault(i => i.InvoiceNo == savedSelectedInvoice.InvoiceNo);
                        if (match != null) SelectedInvoice = match;
                    }
                }
                finally
                {
                    _bulkUpdateCount--;
                    _suppressStatsUpdate = false;
                    _isLoadingFromFolder = false;
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteClearFilters(object? parameter)
        {
            _suppressStatsUpdate = true;
            try
            {
                _searchText = "";
                OnPropertyChanged(nameof(SearchText));
                _selectedStatus = _selectedSalesman = _selectedCustomer = _selectedCustRef = null;
                OnPropertyChanged(nameof(SelectedStatus));
                OnPropertyChanged(nameof(SelectedSalesman));
                OnPropertyChanged(nameof(SelectedCustomer));
                OnPropertyChanged(nameof(SelectedCustRef));
                _dateFrom = _dateTo = null;
                ValidateDateRange();
                OnPropertyChanged(nameof(DateFrom));
                OnPropertyChanged(nameof(DateTo));
                OnPropertyChanged(nameof(IsDateFilterActive));
                _filterDraft = _filterSent = _filterConfirmed = _filterHold = _filterRevised = _filterCompleted = true;
                _filterCancelled = _filterConvertedToJO = false;
                OnPropertyChanged(nameof(FilterDraft));
                OnPropertyChanged(nameof(FilterSent));
                OnPropertyChanged(nameof(FilterConfirmed));
                OnPropertyChanged(nameof(FilterHold));
                OnPropertyChanged(nameof(FilterRevised));
                OnPropertyChanged(nameof(FilterCompleted));
                OnPropertyChanged(nameof(FilterCancelled));
                OnPropertyChanged(nameof(FilterConvertedToJO));
            }
            finally
            {
                _suppressStatsUpdate = false;
            }
            ApplyFilters();
        }

        public void ApplyFilters()
        {
            if (_bulkUpdateCount > 0) return;
            if (HasDateRangeError) return;

            var query = AllInvoices.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var s = SearchText.ToLower();
                query = query.Where(i =>
                    (i.InvoiceNo?.ToLower().Contains(s) ?? false) ||
                    (i.CustomerName?.ToLower().Contains(s) ?? false) ||
                    (i.ProjectName?.ToLower().Contains(s) ?? false) ||
                    (i.CustomerReference?.ToLower().Contains(s) ?? false));
            }

            if (!string.IsNullOrWhiteSpace(SelectedStatus))
                query = query.Where(i => i.Status == SelectedStatus);
            if (!string.IsNullOrWhiteSpace(SelectedSalesman))
                query = query.Where(i => i.Salesman == SelectedSalesman);
            if (!string.IsNullOrWhiteSpace(SelectedCustomer))
                query = query.Where(i => i.CustomerName == SelectedCustomer);
            if (!string.IsNullOrWhiteSpace(SelectedCustRef))
                query = query.Where(i => i.CustomerReference == SelectedCustRef);
            if (DateFrom.HasValue)
                query = query.Where(i => i.InvoiceDate >= DateFrom.Value);
            if (DateTo.HasValue)
                query = query.Where(i => i.InvoiceDate <= DateTo.Value.AddDays(1));

            var allowedStatuses = new HashSet<string>();
            if (FilterDraft) allowedStatuses.Add("Draft");
            if (FilterSent) allowedStatuses.Add("Sent");
            if (FilterConfirmed) allowedStatuses.Add("Confirmed");
            if (FilterHold) allowedStatuses.Add("Hold");
            if (FilterRevised) allowedStatuses.Add("Revised");
            if (FilterCompleted) allowedStatuses.Add("Completed");
            if (FilterCancelled) allowedStatuses.Add("Cancelled");

            query = query.Where(i => allowedStatuses.Contains(i.Status ?? ""));

            if (FilterConvertedToJO)
                query = query.Where(i => i.IsConvertedToJobOrder);

            var filteredList = query
                .OrderByDescending(i => i.InvoiceDate)
                .ThenByDescending(i => i.InvoiceNo)
                .ToList();

            FilteredInvoices.Clear();
            foreach (var inv in filteredList)
                FilteredInvoices.Add(inv);

            if (!_suppressStatsUpdate)
                CalculateStatistics();
        }

        // ==================== LOAD FROM FOLDER (ROBUST VERSION) ====================
        private void LoadInvoicesFromFolder(string folderPath)
        {
            LoadInvoicesFromFolderInternal(folderPath);
        }

        private void LoadInvoicesFromFolderInternal(string folderPath)
        {
            try
            {
                _isLoadingFromFolder = true;

                AllInvoices.Clear();
                SalesmanOptions.Clear();
                CustomerOptions.Clear();
                CustRefOptions.Clear();

                if (!Directory.Exists(folderPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[PI] Data folder doesn't exist, creating: {folderPath}");
                    Directory.CreateDirectory(folderPath);
                    return;
                }

                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };

                var salesmen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var customers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var custRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // ✅ Deduplication using file modification time
                var invoiceDict = new Dictionary<string, ProformaInvoiceModel>(StringComparer.OrdinalIgnoreCase);
                var fileTimes = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

                var files = Directory.GetFiles(folderPath, "*.json");
                System.Diagnostics.Debug.WriteLine($"[PI] Found {files.Length} JSON files");

                int successCount = 0;
                int failCount = 0;

                Parallel.ForEach(files, file =>
                {
                    try
                    {
                        var json = File.ReadAllText(file);

                        if (string.IsNullOrWhiteSpace(json))
                        {
                            System.Diagnostics.Debug.WriteLine($"[PI] Empty file: {file}");
                            return;
                        }

                        var invoice = JsonConvert.DeserializeObject<ProformaInvoiceModel>(json, settings);

                        // ✅ FIX: Validate the deserialized invoice
                        if (invoice == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[PI] Failed to deserialize: {file}");
                            Interlocked.Increment(ref failCount);
                            return;
                        }

                        if (string.IsNullOrWhiteSpace(invoice.InvoiceNo))
                        {
                            System.Diagnostics.Debug.WriteLine($"[PI] Invoice has no InvoiceNo: {file}");
                            Interlocked.Increment(ref failCount);
                            return;
                        }

                        var fileTime = File.GetLastWriteTime(file);
                        lock (invoiceDict)
                        {
                            if (!invoiceDict.ContainsKey(invoice.InvoiceNo))
                            {
                                invoiceDict[invoice.InvoiceNo] = invoice;
                                fileTimes[invoice.InvoiceNo] = fileTime;
                            }
                            else
                            {
                                if (fileTime > fileTimes[invoice.InvoiceNo])
                                {
                                    invoiceDict[invoice.InvoiceNo] = invoice;
                                    fileTimes[invoice.InvoiceNo] = fileTime;
                                }
                            }
                        }

                        Interlocked.Increment(ref successCount);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PI] Error reading {file}: {ex.Message}");
                        Interlocked.Increment(ref failCount);
                    }
                });

                System.Diagnostics.Debug.WriteLine($"[PI] Deserialization: {successCount} success, {failCount} failed");

                // ✅ Stable ordering
                var uniqueInvoices = invoiceDict.Values
                    .OrderByDescending(i => i.InvoiceDate)
                    .ThenByDescending(i => i.InvoiceNo)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"[PI] Adding {uniqueInvoices.Count} unique invoices to AllInvoices");

                foreach (var invoice in uniqueInvoices)
                {
                    // ✅ Safety check - ensure invoice is not null
                    if (invoice == null) continue;

                    AllInvoices.Add(invoice);

                    // ✅ Safe property access
                    if (!string.IsNullOrWhiteSpace(invoice.Salesman))
                        salesmen.Add(invoice.Salesman);
                    if (!string.IsNullOrWhiteSpace(invoice.CustomerName))
                        customers.Add(invoice.CustomerName);
                    if (!string.IsNullOrWhiteSpace(invoice.CustomerReference))
                        custRefs.Add(invoice.CustomerReference);
                }

                foreach (var s in salesmen.OrderBy(x => x))
                    SalesmanOptions.Add(new SalesmanOption { Name = s });
                foreach (var c in customers.OrderBy(x => x))
                    CustomerOptions.Add(new CustomerOption { Name = c });
                foreach (var r in custRefs.OrderBy(x => x))
                    CustRefOptions.Add(new CustRefOption { Reference = r });

                System.Diagnostics.Debug.WriteLine($"[PI] Load complete. AllInvoices: {AllInvoices.Count}, FilteredInvoices: {FilteredInvoices.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PI] LoadInvoicesFromFolderInternal error: {ex.Message}");
            }
            finally
            {
                _isLoadingFromFolder = false;
            }
        }

        public void UpdateStatus(ProformaInvoiceModel invoice, string newStatus)
        {
            if (invoice == null || string.IsNullOrWhiteSpace(newStatus)) return;
            if (invoice.Status == newStatus) return;
            try
            {
                invoice.Status = newStatus;
                var existing = AllInvoices.FirstOrDefault(i => i.InvoiceNo == invoice.InvoiceNo);
                if (existing != null) existing.Status = newStatus;
                SaveInvoice(invoice);
                if (!_suppressStatsUpdate)
                    CalculateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SaveInvoice(ProformaInvoiceModel invoice)
        {
            try
            {
                if (invoice == null || string.IsNullOrWhiteSpace(invoice.InvoiceNo))
                {
                    System.Diagnostics.Debug.WriteLine("[PI] Cannot save: invoice or InvoiceNo is null/empty");
                    return;
                }

                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Include,
                    Formatting = Formatting.Indented
                };

                var json = JsonConvert.SerializeObject(invoice, settings);
                var filePath = GetInvoiceFilePath(invoice.InvoiceNo);
                File.WriteAllText(filePath, json);
                System.Diagnostics.Debug.WriteLine($"[PI] Saved invoice: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PI] SaveInvoice Error: {ex.Message}");
            }
        }

        private string GetInvoiceFilePath(string invoiceNo)
        {
            if (string.IsNullOrWhiteSpace(invoiceNo))
                return null;

            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            return Path.Combine(folder, $"{invoiceNo}.json");
        }

        private void CalculateStatistics()
        {
            int total = 0, converted = 0, draft = 0, sent = 0, confirmed = 0, hold = 0, completed = 0;
            foreach (var inv in FilteredInvoices)
            {
                if (inv == null) continue;
                total++;
                if (inv.IsConvertedToJobOrder) converted++;
                switch (inv.Status)
                {
                    case "Draft": draft++; break;
                    case "Sent": sent++; break;
                    case "Confirmed": confirmed++; break;
                    case "Hold": hold++; break;
                    case "Completed": completed++; break;
                }
            }

            TotalInvoiceCount = AllInvoices.Count;
            ConvertedToJOCount = converted;
            DraftCount = draft;
            SentCount = sent;
            ConfirmedCount = confirmed;
            HoldCount = hold;
            CompletedCount = completed;
            HasNoInvoices = FilteredInvoices.Count == 0;
        }

        public void SaveOnExit()
        {
            try
            {
                foreach (var invoice in AllInvoices)
                {
                    if (invoice != null && invoice.IsDirty)
                    {
                        SaveInvoice(invoice);
                        invoice.IsDirty = false;
                    }
                }
            }
            catch { }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public class StatusOption { public string Label { get; set; } = ""; public string Value { get; set; } = ""; }
        public class SalesmanOption { public string Name { get; set; } = ""; }
        public class CustomerOption { public string Name { get; set; } = ""; }
        public class CustRefOption { public string Reference { get; set; } = ""; }

        public class RelayCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Func<object?, bool>? _canExecute;
            public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }
            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
            public void Execute(object? parameter) => _execute(parameter);
            public event EventHandler? CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }
        }

        public class RelayCommand<T> : ICommand
        {
            private readonly Action<T?> _execute;
            private readonly Func<T?, bool>? _canExecute;
            public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
            {
                _execute = execute;
                _canExecute = canExecute;
            }
            public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
            public void Execute(object? parameter) => _execute((T?)parameter);
            public event EventHandler? CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }
        }
    }
}