using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ProGlassAutomation.Views.GlassOptimization
{
    public class GlassOptimizationViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<SheetItem> Sheets { get; } = new();

        public ObservableCollection<string> Colors => SheetOptions.Colors;
        public ObservableCollection<string> Categories => SheetOptions.Categories;
        public ObservableCollection<string> ThicknessOptions => SheetOptions.ThicknessOptions;
        public ObservableCollection<string> ColorTypes => SheetOptions.ColorTypes;

        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand ClearAllCommand { get; }

        public GlassOptimizationViewModel()
        {
            AddCommand = new RelayCommand(_ => AddSheet());
            RemoveCommand = new RelayCommand(param => RemoveSheet(param as SheetItem));
            ClearAllCommand = new RelayCommand(_ => ClearAll());
        }

        private void AddSheet()
        {
            var sheet = new SheetItem
            {
                Id = Sheets.Count + 1,
                Name = $"Sheet {Sheets.Count + 1}"
            };
            Sheets.Add(sheet);
            OnPropertyChanged(nameof(Sheets));
        }

        private void RemoveSheet(SheetItem? sheet)
        {
            if (sheet == null) return;
            Sheets.Remove(sheet);
            OnPropertyChanged(nameof(Sheets));
        }

        private void ClearAll()
        {
            Sheets.Clear();
            OnPropertyChanged(nameof(Sheets));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class SheetItem : INotifyPropertyChanged
    {
        public SheetItem()
        {
        }

        // ═══════════════════════════════════════════════════════
        // STRING PROPERTIES - Store raw input as strings
        // ═══════════════════════════════════════════════════════

        private string _priceText = "";
        public string PriceText
        {
            get => _priceText;
            set
            {
                if (SetProperty(ref _priceText, value))
                {
                    ParsePrice(value);
                }
            }
        }

        private string _optimizationText = "";
        public string OptimizationText
        {
            get => _optimizationText;
            set
            {
                if (SetProperty(ref _optimizationText, value))
                {
                    ParseOptimization(value);
                }
            }
        }

        private string _chargeableSqmText = "";
        public string ChargeableSqmText
        {
            get => _chargeableSqmText;
            set
            {
                if (SetProperty(ref _chargeableSqmText, value))
                {
                    ParseChargeableSqm(value);
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        // DOUBLE PROPERTIES - Internal calculation values
        // ═══════════════════════════════════════════════════════

        private double _price;
        public double Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        private double _optimization;
        public double Optimization
        {
            get => _optimization;
            set => SetProperty(ref _optimization, value);
        }

        private double _chargeableSqm;
        public double ChargeableSqm
        {
            get => _chargeableSqm;
            set => SetProperty(ref _chargeableSqm, value);
        }

        // ═══════════════════════════════════════════════════════
        // PARSING METHODS - Use InvariantCulture
        // ═══════════════════════════════════════════════════════

        private void ParsePrice(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _price = 0;
            }
            else
            {
                var normalized = value.Replace(",", ".");
                if (double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                {
                    _price = result;
                }
                else
                {
                    _price = 0;
                }
            }
            OnPropertyChanged(nameof(Price));
            RecalculateResults();
        }

        private void ParseOptimization(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _optimization = 0;
            }
            else
            {
                var normalized = value.Replace(",", ".");
                if (double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                {
                    _optimization = result;
                }
                else
                {
                    _optimization = 0;
                }
            }
            OnPropertyChanged(nameof(Optimization));
            RecalculateResults();
        }

        private void ParseChargeableSqm(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _chargeableSqm = 0;
            }
            else
            {
                var normalized = value.Replace(",", ".");
                if (double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                {
                    _chargeableSqm = result;
                }
                else
                {
                    _chargeableSqm = 0;
                }
            }
            OnPropertyChanged(nameof(ChargeableSqm));
            RecalculateResults();
        }

        // ═══════════════════════════════════════════════════════
        // CALCULATION RESULTS
        // ═══════════════════════════════════════════════════════

        private void RecalculateResults()
        {
            if (Price <= 0 || Optimization <= 0 || ChargeableSqm <= 0)
            {
                ResetResults();
                return;
            }

            var optimization = Optimization / 100.0;
            Section1Value = Price / optimization;
            Section2Value = Price / 0.85;
            BaseResultValue = Section1Value - Section2Value;
            FinalResultValue = BaseResultValue * ChargeableSqm;

            Section1Text = Section1Value.ToString("N2", CultureInfo.InvariantCulture);
            Section2Text = Section2Value.ToString("N2", CultureInfo.InvariantCulture);
            BaseResultText = BaseResultValue.ToString("N2", CultureInfo.InvariantCulture);
            FinalResultText = FinalResultValue.ToString("N2", CultureInfo.InvariantCulture);
        }

        private void ResetResults()
        {
            Section1Value = 0;
            Section2Value = 0;
            BaseResultValue = 0;
            FinalResultValue = 0;
            Section1Text = "0";
            Section2Text = "0";
            BaseResultText = "0";
            FinalResultText = "0";
        }

        private int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _name = "";
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private double _section1Value;
        public double Section1Value
        {
            get => _section1Value;
            set => SetProperty(ref _section1Value, value);
        }

        private double _section2Value;
        public double Section2Value
        {
            get => _section2Value;
            set => SetProperty(ref _section2Value, value);
        }

        private double _baseResultValue;
        public double BaseResultValue
        {
            get => _baseResultValue;
            set => SetProperty(ref _baseResultValue, value);
        }

        private double _finalResultValue;
        public double FinalResultValue
        {
            get => _finalResultValue;
            set => SetProperty(ref _finalResultValue, value);
        }

        private string _section1Text = "0";
        public string Section1Text
        {
            get => _section1Text;
            set => SetProperty(ref _section1Text, value);
        }

        private string _section2Text = "0";
        public string Section2Text
        {
            get => _section2Text;
            set => SetProperty(ref _section2Text, value);
        }

        private string _baseResultText = "0";
        public string BaseResultText
        {
            get => _baseResultText;
            set => SetProperty(ref _baseResultText, value);
        }

        private string _finalResultText = "0";
        public string FinalResultText
        {
            get => _finalResultText;
            set => SetProperty(ref _finalResultText, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);
    }

    public static class SheetOptions
    {
        public static ObservableCollection<string> Colors { get; } = new()
        {
            "Clear", "Green", "Bronze", "Grey", "Blue", "Black"
        };

        public static ObservableCollection<string> Categories { get; } = new()
        {
            "Clear", "Tempered", "Laminated", "Insulated"
        };

        public static ObservableCollection<string> ThicknessOptions { get; } = new()
        {
            "4mm", "5mm", "6mm", "8mm", "10mm", "12mm"
        };

        public static ObservableCollection<string> ColorTypes { get; } = new()
        {
            "Standard", "Reflective", "Low-E", "Tinted"
        };
    }

    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EmptyZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "";

            if (value is double d && d == 0)
                return "";

            if (value is decimal dec && dec == 0)
                return "";

            if (value is double dv)
                return dv.ToString(CultureInfo.InvariantCulture);

            return value.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                if (string.IsNullOrWhiteSpace(s))
                    return 0.0;

                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                {
                    return result;
                }
            }

            return 0.0;
        }
    }

    public static class DecimalInputBehavior
    {
        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable",
                typeof(bool),
                typeof(DecimalInputBehavior),
                new PropertyMetadata(false, OnEnableChanged));

        public static bool GetEnable(DependencyObject obj) => (bool)obj.GetValue(EnableProperty);
        public static void SetEnable(DependencyObject obj, bool value) => obj.SetValue(EnableProperty, value);

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.PreviewTextInput += TextBox_PreviewTextInput;
                    DataObject.AddPastingHandler(textBox, TextBox_Paste);
                }
                else
                {
                    textBox.PreviewTextInput -= TextBox_PreviewTextInput;
                    DataObject.RemovePastingHandler(textBox, TextBox_Paste);
                }
            }
        }

        private static void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (char.IsDigit(e.Text, 0) || e.Text == "." || e.Text == ",")
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private static void TextBox_Paste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string pasteText = (string)e.DataObject.GetData(typeof(string));
                pasteText = pasteText.Replace(",", ".");

                string filtered = "";
                foreach (char c in pasteText)
                {
                    if (char.IsDigit(c) || c == '.')
                    {
                        filtered += c;
                    }
                }

                e.DataObject = new DataObject(typeof(string), filtered);
            }
        }
    }
}