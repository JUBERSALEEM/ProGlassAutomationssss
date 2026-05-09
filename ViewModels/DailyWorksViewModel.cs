using ProGlassAutomation.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ProGlassAutomation.ViewModels
{
    public class DailyWorksViewModel : ViewModelBase
    {
        private ObservableCollection<DailyWork> _dailyWorks;
        private DailyWork _selectedWork;
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
        private DateTime? _filterStartDate;
        private DateTime? _filterEndDate;
        private bool _isEditing;
        private DailyWork _editingWork;
        private bool _isNewRecord;

        // Options collections
        public ObservableCollection<string> TypeOfWorkOptions { get; private set; }
        public ObservableCollection<string> ProductionStatusOptions { get; private set; }
        public ObservableCollection<string> DailyReportStatusOptions { get; private set; }
        public ObservableCollection<string> StatusOptions { get; private set; }
        public ObservableCollection<string> ColorOptions { get; private set; }
        public ObservableCollection<string> SalesmanOptions { get; private set; }
        public ObservableCollection<string> CompanyOptions { get; private set; }

        public DailyWorksViewModel()
        {
            DailyWorks = new ObservableCollection<DailyWork>();
            InitializeOptions();

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
            PrintCommand = new RelayCommand(ExecutePrint);

            LoadSampleData();
            CreateDataView();
        }

        #region Properties

        public ObservableCollection<DailyWork> DailyWorks
        {
            get => _dailyWorks;
            set => SetProperty(ref _dailyWorks, value);
        }

        public DataView FilteredDataView
        {
            get => _filteredDataView;
            private set => SetProperty(ref _filteredDataView, value);
        }

        public DailyWork SelectedWork
        {
            get => _selectedWork;
            set
            {
                if (SetProperty(ref _selectedWork, value))
                    CommandManager.InvalidateRequerySuggested();
            }
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

        public DailyWork EditingWork
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
        public ICommand PrintCommand { get; }

        #endregion

        #region Initialization

        private void InitializeOptions()
        {
            TypeOfWorkOptions = new ObservableCollection<string>
            {
                "Single Unit (SGU)", "Double Unit (DGU)", "Lamination Unit",
                "Single + Double Unit", "SGU + DGU", "SGU + Lamination",
                "DGU + Lamination", "SGU + DGU + Lamination", "Tempered",
                "Tempered + Lamination", "Other"
            };

            ProductionStatusOptions = new ObservableCollection<string>
            {
                "Sent", "Confirmed", "Prepared", "In Production",
                "Quality Check", "Completed", "Pending"
            };

            DailyReportStatusOptions = new ObservableCollection<string>
            {
                "Not Started", "In Progress", "On Hold", "Completed",
                "Issue Found", "Re-work Required"
            };

            StatusOptions = new ObservableCollection<string>
{
    "Pending", "In Progress", "Confirmed", "Cancelled",
    "Release", "Hold", "Cancel"
};

            ColorOptions = new ObservableCollection<string>
            {
                "Clear", "Green", "Blue", "Grey", "Bronze",
                "Reflective Blue", "Reflective Green", "Reflective Grey",
                "Low-E Clear", "Low-E Blue", "Frosted", "Tinted", "Other"
            };

            SalesmanOptions = new ObservableCollection<string>
            {
                "Ahmed Khan", "Muhammad Ali", "Hassan Ahmed", "Usman Malik",
                "Bilal Shah", "Ali Raza", "Faisal Mahmood", "Imran Hussain"
            };

            CompanyOptions = new ObservableCollection<string>
            {
                "ABC Construction", "XYZ Windows", "Secure Buildings Ltd",
                "Modern Glass Works", "Elite Glazing Co", "Premium Windows Inc"
            };
        }

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

            FilteredDataView = dataTable.DefaultView;
        }

        private void RefreshDataView()
        {
            if (FilteredDataView == null) return;
            var currentSort = FilteredDataView.Sort;
            var currentFilter = FilteredDataView.RowFilter;
            CreateDataView();
            if (!string.IsNullOrEmpty(currentSort)) FilteredDataView.Sort = currentSort;
            if (!string.IsNullOrEmpty(currentFilter)) FilteredDataView.RowFilter = currentFilter;
        }

        #endregion

        #region Load Sample Data

        private void LoadSampleData()
        {
            var random = new Random();
            var companies = new[] { "ABC Construction", "XYZ Windows", "Secure Buildings Ltd", "Modern Glass Works", "Elite Glazing Co" };
            var workTypes = new[] { "Single Unit (SGU)", "Double Unit (DGU)", "Lamination Unit", "Single + Double Unit", "SGU + DGU" };
            var salesmen = new[] { "Ahmed Khan", "Muhammad Ali", "Hassan Ahmed", "Usman Malik", "Bilal Shah" };
            var customerRefs = new[] { "CUST-001", "CUST-002", "CUST-003", "CUST-004", "CUST-005" };

            for (int i = 1; i <= 20; i++)
            {
                var qty = random.Next(20, 100) * 2;
                var statuses = new[] { "Pending", "In Progress", "Confirmed", "Cancelled" };
                var status = statuses[random.Next(statuses.Length)];
                var productionStatus = status == "Confirmed" ? "Completed" : status == "In Progress" ? "In Progress" : "Pending";

                var order = new DailyWork
                {
                    Id = i,
                    Date = DateTime.Today.AddDays(-random.Next(1, 15)),
                    Company = companies[random.Next(companies.Length)],
                    PINumber = $"PI-{DateTime.Now.Year}-{1000 + i}",
                    CustomerReference = customerRefs[random.Next(customerRefs.Length)],
                    TypeOfWork = workTypes[random.Next(workTypes.Length)],
                    Qty = qty,
                    SQM = Math.Round(random.Next(50, 500) * 0.1, 2),
                    Salesman = salesmen[random.Next(salesmen.Length)],
                    Status = status,
                    ProductionStatus = productionStatus,
                    Color = "",
                    Notes = i % 4 == 0 ? $"Order {i} notes" : "",
                    CreatedDate = DateTime.Today.AddDays(-random.Next(1, 15))
                };
                DailyWorks.Add(order);
            }
            UpdateStatistics();
        }

        #endregion

        #region Filter Implementation

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
                filterExpressions.Add($"Status = '{FilterStatus}'");

            if (!string.IsNullOrWhiteSpace(FilterProductionStatus))
                filterExpressions.Add($"ProductionStatus = '{FilterProductionStatus}'");

            if (!string.IsNullOrWhiteSpace(FilterTypeOfWork))
                filterExpressions.Add($"TypeOfWork = '{FilterTypeOfWork}'");

            if (!string.IsNullOrWhiteSpace(FilterSalesman))
                filterExpressions.Add($"Salesman = '{FilterSalesman}'");

            if (!string.IsNullOrWhiteSpace(FilterCompany))
                filterExpressions.Add($"Company = '{FilterCompany}'");

            if (!string.IsNullOrWhiteSpace(FilterColor))
                filterExpressions.Add($"Color = '{FilterColor}'");

            if (FilterStartDate.HasValue)
                filterExpressions.Add($"Date >= #{FilterStartDate.Value:yyyy-MM-dd}#");

            if (FilterEndDate.HasValue)
                filterExpressions.Add($"Date <= #{FilterEndDate.Value:yyyy-MM-dd}#");

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
            EditingWork = new DailyWork
            {
                Id = 0,
                Date = DateTime.Today,
                UpdateDate = DateTime.Today,
                Status = "Release",
                ProductionStatus = "Sent",
                DailyReportStatus = "Not Started",
                TypeOfWork = "Single Unit (SGU)",
                Color = "Clear",
                Qty = 0,
                SQM = 0
            };
            IsEditing = true;
        }

        private void ExecuteEdit(object parameter)
        {
            DailyWork workToEdit = null;

            if (SelectedDataRowView != null)
            {
                var dataRow = SelectedDataRowView;
                workToEdit = new DailyWork
                {
                    Id = Convert.ToInt32(dataRow["Id"]),
                    Date = Convert.ToDateTime(dataRow["Date"]),
                    UpdateDate = Convert.ToDateTime(dataRow["UpdateDate"]),
                    Company = dataRow["Company"].ToString(),
                    PINumber = dataRow["PINumber"].ToString(),
                    CustomerReference = dataRow["CustomerReference"].ToString(),
                    TypeOfWork = dataRow["TypeOfWork"].ToString(),
                    ProductionStatus = dataRow["ProductionStatus"].ToString(),
                    DailyReportStatus = dataRow["DailyReportStatus"].ToString(),
                    Qty = Convert.ToInt32(dataRow["Qty"]),
                    SQM = Convert.ToDouble(dataRow["SQM"]),
                    Status = dataRow["Status"].ToString(),
                    Salesman = dataRow["Salesman"].ToString(),
                    Color = dataRow["Color"].ToString(),
                    Notes = dataRow["Notes"].ToString()
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
            }
        }

        private bool CanExecuteEdit(object parameter)
        {
            return parameter != null || SelectedDataRowView != null || SelectedWork != null;
        }

        private void ExecuteDelete(object parameter)
        {
            DailyWork workToDelete = null;

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
                    DailyWorks.Remove(workToDelete);
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

            if (_isNewRecord)
            {
                EditingWork.Id = DailyWorks.Count > 0 ? DailyWorks.Max(w => w.Id) + 1 : 1;
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
                }
            }

            IsEditing = false;
            EditingWork = null;
            RefreshDataView();
            UpdateStatistics();
        }

        private bool CanExecuteSave(object parameter) => EditingWork != null;

        private void ExecuteCancel(object parameter)
        {
            IsEditing = false;
            EditingWork = null;
        }

        private void ExecuteRefresh(object parameter)
        {
            RefreshDataView();
            UpdateStatistics();
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

        private void ExportToCSV(string filePath)
        {
            var sb = new System.Text.StringBuilder();
            var headers = new[] { "Date", "Update Date", "Company", "PI Number", "Customer Ref", "Type of Work", "Production Status", "Daily Report Status", "Qty", "SQM", "Status", "Salesman", "Color", "Notes" };
            sb.AppendLine(string.Join(",", headers));

            foreach (DataRowView rowView in FilteredDataView)
            {
                var fields = rowView.Row.ItemArray.Select(f => $"\"{f?.ToString()?.Replace("\"", "\"\"")}\"");
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

        private void ExecuteCopyRow(object parameter)
        {
            DailyWork sourceWork = null;

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
                copy.PINumber = $"COPY_{copy.PINumber}";
                copy.Date = DateTime.Today;
                copy.UpdateDate = DateTime.Today;
                DailyWorks.Add(copy);
                RefreshDataView();
                UpdateStatistics();
            }
        }

        private bool CanExecuteCopyRow(object parameter)
        {
            return SelectedDataRowView != null || SelectedWork != null;
        }

        private void ExecuteDuplicateRow(object parameter)
        {
            DailyWork sourceWork = null;

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
                duplicate.PINumber = $"DUP_{duplicate.PINumber}";
                duplicate.Date = DateTime.Today;
                duplicate.UpdateDate = DateTime.Today;
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

        private Grid CreatePrintVisual()
        {
            var grid = new Grid { Margin = new Thickness(20) };

            // Header
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
                Text = $"Records: {FilteredRecords} | Total Qty: {FilteredQty} | Total SQM: {FilteredSQM:N2}",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0)
            });
            grid.Children.Add(headerPanel);

            // DataGrid for printing
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

            grid.Children.Add(dataGrid);

            return grid;
        }

        #endregion
    }
}