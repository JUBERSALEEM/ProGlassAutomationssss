using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ProGlassAutomation.ViewModels;
using ProGlassAutomation.Services;
using ProGlassAutomation.Data.Database;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Views.Dashboard
{
    public partial class DashboardView : UserControl, IDisposable
    {
        #region Fields

        // Timers
        private DispatcherTimer _liveTimer;
        private DispatcherTimer _clockTimer;
        private DispatcherTimer _broadcastTimer;
        private DispatcherTimer _balanceSyncTimer;
        private DispatcherTimer _moduleStatsTimer;

        // Live Services
        private LiveDataService _liveDataService;
        private SignalRService _signalRService;
        private WebSocketService _webSocketService;
        private BalanceService _balanceService;

        // ViewModels for Module Integration
        private SheetStoreViewModel _sheetStoreVM;
        private DailyWorksViewModel _dailyWorksVM;
        private DeliveryViewModel _deliveriesVM;

        // Activity Log
        private ObservableCollection<ActivityItem> _activityLog;

        // State
        private bool _isLicensed;
        private bool _isConnected;
        private bool _isDisposed;

        // Current values
        private double _currentProduction;
        private double _currentEfficiency = 94.2;
        private double _currentUptime = 99.8;

        #endregion

        #region Constructor

        public DashboardView()
        {
            InitializeComponent();

            // Initialize activity log
            _activityLog = new ObservableCollection<ActivityItem>();
            ActivityLogList.ItemsSource = _activityLog;

            // Initialize ViewModels
            InitializeViewModels();

            // Initialize services
            InitializeServices();

            // Setup timers
            SetupTimers();

            // Load initial state
            UpdateSubscriptionStatus();

            // Start services
            StartServices();

            // Log startup
            AddActivity("Dashboard loaded", "System");
        }

        #endregion

        #region ViewModel Initialization

        private void InitializeViewModels()
        {
            try
            {
                _sheetStoreVM = new SheetStoreViewModel();
                _dailyWorksVM = new DailyWorksViewModel();
                _deliveriesVM = new DeliveryViewModel();
                AddActivity("Module ViewModels initialized", "System");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] ViewModel init error: {ex.Message}");
                AddActivity($"ViewModel init failed: {ex.Message}", "Error");
            }
        }

        #endregion

        #region Initialization

        private void InitializeServices()
        {
            _liveDataService = new LiveDataService();
            _liveDataService.OnProductionUpdated += OnProductionUpdated;
            _liveDataService.OnMetricsUpdated += OnMetricsUpdated;
            _liveDataService.OnModuleStatusChanged += OnModuleStatusChanged;
            _liveDataService.OnLiveCounterUpdated += OnLiveCounterUpdated;
            _liveDataService.OnError += OnServiceError;

            _signalRService = new SignalRService();
            _signalRService.OnMessageReceived += OnSignalRMessageReceived;
            _signalRService.OnConnectionStateChanged += OnSignalRConnectionChanged;
            _signalRService.OnError += OnServiceError;

            _webSocketService = new WebSocketService();
            _webSocketService.OnMessageReceived += OnWebSocketMessageReceived;
            _webSocketService.OnConnectionStateChanged += OnWebSocketConnectionChanged;
            _webSocketService.OnError += OnServiceError;
            _webSocketService.OnLogMessage += OnServiceLog;

            _balanceService = new BalanceService();
            _balanceService.OnBalanceUpdated += OnBalanceUpdated;
            _balanceService.OnTransactionRecorded += OnTransactionRecorded;
            _balanceService.OnError += OnServiceError;
        }

        private void SetupTimers()
        {
            _liveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _liveTimer.Tick += OnLiveTimerTick;

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += OnClockTimerTick;

            _broadcastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            _broadcastTimer.Tick += OnBroadcastTimerTick;

            _balanceSyncTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _balanceSyncTimer.Tick += OnBalanceSyncTimerTick;

            _moduleStatsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _moduleStatsTimer.Tick += OnModuleStatsTimerTick;
        }

        private void StartServices()
        {
            _liveDataService.Start();
            _balanceService.Start();
            _ = ConnectToSignalRAsync();

            _clockTimer.Start();
            _liveTimer.Start();
            _broadcastTimer.Start();
            _balanceSyncTimer.Start();
            _moduleStatsTimer.Start();

            RefreshDashboardData();
            RefreshBalanceData();
            RefreshModuleStats();

            AddActivity("All services started", "System");
        }

        #endregion

        #region Timer Events

        private void OnLiveTimerTick(object sender, EventArgs e)
        {
            var metrics = _liveDataService.GetCurrentMetrics();
            UpdateProductionDisplay(metrics.TotalProduction);
            UpdateEfficiencyDisplay(metrics.Efficiency);
            UpdateUptimeDisplay(metrics.Uptime);
            UpdateModuleCount(metrics);
            LastUpdateText.Text = $"Last update: {DateTime.Now:HH:mm:ss}";
            LastSyncText.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void OnClockTimerTick(object sender, EventArgs e)
        {
            LastUpdateText.Text = $"Last update: {DateTime.Now:HH:mm:ss}";
        }

        private async void OnBroadcastTimerTick(object sender, EventArgs e)
        {
            if (_signalRService.IsConnected)
            {
                var metrics = _liveDataService.GetCurrentMetrics();
                await _signalRService.BroadcastMetricsAsync(metrics);
            }
        }

        private void OnBalanceSyncTimerTick(object sender, EventArgs e)
        {
            RefreshBalanceData();
            LastSyncText.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void OnModuleStatsTimerTick(object sender, EventArgs e)
        {
            RefreshModuleStats();
        }

        #endregion

        #region Module Stats Updates

        private void RefreshModuleStats()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    UpdateSheetStoreStats();
                    UpdateDailyWorksStats();
                    UpdateDeliveriesStats();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] Module stats error: {ex.Message}");
            }
        }

        private void UpdateSheetStoreStats()
        {
            if (_sheetStoreVM == null) return;

            try
            {
                SheetTotalStock.Text = _sheetStoreVM.TotalStock.ToString("N0");
                SheetBalance.Text = _sheetStoreVM.BalanceSheets.ToString("N0");
                SheetTypes.Text = _sheetStoreVM.TotalSheets.ToString("N0");
                SheetValue.Text = $"AED {_sheetStoreVM.TotalAll:N0}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] SheetStore update error: {ex.Message}");
            }
        }

        private void UpdateDailyWorksStats()
        {
            if (_dailyWorksVM == null) return;

            try
            {
                var today = DateTime.Today;
                var todayWorks = _dailyWorksVM.DailyWorks?
                    .Where(w => w.Date.Date == today)
                    .ToList() ?? new List<DailyWork>();

                WorksTodayCount.Text = todayWorks.Count.ToString("N0");
                WorksTodaySQM.Text = todayWorks.Sum(w => w.SQM).ToString("N1");
                WorksTotalRecords.Text = _dailyWorksVM.TotalRecords.ToString("N0");
                WorksTotalSQM.Text = _dailyWorksVM.TotalSQM.ToString("N1");
                LiveProductionText.Text = todayWorks.Sum(w => w.SQM).ToString("N1");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] DailyWorks update error: {ex.Message}");
            }
        }

        private void UpdateDeliveriesStats()
        {
            if (_deliveriesVM == null) return;

            try
            {
                var pendingCount = _deliveriesVM.DeliveryOrders?
                    .Count(d => d.Status == "Pending" || d.Status == "Partially Delivered") ?? 0;
                var completedCount = _deliveriesVM.DeliveryOrders?
                    .Count(d => d.Status == "Completed") ?? 0;

                DeliveriesPending.Text = pendingCount.ToString("N0");
                DeliveriesCompleted.Text = completedCount.ToString("N0");
                DeliveriesTotal.Text = _deliveriesVM.TotalRecords.ToString("N0");

                var totalBalance = _deliveriesVM.DeliveryOrders?
                    .Sum(d => d.Balance) ?? 0;
                DeliveriesBalance.Text = totalBalance.ToString("N0");

                PendingDeliveriesText.Text = pendingCount.ToString();
                CompletedTodayText.Text = completedCount.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] Deliveries update error: {ex.Message}");
            }
        }

        #endregion

        #region Live Data Service Events

        private void OnProductionUpdated(object sender, double production)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateProductionDisplay(production);
                AddActivity($"Production: {production:N0} sqm", "Production");
            });
        }

        private void OnMetricsUpdated(object sender, LiveMetrics metrics)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateProductionDisplay(metrics.TotalProduction);
                UpdateEfficiencyDisplay(metrics.Efficiency);
                UpdateUptimeDisplay(metrics.Uptime);
                UpdateModuleCount(metrics);
            });
        }

        private void OnModuleStatusChanged(object sender, (string Module, bool IsActive) e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateModuleStatus(e.Module, e.IsActive);
                AddActivity($"Module {e.Module}: {(e.IsActive ? "Activated" : "Deactivated")}", "System");
            });
        }

        private void OnLiveCounterUpdated(object sender, (string Module, int Count) e)
        {
            Dispatcher.Invoke(() =>
            {
                switch (e.Module)
                {
                    case "SGU":
                        SguCountText.Text = e.Count.ToString();
                        SguLiveCounter.Text = $" Live: {e.Count}/min";
                        break;
                    case "DGU":
                        DguCountText.Text = e.Count.ToString();
                        DguLiveCounter.Text = $" Live: {e.Count}/min";
                        break;
                    case "Lamination":
                        LamCountText.Text = e.Count.ToString();
                        LamLiveCounter.Text = $" Live: {e.Count}/min";
                        break;
                }
            });
        }

        private void OnServiceError(object sender, string error)
        {
            Dispatcher.Invoke(() => AddActivity($"Error: {error}", "Error"));
        }

        #endregion

        #region Balance Service Events

        private void OnBalanceUpdated(object sender, BalanceData balance)
        {
            Dispatcher.Invoke(() =>
            {
                DailyBalanceText.Text = $"AED {balance.DailyBalance:N2}";
                MonthlyBalanceText.Text = $"AED {balance.MonthlyBalance:N2}";
                AnnualBalanceText.Text = $"AED {balance.AnnualBalance:N2}";
                TodayRevenueText.Text = $"AED {balance.TodayRevenue:N2}";
                TodayExpensesText.Text = $"AED {balance.TodayExpenses:N2}";
                NetIncomeText.Text = $"AED {balance.NetIncome:N2}";
                TransactionsText.Text = $"{balance.TransactionCount} today";
                PendingDeliveriesText.Text = balance.PendingDeliveries.ToString();
                CompletedTodayText.Text = balance.CompletedToday.ToString();
                UpdateBalanceServiceStatus(true);
            });
        }

        private void OnTransactionRecorded(object sender, TransactionInfo transaction)
        {
            Dispatcher.Invoke(() => AddActivity($"Transaction: {transaction.Type} - AED {transaction.Amount:N2}", "Finance"));
        }

        private void UpdateBalanceServiceStatus(bool isActive)
        {
            if (isActive)
            {
                var green = (Color)ColorConverter.ConvertFromString("#10B981");
                BalanceServiceIndicator.Fill = new SolidColorBrush(green);
                BalanceServiceStatusText.Text = "Online";
                BalanceServiceStatusText.Foreground = new SolidColorBrush(green);
            }
            else
            {
                var red = (Color)ColorConverter.ConvertFromString("#EF4444");
                BalanceServiceIndicator.Fill = new SolidColorBrush(red);
                BalanceServiceStatusText.Text = "Offline";
                BalanceServiceStatusText.Foreground = new SolidColorBrush(red);
            }
        }

        #endregion

        #region SignalR Service Events

        private void OnSignalRMessageReceived(object sender, LiveDataMessage message)
        {
            Dispatcher.Invoke(() =>
            {
                switch (message.Type)
                {
                    case "ProductionUpdate":
                        UpdateProductionDisplay(message.Production);
                        break;
                    case "EfficiencyUpdate":
                        UpdateEfficiencyDisplay(message.Efficiency);
                        break;
                    case "MetricsUpdate":
                        UpdateProductionDisplay(message.Production);
                        UpdateEfficiencyDisplay(message.Efficiency);
                        UpdateUptimeDisplay(message.Uptime);
                        break;
                    case "ModuleStatusChange":
                        UpdateModuleStatus(message.ModuleName, message.IsActive);
                        break;
                    case "LicenseChange":
                        if (message.IsLicensed != _isLicensed)
                        {
                            _isLicensed = message.IsLicensed;
                            UpdateSubscriptionStatus();
                            AddActivity($"License: {(_isLicensed ? "Active" : "Inactive")}", "System");
                        }
                        break;
                    case "BalanceUpdate":
                        RefreshBalanceData();
                        break;
                    case "SheetStoreUpdate":
                    case "DailyWorksUpdate":
                    case "DeliveriesUpdate":
                        RefreshModuleStats();
                        break;
                }
            });
        }

        private void OnSignalRConnectionChanged(object sender, bool isConnected)
        {
            Dispatcher.Invoke(() =>
            {
                _isConnected = isConnected;
                UpdateConnectionStatus(isConnected);
                AddActivity($"SignalR: {(isConnected ? "Connected" : "Disconnected")}", "Connection");
            });
        }

        #endregion

        #region WebSocket Service Events

        private void OnWebSocketMessageReceived(object sender, LiveDataMessage message)
        {
            OnSignalRMessageReceived(sender, message);
        }

        private void OnWebSocketConnectionChanged(object sender, bool isConnected)
        {
            System.Diagnostics.Debug.WriteLine($"[WebSocket] Connection: {(isConnected ? "Connected" : "Disconnected")}");
        }

        private void OnServiceLog(object sender, string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

        #endregion

        #region UI Updates

        private void UpdateProductionDisplay(double production)
        {
            _currentProduction = production;
            LiveProductionText.Text = production.ToString("N0");
            AnimateTextChange(LiveProductionText);
        }

        private void UpdateEfficiencyDisplay(double efficiency)
        {
            _currentEfficiency = Math.Clamp(efficiency, 0, 100);
            EfficiencyText.Text = _currentEfficiency.ToString("N1");
            EfficiencyBar.Value = _currentEfficiency;
        }

        private void UpdateUptimeDisplay(double uptime)
        {
            _currentUptime = Math.Clamp(uptime, 0, 100);
            UptimeText.Text = _currentUptime.ToString("N1");
        }

        private void UpdateModuleCount(LiveMetrics metrics)
        {
            int activeCount = 0;
            string[] modules = { "SGU", "DGU", "Lamination", "DguLam", "Optimization" };
            foreach (var module in modules)
            {
                if (metrics.GetModuleStatus(module))
                    activeCount++;
            }
            ModuleCountText.Text = $"{activeCount}/5 Active";

            int totalCalcs = metrics.SguCount + metrics.DguCount + metrics.LamCount;
            CalculationsText.Text = totalCalcs.ToString();
        }

        private void UpdateModuleStatus(string moduleName, bool isActive)
        {
            var green = (Color)ColorConverter.ConvertFromString("#10B981");
            var greenBg = (Color)ColorConverter.ConvertFromString("#D1FAE5");
            var red = (Color)ColorConverter.ConvertFromString("#EF4444");
            var redBg = (Color)ColorConverter.ConvertFromString("#FEE2E2");

            (Border statusBorder, TextBlock statusText) = moduleName switch
            {
                "SGU" => (SguStatus, SguStatusText),
                "DGU" => (DguStatus, DguStatusText),
                "Lamination" => (LamStatus, LamStatusText),
                "DguLam" => (DguLamStatus, DguLamStatusText),
                "Optimization" => (OptStatus, OptStatusText),
                _ => (null, null)
            };

            if (statusBorder != null && statusText != null)
            {
                var text = isActive ? "✅ LIVE" : "🔒 LOCKED";
                var textColor = isActive ? green : red;
                var bgColor = isActive ? greenBg : redBg;

                statusBorder.Background = new SolidColorBrush(bgColor);
                statusText.Text = text;
                statusText.Foreground = new SolidColorBrush(textColor);
            }
        }

        private void UpdateConnectionStatus(bool isConnected)
        {
            if (isConnected)
            {
                var green = (Color)ColorConverter.ConvertFromString("#10B981");
                ConnectionIndicator.Fill = new SolidColorBrush(green);
                ConnectionStatusText.Text = "Live";
                ConnectionStatusText.Foreground = new SolidColorBrush(green);
            }
            else
            {
                var red = (Color)ColorConverter.ConvertFromString("#EF4444");
                ConnectionIndicator.Fill = new SolidColorBrush(red);
                ConnectionStatusText.Text = "Offline";
                ConnectionStatusText.Foreground = new SolidColorBrush(red);
            }
        }

        private void RefreshDashboardData()
        {
            try
            {
                var stats = DbHelper.GetDashboardStats();

                UpdateProductionDisplay(stats.TotalProduction);
                UpdateEfficiencyDisplay(stats.Efficiency);
                UpdateUptimeDisplay(stats.AverageUptime);

                // Update module totals
                SguTotalText.Text = $"SGU: {DbHelper.GetTodayTotalSGU():N0} sqm";
                DguTotalText.Text = $"DGU: {DbHelper.GetTodayTotalDGU():N0} sqm";
                LamTotalText.Text = $"Lam: {DbHelper.GetTodayTotalLamination():N0} sqm";

                // Update calculation counts
                SguCountText.Text = DbHelper.GetTodayCalculationCount("SGU").ToString();
                DguCountText.Text = DbHelper.GetTodayCalculationCount("DGU").ToString();
                LamCountText.Text = DbHelper.GetTodayCalculationCount("Lamination").ToString();

                // Update module count based on license
                int activeModules = _isLicensed ? 5 : 0;
                ModuleCountText.Text = $"{activeModules}/5 Active";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] Refresh error: {ex.Message}");
            }
        }

        private void RefreshBalanceData()
        {
            try
            {
                var balance = _balanceService.GetCurrentBalance();

                DailyBalanceText.Text = $"AED {balance.DailyBalance:N2}";
                MonthlyBalanceText.Text = $"AED {balance.MonthlyBalance:N2}";
                AnnualBalanceText.Text = $"AED {balance.AnnualBalance:N2}";
                TodayRevenueText.Text = $"AED {balance.TodayRevenue:N2}";
                TodayExpensesText.Text = $"AED {balance.TodayExpenses:N2}";
                NetIncomeText.Text = $"AED {balance.NetIncome:N2}";
                TransactionsText.Text = $"{balance.TransactionCount} today";
                PendingDeliveriesText.Text = balance.PendingDeliveries.ToString();
                CompletedTodayText.Text = balance.CompletedToday.ToString();

                UpdateBalanceServiceStatus(true);
                LastSyncText.Text = DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] Balance refresh error: {ex.Message}");
                UpdateBalanceServiceStatus(false);
            }
        }

        #endregion

        #region Activity Log

        private void AddActivity(string message, string category)
        {
            Dispatcher.Invoke(() =>
            {
                var item = new ActivityItem
                {
                    Message = $"[{category}] {message}",
                    Time = DateTime.Now.ToString("HH:mm:ss")
                };

                _activityLog.Insert(0, item);

                while (_activityLog.Count > 50)
                {
                    _activityLog.RemoveAt(_activityLog.Count - 1);
                }

                NoActivityText.Visibility = _activityLog.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            });
        }

        #endregion

        #region Animations

        private void AnimateTextChange(TextBlock textBlock)
        {
            if (textBlock.RenderTransform is ScaleTransform)
                return;

            var scaleTransform = new ScaleTransform(1, 1);
            textBlock.RenderTransform = scaleTransform;
            textBlock.RenderTransformOrigin = new Point(0.5, 0.5);

            var animation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.12,
                Duration = TimeSpan.FromMilliseconds(200),
                AutoReverse = true,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            timer.Tick += (s, e) =>
            {
                textBlock.RenderTransform = null;
                timer.Stop();
            };
            timer.Start();
        }

        private void PulseAnimation(Border border)
        {
            var scaleTransform = new ScaleTransform(1, 1);
            border.RenderTransform = scaleTransform;
            border.RenderTransformOrigin = new Point(0.5, 0.5);

            var animation = new DoubleAnimation(1.0, 1.06, TimeSpan.FromSeconds(0.3))
            {
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(2),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }

        #endregion

        #region Subscription Status

        private bool CheckLicenseStatus()
        {
            try
            {
                string storedKey = GetStoredKey();
                if (string.IsNullOrEmpty(storedKey)) return false;

                string machineId = MachineIdService.Instance.GetMachineId();
                string errorMsg = "";
                return KeyGeneratorService.Instance.ValidateKeyWithActivation(storedKey, machineId, out errorMsg);
            }
            catch
            {
                return false;
            }
        }

        private string GetStoredKey()
        {
            try
            {
                string keyFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "license.key");
                if (System.IO.File.Exists(keyFile))
                    return System.IO.File.ReadAllText(keyFile).Trim();
            }
            catch { }
            return "";
        }

        private int GetRemainingDays()
        {
            try
            {
                string key = GetStoredKey();
                if (string.IsNullOrEmpty(key) || key.Length < 20) return 0;

                var dateStr = key.Substring(key.Length - 8, 8);
                if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null,
                    System.Globalization.DateTimeStyles.None, out var expDate))
                {
                    return Math.Max(0, (expDate - DateTime.Today).Days);
                }
            }
            catch { }
            return 0;
        }

        void UpdateSubscriptionStatus()
        {
            try
            {
                SubscriptionService.Instance.ReloadConfig();
                _isLicensed = CheckLicenseStatus();

                if (_isLicensed)
                {
                    SetActiveStatus();
                    _liveTimer.Start();
                }
                else
                {
                    SetInactiveStatus();
                    _liveTimer.Stop();
                }

                if (_signalRService.IsConnected)
                {
                    int daysRemaining = GetRemainingDays();
                    _ = _signalRService.BroadcastLicenseChangeAsync(_isLicensed, daysRemaining);
                }

                AddActivity($"License: {(_isLicensed ? "Active" : "Inactive")}", "System");
            }
            catch
            {
                SetInactiveStatus();
            }
        }

        private void SetActiveStatus()
        {
            var green = (Color)ColorConverter.ConvertFromString("#10B981");
            var greenBg = (Color)ColorConverter.ConvertFromString("#D1FAE5");

            StatusText.Text = "✅ ACTIVE";
            StatusText.Foreground = new SolidColorBrush(green);
            DaysText.Text = GetRemainingDays().ToString();
            DaysText.Foreground = new SolidColorBrush(green);
            ActivateBtn.Visibility = Visibility.Collapsed;
            StatusIndicator.Fill = new SolidColorBrush(green);
            SubscriptionCard.BorderBrush = new SolidColorBrush(green);
            SubscriptionCard.BorderThickness = new Thickness(3);

            SubscriptionStatusText.Text = "Active";
            SubscriptionStatusText.Foreground = new SolidColorBrush(green);
            ModuleCountText.Text = "5/5 Active";

            UpdateModuleStatus("SGU", true);
            UpdateModuleStatus("DGU", true);
            UpdateModuleStatus("Lamination", true);
            UpdateModuleStatus("DguLam", true);
            UpdateModuleStatus("Optimization", true);

            PulseAnimation(SubscriptionCard);
        }

        private void SetInactiveStatus()
        {
            var red = (Color)ColorConverter.ConvertFromString("#EF4444");
            var redBg = (Color)ColorConverter.ConvertFromString("#FEF2F2");

            StatusText.Text = "❌ INACTIVE";
            StatusText.Foreground = new SolidColorBrush(red);
            DaysText.Text = "0";
            DaysText.Foreground = new SolidColorBrush(red);
            ActivateBtn.Visibility = Visibility.Visible;
            StatusIndicator.Fill = new SolidColorBrush(red);
            SubscriptionCard.BorderBrush = new SolidColorBrush(red);
            SubscriptionCard.BorderThickness = new Thickness(2);

            SubscriptionStatusText.Text = "Inactive";
            SubscriptionStatusText.Foreground = new SolidColorBrush(red);
            ModuleCountText.Text = "0/5 Active";

            UpdateModuleStatus("SGU", false);
            UpdateModuleStatus("DGU", false);
            UpdateModuleStatus("Lamination", false);
            UpdateModuleStatus("DguLam", false);
            UpdateModuleStatus("Optimization", false);
        }

        #endregion

        #region Connection Management

        private async System.Threading.Tasks.Task ConnectToSignalRAsync()
        {
            try
            {
                await _signalRService.ConnectAsync();
                _isConnected = true;
                UpdateConnectionStatus(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] SignalR connect error: {ex.Message}");
                _isConnected = false;
                UpdateConnectionStatus(false);
            }
        }

        private async System.Threading.Tasks.Task DisconnectFromSignalRAsync()
        {
            try
            {
                await _signalRService.DisconnectAsync();
                _isConnected = false;
                UpdateConnectionStatus(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] SignalR disconnect error: {ex.Message}");
            }
        }

        #endregion

        #region Navigation Handlers

        private void SheetStore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowSheetStore();
                }
                AddActivity("Navigated to Sheet Store", "Navigation");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DailyWorks_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowDailyWorks();
                }
                AddActivity("Navigated to Daily Works", "Navigation");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Deliveries_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowDeliveries();
                }
                AddActivity("Navigated to Deliveries", "Navigation");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActivateNow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowSubscriptionPlan();
                }

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    System.Threading.Thread.Sleep(500);
                    UpdateSubscriptionStatus();
                }), DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshDashboardData();
            RefreshBalanceData();
            RefreshModuleStats();
            AddActivity("Manual refresh triggered", "User");
        }

        private void SyncBalance_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _balanceService.SyncNow();
                RefreshBalanceData();
                AddActivity("Balance sync triggered", "User");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sync error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewBalanceReports_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var balance = _balanceService.GetCurrentBalance();

                MessageBox.Show(
                    "Balance Summary Report\n\n" +
                    "━━━━━━━━━━━━━━━━━━━━\n" +
                    $"Daily Balance: AED {balance.DailyBalance:N2}\n" +
                    $"Monthly Balance: AED {balance.MonthlyBalance:N2}\n" +
                    $"Annual Balance: AED {balance.AnnualBalance:N2}\n\n" +
                    "━━━━━━━━━━━━━━━━━━━━\n" +
                    $"Today's Revenue: AED {balance.TodayRevenue:N2}\n" +
                    $"Today's Expenses: AED {balance.TodayExpenses:N2}\n" +
                    $"Net Income: AED {balance.NetIncome:N2}\n\n" +
                    $"Transactions: {balance.TransactionCount:N0}\n" +
                    $"Completed Today: {balance.CompletedToday:N0}\n" +
                    $"Pending Deliveries: {balance.PendingDeliveries:N0}",
                    "Balance Reports",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                AddActivity("Viewed balance reports", "User");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _liveTimer?.Stop();
            _clockTimer?.Stop();
            _broadcastTimer?.Stop();
            _balanceSyncTimer?.Stop();
            _moduleStatsTimer?.Stop();

            _ = DisconnectFromSignalRAsync();
            _ = _webSocketService.DisconnectAsync();

            _liveDataService?.Dispose();
            _signalRService?.Dispose();
            _webSocketService?.Dispose();
            _balanceService?.Dispose();

            _sheetStoreVM?.Cleanup();

            if (_liveDataService != null)
            {
                _liveDataService.OnProductionUpdated -= OnProductionUpdated;
                _liveDataService.OnMetricsUpdated -= OnMetricsUpdated;
                _liveDataService.OnModuleStatusChanged -= OnModuleStatusChanged;
                _liveDataService.OnLiveCounterUpdated -= OnLiveCounterUpdated;
                _liveDataService.OnError -= OnServiceError;
            }

            if (_signalRService != null)
            {
                _signalRService.OnMessageReceived -= OnSignalRMessageReceived;
                _signalRService.OnConnectionStateChanged -= OnSignalRConnectionChanged;
                _signalRService.OnError -= OnServiceError;
            }

            if (_webSocketService != null)
            {
                _webSocketService.OnMessageReceived -= OnWebSocketMessageReceived;
                _webSocketService.OnConnectionStateChanged -= OnWebSocketConnectionChanged;
                _webSocketService.OnError -= OnServiceError;
                _webSocketService.OnLogMessage -= OnServiceLog;
            }

            if (_balanceService != null)
            {
                _balanceService.OnBalanceUpdated -= OnBalanceUpdated;
                _balanceService.OnTransactionRecorded -= OnTransactionRecorded;
                _balanceService.OnError -= OnServiceError;
            }

            System.Diagnostics.Debug.WriteLine("[DashboardView] Disposed");
        }

        #endregion

        #region Helper Properties

        private MainViewModel ViewModel => DataContext as MainViewModel;

        #endregion
    }

    #region Helper Classes

    public class ActivityItem
    {
        public string Message { get; set; }
        public string Time { get; set; }
    }

    public class BalanceData
    {
        public double DailyBalance { get; set; }
        public double MonthlyBalance { get; set; }
        public double AnnualBalance { get; set; }
        public double TodayRevenue { get; set; }
        public double TodayExpenses { get; set; }
        public double NetIncome { get; set; }
        public int TransactionCount { get; set; }
        public int PendingDeliveries { get; set; }
        public int CompletedToday { get; set; }
    }

    public class TransactionInfo
    {
        public string Type { get; set; }
        public double Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
    }

    #endregion
}