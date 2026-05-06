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

        // [UPDATED] Wastage Factor
        private double _cachedWastageFactor = 0.85;
        private string _wastageConsider = "15";

        // [UPDATED] Profit Margin (WITH % SIGN)
        private double _cachedProfitMargin = 0.15;
        private string _profitMargin = "15%";

        // CATEGORIES
        private static readonly string[] AllCategories = new string[]
        {
            "Clear Float", "Ultra Clear", "Crystal Clear", "Optifloat Clear",
            "HD Clear", "HD Bronze", "HD Grey", "HD Green", "HD Blue", "HD Black",
            "Belgium Clear", "Belgium Bronze", "Belgium Grey", "Belgium Green",
            "Planibel Clear", "Planibel A", "Planibel Top N+",
            "Planibel Grey", "Planibel Bronze", "Planibel Green",
            "PNA Clear", "PNA Bronze", "PNA Grey", "PNA Green", "PNA Blue",
            "PNA Reflective Silver", "PNA Reflective Gold",
            "Ramly Clear", "Ramly Bronze", "Ramly Grey", "Ramly Green", "Ramly Blue",
            "Sunlux Silver", "Sunlux Gold", "Sunlux Blue", "Sunlux Green", "Sunlux Bronze",
            "Reflite Silver", "Reflite Gold", "Reflite Blue", "Reflite Green", "Reflite Bronze",
            "Stopsol Classic Clear", "Stopsol Classic Bronze", "Stopsol Classic Grey",
            "Stopsol Classic Green", "Stopsol Classic Blue",
            "Stopsol Superburn Clear", "Stopsol Superburn Bronze", "Stopsol Superburn Grey",
            "Stopsol Superburn Green", "Stopsol Superburn Blue",
            "Stopsol Silver Lite", "Stopsol Silver Dark",
            "Stopray Classic Clear", "Stopray Classic Bronze", "Stopray Classic Grey",
            "Stopray Classic Green", "Stopray Classic Blue",
            "Stopray Silver", "Stopray Gold", "Stopray Vision",
            "Chromafloat Silver", "Chromafloat Gold", "Chromafloat Blue", "Chromafloat Green",
            "Sunergy Clear", "Sunergy Bronze", "Sunergy Grey", "Sunergy Green", "Sunergy Plus",
            "Tinted Bronze", "Tinted Grey", "Tinted Green", "Tinted Blue", "Tinted Black",
            "Guardian Clear", "Guardian Ultra Clear",
            "SunGuard Clear", "SunGuard Blue", "SunGuard Green", "SunGuard Bronze", "SunGuard Grey",
            "SunGuard Neutral 63", "SunGuard Neutral 70",
            "Solarban 60", "Solarban 70", "Solarban 70XL", "Solarban 90",
            "Guardian Reflective Silver", "Guardian Reflective Gold",
            "AGC Clear", "AGC Ultra Clear", "AGC Low Iron",
            "AGC Tinted Bronze", "AGC Tinted Grey", "AGC Tinted Green", "AGC Tinted Blue",
            "Şişecam Clear", "Şişecam Ultra Clear",
            "Şişecam Stopray Bronze", "Şişecam Stopray Grey", "Şişecam Stopray Green",
            "Şişecam Tinted Bronze", "Şişecam Tinted Grey", "Şişecam Tinted Green",
            "Şişecam Reflective Silver", "Şişecam Reflective Gold",
            "Trakya Clear", "Trakya Tinted", "Trakya Stopray",
            "SGG Clear", "SGG Ultra Clear",
            "SGG Planitherm One", "SGG Planitherm Total", "SGG Planitherm Ultra N",
            "SGG Reflective Silver", "SGG Reflective Gold", "SGG Reflective Blue",
            "SGG Tinted Bronze", "SGG Tinted Grey", "SGG Tinted Green",
            "SGG Climalit", "SGG Antelio",
            "Pilkington Optifloat Clear", "Pilkington Optifloat Tinted",
            "Pilkington K Glass", "Pilkington Low-E",
            "Pilkington Sunshade", "Pilkington Arctic Blue",
            "PGI Clear", "PGI Tinted Bronze", "PGI Tinted Grey", "PGI Tinted Green",
            "PGI Reflective Silver", "PGI Reflective Blue",
            "Taiwan Clear", "Taiwan Tinted Bronze", "Taiwan Tinted Grey", "Taiwan Tinted Green",
            "Taiwan Reflective Silver", "Taiwan Reflective Blue",
            "Xinyi Clear", "Xinyi Tinted", "Xinyi Low-E",
            "Low-E Clear", "Low-E Neutral", "Low-E Silver",
            "iPlus 1.0", "iPlus 1.1", "iPlus 1.2",
            "Comfort Plus", "Energy Advantage",
            "Antelio Silver", "Antelio Gold", "Antelio Blue", "Antelio Green",
            "Miralite Silver", "Miralite Gold", "Miralite Bronze",
            "Spectran Silver", "Spectran Blue",
            "Sunfilm Clear", "Sunfilm Ceramic", "Sunfilm Privacy",
            "Comfilm Safety", "Comfilm UV",
            "Matelux Clear", "Matelux Bronze", "Matelux Grey", "Matelux Green",
            "Decormatt", "Mastercote", "Satinato", "Masterglass",
            "Lacobel White", "Lacobel Black", "Lacobel Grey", "Lacobel Red",
            "Lacobel Blue", "Lacobel Brown", "Lacobel Green",
            "Lacobel Extra White", "Lacobel Extra Black", "Lacobel Easy Clean",
            "Pyrobel Clear", "Pyrobel Bronze",
            "Pyrostop 30", "Pyrostop 60", "Pyrostop 90",
            "Pyrodur", "Pyroguard",
            "Custom", "Other"
        };

        private static readonly string[] AllThicknesses = new string[]
        {
            "2mm", "2.5mm", "3mm", "4mm", "5mm", "6mm", "8mm", "10mm", "12mm", "15mm", "19mm"
        };

        private static readonly string[] AllColors = new string[]
        {
            "Clear", "Ultra Clear", "Bronze", "Grey", "Green", "Blue", "Black",
            "Silver", "Gold", "Pink", "Ocean Blue", "Dark Grey", "Amber",
            "White", "Red", "Brown", "Neutral", "Arctic Blue"
        };

        private bool _isRebuildingCache = false;
        private bool _isManualSheetPrice = false;

        public ObservableCollection<string> CategoryOptions { get; } = new();
        public ObservableCollection<string> ThicknessOptions { get; } = new();
        public ObservableCollection<string> ColorOptions { get; } = new();

        // [NEW] Wastage Options (5% to 50%)
        public ObservableCollection<string> WastageOptions { get; } = new()
        {
            "5", "10", "15", "20", "25", "30", "35", "40", "45", "50"
        };

        // [UPDATED] Profit Margin Options (WITH % SIGN)
        public ObservableCollection<string> ProfitMarginOptions { get; } = new()
        {
            "1%", "5%", "10%", "15%", "20%", "25%", "30%", "35%", "40%", "45%",
            "50%", "60%", "70%", "80%", "90%", "100%"
        };

        public ObservableCollection<RecordModel> Records { get; } = new();

        private string _cat;
        public string Category
        {
            get => _cat;
            set { _cat = value; _isManualSheetPrice = false; OnPropertyChanged(); LoadColorsByCategory(); SchedulePriceLoad(); }
        }

        private string _th = "6mm";
        public string Thickness
        {
            get => _th;
            set { _th = value; _isManualSheetPrice = false; OnPropertyChanged(); SchedulePriceLoad(); ScheduleCalculate(); }
        }

        private string _c = "Clear";
        public string ColorName
        {
            get => _c;
            set { _c = value; _isManualSheetPrice = false; OnPropertyChanged(); SchedulePriceLoad(); ScheduleCalculate(); }
        }

        // [UPDATED] Wastage Consider Property
        public string WastageConsider
        {
            get => _wastageConsider;
            set
            {
                if (_wastageConsider == value) return;
                _wastageConsider = value;
                UpdateWastageFactor();
                Notify(nameof(WastageConsider), nameof(WastageConsiderDisplay), nameof(WastageFactor), nameof(WastageFactorDisplay));
                Calculate(); // IMMEDIATE calculate
            }
        }

        public string WastageConsiderDisplay => $"{_wastageConsider}%";
        public double WastageFactor => _cachedWastageFactor;
        public string WastageFactorDisplay => _cachedWastageFactor.ToString("0.00");

        // [UPDATED] Profit Margin Property (WITH % SIGN)
        public string ProfitMargin
        {
            get => _profitMargin;
            set
            {
                if (_profitMargin == value) return;
                _profitMargin = value;
                UpdateProfitMargin();
                Notify(nameof(ProfitMargin), nameof(ProfitMarginDisplay), nameof(ProfitMarginDisplayText), nameof(ProfitMarginFactor));
                Calculate(); // IMMEDIATE calculate
            }
        }

        // [NEW] ProfitMarginDisplayText for UI showing "15%"
        public string ProfitMarginDisplay => _profitMargin;

        // [NEW] Display text showing percentage value
        public string ProfitMarginDisplayText => $"{_cachedProfitMargin * 100:0}%";

        // [NEW] Factor display showing "1.15"
        public string ProfitMarginFactorDisplay => ProfitMarginFactor.ToString("0.00");
        public double ProfitMarginFactor => 1 + _cachedProfitMargin;

        public string Spec => $"{Thickness} {ColorName}";

        private double _result;
        public double Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(); }
        }

        // Formula breakdown properties
        public double BaseCost => _cachedSheetPrice / _cachedWastageFactor;
        public double ProcessingCost => _cachedCutting + _cachedTempering;
        public double Subtotal => BaseCost + ProcessingCost;
        public double FinalPrice => Subtotal * ProfitMarginFactor;

        private double _sh;
        public double SheetPrice
        {
            get => _sh;
            set
            {
                _sh = value;
                _cachedSheetPrice = value;
                _isManualSheetPrice = true;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BaseCost));
                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(FinalPrice));
                Calculate();
            }
        }

        private double _cutting = 5;
        public double Cutting
        {
            get => _cutting;
            set
            {
                _cutting = value;
                _cachedCutting = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProcessingCost));
                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(FinalPrice));
                Calculate();
            }
        }

        private double _tempering = 10;
        public double Tempering
        {
            get => _tempering;
            set
            {
                _tempering = value;
                _cachedTempering = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProcessingCost));
                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(FinalPrice));
                Calculate();
            }
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
            // Debug: Check SheetStoreService on startup
            try
            {
                var service = Services.SheetStoreService.Instance;
                var sheets = service.GetAllActive().ToList();
                System.Diagnostics.Debug.WriteLine($"[SGU INIT] SheetStore has {sheets.Count} sheets");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SGU INIT ERROR] {ex.Message}");
            }

            // Subscribe to SheetStoreService event
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

            // Initialize Wastage Factor and Profit Margin
            UpdateWastageFactor();
            UpdateProfitMargin();

            SaveCommand = new RelayCommand(o =>
            {
                Records.Insert(0, new RecordModel
                {
                    DisplayText = $"{Category} | {Thickness} {ColorName} | {Result:F2} AED | {DateTime.Now:HH:mm}",
                    Wastage = $"{WastageConsider}% (Factor: {_cachedWastageFactor:F2})",
                    ProfitMargin = ProfitMarginDisplay
                });
                MessageBox.Show("Saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            ExportPdfCommand = new RelayCommand(o => ExportPdf());

            ClearCommand = new RelayCommand(o =>
            {
                _isManualSheetPrice = false;
                SheetPrice = 0;
                _cachedCutting = 5;
                _cachedTempering = 10;
                _wastageConsider = "15";
                _profitMargin = "15%";
                _cachedWastageFactor = 0.85;
                _cachedProfitMargin = 0.15;
                _th = "6mm";
                _c = "Clear";
                _cutting = 5;
                _tempering = 10;

                OnPropertyChanged(nameof(Cutting));
                OnPropertyChanged(nameof(Tempering));
                Notify("Thickness", "ColorName", "BaseCost", "ProcessingCost", "Subtotal", "FinalPrice", "Result",
                       "WastageConsider", "WastageConsiderDisplay", "WastageFactor", "WastageFactorDisplay",
                       "ProfitMargin", "ProfitMarginDisplay", "ProfitMarginDisplayText", "ProfitMarginFactor", "ProfitMarginFactorDisplay");

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

        // Update Wastage Factor Method
        private void UpdateWastageFactor()
        {
            double wastage = ParsePercentage(_wastageConsider);
            _cachedWastageFactor = 1 - (wastage / 100.0);
            OnPropertyChanged(nameof(WastageFactor));
            OnPropertyChanged(nameof(WastageFactorDisplay));
            OnPropertyChanged(nameof(BaseCost));
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(FinalPrice));
        }

        // Update Profit Margin Method
        private void UpdateProfitMargin()
        {
            _cachedProfitMargin = _profitMargin switch
            {
                "1%" => 0.01,
                "5%" => 0.05,
                "10%" => 0.10,
                "15%" => 0.15,
                "20%" => 0.20,
                "25%" => 0.25,
                "30%" => 0.30,
                "35%" => 0.35,
                "40%" => 0.40,
                "45%" => 0.45,
                "50%" => 0.50,
                "60%" => 0.60,
                "70%" => 0.70,
                "80%" => 0.80,
                "90%" => 0.90,
                "100%" => 1.00,
                _ => 0.15
            };
            OnPropertyChanged(nameof(ProfitMarginDisplayText));
            OnPropertyChanged(nameof(ProfitMarginFactor));
            OnPropertyChanged(nameof(ProfitMarginFactorDisplay));
            OnPropertyChanged(nameof(FinalPrice));
        }

        // Parse Percentage Helper
        private double ParsePercentage(string value)
        {
            if (string.IsNullOrEmpty(value)) return 15.0;
            string clean = value.Replace("%", "").Trim();
            if (double.TryParse(clean, out double result))
                return result;
            return 15.0;
        }

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

            string[] commonColors = new[] { "Clear", "Ultra Clear" };

            var categoryColors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "Bronze", new[] { "Bronze", "Clear" } },
                { "Grey", new[] { "Grey", "Dark Grey", "Clear" } },
                { "Green", new[] { "Green", "Clear" } },
                { "Blue", new[] { "Blue", "Ocean Blue", "Clear" } },
                { "Black", new[] { "Black", "Clear" } },
                { "Silver", new[] { "Silver", "Clear" } },
                { "Gold", new[] { "Gold", "Clear" } },
                { "Reflective", new[] { "Silver", "Gold", "Blue", "Green" } },
                { "Lacobel", new[] { "White", "Black", "Grey", "Red", "Blue", "Brown", "Green" } },
                { "Sunlux", new[] { "Silver", "Gold", "Blue", "Green", "Clear" } },
                { "Reflite", new[] { "Silver", "Gold", "Blue", "Green", "Clear" } },
                { "Stopsol", new[] { "Clear", "Silver" } },
                { "Guardian", new[] { "Clear", "Silver" } },
                { "Low-E", new[] { "Clear", "Neutral", "Silver" } },
                { "Tinted", new[] { "Bronze", "Grey", "Green", "Blue", "Black" } },
                { "Ultra Clear", new[] { "Ultra Clear", "Clear" } }
            };

            foreach (var kvp in categoryColors)
            {
                if (Category.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var color in kvp.Value)
                    {
                        if (AllColors.Contains(color) && !ColorOptions.Contains(color))
                            ColorOptions.Add(color);
                    }
                }
            }

            if (ColorOptions.Count == 0)
            {
                foreach (var c in commonColors)
                    ColorOptions.Add(c);
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
                if (_isManualSheetPrice)
                {
                    Calculate();
                    return;
                }

                if (string.IsNullOrEmpty(Category) || string.IsNullOrEmpty(Thickness) || string.IsNullOrEmpty(ColorName))
                {
                    _cachedSheetPrice = 0;
                    _sh = 0;
                    OnPropertyChanged(nameof(SheetPrice));
                    Calculate();
                    return;
                }

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

                    var newSpecIdx = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                    var newThickIdx = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                    var newCatIdx = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

                    foreach (var s in sheets)
                    {
                        string cat = s.Category ?? "";
                        string thick = s.Thickness ?? "";
                        string color = s.Color ?? "";
                        double sheetPrice = (double)s.PurchasePrice;

                        string sKey = $"{cat}|{thick}|{color}";
                        string tKey = $"{cat}|{thick}";

                        if (!newSpecIdx.ContainsKey(sKey))
                            newSpecIdx[sKey] = sheetPrice;
                        if (!newThickIdx.ContainsKey(tKey))
                            newThickIdx[tKey] = sheetPrice;
                        if (!newCatIdx.ContainsKey(cat))
                            newCatIdx[cat] = sheetPrice;
                    }

                    SetCache(new SheetCacheSnapshot(newSpecIdx, newThickIdx, newCatIdx));
                }

                // Try exact match
                string lookupSpecKey = $"{Category}|{Thickness}|{ColorName}";
                double price = 0;

                if (_priceBySpecIndex.TryGetValue(lookupSpecKey, out price) ||
                    _priceBySpecIndex.TryGetValue($"{Category}|{Thickness}|Clear", out price) ||
                    _priceBySpecIndex.TryGetValue($"{Category}|{Thickness}|Ultra Clear", out price))
                {
                    _cachedSheetPrice = price;
                    _sh = price;
                    OnPropertyChanged(nameof(SheetPrice));
                    Calculate();
                    return;
                }

                // Try partial match
                string lookupThickKey = $"{Category}|{Thickness}";
                if (_priceByThicknessIndex.TryGetValue(lookupThickKey, out price))
                {
                    _cachedSheetPrice = price;
                    _sh = price;
                    OnPropertyChanged(nameof(SheetPrice));
                    Calculate();
                    return;
                }

                if (_priceByCategoryIndex.TryGetValue(Category, out price))
                {
                    _cachedSheetPrice = price;
                    _sh = price;
                    OnPropertyChanged(nameof(SheetPrice));
                    Calculate();
                    return;
                }

                _cachedSheetPrice = 0;
                _sh = 0;
                OnPropertyChanged(nameof(SheetPrice));
            }
            catch (Exception ex)
            {
                _cachedSheetPrice = 0;
                _sh = 0;
                OnPropertyChanged(nameof(SheetPrice));
            }
        }

        public void Calculate()
        {
            // Formula: (SheetPrice / WastageFactor) + ProcessingCost) * ProfitFactor
            double baseCost = _cachedSheetPrice / _cachedWastageFactor;
            double processingCost = _cachedCutting + _cachedTempering;
            double subtotal = baseCost + processingCost;
            Result = subtotal * ProfitMarginFactor;
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

            // Price Breakdown
            content.Children.Add(new TextBlock { Text = "PRICE BREAKUP", FontSize = 14, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
            content.Children.Add(CreateDetailRow("Category:", Category));
            content.Children.Add(CreateDetailRow("Sheet Price:", $"{_cachedSheetPrice:F2} AED"));
            content.Children.Add(CreateDetailRow("Wastage Factor:", $"{_cachedWastageFactor:F2} ({WastageConsider}%)"));
            content.Children.Add(CreateDetailRow("Base Cost (÷Wastage):", $"{BaseCost:F2} AED"));
            content.Children.Add(CreateDetailRow("Cutting Charge:", $"{_cachedCutting:F2} AED"));
            content.Children.Add(CreateDetailRow("Tempering Charge:", $"{_cachedTempering:F2} AED"));
            content.Children.Add(CreateDetailRow("Processing Cost:", $"{ProcessingCost:F2} AED"));
            content.Children.Add(CreateDetailRow("Subtotal:", $"{Subtotal:F2} AED"));
            content.Children.Add(CreateDetailRow("Profit Margin:", ProfitMarginDisplay));
            content.Children.Add(CreateDetailRow("Profit Factor:", $"{ProfitMarginFactor:F2}x"));

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

    // ============================================
    // SUPPORT CLASSES
    // ============================================

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

    public class RecordModel
    {
        public string DisplayText { get; set; }
        public string Wastage { get; set; }
        public string ProfitMargin { get; set; }
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