using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using ProGlassAutomation.Services;

namespace ProGlassAutomation.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly DataService _dataService = new DataService();

        // ══════════════════════════════════════════════════════════════════════════════
        // SALESMAN DATA
        // ══════════════════════════════════════════════════════════════════════════════
        public string[] SalesmanNames { get; } = new[]
        {
            "Mr Pradeep",
            "Mr Sooraj",
            "Mr Aftab Akram",
            "Ms Maya Ibrahim",
            "Mr Najim",
            "Mr Bilal",
            "Mr Salman (Sunny)",
            "Mr Harvinder",
            "Mr Talha"
        };

        public SKColor[] BarColors { get; } = new[]
        {
            SKColor.Parse("#EC4899"),
            SKColor.Parse("#3B82F6"),
            SKColor.Parse("#10B981"),
            SKColor.Parse("#8B5CF6"),
            SKColor.Parse("#F59E0B"),
            SKColor.Parse("#06B6D4"),
            SKColor.Parse("#EF4444"),
            SKColor.Parse("#84CC16"),
            SKColor.Parse("#F97316")
        };

        // ══════════════════════════════════════════════════════════════════════════════
        // FILTER
        // ══════════════════════════════════════════════════════════════════════════════
        private string _selectedFilter = "This Week";
        public string SelectedFilter
        {
            get => _selectedFilter;
            set { _selectedFilter = value; OnPropertyChanged(); }
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // STATS PROPERTIES
        // ══════════════════════════════════════════════════════════════════════════════
        private string _totalRevenue = "AED 0";
        public string TotalRevenue { get => _totalRevenue; set { _totalRevenue = value; OnPropertyChanged(); } }

        private string _revenueGrowth = "+0% Growth";
        public string RevenueGrowth { get => _revenueGrowth; set { _revenueGrowth = value; OnPropertyChanged(); } }

        private string _orders = "0";
        public string Orders { get => _orders; set { _orders = value; OnPropertyChanged(); } }

        private string _ordersGrowth = "+0% Growth";
        public string OrdersGrowth { get => _ordersGrowth; set { _ordersGrowth = value; OnPropertyChanged(); } }

        private string _salesmen = "9";
        public string Salesmen { get => _salesmen; set { _salesmen = value; OnPropertyChanged(); } }

        private string _pendingQuotes = "0";
        public string PendingQuotes { get => _pendingQuotes; set { _pendingQuotes = value; OnPropertyChanged(); } }

        // ══════════════════════════════════════════════════════════════════════════════
        // SALESMAN DATA PROPERTIES
        // ══════════════════════════════════════════════════════════════════════════════
        public double[] SalesmanSales { get; private set; } = new double[9];
        public string[] SalesmanAmounts { get; } = new string[9];
        public string[] SalesmanDiffs { get; } = new string[9];
        public double[] SalesmanPercentages { get; } = new double[9];

        // ══════════════════════════════════════════════════════════════════════════════
        // MODULE PROPERTIES
        // ══════════════════════════════════════════════════════════════════════════════
        private string _piTotal = "0";
        public string PITotal { get => _piTotal; set { _piTotal = value; OnPropertyChanged(); } }

        private string _piConfirmed = "0";
        public string PIConfirmed { get => _piConfirmed; set { _piConfirmed = value; OnPropertyChanged(); } }

        private string _piPending = "0";
        public string PIPending { get => _piPending; set { _piPending = value; OnPropertyChanged(); } }

        private string _piValue = "AED 0";
        public string PIValue { get => _piValue; set { _piValue = value; OnPropertyChanged(); } }

        private string _joTotal = "0";
        public string JOTotal { get => _joTotal; set { _joTotal = value; OnPropertyChanged(); } }

        private string _joInProgress = "0";
        public string JOInProgress { get => _joInProgress; set { _joInProgress = value; OnPropertyChanged(); } }

        private string _joCompleted = "0";
        public string JOCompleted { get => _joCompleted; set { _joCompleted = value; OnPropertyChanged(); } }

        private string _delPending = "0";
        public string DelPending { get => _delPending; set { _delPending = value; OnPropertyChanged(); } }

        private string _delCompleted = "0";
        public string DelCompleted { get => _delCompleted; set { _delCompleted = value; OnPropertyChanged(); } }

        private string _delTotal = "0";
        public string DelTotal { get => _delTotal; set { _delTotal = value; OnPropertyChanged(); } }

        private string _sheetTotal = "0";
        public string SheetTotal { get => _sheetTotal; set { _sheetTotal = value; OnPropertyChanged(); } }

        private string _sheetAvailable = "0";
        public string SheetAvailable { get => _sheetAvailable; set { _sheetAvailable = value; OnPropertyChanged(); } }

        private string _sheetTypes = "0";
        public string SheetTypes { get => _sheetTypes; set { _sheetTypes = value; OnPropertyChanged(); } }

        // ══════════════════════════════════════════════════════════════════════════════
        // BALANCE PROPERTIES
        // ══════════════════════════════════════════════════════════════════════════════
        private string _dailyBalance = "AED 0";
        public string DailyBalance { get => _dailyBalance; set { _dailyBalance = value; OnPropertyChanged(); } }

        private string _todayRevenue = "0";
        public string TodayRevenue { get => _todayRevenue; set { _todayRevenue = value; OnPropertyChanged(); } }

        private string _todayExpenses = "0";
        public string TodayExpenses { get => _todayExpenses; set { _todayExpenses = value; OnPropertyChanged(); } }

        private string _monthlyBalance = "AED 0";
        public string MonthlyBalance { get => _monthlyBalance; set { _monthlyBalance = value; OnPropertyChanged(); } }

        private string _monthlyChange = "📈 +0%";
        public string MonthlyChange { get => _monthlyChange; set { _monthlyChange = value; OnPropertyChanged(); } }

        private string _annualBalance = "AED 0";
        public string AnnualBalance { get => _annualBalance; set { _annualBalance = value; OnPropertyChanged(); } }

        private string _annualChange = "📈 +0%";
        public string AnnualChange { get => _annualChange; set { _annualChange = value; OnPropertyChanged(); } }

        private string _lastSync = "--:--:--";
        public string LastSync { get => _lastSync; set { _lastSync = value; OnPropertyChanged(); } }

        private string _lastUpdate = "Last update: --:--:--";
        public string LastUpdate { get => _lastUpdate; set { _lastUpdate = value; OnPropertyChanged(); } }

        // ══════════════════════════════════════════════════════════════════════════════
        // CHART PROPERTIES
        // ══════════════════════════════════════════════════════════════════════════════
        public ISeries[] ChartSeries { get; private set; }
        public Axis[] ChartXAxes { get; private set; }
        public Axis[] ChartYAxes { get; private set; }

        // ══════════════════════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════════════════════════════
        public DashboardViewModel()
        {
            InitializeChart();
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // INITIALIZE CHART
        // ══════════════════════════════════════════════════════════════════════════════
        private void InitializeChart()
        {
            ChartSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = new double[] { 0 },
                    Fill = new SolidColorPaint(SKColor.Parse("#2563EB")),
                    MaxBarWidth = 50,
                    Padding = 10,
                    Rx = 8,
                    Ry = 8
                }
            };

            ChartXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = SalesmanNames,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#6B7280")),
                    TextSize = 11
                }
            };

            ChartYAxes = new Axis[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#E5E7EB")),
                    Labeler = value => $"AED {value:N0}",
                    TextSize = 11,
                    MinLimit = 0
                }
            };
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // LOAD DATA
        // ══════════════════════════════════════════════════════════════════════════════
        public void LoadData()
        {
            var invoices = _dataService.LoadInvoices();
            var (start, end) = GetDateRange(SelectedFilter);

            // Filter invoices by date range (InvoiceDate, not CreatedDate)
            var filteredInvoices = invoices.Where(i => i.InvoiceDate >= start && i.InvoiceDate <= end).ToList();

            LoadStats(filteredInvoices);
            LoadSalesmanData(filteredInvoices);
            LoadModuleData(invoices);
            LoadBalanceData(filteredInvoices);
            UpdateChart();
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // LOAD STATS
        // ══════════════════════════════════════════════════════════════════════════════
        private void LoadStats(System.Collections.Generic.List<Models.ProformaInvoiceModel> invoices)
        {
            // Use NetTotal (includes VAT) for revenue
            decimal totalRevenue = (decimal)invoices.Sum(i => i.NetTotal);
            TotalRevenue = $"AED {totalRevenue:N0}";

            decimal previousRevenue = totalRevenue * 0.9m;
            decimal growth = previousRevenue > 0 ? ((totalRevenue - previousRevenue) / previousRevenue) * 100 : 0;
            RevenueGrowth = $"+{growth:F1}% Growth";

            Orders = invoices.Count.ToString("N0");
            OrdersGrowth = "+0% Growth";

            Salesmen = "9";
            PendingQuotes = invoices.Count(i => i.Status == "Pending").ToString();
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // LOAD SALESMAN DATA
        // ══════════════════════════════════════════════════════════════════════════════
        private void LoadSalesmanData(System.Collections.Generic.List<Models.ProformaInvoiceModel> invoices)
        {
            double[] salesData = new double[9];

            for (int i = 0; i < 9; i++)
            {
                // Use Salesman (not SalesmanName) and NetTotal
                var salesmanInvoices = invoices.Where(inv => inv.Salesman == SalesmanNames[i]).ToList();
                salesData[i] = salesmanInvoices.Sum(inv => inv.NetTotal);
            }

            double maxSale = salesData.Length > 0 ? Math.Max(salesData.Max(), 1) : 1;

            for (int i = 0; i < 9; i++)
            {
                double sale = salesData[i];
                SalesmanSales[i] = sale;
                SalesmanAmounts[i] = $"AED {sale:N0}";
                SalesmanPercentages[i] = (sale / maxSale) * 100;

                if (sale > 0)
                {
                    double avg = salesData.Where(s => s > 0).DefaultIfEmpty(0).Average();
                    double diff = avg > 0 ? ((sale - avg) / avg) * 100 : 0;
                    SalesmanDiffs[i] = diff > 0 ? $"+{diff:F0}% above average" :
                                       diff < 0 ? $"{diff:F0}% below average" : "At average";
                }
                else
                {
                    SalesmanDiffs[i] = "No sales yet";
                }
            }

            OnPropertyChanged(nameof(SalesmanAmounts));
            OnPropertyChanged(nameof(SalesmanDiffs));
            OnPropertyChanged(nameof(SalesmanPercentages));
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // UPDATE CHART
        // ══════════════════════════════════════════════════════════════════════════════
        private void UpdateChart()
        {
            ChartSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = SalesmanSales,
                    Fill = new SolidColorPaint(SKColor.Parse("#2563EB")),
                    MaxBarWidth = 50,
                    Padding = 10,
                    Rx = 8,
                    Ry = 8
                }
            };

            OnPropertyChanged(nameof(ChartSeries));
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // LOAD MODULE DATA
        // ══════════════════════════════════════════════════════════════════════════════
        private void LoadModuleData(System.Collections.Generic.List<Models.ProformaInvoiceModel> invoices)
        {
            PITotal = invoices.Count.ToString();
            PIConfirmed = invoices.Count(i => i.Status == "Confirmed").ToString();
            PIPending = invoices.Count(i => i.Status == "Pending").ToString();
            PIValue = $"AED {invoices.Sum(i => i.NetTotal):N0}";

            // Job Orders - update based on your data
            JOTotal = "0";
            JOInProgress = "0";
            JOCompleted = "0";

            // Deliveries - update based on your data
            DelPending = "0";
            DelCompleted = "0";
            DelTotal = "0";

            // Sheet Store - update based on your data
            SheetTotal = "0";
            SheetAvailable = "0";
            SheetTypes = "0";
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // LOAD BALANCE DATA
        // ══════════════════════════════════════════════════════════════════════════════
        private void LoadBalanceData(System.Collections.Generic.List<Models.ProformaInvoiceModel> invoices)
        {
            decimal dailyRev = (decimal)invoices.Sum(i => i.NetTotal);
            decimal dailyExp = 0; // Add expenses tracking
            decimal dailyBal = dailyRev - dailyExp;

            DailyBalance = $"AED {dailyBal:N0}";
            TodayRevenue = dailyRev.ToString("N0");
            TodayExpenses = dailyExp.ToString("N0");

            MonthlyBalance = $"AED {invoices.Sum(i => i.NetTotal):N0}";
            MonthlyChange = "📈 +0%";

            AnnualBalance = $"AED {invoices.Sum(i => i.NetTotal):N0}";
            AnnualChange = "📈 +0%";

            LastSync = DateTime.Now.ToString("HH:mm:ss");
            LastUpdate = $"Last update: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // NAVIGATE
        // ══════════════════════════════════════════════════════════════════════════════
        public void Navigate(string destination)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            if (mainWindow?.DataContext is MainViewModel mainVm)
            {
                switch (destination)
                {
                    case "ProformaInvoice":
                        mainVm.ShowProformaInvoice();
                        break;
                    case "JobOrders":
                        mainVm.ShowJobOrders();
                        break;
                    case "Deliveries":
                        mainVm.ShowDeliveries();
                        break;
                    case "SheetStore":
                        mainVm.ShowSheetStore();
                        break;
                    case "BalanceReports":
                        mainVm.ShowDailyWorks();
                        break;
                    default:
                        mainVm.ShowDashboard();
                        break;
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // SYNC
        // ══════════════════════════════════════════════════════════════════════════════
        public void Sync()
        {
            LoadData();
            LastSync = DateTime.Now.ToString("HH:mm:ss");
            LastUpdate = $"Last update: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // ACTIVATE
        // ══════════════════════════════════════════════════════════════════════════════
        public void Activate()
        {
            System.Windows.MessageBox.Show("Please contact support to activate your license.",
                "Activation", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // GET DATE RANGE
        // ══════════════════════════════════════════════════════════════════════════════
        private (DateTime start, DateTime end) GetDateRange(string filter)
        {
            DateTime now = DateTime.Today;

            return filter switch
            {
                "Today" => (now, now.AddDays(1).AddSeconds(-1)),
                "This Week" => (now.AddDays(-(int)now.DayOfWeek), now.AddDays(7 - (int)now.DayOfWeek).AddSeconds(-1)),
                "This Month" => (new DateTime(now.Year, now.Month, 1), new DateTime(now.Year, now.Month, 1).AddMonths(1).AddSeconds(-1)),
                "This Year" => (new DateTime(now.Year, 1, 1), new DateTime(now.Year, 12, 31, 23, 59, 59)),
                _ => (now.AddDays(-7), now)
            };
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // PROPERTY CHANGED
        // ══════════════════════════════════════════════════════════════════════════════
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}