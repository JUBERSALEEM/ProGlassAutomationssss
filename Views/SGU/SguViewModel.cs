using ProGlassAutomation.Views.DGULamination;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ProGlassAutomation.Views.SGU
{
    public class SguViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        protected void Notify(params string[] properties)
        {
            foreach (var p in properties)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }

        private CancellationTokenSource _debounceCts;
        private readonly object _debounceLock = new();
        private const int DebounceMs = 300;

        private SheetCacheSnapshot _sheetCache;
        private readonly object _cacheLock = new();

        private Dictionary<string, double> _priceBySpecIndex = new();
        private Dictionary<string, double> _priceByThicknessIndex = new();
        private Dictionary<string, double> _priceByCategoryIndex = new();

        private double _cachedSheetPrice = 0;
        private double _cachedCutting = 5;
        private double _cachedTempering = 10;
        private double _cachedProfitFactor = 1.15;

        private static readonly string[] AllCategories = new string[]
        {
            "HD Clear", "HD Bronze", "HD Grey", "Belgium Clear", "PNA Clear",
            "Ramly Clear", "Sunlux Silver", "Reflite Silver", "Stopsol Classic",
            "Guardian Clear", "AGC Clear", "Şişecam Clear", "SGG Clear",
            "Pilkington Clear", "Tinted Bronze", "Low-E Clear", "Lacobel White"
        };

        private static readonly string[] AllThicknesses = new string[]
        {
            "2mm", "2.5mm", "3mm", "4mm", "5mm", "6mm", "8mm", "10mm", "12mm", "15mm", "19mm"
        };

        private static readonly string[] AllColors = new string[]
        {
            "Clear", "Bronze", "Dark Bronze", "Grey", "Dark Grey", "Green",
            "Blue", "Reflective Silver", "Reflective Gold", "Reflective Blue",
            "Mirror", "Mirror Silver", "White", "Black"
        };

        // CACHE REBUILD FLAGS - prevent race conditions
        private bool _isRebuildingCache = false;

        public ObservableCollection<string> CategoryOptions { get; } = new();
        public ObservableCollection<string> ThicknessOptions { get; } = new();
        public ObservableCollection<string> ColorOptions { get; } = new();
        public ObservableCollection<string> ProfitOptions { get; } = new() { "5%", "10%", "15%", "20%", "25%", "30%", "35%", "40%" };
        public ObservableCollection<RecordModel> Records { get; } = new();

        private string _cat;
        public string Category
        {
            get => _cat;
            set { _cat = value; OnPropertyChanged(); LoadColorsByCategory(); SchedulePriceLoad(); }
        }

        private string _th = "6mm";
        public string Thickness
        {
            get => _th;
            set { _th = value; OnPropertyChanged(); SchedulePriceLoad(); ScheduleCalculate(); }
        }

        private string _c = "Clear";
        public string ColorName
        {
            get => _c;
            set { _c = value; OnPropertyChanged(); SchedulePriceLoad(); ScheduleCalculate(); }
        }

        private string _p = "15%";
        public string Profit
        {
            get => _p;
            set { _p = value; UpdateProfitFactor(); Notify("PF"); ScheduleCalculate(); }
        }

        public double PF => _cachedProfitFactor;
        public string Spec => $"{Thickness} {ColorName}";

        private double _result;
        public double Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(); }
        }

        private double _sh;
        public double SheetPrice
        {
            get => _sh;
            set { _sh = value; OnPropertyChanged(); ScheduleCalculate(); }
        }

        private double _cutting = 5;
        public double Cutting
        {
            get => _cutting;
            set { _cachedCutting = value; OnPropertyChanged(); ScheduleCalculate(); }
        }

        private double _tempering = 10;
        public double Tempering
        {
            get => _tempering;
            set { _cachedTempering = value; OnPropertyChanged(); ScheduleCalculate(); }
        }

        private bool _histVis = true;
        public bool IsHistoryVisible
        {
            get => _histVis;
            set { _histVis = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand DeleteCommand { get; }

        public SguViewModel()
        {
            // AUTO-INVALIDATION: Subscribe to SheetStoreService event
            try
            {
                var service = Services.SheetStoreService.Instance;
                var evt = service.GetType().GetEvent("Changed");
                if (evt != null)
                    evt.AddEventHandler(service, new EventHandler((s, e) => InvalidateCache()));
            }
            catch { }

            LoadOptions();
            LoadPriceFromSheetStore();

            SaveCommand = new RelayCommand(o =>
            {
                Records.Insert(0, new RecordModel
                {
                    DisplayText = $"{Category} | {Thickness} {ColorName} | {Result:F2} AED | {DateTime.Now:HH:mm}"
                });
                MessageBox.Show("Saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            ExportPdfCommand = new RelayCommand(o => ExportPdf());

            ClearCommand = new RelayCommand(o =>
            {
                SheetPrice = 0;
                _cachedCutting = 5;
                _cachedTempering = 10;
                _cachedProfitFactor = 1.15;
                _th = "6mm";
                _c = "Clear";
                _cutting = 5;
                _tempering = 10;
                Notify("Cutting", "Tempering", "PF", "Thickness", "ColorName");
                if (CategoryOptions.Count > 0)
                    Category = CategoryOptions[0];
                Calculate();
            });

            ClearAllCommand = new RelayCommand(o => Records.Clear());

            DeleteCommand = new RelayCommand(o =>
            {
                if (o is RecordModel r)
                    Records.Remove(r);
            });

            Calculate();
        }

        // FIXED: Thread-safe debounce with lock
        private void ScheduleDebounced(Action action, int ms = DebounceMs)
        {
            lock (_debounceLock)
            {
                _debounceCts?.Cancel();
                _debounceCts = new CancellationTokenSource();
            }
            var token = _debounceCts.Token;
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(ms, token);
                    if (!token.IsCancellationRequested)
                        await Application.Current.Dispatcher.InvokeAsync(action);
                }
                catch (TaskCanceledException) { }
            }, token);
        }

        private void SchedulePriceLoad() => ScheduleDebounced(LoadPriceFromSheetStore);
        private void ScheduleCalculate() => ScheduleDebounced(Calculate, 50);

        private void UpdateProfitFactor()
        {
            _cachedProfitFactor = Profit switch
            {
                "5%" => 1.05,
                "10%" => 1.10,
                "15%" => 1.15,
                "20%" => 1.20,
                "25%" => 1.25,
                "30%" => 1.30,
                "35%" => 1.35,
                "40%" => 1.40,
                _ => 1.15
            };
        }

        private void LoadOptions()
        {
            foreach (var cat in AllCategories)
                CategoryOptions.Add(cat);
            foreach (var t in AllThicknesses)
                ThicknessOptions.Add(t);
            foreach (var c in AllColors)
                ColorOptions.Add(c);
            if (CategoryOptions.Count > 0)
                Category = CategoryOptions[0];
        }

        private void LoadColorsByCategory()
        {
            ColorOptions.Clear();
            if (string.IsNullOrEmpty(Category))
            {
                foreach (var c in AllColors)
                    ColorOptions.Add(c);
                return;
            }

            switch (Category)
            {
                case "HD Clear":
                case "Belgium Clear":
                case "PNA Clear":
                case "Ramly Clear":
                case "Guardian Clear":
                case "AGC Clear":
                case "Şişecam Clear":
                case "SGG Clear":
                case "Pilkington Clear":
                case "Low-E Clear":
                    ColorOptions.Add("Clear"); break;
                case "HD Bronze":
                case "Tinted Bronze":
                    ColorOptions.Add("Bronze");
                    ColorOptions.Add("Dark Bronze"); break;
                case "HD Grey":
                    ColorOptions.Add("Grey");
                    ColorOptions.Add("Dark Grey"); break;
                case "Sunlux Silver":
                case "Reflite Silver":
                    ColorOptions.Add("Reflective Silver");
                    ColorOptions.Add("Mirror Silver"); break;
                case "Stopsol Classic":
                    ColorOptions.Add("Reflective Silver");
                    ColorOptions.Add("Reflective Gold");
                    ColorOptions.Add("Reflective Blue"); break;
                case "Lacobel White":
                    ColorOptions.Add("White"); break;
                default:
                    foreach (var c in AllColors)
                        ColorOptions.Add(c); break;
            }

            if (ColorOptions.Count > 0)
                ColorName = ColorOptions[0];
        }

        private void SetCache(SheetCacheSnapshot newCache)
        {
            lock (_cacheLock)
            {
                _sheetCache = newCache;
                _priceBySpecIndex = newCache.SpecIndex;
                _priceByThicknessIndex = newCache.ThicknessIndex;
                _priceByCategoryIndex = newCache.CategoryIndex;
                _isRebuildingCache = false;
            }
        }

        public void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _sheetCache = null;
                _priceBySpecIndex.Clear();
                _priceByThicknessIndex.Clear();
                _priceByCategoryIndex.Clear();
                _isRebuildingCache = false;
            }
        }

        private void LoadPriceFromSheetStore()
        {
            try
            {
                if (string.IsNullOrEmpty(Category) || string.IsNullOrEmpty(Thickness) || string.IsNullOrEmpty(ColorName))
                {
                    _cachedSheetPrice = 0;
                    SheetPrice = 0;
                    Calculate();
                    return;
                }

                // FIXED: Prevent concurrent cache rebuilds
                bool needsRebuild;
                lock (_cacheLock)
                {
                    needsRebuild = _sheetCache == null || _sheetCache.IsStale || _isRebuildingCache;
                }

                if (needsRebuild)
                {
                    lock (_cacheLock)
                        _isRebuildingCache = true;

                    var sheets = Services.SheetStoreService.Instance.GetAllActive().ToList();

                    var newSpecIdx = new Dictionary<string, double>();
                    var newThickIdx = new Dictionary<string, double>();
                    var newCatIdx = new Dictionary<string, double>();

                    foreach (var s in sheets)
                    {
                        string sKey = $"{s.Category}|{s.Thickness}|{s.Color}";
                        string tKey = $"{s.Category}|{s.Thickness}";

                        if (!newSpecIdx.ContainsKey(sKey))
                            newSpecIdx[sKey] = (double)s.PurchasePrice;
                        if (!newThickIdx.ContainsKey(tKey))
                            newThickIdx[tKey] = (double)s.PurchasePrice;
                        if (!newCatIdx.ContainsKey(s.Category))
                            newCatIdx[s.Category] = (double)s.PurchasePrice;
                    }

                    SetCache(new SheetCacheSnapshot(newSpecIdx, newThickIdx, newCatIdx));
                }

                string lookupSpecKey = $"{Category}|{Thickness}|{ColorName}";
                if (_priceBySpecIndex.TryGetValue(lookupSpecKey, out double price))
                {
                    _cachedSheetPrice = price;
                    SheetPrice = price;
                    Calculate();
                    return;
                }

                string lookupThickKey = $"{Category}|{Thickness}";
                if (_priceByThicknessIndex.TryGetValue(lookupThickKey, out price))
                {
                    _cachedSheetPrice = price;
                    SheetPrice = price;
                    Calculate();
                    return;
                }

                if (_priceByCategoryIndex.TryGetValue(Category, out price))
                {
                    _cachedSheetPrice = price;
                    SheetPrice = price;
                    Calculate();
                    return;
                }

                _cachedSheetPrice = 0;
                SheetPrice = 0;
            }
            catch
            {
                _cachedSheetPrice = 0;
                SheetPrice = 0;
            }
        }

        public void Calculate()
        {
            Result = ((_cachedSheetPrice / 0.85) + _cachedCutting + _cachedTempering) * _cachedProfitFactor;
        }

        public void ExportPdf()
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(CreatePrintVisual(), "SGU Quotation");
                    MessageBox.Show("PDF Exported!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        Visual CreatePrintVisual()
        {
            var grid = new Grid { Width = 600, Background = Brushes.White };
            var blue = new SolidColorBrush(Color.FromRgb(37, 99, 235));
            var orange = new SolidColorBrush(Color.FromRgb(194, 65, 12));
            var yellow = new SolidColorBrush(Color.FromRgb(254, 243, 199));
            var green = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            var lightGreen = new SolidColorBrush(Color.FromRgb(187, 247, 208));
            var darkBlue = new SolidColorBrush(Color.FromRgb(30, 58, 95));

            // Header
            var header = new Border { Background = blue, Padding = new Thickness(15) };
            var headerStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            headerStack.Children.Add(new TextBlock { Text = "PRO GLASS AUTOMATION", FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Brushes.White, TextAlignment = TextAlignment.Center });
            headerStack.Children.Add(new TextBlock { Text = $"Date: {DateTime.Now:dd MMM yyyy}", FontSize = 10, Foreground = Brushes.White, TextAlignment = TextAlignment.Center });
            header.Child = headerStack;
            grid.Children.Add(header);

            // Content
            var content = new StackPanel { Margin = new Thickness(20) };
            content.Children.Add(new TextBlock { Text = "SGU QUOTATION", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = blue, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 20, 0, 20) });

            var specBox = new Border { Background = yellow, Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 20) };
            specBox.Child = new TextBlock { Text = $"Specification: {Category} | {Spec}", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = orange };
            content.Children.Add(specBox);

            content.Children.Add(new TextBlock { Text = "PRICE BREAKUP", FontSize = 14, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
            content.Children.Add(CreateDetailRow("Category:", Category));
            content.Children.Add(CreateDetailRow("Sheet Price:", $"{SheetPrice:F2} AED"));
            content.Children.Add(CreateDetailRow("Cutting Charge:", $"{_cachedCutting:F2} AED"));
            content.Children.Add(CreateDetailRow("Tempering Charge:", $"{_cachedTempering:F2} AED"));
            content.Children.Add(CreateDetailRow("Profit Margin:", Profit));

            var finalBox = new Border { Background = green, Padding = new Thickness(15), Margin = new Thickness(0, 20, 0, 20) };
            var finalStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            finalStack.Children.Add(new TextBlock { Text = "FINAL UNIT PRICE", FontSize = 10, Foreground = lightGreen, TextAlignment = TextAlignment.Center });
            finalStack.Children.Add(new TextBlock { Text = $"{Result:F2} AED", FontSize = 24, FontWeight = FontWeights.Bold, Foreground = Brushes.White, TextAlignment = TextAlignment.Center });
            finalBox.Child = finalStack;
            content.Children.Add(finalBox);

            var footer = new Border { Background = darkBlue, Padding = new Thickness(10) };
            footer.Child = new TextBlock { Text = "PRO GLASS AUTOMATION | Dubai, UAE | jubersaleem01@gmail.com", FontSize = 9, Foreground = Brushes.White, TextAlignment = TextAlignment.Center };
            content.Children.Add(footer);

            grid.Children.Add(content);
            return grid;
        }

        StackPanel CreateDetailRow(string label, string value)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };
            row.Children.Add(new TextBlock { Text = label, Width = 150 });
            row.Children.Add(new TextBlock { Text = value, FontWeight = FontWeights.Bold });
            return row;
        }
    }

    public class SheetCacheSnapshot
    {
        public Dictionary<string, double> SpecIndex { get; }
        public Dictionary<string, double> ThicknessIndex { get; }
        public Dictionary<string, double> CategoryIndex { get; }
        public DateTime CreatedAt { get; }

        public SheetCacheSnapshot(Dictionary<string, double> spec, Dictionary<string, double> thick, Dictionary<string, double> cat)
        {
            SpecIndex = spec;
            ThicknessIndex = thick;
            CategoryIndex = cat;
            CreatedAt = DateTime.UtcNow;
        }

        public bool IsStale => (DateTime.UtcNow - CreatedAt).TotalMinutes > 5;
    }

    public class RecordModel { public string DisplayText { get; set; } }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        public RelayCommand(Action<object> execute) => _execute = execute;
        public event EventHandler CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
        public bool CanExecute(object p) => true;
        public void Execute(object p) => _execute(p);
    }
}