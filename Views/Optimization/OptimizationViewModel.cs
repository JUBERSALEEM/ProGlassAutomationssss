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

        public ObservableCollection<StockSheet> StockSheets { get; } = new();
        public ObservableCollection<CutPart> Parts { get; } = new();

        private OptimizationResult? _lastResult;

        public OptimizationResult? LastResult
        {
            get => _lastResult;
            private set
            {
                _lastResult = value;
                OnPropertyChanged();
            }
        }

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

        private double _cost;
        public double Cost
        {
            get => _cost;
            private set
            {
                _cost = value;
                OnPropertyChanged();
            }
        }

        private double _trimLeft = 15;
        private double _trimRight = 15;
        private double _trimTop = 15;
        private double _trimBottom = 15;
        private double _kerf = 4;

        public ICommand RunOptimizationCommand { get; }

        public OptimizationViewModel()
        {
            RunOptimizationCommand = new RelayCommand(RunOptimization);
        }

        private void RunOptimization()
        {
            if (StockSheets.Count == 0 || Parts.Count == 0)
                return;

            LastResult = _engine.Execute(
                new List<StockSheet>(StockSheets),
                new List<CutPart>(Parts),
                _trimLeft,
                _trimRight,
                _trimTop,
                _trimBottom,
                _kerf,
                RotationMode.BestFit);

            if (LastResult != null)
            {
                Utilization = LastResult.Util;
                Waste = LastResult.Waste;
                Cost = _services.CalculateCost(LastResult.Area);
            }
        }

        // ===============================
        // Compatibility Surface
        // ===============================

        public int SheetsUsed => LastResult != null ? 1 : 0;

        public double AverageUtilization => Utilization;

        public List<OptimizationResult> GetResultsList()
        {
            if (LastResult == null)
                return new List<OptimizationResult>();

            return new List<OptimizationResult> { LastResult };
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
            _trimLeft = lr;
            _trimBottom = br;
            _trimTop = tr;
            _trimRight = rm;
            _kerf = kerf;
        }

        public void RunOptimizationFromInvoice()
        {
            RunOptimization();
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

        public void Execute(object? parameter)
        {
            _execute();
        }
    }
}