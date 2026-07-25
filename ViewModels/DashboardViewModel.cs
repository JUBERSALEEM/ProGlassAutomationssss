using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using ProGlassAutomation.Data.Database;
using ProGlassAutomation.ViewModels.Dashboard.Models;

namespace ProGlassAutomation.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        // KPI Cards
        public KPICard ProformaCard { get; set; } = new KPICard
        {
            Title = "Proforma Invoices",
            Icon = "📄",
            ColorKey = "Blue",
            ChangePercent = 12.4,
            ChangeLabel = "vs last month",
            Value = 248
        };

        public KPICard JobOrdersCard { get; set; } = new KPICard
        {
            Title = "Job Orders",
            Icon = "📋",
            ColorKey = "Green",
            ChangePercent = 8.2,
            ChangeLabel = "vs last month",
            Value = 187
        };

        public KPICard OptimizationCard { get; set; } = new KPICard
        {
            Title = "Avg Optimization",
            Icon = "⚡",
            ColorKey = "Purple",
            Suffix = "%",
            ChangePercent = 3.1,
            ChangeLabel = "vs last month",
            Value = 87.3
        };

        public KPICard DailyOutputCard { get; set; } = new KPICard
        {
            Title = "Daily Output (sqm)",
            Icon = "🏭",
            ColorKey = "Amber",
            ChangePercent = -2.1,
            ChangeLabel = "vs yesterday",
            Value = 1245
        };

        public KPICard DeliveriesCard { get; set; } = new KPICard
        {
            Title = "Deliveries Today",
            Icon = "🚚",
            ColorKey = "Rose",
            ChangePercent = 6.3,
            ChangeLabel = "vs yesterday",
            Value = 34
        };

        public KPICard InventoryCard { get; set; } = new KPICard
        {
            Title = "Sheet Inventory",
            Icon = "📦",
            ColorKey = "Cyan",
            ChangePercent = -4.5,
            ChangeLabel = "vs last week",
            Value = 4850
        };

        // Daily Works Data
        public ObservableCollection<DailyWorkRecord> DailyWorkRecords { get; set; } = new ObservableCollection<DailyWorkRecord>();

        private int _dailyWorksCount;
        public int DailyWorksCount
        {
            get => _dailyWorksCount;
            set { _dailyWorksCount = value; OnPropertyChanged(); }
        }

        // Delivery Data
        public ObservableCollection<DeliveryRecord> DeliveryRecords { get; set; } = new ObservableCollection<DeliveryRecord>();

        private int _deliveryCount;
        public int DeliveryCount
        {
            get => _deliveryCount;
            set { _deliveryCount = value; OnPropertyChanged(); }
        }

        private int _deliveryTotalQty;
        public int DeliveryTotalQty
        {
            get => _deliveryTotalQty;
            set { _deliveryTotalQty = value; OnPropertyChanged(); }
        }

        private int _deliveryTotalDelivered;
        public int DeliveryTotalDelivered
        {
            get => _deliveryTotalDelivered;
            set { _deliveryTotalDelivered = value; OnPropertyChanged(); }
        }

        private int _deliveryTotalReturned;
        public int DeliveryTotalReturned
        {
            get => _deliveryTotalReturned;
            set { _deliveryTotalReturned = value; OnPropertyChanged(); }
        }

        private int _deliveryTotalBalance;
        public int DeliveryTotalBalance
        {
            get => _deliveryTotalBalance;
            set { _deliveryTotalBalance = value; OnPropertyChanged(); }
        }

        public double DeliveryProgressPercent => DeliveryTotalQty > 0 ? (double)DeliveryTotalDelivered / DeliveryTotalQty * 100 : 0;

        // Legacy String Properties for Binding
        public string DelPending { get; set; } = "0";
        public string DelCompleted { get; set; } = "0";
        public string DelTotal { get; set; } = "0";

        // Charts
        public ISeries[] DailyTrendSeries { get; set; }
        public Axis[] DailyTrendXAxes { get; set; }
        public Axis[] DailyTrendYAxes { get; set; }

        public ISeries[] SalesmanChartSeries { get; set; }
        public Axis[] SalesmanChartXAxes { get; set; }
        public Axis[] SalesmanChartYAxes { get; set; }

        public ISeries[] ProductMixSeries { get; set; }
        public ISeries[] MachineUtilSeries { get; set; }
        public Axis[] MachineUtilXAxes { get; set; }
        public Axis[] MachineUtilYAxes { get; set; }
        public ISeries[] DeliveryStatusSeries { get; set; }
        public ISeries[] OptimizationSeries { get; set; }
        public Axis[] OptimizationXAxes { get; set; }
        public Axis[] OptimizationYAxes { get; set; }

        // Other Properties
        public string LastUpdate { get; set; } = "Last update: Just now";
        public string SelectedFilter { get; set; } = "This Week";
        public string DayFilterBg { get; set; } = "#F3F4F6";
        public string WeekFilterBg { get; set; } = "#2563EB";
        public string MonthFilterBg { get; set; } = "#F3F4F6";
        public string YearFilterBg { get; set; } = "#F3F4F6";
        public string DayFilterFg { get; set; } = "#1F2937";
        public string WeekFilterFg { get; set; } = "#FFFFFF";
        public string MonthFilterFg { get; set; } = "#1F2937";
        public string YearFilterFg { get; set; } = "#1F2937";

        // Legacy Stats
        public string TotalRevenue { get; set; } = "AED 2,450,000";
        public string RevenueGrowth { get; set; } = "+12.5%";
        public string Orders { get; set; } = "156";
        public string OrdersGrowth { get; set; } = "+8.2%";
        public string Salesmen { get; set; } = "9";
        public string PendingQuotes { get; set; } = "23";
        public string PITotal { get; set; } = "248";
        public string PIConfirmed { get; set; } = "198";
        public string PIPending { get; set; } = "50";
        public string PIValue { get; set; } = "AED 1,240,000";
        public string JOTotal { get; set; } = "187";
        public string JOInProgress { get; set; } = "45";
        public string JOCompleted { get; set; } = "142";
        public string DailyBalance { get; set; } = "AED 45,200";
        public string MonthlyBalance { get; set; } = "AED 1,240,000";
        public string AnnualBalance { get; set; } = "AED 14,850,000";

        public ObservableCollection<SalesmanData> SalesmanList { get; set; } = new ObservableCollection<SalesmanData>();

        public ICommand RefreshCommand { get; }
        public ICommand DayFilterCommand { get; }
        public ICommand WeekFilterCommand { get; }
        public ICommand MonthFilterCommand { get; }
        public ICommand YearFilterCommand { get; }

        public DashboardViewModel()
        {
            RefreshCommand = new RelayCommand(_ => LoadData());
            DayFilterCommand = new RelayCommand(_ => ApplyFilter("Today"));
            WeekFilterCommand = new RelayCommand(_ => ApplyFilter("This Week"));
            MonthFilterCommand = new RelayCommand(_ => ApplyFilter("This Month"));
            YearFilterCommand = new RelayCommand(_ => ApplyFilter("This Year"));

            InitializeSalesmen();
            InitializeCharts();
            LoadData();
        }

        private void InitializeSalesmen()
        {
            var names = new[] { "Mr Pradeep", "Mr Sooraj", "Ms Maya", "Mr Aftab", "Mr Talha", "Mr Bilal", "Mr Sunny", "Mr Harvinder", "Mr Nazim" };
            foreach (var name in names)
                SalesmanList.Add(new SalesmanData { Name = name, Amount = "AED 0" });
        }

        private void InitializeCharts()
        {
            // Daily Trend
            DailyTrendSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Name = "SQM",
                    Values = new double[] { 1100, 1150, 1200, 1180, 1250, 1220, 1280 },
                    Fill = new SolidColorPaint(new SKColor(59, 130, 246, 30)),
                    Stroke = new SolidColorPaint(new SKColor(59, 130, 246), 3),
                    GeometrySize = 6
                },
                new LineSeries<double>
                {
                    Name = "Sheets",
                    Values = new double[] { 200, 210, 205, 220, 215, 230, 225 },
                    Stroke = new SolidColorPaint(new SKColor(16, 185, 129), 2),
                    GeometrySize = 4,
                    ScalesYAt = 1
                }
            };
            DailyTrendXAxes = new Axis[] { new Axis { LabelsPaint = new SolidColorPaint(SKColors.Gray) } };
            DailyTrendYAxes = new Axis[]
            {
                new Axis { LabelsPaint = new SolidColorPaint(SKColors.Gray) },
                new Axis { LabelsPaint = new SolidColorPaint(SKColors.Gray), Position = LiveChartsCore.Measure.AxisPosition.End }
            };

            // Salesman Chart
            SalesmanChartSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Name = "Revenue",
                    Values = new double[] { 120, 145, 98, 167, 134, 156, 189, 143, 112 },
                    Fill = new SolidColorPaint(new SKColor(139, 92, 246)),
                    MaxBarWidth = 30
                }
            };
            SalesmanChartXAxes = new Axis[]
            {
                new Axis { Labels = new[] { "Pradeep", "Sooraj", "Maya", "Aftab", "Talha", "Bilal", "Sunny", "Harvi", "Nazim" }, LabelsPaint = new SolidColorPaint(SKColors.Gray) }
            };
            SalesmanChartYAxes = new Axis[] { new Axis { LabelsPaint = new SolidColorPaint(SKColors.Gray) } };

            // Product Mix
            ProductMixSeries = new ISeries[]
            {
                new PieSeries<double> { Name = "Tempered", Values = new[] { 35.0 }, Fill = new SolidColorPaint(new SKColor(59, 130, 246)), InnerRadius = 60 },
                new PieSeries<double> { Name = "Laminated", Values = new[] { 25.0 }, Fill = new SolidColorPaint(new SKColor(16, 185, 129)), InnerRadius = 60 },
                new PieSeries<double> { Name = "Insulated", Values = new[] { 20.0 }, Fill = new SolidColorPaint(new SKColor(139, 92, 246)), InnerRadius = 60 },
                new PieSeries<double> { Name = "Curved", Values = new[] { 12.0 }, Fill = new SolidColorPaint(new SKColor(245, 158, 11)), InnerRadius = 60 },
                new PieSeries<double> { Name = "Decorative", Values = new[] { 8.0 }, Fill = new SolidColorPaint(new SKColor(239, 68, 68)), InnerRadius = 60 }
            };

            // Machine Util
            MachineUtilSeries = new ISeries[]
            {
                new RowSeries<int>
                {
                    Values = new[] { 87, 74, 92, 68, 81, 55 },
                    Fill = new SolidColorPaint(new SKColor(139, 92, 246)),
                    MaxBarWidth = 20
                }
            };
            MachineUtilXAxes = new Axis[] { new Axis { MaxLimit = 100, LabelsPaint = new SolidColorPaint(SKColors.Gray) } };
            MachineUtilYAxes = new Axis[]
            {
                new Axis { Labels = new[] { "CNC-01", "CNC-02", "Temper-01", "Lam-01", "IG-01", "Polish-01" }, LabelsPaint = new SolidColorPaint(SKColors.Gray) }
            };

            // Delivery Status
            DeliveryStatusSeries = new ISeries[]
            {
                new PieSeries<double> { Name = "Delivered", Values = new[] { 52.0 }, Fill = new SolidColorPaint(new SKColor(16, 185, 129)), InnerRadius = 60 },
                new PieSeries<double> { Name = "In Transit", Values = new[] { 15.0 }, Fill = new SolidColorPaint(new SKColor(59, 130, 246)), InnerRadius = 60 },
                new PieSeries<double> { Name = "Scheduled", Values = new[] { 25.0 }, Fill = new SolidColorPaint(new SKColor(245, 158, 11)), InnerRadius = 60 },
                new PieSeries<double> { Name = "Delayed", Values = new[] { 8.0 }, Fill = new SolidColorPaint(new SKColor(239, 68, 68)), InnerRadius = 60 }
            };

            // Optimization
            OptimizationSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Name = "Efficiency %",
                    Values = new double[] { 82, 85, 84, 88, 87, 89, 86, 90 },
                    Stroke = new SolidColorPaint(new SKColor(139, 92, 246), 3),
                    Fill = null
                }
            };
            OptimizationXAxes = new Axis[] { new Axis { LabelsPaint = new SolidColorPaint(SKColors.Gray) } };
            OptimizationYAxes = new Axis[] { new Axis { MinLimit = 65, MaxLimit = 100, LabelsPaint = new SolidColorPaint(SKColors.Gray) } };
        }

        public void LoadData()
        {
            try
            {
                // Load DailyWorks
                var dbWorks = DbHelper.GetAllDailyWork();
                DailyWorkRecords.Clear();
                foreach (var w in dbWorks)
                {
                    DailyWorkRecords.Add(new DailyWorkRecord
                    {
                        Id = w.Id,
                        Date = w.Date,
                        Company = w.Company,
                        PiNumber = w.PINumber,
                        Qty = w.Qty,
                        Sqm = w.SQM,
                        Status = w.Status
                    });
                }
                DailyWorksCount = DailyWorkRecords.Count;

                // Load Deliveries with Items
                var dbDeliveries = DbHelper.GetAllDeliveries();
                DeliveryRecords.Clear();

                int totalQty = 0, totalDelivered = 0, totalReturned = 0, totalBalance = 0;

                foreach (var d in dbDeliveries)
                {
                    // Load items if not already loaded
                    if (d.DeliveryItems == null || d.DeliveryItems.Count == 0)
                    {
                        var items = DbHelper.GetDeliveryItems(d.Id);
                        d.DeliveryItems = new ObservableCollection<DeliveryItem>(items);
                    }

                    var delivered = d.DeliveryItems.Sum(x => x.DeliveredQty);
                    var returned = d.DeliveryItems.Sum(x => x.ReturnedQty);
                    var balance = d.OrderQty - delivered + returned;

                    totalQty += d.OrderQty;
                    totalDelivered += delivered;
                    totalReturned += returned;
                    totalBalance += balance;

                    DeliveryRecords.Add(new DeliveryRecord
                    {
                        Id = d.Id,
                        Date = d.Date,
                        Company = d.Company,
                        PINumber = d.PINumber,
                        OrderQty = d.OrderQty,
                        TotalDelivered = delivered,
                        TotalReturned = returned,
                        Balance = balance,
                        Status = d.Status
                    });
                }

                // Update Properties
                DeliveryCount = dbDeliveries.Count;
                DeliveryTotalQty = totalQty;
                DeliveryTotalDelivered = totalDelivered;
                DeliveryTotalReturned = totalReturned;
                DeliveryTotalBalance = totalBalance;

                DelTotal = DeliveryCount.ToString();
                DelPending = dbDeliveries.Count(x => x.Status == "Pending").ToString();
                DelCompleted = dbDeliveries.Count(x => x.Status == "Completed").ToString();

                LastUpdate = $"Last update: {DateTime.Now:HH:mm}";

                // Notify all
                OnPropertyChanged(nameof(DeliveryTotalQty));
                OnPropertyChanged(nameof(DeliveryTotalDelivered));
                OnPropertyChanged(nameof(DeliveryTotalReturned));
                OnPropertyChanged(nameof(DeliveryTotalBalance));
                OnPropertyChanged(nameof(DeliveryProgressPercent));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] LoadData Error: {ex.Message}");
            }
        }

        private void ApplyFilter(string filter)
        {
            SelectedFilter = filter;
            DayFilterBg = "#F3F4F6"; WeekFilterBg = "#F3F4F6"; MonthFilterBg = "#F3F4F6"; YearFilterBg = "#F3F4F6";
            DayFilterFg = "#1F2937"; WeekFilterFg = "#1F2937"; MonthFilterFg = "#1F2937"; YearFilterFg = "#1F2937";

            switch (filter)
            {
                case "Today": DayFilterBg = "#2563EB"; DayFilterFg = "#FFFFFF"; break;
                case "This Week": WeekFilterBg = "#2563EB"; WeekFilterFg = "#FFFFFF"; break;
                case "This Month": MonthFilterBg = "#2563EB"; MonthFilterFg = "#FFFFFF"; break;
                case "This Year": YearFilterBg = "#2563EB"; YearFilterFg = "#FFFFFF"; break;
            }

            OnPropertyChanged(nameof(SelectedFilter));
            OnPropertyChanged(nameof(DayFilterBg)); OnPropertyChanged(nameof(WeekFilterBg));
            OnPropertyChanged(nameof(MonthFilterBg)); OnPropertyChanged(nameof(YearFilterBg));
            OnPropertyChanged(nameof(DayFilterFg)); OnPropertyChanged(nameof(WeekFilterFg));
            OnPropertyChanged(nameof(MonthFilterFg)); OnPropertyChanged(nameof(YearFilterFg));

            LoadData();
        }

        public void Sync()
        {
            LoadData();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}