using ProGlassAutomation.Data.Database;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
        public event Action<DbJobOrder>? OpenProformaInvoiceRequested;
        public event Action? NewJobOrderRequested;

        // Private fields
        private string _searchText = "";
        private string _selectedStatus = "All Status";
        private string _customerFilter = "";
        private DateTime? _fromDate;
        private DateTime? _toDate;
        private DbJobOrder? _selectedJobOrder;
        private bool _isLoading;
        private bool _isRefreshing;

        // Collections
        public ObservableCollection<DbJobOrder> JobOrders { get; } = new ObservableCollection<DbJobOrder>();
        public ObservableCollection<DbJobOrder> FilteredJobOrders { get; } = new ObservableCollection<DbJobOrder>();
        public ObservableCollection<string> SalesmanList { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> StatusOptions { get; } = new ObservableCollection<string>
        {
            "All Status", "Pending", "In Progress", "Completed", "On Hold", "Cancelled"
        };

        // Properties
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ApplyFilters(); }
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set { _selectedStatus = value; OnPropertyChanged(); ApplyFilters(); }
        }

        public string CustomerFilter
        {
            get => _customerFilter;
            set { _customerFilter = value; OnPropertyChanged(); ApplyFilters(); }
        }

        public DateTime? FromDate
        {
            get => _fromDate;
            set { _fromDate = value; OnPropertyChanged(); ApplyFilters(); }
        }

        public DateTime? ToDate
        {
            get => _toDate;
            set { _toDate = value; OnPropertyChanged(); ApplyFilters(); }
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

        // Summary Counts
        public int TotalJOCount => JobOrders.Count;
        public int PendingCount => JobOrders.Count(j => j.Status == "Pending");
        public int InProgressCount => JobOrders.Count(j => j.Status == "In Progress");
        public int CompletedCount => JobOrders.Count(j => j.Status == "Completed");

        // Commands
        public ICommand CreateNewJobOrderCommand { get; }
        public ICommand ViewJobOrderCommand { get; }
        public ICommand EditJobOrderCommand { get; }
        public ICommand DeleteJobOrderCommand { get; }
        public ICommand OpenPICommand { get; }
        public ICommand ExportReportCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand RefreshCommand { get; }

        public JobOrderListViewModel()
        {
            CreateNewJobOrderCommand = new RelayCommand(_ => CreateNewJobOrder());
            ViewJobOrderCommand = new RelayCommand(param => ViewJobOrder(param as DbJobOrder));
            EditJobOrderCommand = new RelayCommand(param => EditJobOrder(param as DbJobOrder));
            DeleteJobOrderCommand = new RelayCommand(param => DeleteJobOrder(param as DbJobOrder));
            OpenPICommand = new RelayCommand(param => OpenProformaInvoice(param as DbJobOrder));
            ExportReportCommand = new RelayCommand(_ => ExportReport());
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
            RefreshCommand = new RelayCommand(_ => LoadJobOrdersAsync(), _ => !IsLoading);

            // Initial load
            LoadJobOrdersAsync();
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

                // Load data in background thread
                var orders = await Task.Run(() =>
                {
                    try
                    {
                        return Data.Database.DbHelper.GetAllJobOrders();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[JobOrderListVM] DB Error: {ex.Message}");
                        return null;
                    }
                });

                // Clear on UI thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    JobOrders.Clear();
                    FilteredJobOrders.Clear();
                });

                if (orders == null || orders.Count == 0)
                {
                    IsLoading = false;
                    IsRefreshing = false;
                    return;
                }

                // Add items in batches to avoid UI freeze
                const int batchSize = 50;
                var batches = orders
                    .Select((item, index) => new { item, index })
                    .GroupBy(x => x.index / batchSize)
                    .Select(g => g.Select(x => x.item).ToList())
                    .ToList();

                foreach (var batch in batches)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var order in batch)
                        {
                            JobOrders.Add(order);
                        }
                    });

                    // Small delay to allow UI to breathe
                    await Task.Delay(1);
                }

                // Apply filters
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ApplyFilters();
                    RefreshCounts();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobOrderListVM] LoadJobOrders Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    System.Windows.MessageBox.Show($"Error loading job orders: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
            }
        }

        private void ApplyFilters()
        {
            if (JobOrders == null) return;

            try
            {
                FilteredJobOrders.Clear();

                var filtered = JobOrders.AsEnumerable();

                // Search filter - CORRECTED property names
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var search = SearchText.ToLower();
                    filtered = filtered.Where(j =>
                        (j.JONumber?.ToLower().Contains(search) ?? false) ||
                        (j.ClientName?.ToLower().Contains(search) ?? false) ||
                        (j.ProjectName?.ToLower().Contains(search) ?? false) ||
                        (j.PINumber?.ToLower().Contains(search) ?? false));
                }

                // Status filter
                if (!string.IsNullOrWhiteSpace(SelectedStatus) && SelectedStatus != "All Status")
                {
                    filtered = filtered.Where(j => j.Status == SelectedStatus);
                }

                // Customer filter - use ClientName
                if (!string.IsNullOrWhiteSpace(CustomerFilter))
                {
                    var customerSearch = CustomerFilter.ToLower();
                    filtered = filtered.Where(j =>
                        j.ClientName?.ToLower().Contains(customerSearch) ?? false);
                }

                // Date range filter - use JODate
                if (FromDate.HasValue)
                {
                    filtered = filtered.Where(j => j.JODate >= FromDate.Value);
                }

                if (ToDate.HasValue)
                {
                    filtered = filtered.Where(j => j.JODate <= ToDate.Value.AddDays(1));
                }

                // Sort by date descending
                foreach (var order in filtered.OrderByDescending(j => j.JODate))
                {
                    FilteredJobOrders.Add(order);
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobOrderListVM] RefreshCounts Error: {ex.Message}");
            }
        }

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
                OpenJobOrderRequested?.Invoke(jobOrder);
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
                // Run delete on background thread
                await Task.Run(() => Data.Database.DbHelper.DeleteJobOrder(jobOrder.Id));

                // Update UI on main thread
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
                        System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        public void UpdateStatus(ProGlassAutomation.Data.Database.JobOrderModel jobOrder, string newStatus)
        {
            if (jobOrder == null || string.IsNullOrWhiteSpace(newStatus)) return;

            Task.Run(async () =>
            {
                try
                {
                    jobOrder.Status = newStatus;
                    await Task.Run(() =>
                        Data.Database.DbHelper.UpdateJobOrderStatus(jobOrder.Id, newStatus));

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ApplyFilters();
                        RefreshCounts();
                    });
                }
                catch (Exception ex)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        System.Windows.MessageBox.Show($"Error updating status: {ex.Message}", "Error",
                            System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });
        }

        private void ExportReport()
        {
            System.Windows.MessageBox.Show("Export feature coming soon!", "Export",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void ClearFilters()
        {
            SearchText = "";
            SelectedStatus = "All Status";
            CustomerFilter = "";
            FromDate = null;
            ToDate = null;
        }

        public void Cleanup()
        {
            // Called when leaving the view
            JobOrders.Clear();
            FilteredJobOrders.Clear();
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}