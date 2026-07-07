// Services/LiveDataService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using ProGlassAutomation.Data.Database;

namespace ProGlassAutomation.Services
{
    /// <summary>
    /// Service for managing live data updates from database
    /// </summary>
    public class LiveDataService : IDisposable
    {
        #region Events

        public event EventHandler<double>? OnProductionUpdated;
        public event EventHandler<(string Module, bool IsActive)>? OnModuleStatusChanged;
        public event EventHandler<(string Module, int Count)>? OnLiveCounterUpdated;
        public event EventHandler<LiveMetrics>? OnMetricsUpdated;
        public event EventHandler<string>? OnError;

        #endregion

        #region Fields

        private readonly DispatcherTimer _pollingTimer;
        private readonly Dictionary<string, DateTime> _lastUpdateTimes = new();
        private readonly Dictionary<string, int> _minuteCounters = new();
        private bool _isDisposed;
        private LiveMetrics _currentMetrics;

        #endregion

        #region Properties

        public bool IsRunning => _pollingTimer?.IsEnabled ?? false;
        public LiveMetrics CurrentMetrics => _currentMetrics;

        #endregion

        #region Constructor

        public LiveDataService()
        {
            _pollingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _pollingTimer.Tick += PollingTimer_Tick;

            _currentMetrics = new LiveMetrics();
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            if (_isDisposed) return;
            _pollingTimer.Start();
            System.Diagnostics.Debug.WriteLine("[LiveDataService] Started");
        }

        public void Stop()
        {
            _pollingTimer?.Stop();
            System.Diagnostics.Debug.WriteLine("[LiveDataService] Stopped");
        }

        public void SetPollingInterval(int seconds)
        {
            if (_pollingTimer != null)
                _pollingTimer.Interval = TimeSpan.FromSeconds(seconds);
        }

        public void RefreshNow()
        {
            PollData();
        }

        public LiveMetrics GetCurrentMetrics()
        {
            return _currentMetrics;
        }

        #endregion

        #region Polling

        private void PollingTimer_Tick(object? sender, EventArgs e)
        {
            PollData();
        }

        private void PollData()
        {
            try
            {
                // Poll production data
                PollProductionData();

                // Poll module statuses
                PollModuleStatuses();

                // Poll live counters
                PollLiveCounters();

                // Poll efficiency metrics
                PollEfficiencyMetrics();

                // Broadcast to all subscribers
                BroadcastMetrics();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LiveDataService] Polling error: {ex.Message}");
                OnError?.Invoke(this, ex.Message);
            }
        }

        private void PollProductionData()
        {
            try
            {
                var todayProduction = DbHelper.GetTodayTotalProduction();
                var previousProduction = _currentMetrics.TotalProduction;

                _currentMetrics.TotalProduction = todayProduction;
                _currentMetrics.ProductionChange = todayProduction - previousProduction;
                _currentMetrics.LastProductionUpdate = DateTime.Now;

                if (Math.Abs(todayProduction - previousProduction) > 0.01)
                {
                    OnProductionUpdated?.Invoke(this, todayProduction);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LiveDataService] Production poll error: {ex.Message}");
            }
        }

