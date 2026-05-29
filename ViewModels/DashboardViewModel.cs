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
using ProGlassAutomation.Services;
using ProGlassAutomation.Data.Database;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        // ══════════════════════════════════════════════════════════════════════════════
        // PROPERTIES
        // ══════════════════════════════════════════════════════════════════════════════

        public string TotalRevenue { get; set; } = "AED 0";
        public string RevenueGrowth { get; set; } = "+0% this year";
        public string Orders { get; set; } = "0";
        public string OrdersGrowth { get; set; } = "+0 this week";
        public string Salesmen { get; set; } = "0";
        public string PendingQuotes { get; set; } = "0";

        public string PITotal { get; set; } = "0";
        public string PIConfirmed { get; set; } = "0";
        public string PIPending { get; set; } = "0";
        public string PIValue { get; set; } = "AED 0";

        public string JOTotal { get; set; } = "0";
        public string JOInProgress { get; set; } = "0";
        public string JOCompleted { get; set; } = "0";

        public string DelPending { get; set; } = "0";
        public string DelCompleted { get; set; } = "0";
        public string DelTotal { get; set; } = "0";

        public string SheetTotal { get; set; } = "0";
        public string SheetAvailable { get; set; } = "0";
        public string SheetTypes { get; set; } = "0";

        public string DailyBalance { get; set; } = "AED 0";
        public string MonthlyBalance { get; set; } = "AED 0";
        public string AnnualBalance { get; set; } = "AED 0";

        public string LastUpdate { get; set; } = "Last update: Just now";
        public string LastSync { get; set; } = "Never";

        public string SelectedFilter { get; set; } = "This Week";

        // ══════════════════════════════════════════════════════════════════════════════
        // SALESMAN ARRAYS - Top 9 salesmen with their totals
        // ══════════════════════════════════════════════════════════════════════════════

        public ObservableCollection<string> SalesmanNames { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> SalesmanAmounts { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> SalesmanDiffs { get; set; } = new ObservableCollection<string>();

        // ══════════════════════════════════════════════════════════════════════════════
        // LIVECHARTS
        // ══════════════════════════════════════════════════════════════════════════════

        public ISeries[] ChartSeries { get; set; }
        public Axis[] ChartXAxes { get; set; }
        public Axis[] ChartYAxes { get; set; }

        // ══════════════════════════════════════════════════════════════════════════════
        // ACTIVITY LOG
        // ══════════════════════════════════════════════════════════════════════════════

        public ObservableCollection<ActivityItem> ActivityItems { get; set; } = new ObservableCollection<ActivityItem>();

        // ══════════════════════════════════════════════════════════════════════════════
        // COMMANDS
        // ══════════════════════════════════════════════════════════════════════════════

        public ICommand RefreshCommand { get; }
        public ICommand SyncCommand { get; }
        public ICommand ActivateCommand { get; }

        // ══════════════════════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════════════════════════════

        public DashboardViewModel()
        {
            RefreshCommand = new RelayCommand(_ => LoadData());
            SyncCommand = new RelayCommand(_ => Sync());
            ActivateCommand = new RelayCommand(_ => Activate());

            InitializeChart();
            InitializeSalesmanArrays();
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // INITIALIZE
        // ══════════════════════════════════════════════════════════════════════════════

        private void InitializeChart()
        {
            ChartSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = new double[] { 2, 1, 3, 5, 3, 4, 6 },
                    Fill = new SolidColorPaint(SKColors.LightBlue.WithAlpha(80)),
                    Stroke = new SolidColorPaint(SKColors.DodgerBlue, 3),
                    GeometrySize = 8,
                    GeometryStroke = new SolidColorPaint(SKColors.DodgerBlue, 2),
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    LineSmoothness = 0.5
                }
            };

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
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1)
                }
            };
        }

        private void InitializeSalesmanArrays()
        {
            try
            {
                var allSalesmen = DbHelper.GetAllSalesmanOptions();
                var dailyWorks = DbHelper.GetAllDailyWork();
                var deliveries = DbHelper.GetAllDeliveries();

                var salesmanData = allSalesmen.Select(s => new
                {
                    Name = s,
                    DailyWorkTotal = dailyWorks.Where(d => d.Salesman == s).Sum(d => d.SQM),
                    DeliveryTotal = deliveries.Where(d => d.Salesman == s).Sum(d => d.OrderSQM)
                })
                .Select(x => new { x.Name, Total = x.DailyWorkTotal + x.DeliveryTotal })
                .OrderByDescending(x => x.Total)
                .Take(9)
                .ToList();

                SalesmanNames.Clear();
                SalesmanAmounts.Clear();
                SalesmanDiffs.Clear();

                if (salesmanData.Any())
                {
                    foreach (var item in salesmanData)
                    {
                        SalesmanNames.Add(item.Name);
                        SalesmanAmounts.Add($"AED {item.Total:N0}");
                        SalesmanDiffs.Add("Active");
                    }
                }
                else
                {
                    for (int i = 0; i < 5; i++)
                    {
                        SalesmanNames.Add("No Salesmen");
                        SalesmanAmounts.Add("AED 0");
                        SalesmanDiffs.Add("+0%");
                    }
                }

                OnPropertyChanged(nameof(SalesmanNames));
                OnPropertyChanged(nameof(SalesmanAmounts));
                OnPropertyChanged(nameof(SalesmanDiffs));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] InitializeSalesmanArrays Error: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // LOAD DATA
        // ══════════════════════════════════════════════════════════════════════════════

        public void LoadData()
        {
            try
            {
                LoadPIStats();
                LoadJOStats();
                LoadDeliveryStats();
                LoadSheetStats();
                LoadBalanceStats();
                UpdateChartData();
                InitializeSalesmanArrays();

                LastUpdate = $"Last update: {DateTime.Now:HH:mm}";
                OnPropertyChanged(nameof(LastUpdate));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] LoadData Error: {ex.Message}");
            }
        }

        private void LoadPIStats()
        {
            try
            {
                var pis = DbHelper.GetAllProformaInvoices();
                var dailyWorks = DbHelper.GetAllDailyWork();
                var deliveries = DbHelper.GetAllDeliveries();

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
                var allSalesmen = dwSalesmen.Union(delSalesmen).Distinct().ToList();
                Salesmen = allSalesmen.Count.ToString();

                OnPropertyChanged(nameof(TotalRevenue));
                OnPropertyChanged(nameof(Orders));
                OnPropertyChanged(nameof(Salesmen));
                OnPropertyChanged(nameof(PITotal));
                OnPropertyChanged(nameof(PIConfirmed));
                OnPropertyChanged(nameof(PIPending));
                OnPropertyChanged(nameof(PIValue));
                OnPropertyChanged(nameof(PendingQuotes));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] LoadPIStats Error: {ex.Message}");
            }
        }

        private void LoadJOStats()
        {
            try
            {
                var jos = DbHelper.GetAllJobOrders();

                JOTotal = jos.Count.ToString();
                JOInProgress = jos.Count(j => j.Status == "In Progress" || j.Status == "Pending").ToString();
                JOCompleted = jos.Count(j => j.Status == "Completed").ToString();

                OnPropertyChanged(nameof(JOTotal));
                OnPropertyChanged(nameof(JOInProgress));
                OnPropertyChanged(nameof(JOCompleted));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] LoadJOStats Error: {ex.Message}");
            }
        }

        private void LoadDeliveryStats()
        {
            try
            {
                var deliveries = DbHelper.GetAllDeliveries();

                DelTotal = deliveries.Count.ToString();
                DelPending = deliveries.Count(d => d.Status == "Pending" || d.Status == "In Transit").ToString();
                DelCompleted = deliveries.Count(d => d.Status == "Completed").ToString();

                OnPropertyChanged(nameof(DelTotal));
                OnPropertyChanged(nameof(DelPending));
                OnPropertyChanged(nameof(DelCompleted));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] LoadDeliveryStats Error: {ex.Message}");
            }
        }

        private void LoadSheetStats()
        {
            try
            {
                var sheets = DbHelper.GetAllSheets();

                var totalQty = sheets.Sum(s => s.TotalStock);
                SheetTotal = totalQty.ToString();
                SheetAvailable = sheets.Count(s => s.BalanceSheets > 0).ToString();
                SheetTypes = sheets.Select(s => s.Thickness).Distinct().Count().ToString();

                OnPropertyChanged(nameof(SheetTotal));
                OnPropertyChanged(nameof(SheetAvailable));
                OnPropertyChanged(nameof(SheetTypes));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] LoadSheetStats Error: {ex.Message}");
            }
        }

        private void LoadBalanceStats()
        {
            try
            {
                var dailyWorks = DbHelper.GetAllDailyWork();
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

                OnPropertyChanged(nameof(DailyBalance));
                OnPropertyChanged(nameof(MonthlyBalance));
                OnPropertyChanged(nameof(AnnualBalance));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] LoadBalanceStats Error: {ex.Message}");
            }
        }

        private void UpdateChartData()
        {
            try
            {
                var dailyWorks = DbHelper.GetAllDailyWork();
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

                ChartSeries = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = values,
                        Fill = new SolidColorPaint(SKColors.LightBlue.WithAlpha(80)),
                        Stroke = new SolidColorPaint(SKColors.DodgerBlue, 3),
                        GeometrySize = 8,
                        GeometryStroke = new SolidColorPaint(SKColors.DodgerBlue, 2),
                        GeometryFill = new SolidColorPaint(SKColors.White),
                        LineSmoothness = 0.5
                    }
                };

                OnPropertyChanged(nameof(ChartSeries));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] UpdateChartData Error: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // ACTIONS
        // ══════════════════════════════════════════════════════════════════════════════

        public void Sync()
        {
            try
            {
                LastSync = DateTime.Now.ToString("HH:mm");
                OnPropertyChanged(nameof(LastSync));
                LoadData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] Sync Error: {ex.Message}");
            }
        }

        public void Activate()
        {
            MessageBox.Show("Please enter your license key to activate.", "Activate", MessageBoxButton.OK, MessageBoxImage.Information);
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

    public class ActivityItem
    {
        public string Message { get; set; } = "";
        public string Time { get; set; } = "";
    }
}