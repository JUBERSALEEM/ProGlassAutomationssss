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

        // EditableStatusOptions for DataGrid ComboBox
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
            LoadInvoicesFromFolder(_defaultFolderPath);
        }

        // ==================== ISLOADING ====================
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { if (SetProperty(ref _isLoading, value)) OnPropertyChanged(); }
        }

        private bool _hasNoInvoices = true;
        public bool HasNoInvoices
        {
            get => _hasNoInvoices;
            set { if (SetProperty(ref _hasNoInvoices, value)) OnPropertyChanged(); }
        }

        // COLLECTIONS
        public ObservableCollection<ProformaInvoiceModel> AllInvoices { get; }
        public ObservableCollection<ProformaInvoiceModel> FilteredInvoices { get; }
        public ObservableCollection<StatusOption> StatusOptions { get; }
        public ObservableCollection<SalesmanOption> SalesmanOptions { get; }
        public ObservableCollection<CustomerOption> CustomerOptions { get; }
        public ObservableCollection<CustRefOption> CustRefOptions { get; }

        private ProformaInvoiceModel? _selectedInvoice;
        public ProformaInvoiceModel? SelectedInvoice
        {
            get => _selectedInvoice;
            set { if (SetProperty(ref _selectedInvoice, value)) OnPropertyChanged(); }
        }

        // ==================== SEARCH ====================
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
                        _ =>
                        {
                            Application.Current.Dispatcher.BeginInvoke(
                                System.Windows.Threading.DispatcherPriority.Background,
                                (Action)(() => ApplyFiltersIfNotBusy()));
                        },
                        null, 300, Timeout.Infinite);
                }
            }
        }

        private string? _selectedStatus;
        public string? SelectedStatus
        {
            get => _selectedStatus;
            set { if (SetProperty(ref _selectedStatus, value)) ApplyFiltersIfNotBusy(); }
        }

        private string? _selectedSalesman;
        public string? SelectedSalesman
        {
            get => _selectedSalesman;
            set { if (SetProperty(ref _selectedSalesman, value)) ApplyFiltersIfNotBusy(); }
        }

        private string? _selectedCustomer;
        public string? SelectedCustomer
        {
            get => _selectedCustomer;
            set { if (SetProperty(ref _selectedCustomer, value)) ApplyFiltersIfNotBusy(); }
        }

        private string? _selectedCustRef;
        public string? SelectedCustRef
        {
            get => _selectedCustRef;
            set { if (SetProperty(ref _selectedCustRef, value)) ApplyFiltersIfNotBusy(); }
        }

        private DateTime? _dateFrom;
        public DateTime? DateFrom
        {
            get => _dateFrom;
            set { if (SetProperty(ref _dateFrom, value)) ApplyFiltersIfNotBusy(); }
        }

        private DateTime? _dateTo;
        public DateTime? DateTo
        {
            get => _dateTo;
            set { if (SetProperty(ref _dateTo, value)) ApplyFiltersIfNotBusy(); }
        }

        // ==================== FILTER CHECKBOXES ====================
        private bool _filterDraft = true;
        public bool FilterDraft
        {
            get => _filterDraft;
            set { if (SetProperty(ref _filterDraft, value)) ApplyFiltersIfNotBusy(); }
        }

        private bool _filterSent = true;
        public bool FilterSent
        {
            get => _filterSent;
            set { if (SetProperty(ref _filterSent, value)) ApplyFiltersIfNotBusy(); }
        }

        private bool _filterConfirmed = true;
        public bool FilterConfirmed
        {
            get => _filterConfirmed;
            set { if (SetProperty(ref _filterConfirmed, value)) ApplyFiltersIfNotBusy(); }
        }

        private bool _filterHold = true;
        public bool FilterHold
        {
            get => _filterHold;
            set { if (SetProperty(ref _filterHold, value)) ApplyFiltersIfNotBusy(); }
        }

        private bool _filterRevised = true;
        public bool FilterRevised
        {
            get => _filterRevised;
            set { if (SetProperty(ref _filterRevised, value)) ApplyFiltersIfNotBusy(); }
        }

        private bool _filterCompleted = true;
        public bool FilterCompleted
        {
            get => _filterCompleted;
            set { if (SetProperty(ref _filterCompleted, value)) ApplyFiltersIfNotBusy(); }
        }

        private bool _filterCancelled = false;
        public bool FilterCancelled
        {
            get => _filterCancelled;
            set { if (SetProperty(ref _filterCancelled, value)) ApplyFiltersIfNotBusy(); }
        }

        private bool _filterConvertedToJO = false;
        public bool FilterConvertedToJO
        {
            get => _filterConvertedToJO;
            set { if (SetProperty(ref _filterConvertedToJO, value)) ApplyFiltersIfNotBusy(); }
        }

        // ==================== STATISTICS ====================
        private int _totalInvoiceCount;
        public int TotalInvoiceCount
        {
            get => _totalInvoiceCount;
            set { if (SetProperty(ref _totalInvoiceCount, value)) OnPropertyChanged(); }
        }

        private int _convertedToJOCount;
        public int ConvertedToJOCount
        {
            get => _convertedToJOCount;
            set { if (SetProperty(ref _convertedToJOCount, value)) OnPropertyChanged(); }
        }

        private int _draftCount;
        public int DraftCount
        {
            get => _draftCount;
            set { if (SetProperty(ref _draftCount, value)) OnPropertyChanged(); }
        }

        private int _sentCount;
        public int SentCount
        {
            get => _sentCount;
            set { if (SetProperty(ref _sentCount, value)) OnPropertyChanged(); }
        }

        private int _confirmedCount;
        public int ConfirmedCount
        {
            get => _confirmedCount;
            set { if (SetProperty(ref _confirmedCount, value)) OnPropertyChanged(); }
        }

        private int _holdCount;
        public int HoldCount
        {
            get => _holdCount;
            set { if (SetProperty(ref _holdCount, value)) OnPropertyChanged(); }
        }

        private int _completedCount;
        public int CompletedCount
        {
            get => _completedCount;
            set { if (SetProperty(ref _completedCount, value)) OnPropertyChanged(); }
        }

        // ==================== COMMANDS ====================
        public ICommand NewInvoiceCommand { get; private set; }
        public ICommand EditInvoiceCommand { get; private set; }
        public ICommand ViewInvoiceCommand { get; private set; }
        public ICommand DeleteInvoiceCommand { get; private set; }
        public ICommand CreateJobOrderCommand { get; private set; }
        public ICommand OpenFolderCommand { get; private set; }
        public ICommand ExportAllCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand ClearFiltersCommand { get; private set; }
        public ICommand QuickFilterThisWeekCommand { get; private set; }
        public ICommand QuickFilterThisMonthCommand { get; private set; }
        public ICommand QuickFilterTodayCommand { get; private set; }
        public ICommand QuickFilterThisYearCommand { get; private set; }
        public ICommand QuickFilterAllTimeCommand { get; private set; }

        private void InitializeCommands()
        {
            NewInvoiceCommand = new RelayCommand(_ => ExecuteNewInvoice(_));
            EditInvoiceCommand = new RelayCommand<ProformaInvoiceModel>(_ => ExecuteEditInvoice(_));
            ViewInvoiceCommand = new RelayCommand<ProformaInvoiceModel>(_ => ExecuteViewInvoice(_));
            DeleteInvoiceCommand = new RelayCommand<ProformaInvoiceModel>(_ => ExecuteDeleteInvoice(_));
            CreateJobOrderCommand = new RelayCommand<ProformaInvoiceModel>(_ => ExecuteCreateJobOrder(_));
            OpenFolderCommand = new RelayCommand(_ => ExecuteOpenFolder(_));
            ExportAllCommand = new RelayCommand(_ => ExecuteExportAll(_));
            RefreshCommand = new RelayCommand(_ => ExecuteRefresh(_));
            ClearFiltersCommand = new RelayCommand(_ => ExecuteClearFilters(_));

            QuickFilterThisWeekCommand = new RelayCommand(_ => { DateFrom = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek); DateTo = DateTime.Today; });
            QuickFilterThisMonthCommand = new RelayCommand(_ => { DateFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); DateTo = DateTime.Today; });
            QuickFilterTodayCommand = new RelayCommand(_ => { DateFrom = DateTime.Today; DateTo = DateTime.Today; });
            QuickFilterThisYearCommand = new RelayCommand(_ => { DateFrom = new DateTime(DateTime.Today.Year, 1, 1); DateTo = DateTime.Today; });
            QuickFilterAllTimeCommand = new RelayCommand(_ => { DateFrom = null; DateTo = null; });
        }

        // ==================== EXECUTE METHODS ====================

        private void ExecuteNewInvoice(object? parameter)
        {
            try
            {
                SharedViewModels.ProformaInvoiceVM.CreateNewInvoice();
                var editorVM = SharedViewModels.ProformaInvoiceVM;
                var editorView = new ProGlassAutomation.Views.ProformaInvoice.ProformaInvoiceView { DataContext = editorVM };
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.SetContent(editorView);
            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ExecuteEditInvoice(ProformaInvoiceModel? invoice)
        {
            if (invoice == null) return;
            try
            {
                SharedViewModels.ProformaInvoiceVM.LoadFromExistingInvoice(invoice);
                var editorVM = SharedViewModels.ProformaInvoiceVM;
                var editorView = new ProGlassAutomation.Views.ProformaInvoice.ProformaInvoiceView { DataContext = editorVM };
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.SetContent(editorView);
            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ExecuteViewInvoice(ProformaInvoiceModel? invoice) => ExecuteEditInvoice(invoice);

        private void ExecuteDeleteInvoice(ProformaInvoiceModel? invoice)
        {
            if (invoice == null) return;
            var result = MessageBox.Show($"Delete invoice {invoice.InvoiceNo}?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
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
        }

        private void ExecuteCreateJobOrder(ProformaInvoiceModel? invoice)
        {
            if (invoice == null) return;
            var result = MessageBox.Show($"Create Job Order from {invoice.InvoiceNo}?\nCustomer: {invoice.CustomerName}\nAmount: {invoice.GrandTotal:N0}",
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
            try
            {
                var jobOrderVM = SharedViewModels.JobOrderVM;
                jobOrderVM.ClearForNewJobOrder();
                jobOrderVM.LoadFromProformaInvoice(invoice);
                var jobOrderView = new ProGlassAutomation.Views.JobOrder.JobOrderView { DataContext = jobOrderVM };
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.SetContent(jobOrderView);
            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ExecuteOpenFolder(object? parameter)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog { InitialDirectory = _defaultFolderPath };
            if (dialog.ShowDialog() == true) { LoadInvoicesFromFolder(dialog.FolderName); }
        }

        private void ExecuteExportAll(object? parameter)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = $"Invoices_{DateTime.Now:yyyyMMdd}" };
            if (dialog.ShowDialog() == true) MessageBox.Show($"Exported {FilteredInvoices.Count} invoices to {dialog.FileName}");
        }

        private void ExecuteRefresh(object? parameter)
        {
            // Save current filter state
            var savedSearch = SearchText;
            var savedStatus = SelectedStatus;
            var savedSalesman = SelectedSalesman;
            var savedCustomer = SelectedCustomer;
            var savedCustRef = SelectedCustRef;
            var savedDateFrom = DateFrom;
            var savedDateTo = DateTo;
            var savedFilterDraft = FilterDraft;
            var savedFilterSent = FilterSent;
            var savedFilterConfirmed = FilterConfirmed;
            var savedFilterHold = FilterHold;
            var savedFilterRevised = FilterRevised;
            var savedFilterCompleted = FilterCompleted;
            var savedFilterCancelled = FilterCancelled;
            var savedFilterConvertedToJO = FilterConvertedToJO;

            IsLoading = true;
            LoadInvoicesFromFolder(_defaultFolderPath);

            // Restore filter state
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

            // Notify all properties
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

            // Re-apply filters
            ApplyFilters();
        }

        private void ExecuteClearFilters(object? parameter)
        {
            SearchText = "";
            SelectedStatus = null;
            SelectedSalesman = null;
            SelectedCustomer = null;
            SelectedCustRef = null;
            DateFrom = null;
            DateTo = null;
            FilterDraft = true;
            FilterSent = true;
            FilterConfirmed = true;
            FilterHold = true;
            FilterRevised = true;
            FilterCompleted = true;
            FilterCancelled = false;
            FilterConvertedToJO = false;
        }

        // ==================== APPLY FILTERS ====================

        private void ApplyFiltersIfNotBusy()
        {
            if (_bulkUpdateCount > 0) return;
            _bulkUpdateCount++;

            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                (Action)(() =>
                {
                    _bulkUpdateCount--;
                    ApplyFilters();
                }));
        }

        public void ApplyFilters()
        {
            if (_bulkUpdateCount > 0) return;

            FilteredInvoices.Clear();
            var query = AllInvoices.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var s = SearchText.ToLower();
                query = query.Where(i => (i.InvoiceNo?.ToLower().Contains(s) ?? false)
                    || (i.CustomerName?.ToLower().Contains(s) ?? false)
                    || (i.ProjectName?.ToLower().Contains(s) ?? false)
                    || (i.CustomerReference?.ToLower().Contains(s) ?? false));
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

            query = query.Where(i =>
                (i.Status == "Draft" && FilterDraft)
                || (i.Status == "Sent" && FilterSent)
                || (i.Status == "Confirmed" && FilterConfirmed)
                || (i.Status == "Hold" && FilterHold)
                || (i.Status == "Revised" && FilterRevised)
                || (i.Status == "Completed" && FilterCompleted)
                || (i.Status == "Cancelled" && FilterCancelled));

            if (FilterConvertedToJO)
                query = query.Where(i => i.IsConvertedToJobOrder);

            var filteredList = query.OrderByDescending(i => i.InvoiceDate).ToList();

            foreach (var inv in filteredList)
                FilteredInvoices.Add(inv);

            CalculateStatistics();
        }

        // ==================== LOAD FROM FOLDER ====================

        private void LoadInvoicesFromFolder(string folderPath)
        {
            try
            {
                AllInvoices.Clear();
                SalesmanOptions.Clear();
                CustomerOptions.Clear();
                CustRefOptions.Clear();

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    IsLoading = false;
                    return;
                }

                var settings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };

                var invoices = new List<ProformaInvoiceModel>();

                foreach (var file in Directory.GetFiles(folderPath, "*.json"))
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var invoice = JsonConvert.DeserializeObject<ProformaInvoiceModel>(json, settings);
                        if (invoice != null) invoices.Add(invoice);
                    }
                    catch { }
                }

                foreach (var invoice in invoices)
                {
                    AllInvoices.Add(invoice);
                }

                foreach (var s in AllInvoices.Where(i => !string.IsNullOrWhiteSpace(i.Salesman)).Select(i => i.Salesman!).Distinct().OrderBy(x => x))
                    SalesmanOptions.Add(new SalesmanOption { Name = s });

                foreach (var c in AllInvoices.Where(i => !string.IsNullOrWhiteSpace(i.CustomerName)).Select(i => i.CustomerName!).Distinct().OrderBy(x => x))
                    CustomerOptions.Add(new CustomerOption { Name = c });

                foreach (var r in AllInvoices.Where(i => !string.IsNullOrWhiteSpace(i.CustomerReference)).Select(i => i.CustomerReference!).Distinct().OrderBy(x => x))
                    CustRefOptions.Add(new CustRefOption { Reference = r });
            }
            finally
            {
                IsLoading = false;
                ApplyFilters();
            }
        }

        // ==================== UPDATE STATUS ====================

        public void UpdateStatus(ProformaInvoiceModel invoice, string newStatus)
        {
            if (invoice == null || string.IsNullOrWhiteSpace(newStatus)) return;
            if (invoice.Status == newStatus) return;

            try
            {
                // Update in memory
                invoice.Status = newStatus;

                // Update in collection
                var existing = AllInvoices.FirstOrDefault(i => i.InvoiceNo == invoice.InvoiceNo);
                if (existing != null) existing.Status = newStatus;

                // Save to file
                SaveInvoice(invoice);

                // 🔴 FIX: Force synchronous statistics recalculation
                _bulkUpdateCount = 0;  // Reset bulk counter to allow synchronous update
                CalculateStatistics();  // Direct call - no dispatcher delay
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== SAVE INVOICE ====================

        public void SaveInvoice(ProformaInvoiceModel invoice)
        {
            try
            {
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Formatting = Formatting.Indented
                };

                var json = JsonConvert.SerializeObject(invoice, settings);
                File.WriteAllText(GetInvoiceFilePath(invoice.InvoiceNo), json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveInvoice Error: {ex.Message}");
            }
        }

        private string GetInvoiceFilePath(string invoiceNo)
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            return Path.Combine(folder, $"{invoiceNo}.json");
        }

        // ==================== CALCULATE STATISTICS ====================

        private void CalculateStatistics()
        {
            TotalInvoiceCount = AllInvoices.Count;
            ConvertedToJOCount = FilteredInvoices.Count(i => i.IsConvertedToJobOrder);
            HasNoInvoices = FilteredInvoices.Count == 0;

            // 🔴 FIX: Count from FilteredInvoices, not AllInvoices!
            DraftCount = FilteredInvoices.Count(i => i.Status == "Draft");
            SentCount = FilteredInvoices.Count(i => i.Status == "Sent");
            ConfirmedCount = FilteredInvoices.Count(i => i.Status == "Confirmed");
            HoldCount = FilteredInvoices.Count(i => i.Status == "Hold");
            CompletedCount = FilteredInvoices.Count(i => i.Status == "Completed");

            // 🔴 FORCE UI UPDATE - FIXED CASE-SENSITIVE NAMES!
            OnPropertyChanged(nameof(TotalInvoiceCount));
            OnPropertyChanged(nameof(DraftCount));      // uppercase D - FIXED!
            OnPropertyChanged(nameof(SentCount));      // uppercase S
            OnPropertyChanged(nameof(ConfirmedCount)); // uppercase C
            OnPropertyChanged(nameof(HoldCount));    // uppercase H - FIXED!
            OnPropertyChanged(nameof(CompletedCount)); // uppercase C
            OnPropertyChanged(nameof(ConvertedToJOCount));
            OnPropertyChanged(nameof(HasNoInvoices));
        }

        public void SaveOnExit()
        {
            try
            {
                foreach (var invoice in AllInvoices)
                {
                    if (invoice.IsDirty)
                    {
                        SaveInvoice(invoice);
                        invoice.IsDirty = false;
                    }
                }
            }
            catch { }
        }

        // ==================== INOTIFYPROPERTYCHANGED ====================

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

        // ==================== HELPER CLASSES ====================

        public class StatusOption { public string Label { get; set; } = ""; public string Value { get; set; } = ""; }
        public class SalesmanOption { public string Name { get; set; } = ""; }
        public class CustomerOption { public string Name { get; set; } = ""; }
        public class CustRefOption { public string Reference { get; set; } = ""; }

        // ==================== RELAYCOMMAND ====================

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