using ProGlassAutomation.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ProGlassAutomation.ViewModels
{
    public class ProformaInvoiceMainViewModel : INotifyPropertyChanged
    {
        private readonly ProformaInvoiceViewModel _editorViewModel;

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<ProformaInvoiceModel> OpenPIEditor;
        public event Action<string> StatusChanged;

        // ==================== CONSTRUCTOR ====================
        public ProformaInvoiceMainViewModel(ProformaInvoiceViewModel editorViewModel)
        {
            _editorViewModel = editorViewModel;

            // Initialize commands
            CreateNewPICommand = new RelayCommand(CreateNewPI);
            ViewPICommand = new RelayCommand(ViewPI);
            EditPICommand = new RelayCommand(EditPI);
            DeletePICommand = new RelayCommand(DeletePI);
            ConvertToJOCommand = new RelayCommand(ConvertToJO);
            RefreshCommand = new RelayCommand(LoadData);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ExportReportCommand = new RelayCommand(ExportReport);
            ChangeStatusCommand = new RelayCommand(ChangeStatus);

            // Initialize collections
            ProformaInvoices = new ObservableCollection<ProformaInvoiceModel>();
            FilteredProformaInvoices = new ObservableCollection<ProformaInvoiceModel>();
            SalesmanList = new ObservableCollection<SalesmanItem>();

            // Initialize dates
            FromDate = DateTime.Now.AddMonths(-1);
            ToDate = DateTime.Now;

            // Load data
            LoadData();
        }

        // ==================== COLLECTIONS ====================
        private ObservableCollection<ProformaInvoiceModel> _proformaInvoices;
        public ObservableCollection<ProformaInvoiceModel> ProformaInvoices
        {
            get => _proformaInvoices;
            set => SetProperty(ref _proformaInvoices, value);
        }

        private ObservableCollection<ProformaInvoiceModel> _filteredProformaInvoices;
        public ObservableCollection<ProformaInvoiceModel> FilteredProformaInvoices
        {
            get => _filteredProformaInvoices;
            set => SetProperty(ref _filteredProformaInvoices, value);
        }

        private ObservableCollection<SalesmanItem> _salesmanList;
        public ObservableCollection<SalesmanItem> SalesmanList
        {
            get => _salesmanList;
            set => SetProperty(ref _salesmanList, value);
        }

        // ==================== STATUS LIST ====================
        public List<string> StatusList { get; } = new List<string>
        {
            "Draft", "Sent", "Pending", "Hold", "Confirmed", "Revised",
            "In Progress", "Converted To JO", "Partial Delivered", "Delivered",
            "Invoiced", "Completed", "Cancelled", "Voided"
        };

        // ==================== SELECTED ITEM ====================
        private ProformaInvoiceModel _selectedInvoice;
        public ProformaInvoiceModel SelectedInvoice
        {
            get => _selectedInvoice;
            set => SetProperty(ref _selectedInvoice, value);
        }

        // ==================== FILTERS ====================
        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    ApplyFilters();
            }
        }

        private string _customerFilter = "";
        public string CustomerFilter
        {
            get => _customerFilter;
            set
            {
                if (SetProperty(ref _customerFilter, value))
                    ApplyFilters();
            }
        }

        private SalesmanItem _selectedSalesman;
        public SalesmanItem SelectedSalesman
        {
            get => _selectedSalesman;
            set
            {
                if (SetProperty(ref _selectedSalesman, value))
                    ApplyFilters();
            }
        }

        private string _selectedStatus = "All Status";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetProperty(ref _selectedStatus, value))
                    ApplyFilters();
            }
        }

        private DateTime? _fromDate;
        public DateTime? FromDate
        {
            get => _fromDate;
            set
            {
                if (SetProperty(ref _fromDate, value))
                    ApplyFilters();
            }
        }

        private DateTime? _toDate;
        public DateTime? ToDate
        {
            get => _toDate;
            set
            {
                if (SetProperty(ref _toDate, value))
                    ApplyFilters();
            }
        }

        // ==================== SUMMARY COUNTS ====================
        public int TotalPICount => ProformaInvoices.Count;
        public int ConfirmedCount => ProformaInvoices.Count(p => p.Status == "Confirmed");
        public int ConvertedCount => ProformaInvoices.Count(p => p.Status == "Converted To JO");
        public int PendingCount => ProformaInvoices.Count(p => p.Status == "Confirmed" && string.IsNullOrEmpty(p.JobOrderRef));

        // ==================== STATES ====================
        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool HasNoRecords => !IsLoading && FilteredProformaInvoices.Count == 0;

        // ==================== COMMANDS ====================
        public ICommand CreateNewPICommand { get; }
        public ICommand ViewPICommand { get; }
        public ICommand EditPICommand { get; }
        public ICommand DeletePICommand { get; }
        public ICommand ConvertToJOCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand ExportReportCommand { get; }
        public ICommand ChangeStatusCommand { get; }

        // ==================== COMMAND IMPLEMENTATIONS ====================
        private void CreateNewPI()
        {
            try
            {
                var newInvoice = new ProformaInvoiceModel
                {
                    InvoiceNo = ProformaInvoiceModel.GenerateInvoiceNo(),
                    InvoiceDate = DateTime.Now,
                    ValidUntil = DateTime.Now.AddDays(30),
                    Status = "Draft"
                };

                newInvoice.Specifications.Add(new SpecificationModel
                {
                    SpecificationName = "Specification 1",
                    Invoice = newInvoice
                });

                ProformaInvoices.Add(newInvoice);
                UpdateSummaryCounts();
                ApplyFilters();
                OpenPIEditor?.Invoke(newInvoice);
                StatusChanged?.Invoke($"✅ Created new PI: {newInvoice.InvoiceNo}");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Error creating PI: {ex.Message}");
                MessageBox.Show($"Error creating PI: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewPI(object param)
        {
            if (param is not ProformaInvoiceModel invoice) return;
            OpenPIEditor?.Invoke(invoice);
            StatusChanged?.Invoke($"📂 Viewing PI: {invoice.InvoiceNo}");
        }

        private void EditPI(object param)
        {
            if (param is not ProformaInvoiceModel invoice) return;
            OpenPIEditor?.Invoke(invoice);
            StatusChanged?.Invoke($"✏️ Editing PI: {invoice.InvoiceNo}");
        }

        private void DeletePI(object param)
        {
            if (param is not ProformaInvoiceModel invoice) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete PI '{invoice.InvoiceNo}'?\n\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                ProformaInvoices.Remove(invoice);
                UpdateSummaryCounts();
                ApplyFilters();
                StatusChanged?.Invoke($"🗑️ Deleted PI: {invoice.InvoiceNo}");
            }
            else
            {
                StatusChanged?.Invoke($"❌ Delete cancelled for PI: {invoice.InvoiceNo}");
            }
        }

        private void ConvertToJO(object param)
        {
            if (param is not ProformaInvoiceModel invoice) return;
            StatusChanged?.Invoke($"🔄 Converting PI to JO: {invoice.InvoiceNo}");
        }

        private void ChangeStatus(object param)
        {
            var invoice = SelectedInvoice;
            if (invoice == null) return;

            if (param is string newStatus && !string.IsNullOrEmpty(newStatus))
            {
                var existingInvoice = ProformaInvoices.FirstOrDefault(p => p.InvoiceNo == invoice.InvoiceNo);
                if (existingInvoice != null)
                {
                    var oldStatus = existingInvoice.Status;
                    existingInvoice.Status = newStatus;
                    UpdateSummaryCounts();
                    ApplyFilters();
                    StatusChanged?.Invoke($"✅ Status changed from '{oldStatus}' to '{newStatus}' for PI: {existingInvoice.InvoiceNo}");
                }
            }
        }

        private void ExportReport()
        {
            StatusChanged?.Invoke($"📊 Exporting report...");
        }

        private void LoadData()
        {
            IsLoading = true;

            try
            {
                if (ProformaInvoices.Count == 0)
                {
                    var sampleInvoice = new ProformaInvoiceModel
                    {
                        InvoiceNo = "PI-2026-0012",
                        CustomerName = "Al Noor Glass",
                        ProjectName = "Dubai Marina Tower",
                        Salesman = "Ahmed",
                        GrandTotal = 14250.00,
                        Status = "Confirmed",
                        InvoiceDate = DateTime.Now.AddDays(-5),
                        JobOrderRef = ""
                    };
                    sampleInvoice.Specifications.Add(new SpecificationModel { SpecificationName = "Main Glass", Invoice = sampleInvoice });
                    ProformaInvoices.Add(sampleInvoice);

                    var sampleInvoice2 = new ProformaInvoiceModel
                    {
                        InvoiceNo = "PI-2026-0013",
                        CustomerName = "Modern Facade LLC",
                        ProjectName = "Business Bay Office",
                        Salesman = "John",
                        GrandTotal = 22800.00,
                        Status = "Revised",
                        InvoiceDate = DateTime.Now.AddDays(-10),
                        JobOrderRef = "JO-2026-0041"
                    };
                    sampleInvoice2.Specifications.Add(new SpecificationModel { SpecificationName = "Facade Glass", Invoice = sampleInvoice2 });
                    ProformaInvoices.Add(sampleInvoice2);

                    var sampleInvoice3 = new ProformaInvoiceModel
                    {
                        InvoiceNo = "PI-2026-0014",
                        CustomerName = "Skyline Interiors",
                        ProjectName = "Villa Partition Work",
                        Salesman = "David",
                        GrandTotal = 8750.00,
                        Status = "Hold",
                        InvoiceDate = DateTime.Now.AddDays(-15),
                        JobOrderRef = ""
                    };
                    sampleInvoice3.Specifications.Add(new SpecificationModel { SpecificationName = "Interior Glass", Invoice = sampleInvoice3 });
                    ProformaInvoices.Add(sampleInvoice3);

                    var sampleInvoice4 = new ProformaInvoiceModel
                    {
                        InvoiceNo = "PI-2026-0015",
                        CustomerName = "Elite Glazing",
                        ProjectName = "Hotel Lobby Glass",
                        Salesman = "Ahmed",
                        GrandTotal = 18950.00,
                        Status = "Sent",
                        InvoiceDate = DateTime.Now.AddDays(-20),
                        JobOrderRef = ""
                    };
                    sampleInvoice4.Specifications.Add(new SpecificationModel { SpecificationName = "Lobby Glass", Invoice = sampleInvoice4 });
                    ProformaInvoices.Add(sampleInvoice4);
                }

                SalesmanList.Clear();
                SalesmanList.Add(new SalesmanItem { Name = "All Salesmen" });
                foreach (var salesman in ProformaInvoices.Where(p => !string.IsNullOrEmpty(p.Salesman)).Select(p => p.Salesman).Distinct().OrderBy(s => s))
                {
                    SalesmanList.Add(new SalesmanItem { Name = salesman });
                }

                UpdateSummaryCounts();
                ApplyFilters();
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(HasNoRecords));
            }
        }

        private void ClearFilters()
        {
            SearchText = "";
            CustomerFilter = "";
            SelectedSalesman = null;
            SelectedStatus = "All Status";
            FromDate = DateTime.Now.AddMonths(-1);
            ToDate = DateTime.Now;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var filtered = ProformaInvoices.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                filtered = filtered.Where(p =>
                    (p.InvoiceNo?.ToLower().Contains(search) ?? false) ||
                    (p.CustomerName?.ToLower().Contains(search) ?? false) ||
                    (p.ProjectName?.ToLower().Contains(search) ?? false) ||
                    (p.Salesman?.ToLower().Contains(search) ?? false));
            }

            if (!string.IsNullOrWhiteSpace(CustomerFilter))
            {
                var search = CustomerFilter.ToLower();
                filtered = filtered.Where(p => (p.CustomerName?.ToLower().Contains(search) ?? false));
            }

            if (SelectedSalesman != null && !string.IsNullOrEmpty(SelectedSalesman.Name) && SelectedSalesman.Name != "All Salesmen")
            {
                filtered = filtered.Where(p => p.Salesman == SelectedSalesman.Name);
            }

            if (!string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != "All Status")
            {
                filtered = filtered.Where(p => p.Status == SelectedStatus);
            }

            if (FromDate.HasValue)
            {
                filtered = filtered.Where(p => p.InvoiceDate.Date >= FromDate.Value.Date);
            }

            if (ToDate.HasValue)
            {
                filtered = filtered.Where(p => p.InvoiceDate.Date <= ToDate.Value.Date);
            }

            FilteredProformaInvoices = new ObservableCollection<ProformaInvoiceModel>(filtered);
            OnPropertyChanged(nameof(HasNoRecords));
        }

        private void UpdateSummaryCounts()
        {
            OnPropertyChanged(nameof(TotalPICount));
            OnPropertyChanged(nameof(ConfirmedCount));
            OnPropertyChanged(nameof(ConvertedCount));
            OnPropertyChanged(nameof(PendingCount));
        }

        // ==================== HELPERS ====================
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    // ==================== SUPPORT CLASSES ====================
    public class SalesmanItem
    {
        public string Name { get; set; } = "";
    }
}