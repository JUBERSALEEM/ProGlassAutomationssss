using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ProGlassAutomation.Views.SGU
{
    public class SguViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        private static readonly string[] AllCategories = new string[]
        {
            "Clear Float", "Ultra Clear", "Crystal Clear", "Reflective", "Tinted", "Low-E", "Custom"
        };

        private static readonly string[] AllThicknesses = new string[]
        {
            "2mm", "3mm", "4mm", "5mm", "6mm", "8mm", "10mm", "12mm"
        };

        private static readonly string[] AllColors = new string[]
        {
            "Clear", "Ultra Clear", "Grey", "Green", "Blue", "Bronze", "Black"
        };

        public ObservableCollection<string> CategoryOptions { get; } = new();
        public ObservableCollection<string> ThicknessOptions { get; } = new();
        public ObservableCollection<string> ColorOptions { get; } = new();

        public ObservableCollection<string> WastageOptions { get; } = new()
        {
            "5", "10", "15", "20", "25", "30"
        };

        public ObservableCollection<string> ProfitMarginOptions { get; } = new()
        {
            "5%", "10%", "15%", "20%", "25%", "30%"
        };

        public ObservableCollection<RecordModel> Records { get; } = new();

        private string _cat = "Clear Float";
        public string Category
        {
            get => _cat;
            set { _cat = value; OnPropertyChanged(); Calculate(); }
        }

        private string _th = "6mm";
        public string Thickness
        {
            get => _th;
            set { _th = value; OnPropertyChanged(); Calculate(); }
        }

        private string _c = "Clear";
        public string ColorName
        {
            get => _c;
            set { _c = value; OnPropertyChanged(); Calculate(); }
        }

        private string _wastageConsider = "15";
        public string WastageConsider
        {
            get => _wastageConsider;
            set { _wastageConsider = value; OnPropertyChanged(); Calculate(); }
        }

        private string _profitMargin = "15%";
        public string ProfitMargin
        {
            get => _profitMargin;
            set { _profitMargin = value; OnPropertyChanged(); Calculate(); }
        }

        public string Spec => $"{Thickness} {ColorName}";

        private double _result;
        public double Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(); }
        }

        private double _sheetPrice;
        public double SheetPrice
        {
            get => _sheetPrice;
            set { _sheetPrice = value; OnPropertyChanged(); Calculate(); }
        }

        private double _cutting = 5;
        public double Cutting
        {
            get => _cutting;
            set { _cutting = value; OnPropertyChanged(); Calculate(); }
        }

        private double _tempering = 10;
        public double Tempering
        {
            get => _tempering;
            set { _tempering = value; OnPropertyChanged(); Calculate(); }
        }

        public double BaseCost => _sheetPrice / WastageFactor;
        public double ProcessingCost => _cutting + _tempering;
        public double Subtotal => BaseCost + ProcessingCost;
        public double FinalPrice => Subtotal * ProfitMarginFactor;

        public double WastageFactor => 1 - (double.Parse(_wastageConsider) / 100.0);
        public double ProfitMarginFactor => 1 + (double.Parse(_profitMargin.Replace("%", "")) / 100.0);

        public ICommand SaveCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand DeleteCommand { get; }

        public SguViewModel()
        {
            foreach (var c in AllCategories) CategoryOptions.Add(c);
            foreach (var t in AllThicknesses) ThicknessOptions.Add(t);
            foreach (var c in AllColors) ColorOptions.Add(c);

            SaveCommand = new RelayCommand(o =>
            {
                Records.Insert(0, new RecordModel
                {
                    DisplayText = $"{Category} | {Spec} | {Result:F2} AED",
                    Wastage = $"{_wastageConsider}%",
                    ProfitMargin = _profitMargin,
                    CreatedAt = DateTime.Now.ToString("HH:mm"),
                    Result = Result
                });
                MessageBox.Show("Saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            ClearCommand = new RelayCommand(o => Records.Clear());

            DeleteCommand = new RelayCommand(o =>
            {
                if (o is RecordModel r) Records.Remove(r);
            });

            Calculate();
        }

        public void Calculate()
        {
            double baseCost = _sheetPrice / WastageFactor;
            double processing = _cutting + _tempering;
            Result = (baseCost + processing) * ProfitMarginFactor;
        }
    }

    public class RecordModel
    {
        public string DisplayText { get; set; }
        public string Wastage { get; set; }
        public string ProfitMargin { get; set; }
        public string CreatedAt { get; set; }
        public double Result { get; set; }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        public RelayCommand(Action<object> execute) => _execute = execute;
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
        public bool CanExecute(object p) => true;
        public void Execute(object p) => _execute(p);
    }
}