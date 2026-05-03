using ProGlassAutomation.Models;
using ProGlassAutomation.Services;
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

namespace ProGlassAutomation.Views.Lamination
{
    public class LaminationViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void Notify(params string[] props)
        {
            foreach (var p in props)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }

        protected bool Set<T>(ref T field, T value, params string[] props)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            Notify(props);
            return true;
        }

        // ================= DEBOUNCE =================
        private CancellationTokenSource _debounceCts;
        private readonly object _debounceLock = new();
        private const int DebounceMs = 250;

        // ================= ATOMIC CACHE =================
        private volatile CacheSnapshot _cache = CacheSnapshot.Empty;
        private readonly object _cacheLock = new();

        private sealed class CacheSnapshot
        {
            public Dictionary<string, double> SpecIndex { get; init; }
            public Dictionary<string, double> ThicknessIndex { get; init; }
            public Dictionary<string, double> CategoryIndex { get; init; }
            public HashSet<string> Categories { get; init; } = new();
            public HashSet<string> Thicknesses { get; init; } = new();
            public HashSet<string> Colors { get; init; } = new();
            public DateTime Timestamp { get; init; }
            public static CacheSnapshot Empty => new() { Timestamp = DateTime.MinValue };
            public bool IsStale => (DateTime.Now - Timestamp).TotalMinutes > 5;
        }

        // ================= NUMERIC CACHING =================
        private double _cachedSheet1 = 0, _cachedSheet2 = 0;
        private double _cachedCutting = 0, _cachedTempering = 0;
        private double _cachedPVBPrice = 110;
        private double _cachedProfitFactor = 0.85;

        // ================= LOOKUPS - FIXED =================
        private static readonly Dictionary<string, double> PVBPrices = new()
        {
            { "Standard Clear", 110 }, { "Standard White", 115 }, { "Standard Black", 120 },
            { "Premium Clear", 125 }, { "Premium White", 130 }, { "Premium Black", 135 },
            { "Architectural Clear", 140 }, { "Architectural White", 145 }, { "Architectural Black", 150 }
        };

        private static readonly Dictionary<string, double> ProfitFactors = new()
        {
            { "5%", 0.95 }, { "10%", 0.90 }, { "15%", 0.85 }, { "20%", 0.80 },
            { "25%", 0.75 }, { "30%", 0.70 }, { "35%", 0.65 }, { "40%", 0.60 }
        };

        // ================= COLLECTIONS =================
        public ObservableCollection<string> CategoryOptions1 { get; } = new();
        public ObservableCollection<string> CategoryOptions2 { get; } = new();
        public ObservableCollection<string> ThicknessOptions1 { get; } = new();
        public ObservableCollection<string> ThicknessOptions2 { get; } = new();
        public ObservableCollection<string> ColorOptions1 { get; } = new();
        public ObservableCollection<string> ColorOptions2 { get; } = new();
        public ObservableCollection<string> PVBOptions { get; } = new()
        {
            "Standard Clear", "Standard White", "Standard Black",
            "Premium Clear", "Premium White", "Premium Black",
            "Architectural Clear", "Architectural White", "Architectural Black"
        };
        public ObservableCollection<string> ProfitOptions { get; } = new()
            { "5%", "10%", "15%", "20%", "25%", "30%", "35%", "40%" };
        public ObservableCollection<LaminationRecord> Records { get; } = new();

        // ================= SHEET 1 PROPERTIES =================
        private string _cat1 = "";
        public string Category1
        {
            get => _cat1;
            set
            {
                if (_cat1 == value) return;
                _cat1 = value;
                Notify(nameof(Category1));
                LoadThicknessOptions1ByCategory();
                LoadColorOptions1ByCategory();
                LoadPrice1FromCache();
                ScheduleCalc();
            }
        }

        private string _th1 = "";
        public string Thickness1
        {
            get => _th1;
            set
            {
                if (_th1 == value) return;
                _th1 = value;
                Notify(nameof(Thickness1));
                LoadColorOptions1ByThickness();
                LoadPrice1FromCache();
                ScheduleCalc();
            }
        }

        private string _color1 = "";
        public string Color1
        {
            get => _color1;
            set
            {
                if (_color1 == value) return;
                _color1 = value;
                Notify(nameof(Color1));
                LoadPrice1FromCache();
                ScheduleCalc();
            }
        }

        private double _sheet1;
        public double Sheet1 { get => _sheet1; set => Set(ref _sheet1, value, nameof(Sheet1)); }

        // ================= SHEET 2 PROPERTIES =================
        private string _cat2 = "";
        public string Category2
        {
            get => _cat2;
            set
            {
                if (_cat2 == value) return;
                _cat2 = value;
                Notify(nameof(Category2));
                LoadThicknessOptions2ByCategory();
                LoadColorOptions2ByCategory();
                LoadPrice2FromCache();
                ScheduleCalc();
            }
        }

        private string _th2 = "";
        public string Thickness2
        {
            get => _th2;
            set
            {
                if (_th2 == value) return;
                _th2 = value;
                Notify(nameof(Thickness2));
                LoadColorOptions2ByThickness();
                LoadPrice2FromCache();
                ScheduleCalc();
            }
        }

        private string _color2 = "";
        public string Color2
        {
            get => _color2;
            set
            {
                if (_color2 == value) return;
                _color2 = value;
                Notify(nameof(Color2));
                LoadPrice2FromCache();
                ScheduleCalc();
            }
        }

        private double _sheet2;
        public double Sheet2 { get => _sheet2; set => Set(ref _sheet2, value, nameof(Sheet2)); }

        // ================= OTHER PROPERTIES =================
        private double _cutting;
        public double Cutting
        {
            get => _cutting;
            set { if (_cutting == value) return; _cutting = value; _cachedCutting = value; Notify(nameof(Cutting)); ScheduleCalc(); }
        }

        private double _tempering;
        public double Tempering
        {
            get => _tempering;
            set { if (_tempering == value) return; _tempering = value; _cachedTempering = value; Notify(nameof(Tempering)); ScheduleCalc(); }
        }

        private string _pvbType = "Standard Clear";
        public string PVBType
        {
            get => _pvbType;
            set
            {
                if (_pvbType == value) return;
                _pvbType = value;
                Notify(nameof(PVBType));

                // FIXED: Use TryGetValue instead of GetValueOrDefault
                if (PVBPrices.TryGetValue(value, out double pvbPrice))
                    _cachedPVBPrice = pvbPrice;
                else
                    _cachedPVBPrice = 110;

                PVBPrice = _cachedPVBPrice;
                Notify(nameof(PVBPrice));
                ScheduleCalc();
            }
        }

        private double _pvbPrice = 110;
        public double PVBPrice { get => _pvbPrice; set => Set(ref _pvbPrice, value, nameof(PVBPrice)); }

        private string _profit = "15%";
        public string Profit
        {
            get => _profit;
            set
            {
                if (_profit == value) return;
                _profit = value;
                Notify(nameof(Profit));

                // FIXED: Use TryGetValue instead of GetValueOrDefault
                if (ProfitFactors.TryGetValue(value, out double factor))
                    _cachedProfitFactor = factor;
                else
                    _cachedProfitFactor = 0.85;

                Notify(nameof(ProfitFactor));
                ScheduleCalc();
            }
        }

        public double ProfitFactor => _cachedProfitFactor;

        // ================= RESULTS =================
        private double _result1, _result2, _result3, _result4, _result;
        public double Result1 { get => _result1; set => Set(ref _result1, value, nameof(Result1)); }
        public double Result2 { get => _result2; set => Set(ref _result2, value, nameof(Result2)); }
        public double Result3 { get => _result3; set => Set(ref _result3, value, nameof(Result3)); }
        public double Result4 { get => _result4; set => Set(ref _result4, value, nameof(Result4)); }
        public double Result { get => _result; set => Set(ref _result, value, nameof(Result)); }

        private bool _isHistoryVisible = true;
        public bool IsHistoryVisible { get => _isHistoryVisible; set => Set(ref _isHistoryVisible, value, nameof(IsHistoryVisible)); }

        // ================= COMMANDS =================
        public ICommand SaveCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand DeleteCommand { get; }

        // ================= CONSTRUCTOR =================
        public LaminationViewModel()
        {
            SheetStoreService.Instance.DataChanged += OnSheetStoreChanged;

            LoadAllOptions();
            EnsureCache();

            if (CategoryOptions1.Count > 0) Category1 = CategoryOptions1[0];
            if (ThicknessOptions1.Count > 0) Thickness1 = ThicknessOptions1[0];
            if (ColorOptions1.Count > 0) Color1 = ColorOptions1[0];

            if (CategoryOptions2.Count > 0) Category2 = CategoryOptions2.Count > 1 ? CategoryOptions2[1] : CategoryOptions2[0];
            if (ThicknessOptions2.Count > 0) Thickness2 = ThicknessOptions2[0];
            if (ColorOptions2.Count > 0) Color2 = ColorOptions2[0];

            SaveCommand = new RelayCommand(o => Save());
            ClearCommand = new RelayCommand(o => Clear());
            DeleteCommand = new RelayCommand(o => { if (o is LaminationRecord r) Records.Remove(r); });
            ClearAllCommand = new RelayCommand(o => Records.Clear());

            CalcInternal();
        }

        private void OnSheetStoreChanged(object sender, EventArgs e)
        {
            InvalidateCache();
            Application.Current.Dispatcher.Invoke(() => LoadAllOptions());
        }

        // ================= DEBOUNCE =================
        private void ScheduleCalc()
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
                    await Task.Delay(DebounceMs, token);
                    if (!token.IsCancellationRequested)
                        await Application.Current.Dispatcher.InvokeAsync(CalcInternal);
                }
                catch (TaskCanceledException) { }
            }, token);
        }

        // ================= ATOMIC CACHE =================
        private void EnsureCache()
        {
            var cache = _cache;
            if (!cache.IsStale && cache.SpecIndex.Count > 0) return;
            SetCache();
        }

        private void SetCache()
        {
            var sheets = SheetStoreService.Instance.GetAllActive().ToList();

            var newSpecIdx = new Dictionary<string, double>();
            var newThickIdx = new Dictionary<string, double>();
            var newCatIdx = new Dictionary<string, double>();
            var cats = new HashSet<string>();
            var ths = new HashSet<string>();
            var clrs = new HashSet<string>();

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

                if (!string.IsNullOrEmpty(s.Category)) cats.Add(s.Category);
                if (!string.IsNullOrEmpty(s.Thickness)) ths.Add(s.Thickness);
                if (!string.IsNullOrEmpty(s.Color)) clrs.Add(s.Color);
            }

            _cache = new CacheSnapshot
            {
                SpecIndex = newSpecIdx,
                ThicknessIndex = newThickIdx,
                CategoryIndex = newCatIdx,
                Categories = cats,
                Thicknesses = ths,
                Colors = clrs,
                Timestamp = DateTime.Now
            };
        }

        public void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _cache = CacheSnapshot.Empty;
            }
        }

        // ================= LOAD ALL OPTIONS =================
        private void LoadAllOptions()
        {
            CategoryOptions1.Clear();
            CategoryOptions2.Clear();
            ThicknessOptions1.Clear();
            ThicknessOptions2.Clear();
            ColorOptions1.Clear();
            ColorOptions2.Clear();

            EnsureCache();
            var cache = _cache;

            foreach (var c in cache.Categories.OrderBy(x => x))
            {
                CategoryOptions1.Add(c);
                CategoryOptions2.Add(c);
            }

            // FIXED: Sort thicknesses properly
            var sortedThs = cache.Thicknesses.OrderBy(t => ParseThickness(t)).ToList();

            foreach (var t in sortedThs)
            {
                ThicknessOptions1.Add(t);
                ThicknessOptions2.Add(t);
            }

            foreach (var c in cache.Colors.OrderBy(x => x))
            {
                ColorOptions1.Add(c);
                ColorOptions2.Add(c);
            }

            if (CategoryOptions1.Count == 0)
            {
                foreach (var d in new[] { "HD Clear", "HD Bronze", "HD Grey", "Guardian Clear", "AGC Clear" })
                {
                    CategoryOptions1.Add(d);
                    CategoryOptions2.Add(d);
                }
            }
            if (ThicknessOptions1.Count == 0)
            {
                foreach (var d in new[] { "2mm", "3mm", "4mm", "5mm", "6mm", "8mm", "10mm", "12mm" })
                {
                    ThicknessOptions1.Add(d);
                    ThicknessOptions2.Add(d);
                }
            }
            if (ColorOptions1.Count == 0)
            {
                foreach (var d in new[] { "Clear", "Bronze", "Grey", "Green", "Blue", "Reflective Silver", "White" })
                {
                    ColorOptions1.Add(d);
                    ColorOptions2.Add(d);
                }
            }

            RefreshSelections();
        }

        // FIXED: Helper method for thickness sorting
        private double ParseThickness(string t)
        {
            if (string.IsNullOrEmpty(t)) return 0;
            string cleaned = t.Replace("mm", "").Trim();
            if (double.TryParse(cleaned, out double val))
                return val;
            return 0;
        }

        private void RefreshSelections()
        {
            if (!string.IsNullOrEmpty(Category1) && !CategoryOptions1.Contains(Category1) && CategoryOptions1.Count > 0)
                Category1 = CategoryOptions1[0];
            if (!string.IsNullOrEmpty(Thickness1) && !ThicknessOptions1.Contains(Thickness1) && ThicknessOptions1.Count > 0)
                Thickness1 = ThicknessOptions1[0];
            if (!string.IsNullOrEmpty(Color1) && !ColorOptions1.Contains(Color1) && ColorOptions1.Count > 0)
                Color1 = ColorOptions1[0];

            if (!string.IsNullOrEmpty(Category2) && !CategoryOptions2.Contains(Category2) && CategoryOptions2.Count > 0)
                Category2 = CategoryOptions2[0];
            if (!string.IsNullOrEmpty(Thickness2) && !ThicknessOptions2.Contains(Thickness2) && ThicknessOptions2.Count > 0)
                Thickness2 = ThicknessOptions2[0];
            if (!string.IsNullOrEmpty(Color2) && !ColorOptions2.Contains(Color2) && ColorOptions2.Count > 0)
                Color2 = ColorOptions2[0];
        }

        // ================= SHEET 1 THICKNESS/COLOR LOADERS =================
        private void LoadThicknessOptions1ByCategory()
        {
            if (string.IsNullOrEmpty(Category1)) return;

            var cache = _cache;
            var thicknesses = new List<string>();

            foreach (var kvp in cache.ThicknessIndex)
            {
                if (kvp.Key.StartsWith(Category1 + "|"))
                {
                    var t = kvp.Key.Replace(Category1 + "|", "");
                    if (!thicknesses.Contains(t)) thicknesses.Add(t);
                }
            }

            foreach (var t in ThicknessOptions1)
                if (!thicknesses.Contains(t)) thicknesses.Add(t);

            thicknesses = thicknesses.OrderBy(t => ParseThickness(t)).ToList();

            if (thicknesses.Count == 0) thicknesses.Add("4mm");

            ThicknessOptions1.Clear();
            foreach (var t in thicknesses) ThicknessOptions1.Add(t);

            if (!thicknesses.Contains(Thickness1) && ThicknessOptions1.Count > 0)
                Thickness1 = ThicknessOptions1[0];
        }

        private void LoadColorOptions1ByCategory()
        {
            if (string.IsNullOrEmpty(Category1)) return;

            var cache = _cache;
            var colors = new List<string>();

            foreach (var kvp in cache.SpecIndex)
            {
                if (kvp.Key.StartsWith(Category1 + "|"))
                {
                    var parts = kvp.Key.Split('|');
                    if (parts.Length >= 3 && !colors.Contains(parts[2]))
                        colors.Add(parts[2]);
                }
            }

            foreach (var c in ColorOptions1)
                if (!colors.Contains(c)) colors.Add(c);

            colors.Sort();

            ColorOptions1.Clear();
            foreach (var c in colors) ColorOptions1.Add(c);

            if (ColorOptions1.Count == 0) ColorOptions1.Add("Clear");
            if (!colors.Contains(Color1) && ColorOptions1.Count > 0)
                Color1 = ColorOptions1[0];
        }

        private void LoadColorOptions1ByThickness()
        {
            if (string.IsNullOrEmpty(Category1) || string.IsNullOrEmpty(Thickness1)) return;

            var cache = _cache;
            var colors = new List<string>();

            string prefix = $"{Category1}|{Thickness1}|";
            foreach (var kvp in cache.SpecIndex)
            {
                if (kvp.Key.StartsWith(prefix))
                {
                    var parts = kvp.Key.Split('|');
                    if (parts.Length >= 3 && !colors.Contains(parts[2]))
                        colors.Add(parts[2]);
                }
            }

            colors.Sort();

            if (colors.Count > 0)
            {
                ColorOptions1.Clear();
                foreach (var c in colors) ColorOptions1.Add(c);
                if (!colors.Contains(Color1))
                    Color1 = ColorOptions1[0];
            }
        }

        // ================= SHEET 2 THICKNESS/COLOR LOADERS =================
        private void LoadThicknessOptions2ByCategory()
        {
            if (string.IsNullOrEmpty(Category2)) return;

            var cache = _cache;
            var thicknesses = new List<string>();

            foreach (var kvp in cache.ThicknessIndex)
            {
                if (kvp.Key.StartsWith(Category2 + "|"))
                {
                    var t = kvp.Key.Replace(Category2 + "|", "");
                    if (!thicknesses.Contains(t)) thicknesses.Add(t);
                }
            }

            foreach (var t in ThicknessOptions2)
                if (!thicknesses.Contains(t)) thicknesses.Add(t);

            thicknesses = thicknesses.OrderBy(t => ParseThickness(t)).ToList();

            if (thicknesses.Count == 0) thicknesses.Add("4mm");

            ThicknessOptions2.Clear();
            foreach (var t in thicknesses) ThicknessOptions2.Add(t);

            if (!thicknesses.Contains(Thickness2) && ThicknessOptions2.Count > 0)
                Thickness2 = ThicknessOptions2[0];
        }

        private void LoadColorOptions2ByCategory()
        {
            if (string.IsNullOrEmpty(Category2)) return;

            var cache = _cache;
            var colors = new List<string>();

            foreach (var kvp in cache.SpecIndex)
            {
                if (kvp.Key.StartsWith(Category2 + "|"))
                {
                    var parts = kvp.Key.Split('|');
                    if (parts.Length >= 3 && !colors.Contains(parts[2]))
                        colors.Add(parts[2]);
                }
            }

            foreach (var c in ColorOptions2)
                if (!colors.Contains(c)) colors.Add(c);

            colors.Sort();

            ColorOptions2.Clear();
            foreach (var c in colors) ColorOptions2.Add(c);

            if (ColorOptions2.Count == 0) ColorOptions2.Add("Clear");
            if (!colors.Contains(Color2) && ColorOptions2.Count > 0)
                Color2 = ColorOptions2[0];
        }

        private void LoadColorOptions2ByThickness()
        {
            if (string.IsNullOrEmpty(Category2) || string.IsNullOrEmpty(Thickness2)) return;

            var cache = _cache;
            var colors = new List<string>();

            string prefix = $"{Category2}|{Thickness2}|";
            foreach (var kvp in cache.SpecIndex)
            {
                if (kvp.Key.StartsWith(prefix))
                {
                    var parts = kvp.Key.Split('|');
                    if (parts.Length >= 3 && !colors.Contains(parts[2]))
                        colors.Add(parts[2]);
                }
            }

            colors.Sort();

            if (colors.Count > 0)
            {
                ColorOptions2.Clear();
                foreach (var c in colors) ColorOptions2.Add(c);
                if (!colors.Contains(Color2))
                    Color2 = ColorOptions2[0];
            }
        }

        // ================= LOAD PRICES FROM CACHE =================
        private void LoadPrice1FromCache()
        {
            EnsureCache();

            if (string.IsNullOrEmpty(Category1) || string.IsNullOrEmpty(Thickness1) || string.IsNullOrEmpty(Color1))
            {
                _cachedSheet1 = 0;
                Sheet1 = 0;
                return;
            }

            string specKey = $"{Category1}|{Thickness1}|{Color1}";
            if (_cache.SpecIndex.TryGetValue(specKey, out double price))
            {
                _cachedSheet1 = price;
                Sheet1 = price;
                return;
            }

            string thKey = $"{Category1}|{Thickness1}";
            if (_cache.ThicknessIndex.TryGetValue(thKey, out price))
            {
                _cachedSheet1 = price;
                Sheet1 = price;
                return;
            }

            if (_cache.CategoryIndex.TryGetValue(Category1, out price))
            {
                _cachedSheet1 = price;
                Sheet1 = price;
                return;
            }

            _cachedSheet1 = 0;
            Sheet1 = 0;
        }

        private void LoadPrice2FromCache()
        {
            EnsureCache();

            if (string.IsNullOrEmpty(Category2) || string.IsNullOrEmpty(Thickness2) || string.IsNullOrEmpty(Color2))
            {
                _cachedSheet2 = 0;
                Sheet2 = 0;
                return;
            }

            string specKey = $"{Category2}|{Thickness2}|{Color2}";
            if (_cache.SpecIndex.TryGetValue(specKey, out double price))
            {
                _cachedSheet2 = price;
                Sheet2 = price;
                return;
            }

            string thKey = $"{Category2}|{Thickness2}";
            if (_cache.ThicknessIndex.TryGetValue(thKey, out price))
            {
                _cachedSheet2 = price;
                Sheet2 = price;
                return;
            }

            if (_cache.CategoryIndex.TryGetValue(Category2, out price))
            {
                _cachedSheet2 = price;
                Sheet2 = price;
                return;
            }

            _cachedSheet2 = 0;
            Sheet2 = 0;
        }

        // ================= CALCULATE =================
        private void CalcInternal()
        {
            double sheet1PerSqm = _cachedSheet1;
            double sheet2PerSqm = _cachedSheet2;

            // Result 1: Sheet 1 only
            Result1 = (sheet1PerSqm / _cachedProfitFactor);

            // Result 2: Sheet 2 only
            Result2 = (sheet2PerSqm / _cachedProfitFactor);

            // Result 3: Sheet 1 + PVB
            Result3 = ((sheet1PerSqm + _cachedPVBPrice) / _cachedProfitFactor);

            // Result 4: Sheet 1 + Sheet 2 + PVB
            Result4 = ((sheet1PerSqm + sheet2PerSqm + _cachedPVBPrice) / _cachedProfitFactor);

            // Final Result: Full calculation with all charges
            Result = (((sheet1PerSqm + sheet2PerSqm + _cachedPVBPrice + _cachedCutting + _cachedTempering) / _cachedProfitFactor));
        }

        // ================= ACTIONS =================
        private void Save()
        {
            Records.Insert(0, new LaminationRecord
            {
                DisplayText = $"S1: {Category1}|{Thickness1}|{Color1} | S2: {Category2}|{Thickness2}|{Color2} | PVB: {PVBType} | Result: {Result:F2} AED | {DateTime.Now:HH:mm}"
            });
            MessageBox.Show("Saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Clear()
        {
            _cachedCutting = 0;
            _cachedTempering = 0;
            _cachedProfitFactor = 0.85;
            _cachedPVBPrice = 110;
            _cutting = 0;
            _tempering = 0;
            _profit = "15%";
            _pvbType = "Standard Clear";
            _pvbPrice = 110;

            Notify("Cutting", "Tempering", "Profit", "ProfitFactor", "PVBType", "PVBPrice");

            if (CategoryOptions1.Count > 0) Category1 = CategoryOptions1[0];
            if (CategoryOptions2.Count > 0) Category2 = CategoryOptions2[0];

            CalcInternal();
        }

        public void ExportPdf()
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(CreatePrintVisual(), "Lamination Quotation");
                    MessageBox.Show("PDF Exported!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Visual CreatePrintVisual()
        {
            var grid = new Grid { Width = 600, Background = Brushes.White };
            var blue = new SolidColorBrush(Color.FromRgb(37, 99, 235));
            var green = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            var darkBlue = new SolidColorBrush(Color.FromRgb(30, 58, 95));

            var header = new Border { Background = blue, Padding = new Thickness(15) };
            var headerStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            headerStack.Children.Add(new TextBlock { Text = "PRO GLASS AUTOMATION", FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Brushes.White, TextAlignment = TextAlignment.Center });
            headerStack.Children.Add(new TextBlock { Text = $"Date: {DateTime.Now:dd MMM yyyy}", FontSize = 10, Foreground = Brushes.White, TextAlignment = TextAlignment.Center });
            header.Child = headerStack;
            grid.Children.Add(header);

            var content = new StackPanel { Margin = new Thickness(20) };
            content.Children.Add(new TextBlock { Text = "LAMINATION QUOTATION", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = blue, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 20, 0, 20) });

            content.Children.Add(CreateDetailRow("SHEET 1:", $"{Category1} | {Thickness1} | {Color1}"));
            content.Children.Add(CreateDetailRow("Sheet 1 Price:", $"{Sheet1:F2} AED"));
            content.Children.Add(new TextBlock { Text = "", Margin = new Thickness(0, 10, 0, 10) });
            content.Children.Add(CreateDetailRow("SHEET 2:", $"{Category2} | {Thickness2} | {Color2}"));
            content.Children.Add(CreateDetailRow("Sheet 2 Price:", $"{Sheet2:F2} AED"));
            content.Children.Add(new TextBlock { Text = "", Margin = new Thickness(0, 10, 0, 10) });
            content.Children.Add(CreateDetailRow("PVB Type:", PVBType));
            content.Children.Add(CreateDetailRow("PVB Price:", $"{PVBPrice:F2} AED"));
            content.Children.Add(CreateDetailRow("Cutting:", $"{_cachedCutting:F2} AED"));
            content.Children.Add(CreateDetailRow("Tempering:", $"{_cachedTempering:F2} AED"));
            content.Children.Add(CreateDetailRow("Profit:", Profit));

            var finalBox = new Border { Background = green, Padding = new Thickness(15), Margin = new Thickness(0, 20, 0, 20) };
            var finalStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            finalStack.Children.Add(new TextBlock { Text = "FINAL UNIT PRICE", FontSize = 10, Foreground = Brushes.White, TextAlignment = TextAlignment.Center });
            finalStack.Children.Add(new TextBlock { Text = $"{Result:F2} AED", FontSize = 24, FontWeight = FontWeights.Bold, Foreground = Brushes.White, TextAlignment = TextAlignment.Center });
            finalBox.Child = finalStack;
            content.Children.Add(finalBox);

            var footer = new Border { Background = darkBlue, Padding = new Thickness(10) };
            footer.Child = new TextBlock { Text = "PRO GLASS AUTOMATION | Dubai, UAE | jubersaleem01@gmail.com", FontSize = 9, Foreground = Brushes.White, TextAlignment = TextAlignment.Center };
            content.Children.Add(footer);

            grid.Children.Add(content);
            return grid;
        }

        private StackPanel CreateDetailRow(string label, string value)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
            row.Children.Add(new TextBlock { Text = label, Width = 120, Foreground = Brushes.Gray });
            row.Children.Add(new TextBlock { Text = value, FontWeight = FontWeights.Bold });
            return row;
        }
    }

    // ================= RECORD MODEL =================
    public class LaminationRecord
    {
        public string DisplayText { get; set; }
    }

    // ================= RELAY COMMAND =================
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null)
            : this(_ => execute(), canExecute != null ? _ => canExecute() : null) { }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object p) => _canExecute?.Invoke(p) ?? true;
        public void Execute(object p) => _execute(p);
    }
}