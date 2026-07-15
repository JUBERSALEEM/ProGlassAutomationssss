using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using ProGlassAutomation.Data.Database;
using ProGlassAutomation.Models;

// ✅ FIX: Explicit aliases to resolve ambiguities
using DbDelivery = ProGlassAutomation.Data.Database.Delivery;
using DbDeliveryItem = ProGlassAutomation.Data.Database.DeliveryItem;
using DbJobOrder = ProGlassAutomation.Data.Database.JobOrderModel;
using UiJobOrder = ProGlassAutomation.Models.JobOrder;
using UiProforma = ProGlassAutomation.Models.ProformaInvoiceModel;
using UiDailyWork = ProGlassAutomation.Models.DailyWorkModel;
using ProformaListVM = ProGlassAutomation.ViewModels.ProformaInvoiceListViewModel;
using JobOrderListVM = ProGlassAutomation.ViewModels.JobOrderListViewModel;
using DeliveryVM = ProGlassAutomation.ViewModels.DeliveryViewModel;
using DailyWorksVM = ProGlassAutomation.ViewModels.DailyWorksViewModel;

namespace ProGlassAutomation.ViewModels
{
    // ==================== HELPER CLASSES ====================
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
    }

    // ==================== DASHBOARD VIEWMODEL ====================
    public class DashboardViewModel : INotifyPropertyChanged
    {
        // ============ SHARED VIEWMODEL REFERENCES ============
        private ProformaListVM? _piList;
        private JobOrderListVM? _joList;
        private DeliveryVM? _delivery;
        private DailyWorksVM? _dailyWorks;

        private DateTime? _filterStartDate;
        private DateTime? _filterEndDate;

        // ✅ FIX: Empty collections as fallback (no ?? on different generic types)
        private static readonly ObservableCollection<UiProforma> EmptyInvoices = new ObservableCollection<UiProforma>();
        private static readonly ObservableCollection<DbJobOrder> EmptyJobOrders = new ObservableCollection<DbJobOrder>();
        private static readonly ObservableCollection<UiDailyWork> EmptyDailyWorks = new ObservableCollection<UiDailyWork>();
        private static readonly List<SalesmanTotal> EmptySalesmenList = new List<SalesmanTotal>();
        private static readonly List<TypeTotal> EmptyTypeList = new List<TypeTotal>();

        // ============ CONSTRUCTOR ============
        public DashboardViewModel()
        {
            try
            {
                _piList = SharedViewModels.ProformaInvoiceListVM;
                _joList = SharedViewModels.JobOrderListVM;
                _delivery = SharedViewModels.DeliveryVM;
                _dailyWorks = SharedViewModels.DailyWorksVM;

                DailyWorkRecords = new ObservableCollection<DailyWorkRecord>();
                DeliveryRecords = new ObservableCollection<DeliveryRecord>();
                SalesmanList = new ObservableCollection<SalesmanData>();

                // ✅ NEW: SyncCommand replaces both Refresh and Sync handlers
                SyncCommand = new RelayCommand(async _ => await SyncDataAsync(), _ => !IsSyncing);
                DayFilterCommand = new RelayCommand(_ => ApplyFilter("Today"), _ => !IsSyncing);
                WeekFilterCommand = new RelayCommand(_ => ApplyFilter("This Week"), _ => !IsSyncing);
                MonthFilterCommand = new RelayCommand(_ => ApplyFilter("This Month"), _ => !IsSyncing);
                YearFilterCommand = new RelayCommand(_ => ApplyFilter("This Year"), _ => !IsSyncing);
                AllTimeFilterCommand = new RelayCommand(_ => ApplyFilter("All Time"), _ => !IsSyncing);

                // Keep legacy RefreshCommand for backward compatibility
                RefreshCommand = SyncCommand;

                InitializeKpiCards();
                InitializeSalesmen();
                InitializeCharts();
                SubscribeToDataChanges();
                LoadData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardVM] Init Error: {ex.Message}");
            }
        }

        // ============ SUBSCRIBE TO DATA CHANGES ============
        private void SubscribeToDataChanges()
        {
            try
            {
                if (_piList?.AllInvoices != null)
                    _piList.AllInvoices.CollectionChanged += (s, e) => RefreshAll();
                if (_joList?.JobOrders != null)
                    _joList.JobOrders.CollectionChanged += (s, e) => RefreshAll();
                if (_delivery?.DeliveryOrders != null)
                    _delivery.DeliveryOrders.CollectionChanged += (s, e) => RefreshAll();
                if (_dailyWorks?.DailyWorks != null)
                    _dailyWorks.DailyWorks.CollectionChanged += (s, e) => RefreshAll();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardVM] Subscribe error: {ex.Message}");
            }
        }

        // ============ COMMANDS ============
        public ICommand RefreshCommand { get; private set; }
        public ICommand SyncCommand { get; private set; }
        public ICommand DayFilterCommand { get; private set; }
        public ICommand WeekFilterCommand { get; private set; }
        public ICommand MonthFilterCommand { get; private set; }
        public ICommand YearFilterCommand { get; private set; }
        public ICommand AllTimeFilterCommand { get; private set; }

        // ============ SYNC STATE PROPERTIES ============
        private bool _isSyncing;
        private bool _isSynced;
        private System.Windows.Threading.DispatcherTimer? _syncedTimer;

        public bool IsSyncing
        {
            get => _isSyncing;
            set
            {
                if (_isSyncing != value)
                {
                    _isSyncing = value;
                    if (value)
                    {
                        _isSynced = false;
                    }
                    OnSyncPropertiesChanged();
                }
            }
        }

        public string SyncButtonText
        {
            get
            {
                if (IsSyncing) return "Syncing...";
                if (_isSynced) return "Synced ✓";
                return "Sync";
            }
        }

        public string SyncButtonIcon
        {
            get
            {
                if (IsSyncing) return "↻";
                if (_isSynced) return "✓";
                return "⟳";
            }
        }

        public Brush SyncButtonBackground
        {
            get
            {
                if (IsSyncing) return Brushes.SteelBlue;
                if (_isSynced) return Brushes.MediumSeaGreen;
                return new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)); // #3B82F6
            }
        }

        // ===== Status Badge Properties =====
        public string StatusText
        {
            get
            {
                if (IsSyncing) return "SYNCING";
                if (_isSynced) return "SYNCED";
                return "LIVE";
            }
        }

        public string StatusTextColor
        {
            get
            {
                if (IsSyncing) return "#B45309";
                if (_isSynced) return "#065F46";
                return "#047857";
            }
        }

        public string StatusDotColor
        {
            get
            {
                if (IsSyncing) return "#F59E0B";
                if (_isSynced) return "#10B981";
                return "#10B981";
            }
        }

        public string StatusBadgeBg
        {
            get
            {
                if (IsSyncing) return "#FEF3C7";
                if (_isSynced) return "#D1FAE5";
                return "#ECFDF5";
            }
        }

        public string StatusBadgeBorder
        {
            get
            {
                if (IsSyncing) return "#F59E0B";
                if (_isSynced) return "#10B981";
                return "#10B981";
            }
        }

        private void OnSyncPropertiesChanged()
        {
            OnPropertyChanged(nameof(IsSyncing));
            OnPropertyChanged(nameof(SyncButtonText));
            OnPropertyChanged(nameof(SyncButtonIcon));
            OnPropertyChanged(nameof(SyncButtonBackground));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusTextColor));
            OnPropertyChanged(nameof(StatusDotColor));
            OnPropertyChanged(nameof(StatusBadgeBg));
            OnPropertyChanged(nameof(StatusBadgeBorder));
            CommandManager.InvalidateRequerySuggested();
        }

        // ============ ASYNC SYNC METHOD ============
        public async Task SyncDataAsync()
        {
            if (IsSyncing) return;
            try
            {
                IsSyncing = true;
                LastUpdate = "Syncing...";
                OnPropertyChanged(nameof(LastUpdate));

                // Run sync on background thread to keep UI responsive
                await Task.Run(() =>
                {
                    try
                    {
                        if (_piList != null)
                        {
                            try
                            {
                                var method = typeof(ProformaListVM).GetMethod("LoadInvoicesFromFolder",
                                    BindingFlags.NonPublic | BindingFlags.Instance);
                                if (method != null)
                                {
                                    var dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                                    if (Directory.Exists(dataFolder))
                                        method.Invoke(_piList, new object[] { dataFolder });
                                }
                            }
                            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DashboardVM] PI reload: {ex.Message}"); }
                        }

                        if (_joList != null)
                        {
                            try { _joList.RefreshCommand?.Execute(null); }
                            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DashboardVM] JO refresh: {ex.Message}"); }
                        }

                        if (_delivery != null)
                        {
                            try { _delivery.RefreshCommand?.Execute(null); }
                            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DashboardVM] Delivery refresh: {ex.Message}"); }
                        }

                        // Small delay to show the syncing state visually
                        System.Threading.Thread.Sleep(600);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DashboardVM] Sync background error: {ex.Message}");
                    }
                });

                // Update UI on UI thread
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    LoadLegacyData();
                    UpdateKpiCards();
                    InitializeCharts();
                    UpdateSalesmanList();
                    LastUpdate = $"Last update: {DateTime.Now:HH:mm:ss}";
                    RefreshAll();
                });

                // ✅ Show "Synced ✓" success state for 2.5 seconds
                IsSyncing = false;
                _isSynced = true;
                OnSyncPropertiesChanged();

                // Reset after 2.5 seconds using a DispatcherTimer
                _syncedTimer?.Stop();
                _syncedTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2.5)
                };
                _syncedTimer.Tick += (s, e) =>
                {
                    _syncedTimer?.Stop();
                    _isSynced = false;
                    OnSyncPropertiesChanged();
                };
                _syncedTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardVM] SyncDataAsync error: {ex.Message}");
                MessageBox.Show($"Sync failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                IsSyncing = false;
            }
        }

        // ============ KPI CARDS (LEGACY) ============
        public KPICard ProformaCard { get; set; } = new KPICard
        {
            Title = "Proforma Invoices",
            Icon = "📄",
            ColorKey = "Blue",
            ChangePercent = 0,
            ChangeLabel = "live data",
            Value = 0
        };
        public KPICard JobOrdersCard { get; set; } = new KPICard
        {
            Title = "Job Orders",
            Icon = "📋",
            ColorKey = "Green",
            ChangePercent = 0,
            ChangeLabel = "live data",
            Value = 0
        };
        public KPICard OptimizationCard { get; set; } = new KPICard
        {
            Title = "Avg Optimization",
            Icon = "⚡",
            ColorKey = "Purple",
            Suffix = "%",
            ChangePercent = 0,
            ChangeLabel = "live data",
            Value = 0
        };
        public KPICard DailyOutputCard { get; set; } = new KPICard
        {
            Title = "Daily Output (sqm)",
            Icon = "🏭",
            ColorKey = "Amber",
            ChangePercent = 0,
            ChangeLabel = "live data",
            Value = 0
        };
        public KPICard DeliveriesCard { get; set; } = new KPICard
        {
            Title = "Deliveries",
            Icon = "🚚",
            ColorKey = "Rose",
            ChangePercent = 0,
            ChangeLabel = "live data",
            Value = 0
        };
        public KPICard InventoryCard { get; set; } = new KPICard
        {
            Title = "Sheet Inventory",
            Icon = "📦",
            ColorKey = "Cyan",
            ChangePercent = 0,
            ChangeLabel = "live data",
            Value = 0
        };

        private void InitializeKpiCards() { }

        // ============ LEGACY COLLECTIONS ============
        public ObservableCollection<DailyWorkRecord> DailyWorkRecords { get; set; }
        public ObservableCollection<DeliveryRecord> DeliveryRecords { get; set; }

        // ============ DELIVERY PROPERTIES ============
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

        public double DeliveryProgressPercent => DeliveryTotalQty > 0
            ? (double)DeliveryTotalDelivered / DeliveryTotalQty * 100 : 0;

        // ============ CHARTS ============
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

        // ============ UI STATE ============
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

        // ============ HELPER: Get invoices safely ============
        private ObservableCollection<UiProforma> SafeInvoices =>
            _piList?.AllInvoices ?? EmptyInvoices;

        private ObservableCollection<DbJobOrder> SafeJobOrders =>
            _joList?.JobOrders ?? EmptyJobOrders;

        private ObservableCollection<UiDailyWork> SafeDailyWorks =>
            _dailyWorks?.DailyWorks ?? EmptyDailyWorks;

        // ============ REAL DATA KPI PROPERTIES ============

        public string TotalRevenue
        {
            get
            {
                try
                {
                    var total = GetFilteredInvoices()
                        .Where(i => i.Status == "Confirmed")
                        .Sum(i => (decimal)i.NetTotal);
                    return $"AED {total:N0}";
                }
                catch { return "AED 0"; }
            }
        }

        public string RevenueGrowth
        {
            get
            {
                try
                {
                    var all = SafeInvoices;
                    var thisMonth = all.Where(i => i.InvoiceDate.Month == DateTime.Today.Month
                                                && i.InvoiceDate.Year == DateTime.Today.Year
                                                && i.Status == "Confirmed")
                                       .Sum(i => (decimal)i.NetTotal);
                    var lastMonth = DateTime.Today.AddMonths(-1);
                    var prevMonth = all.Where(i => i.InvoiceDate.Month == lastMonth.Month
                                                && i.InvoiceDate.Year == lastMonth.Year
                                                && i.Status == "Confirmed")
                                       .Sum(i => (decimal)i.NetTotal);
                    if (prevMonth == 0) return thisMonth > 0 ? "+100%" : "0%";
                    var growth = (double)((thisMonth - prevMonth) / prevMonth * 100);
                    return $"{(growth >= 0 ? "+" : "")}{growth:N1}%";
                }
                catch { return "+0%"; }
            }
        }

        public string Orders => (_joList?.TotalJOCount ?? 0).ToString();

        public string OrdersGrowth
        {
            get
            {
                try
                {
                    var orders = SafeJobOrders;
                    var thisMonth = orders.Count(j => j.JODate.Month == DateTime.Today.Month && j.JODate.Year == DateTime.Today.Year);
                    var lastMonth = DateTime.Today.AddMonths(-1);
                    var prevMonth = orders.Count(j => j.JODate.Month == lastMonth.Month && j.JODate.Year == lastMonth.Year);
                    if (prevMonth == 0) return thisMonth > 0 ? "+100%" : "0%";
                    var growth = (double)((thisMonth - prevMonth) / (decimal)prevMonth * 100);
                    return $"{(growth >= 0 ? "+" : "")}{growth:N1}%";
                }
                catch { return "+0%"; }
            }
        }

        public string Salesmen
        {
            get
            {
                try
                {
                    var piSalesmen = SafeInvoices
                        .Where(i => !string.IsNullOrWhiteSpace(i.Salesman))
                        .Select(i => i.Salesman.Trim())
                        .Distinct();

                    var joSalesmen = SafeJobOrders
                        .Where(j => !string.IsNullOrWhiteSpace(j.Salesman))
                        .Select(j => j.Salesman.Trim())
                        .Distinct();

                    var dwSalesmen = SafeDailyWorks
                        .Where(d => !string.IsNullOrWhiteSpace(d.Salesman))
                        .Select(d => d.Salesman.Trim())
                        .Distinct();

                    return piSalesmen.Union(joSalesmen).Union(dwSalesmen).Count().ToString();
                }
                catch { return "0"; }
            }
        }

        public string PendingQuotes
        {
            get
            {
                try
                {
                    return SafeInvoices.Count(i =>
                        i.Status == "Sent" || i.Status == "Pending" || i.Status == "Draft").ToString();
                }
                catch { return "0"; }
            }
        }

        public string PITotal => SafeInvoices.Count.ToString();

        public string PIConfirmed => SafeInvoices.Count(i => i.Status == "Confirmed").ToString();

        public string PIPending => SafeInvoices.Count(i =>
            i.Status == "Sent" || i.Status == "Pending" || i.Status == "Draft" || i.Status == "Hold").ToString();

        public string PIValue
        {
            get
            {
                try
                {
                    var total = GetFilteredInvoices()
                        .Where(i => i.Status == "Confirmed")
                        .Sum(i => (decimal)i.NetTotal);
                    return $"AED {total:N0}";
                }
                catch { return "AED 0"; }
            }
        }

        public string JOTotal => (_joList?.TotalJOCount ?? 0).ToString();
        public string JOInProgress => (_joList?.InProgressCount ?? 0).ToString();
        public string JOCompleted => (_joList?.CompletedCount ?? 0).ToString();

        public string DelTotal => DeliveryCount.ToString();
        public string DelPending => (_delivery?.DeliveryOrders?.Count(d => d.Status == "Pending") ?? 0).ToString();
        public string DelCompleted => (_delivery?.DeliveryOrders?.Count(d => d.Status == "Completed") ?? 0).ToString();

        public string DailyBalance
        {
            get
            {
                try
                {
                    var total = SafeInvoices
                        .Where(i => i.InvoiceDate.Date == DateTime.Today && i.Status == "Confirmed")
                        .Sum(i => (decimal)i.NetTotal);
                    return $"AED {total:N0}";
                }
                catch { return "AED 0"; }
            }
        }

        public string MonthlyBalance
        {
            get
            {
                try
                {
                    var total = SafeInvoices
                        .Where(i => i.InvoiceDate.Month == DateTime.Today.Month
                                 && i.InvoiceDate.Year == DateTime.Today.Year
                                 && i.Status == "Confirmed")
                        .Sum(i => (decimal)i.NetTotal);
                    return $"AED {total:N0}";
                }
                catch { return "AED 0"; }
            }
        }

        public string AnnualBalance
        {
            get
            {
                try
                {
                    var total = SafeInvoices
                        .Where(i => i.InvoiceDate.Year == DateTime.Today.Year && i.Status == "Confirmed")
                        .Sum(i => (decimal)i.NetTotal);
                    return $"AED {total:N0}";
                }
                catch { return "AED 0"; }
            }
        }

        public bool HasData => SafeInvoices.Count > 0 || SafeJobOrders.Count > 0 || DeliveryCount > 0;

        public ObservableCollection<SalesmanData> SalesmanList { get; set; }

        // ============ LOAD DATA (Legacy - kept for initial load) ============
        public void LoadData()
        {
            try
            {
                if (_piList != null)
                {
                    try
                    {
                        var method = typeof(ProformaListVM).GetMethod("LoadInvoicesFromFolder",
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        if (method != null)
                        {
                            var dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                            if (Directory.Exists(dataFolder))
                                method.Invoke(_piList, new object[] { dataFolder });
                        }
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DashboardVM] PI reload: {ex.Message}"); }
                }

                if (_joList != null)
                {
                    try { _joList.RefreshCommand?.Execute(null); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DashboardVM] JO refresh: {ex.Message}"); }
                }

                if (_delivery != null)
                {
                    try { _delivery.RefreshCommand?.Execute(null); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DashboardVM] Delivery refresh: {ex.Message}"); }
                }

                LoadLegacyData();
                UpdateKpiCards();
                InitializeCharts();
                UpdateSalesmanList();
                LastUpdate = $"Last update: {DateTime.Now:HH:mm:ss}";

                RefreshAll();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardVM] LoadData error: {ex.Message}");
            }
        }

        private void LoadLegacyData()
        {
            try
            {
                DailyWorkRecords?.Clear();
                if (DailyWorkRecords != null)
                {
                    foreach (var w in SafeDailyWorks)
                    {
                        DailyWorkRecords.Add(new DailyWorkRecord
                        {
                            Id = w.Id,
                            Date = w.Date,
                            Company = w.Company ?? "",
                            PiNumber = w.PiNumber ?? "",
                            Qty = w.Qty,
                            Sqm = w.Sqm,
                            Status = w.Status ?? "",
                            Salesman = w.Salesman ?? "",
                            Color = w.Color ?? ""
                        });
                    }
                }

                DeliveryRecords?.Clear();
                if (DeliveryRecords != null && _delivery?.DeliveryOrders != null)
                {
                    int totalQty = 0, totalDelivered = 0, totalReturned = 0, totalBalance = 0;
                    foreach (var d in _delivery.DeliveryOrders)
                    {
                        var delivered = d.TotalDelivered;
                        var returned = d.TotalReturned;
                        var balance = d.OrderQty - delivered + returned;

                        totalQty += d.OrderQty;
                        totalDelivered += delivered;
                        totalReturned += returned;
                        totalBalance += balance;

                        DeliveryRecords.Add(new DeliveryRecord
                        {
                            Id = d.Id,
                            Date = d.Date,
                            Company = d.Company ?? "",
                            PINumber = d.PINumber ?? "",
                            OrderQty = d.OrderQty,
                            TotalDelivered = delivered,
                            TotalReturned = returned,
                            Balance = balance,
                            Status = d.Status ?? ""
                        });
                    }

                    DeliveryCount = _delivery.DeliveryOrders.Count;
                    DeliveryTotalQty = totalQty;
                    DeliveryTotalDelivered = totalDelivered;
                    DeliveryTotalReturned = totalReturned;
                    DeliveryTotalBalance = totalBalance;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardVM] LoadLegacyData error: {ex.Message}");
            }
        }

        private void UpdateKpiCards()
        {
            try
            {
                JobOrdersCard.Value = _joList?.TotalJOCount ?? 0;
                ProformaCard.Value = SafeInvoices.Count;
                DeliveriesCard.Value = DeliveryCount;
                DailyOutputCard.Value = (int)DeliveryTotalBalance;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardVM] UpdateKpiCards error: {ex.Message}");
            }
        }

        private void UpdateSalesmanList()
        {
            try
            {
                SalesmanList?.Clear();
                if (SalesmanList == null) return;

                List<SalesmanTotal> salesmen;
                try
                {
                    salesmen = SafeInvoices
                        .Where(i => i.Status == "Confirmed" && !string.IsNullOrWhiteSpace(i.Salesman))
                        .GroupBy(i => i.Salesman.Trim())
                        .Select(g => new SalesmanTotal { Name = g.Key, Total = g.Sum(i => (decimal)i.NetTotal) })
                        .OrderByDescending(x => x.Total)
                        .Take(9)
                        .ToList();
                }
                catch
                {
                    salesmen = EmptySalesmenList;
                }

                if (salesmen.Any())
                {
                    foreach (var s in salesmen)
                    {
                        SalesmanList.Add(new SalesmanData { Name = s.Name, Amount = $"AED {s.Total:N0}" });
                    }
                }
                else
                {
                    InitializeSalesmen();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardVM] UpdateSalesmanList error: {ex.Message}");
            }
        }

        private void InitializeSalesmen()
        {
            SalesmanList ??= new ObservableCollection<SalesmanData>();
            SalesmanList.Clear();
            var names = new[] { "Mr Pradeep", "Mr Sooraj", "Ms Maya", "Mr Aftab", "Mr Talha", "Mr Bilal", "Mr Sunny", "Mr Harvinder", "Mr Nazim" };
            foreach (var name in names)
                SalesmanList.Add(new SalesmanData { Name = name, Amount = "AED 0" });
        }

        // ============ APPLY DATE FILTER ============
        private void ApplyFilter(string filter)
        {
            try
            {
                SelectedFilter = filter;
                DayFilterBg = "#F3F4F6"; WeekFilterBg = "#F3F4F6";
                MonthFilterBg = "#F3F4F6"; YearFilterBg = "#F3F4F6";
                DayFilterFg = "#1F2937"; WeekFilterFg = "#1F2937";
                MonthFilterFg = "#1F2937"; YearFilterFg = "#1F2937";

                switch (filter)
                {
                    case "Today":
                        DayFilterBg = "#2563EB"; DayFilterFg = "#FFFFFF";
                        _filterStartDate = DateTime.Today;
                        _filterEndDate = DateTime.Today;
                        break;
                    case "This Week":
                        WeekFilterBg = "#2563EB"; WeekFilterFg = "#FFFFFF";
                        _filterStartDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                        _filterEndDate = DateTime.Today;
                        break;
                    case "This Month":
                        MonthFilterBg = "#2563EB"; MonthFilterFg = "#FFFFFF";
                        _filterStartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                        _filterEndDate = DateTime.Today;
                        break;
                    case "This Year":
                        YearFilterBg = "#2563EB"; YearFilterFg = "#FFFFFF";
                        _filterStartDate = new DateTime(DateTime.Today.Year, 1, 1);
                        _filterEndDate = DateTime.Today;
                        break;
                    case "All Time":
                        _filterStartDate = null;
                        _filterEndDate = null;
                        break;
                }

                OnPropertyChanged(nameof(DayFilterBg)); OnPropertyChanged(nameof(WeekFilterBg));
                OnPropertyChanged(nameof(MonthFilterBg)); OnPropertyChanged(nameof(YearFilterBg));
                OnPropertyChanged(nameof(DayFilterFg)); OnPropertyChanged(nameof(WeekFilterFg));
                OnPropertyChanged(nameof(MonthFilterFg)); OnPropertyChanged(nameof(YearFilterFg));
                OnPropertyChanged(nameof(SelectedFilter));

                RefreshAll();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardVM] ApplyFilter error: {ex.Message}");
            }
        }

        // ============ HELPER: Get filtered invoices ============
        private IEnumerable<UiProforma> GetFilteredInvoices()
        {
            try
            {
                var query = SafeInvoices.AsEnumerable();
                if (_filterStartDate.HasValue)
                    query = query.Where(i => i.InvoiceDate >= _filterStartDate.Value);
                if (_filterEndDate.HasValue)
                    query = query.Where(i => i.InvoiceDate <= _filterEndDate.Value.AddDays(1));
                return query.ToList();
            }
            catch
            {
                return Enumerable.Empty<UiProforma>();
            }
        }

        // ============ INITIALIZE CHARTS ============
        private void InitializeCharts()
        {
            try
            {
                var dates = Enumerable.Range(0, 7)
                    .Select(i => DateTime.Today.AddDays(-i))
                    .Reverse()
                    .ToList();

                var deliveryByDay = dates.Select(d =>
                    (double)(_delivery?.DeliveryOrders?
                        .Where(x => x.Date.Date == d.Date)
                        .Sum(x => x.OrderQty) ?? 0)).ToArray();

                var joByDay = dates.Select(d =>
                    (double)(_joList?.JobOrders?
                        .Where(x => x.JODate.Date == d.Date)
                        .Sum(x => x.TotalQty) ?? 0)).ToArray();

                DailyTrendSeries = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Name = "Delivery Qty",
                        Values = deliveryByDay,
                        Fill = new SolidColorPaint(new SKColor(59, 130, 246, 30)),
                        Stroke = new SolidColorPaint(new SKColor(59, 130, 246), 3),
                        GeometrySize = 6
                    },
                    new LineSeries<double>
                    {
                        Name = "Job Order Qty",
                        Values = joByDay,
                        Stroke = new SolidColorPaint(new SKColor(16, 185, 129), 2),
                        GeometrySize = 4
                    }
                };
                DailyTrendXAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = dates.Select(d => d.ToString("ddd")).ToArray(),
                        LabelsPaint = new SolidColorPaint(SKColors.Gray)
                    }
                };
                DailyTrendYAxes = new Axis[] { new Axis { LabelsPaint = new SolidColorPaint(SKColors.Gray) } };

                List<SalesmanTotal> topSalesmen;
                try
                {
                    topSalesmen = SafeInvoices
                        .Where(i => i.Status == "Confirmed" && !string.IsNullOrWhiteSpace(i.Salesman))
                        .GroupBy(i => i.Salesman.Trim())
                        .Select(g => new SalesmanTotal { Name = g.Key, Total = (decimal)g.Sum(i => i.NetTotal) })
                        .OrderByDescending(x => x.Total)
                        .Take(9)
                        .ToList();
                }
                catch
                {
                    topSalesmen = EmptySalesmenList;
                }

                if (topSalesmen.Any())
                {
                    SalesmanChartSeries = new ISeries[]
                    {
                        new ColumnSeries<double>
                        {
                            Name = "Revenue",
                            Values = topSalesmen.Select(s => (double)s.Total).ToArray(),
                            Fill = new SolidColorPaint(new SKColor(139, 92, 246)),
                            MaxBarWidth = 30
                        }
                    };
                    SalesmanChartXAxes = new Axis[]
                    {
                        new Axis
                        {
                            Labels = topSalesmen.Select(s => GetShortName(s.Name)).ToArray(),
                            LabelsPaint = new SolidColorPaint(SKColors.Gray)
                        }
                    };
                    SalesmanChartYAxes = new Axis[] { new Axis { LabelsPaint = new SolidColorPaint(SKColors.Gray) } };
                }
                else
                {
                    SalesmanChartSeries = new ISeries[]
                    {
                        new ColumnSeries<double> { Values = new double[] { 0 }, Fill = new SolidColorPaint(SKColors.LightGray) }
                    };
                    SalesmanChartXAxes = new Axis[] { new Axis { Labels = new[] { "No data" } } };
                    SalesmanChartYAxes = new Axis[] { new Axis() };
                }

                List<TypeTotal> typeGroups;
                try
                {
                    typeGroups = SafeDailyWorks
                        .Where(d => !string.IsNullOrWhiteSpace(d.TypeOfWork))
                        .GroupBy(d => d.TypeOfWork.Trim())
                        .Select(g => new TypeTotal { Type = g.Key, Qty = g.Sum(d => (double)d.Qty) })
                        .OrderByDescending(x => x.Qty)
                        .Take(5)
                        .ToList();
                }
                catch
                {
                    typeGroups = EmptyTypeList;
                }

                if (typeGroups.Any())
                {
                    var colors = new[] {
                        new SKColor(59, 130, 246), new SKColor(16, 185, 129),
                        new SKColor(139, 92, 246), new SKColor(245, 158, 11),
                        new SKColor(239, 68, 68)
                    };
                    ProductMixSeries = typeGroups.Select((g, i) => new PieSeries<double>
                    {
                        Name = g.Type,
                        Values = new[] { g.Qty },
                        Fill = new SolidColorPaint(colors[i % colors.Length]),
                        InnerRadius = 60
                    }).Cast<ISeries>().ToArray();
                }
                else
                {
                    ProductMixSeries = new ISeries[]
                    {
                        new PieSeries<double> { Name = "No data", Values = new[] { 1.0 }, Fill = new SolidColorPaint(SKColors.LightGray), InnerRadius = 60 }
                    };
                }

                MachineUtilSeries = new ISeries[]
                {
                    new RowSeries<int>
                    {
                        Values = new[] { 0, 0, 0, 0, 0, 0 },
                        Fill = new SolidColorPaint(new SKColor(139, 92, 246)),
                        MaxBarWidth = 20
                    }
                };
                MachineUtilXAxes = new Axis[] { new Axis { MaxLimit = 100, LabelsPaint = new SolidColorPaint(SKColors.Gray) } };
                MachineUtilYAxes = new Axis[]
                {
                    new Axis { Labels = new[] { "CNC-01", "CNC-02", "Temper-01", "Lam-01", "IG-01", "Polish-01" }, LabelsPaint = new SolidColorPaint(SKColors.Gray) }
                };

                var pending = _delivery?.DeliveryOrders?.Count(d => d.Status == "Pending") ?? 0;
                var completed = _delivery?.DeliveryOrders?.Count(d => d.Status == "Completed") ?? 0;
                var partial = _delivery?.DeliveryOrders?.Count(d => d.Status == "Partially Delivered") ?? 0;

                DeliveryStatusSeries = new ISeries[]
                {
                    new PieSeries<double> { Name = "Completed", Values = new[] { (double)completed }, Fill = new SolidColorPaint(new SKColor(16, 185, 129)), InnerRadius = 60 },
                    new PieSeries<double> { Name = "Partial", Values = new[] { (double)partial }, Fill = new SolidColorPaint(new SKColor(245, 158, 11)), InnerRadius = 60 },
                    new PieSeries<double> { Name = "Pending", Values = new[] { (double)pending }, Fill = new SolidColorPaint(new SKColor(239, 68, 68)), InnerRadius = 60 }
                };

                OptimizationSeries = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Name = "Efficiency %",
                        Values = new double[] { 0, 0, 0, 0, 0, 0, 0, 0 },
                        Stroke = new SolidColorPaint(new SKColor(139, 92, 246), 3),
                        Fill = null
                    }
                };
                OptimizationXAxes = new Axis[] { new Axis { LabelsPaint = new SolidColorPaint(SKColors.Gray) } };
                OptimizationYAxes = new Axis[] { new Axis { MinLimit = 0, MaxLimit = 100, LabelsPaint = new SolidColorPaint(SKColors.Gray) } };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardVM] InitializeCharts error: {ex.Message}");
            }
        }

        private string GetShortName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return "?";
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1) return parts[parts.Length - 1].Substring(0, Math.Min(6, parts[parts.Length - 1].Length));
            return parts[0].Substring(0, Math.Min(6, parts[0].Length));
        }

        // ============ REFRESH ALL ============
        public void RefreshAll()
        {
            try
            {
                OnPropertyChanged(nameof(TotalRevenue));
                OnPropertyChanged(nameof(RevenueGrowth));
                OnPropertyChanged(nameof(Orders));
                OnPropertyChanged(nameof(OrdersGrowth));
                OnPropertyChanged(nameof(Salesmen));
                OnPropertyChanged(nameof(PendingQuotes));
                OnPropertyChanged(nameof(PITotal));
                OnPropertyChanged(nameof(PIConfirmed));
                OnPropertyChanged(nameof(PIPending));
                OnPropertyChanged(nameof(PIValue));
                OnPropertyChanged(nameof(JOTotal));
                OnPropertyChanged(nameof(JOInProgress));
                OnPropertyChanged(nameof(JOCompleted));
                OnPropertyChanged(nameof(DelTotal));
                OnPropertyChanged(nameof(DelPending));
                OnPropertyChanged(nameof(DelCompleted));
                OnPropertyChanged(nameof(DailyBalance));
                OnPropertyChanged(nameof(MonthlyBalance));
                OnPropertyChanged(nameof(AnnualBalance));
                OnPropertyChanged(nameof(LastUpdate));
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(SalesmanList));
                OnPropertyChanged(nameof(DeliveryTotalQty));
                OnPropertyChanged(nameof(DeliveryTotalDelivered));
                OnPropertyChanged(nameof(DeliveryTotalReturned));
                OnPropertyChanged(nameof(DeliveryTotalBalance));
                OnPropertyChanged(nameof(DeliveryProgressPercent));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardVM] RefreshAll error: {ex.Message}");
            }
        }

        public void Sync() => LoadData();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // ==================== HELPER CLASSES ====================
    public class SalesmanData
    {
        public string Name { get; set; } = "";
        public string Amount { get; set; } = "";
    }

    public class SalesmanTotal
    {
        public string Name { get; set; } = "";
        public decimal Total { get; set; }
    }

    public class TypeTotal
    {
        public string Type { get; set; } = "";
        public double Qty { get; set; }
    }
}