using ProGlassAutomation.Data.Database;
using ProGlassAutomation.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ProGlassAutomation.ViewModels
{
    public class JobOrderListViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<JobOrder> _jobOrders = new();
        private JobOrder _selectedJobOrder;
        private string _searchText = "";
        private string _selectedStatus = "All";
        private bool _isLoading;

        // Events for navigation
        public event Action<JobOrder> OpenJobOrderRequested;
        public event Action<JobOrder> OpenProformaInvoiceRequested;
        public event Action NewJobOrderRequested;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ObservableCollection<JobOrder> JobOrders
        {
            get => _jobOrders;
            set
            {
                _jobOrders = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredJobOrders));
            }
        }

        public ObservableCollection<JobOrder> FilteredJobOrders
        {
            get
            {
                var filtered = JobOrders.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var search = SearchText.ToLower();
                    filtered = filtered.Where(jo =>
                        (jo.JobNumber?.ToLower().Contains(search) ?? false) ||
                        (jo.CustomerName?.ToLower().Contains(search) ?? false) ||
                        (jo.ProjectName?.ToLower().Contains(search) ?? false));
                }

                if (SelectedStatus != "All")
                {
                    filtered = filtered.Where(jo => jo.Status == SelectedStatus);
                }

                return new ObservableCollection<JobOrder>(filtered);
            }
        }

        public JobOrder SelectedJobOrder
        {
            get => _selectedJobOrder;
            set
            {
                _selectedJobOrder = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredJobOrders));
            }
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                _selectedStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredJobOrders));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public string[] StatusOptions { get; } = { "All", "Pending", "In Progress", "Completed", "Cancelled" };

        public ICommand RefreshCommand { get; }
        public ICommand NewJobOrderCommand { get; }
        public ICommand OpenJobOrderCommand { get; }
        public ICommand OpenProformaInvoiceCommand { get; }
        public ICommand DeleteJobOrderCommand { get; }

        public JobOrderListViewModel()
        {
            RefreshCommand = new RelayCommand(_ => LoadJobOrders());
            NewJobOrderCommand = new RelayCommand(_ => NewJobOrderRequested?.Invoke());
            OpenJobOrderCommand = new RelayCommand(jo => OpenJobOrder(jo as JobOrder));
            OpenProformaInvoiceCommand = new RelayCommand(jo => OpenProformaInvoice(jo as JobOrder));
            DeleteJobOrderCommand = new RelayCommand(jo => DeleteJobOrder(jo as JobOrder));

            LoadJobOrders();
        }

        private void LoadJobOrders()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[JobOrderListVM] Loading Job Orders from database...");
                var dbOrders = DbHelper.GetAllJobOrders();
                System.Diagnostics.Debug.WriteLine($"[JobOrderListVM] Found {dbOrders.Count} Job Orders in database");

                var orders = new ObservableCollection<JobOrder>();
                foreach (var db in dbOrders)
                {
                    var jo = new JobOrder
                    {
                        Id = db.Id,
                        JobNumber = db.JONumber,
                        PINumber = db.PINumber ?? "",
                        CustomerName = db.ClientName ?? "",
                        CustomerTRN = db.ClientTRN ?? "",
                        CustomerAddress = db.ClientAddress ?? "",
                        ProjectName = db.ProjectName ?? "",
                        ProjectLocation = db.ProjectLocation ?? "",
                        LPONumber = db.LPONumber ?? "",
                        Date = db.JODate,
                        RequiredDate = db.RequiredDate,
                        Status = db.Status ?? "Pending",
                        TotalQty = db.TotalQty,
                        ReleasedQty = db.ReleasedQty,
                        BalanceQty = db.BalanceQty,
                        TotalAmount = db.TotalAmount,
                        Notes = db.Notes ?? "",
                        SpecificationsJson = db.SpecificationsJson ?? "",
                        CreatedDate = DateTime.Now
                    };

                    System.Diagnostics.Debug.WriteLine($"[JobOrderListVM] Loaded JO: {jo.JobNumber}, JSON Length: {jo.SpecificationsJson?.Length ?? 0}");
                    orders.Add(jo);
                }

                JobOrders = orders;
                OnPropertyChanged(nameof(FilteredJobOrders));
                System.Diagnostics.Debug.WriteLine($"[JobOrderListVM] Loaded {JobOrders.Count} Job Orders into collection");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobOrderListVM] Error: {ex.Message}");
                MessageBox.Show($"Error loading Job Orders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OpenJobOrder(JobOrder jo)
        {
            if (jo == null) return;
            OpenJobOrderRequested?.Invoke(jo);
        }

        private void OpenProformaInvoice(JobOrder jo)
        {
            if (jo == null || string.IsNullOrWhiteSpace(jo.PINumber))
            {
                MessageBox.Show("No linked Proforma Invoice found.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            OpenProformaInvoiceRequested?.Invoke(jo);
        }

        private void DeleteJobOrder(JobOrder jo)
        {
            if (jo == null) return;

            var result = MessageBox.Show(
                $"Delete Job Order {jo.JobNumber}?\n\nThis cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    DbHelper.DeleteJobOrder(jo.Id);
                    JobOrders.Remove(jo);
                    OnPropertyChanged(nameof(FilteredJobOrders));
                    MessageBox.Show("Job Order deleted.", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}