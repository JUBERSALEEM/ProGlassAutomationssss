using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ProGlassAutomation.Views.ProformaInvoice.Optimization
{
    public class GlassOptimizationSheetItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _sheetName = "Sheet 1";
        public string SheetName
        {
            get => _sheetName;
            set { _sheetName = value; OnPropertyChanged(); }
        }

        private string _priceText = "";
        public string PriceText
        {
            get => _priceText;
            set { _priceText = value; OnPropertyChanged(); Calculate(); }
        }

        private string _optimizationText = "";
        public string OptimizationText
        {
            get => _optimizationText;
            set { _optimizationText = value; OnPropertyChanged(); Calculate(); }
        }

        private double _result1 = 0;
        private double _result2 = 0;
        private double _baseResult = 0;

        public double Result1 => _result1;
        public double Result2 => _result2;
        public double BaseResult => _baseResult;

        public string Result1Text => _result1.ToString("N2");
        public string Result2Text => _result2.ToString("N2");
        public string BaseResultText => _baseResult.ToString("N2");

        private const double BaselineOptimization = 0.85;

        private void Calculate()
        {
            double price = ParseDouble(PriceText);
            double sqm = ParseDouble(OptimizationText);

            if (price > 0 && sqm > 0)
            {
                // Result 1 = (Price × 100) ÷ SQM
                _result1 = (price * 100) / sqm;
                // Result 2 = Price ÷ 0.85
                _result2 = price / BaselineOptimization;
                // Base Result = Result1 - Result2
                _baseResult = _result1 - _result2;
            }
            else
            {
                _result1 = 0;
                _result2 = 0;
                _baseResult = 0;
            }

            OnPropertyChanged(nameof(Result1));
            OnPropertyChanged(nameof(Result2));
            OnPropertyChanged(nameof(BaseResult));
            OnPropertyChanged(nameof(Result1Text));
            OnPropertyChanged(nameof(Result2Text));
            OnPropertyChanged(nameof(BaseResultText));
        }

        private double ParseDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            string cleaned = value.Trim().Replace(",", "").Replace("AED", "");
            if (double.TryParse(cleaned, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double result))
                return Math.Max(0, result);
            return 0;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class GlassOptimizationSectionViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<GlassOptimizationSheetItem> Sheets { get; } = new();

        public ICommand AddSheetCommand { get; }
        public ICommand RemoveSheetCommand { get; }

        public double TotalBaseResult => Sheets.Sum(s => s.BaseResult);
        public string TotalBaseResultText => TotalBaseResult.ToString("N2");

        public GlassOptimizationSectionViewModel()
        {
            AddSheetCommand = new RelayCommand(_ => AddSheet());
            RemoveSheetCommand = new RelayCommand(param => RemoveSheet(param as GlassOptimizationSheetItem));

            AddSheet();
        }

        private void AddSheet()
        {
            var sheet = new GlassOptimizationSheetItem
            {
                SheetName = $"Sheet {Sheets.Count + 1}"
            };
            sheet.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(TotalBaseResult));
                OnPropertyChanged(nameof(TotalBaseResultText));
            };
            Sheets.Add(sheet);
            OnPropertyChanged(nameof(TotalBaseResult));
            OnPropertyChanged(nameof(TotalBaseResultText));
        }

        private void RemoveSheet(GlassOptimizationSheetItem? sheet)
        {
            if (sheet != null && Sheets.Count > 1)
            {
                Sheets.Remove(sheet);
                for (int i = 0; i < Sheets.Count; i++)
                    Sheets[i].SheetName = $"Sheet {i + 1}";
                OnPropertyChanged(nameof(TotalBaseResult));
                OnPropertyChanged(nameof(TotalBaseResultText));
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}