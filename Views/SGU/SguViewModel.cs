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

        private const double CostRecoveryFactor = 0.85;

        // CATEGORIES - Match Sheet.cs Categories exactly
        private static readonly string[] AllCategories = new string[]
        {
            // CLEAR
            "Clear Float", "Ultra Clear", "Crystal Clear", "Optifloat Clear",
            
            // HD SERIES
            "HD Clear", "HD Bronze", "HD Grey", "HD Green", "HD Blue", "HD Black",
            
            // BELGIUM / PLANIBEL
            "Belgium Clear", "Belgium Bronze", "Belgium Grey", "Belgium Green",
            "Planibel Clear", "Planibel A", "Planibel Top N+",
            "Planibel Grey", "Planibel Bronze", "Planibel Green",
            
            // PNA
            "PNA Clear", "PNA Bronze", "PNA Grey", "PNA Green", "PNA Blue",
            "PNA Reflective Silver", "PNA Reflective Gold",
            
            // RAMLY
            "Ramly Clear", "Ramly Bronze", "Ramly Grey", "Ramly Green", "Ramly Blue",
            
            // SUNLUX / REFLITE
            "Sunlux Silver", "Sunlux Gold", "Sunlux Blue", "Sunlux Green", "Sunlux Bronze",
            "Reflite Silver", "Reflite Gold", "Reflite Blue", "Reflite Green", "Reflite Bronze",
            
            // STOPSOL (AGC)
            "Stopsol Classic Clear", "Stopsol Classic Bronze", "Stopsol Classic Grey",
            "Stopsol Classic Green", "Stopsol Classic Blue",
            "Stopsol Superburn Clear", "Stopsol Superburn Bronze", "Stopsol Superburn Grey",
            "Stopsol Superburn Green", "Stopsol Superburn Blue",
            "Stopsol Silver Lite", "Stopsol Silver Dark",
            
            // STOPRAY (AGC)
            "Stopray Classic Clear", "Stopray Classic Bronze", "Stopray Classic Grey",
            "Stopray Classic Green", "Stopray Classic Blue",
            "Stopray Silver", "Stopray Gold", "Stopray Vision",
            
            // CHROMAFLOAT (AGC)
            "Chromafloat Silver", "Chromafloat Gold", "Chromafloat Blue", "Chromafloat Green",
            
            // SUNERGY (AGC)
            "Sunergy Clear", "Sunergy Bronze", "Sunergy Grey", "Sunergy Green", "Sunergy Plus",
            
            // TINTED
            "Tinted Bronze", "Tinted Grey", "Tinted Green", "Tinted Blue", "Tinted Black",
            
            // GUARDIAN
            "Guardian Clear", "Guardian Ultra Clear",
            "SunGuard Clear", "SunGuard Blue", "SunGuard Green", "SunGuard Bronze", "SunGuard Grey",
            "SunGuard Neutral 63", "SunGuard Neutral 70",
            "Solarban 60", "Solarban 70", "Solarban 70XL", "Solarban 90",
            "Guardian Reflective Silver", "Guardian Reflective Gold",
            
            // AGC
            "AGC Clear", "AGC Ultra Clear", "AGC Low Iron",
            "AGC Tinted Bronze", "AGC Tinted Grey", "AGC Tinted Green", "AGC Tinted Blue",
            
            // ŞIŞECAM / TRAKYA (Turkey)
            "Şişecam Clear", "Şişecam Ultra Clear",
            "Şişecam Stopray Bronze", "Şişecam Stopray Grey", "Şişecam Stopray Green",
            "Şişecam Tinted Bronze", "Şişecam Tinted Grey", "Şişecam Tinted Green",
            "Şişecam Reflective Silver", "Şişecam Reflective Gold",
            "Trakya Clear", "Trakya Tinted", "Trakya Stopray",
            
            // SGG (Saint Gobain)
            "SGG Clear", "SGG Ultra Clear",
            "SGG Planitherm One", "SGG Planitherm Total", "SGG Planitherm Ultra N",
            "SGG Reflective Silver", "SGG Reflective Gold", "SGG Reflective Blue",
            "SGG Tinted Bronze", "SGG Tinted Grey", "SGG Tinted Green",
            "SGG Climalit", "SGG Antelio",
            
            // PILKINGTON
            "Pilkington Optifloat Clear", "Pilkington Optifloat Tinted",
            "Pilkington K Glass", "Pilkington Low-E",
            "Pilkington Sunshade", "Pilkington Arctic Blue",
            
            // PGI (South Africa)
            "PGI Clear", "PGI Tinted Bronze", "PGI Tinted Grey", "PGI Tinted Green",
            "PGI Reflective Silver", "PGI Reflective Blue",
            
            // TAIWAN GLASS
            "Taiwan Clear", "Taiwan Tinted Bronze", "Taiwan Tinted Grey", "Taiwan Tinted Green",
            "Taiwan Reflective Silver", "Taiwan Reflective Blue",
            
            // XINYI (China)
            "Xinyi Clear", "Xinyi Tinted", "Xinyi Low-E",
            
            // LOW-E
            "Low-E Clear", "Low-E Neutral", "Low-E Silver",
            "iPlus 1.0", "iPlus 1.1", "iPlus 1.2",
            "Comfort Plus", "Energy Advantage",
            
            // ANTELIO / MIRALITE / SPECTRAN
            "Antelio Silver", "Antelio Gold", "Antelio Blue", "Antelio Green",
            "Miralite Silver", "Miralite Gold", "Miralite Bronze",
            "Spectran Silver", "Spectran Blue",
            
            // SUNFILM / COMFILM
            "Sunfilm Clear", "Sunfilm Ceramic", "Sunfilm Privacy",
            "Comfilm Safety", "Comfilm UV",
            
            // PRIVACY / DECORATIVE
            "Matelux Clear", "Matelux Bronze", "Matelux Grey", "Matelux Green",
            "Decormatt", "Mastercote", "Satinato", "Masterglass",
            
            // LACOBEL (Painted)
            "Lacobel White", "Lacobel Black", "Lacobel Grey", "Lacobel Red",
            "Lacobel Blue", "Lacobel Brown", "Lacobel Green",
            "Lacobel Extra White", "Lacobel Extra Black", "Lacobel Easy Clean",
            
            // PYROGLASS (Fire Rated)
            "Pyrobel Clear", "Pyrobel Bronze",
            "Pyrostop 30", "Pyrostop 60", "Pyrostop 90",
            "Pyrodur", "Pyroguard",
            
            // CUSTOM
            "Custom", "Other"
        };

        // THICKNESS - Match Sheet.cs Thicknesses exactly
        private static readonly string[] AllThicknesses = new string[]
        {
            "2mm", "2.5mm", "3mm", "4mm", "5mm", "6mm", "8mm", "10mm", "12mm", "15mm", "19mm"
        };

        // COLORS - Match Sheet.cs ColorItems exactly
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
        public ObservableCollection<string> ProfitOptions { get; } = new() { "5%", "10%", "15%", "20%", "25%", "30%", "35%", "40%" };
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

        // Formula breakdown properties
        public double BaseCost => _cachedSheetPrice / CostRecoveryFactor;
        public double ProcessingCost => _cachedCutting + _cachedTempering;
        public double Subtotal => BaseCost + ProcessingCost;
        public double FinalPrice => Subtotal * _cachedProfitFactor;

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
                OnPropertyChanged(nameof(Result));
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
                OnPropertyChanged(nameof(Result));
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
                OnPropertyChanged(nameof(Result));
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

                foreach (var s in sheets.Take(5))
                {
                    System.Diagnostics.Debug.WriteLine($"[SGU INIT] {s.Category} | {s.Thickness} | {s.Color} | {s.PurchasePrice}");
                }
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
                _isManualSheetPrice = false;
                SheetPrice = 0;
                _cachedCutting = 5;
                _cachedTempering = 10;
                _cachedProfitFactor = 1.15;
                _th = "6mm";
                _c = "Clear";
                _cutting = 5;
                _tempering = 10;
                OnPropertyChanged(nameof(Cutting));
                OnPropertyChanged(nameof(Tempering));
                Notify("PF", "Thickness", "ColorName", "BaseCost", "ProcessingCost", "Subtotal", "FinalPrice", "Result");
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
                // Skip if user manually entered sheet price
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

                    System.Diagnostics.Debug.WriteLine($"[SGU DEBUG] Loading {sheets.Count} sheets from SheetStore");

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

                        System.Diagnostics.Debug.WriteLine($"[SGU DEBUG] Loaded: {sKey} = {sheetPrice}");
                    }

                    System.Diagnostics.Debug.WriteLine($"[SGU DEBUG] Total - Spec:{newSpecIdx.Count}, Thickness:{newThickIdx.Count}, Category:{newCatIdx.Count}");

                    SetCache(new SheetCacheSnapshot(newSpecIdx, newThickIdx, newCatIdx));
                }

                // Try exact match: Category + Thickness + Color
                string lookupSpecKey = $"{Category}|{Thickness}|{ColorName}";
                System.Diagnostics.Debug.WriteLine($"[SGU DEBUG] Looking for EXACT: '{lookupSpecKey}'");

                double price = 0;
                if (_priceBySpecIndex.TryGetValue(lookupSpecKey, out price))
                {
                    System.Diagnostics.Debug.WriteLine($"[SGU DEBUG] Found EXACT match: {price}");
                    _cachedSheetPrice = price;
                    _sh = price;
                    OnPropertyChanged(nameof(SheetPrice));
                    Calculate();
                    return;
                }

                // Try partial match: Category + Thickness (ignore Color)
                string lookupThickKey = $"{Category}|{Thickness}";
                System.Diagnostics.Debug.WriteLine($"[SGU DEBUG] Looking for THICK: '{lookupThickKey}'");

                if (_priceByThicknessIndex.TryGetValue(lookupThickKey, out price))
                {
                    System.Diagnostics.Debug.WriteLine($"[SGU DEBUG] Found THICK match: {price}");
                    _cachedSheetPrice = price;
                    _sh = price;
                    OnPropertyChanged(nameof(SheetPrice));
                    Calculate();
                    return;
                }

                // Try category only
                System.Diagnostics.Debug.WriteLine($"[SGU DEBUG] Looking for CAT: '{Category}'");

                if (_priceByCategoryIndex.TryGetValue(Category, out price))
                {
                    System.Diagnostics.Debug.WriteLine($"[SGU DEBUG] Found CAT match: {price}");
                    _cachedSheetPrice = price;
                    _sh = price;
                    OnPropertyChanged(nameof(SheetPrice));
                    Calculate();
                    return;
                }

                // Try fuzzy match (partial category name)
                price = FindBestMatch(Category, Thickness, ColorName);
                if (price > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[SGU DEBUG] Found FUZZY match: {price}");
                    _cachedSheetPrice = price;
                    _sh = price;
                    OnPropertyChanged(nameof(SheetPrice));
                    Calculate();
                    return;
                }

                System.Diagnostics.Debug.WriteLine("[SGU DEBUG] NO MATCH FOUND - Setting 0");
                _cachedSheetPrice = 0;
                _sh = 0;
                OnPropertyChanged(nameof(SheetPrice));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SGU ERROR] LoadPriceFromSheetStore: {ex.Message}");
                _cachedSheetPrice = 0;
                _sh = 0;
                OnPropertyChanged(nameof(SheetPrice));
            }
        }

        private double FindBestMatch(string category, string thickness, string color)
        {
            System.Diagnostics.Debug.WriteLine($"[SGU FUZZY] Searching for: {category}|{thickness}|{color}");

            // Try to find category that contains our search term
            foreach (var catKvp in _priceByCategoryIndex)
            {
                if (catKvp.Key.Contains(category, StringComparison.OrdinalIgnoreCase) ||
                    category.Contains(catKvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Debug.WriteLine($"[SGU FUZZY] Category match: {catKvp.Key}");

                    // Find thickness match under this category
                    foreach (var thickKvp in _priceByThicknessIndex)
                    {
                        if (thickKvp.Key.StartsWith(catKvp.Key + "|", StringComparison.OrdinalIgnoreCase) &&
                            thickKvp.Key.EndsWith("|" + thickness, StringComparison.OrdinalIgnoreCase))
                        {
                            System.Diagnostics.Debug.WriteLine($"[SGU FUZZY] Thickness match: {thickKvp.Key}");
                            return thickKvp.Value;
                        }
                    }

                    // Return category price if no thickness match
                    return catKvp.Value;
                }
            }

            // Try just thickness match
            foreach (var thickKvp in _priceByThicknessIndex)
            {
                if (thickKvp.Key.EndsWith("|" + thickness, StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Debug.WriteLine($"[SGU FUZZY] Partial thickness: {thickKvp.Key}");
                    return thickKvp.Value;
                }
            }

            return 0;
        }

        public void Calculate()
        {
            // Formula: ((SheetPrice / 0.85) + Cutting + Tempering) * ProfitFactor
            double baseCost = _cachedSheetPrice / CostRecoveryFactor;
            double processingCost = _cachedCutting + _cachedTempering;
            double subtotal = baseCost + processingCost;
            Result = subtotal * _cachedProfitFactor;
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
            content.Children.Add(CreateDetailRow("Base Cost (÷0.85):", $"{BaseCost:F2} AED"));
            content.Children.Add(CreateDetailRow("Cutting Charge:", $"{_cachedCutting:F2} AED"));
            content.Children.Add(CreateDetailRow("Tempering Charge:", $"{_cachedTempering:F2} AED"));
            content.Children.Add(CreateDetailRow("Processing Cost:", $"{ProcessingCost:F2} AED"));
            content.Children.Add(CreateDetailRow("Subtotal:", $"{Subtotal:F2} AED"));
            content.Children.Add(CreateDetailRow("Profit Margin:", Profit));
            content.Children.Add(CreateDetailRow("Profit Factor:", $"{PF:F2}x"));

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