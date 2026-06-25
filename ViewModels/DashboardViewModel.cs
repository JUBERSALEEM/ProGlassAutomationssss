using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using ProGlassAutomation.Data.Database;
using DbDelivery = ProGlassAutomation.Data.Database.Delivery;

namespace ProGlassAutomation.ViewModels
{
    public class KPICard : INotifyPropertyChanged
    {
        private double _value;
        public string Title { get; set; } = "";
        public double Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }
        public string Suffix { get; set; } = "";
        public double ChangePercent { get; set; }
        public string ChangeLabel { get; set; } = "";
        public string Icon { get; set; } = "";
        public string ColorKey { get; set; } = "Blue";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class DailyWorkRecord
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Company { get; set; } = "";
        public string PiNumber { get; set; } = "";
        public string CustomerReference { get; set; } = "";
        public string TypeOfWork { get; set; } = "";
        public string ProductionStatus { get; set; } = "";
        public int Qty { get; set; }
        public double Sqm { get; set; }
        public string Status { get; set; } = "";
        public string Salesman { get; set; } = "";
        public string Color { get; set; } = "";
    }

    public class DeliveryRecord
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Company { get; set; } = "";
        public string PINumber { get; set; } = "";
        public string TypeOfWork { get; set; } = "";
        public string Color { get; set; } = "";
        public int OrderQty { get; set; }
        public int TotalDelivered { get; set; }
        public int TotalReturned { get; set; }
        public int Balance { get; set; }
        public double OrderSQM { get; set; }
        public string Salesman { get; set; } = "";
        public string Status { get; set; } = "";
        public string Notes { get; set; } = "";
    }

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

        // Delivery Data - WITH PROPER NOTIFICATION
        public ObservableCollection<DeliveryRecord> DeliveryRecords { get; set; } = new ObservableCollection<DeliveryRecord>();

        private int _deliveryCount;
        public int DeliveryCount
        {
            get => _deliveryCount;
            set { _deliveryCount = value; OnPropertyChanged(); }
        }

        private int _totalDeliveryQty;
        public int TotalDeliveryQty
        {
            get => _totalDeliveryQty;
            set { _totalDeliveryQty = value; OnPropertyChanged(); }
        }

        private int _totalDeliveredQty;
        public int TotalDeliveredQty
        {
            get => _totalDeliveredQty;
            set { _totalDeliveredQty = value; OnPropertyChanged(); }
        }

        private int _totalBalanceQty;
        public int TotalBalanceQty
        {
            get => _totalBalanceQty;
            set { _totalBalanceQty = value; OnPropertyChanged(); }
        }

        private double _totalDeliverySQM;
        public double TotalDeliverySQM
        {
            get => _totalDeliverySQM;
            set { _totalDeliverySQM = value; OnPropertyChanged(); }
        }

        // Charts
        public ISeries[] DailyTrendSeries { get; set; }
        public Axis[] DailyTrendXAxes { get; set; }
        public Axis[] DailyTrendYAxes { get; set; }

        public ISeries[] WeeklyJobSeries { get; set; }
        public Axis[] WeeklyJobXAxes { get; set; }
        public Axis[] WeeklyJobYAxes { get; set; }

        public ISeries[] ProductMixSeries { get; set; }

        public ISeries[] MachineUtilSeries { get; set; }
        public Axis[] MachineUtilXAxes { get; set; }
        public Axis[] MachineUtilYAxes { get; set; }

        public ISeries[] DeliveryStatusSeries { get; set; }

        public ISeries[] OptimizationSeries { get; set; }
        public Axis[] OptimizationXAxes { get; set; }
        public Axis[] OptimizationYAxes { get; set; }

        // Delivery Charts
        public ISeries[] DeliveryTrendSeries { get; set; }
        public Axis[] DeliveryTrendXAxes { get; set; }
        public Axis[] DeliveryTrendYAxes { get; set; }

        private string _lastUpdate = "Last update: Just now";
        public string LastUpdate
        {
            get => _lastUpdate;
            set { _lastUpdate = value; OnPropertyChanged(); }
        }

        public ICommand RefreshCommand { get; }

        public DashboardViewModel()
        {
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            InitializeCharts();
            _ = LoadDataAsync();
        }

        private void InitializeCharts()
        {
            // Chart 1: Daily Production Trend
            DailyTrendSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Name = "SQM",
                    Values = new double[] { 1100, 1150, 1200, 1180, 1250, 1220, 1280 },
                    Fill = new SolidColorPaint(new SKColor(59, 130, 246, 80)),
                    Stroke = new SolidColorPaint(new SKColor(59, 130, 246), 2),
                    GeometrySize = 0,
                    LineSmoothness = 0.5f
                },
                new LineSeries<double>
                {
                    Name = "Sheets",
                    Values = new double[] { 200, 210, 205, 220, 215, 230, 225 },
                    Fill = new SolidColorPaint(new SKColor(16, 185, 129, 80)),
                    Stroke = new SolidColorPaint(new SKColor(16, 185, 129), 2),
                    GeometrySize = 0,
                    LineSmoothness = 0.5f,
                    ScalesYAt = 1
                }
            };
            DailyTrendXAxes = new Axis[] { new Axis { LabelsPaint = new SolidColorPaint(SKColors.Gray) } };
            DailyTrendYAxes = new Axis[]
            {
                new Axis { LabelsPaint = new SolidColorPaint(SKColors.Gray) },
                new Axis { LabelsPaint = new SolidColorPaint(SKColors.Gray), Position = LiveChartsCore.Measure.AxisPosition.End }
            };

            // Chart 2: Weekly Jobs
            WeeklyJobSeries = new ISeries[]
            {
                new StackedColumnSeries<int>
                {
                    Name = "Completed",
                    Values = new int[] { 35, 42, 28, 45, 38, 50, 33, 41 },
                    Fill = new SolidColorPaint(new SKColor(16, 185, 129))
                },
                new StackedColumnSeries<int>
                {
                    Name = "In Progress",
                    Values = new int[] { 15, 18, 22, 12, 20, 15, 25, 18 },
                    Fill = new SolidColorPaint(new SKColor(59, 130, 246))
                },
                new StackedColumnSeries<int>
                {
                    Name = "Pending",
                    Values = new int[] { 8, 12, 5, 15, 10, 8, 12, 9 },
                    Fill = new SolidColorPaint(new SKColor(245, 158, 11))
                }
            };
            WeeklyJobXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = new[] { "Week 1", "Week 2", "Week 3", "Week 4", "Week 5", "Week 6", "Week 7", "Week 8" },
                    LabelsPaint = new SolidColorPaint(SKColors.Gray)
                }
            };
            WeeklyJobYAxes = new Axis[] { new Axis { LabelsPaint = new SolidColorPaint(SKColors.Gray) } };

            // Chart 3: Product Mix
            ProductMixSeries = new ISeries[]
            {
                new PieSeries<double> { Name = "Tempered", Values = new double[] { 35 }, Fill = new SolidColorPaint(new SKColor(59, 130, 246)), InnerRadius = 60 },
                new PieSeries<double> { Name = "Laminated", Values = new double[] { 25 }, Fill = new SolidColorPaint(new SKColor(16, 185, 129)), InnerRadius = 60 },
                new PieSeries<double> { Name = "Insulated", Values = new double[] { 20 }, Fill = new SolidColorPaint(new SKColor(139, 92, 246)), InnerRadius = 60 },
                new PieSeries<double> { Name = "Curved", Values = new double[] { 12 }, Fill = new SolidColorPaint(new SKColor(245, 158, 11)), InnerRadius = 60 },
                new PieSeries<double> { Name = "Decorative", Values = new double[] { 8 }, Fill = new SolidColorPaint(new SKColor(239, 68, 68)), InnerRadius = 60 }
            };

            // Chart 4: Machine Utilization
            MachineUtilSeries = new ISeries[]
            {
                new RowSeries<int>
                {
                    Values = new int[] { 87, 74, 92, 68, 81, 55 },
                    Fill = new SolidColorPaint(new SKColor(139, 92, 246)),
                    MaxBarWidth = 20
                }
            };
            MachineUtilXAxes = new Axis[] { new Axis { LabelsPaint = new SolidColorPaint(SKColors.Gray), MaxLimit = 100 } };
            MachineUtilYAxes = new Axis[]
            {
                new Axis
                {
                    Labels = new[] { "CNC-01", "CNC-02", "Temper-01", "Lam-01", "IG-01", "Polish-01" },
                    LabelsPaint = new SolidColorPaint(SKColors.Gray)
                }
            };

            // Chart 5: Delivery Status
            DeliveryStatusSeries = new ISeries[]
            {
                new PieSeries<double> { Name = "Delivered", Values = new double[] { 52 }, Fill = new SolidColorPaint(new SKColor(16, 185, 129)), InnerRadius = 55 },
                new PieSeries<double> { Name = "In Transit", Values = new double[] { 15 }, Fill = new SolidColorPaint(new SKColor(59, 130, 246)), InnerRadius = 55 },
                new PieSeries<double> { Name = "Scheduled", Values = new double[] { 25 }, Fill = new SolidColorPaint(new SKColor(245, 158, 11)), InnerRadius = 55 },
                new PieSeries<double> { Name = "Delayed", Values = new double[] { 8 }, Fill = new SolidColorPaint(new SKColor(239, 68, 68)), InnerRadius = 55 }
            };

            // Chart 6: Optimization
            OptimizationSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Name = "Efficiency %",
                    Values = new double[] { 82, 85, 84, 88, 87, 89, 86, 90 },
                    Stroke = new SolidColorPaint(new SKColor(139, 92, 246), 3),
                    GeometrySize = 4,
                    Fill = null
                },
                new LineSeries<double>
                {
                    Name = "Waste",
                    Values = new double[] { 12, 10, 11, 8, 9, 7, 10, 6 },
                    Stroke = new SolidColorPaint(new SKColor(245, 158, 11), 2),
                    Fill = new SolidColorPaint(new SKColor(245, 158, 11, 30)),
                    GeometrySize = 0,
                    ScalesYAt = 1
                }
            };
            OptimizationXAxes = new Axis[] { new Axis { LabelsPaint = new SolidColorPaint(SKColors.Gray) } };
            OptimizationYAxes = new Axis[]
            {
                new Axis { LabelsPaint = new SolidColorPaint(SKColors.Gray), MinLimit = 65, MaxLimit = 100 },
                new Axis { LabelsPaint = new SolidColorPaint(SKColors.Gray), Position = LiveChartsCore.Measure.AxisPosition.End }
            };

            // Delivery Trend Chart
            DeliveryTrendSeries = new ISeries[]
            {
                new LineSeries<int>
                {
                    Name = "Order Qty",
                    Values = new int[] { 120, 135, 128, 142, 156, 148, 162, 175 },
                    Stroke = new SolidColorPaint(new SKColor(59, 130, 246), 2),
                    Fill = new SolidColorPaint(new SKColor(59, 130, 246, 30)),
                    GeometrySize = 4
                },
                new LineSeries<int>
                {
                    Name = "Delivered",
                    Values = new int[] { 100, 125, 120, 135, 145, 140, 155, 168 },
                    Stroke = new SolidColorPaint(new SKColor(16, 185, 129), 2),
                    Fill = null,
                    GeometrySize = 4
                }
            };
            DeliveryTrendXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun", "Mon" },
                    LabelsPaint = new SolidColorPaint(SKColors.Gray)
                }
            };
            DeliveryTrendYAxes = new Axis[] { new Axis { LabelsPaint = new SolidColorPaint(SKColors.Gray) } };
        }

        public async Task LoadDataAsync()
        {
            try
            {
                // Load DailyWorks
                var dbRecords = await Task.Run(() => DbHelper.GetAllDailyWork());
                DailyWorkRecords.Clear();
                foreach (var record in dbRecords)
                {
                    DailyWorkRecords.Add(new DailyWorkRecord
                    {
                        Id = record.Id,
                        Date = record.Date,
                        Company = record.Company ?? "",
                        PiNumber = record.PINumber ?? "",
                        CustomerReference = record.CustomerReference ?? "",
                        TypeOfWork = record.TypeOfWork ?? "",
                        ProductionStatus = record.ProductionStatus ?? "",
                        Qty = record.Qty,
                        Sqm = record.SQM,
                        Status = record.Status ?? "",
                        Salesman = record.Salesman ?? "",
                        Color = record.Color ?? ""
                    });
                }
                DailyWorksCount = DailyWorkRecords.Count;

                // Load Deliveries
                var dbDeliveries = await Task.Run(() => DbHelper.GetAllDeliveries());
                DeliveryRecords.Clear();
                foreach (var delivery in dbDeliveries)
                {
                    DeliveryRecords.Add(new DeliveryRecord
                    {
                        Id = delivery.Id,
                        Date = delivery.Date,
                        Company = delivery.Company ?? "",
                        PINumber = delivery.PINumber ?? "",
                        TypeOfWork = delivery.TypeOfWork ?? "",
                        Color = delivery.Color ?? "",
                        OrderQty = delivery.OrderQty,
                        TotalDelivered = delivery.TotalDelivered,
                        TotalReturned = delivery.TotalReturned,
                        Balance = delivery.Balance,
                        OrderSQM = delivery.OrderSQM,
                        Salesman = delivery.Salesman ?? "",
                        Status = delivery.Status ?? "",
                        Notes = delivery.Notes ?? ""
                    });
                }

                // Update Delivery Statistics
                DeliveryCount = DeliveryRecords.Count;
                TotalDeliveryQty = DeliveryRecords.Sum(d => d.OrderQty);
                TotalDeliveredQty = DeliveryRecords.Sum(d => d.TotalDelivered);
                TotalBalanceQty = DeliveryRecords.Sum(d => d.Balance);
                TotalDeliverySQM = DeliveryRecords.Sum(d => d.OrderSQM);

                // Notify all property changes
                OnPropertyChanged(nameof(DeliveryRecords));
                OnPropertyChanged(nameof(DeliveryCount));
                OnPropertyChanged(nameof(TotalDeliveryQty));
                OnPropertyChanged(nameof(TotalDeliveredQty));
                OnPropertyChanged(nameof(TotalBalanceQty));
                OnPropertyChanged(nameof(TotalDeliverySQM));

                UpdateKPICards();
                LastUpdate = $"Last update: {DateTime.Now:HH:mm}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] LoadData Error: {ex.Message}");
            }
        }

        private void UpdateKPICards()
        {
            var today = DateTime.Today;
            var todayRecords = DailyWorkRecords.Where(r => r.Date.Date == today).ToList();

            DailyOutputCard.Value = todayRecords.Sum(r => r.Sqm);
            DeliveriesCard.Value = DeliveryRecords.Count(d => d.Date.Date == today);

            if (DailyWorkRecords.Any())
            {
                var completed = DailyWorkRecords.Count(r => r.ProductionStatus == "COMPLETED");
                OptimizationCard.Value = Math.Round((double)completed / DailyWorkRecords.Count * 100, 1);
            }

            OnPropertyChanged(nameof(ProformaCard));
            OnPropertyChanged(nameof(JobOrdersCard));
            OnPropertyChanged(nameof(OptimizationCard));
            OnPropertyChanged(nameof(DailyOutputCard));
            OnPropertyChanged(nameof(DeliveriesCard));
            OnPropertyChanged(nameof(InventoryCard));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}