        private void PollModuleStatuses()
        {
            try
            {
                // Check license status for module availability
                bool isLicensed = CheckLicenseStatus();

                string[] modules = { "SGU", "DGU", "Lamination", "DguLam", "Optimization" };
                foreach (var module in modules)
                {
                    bool isActive = isLicensed && CheckModuleActivity(module);
                    bool wasActive = _currentMetrics.GetModuleStatus(module);

                    if (isActive != wasActive)
                    {
                        _currentMetrics.SetModuleStatus(module, isActive);
                        OnModuleStatusChanged?.Invoke(this, (module, isActive));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LiveDataService] Module status poll error: {ex.Message}");
            }
        }

        private void PollLiveCounters()
        {
            try
            {
                var now = DateTime.Now;
                var oneMinuteAgo = now.AddMinutes(-1);

                // Poll SGU
                int sguCount = DbHelper.GetCalculationCount("SGU", oneMinuteAgo, now);
                if (sguCount != _currentMetrics.SguCount)
                {
                    _currentMetrics.SguCount = sguCount;
                    _currentMetrics.SetLiveCount("SGU", sguCount);
                    OnLiveCounterUpdated?.Invoke(this, ("SGU", sguCount));
                }

                // Poll DGU
                int dguCount = DbHelper.GetCalculationCount("DGU", oneMinuteAgo, now);
                if (dguCount != _currentMetrics.DguCount)
                {
                    _currentMetrics.DguCount = dguCount;
                    _currentMetrics.SetLiveCount("DGU", dguCount);
                    OnLiveCounterUpdated?.Invoke(this, ("DGU", dguCount));
                }

                // Poll Lamination
                int lamCount = DbHelper.GetCalculationCount("Lamination", oneMinuteAgo, now);
                if (lamCount != _currentMetrics.LamCount)
                {
                    _currentMetrics.LamCount = lamCount;
                    _currentMetrics.SetLiveCount("Lamination", lamCount);
                    OnLiveCounterUpdated?.Invoke(this, ("Lamination", lamCount));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LiveDataService] Live counter poll error: {ex.Message}");
            }
        }

        private void PollEfficiencyMetrics()
        {
            try
            {
                var completed = DbHelper.GetTodayCompletedOrders();
                var total = DbHelper.GetTodayTotalOrders();
                double efficiency = total > 0 ? (double)completed / total * 100 : 0;

                _currentMetrics.Efficiency = efficiency;
                _currentMetrics.Uptime = CalculateUptime();
                _currentMetrics.CompletedOrders = completed;
                _currentMetrics.TotalOrders = total;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LiveDataService] Efficiency poll error: {ex.Message}");
            }
        }

        private void BroadcastMetrics()
        {
            OnMetricsUpdated?.Invoke(this, _currentMetrics);
        }

        #endregion

        #region Helper Methods

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

        private bool CheckModuleActivity(string moduleName)
        {
            try
            {
                return DbHelper.GetModuleLastActivity(moduleName) > DateTime.Now.AddMinutes(-5);
            }
            catch
            {
                return false;
            }
        }

        private double CalculateUptime()
        {
            try
            {
                var uptimeRecords = DbHelper.GetUptimeRecords();
                if (uptimeRecords.Any())
                    return uptimeRecords.Average();
            }
            catch { }
            return 99.8;
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

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            Stop();
            _pollingTimer.Tick -= PollingTimer_Tick;
            System.Diagnostics.Debug.WriteLine("[LiveDataService] Disposed");
        }

        #endregion
    }

    #region Live Metrics Model

    public class LiveMetrics
    {
        public double TotalProduction { get; set; } = 0;
        public double ProductionChange { get; set; } = 0;
        public double Efficiency { get; set; } = 94.2;
        public double Uptime { get; set; } = 99.8;
        public int TotalOrders { get; set; } = 0;
        public int CompletedOrders { get; set; } = 0;
        public DateTime LastProductionUpdate { get; set; } = DateTime.Now;
        public DateTime LastMetricsUpdate { get; set; } = DateTime.Now;

        // Individual module calculation counts
        public int SguCount { get; set; } = 0;
        public int DguCount { get; set; } = 0;
        public int LamCount { get; set; } = 0;

        // Total calculations (sum of all modules)
        public int TotalCalculations => SguCount + DguCount + LamCount;

        private readonly Dictionary<string, bool> _moduleStatuses = new();
        private readonly Dictionary<string, int> _liveCounters = new();

        public LiveMetrics()
        {
            string[] modules = { "SGU", "DGU", "Lamination", "DguLam", "Optimization" };
            foreach (var module in modules)
            {
                _moduleStatuses[module] = false;
                _liveCounters[module] = 0;
            }
        }

        public bool GetModuleStatus(string module) =>
            _moduleStatuses.TryGetValue(module, out var status) ? status : false;

        public void SetModuleStatus(string module, bool status) =>
            _moduleStatuses[module] = status;

        public int GetLiveCount(string module) =>
            _liveCounters.TryGetValue(module, out var count) ? count : 0;

        public void SetLiveCount(string module, int count) =>
            _liveCounters[module] = count;
    }

    #endregion
}