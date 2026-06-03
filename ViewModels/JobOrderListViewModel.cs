using ProGlassAutomation.Data.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DbJobOrder = ProGlassAutomation.Data.Database.JobOrderModel;

namespace ProGlassAutomation.ViewModels
{
    public class JobOrderListViewModel : INotifyPropertyChanged
    {
        // Events
        public event Action<DbJobOrder>? OpenJobOrderRequested;
        public event Action<DbJobOrder>? OpenJobOrderForEditRequested;
        public event Action<DbJobOrder>? OpenProformaInvoiceRequested;
        public event Action? NewJobOrderRequested;

        // Private fields
        private string _searchText = "";
        private string _selectedStatus = "All Status";
        private string _customerFilter = "";
        private string _selectedSalesman = "";
        private DateTime? _fromDate;
        private DateTime? _toDate;
        private DbJobOrder? _selectedJobOrder;
        private bool _isLoading;
        private bool _isRefreshing;
        private bool _suppressStatusUpdate;
        private bool _isUpdatingStatus;

        // Track pending status updates
        private readonly Dictionary<int, string> _pendingStatusUpdates = new Dictionary<int, string>();
        private readonly object _pendingUpdatesLock = new object();

        // Debounce timer for filters
        private DispatcherTimer? _filterDebounceTimer;

