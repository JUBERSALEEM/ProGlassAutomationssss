using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Views.Optimization
{
    public class OptimizationViewModel : INotifyPropertyChanged
    {
        private readonly OptimizationEngine _engine = new OptimizationEngine();
        private readonly OptimizationServices _services = new OptimizationServices();

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<StockSheet> StockSheets { get; } = new ObservableCollection<StockSheet>();
        public ObservableCollection<CutPart> Parts { get; } = new ObservableCollection<CutPart>();

        public OptimizationResult? LastResult { get; private set; }

        private double _utilization;
        public double Utilization
        {
            get => _utilization;
            private set
            {
                _utilization = value;
                OnPropertyChanged();
            }
        }

        private double _waste;
        public double Waste
        {
            get => _waste;
            private set
            {
                _waste = value;
                OnPropertyChanged();
            }
        }

        public ICommand RunOptimizationCommand { get; }

        public OptimizationViewModel()
        {
            RunOptimizationCommand = new RelayCommand(RunOptimization);
        }

        public void RunOptimization()
        {
            if (StockSheets.Count == 0 || Parts.Count == 0)
                return;

            LastResult = _engine.Execute(
                new List<StockSheet>(StockSheets),
                new List<CutPart>(Parts),
                15, 15, 15, 15,
                4,
                RotationMode.BestFit);

            Utilization = LastResult.Util;
            Waste = LastResult.Waste;
        }

        public int SheetsUsed => LastResult != null ? 1 : 0;
        public double AverageUtilization => Utilization;

        public List<OptimizationResult> GetResultsList()
        {
            return LastResult != null
                ? new List<OptimizationResult> { LastResult }
                : new List<OptimizationResult>();
        }

        public void ImportInvoiceItems(List<InvoiceItemModel> items)
        {
            Parts.Clear();

            if (items == null)
                return;

            foreach (var item in items)
            {
                Parts.Add(new CutPart
                {
                    L = item.Width1,
                    W = item.Height1,
                    Qty = item.Qty
                });
            }
        }

        public void RunOptimizationFromInvoice()
        {
            RunOptimization();
        }

        public void SetStockSheet(double width, double height)
        {
            StockSheets.Clear();
            StockSheets.Add(new StockSheet
            {
                L = width,
                W = height
            });
        }

        public void SetTrimSettings(double lr, double br, double tr, double rm, double kerf, double breakout)
        {
            // Internal default values used; kept for compatibility.
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;

        public RelayCommand(Action execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute();
    }
}