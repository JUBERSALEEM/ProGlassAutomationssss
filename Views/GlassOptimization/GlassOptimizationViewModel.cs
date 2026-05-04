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
        // CACHE SYSTEM - Atomic single assignment only
        // ═══════════════════════════════════════════════════════

        private List<SheetItem> _sheetCache = new();
        private readonly object _cacheLock = new();

        // Pre-built dictionary indexes for O(1) lookup - NEVER use FirstOrDefault in hot paths
        private Dictionary<string, double> _purchasePriceLookup = new();
        private Dictionary<string, double> _sellPriceLookup = new();
        private Dictionary<string, decimal> _categoryMultiplierLookup = new();

        // ═══════════════════════════════════════════════════════
        // DEBOUNCE SYSTEM - Replaces DispatcherTimer
        // ═══════════════════════════════════════════════════════

        private CancellationTokenSource _debounceTokenSource;
        private const int DebounceDelayMs = 300;

        // ═══════════════════════════════════════════════════════
        // CACHED NUMERIC VALUES - Prevent string parsing in loops
        // ═══════════════════════════════════════════════════════

        private double _cachedEditWidth;
        private double _cachedEditHeight;
        private int _cachedEditQty;
        private double _cachedEditUtil;
        private double _cachedEditPrice;
        private double _cachedProfitMargin;

        // ═══════════════════════════════════════════════════════
        // COLLECTIONS
        // ═══════════════════════════════════════════════════════

        public ObservableCollection<SheetItem> Sheets { get; } = new();

        public ObservableCollection<string> Colors { get; } = new()
        {
            "Clear", "Green", "Bronze", "Grey", "Blue", "Black"
        };

        public ObservableCollection<double> ProfitOptions { get; } = new()
        {
            1.0, 1.1, 1.2, 1.3, 1.4, 1.5
        };

        // ═══════════════════════════════════════════════════════
        // EDIT PROPERTIES with Cached Numeric Values
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

        private string _editWidth = string.Empty;
        public string EditWidth
        {
            get => _editWidth;
            set
            {
                if (SetProperty(ref _editWidth, value))
                {
                    if (double.TryParse(value, out var parsed))
                        _cachedEditWidth = parsed;
                    else
                        _cachedEditWidth = 0;
                }
            }
        }

        private string _editHeight = string.Empty;
        public string EditHeight
        {
            get => _editHeight;
            set
            {
                if (SetProperty(ref _editHeight, value))
                {
                    if (double.TryParse(value, out var parsed))
                        _cachedEditHeight = parsed;
                    else
                        _cachedEditHeight = 0;
                }
            }
        }

        private string _editQty = string.Empty;
        public string EditQty
        {
            get => _editQty;
            set
            {
                if (SetProperty(ref _editQty, value))
                {
                    if (int.TryParse(value, out var parsed))
                        _cachedEditQty = parsed;
                    else
                        _cachedEditQty = 0;
                }
            }
        }

        private string _editUtil = string.Empty;
        public string EditUtil
        {
            get => _editUtil;
            set
            {
                if (SetProperty(ref _editUtil, value))
                {
                    if (double.TryParse(value, out var parsed))
                        _cachedEditUtil = parsed;
                    else
                        _cachedEditUtil = 0;
                }
            }
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

        private double _profitMargin = 1.2;
        public double ProfitMargin
        {
            get => _profitMargin;
            set
            {
                if (SetProperty(ref _profitMargin, value))
                {
                    _cachedProfitMargin = value;
                    RecalculateAllAsync();
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        // READONLY PROPERTIES with Batched Notify
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

        private string _totalResult = "0";
        public string TotalResult
        {
            get => _totalResult;
            private set => SetProperty(ref _totalResult, value);
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

            // Initialize cache from service
            RefreshCache();
        }

        // ═══════════════════════════════════════════════════════
        // CACHE SYSTEM - Atomic single snapshot assignment
        // ═══════════════════════════════════════════════════════

        public void SetCache(List<SheetItem> newCache)
        {
            lock (_cacheLock)
            {
                // ATOMIC: Single assignment only - never assign separately
                _sheetCache = newCache;

                // Build all indexes together under same lock
                RebuildLookupIndexes(newCache);
            }

            RefreshCache();
        }

        public void InvalidateCache()
        {
            lock (_cacheLock)
            {
                // ATOMIC: Clear ALL dictionaries together
                _purchasePriceLookup.Clear();
                _sellPriceLookup.Clear();
                _categoryMultiplierLookup.Clear();
                _sheetCache.Clear();
            }

            NotifyProperties(nameof(TotalSec1), nameof(TotalSec2), nameof(TotalResult));
        }

        private void RebuildLookupIndexes(List<SheetItem> items)
        {
            // Pre-build dictionary indexes for O(1) lookup
            _purchasePriceLookup.Clear();
            _sellPriceLookup.Clear();
            _categoryMultiplierLookup.Clear();

            foreach (var item in items)
            {
                var key = $"{item.Category}_{item.Thickness}";

                if (!_purchasePriceLookup.ContainsKey(key))
                    _purchasePriceLookup[key] = item.PurchasePrice;

                if (!_sellPriceLookup.ContainsKey(key))
                    _sellPriceLookup[key] = item.SellPrice;

                if (!_categoryMultiplierLookup.ContainsKey(item.Category))
                    _categoryMultiplierLookup[item.Category] = item.CategoryMultiplier;
            }
        }

        // O(1) lookup - No FirstOrDefault in hot paths
        private bool TryGetPurchasePrice(string category, string thickness, out double price)
        {
            var key = $"{category}_{thickness}";
            return _purchasePriceLookup.TryGetValue(key, out price);
        }

        private bool TryGetSellPrice(string category, string thickness, out double price)
        {
            var key = $"{category}_{thickness}";
            return _sellPriceLookup.TryGetValue(key, out price);
        }

        private bool TryGetCategoryMultiplier(string category, out decimal multiplier)
        {
            return _categoryMultiplierLookup.TryGetValue(category, out multiplier);
        }

        // ═══════════════════════════════════════════════════════
        // DEBOUNCE SYSTEM - Replaces DispatcherTimer
        // ═══════════════════════════════════════════════════════

        private async void RecalculateAllAsync()
        {
            // Cancel previous debounce
            _debounceTokenSource?.Cancel();
            _debounceTokenSource = new CancellationTokenSource();

            try
            {
                // Wait for debounce delay
                await Task.Delay(DebounceDelayMs, _debounceTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                return; // Cancelled - new calculation pending
            }

            // Perform expensive calculation
            RecalculateTotals();
        }

        // ═══════════════════════════════════════════════════════
        // BATCH NOTIFY - Single call for multiple properties
        // ═══════════════════════════════════════════════════════

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
                Name = $"Sheet {Sheets.Count + 1}",
                Color = Colors.FirstOrDefault() ?? "Clear",
                Thickness = "6mm",
                Width = 244,
                Height = 183,
                Qty = 1,
                Util = 85,
                Price = 150
            };

            Sheets.Add(sheet);
            SelectedSheet = sheet;

            OnPropertyChanged(nameof(Sheets));
            RecalculateTotals();
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

            // Use cached numeric values for calculation
            SelectedSheet.Name = EditName;
            SelectedSheet.Color = EditColor;
            SelectedSheet.Thickness = EditThickness;
            SelectedSheet.Width = _cachedEditWidth;
            SelectedSheet.Height = _cachedEditHeight;
            SelectedSheet.Qty = _cachedEditQty;
            SelectedSheet.Util = _cachedEditUtil;
            SelectedSheet.Price = _cachedEditPrice;

            // Recalculate using cached values
            CalculateSheet(SelectedSheet);

            // BATCH NOTIFY - Single call instead of multiple
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
            EditColor = sheet.Color;
            EditThickness = sheet.Thickness;
            EditWidth = sheet.Width.ToString("0");
            EditHeight = sheet.Height.ToString("0");
            EditQty = sheet.Qty.ToString();
            EditUtil = sheet.Util.ToString("0.0");
            EditPrice = sheet.Price.ToString("0");

            // Update cached values
            _cachedEditWidth = sheet.Width;
            _cachedEditHeight = sheet.Height;
            _cachedEditQty = sheet.Qty;
            _cachedEditUtil = sheet.Util;
            _cachedEditPrice = sheet.Price;
        }

        private void ClearEditor()
        {
            EditName = string.Empty;
            EditColor = string.Empty;
            EditThickness = string.Empty;
            EditWidth = string.Empty;
            EditHeight = string.Empty;
            EditQty = string.Empty;
            EditUtil = string.Empty;
            EditPrice = string.Empty;

            _cachedEditWidth = 0;
            _cachedEditHeight = 0;
            _cachedEditQty = 0;
            _cachedEditUtil = 0;
            _cachedEditPrice = 0;
        }

        // ═══════════════════════════════════════════════════════
        // CALCULATION ENGINE - Uses cached numeric values
        // ═══════════════════════════════════════════════════════

        private void RecalculateTotals()
        {
            double totalSec1 = 0;
            double totalSec2 = 0;

            foreach (var sheet in Sheets)
            {
                CalculateSheet(sheet);
                totalSec1 += sheet.Section1Value;
                totalSec2 += sheet.Section2Value;
            }

            // Use cached profit margin - no parsing
            var result = (totalSec1 * _cachedProfitMargin) - totalSec2;

            TotalSec1 = totalSec1.ToString("N0");
            TotalSec2 = totalSec2.ToString("N0");
            TotalResult = result.ToString("N0");
        }

        private void CalculateSheet(SheetItem sheet)
        {
            // Use cached numeric values
            var utilFactor = _cachedEditUtil > 0 ? _cachedEditUtil / 100.0 : 0.85;
            var price = _cachedEditPrice > 0 ? _cachedEditPrice : sheet.Price;

            // O(1) dictionary lookups - no FirstOrDefault
            if (TryGetPurchasePrice(sheet.Category, sheet.Thickness, out var purchasePrice))
            {
                sheet.Section1Value = purchasePrice / utilFactor;
            }
            else
            {
                sheet.Section1Value = price / utilFactor;
            }

            if (TryGetCategoryMultiplier(sheet.Category, out var multiplier))
            {
                var factor = (double)multiplier * 1.5;
                sheet.Section2Value = price / factor;
            }
            else
            {
                sheet.Section2Value = price / 1.5;
            }

            // Update display text
            sheet.Section1Text = sheet.Section1Value.ToString("N0");
            sheet.Section2Text = sheet.Section2Value.ToString("N0");
            sheet.ResultText = (sheet.Section1Value - sheet.Section2Value).ToString("N0");
        }

        private void RefreshCache()
        {
            // Refresh from service if available
            // This triggers the service to emit Changed event
        }

        // ═══════════════════════════════════════════════════════
        // INotifyPropertyChanged IMPLEMENTATION
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
    // SHEET ITEM MODEL
    // ═══════════════════════════════════════════════════════

    public class SheetItem : INotifyPropertyChanged
    {
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

        private double _width;
        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        private double _height;
        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        private int _qty;
        public int Qty
        {
            get => _qty;
            set => SetProperty(ref _qty, value);
        }

        private double _util = 85;
        public double Util
        {
            get => _util;
            set => SetProperty(ref _util, value);
        }

        private double _price;
        public double Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
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

        private decimal _categoryMultiplier = 1.0m;
        public decimal CategoryMultiplier
        {
            get => _categoryMultiplier;
            set => SetProperty(ref _categoryMultiplier, value);
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

        private string _resultText = "0";
        public string ResultText
        {
            get => _resultText;
            set => SetProperty(ref _resultText, value);
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
}