using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ProGlassAutomation.Views.GlassOptimization
{
    public class GlassOptimizationViewModel : INotifyPropertyChanged
    {
        // ═══════════════════════════════════════════════════════
        // CACHE SYSTEM
        // ═══════════════════════════════════════════════════════

        private List<SheetItem> _sheetCache = new();
        private readonly object _cacheLock = new();

        private Dictionary<string, double> _purchasePriceLookup = new();
        private Dictionary<string, double> _sellPriceLookup = new();

        // ═══════════════════════════════════════════════════════
        // DEBOUNCE SYSTEM
        // ═══════════════════════════════════════════════════════

        private CancellationTokenSource _debounceTokenSource;
        private const int DebounceDelayMs = 300;

        // ═══════════════════════════════════════════════════════
        // CACHED NUMERIC VALUES
        // ═══════════════════════════════════════════════════════

        private double _cachedEditPrice;
        private double _cachedEditOptimization;
        private double _cachedEditChargeableSqm;

        // ═══════════════════════════════════════════════════════
        // COLLECTIONS - From SheetOptions
        // ═══════════════════════════════════════════════════════

        public ObservableCollection<SheetItem> Sheets { get; } = new();

        public ObservableCollection<string> Colors => SheetOptions.Colors;
        public ObservableCollection<string> Categories => SheetOptions.Categories;
        public ObservableCollection<string> ThicknessOptions => SheetOptions.ThicknessOptions;
        public ObservableCollection<string> ColorTypes => SheetOptions.ColorTypes;
        public ObservableCollection<double> OptimizationOptions => SheetOptions.OptimizationOptions;
        public ObservableCollection<double> ChargeableSqmOptions => SheetOptions.ChargeableSqmOptions;

        // ═══════════════════════════════════════════════════════
        // EDIT PROPERTIES
        // ═══════════════════════════════════════════════════════

        private string _editName = string.Empty;
        public string EditName
        {
            get => _editName;
            set => SetProperty(ref _editName, value);
        }

        private string _editThickness = string.Empty;
        public string EditThickness
        {
            get => _editThickness;
            set => SetProperty(ref _editThickness, value);
        }

        private string _editColor = string.Empty;
        public string EditColor
        {
            get => _editColor;
            set => SetProperty(ref _editColor, value);
        }

        private string _editCategory = string.Empty;
        public string EditCategory
        {
            get => _editCategory;
            set => SetProperty(ref _editCategory, value);
        }

        private string _editColorType = string.Empty;
        public string EditColorType
        {
            get => _editColorType;
            set => SetProperty(ref _editColorType, value);
        }

        private string _editPrice = string.Empty;
        public string EditPrice
        {
            get => _editPrice;
            set
            {
                if (SetProperty(ref _editPrice, value))
                {
                    if (double.TryParse(value, out var parsed))
                        _cachedEditPrice = parsed;
                    else
                        _cachedEditPrice = 0;
                }
            }
        }

        private string _editOptimization = string.Empty;
        public string EditOptimization
        {
            get => _editOptimization;
            set
            {
                if (SetProperty(ref _editOptimization, value))
                {
                    if (double.TryParse(value, out var parsed))
                        _cachedEditOptimization = parsed;
                    else
                        _cachedEditOptimization = 65.80;
                }
            }
        }

        private string _editChargeableSqm = "10.03";
        public string EditChargeableSqm
        {
            get => _editChargeableSqm;
            set
            {
                if (SetProperty(ref _editChargeableSqm, value))
                {
                    if (double.TryParse(value, out var parsed))
                        _cachedEditChargeableSqm = parsed;
                    else
                        _cachedEditChargeableSqm = 10.03;
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        // GLOBAL SETTINGS
        // ═══════════════════════════════════════════════════════

        private double _optimization = 65.80;
        public double Optimization
        {
            get => _optimization;
            set
            {
                if (SetProperty(ref _optimization, value))
                {
                    _cachedEditOptimization = value;
                    RecalculateAllAsync();
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        // READONLY PROPERTIES
        // ═══════════════════════════════════════════════════════

        private string _totalSec1 = "0";
        public string TotalSec1
        {
            get => _totalSec1;
            private set => SetProperty(ref _totalSec1, value);
        }

        private string _totalSec2 = "0";
        public string TotalSec2
        {
            get => _totalSec2;
            private set => SetProperty(ref _totalSec2, value);
        }

        private string _totalBaseResult = "0";
        public string TotalBaseResult
        {
            get => _totalBaseResult;
            private set => SetProperty(ref _totalBaseResult, value);
        }

        private string _totalFinalResult = "0";
        public string TotalFinalResult
        {
            get => _totalFinalResult;
            private set => SetProperty(ref _totalFinalResult, value);
        }

        private SheetItem? _selectedSheet;
        public SheetItem? SelectedSheet
        {
            get => _selectedSheet;
            set
            {
                if (SetProperty(ref _selectedSheet, value))
                {
                    LoadSheetToEditor(value);
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        // COMMANDS
        // ═══════════════════════════════════════════════════════

        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand UpdateCommand { get; }

        // ═══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════

        public GlassOptimizationViewModel()
        {
            AddCommand = new RelayCommand(_ => AddSheet());
            RemoveCommand = new RelayCommand(param => RemoveSheet(param as SheetItem));
            ClearAllCommand = new RelayCommand(_ => ClearAll());
            UpdateCommand = new RelayCommand(_ => UpdateSheet(), _ => SelectedSheet != null);

            RefreshCache();
        }

        // ═══════════════════════════════════════════════════════
        // CACHE SYSTEM
        // ═══════════════════════════════════════════════════════

        public void SetCache(List<SheetItem> newCache)
        {
            lock (_cacheLock)
            {
                _sheetCache = newCache;
                RebuildLookupIndexes(newCache);
            }

            RefreshCache();
        }

        public void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _purchasePriceLookup.Clear();
                _sellPriceLookup.Clear();
                _sheetCache.Clear();
            }

            NotifyProperties(nameof(TotalSec1), nameof(TotalSec2), nameof(TotalBaseResult), nameof(TotalFinalResult));
        }

        private void RebuildLookupIndexes(List<SheetItem> items)
        {
            _purchasePriceLookup.Clear();
            _sellPriceLookup.Clear();

            foreach (var item in items)
            {
                var key = $"{item.Category}_{item.Thickness}";

                if (!_purchasePriceLookup.ContainsKey(key))
                    _purchasePriceLookup[key] = item.PurchasePrice;

                if (!_sellPriceLookup.ContainsKey(key))
                    _sellPriceLookup[key] = item.SellPrice;
            }
        }

        private bool TryGetPurchasePrice(string category, string thickness, out double price)
        {
            var key = $"{category}_{thickness}";
            return _purchasePriceLookup.TryGetValue(key, out price);
        }

        // ═══════════════════════════════════════════════════════
        // DEBOUNCE SYSTEM
        // ═══════════════════════════════════════════════════════

        private async void RecalculateAllAsync()
        {
            _debounceTokenSource?.Cancel();
            _debounceTokenSource = new CancellationTokenSource();

            try
            {
                await Task.Delay(DebounceDelayMs, _debounceTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            RecalculateTotals();
        }

        private void NotifyProperties(params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                OnPropertyChanged(name);
            }
        }

        // ═══════════════════════════════════════════════════════
        // CRUD OPERATIONS
        // ═══════════════════════════════════════════════════════

        private void AddSheet()
        {
            var sheet = new SheetItem
            {
                Id = Sheets.Count + 1,
                Name = string.IsNullOrEmpty(EditName) ? $"Sheet {Sheets.Count + 1}" : EditName,
                Category = string.IsNullOrEmpty(EditCategory) ? (Categories.FirstOrDefault() ?? "Clear") : EditCategory,
                Thickness = string.IsNullOrEmpty(EditThickness) ? (ThicknessOptions.FirstOrDefault() ?? "6mm") : EditThickness,
                Color = string.IsNullOrEmpty(EditColor) ? (Colors.FirstOrDefault() ?? "Clear") : EditColor,
                ColorType = string.IsNullOrEmpty(EditColorType) ? (ColorTypes.FirstOrDefault() ?? "Standard") : EditColorType,
                Price = _cachedEditPrice > 0 ? _cachedEditPrice : 47,
                Optimization = _cachedEditOptimization > 0 ? _cachedEditOptimization : 65.80,
                ChargeableSqm = _cachedEditChargeableSqm > 0 ? _cachedEditChargeableSqm : 10.03
            };

            Sheets.Add(sheet);
            SelectedSheet = sheet;

            // Clear editor for next entry
            ClearEditor();

            OnPropertyChanged(nameof(Sheets));
        }

        private void RemoveSheet(SheetItem? sheet)
        {
            if (sheet == null) return;

            Sheets.Remove(sheet);
            SelectedSheet = null;

            OnPropertyChanged(nameof(Sheets));
            RecalculateTotals();
        }

        private void ClearAll()
        {
            Sheets.Clear();
            SelectedSheet = null;
            ClearEditor();

            OnPropertyChanged(nameof(Sheets));
            RecalculateTotals();
        }

        private void UpdateSheet()
        {
            if (SelectedSheet == null) return;

            SelectedSheet.Name = EditName;
            SelectedSheet.Category = EditCategory;
            SelectedSheet.Thickness = EditThickness;
            SelectedSheet.Color = EditColor;
            SelectedSheet.ColorType = EditColorType;
            SelectedSheet.Price = _cachedEditPrice;
            SelectedSheet.Optimization = _cachedEditOptimization;
            SelectedSheet.ChargeableSqm = _cachedEditChargeableSqm;

            CalculateSheet(SelectedSheet);

            OnPropertyChanged(nameof(Sheets));
            RecalculateTotals();
        }

        private void LoadSheetToEditor(SheetItem? sheet)
        {
            if (sheet == null)
            {
                ClearEditor();
                return;
            }

            EditName = sheet.Name;
            EditCategory = sheet.Category;
            EditThickness = sheet.Thickness;
            EditColor = sheet.Color;
            EditColorType = sheet.ColorType;
            EditPrice = sheet.Price.ToString("0");
            EditOptimization = sheet.Optimization.ToString("0.00");
            EditChargeableSqm = sheet.ChargeableSqm.ToString("0.00");

            _cachedEditPrice = sheet.Price;
            _cachedEditOptimization = sheet.Optimization;
            _cachedEditChargeableSqm = sheet.ChargeableSqm;
        }

        private void ClearEditor()
        {
            EditName = string.Empty;
            EditCategory = string.Empty;
            EditThickness = string.Empty;
            EditColor = string.Empty;
            EditColorType = string.Empty;
            EditPrice = string.Empty;
            EditOptimization = string.Empty;
            EditChargeableSqm = string.Empty;

            _cachedEditPrice = 0;
            _cachedEditOptimization = 65.80;
            _cachedEditChargeableSqm = 10.03;
        }

        // ═══════════════════════════════════════════════════════
        // FORMULA
        // ═══════════════════════════════════════════════════════
        //
        // SEC1 = Price ÷ (Optimization ÷ 100)
        // SEC2 = Price ÷ 0.85
        // BASE = SEC1 - SEC2
        // FINAL = BASE × ChargeableSqm
        // ═══════════════════════════════════════════════════════

        private void RecalculateTotals()
        {
            double totalSec1 = 0;
            double totalSec2 = 0;
            double totalBaseResult = 0;
            double totalFinalResult = 0;

            foreach (var sheet in Sheets)
            {
                totalSec1 += sheet.Section1Value;
                totalSec2 += sheet.Section2Value;
                totalBaseResult += sheet.BaseResultValue;
                totalFinalResult += sheet.FinalResultValue;
            }

            TotalSec1 = totalSec1.ToString("N2");
            TotalSec2 = totalSec2.ToString("N2");
            TotalBaseResult = totalBaseResult.ToString("N2");
            TotalFinalResult = totalFinalResult.ToString("N2");
        }

        private void CalculateSheet(SheetItem sheet)
        {
            var optimization = sheet.Optimization / 100.0;
            var price = sheet.Price;
            var chargeableSqm = sheet.ChargeableSqm;

            sheet.Section1Value = price / optimization;
            sheet.Section2Value = price / 0.85;
            sheet.BaseResultValue = sheet.Section1Value - sheet.Section2Value;
            sheet.FinalResultValue = sheet.BaseResultValue * chargeableSqm;

            sheet.Section1Text = sheet.Section1Value.ToString("N2");
            sheet.Section2Text = sheet.Section2Value.ToString("N2");
            sheet.BaseResultText = sheet.BaseResultValue.ToString("N2");
            sheet.FinalResultText = sheet.FinalResultValue.ToString("N2");
        }

        private void RefreshCache()
        {
            // Refresh from service if available
        }

        // ═══════════════════════════════════════════════════════
        // INotifyPropertyChanged
        // ═══════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════
    // SHEET ITEM MODEL - With Live Calculation
    // ═══════════════════════════════════════════════════════

    public class SheetItem : INotifyPropertyChanged
    {
        public SheetItem()
        {
            PropertyChanged += SheetItem_PropertyChanged;
        }

        private void SheetItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Price) ||
                e.PropertyName == nameof(Optimization) ||
                e.PropertyName == nameof(ChargeableSqm))
            {
                RecalculateResults();
            }
        }

        private void RecalculateResults()
        {
            var optimization = Optimization / 100.0;

            if (optimization > 0)
            {
                Section1Value = Price / optimization;
            }

            Section2Value = Price / 0.85;
            BaseResultValue = Section1Value - Section2Value;
            FinalResultValue = BaseResultValue * ChargeableSqm;

            Section1Text = Section1Value.ToString("N2");
            Section2Text = Section2Value.ToString("N2");
            BaseResultText = BaseResultValue.ToString("N2");
            FinalResultText = FinalResultValue.ToString("N2");
        }

        private int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _category = "Clear";
        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        private string _color = string.Empty;
        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        private string _thickness = string.Empty;
        public string Thickness
        {
            get => _thickness;
            set => SetProperty(ref _thickness, value);
        }

        private string _colorType = "Standard";
        public string ColorType
        {
            get => _colorType;
            set => SetProperty(ref _colorType, value);
        }

        private double _price;
        public double Price
        {
            get => _price;
            set
            {
                if (SetProperty(ref _price, value))
                {
                    RecalculateResults();
                }
            }
        }

        private double _optimization = 65.80;
        public double Optimization
        {
            get => _optimization;
            set
            {
                if (SetProperty(ref _optimization, value))
                {
                    RecalculateResults();
                }
            }
        }

        private double _chargeableSqm = 10.03;
        public double ChargeableSqm
        {
            get => _chargeableSqm;
            set
            {
                if (SetProperty(ref _chargeableSqm, value))
                {
                    RecalculateResults();
                }
            }
        }

        private double _purchasePrice;
        public double PurchasePrice
        {
            get => _purchasePrice;
            set => SetProperty(ref _purchasePrice, value);
        }

        private double _sellPrice;
        public double SellPrice
        {
            get => _sellPrice;
            set => SetProperty(ref _sellPrice, value);
        }

        // Calculation Results
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

    // ═══════════════════════════════════════════════════════
    // RELAY COMMAND
    // ═══════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════
    // SHEET OPTIONS - Static collections
    // ═══════════════════════════════════════════════════════

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

        public static ObservableCollection<double> OptimizationOptions { get; } = new()
        {
            50.0, 55.0, 60.0, 65.0, 65.80, 70.0, 75.0, 80.0, 85.0, 90.0
        };

        public static ObservableCollection<double> ChargeableSqmOptions { get; } = new()
        {
            0.5, 0.75, 1.0, 1.25, 1.5, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0, 10.03
        };
    }
}