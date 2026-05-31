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

namespace ProGlassAutomation.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        // ════════════════════════════════════════════════════════════════════
        // PROPERTIES - Main Stats
        // ════════════════════════════════════════════════════════════════════

        public string TotalRevenue { get; set; } = "AED 0";
        public string RevenueGrowth { get; set; } = "+0%";
        public string Orders { get; set; } = "0";
        public string OrdersGrowth { get; set; } = "+0";
        public string Salesmen { get; set; } = "0";
        public string PendingQuotes { get; set; } = "0";

        // Proforma Invoice Stats
        public string PITotal { get; set; } = "0";
        public string PIConfirmed { get; set; } = "0";
        public string PIPending { get; set; } = "0";
        public string PIValue { get; set; } = "AED 0";

        // Job Order Stats
        public string JOTotal { get; set; } = "0";
        public string JOInProgress { get; set; } = "0";
        public string JOCompleted { get; set; } = "0";

        // Delivery Stats
        public string DelPending { get; set; } = "0";
        public string DelCompleted { get; set; } = "0";
        public string DelTotal { get; set; } = "0";

        // Sheet Stats
        public string SheetTotal { get; set; } = "0";
        public string SheetAvailable { get; set; } = "0";
        public string SheetTypes { get; set; } = "0";

        // Balance Stats
        public string DailyBalance { get; set; } = "AED 0";
        public string MonthlyBalance { get; set; } = "AED 0";
        public string AnnualBalance { get; set; } = "AED 0";

        // UI Properties
        public string LastUpdate { get; set; } = "Last update: Just now";
        public string SelectedFilter { get; set; } = "This Week";

        // Filter button states
        public string DayFilterBg { get; set; } = "#F3F4F6";
        public string WeekFilterBg { get; set; } = "#2563EB";
        public string MonthFilterBg { get; set; } = "#F3F4F6";
        public string YearFilterBg { get; set; } = "#F3F4F6";

        public string DayFilterFg { get; set; } = "#1F2937";
        public string WeekFilterFg { get; set; } = "#FFFFFF";
        public string MonthFilterFg { get; set; } = "#1F2937";
        public string YearFilterFg { get; set; } = "#1F2937";

        // ════════════════════════════════════════════════════════════════════════════
        // SALESMAN DATA
        // ════════════════════════════════════════════════════════════════════

        public ObservableCollection<SalesmanData> SalesmanList { get; set; } = new ObservableCollection<SalesmanData>();
        public ObservableCollection<SalesmanData> SalesmanPerformance { get; set; } = new ObservableCollection<SalesmanData>();

        // ════════════════════════════════════════════════════════════════════════════
        // LIVECHARTS
        // ════════════════════════════════════════════════════════════════════════════

        public ISeries[] ChartSeries { get; set; }
        public Axis[] ChartXAxes { get; set; }
        public Axis[] ChartYAxes { get; set; }

        // Secondary chart for salesman performance
        public ISeries[] SalesmanChartSeries { get; set; }
        public Axis[] SalesmanChartXAxes { get; set; }
        public Axis[] SalesmanChartYAxes { get; set; }

        private LineSeries<double> _salesSeries;
        private ColumnSeries<double> _salesmanColumnSeries;

        // ════════════════════════════════════════════════════════════════════════════
        // COMMANDS
        // ════════════════════════════════════════════════════════════════════

        public ICommand RefreshCommand { get; }
        public ICommand SyncCommand { get; }
        public ICommand ActivateCommand { get; }
        public ICommand DayFilterCommand { get; }
        public ICommand WeekFilterCommand { get; }
        public ICommand MonthFilterCommand { get; }
        public ICommand YearFilterCommand { get; }

        // ════════════════════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ════════════════════════════════════════════════════════════════════

        public DashboardViewModel()
        {
            RefreshCommand = new RelayCommand(_ => LoadData());
            SyncCommand = new RelayCommand(_ => Sync());
            ActivateCommand = new RelayCommand(_ => Activate());

            DayFilterCommand = new RelayCommand(_ => ApplyFilter("Today"));
            WeekFilterCommand = new RelayCommand(_ => ApplyFilter("This Week"));
            MonthFilterCommand = new RelayCommand(_ => ApplyFilter("This Month"));
            YearFilterCommand = new RelayCommand(_ => ApplyFilter("This Year"));

            // Initialize default salesmen
            InitializeDefaultSalesmen();
            InitializeCharts();
        }

        private void InitializeDefaultSalesmen()
        {
            var defaultNames = new[]
            {
                "Mr Pradeep", "Mr Sooraj", "Ms Maya Ibrahim",
                "Mr Aftab Akram", "Mr Talha", "Mr Bilal",
                "Mr Salman (Sunny)", "Mr Harvinder", "Mr Nazim"
            };

            foreach (var name in defaultNames)
            {
                SalesmanList.Add(new SalesmanData { Name = name, Amount = "AED 0" });
                SalesmanPerformance.Add(new SalesmanData { Name = name, Amount = "0" });
            }
        }

        private void InitializeCharts()
        {
            // Main Sales Chart (Line)
            _salesSeries = new LineSeries<double>
            {
                Values = new double[] { 2, 1, 3, 5, 3, 4, 6 },
                Fill = new SolidColorPaint(SKColors.LightBlue.WithAlpha(80)),
                Stroke = new SolidColorPaint(SKColors.DodgerBlue, 3),
                GeometrySize = 8,
                GeometryStroke = new SolidColorPaint(SKColors.DodgerBlue, 2),
                GeometryFill = new SolidColorPaint(SKColors.White),
                LineSmoothness = 0.5
            };

            ChartSeries = new ISeries[] { _salesSeries };

            ChartXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = new string[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" },
                    LabelsPaint = new SolidColorPaint(SKColors.Gray),
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1)
                }
            };

            ChartYAxes = new Axis[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(SKColors.Gray),
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1),
                    Labeler = value => $"AED {value:N0}"
                }
            };

            // Salesman Performance Chart (Bar/Column)
            _salesmanColumnSeries = new ColumnSeries<double>
            {
                Values = new double[] { 10, 25, 30, 15, 40, 20, 35, 18, 12 },
                Fill = new SolidColorPaint(SKColors.MediumPurple),
                MaxBarWidth = 30
            };

            SalesmanChartSeries = new ISeries[] { _salesmanColumnSeries };

            SalesmanChartXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = new string[] { "Pradeep", "Sooraj", "Maya", "Aftab", "Talha", "Bilal", "Sunny", "Harvi", "Nazim" },
                    LabelsPaint = new SolidColorPaint(SKColors.Gray),
                    TextSize = 10
                }
            };

            SalesmanChartYAxes = new Axis[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(SKColors.Gray),
                    Labeler = value => $"AED {value:N0}"
                }
            };
        }

        // ════════════════════════════════════════════════════════════════════
        // FILTER LOGIC
        // ════════════════════════════════════════════════════════════════════

        private void ApplyFilter(string filter)
        {
            SelectedFilter = filter;

            // Reset all filters
            DayFilterBg = "#F3F4F6"; WeekFilterBg = "#F3F4F6"; MonthFilterBg = "#F3F4F6"; YearFilterBg = "#F3F4F6";
            DayFilterFg = "#1F2937"; WeekFilterFg = "#1F2937"; MonthFilterFg = "#1F2937"; YearFilterFg = "#1F2937";

            // Apply selected filter
            switch (filter)
            {
                case "Today":
                    DayFilterBg = "#2563EB"; DayFilterFg = "#FFFFFF";
                    break;
                case "This Week":
                    WeekFilterBg = "#2563EB"; WeekFilterFg = "#FFFFFF";
                    break;
                case "This Month":
                    MonthFilterBg = "#2563EB"; MonthFilterFg = "#FFFFFF";
                    break;
                case "This Year":
                    YearFilterBg = "#2563EB"; YearFilterFg = "#FFFFFF";
                    break;
            }

            // Notify property changes
            OnPropertyChanged(nameof(SelectedFilter));
            OnPropertyChanged(nameof(DayFilterBg)); OnPropertyChanged(nameof(WeekFilterBg));
            OnPropertyChanged(nameof(MonthFilterBg)); OnPropertyChanged(nameof(YearFilterBg));
            OnPropertyChanged(nameof(DayFilterFg)); OnPropertyChanged(nameof(WeekFilterFg));
            OnPropertyChanged(nameof(MonthFilterFg)); OnPropertyChanged(nameof(YearFilterFg));

            // Reload data with new filter
            LoadData();
        }

        // ════════════════════════════════════════════════════════════════════
        // LOAD DATA
        // ════════════════════════════════════════════════════════════════════

        public void LoadData()
        {
            try
            {
                var dailyWorks = DbHelper.GetAllDailyWork();
                var deliveries = DbHelper.GetAllDeliveries();
                var pis = DbHelper.GetAllProformaInvoices();
                var jos = DbHelper.GetAllJobOrders();
                var sheets = DbHelper.GetAllSheets();

                // Filter data based on selected filter
                var filteredWorks = FilterData(dailyWorks, pis);

                LoadPIStats(pis, filteredWorks, deliveries);
                LoadJOStats(jos);
                LoadDeliveryStats(deliveries);
                LoadSheetStats(sheets);
                LoadBalanceStats(filteredWorks);
                UpdateChartData(filteredWorks);
                LoadSalesmanData(dailyWorks, deliveries, pis);

                LastUpdate = $"Last update: {DateTime.Now:HH:mm}";
                OnPropertyChanged(nameof(LastUpdate));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] LoadData Error: {ex.Message}");
            }
        }

        private List<DailyWork> FilterData(List<DailyWork> dailyWorks, List<ProformaInvoiceModel> pis)
        {
            var today = DateTime.Today;

            switch (SelectedFilter)
            {
                case "Today":
                    return dailyWorks.Where(d => d.Date.Date == today).ToList();

                case "This Week":
                    var weekStart = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
                    if (today.DayOfWeek == DayOfWeek.Sunday) weekStart = weekStart.AddDays(-7);
                    return dailyWorks.Where(d => d.Date.Date >= weekStart && d.Date.Date <= today).ToList();

                case "This Month":
                    var monthStart = new DateTime(today.Year, today.Month, 1);
                    return dailyWorks.Where(d => d.Date >= monthStart).ToList();

                case "This Year":
                    var yearStart = new DateTime(today.Year, 1, 1);
                    return dailyWorks.Where(d => d.Date >= yearStart).ToList();

                default:
                    return dailyWorks;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // SALESMAN DATA
        // ════════════════════════════════════════════════════════════

        private void LoadSalesmanData(
            List<DailyWork> dailyWorks,
            List<Delivery> deliveries,
            List<ProformaInvoiceModel> pis)
        {
            try
            {
                var defaultNames = new[]
                {
                    "Mr Pradeep", "Mr Sooraj", "Ms Maya Ibrahim",
                    "Mr Aftab Akram", "Mr Talha", "Mr Bilal",
                    "Mr Salman (Sunny)", "Mr Harvinder", "Mr Nazim"
                };

                // Calculate totals for each salesman from ProformaInvoices
                var piSalesmen = pis
                    .Where(p => !string.IsNullOrWhiteSpace(p.Salesman) && p.Status == "Confirmed")
                    .GroupBy(p => p.Salesman)
                    .Select(g => new SalesmanData
                    {
                        Name = g.Key,
                        Amount = $"AED {g.Sum(x => x.NetAmount):N0}"
                    })
                    .ToDictionary(s => s.Name, s => s.Amount);

                // Update SalesmanList
                SalesmanList.Clear();
                for (int i = 0; i < defaultNames.Length; i++)
                {
                    var name = defaultNames[i];
                    var amount = piSalesmen.ContainsKey(name) ? piSalesmen[name] : "AED 0";
                    SalesmanList.Add(new SalesmanData { Name = name, Amount = amount });
                }

                // Update Salesman Performance Chart
                var performanceValues = new double[9];
                for (int i = 0; i < defaultNames.Length; i++)
                {
                    var name = defaultNames[i];
                    if (piSalesmen.ContainsKey(name))
                    {
                        var amountStr = piSalesmen[name].Replace("AED ", "").Replace(",", "");
                        double.TryParse(amountStr, out double amount);
                        performanceValues[i] = amount / 1000; // Convert to thousands
                    }
                }

                _salesmanColumnSeries.Values = performanceValues;

                OnPropertyChanged(nameof(SalesmanList));
                OnPropertyChanged(nameof(SalesmanPerformance));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] LoadSalesmanData Error: {ex.Message}");
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // STATS METHODS
        // ════════════════════════════════════════════════════════════

        private void LoadPIStats(List<ProformaInvoiceModel> pis, List<DailyWork> dailyWorks, List<Delivery> deliveries)
        {
            try
            {
                PITotal = pis.Count.ToString();
                PIConfirmed = pis.Count(p => p.Status == "Confirmed").ToString();
                PIPending = pis.Count(p => p.Status == "Draft" || p.Status == "Pending").ToString();
                PendingQuotes = PIPending;

                var confirmedPIs = pis.Where(p => p.Status == "Confirmed").ToList();
                var totalValue = confirmedPIs.Sum(p => p.NetAmount);
                PIValue = $"AED {totalValue:N0}";

                TotalRevenue = $"AED {totalValue:N0}";
                Orders = pis.Count.ToString();

                var dwSalesmen = dailyWorks.Where(d => !string.IsNullOrEmpty(d.Salesman)).Select(d => d.Salesman).Distinct().ToList();
                var delSalesmen = deliveries.Where(d => !string.IsNullOrEmpty(d.Salesman)).Select(d => d.Salesman).Distinct().ToList();
                var piSalesmen = pis.Where(p => !string.IsNullOrEmpty(p.Salesman)).Select(p => p.Salesman).Distinct().ToList();
                var allSalesmen = dwSalesmen.Union(delSalesmen).Union(piSalesmen).Distinct().ToList();
                Salesmen = allSalesmen.Count.ToString();

                NotifyStatsProperties();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] LoadPIStats Error: {ex.Message}");
            }
        }

        private void LoadJOStats(List<JobOrderModel> jos)
        {
            try
            {
                JOTotal = jos.Count.ToString();
                JOInProgress = jos.Count(j => j.Status == "In Progress" || j.Status == "Pending").ToString();
                JOCompleted = jos.Count(j => j.Status == "Completed").ToString();
                OnPropertyChanged(nameof(JOTotal)); OnPropertyChanged(nameof(JOInProgress)); OnPropertyChanged(nameof(JOCompleted));
            }
            catch { }
        }

        private void LoadDeliveryStats(List<Delivery> deliveries)
        {
            try
            {
                DelTotal = deliveries.Count.ToString();
                DelPending = deliveries.Count(d => d.Status == "Pending" || d.Status == "In Transit").ToString();
                DelCompleted = deliveries.Count(d => d.Status == "Completed").ToString();
                OnPropertyChanged(nameof(DelTotal)); OnPropertyChanged(nameof(DelPending)); OnPropertyChanged(nameof(DelCompleted));
            }
            catch { }
        }

        private void LoadSheetStats(List<Sheet> sheets)
        {
            try
            {
                var totalQty = sheets.Sum(s => s.TotalStock);
                SheetTotal = totalQty.ToString();
                SheetAvailable = sheets.Count(s => s.BalanceSheets > 0).ToString();
                SheetTypes = sheets.Select(s => s.Thickness).Distinct().Count().ToString();
                OnPropertyChanged(nameof(SheetTotal)); OnPropertyChanged(nameof(SheetAvailable)); OnPropertyChanged(nameof(SheetTypes));
            }
            catch { }
        }

        private void LoadBalanceStats(List<DailyWork> dailyWorks)
        {
            try
            {
                var today = DateTime.Today;
                var monthStart = new DateTime(today.Year, today.Month, 1);
                var yearStart = new DateTime(today.Year, 1, 1);

                var todayWorks = dailyWorks.Where(d => d.Date.Date == today).ToList();
                var dailyRev = todayWorks.Sum(d => d.SQM * 100);
                DailyBalance = $"AED {dailyRev:N0}";

                var monthWorks = dailyWorks.Where(d => d.Date >= monthStart).ToList();
                var monthBal = monthWorks.Sum(d => d.SQM * 100);
                MonthlyBalance = $"AED {monthBal:N0}";

                var yearWorks = dailyWorks.Where(d => d.Date >= yearStart).ToList();
                var yearBal = yearWorks.Sum(d => d.SQM * 100);
                AnnualBalance = $"AED {yearBal:N0}";

                OnPropertyChanged(nameof(DailyBalance)); OnPropertyChanged(nameof(MonthlyBalance)); OnPropertyChanged(nameof(AnnualBalance));
            }
            catch { }
        }

        private void UpdateChartData(List<DailyWork> dailyWorks)
        {
            try
            {
                var today = DateTime.Today;
                var weekStart = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
                if (today.DayOfWeek == DayOfWeek.Sunday) weekStart = weekStart.AddDays(-7);

                var weekData = dailyWorks
                    .Where(d => d.Date.Date >= weekStart && d.Date.Date <= today)
                    .GroupBy(d => d.Date.DayOfWeek)
                    .Select(g => new { Day = g.Key, Total = g.Sum(x => x.SQM) })
                    .ToList();

                double[] values = new double[7];
                for (int i = 0; i < 7; i++)
                {
                    var dayOfWeek = (DayOfWeek)((i + (int)DayOfWeek.Monday) % 7);
                    var dayData = weekData.FirstOrDefault(d => d.Day == dayOfWeek);
                    values[i] = (dayData?.Total ?? 0) / 10;
                }

                _salesSeries.Values = values;
            }
            catch { }
        }

        private void NotifyStatsProperties()
        {
            OnPropertyChanged(nameof(TotalRevenue)); OnPropertyChanged(nameof(Orders)); OnPropertyChanged(nameof(Salesmen));
            OnPropertyChanged(nameof(PITotal)); OnPropertyChanged(nameof(PIConfirmed)); OnPropertyChanged(nameof(PIPending));
            OnPropertyChanged(nameof(PIValue)); OnPropertyChanged(nameof(PendingQuotes));
        }

        // ════════════════════════════════════════════════════════════════════
        // ACTIONS
        // ════════════════════════════════════════════════════════════

        public void Sync()
        {
            LoadData();
        }

        public void Activate()
        {
            MessageBox.Show("Please enter your license key to activate.", "Activate", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ════════════════════════════════════════════════════════════════════
        // PROPERTY CHANGED
        // ════════════════════════════════════════════════════════════════════

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // SALESMAN DATA CLASS
    // ════════════════════════════════════════════════════════════════════

    public class SalesmanData
    {
        public string Name { get; set; } = "";
        public string Amount { get; set; } = "";
    }
}