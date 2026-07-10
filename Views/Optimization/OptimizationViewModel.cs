using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Views.Optimization
{
    public class OptimizationViewModel : INotifyPropertyChanged
    {
        private readonly OptimizationEngine _engine = new OptimizationEngine();

        public event PropertyChangedEventHandler? PropertyChanged;

        // ═══════════════════════════════════════════════════════
        // COLLECTIONS
        // ═══════════════════════════════════════════════════════

        public ObservableCollection<StockSheet> StockSheets { get; } = new ObservableCollection<StockSheet>();
        public ObservableCollection<CutPart> Parts { get; } = new ObservableCollection<CutPart>();

        // ═══════════════════════════════════════════════════════
        // RESULT
        // ═══════════════════════════════════════════════════════

        private OptimizationResult? _lastResult;
        public OptimizationResult? LastResult
        {
            get => _lastResult;
            private set
            {
                _lastResult = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasResult));
                OnPropertyChanged(nameof(Utilization));
                OnPropertyChanged(nameof(Waste));
                OnPropertyChanged(nameof(PlacedPartCount));
                OnPropertyChanged(nameof(SheetsUsed));
                OnPropertyChanged(nameof(AverageUtilization));
            }
        }

        public bool HasResult => _lastResult != null;
        public int PlacedPartCount => _lastResult?.PlacedParts?.Count ?? 0;

        private double _utilization;
        public double Utilization { get => _utilization; private set { _utilization = value; OnPropertyChanged(); } }

        private double _waste;
        public double Waste { get => _waste; private set { _waste = value; OnPropertyChanged(); } }

        public int SheetsUsed => _lastResult != null ? 1 : 0;
        public double AverageUtilization => _utilization;

        public ICommand RunOptimizationCommand { get; }
        public ICommand AddStockCommand { get; }
        public ICommand AddPartCommand { get; }

        public OptimizationViewModel()
        {
            RunOptimizationCommand = new AsyncRelayCommand(RunOptimizationAsync);
            AddStockCommand = new RelayCommand(AddStock);
            AddPartCommand = new RelayCommand(AddPart);
        }

        private void AddStock()
        {
            StockSheets.Add(new StockSheet { L = 3210, W = 2250, Qty = 10 });
        }

        private void AddPart()
        {
            Parts.Add(new CutPart { L = 800, W = 600, Qty = 1 });
        }

        // ═══════════════════════════════════════════════════════
        // SAFE RUN
        // ═══════════════════════════════════════════════════════

        public bool RunOptimization()
        {
            // If no parts, cannot run
            if (Parts.Count == 0)
                return false;

            // Ensure we have at least one stock sheet
            if (StockSheets.Count == 0)
            {
                StockSheets.Add(new StockSheet { L = 3210, W = 2250, Qty = 10 });
            }

            // Filter out invalid parts
            var validParts = Parts
                .Where(p => p.L > 0 && p.W > 0 && p.Qty > 0)
                .ToList();

            if (validParts.Count == 0)
                return false;

            try
            {
                var result = _engine.Execute(
                    new List<StockSheet>(StockSheets),
                    validParts,
                    15, 15, 15, 15,
                    4,
                    RotationMode.BestFit);

                LastResult = result;

                if (result != null)
                {
                    Utilization = result.Util;
                    Waste = result.Waste;
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OptimizationViewModel] RunOptimization failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RunOptimizationAsync()
        {
            if (Parts.Count == 0)
                return false;

            if (StockSheets.Count == 0)
            {
                StockSheets.Add(new StockSheet { L = 3210, W = 2250, Qty = 10 });
            }

            var validParts = Parts
                .Where(p => p.L > 0 && p.W > 0 && p.Qty > 0)
                .ToList();

            if (validParts.Count == 0)
                return false;

            try
            {
                OptimizationResult? result = await Task.Run(() =>
                {
                    return _engine.Execute(
                        new List<StockSheet>(StockSheets),
                        validParts,
                        15, 15, 15, 15,
                        4,
                        RotationMode.BestFit);
                });

                LastResult = result;

                if (result != null)
                {
                    Utilization = result.Util;
                    Waste = result.Waste;
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OptimizationViewModel] RunOptimizationAsync failed: {ex.Message}");
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════
        // PROFORMA INVOICE IMPORT — STOCK + PARTS HANDLING
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Imports items from a ProformaInvoice.
        /// Convention: items with both dimensions > 2000mm are treated as STOCK sheets.
        /// Items with smaller dimensions are treated as PARTS to cut.
        /// If no stock is found in the invoice, a default 3210x2250 sheet is added.
        /// </summary>
        public void ImportInvoiceItems(List<InvoiceItemModel> items)
        {
            if (items == null)
                return;

            // Separate stock sheets from parts
            var stockItems = new List<InvoiceItemModel>();
            var partItems = new List<InvoiceItemModel>();

            foreach (var item in items)
            {
                if (item == null) continue;

                // Heuristic: if either dimension exceeds 2000mm, treat as stock
                bool isStock = (item.Width1 > 2000 || item.Height1 > 2000);

                if (isStock)
                    stockItems.Add(item);
                else
                    partItems.Add(item);
            }

            // If we found stock in the invoice, use it
            if (stockItems.Count > 0)
            {
                StockSheets.Clear();
                foreach (var stockItem in stockItems)
                {
                    StockSheets.Add(new StockSheet
                    {
                        L = stockItem.Width1,
                        W = stockItem.Height1,
                        Qty = stockItem.Qty > 0 ? stockItem.Qty : 10
                    });
                }
            }
            // If no stock was found in the invoice, keep existing stock or add default
            else if (StockSheets.Count == 0)
            {
                StockSheets.Add(new StockSheet { L = 3210, W = 2250, Qty = 10 });
            }

            // Always replace parts with invoice parts
            Parts.Clear();
            foreach (var partItem in partItems)
            {
                Parts.Add(new CutPart
                {
                    L = partItem.Width1,
                    W = partItem.Height1,
                    Qty = partItem.Qty > 0 ? partItem.Qty : 1
                });
            }
        }

        public void RunOptimizationFromInvoice()
        {
            try
            {
                RunOptimization();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OptimizationViewModel] RunOptimizationFromInvoice failed: {ex.Message}");
            }
        }

        public void SetStockSheet(double width, double height)
        {
            if (width <= 0 || height <= 0) return;

            StockSheets.Clear();
            StockSheets.Add(new StockSheet
            {
                L = width,
                W = height,
                Qty = 100
            });
        }

        public void SetTrimSettings(double lr, double br, double tr, double rm, double kerf, double breakout)
        {
            // Engine uses internal default values; kept for API compatibility.
        }

        public List<OptimizationResult> GetResultsList()
        {
            if (LastResult != null)
                return new List<OptimizationResult> { LastResult };
            return new List<OptimizationResult>();
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) { _execute = execute; }
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
    }

    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task<bool>> _execute;
        public AsyncRelayCommand(Func<Task<bool>> execute) { _execute = execute; }
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => true;

        public async void Execute(object? parameter)
        {
            try { await _execute(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AsyncRelayCommand] {ex.Message}");
            }
        }
    }
}