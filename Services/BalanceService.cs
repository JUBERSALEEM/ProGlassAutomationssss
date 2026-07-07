using ProGlassAutomation.Data.Database;
using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace ProGlassAutomation.Services
{
    public class BalanceService : IDisposable
    {
        #region Events

        public event EventHandler<BalanceData>? OnBalanceUpdated;
        public event EventHandler<TransactionInfo>? OnTransactionRecorded;
        public event EventHandler<string>? OnError;
        public event EventHandler<string>? OnLogMessage;

        #endregion

        #region Fields

        private DispatcherTimer _syncTimer;
        private bool _isRunning;
        private bool _isDisposed;

        #endregion

        #region Constructor

        public BalanceService()
        {
            _syncTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _syncTimer.Tick += OnSyncTimerTick;
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            _syncTimer.Start();

            // Initial load
            RefreshBalance();

            Log("BalanceService started");
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _syncTimer.Stop();

            Log("BalanceService stopped");
        }

        public void SyncNow()
        {
            RefreshBalance();
        }

        public BalanceData GetCurrentBalance()
        {
            return CalculateBalance();
        }

        public void RecordTransaction(string type, double amount, string description = "")
        {
            try
            {
                var transaction = new TransactionInfo
                {
                    Type = type,
                    Amount = amount,
                    Timestamp = DateTime.Now,
                    Description = description
                };

                OnTransactionRecorded?.Invoke(this, transaction);

                // Refresh balance after transaction
                RefreshBalance();

                Log($"Transaction recorded: {type} - ${amount:N2}");
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, $"Failed to record transaction: {ex.Message}");
            }
        }

        public List<TransactionInfo> GetRecentTransactions(int count = 50)
        {
            var transactions = new List<TransactionInfo>();

            try
            {
                // Get from database
                var dbTransactions = DbHelper.GetAllSheetPurchases();

                foreach (var t in dbTransactions)
                {
                    transactions.Add(new TransactionInfo
                    {
                        Type = "Purchase",
                        Amount = (double)(t.UnitPrice * t.Quantity),
                        Timestamp = t.CreatedAt,
                        Description = $"Sheet Purchase - {t.Quantity} units"
                    });
                }

                // Get delivery revenue
                var deliveries = DbHelper.GetAllDeliveries();
                foreach (var d in deliveries)
                {
                    transactions.Add(new TransactionInfo
                    {
                        Type = "Revenue",
                        Amount = d.OrderSQM * GetAveragePricePerSQM(),
                        Timestamp = d.CreatedDate,
                        Description = $"Delivery - {d.Company}"
                    });
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, $"Failed to get transactions: {ex.Message}");
            }

            return transactions;
        }

        #endregion

        #region Private Methods

        private void OnSyncTimerTick(object? sender, EventArgs e)
        {
            RefreshBalance();
        }

        private void RefreshBalance()
        {
            try
            {
                var balance = CalculateBalance();
                OnBalanceUpdated?.Invoke(this, balance);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, $"Refresh balance failed: {ex.Message}");
            }
        }

        private BalanceData CalculateBalance()
        {
            var balance = new BalanceData();

            try
            {
                // Calculate today's revenue from deliveries
                var todayDeliveries = DbHelper.GetAllDeliveries();
                double todayRevenue = 0;
                int completedToday = 0;
                int pendingDeliveries = 0;

                foreach (var delivery in todayDeliveries)
                {
                    if (delivery.Date.Date == DateTime.Today)
                    {
                        todayRevenue += delivery.OrderSQM * GetAveragePricePerSQM();

                        if (delivery.Status == "Completed")
                            completedToday++;
                        else if (delivery.Status == "Pending" || delivery.Status == "Processing")
                            pendingDeliveries++;
                    }
                }

                // Calculate today's expenses from sheet purchases
                double todayExpenses = 0;
                var purchases = DbHelper.GetAllSheetPurchases();

                foreach (var purchase in purchases)
                {
                    if (purchase.PurchasedOn.Date == DateTime.Today)
                    {
                        todayExpenses += (double)(purchase.UnitPrice * purchase.Quantity);
                    }
                }

                // Calculate monthly totals
                double monthlyRevenue = 0;
                double monthlyExpenses = 0;
                var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

                foreach (var delivery in todayDeliveries)
                {
                    if (delivery.Date >= startOfMonth)
                    {
                        monthlyRevenue += delivery.OrderSQM * GetAveragePricePerSQM();
                    }
                }

                foreach (var purchase in purchases)
                {
                    if (purchase.PurchasedOn >= startOfMonth)
                    {
                        monthlyExpenses += (double)(purchase.UnitPrice * purchase.Quantity);
                    }
                }

                // Calculate annual totals
                double annualRevenue = 0;
                double annualExpenses = 0;
                var startOfYear = new DateTime(DateTime.Today.Year, 1, 1);

                foreach (var delivery in todayDeliveries)
                {
                    if (delivery.Date >= startOfYear)
                    {
                        annualRevenue += delivery.OrderSQM * GetAveragePricePerSQM();
                    }
                }

                foreach (var purchase in purchases)
                {
                    if (purchase.PurchasedOn >= startOfYear)
                    {
                        annualExpenses += (double)(purchase.UnitPrice * purchase.Quantity);
                    }
                }

                // Set balance values
                balance.TodayRevenue = todayRevenue;
                balance.TodayExpenses = todayExpenses;
                balance.NetIncome = todayRevenue - todayExpenses;
                balance.DailyBalance = balance.NetIncome;

                balance.MonthlyBalance = monthlyRevenue - monthlyExpenses;
                balance.AnnualBalance = annualRevenue - annualExpenses;

                balance.TransactionCount = GetTodayTransactionCount();
                balance.PendingDeliveries = pendingDeliveries;
                balance.CompletedToday = completedToday;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, $"Calculate balance failed: {ex.Message}");
            }

            return balance;
        }

        private double GetAveragePricePerSQM()
        {
            // Default price per sqm if not available from sheets
            return 85.00;
        }

        private int GetTodayTransactionCount()
        {
            int count = 0;

            try
            {
                var purchases = DbHelper.GetAllSheetPurchases();
                var deliveries = DbHelper.GetAllDeliveries();

                foreach (var p in purchases)
                {
                    if (p.PurchasedOn.Date == DateTime.Today)
                        count++;
                }

                foreach (var d in deliveries)
                {
                    if (d.Date.Date == DateTime.Today)
                        count++;
                }
            }
            catch { }

            return count;
        }

        private void Log(string message)
        {
            OnLogMessage?.Invoke(this, $"[BalanceService] {message}");
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            Stop();
            Log("BalanceService disposed");
        }

        #endregion

        #region Inner Classes (No Separate Files)

        public class TransactionInfo
        {
            public string Type { get; set; } = "";
            public double Amount { get; set; }
            public DateTime Timestamp { get; set; }
            public string Description { get; set; } = "";
        }

        public class BalanceData
        {
            public double TodayRevenue { get; set; }
            public double TodayExpenses { get; set; }
            public double NetIncome { get; set; }
            public double DailyBalance { get; set; }
            public double MonthlyBalance { get; set; }
            public double AnnualBalance { get; set; }
            public int TransactionCount { get; set; }
            public int PendingDeliveries { get; set; }
            public int CompletedToday { get; set; }
        }

        #endregion
    }
}