        // Collections
        public ObservableCollection<DbJobOrder> JobOrders { get; } = new ObservableCollection<DbJobOrder>();
        public ObservableCollection<DbJobOrder> FilteredJobOrders { get; } = new ObservableCollection<DbJobOrder>();
        public ObservableCollection<string> SalesmanList { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> StatusOptions { get; } = new ObservableCollection<string>
        {
            "All Status", "Pending", "In Progress", "Completed", "On Hold", "Cancelled"
        };

        public ObservableCollection<string> EditableStatusOptions { get; } = new ObservableCollection<string>
        {
            "Pending", "In Progress", "Completed", "On Hold", "Cancelled"
        };

        // Properties
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); DebouncedApplyFilters(); }
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set { _selectedStatus = value; OnPropertyChanged(); DebouncedApplyFilters(); }
        }

        public string CustomerFilter
        {
            get => _customerFilter;
            set { _customerFilter = value; OnPropertyChanged(); DebouncedApplyFilters(); }
        }

        public string SelectedSalesman
        {
            get => _selectedSalesman;
            set { _selectedSalesman = value; OnPropertyChanged(); DebouncedApplyFilters(); }
        }

        public DateTime? FromDate
        {
            get => _fromDate;
            set { _fromDate = value; OnPropertyChanged(); DebouncedApplyFilters(); }
        }

        public DateTime? ToDate
        {
            get => _toDate;
            set { _toDate = value; OnPropertyChanged(); DebouncedApplyFilters(); }
        }

        public bool SuppressStatusUpdate
        {
            get => _suppressStatusUpdate;
            set { _suppressStatusUpdate = value; OnPropertyChanged(); }
        }

        public bool IsUpdatingStatus
        {
            get => _isUpdatingStatus;
            set { _isUpdatingStatus = value; OnPropertyChanged(); }
        }

        public DbJobOrder? SelectedJobOrder
        {
            get => _selectedJobOrder;
            set { _selectedJobOrder = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set { _isRefreshing = value; OnPropertyChanged(); }
        }

        // Stats with pending status consideration
        public int TotalJOCount => JobOrders?.Count ?? 0;

        public int PendingCount
        {
            get
            {
                int count = JobOrders?.Count(j =>
                    (j?.Status ?? "") == "Pending" ||
                    IsPendingStatusUpdate(j?.Id ?? 0, "Pending")) ?? 0;
                return count;
            }
        }

        public int InProgressCount
        {
            get
            {
                int count = JobOrders?.Count(j =>
                    (j?.Status ?? "") == "In Progress" ||
                    IsPendingStatusUpdate(j?.Id ?? 0, "In Progress")) ?? 0;
                return count;
            }
        }

        public int CompletedCount
        {
            get
            {
                int count = JobOrders?.Count(j =>
                    (j?.Status ?? "") == "Completed" ||
                    IsPendingStatusUpdate(j?.Id ?? 0, "Completed")) ?? 0;
                return count;
            }
        }

        // 🔴 ADD: OnHoldCount
        public int OnHoldCount
        {
            get
            {
                int count = JobOrders?.Count(j =>
                    (j?.Status ?? "") == "On Hold" ||
                    IsPendingStatusUpdate(j?.Id ?? 0, "On Hold")) ?? 0;
                return count;
            }
        }

        // 🔴 ADD: CancelledCount
        public int CancelledCount
        {
            get
            {
                int count = JobOrders?.Count(j =>
                    (j?.Status ?? "") == "Cancelled" ||
                    IsPendingStatusUpdate(j?.Id ?? 0, "Cancelled")) ?? 0;
                return count;
            }
        }

        private bool IsPendingStatusUpdate(int jobOrderId, string status)
        {
            lock (_pendingUpdatesLock)
            {
                return _pendingStatusUpdates.TryGetValue(jobOrderId, out var pendingStatus) &&
                       pendingStatus == status;
            }
        }

        // Commands
        public ICommand CreateNewJobOrderCommand { get; }
        public ICommand ViewJobOrderCommand { get; }
        public ICommand EditJobOrderCommand { get; }
        public ICommand DeleteJobOrderCommand { get; }
        public ICommand OpenPICommand { get; }
        public ICommand ExportReportCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand FilterTodayCommand { get; }
        public ICommand FilterThisWeekCommand { get; }
        public ICommand FilterThisMonthCommand { get; }
        public ICommand FilterThisYearCommand { get; }
        public ICommand QuickFilterAllTimeCommand { get; }
        public ICommand QuickFilterPendingCommand { get; }
        public ICommand QuickFilterInProgressCommand { get; }
        public ICommand QuickFilterCompletedCommand { get; }
        public ICommand QuickFilterAllCommand { get; }

        public JobOrderListViewModel()
        {
            _filterDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _filterDebounceTimer.Tick += (s, e) =>
            {
                _filterDebounceTimer.Stop();
                ApplyFilters();
            };

            CreateNewJobOrderCommand = new RelayCommand(_ => CreateNewJobOrder());
            ViewJobOrderCommand = new RelayCommand(param => ViewJobOrder(param as DbJobOrder));
            EditJobOrderCommand = new RelayCommand(param => EditJobOrder(param as DbJobOrder));
            DeleteJobOrderCommand = new RelayCommand(param => DeleteJobOrder(param as DbJobOrder));
            OpenPICommand = new RelayCommand(param => OpenProformaInvoice(param as DbJobOrder));
            ExportReportCommand = new RelayCommand(_ => ExportReport());
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
            RefreshCommand = new RelayCommand(_ => LoadJobOrdersAsync(), _ => !IsLoading);

            FilterTodayCommand = new RelayCommand(_ => FilterToday());
            FilterThisWeekCommand = new RelayCommand(_ => FilterThisWeek());
            FilterThisMonthCommand = new RelayCommand(_ => FilterThisMonth());
            FilterThisYearCommand = new RelayCommand(_ => FilterThisYear());
            QuickFilterAllTimeCommand = new RelayCommand(_ => QuickFilterAllTime());

            QuickFilterPendingCommand = new RelayCommand(_ => QuickFilterPending());
            QuickFilterInProgressCommand = new RelayCommand(_ => QuickFilterInProgress());
            QuickFilterCompletedCommand = new RelayCommand(_ => QuickFilterCompleted());
            QuickFilterAllCommand = new RelayCommand(_ => QuickFilterAll());

            LoadJobOrdersAsync();
        }

        private void DebouncedApplyFilters()
        {
            _filterDebounceTimer?.Stop();
            _filterDebounceTimer?.Start();
        }

        public async void LoadJobOrdersAsync()
        {
            try
            {
                await LoadJobOrdersAsyncCore();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobOrderListVM] LoadJobOrdersAsync Error: {ex.Message}");
            }
        }

        private async Task LoadJobOrdersAsyncCore()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                IsRefreshing = true;
                SuppressStatusUpdate = true;

                var orders = await Task.Run(() =>
                {
                    try
                    {
                        WaitForPendingUpdates();
                        return DbHelper.GetAllJobOrders();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[JobOrderListVM] DB Error: {ex.Message}");
                        return null;
                    }
                });

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    JobOrders.Clear();
                    FilteredJobOrders.Clear();

                    if (orders != null)
                    {
                        foreach (var order in orders)
                        {
                            if (order != null)
                            {
                                JobOrders.Add(order);
                            }
                        }
                    }

                    ApplyFilters();
                    RefreshCounts();
                    IsLoading = false;
                    IsRefreshing = false;
                    SuppressStatusUpdate = false;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobOrderListVM] LoadJobOrders Error: {ex.Message}");

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = false;
                    IsRefreshing = false;
                    SuppressStatusUpdate = false;
                    System.Windows.MessageBox.Show($"Error loading job orders: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void WaitForPendingUpdates()
        {
            lock (_pendingUpdatesLock)
            {
                var timeout = DateTime.Now.AddSeconds(5);
                while (_pendingStatusUpdates.Count > 0 && DateTime.Now < timeout)
                {
                    Thread.Sleep(50);
                }
            }
        }

        private void ApplyFilters()
        {
            if (JobOrders == null || JobOrders.Count == 0)
            {
                FilteredJobOrders.Clear();
                return;
            }

            try
            {
                var filtered = JobOrders.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var search = SearchText.ToLower();
                    filtered = filtered.Where(j =>
                        (j?.JONumber?.ToLower().Contains(search) ?? false) ||
                        (j?.ClientName?.ToLower().Contains(search) ?? false) ||
                        (j?.ProjectName?.ToLower().Contains(search) ?? false) ||
                        (j?.PINumber?.ToLower().Contains(search) ?? false));
                }

                if (!string.IsNullOrWhiteSpace(SelectedStatus) && SelectedStatus != "All Status")
                {
                    filtered = filtered.Where(j => j?.Status == SelectedStatus);
                }

                if (!string.IsNullOrWhiteSpace(CustomerFilter))
                {
                    var customerSearch = CustomerFilter.ToLower();
                    filtered = filtered.Where(j =>
                        j?.ClientName?.ToLower().Contains(customerSearch) ?? false);
                }

                if (!string.IsNullOrWhiteSpace(SelectedSalesman))
                {
                    filtered = filtered.Where(j => j?.Salesman == SelectedSalesman);
                }

                if (FromDate.HasValue)
                {
                    filtered = filtered.Where(j => j?.JODate >= FromDate.Value);
                }

                if (ToDate.HasValue)
                {
                    filtered = filtered.Where(j => j?.JODate <= ToDate.Value.AddDays(1));
                }

                FilteredJobOrders.Clear();
                foreach (var order in filtered.OrderByDescending(j => j?.JODate ?? DateTime.MinValue))
                {
                    if (order != null)
                    {
                        FilteredJobOrders.Add(order);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobOrderListVM] ApplyFilters Error: {ex.Message}");
            }
        }

        private void RefreshCounts()
        {
            try
            {
                OnPropertyChanged(nameof(TotalJOCount));
                OnPropertyChanged(nameof(PendingCount));
                OnPropertyChanged(nameof(InProgressCount));
                OnPropertyChanged(nameof(CompletedCount));
                OnPropertyChanged(nameof(OnHoldCount));      // 🔴 ADD
                OnPropertyChanged(nameof(CancelledCount));  // 🔴 ADD
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobOrderListVM] RefreshCounts Error: {ex.Message}");
            }
        }

        private void FilterToday()
        {
            FromDate = DateTime.Today;
            ToDate = DateTime.Today;
        }

        private void FilterThisWeek()
        {
            FromDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            ToDate = DateTime.Today;
        }

        private void FilterThisMonth()
        {
            FromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ToDate = DateTime.Today;
        }

        private void FilterThisYear()
        {
            FromDate = new DateTime(DateTime.Today.Year, 1, 1);
            ToDate = DateTime.Today;
        }

        private void QuickFilterAllTime()
        {
            FromDate = null;
            ToDate = null;
        }

        private void QuickFilterPending() => SelectedStatus = "Pending";
        private void QuickFilterInProgress() => SelectedStatus = "In Progress";
        private void QuickFilterCompleted() => SelectedStatus = "Completed";
        private void QuickFilterAll() => SelectedStatus = "All Status";

        private void CreateNewJobOrder()
        {
            try
            {
                NewJobOrderRequested?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobOrderListVM] CreateNewJobOrder Error: {ex.Message}");
            }
        }

        private void ViewJobOrder(DbJobOrder? jobOrder)
        {
            if (jobOrder == null) return;
            try
            {
                OpenJobOrderRequested?.Invoke(jobOrder);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobOrderListVM] ViewJobOrder Error: {ex.Message}");
            }
        }

        private void EditJobOrder(DbJobOrder? jobOrder)
        {
            if (jobOrder == null) return;
            try
            {
                OpenJobOrderForEditRequested?.Invoke(jobOrder);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobOrderListVM] EditJobOrder Error: {ex.Message}");
            }
        }

        private void OpenProformaInvoice(DbJobOrder? jobOrder)
        {
            if (jobOrder == null) return;
            try
            {
                OpenProformaInvoiceRequested?.Invoke(jobOrder);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobOrderListVM] OpenProformaInvoice Error: {ex.Message}");
            }
        }

        private void DeleteJobOrder(DbJobOrder? jobOrder)
        {
            if (jobOrder == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Delete Job Order {jobOrder.JONumber}?\n\nThis action cannot be undone.",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                DeleteJobOrderAsync(jobOrder);
            }
        }

        private async void DeleteJobOrderAsync(DbJobOrder jobOrder)
        {
            try
            {
                await Task.Run(() => DbHelper.DeleteJobOrder(jobOrder.Id));

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    JobOrders.Remove(jobOrder);
                    ApplyFilters();
                    RefreshCounts();

                    System.Windows.MessageBox.Show(
                        $"Job Order {jobOrder.JONumber} deleted successfully.",
                        "Deleted",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    System.Windows.MessageBox.Show($"Error deleting: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        public void UpdateStatus(DbJobOrder jobOrder, string newStatus)
        {
            if (jobOrder == null || string.IsNullOrWhiteSpace(newStatus)) return;
            if (_isUpdatingStatus) return;
            if (SuppressStatusUpdate) return;
            if (IsLoading) return;
            if (jobOrder.Status == newStatus) return;

            _isUpdatingStatus = true;

            lock (_pendingUpdatesLock)
            {
                _pendingStatusUpdates[jobOrder.Id] = newStatus;
            }

            var oldStatus = jobOrder.Status;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Run(() => DbHelper.UpdateJobOrderStatus(jobOrder.Id, newStatus));

                    lock (_pendingUpdatesLock)
                    {
                        _pendingStatusUpdates.Remove(jobOrder.Id);
                    }

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        jobOrder.Status = newStatus;
                        ApplyFilters();
                        RefreshCounts();
                        _isUpdatingStatus = false;
                    });
                }
                catch (Exception ex)
                {
                    lock (_pendingUpdatesLock)
                    {
                        _pendingStatusUpdates.Remove(jobOrder.Id);
                    }

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _isUpdatingStatus = false;
                        System.Windows.MessageBox.Show($"Error updating status: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });
        }

        private void ExportReport()
        {
            System.Windows.MessageBox.Show("Export feature coming soon!", "Export",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearFilters()
        {
            SearchText = "";
            SelectedStatus = "All Status";
            CustomerFilter = "";
            SelectedSalesman = "";
            FromDate = null;
            ToDate = null;
        }

        public void Cleanup()
        {
            JobOrders.Clear();
            FilteredJobOrders.Clear();
            lock (_pendingUpdatesLock)
            {
                _pendingStatusUpdates.Clear();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}