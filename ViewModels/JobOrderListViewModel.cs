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

        public void LoadJobOrders()
        {
            try
            {
                IsLoading = true;
                var dbOrders = DbHelper.GetAllJobOrders();

                var orders = new ObservableCollection<JobOrder>();
                foreach (var db in dbOrders)
                {
                    var jo = new JobOrder
                    {
                        Id = db.Id,
                        JobNumber = db.JONumber,
                        PINumber = "",
                        CustomerName = db.ClientName,
                        ProjectName = db.ProjectName,
                        ProjectLocation = db.ProjectLocation,
                        Date = db.JODate,
                        RequiredDate = db.RequiredDate,
                        Status = db.Status,
                        TotalQty = db.TotalQty,
                        ReleasedQty = db.ReleasedQty,
                        BalanceQty = db.BalanceQty,
                        TotalAmount = db.TotalAmount,
                        Notes = db.Notes,
                        CreatedDate = DateTime.Now
                    };

                    // Get linked PI number
                    if (db.ProformaInvoiceId > 0)
                    {
                        var allPIs = DbHelper.GetAllProformaInvoices();
                        var pi = allPIs.FirstOrDefault(p => p.Id == db.ProformaInvoiceId);
                        if (pi != null)
                        {
                            jo.PINumber = pi.InvoiceNo;
                        }
                    }

                    orders.Add(jo);
                }

                JobOrders = orders;
                System.Diagnostics.Debug.WriteLine($"[JobOrderListVM] Loaded {orders.Count} job orders");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobOrderListVM] Error: {ex.Message}");
                MessageBox.Show($"Error loading Job Orders: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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