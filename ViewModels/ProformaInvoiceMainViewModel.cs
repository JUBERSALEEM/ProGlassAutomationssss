// File: ViewModels/ProformaInvoiceMainViewModel.cs
using ProGlassAutomation.Models;
using ProGlassAutomation.Services;
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
        private readonly DataService _dataService;
        private bool _isDirty = false;

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<ProformaInvoiceModel> OpenPIEditor;
        public event Action<string> StatusChanged;

        // ==================== CONSTRUCTOR ====================
        public ProformaInvoiceMainViewModel(ProformaInvoiceViewModel editorViewModel)
        {
            _editorViewModel = editorViewModel;
            _dataService = new DataService();

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
            SaveCommand = new RelayCommand(SaveData);

            // Initialize collections
            ProformaInvoices = new ObservableCollection<ProformaInvoiceModel>();
            FilteredProformaInvoices = new ObservableCollection<ProformaInvoiceModel>();
            SalesmanList = new ObservableCollection<SalesmanItem>();

            // Initialize dates
            FromDate = DateTime.Now.AddMonths(-1);
            ToDate = DateTime.Now;

            // PATCH: Subscribe to editor events
            if (_editorViewModel != null)
            {
                _editorViewModel.InvoiceToBeAdded += OnInvoiceToBeAdded;
                // PATCH: Also subscribe to InvoiceSaved event
                _editorViewModel.InvoiceSaved += OnEditorInvoiceSaved;
            }

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
        public int PendingCount => ProformaInvoices.Count(p => p.Status == "Pending" || p.Status == "Sent");

        // ==================== STATES ====================
        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool HasNoRecords => !IsLoading && FilteredProformaInvoices.Count == 0;

        public bool IsDirty
        {
            get => _isDirty;
            set => SetProperty(ref _isDirty, value);
        }

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
        public ICommand SaveCommand { get; }

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
                IsDirty = true;

                UpdateSummaryCounts();
                ApplyFilters();
                OpenPIEditor?.Invoke(newInvoice);
                StatusChanged?.Invoke($"✅ Created new PI: {newInvoice.InvoiceNo}");

                // Auto-save after creating
                SaveData();
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
                IsDirty = true;

                UpdateSummaryCounts();
                ApplyFilters();
                StatusChanged?.Invoke($"🗑️ Deleted PI: {invoice.InvoiceNo}");

                // Save after deleting
                SaveData();
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
            // param is a Tuple<ProformaInvoiceModel, string> containing invoice and new status
            if (param is System.Tuple<ProformaInvoiceModel, string> statusParams)
            {
                ChangeStatusWithParams(statusParams.Item1, statusParams.Item2);
            }
            else if (param is ProformaInvoiceModel invoice)
            {
                // Fallback: if only invoice is passed, use current status (no change)
                return;
            }
        }

        private void ChangeStatusWithParams(ProformaInvoiceModel invoice, string newStatus)
        {
            if (invoice == null) return;
            if (string.IsNullOrEmpty(newStatus)) return;

            var existingInvoice = ProformaInvoices.FirstOrDefault(p => p.InvoiceNo == invoice.InvoiceNo);
            if (existingInvoice != null)
            {
                var oldStatus = existingInvoice.Status;

                // Don't change if same status
                if (oldStatus == newStatus) return;

                existingInvoice.Status = newStatus;
                IsDirty = true;

                UpdateSummaryCounts();
                ApplyFilters();
                StatusChanged?.Invoke($"✅ Status changed from '{oldStatus}' to '{newStatus}' for PI: {existingInvoice.InvoiceNo}");

                // Save after status change
                SaveData();
            }
        }

        private void ExportReport()
        {
            try
            {
                StatusChanged?.Invoke($"📊 Preparing Excel report...");

                if (FilteredProformaInvoices == null || FilteredProformaInvoices.Count == 0)
                {
                    MessageBox.Show("No invoices to export.", "Export",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var filePath = System.IO.Path.Combine(
                    desktopPath,
                    $"ProformaInvoices_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                using (var workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Proforma Invoices");

                    // Header row
                    worksheet.Cell(1, 1).Value = "PI No";
                    worksheet.Cell(1, 2).Value = "Date";
                    worksheet.Cell(1, 3).Value = "Customer";
                    worksheet.Cell(1, 4).Value = "Project";
                    worksheet.Cell(1, 5).Value = "Salesman";
                    worksheet.Cell(1, 6).Value = "Total Amount";
                    worksheet.Cell(1, 7).Value = "Status";
                    worksheet.Cell(1, 8).Value = "JO Ref";

                    // Style header
                    worksheet.Range(1, 1, 1, 8).Style.Font.Bold = true;
                    worksheet.Range(1, 1, 1, 8).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1D4ED8");
                    worksheet.Range(1, 1, 1, 8).Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#FFFFFF");

                    // Data rows
                    int row = 2;
                    foreach (var inv in FilteredProformaInvoices)
                    {
                        worksheet.Cell(row, 1).Value = inv.InvoiceNo;
                        worksheet.Cell(row, 2).Value = inv.InvoiceDate;
                        worksheet.Cell(row, 3).Value = inv.CustomerName;
                        worksheet.Cell(row, 4).Value = inv.ProjectName;
                        worksheet.Cell(row, 5).Value = inv.Salesman;
                        worksheet.Cell(row, 6).Value = inv.GrandTotal;
                        worksheet.Cell(row, 7).Value = inv.Status;
                        worksheet.Cell(row, 8).Value = inv.JobOrderRef;
                        row++;
                    }

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(filePath);
                }

                StatusChanged?.Invoke($"✅ Excel report saved to Desktop");

                // Open the file
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Export failed: {ex.Message}");
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== LOAD DATA ====================
        private void LoadData()
        {
            IsLoading = true;

            try
            {
                // Load from saved file
                var savedInvoices = _dataService.LoadInvoices();

                ProformaInvoices.Clear();

                if (savedInvoices.Count == 0)
                {
                    CreateSampleData();
                }
                else
                {
                    foreach (var invoice in savedInvoices)
                    {
                        // PATCH 10: Rebuild relationships after loading
                        RebuildInvoiceRelationships(invoice);
                        ProformaInvoices.Add(invoice);
                    }
                    StatusChanged?.Invoke($"📂 Loaded {savedInvoices.Count} invoices from storage");
                }

                // Rebuild salesman list
                SalesmanList.Clear();
                SalesmanList.Add(new SalesmanItem { Name = "All Salesmen" });
                foreach (var salesman in ProformaInvoices
                    .Where(p => !string.IsNullOrEmpty(p.Salesman))
                    .Select(p => p.Salesman)
                    .Distinct()
                    .OrderBy(s => s))
                {
                    SalesmanList.Add(new SalesmanItem { Name = salesman });
                }

                UpdateSummaryCounts();
                ApplyFilters();
                IsDirty = false;
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(HasNoRecords));
            }
        }

        // ==================== PATCH 10: REBUILD RELATIONSHIPS ====================
        private void RebuildInvoiceRelationships(ProformaInvoiceModel invoice)
        {
            if (invoice == null) return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"[PIMainViewModel] Rebuilding relationships for {invoice.InvoiceNo}");

                // Rebuild Invoice -> Specification relationships
                foreach (var spec in invoice.Specifications)
                {
                    spec.Invoice = invoice;

                    // Rebuild Specification -> Item relationships
                    foreach (var item in spec.Items)
                    {
                        item.Specification = spec;
                    }

                    // Rebuild OtherCharge relationships
                    foreach (var charge in spec.OtherCharges)
                    {
                        charge.BoundSpecs = invoice.Specifications.ToList();
                    }
                }

                // Recalculate all totals
                invoice.CalculateTotals();

                System.Diagnostics.Debug.WriteLine($"[PIMainViewModel] Relationships rebuilt for {invoice.Specifications.Count} specs");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PIMainViewModel] Rebuild error: {ex.Message}");
            }
        }

        private void CreateSampleData()
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
            sampleInvoice.Specifications.Add(new SpecificationModel
            {
                SpecificationName = "Main Glass",
                Invoice = sampleInvoice
            });
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
            sampleInvoice2.Specifications.Add(new SpecificationModel
            {
                SpecificationName = "Facade Glass",
                Invoice = sampleInvoice2
            });
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
            sampleInvoice3.Specifications.Add(new SpecificationModel
            {
                SpecificationName = "Interior Glass",
                Invoice = sampleInvoice3
            });
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
            sampleInvoice4.Specifications.Add(new SpecificationModel
            {
                SpecificationName = "Lobby Glass",
                Invoice = sampleInvoice4
            });
            ProformaInvoices.Add(sampleInvoice4);

            // Save sample data immediately
            SaveData();
        }

        // ==================== SAVE DATA ====================
        private void SaveData()
        {
            try
            {
                var invoicesList = ProformaInvoices.ToList();
                _dataService.SaveInvoices(invoicesList);
                IsDirty = false;
                StatusChanged?.Invoke("💾 Data saved successfully");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Error saving data: {ex.Message}");
                MessageBox.Show($"Error saving data: {ex.Message}", "Save Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== SAVE ON EXIT ====================
        public void SaveOnExit()
        {
            if (IsDirty)
            {
                try
                {
                    var invoicesList = ProformaInvoices.ToList();
                    _dataService.SaveInvoices(invoicesList);
                    System.Diagnostics.Debug.WriteLine("Data saved on exit");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving on exit: {ex.Message}");
                }
            }
        }

        // ==================== PATCH: HANDLE INVOICE SAVE FROM EDITOR ====================
        public void OnInvoiceToBeAdded(ProformaInvoiceModel invoice)
        {
            if (invoice == null) return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"[PIMainViewModel] OnInvoiceToBeAdded: {invoice.InvoiceNo}");

                // Check if invoice already exists
                var existing = ProformaInvoices.FirstOrDefault(p => p.InvoiceNo == invoice.InvoiceNo);

                if (existing != null)
                {
                    // Update existing - replace with new data
                    var index = ProformaInvoices.IndexOf(existing);
                    ProformaInvoices[index] = invoice;
                    System.Diagnostics.Debug.WriteLine($"[PIMainViewModel] Updated existing invoice: {invoice.InvoiceNo}");
                }
                else
                {
                    // Add new invoice
                    ProformaInvoices.Add(invoice);
                    System.Diagnostics.Debug.WriteLine($"[PIMainViewModel] Added new invoice: {invoice.InvoiceNo}");
                }

                // Mark as dirty and save
                IsDirty = true;
                SaveData();

                // Update UI
                UpdateSummaryCounts();
                ApplyFilters();

                System.Diagnostics.Debug.WriteLine($"[PIMainViewModel] ProformaInvoices count: {ProformaInvoices.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PIMainViewModel] OnInvoiceToBeAdded error: {ex.Message}");
                StatusChanged?.Invoke($"❌ Error saving invoice: {ex.Message}");
            }
        }

        // PATCH: Handle InvoiceSaved event from editor (alternative to InvoiceToBeAdded)
        private void OnEditorInvoiceSaved(ProformaInvoiceModel invoice)
        {
            System.Diagnostics.Debug.WriteLine($"[PIMainViewModel] OnEditorInvoiceSaved: {invoice?.InvoiceNo}");

            if (invoice != null)
            {
                OnInvoiceToBeAdded(invoice);
            }
        }

        // ==================== CLEAR FILTERS ====================
        private void ClearFilters()
        {
            SearchText = "";
            CustomerFilter = "";
            SelectedSalesman = null;
            SelectedStatus = "All Status";
            FromDate = DateTime.Now.AddMonths(-1);
            ToDate = DateTime.Now;
            ApplyFilters();
            StatusChanged?.Invoke("🔄 Filters cleared");
        }

                // ==================== APPLY FILTERS ====================
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

        // ==================== UPDATE SUMMARY COUNTS ====================
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
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
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