using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using ProGlassAutomation.Data.Database;

namespace ProGlassAutomation.ViewModels.Analytics
{
    public class AnalyticsViewModel : INotifyPropertyChanged
    {
        private int _totalTaxInvoices;
        private double _totalInvoicedAmount;
        private double _totalPaidAmount;
        private double _totalBalanceOutstanding;
        private double _collectionRate;

        public event PropertyChangedEventHandler PropertyChanged;

        public int TotalTaxInvoices
        {
            get => _totalTaxInvoices;
            set { _totalTaxInvoices = value; OnPropertyChanged(); }
        }

        public double TotalInvoicedAmount
        {
            get => _totalInvoicedAmount;
            set { _totalInvoicedAmount = value; OnPropertyChanged(); }
        }

        public double TotalPaidAmount
        {
            get => _totalPaidAmount;
            set { _totalPaidAmount = value; OnPropertyChanged(); }
        }

        public double TotalBalanceOutstanding
        {
            get => _totalBalanceOutstanding;
            set { _totalBalanceOutstanding = value; OnPropertyChanged(); }
        }

        public double CollectionRate
        {
            get => _collectionRate;
            set { _collectionRate = value; OnPropertyChanged(); }
        }

        public ObservableCollection<SalesPerformanceData> SalesByClient { get; set; } = new();

        public AnalyticsViewModel()
        {
            LoadAnalytics();
        }

        public void LoadAnalytics()
        {
            try
            {
                var invoices = DbHelper.GetAllTaxInvoices();
                TotalTaxInvoices = invoices.Count;
                TotalInvoicedAmount = invoices.Sum(x => x.TotalAmount);
                TotalPaidAmount = invoices.Sum(x => x.PaidAmount);
                TotalBalanceOutstanding = invoices.Sum(x => x.BalanceAmount);

                CollectionRate = TotalInvoicedAmount > 0
                    ? Math.Round((TotalPaidAmount / TotalInvoicedAmount) * 100.0, 2)
                    : 0.0;

                // Segment by Client for high-fidelity report
                SalesByClient.Clear();
                var grouped = invoices.GroupBy(x => x.ClientName)
                    .Select(g => new SalesPerformanceData
                    {
                        ClientName = g.Key,
                        InvoicedTotal = g.Sum(x => x.TotalAmount),
                        PaidTotal = g.Sum(x => x.PaidAmount),
                        PendingTotal = g.Sum(x => x.BalanceAmount)
                    })
                    .OrderByDescending(x => x.InvoicedTotal)
                    .ToList();

                foreach (var g in grouped)
                {
                    SalesByClient.Add(g);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AnalyticsViewModel] Error: {ex.Message}");
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class SalesPerformanceData
    {
        public string ClientName { get; set; } = "";
        public double InvoicedTotal { get; set; }
        public double PaidTotal { get; set; }
        public double PendingTotal { get; set; }
    }
}
