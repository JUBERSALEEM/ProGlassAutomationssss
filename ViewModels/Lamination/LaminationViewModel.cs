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

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
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

        // ================= [NEW] WASTAGE FACTOR =================
        // Wastage Factor = 1 - (Wastage% / 100)
        // Example: 15% wastage → 0.85, 20% wastage → 0.80
        private double _cachedWastageFactor = 0.85;

        // ================= LOOKUPS =================
        private static readonly Dictionary<string, double> PVBPrices = new()
        {
            { "Standard Clear", 110 }, { "Standard White", 115 }, { "Standard Black", 120 },
            { "Premium Clear", 125 }, { "Premium White", 130 }, { "Premium Black", 135 },
            { "Architectural Clear", 140 }, { "Architectural White", 145 }, { "Architectural Black", 150 }
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

        // [NEW] Wastage Options (5% to 50%)
        public ObservableCollection<string> WastageOptions { get; } = new()
        {
            "5", "10", "15", "20", "25", "30", "35", "40", "45", "50"
        };

        // [NEW] Profit Margin Options (1% to 100%)
        public ObservableCollection<string> ProfitMarginOptions { get; } = new()
        {
            "1", "5", "10", "15", "20", "25", "30", "35", "40", "45", "50", "60", "70", "80", "90", "100"
        };

        public ObservableCollection<LaminationRecord> Records { get; } = new();

        // ================= SHEET 1 PROPERTIES =================
        private string _cat1 = "";
        public string Category1
        {
            get => _cat1;
            set { if (Set(ref _cat1, value)) { LoadThicknessOptions1ByCategory(); LoadColorOptions1ByCategory(); LoadPrice1FromCache(); ScheduleCalc(); } }
        }

        private string _th1 = "";
        public string Thickness1
        {
            get => _th1;
            set { if (Set(ref _th1, value)) { LoadColorOptions1ByThickness(); LoadPrice1FromCache(); ScheduleCalc(); } }
        }

        private string _color1 = "";
        public string Color1
        {
            get => _color1;
            set { if (Set(ref _color1, value)) { LoadPrice1FromCache(); ScheduleCalc(); } }
        }

        private double _sheet1;
        public double Sheet1 { get => _sheet1; set => Set(ref _sheet1, value); }

        // ================= SHEET 2 PROPERTIES =================
        private string _cat2 = "";
        public string Category2
        {
            get => _cat2;
            set { if (Set(ref _cat2, value)) { LoadThicknessOptions2ByCategory(); LoadColorOptions2ByCategory(); LoadPrice2FromCache(); ScheduleCalc(); } }
        }

        private string _th2 = "";
        public string Thickness2
        {
            get => _th2;
            set { if (Set(ref _th2, value)) { LoadColorOptions2ByThickness(); LoadPrice2FromCache(); ScheduleCalc(); } }
        }

        private string _color2 = "";
        public string Color2
        {
            get => _color2;
            set { if (Set(ref _color2, value)) { LoadPrice2FromCache(); ScheduleCalc(); } }
        }

        private double _sheet2;
        public double Sheet2 { get => _sheet2; set => Set(ref _sheet2, value); }

        // ================= OTHER PROPERTIES =================
        private double _cutting;
        public double Cutting
        {
            get => _cutting;
            set { if (Set(ref _cutting, value)) { _cachedCutting = value; ScheduleCalc(); } }
        }

        private double _tempering;
        public double Tempering
        {
            get => _tempering;
            set { if (Set(ref _tempering, value)) { _cachedTempering = value; ScheduleCalc(); } }
        }

        private string _pvbType = "Standard Clear";
        public string PVBType
        {
            get => _pvbType;
            set
            {
                if (Set(ref _pvbType, value))
                {
                    if (PVBPrices.TryGetValue(value, out double pvbPrice))
                        _cachedPVBPrice = pvbPrice;
                    else
                        _cachedPVBPrice = 110;
                    PVBPrice = _cachedPVBPrice;
                    ScheduleCalc();
                }
            }
        }

        private double _pvbPrice = 110;
        public double PVBPrice { get => _pvbPrice; set => Set(ref _pvbPrice, value); }

        // ================= [NEW] WASTAGE CONSIDER =================
        // Wastage Factor = 1 - (Wastage% / 100)
        // This is SEPARATE from Profit Margin
        private string _wastageConsider = "15";
        public string WastageConsider
        {
            get => _wastageConsider;
            set
            {
                if (Set(ref _wastageConsider, value))
                {
                    UpdateWastageFactor();
                    CalcInternal();
                }
            }
        }

        public string WastageConsiderDisplay => $"{_wastageConsider}%";
        public double WastageFactor => _cachedWastageFactor;
        public string WastageFactorDisplay => _cachedWastageFactor.ToString("0.00");

        // ================= [NEW] PROFIT MARGIN =================
        // Profit Margin is ADDED at the end (multiplier)
        // This is SEPARATE from Wastage Factor
        private string _profitMargin = "15";
        public string ProfitMargin
        {
            get => _profitMargin;
            set
            {
                if (Set(ref _profitMargin, value))
                {
                    UpdateProfitMarginDisplay();
                    CalcInternal();
                }
            }
        }

        public string ProfitMarginDisplay => $"{_profitMargin}%";

        // ================= RESULT PROPERTIES =================
        private string _result1 = "0.00";
        public string Result1 { get => _result1; set => Set(ref _result1, value); }

        private string _result2 = "0.00";
        public string Result2 { get => _result2; set => Set(ref _result2, value); }

        private string _result3 = "0.00";
        public string Result3 { get => _result3; set => Set(ref _result3, value); }

        private string _result4 = "0.00";
        public string Result4 { get => _result4; set => Set(ref _result4, value); }

        private string _result = "0.00";
        public string Result { get => _result; set => Set(ref _result, value); }

        private bool _isHistoryVisible = true;
        public bool IsHistoryVisible { get => _isHistoryVisible; set => Set(ref _isHistoryVisible, value); }

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

            UpdateWastageFactor();
            UpdateProfitMarginDisplay();

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

        // ================= [NEW] UPDATE WASTAGE FACTOR =================
        private void UpdateWastageFactor()
        {
            double wastage = ParsePercentage(_wastageConsider);
            _cachedWastageFactor = 1 - (wastage / 100.0);

            OnPropertyChanged(nameof(WastageFactor));
            OnPropertyChanged(nameof(WastageFactorDisplay));
        }

        // ================= [NEW] UPDATE PROFIT MARGIN DISPLAY =================
        private void UpdateProfitMarginDisplay()
        {
            OnPropertyChanged(nameof(ProfitMarginDisplay));
        }

        private double ParsePercentage(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            string clean = value.Replace("%", "").Trim();
            if (double.TryParse(clean, out double result))
                return result;
            return 0;
        }

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

        // ================= SHEET 1 LOADERS =================
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

        // ================= SHEET 2 LOADERS =================
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

        // ================= LOAD PRICES =================
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

        // ================= [NEW] CALCULATE =================
        // Step 1: Sheet1 + Sheet2 = Result1
        // Step 2: Result1 / WastageFactor = Result2
        // Step 3: Result2 + PVB + Cutting + Tempering = Result3
        // Step 4: Result3 + ProfitMargin% = Result4 (Final)
        private void CalcInternal()
        {
            try
            {
                double sheet1PerSqm = _cachedSheet1;
                double sheet2PerSqm = _cachedSheet2;

                // Step 1: Sheet1 + Sheet2 = Result1
                double glassTotal = sheet1PerSqm + sheet2PerSqm;
                Result1 = glassTotal.ToString("0.00");

                // Step 2: Result1 / WastageFactor = Result2
                // Example: 100 / 0.85 = 117.65 (15% wastage)
                // Example: 100 / 0.80 = 125.00 (20% wastage)
                double step1 = glassTotal / _cachedWastageFactor;
                Result2 = step1.ToString("0.00");

                // Step 3: Result2 + PVB + Cutting + Tempering = Result3
                double charges = _cachedPVBPrice + _cachedCutting + _cachedTempering;
                double step2 = step1 + charges;
                Result3 = step2.ToString("0.00");

                // Step 4: Result3 + ProfitMargin% = Result4 (Final)
                double profitMultiplier = 1 + (ParsePercentage(_profitMargin) / 100.0);
                double final = step2 * profitMultiplier;
                Result4 = final.ToString("0.00");
                Result = final.ToString("0.00");
            }
            catch
            {
                Result1 = "0.00";
                Result2 = "0.00";
                Result3 = "0.00";
                Result4 = "0.00";
                Result = "0.00";
            }
        }

        // ================= SAVE =================
        private void Save()
        {
            try
            {
                DateTime now = DateTime.Now;
                string timestamp = now.ToString("yyyy-MM-dd HH:mm");
                double final = ParseDouble(Result);

                string spec = $"{Category1}|{Thickness1}|{Color1} Outer + " +
                             $"{Category2}|{Thickness2}|{Color2} Inner";

                string detail = $"PVB: {PVBType} | PVB: {_cachedPVBPrice:F2} AED | " +
                               $"Cut: {_cachedCutting:F2} AED | Temp: {_cachedTempering:F2} AED | " +
                               $"Wastage: {_wastageConsider}% (Factor: {_cachedWastageFactor:F2}) | " +
                               $"Margin: {_profitMargin}%";

                Records.Insert(0, new LaminationRecord
                {
                    DisplayText = $"{spec} - {final:F2} AED - {timestamp}",
                    DetailText = detail,
                    Timestamp = now,
                    Result = final
                });

                while (Records.Count > 50)
                    Records.RemoveAt(Records.Count - 1);

                MessageBox.Show("Saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { }
        }

        // ================= CLEAR =================
        private void Clear()
        {
            _cachedCutting = 0;
            _cachedTempering = 0;
            _cachedPVBPrice = 110;
            _cutting = 0;
            _tempering = 0;
            _pvbType = "Standard Clear";
            _pvbPrice = 110;
            _wastageConsider = "15";
            _profitMargin = "15";
            _cachedWastageFactor = 0.85;

            OnPropertyChanged("Cutting");
            OnPropertyChanged("Tempering");
            OnPropertyChanged("PVBType");
            OnPropertyChanged("PVBPrice");
            OnPropertyChanged("WastageConsider");
            OnPropertyChanged("WastageConsiderDisplay");
            OnPropertyChanged("WastageFactor");
            OnPropertyChanged("WastageFactorDisplay");
            OnPropertyChanged("ProfitMargin");
            OnPropertyChanged("ProfitMarginDisplay");

            if (CategoryOptions1.Count > 0) Category1 = CategoryOptions1[0];
            if (CategoryOptions2.Count > 0) Category2 = CategoryOptions2[0];

            CalcInternal();
        }

        private double ParseDouble(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            string clean = value.Replace(" ", "").Trim();
            if (double.TryParse(clean, out double result)) return result;
            return 0;
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

            // Calculation breakdown
            content.Children.Add(CreateSectionHeader("CALCULATION BREAKDOWN"));
            content.Children.Add(CreateDetailRow("Step 1 (Sheet1 + Sheet2):", $"{Result1} AED"));
            content.Children.Add(CreateDetailRow($"Step 2 (÷ Wastage Factor {_cachedWastageFactor:F2}):", $"{Result2} AED"));
            content.Children.Add(CreateDetailRow("Step 3 (+ Charges):", $"{Result3} AED"));
            content.Children.Add(CreateDetailRow($"Step 4 (+ Margin {_profitMargin}%):", $"{Result4} AED"));

            content.Children.Add(new TextBlock { Text = "", Margin = new Thickness(0, 10, 0, 10) });
            content.Children.Add(CreateDetailRow("SHEET 1:", $"{Category1} | {Thickness1} | {Color1}"));
            content.Children.Add(CreateDetailRow("Sheet 1 Price:", $"{Sheet1:F2} AED"));
            content.Children.Add(CreateDetailRow("SHEET 2:", $"{Category2} | {Thickness2} | {Color2}"));
            content.Children.Add(CreateDetailRow("Sheet 2 Price:", $"{Sheet2:F2} AED"));
            content.Children.Add(CreateDetailRow("PVB Type:", PVBType));
            content.Children.Add(CreateDetailRow("PVB Price:", $"{PVBPrice:F2} AED"));
            content.Children.Add(CreateDetailRow("Cutting:", $"{_cachedCutting:F2} AED"));
            content.Children.Add(CreateDetailRow("Tempering:", $"{_cachedTempering:F2} AED"));
            content.Children.Add(CreateDetailRow("Wastage Consider:", $"{_wastageConsider}% (Factor: {_cachedWastageFactor:F2})"));
            content.Children.Add(CreateDetailRow("Profit Margin:", $"{_profitMargin}%"));

            var finalBox = new Border { Background = green, Padding = new Thickness(15), Margin = new Thickness(0, 20, 0, 20) };
            var finalStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            finalStack.Children.Add(new TextBlock { Text = "FINAL UNIT PRICE", FontSize = 10, Foreground = Brushes.White, TextAlignment = TextAlignment.Center });
            finalStack.Children.Add(new TextBlock { Text = $"{Result} AED", FontSize = 24, FontWeight = FontWeights.Bold, Foreground = Brushes.White, TextAlignment = TextAlignment.Center });
            finalBox.Child = finalStack;
            content.Children.Add(finalBox);

            var footer = new Border { Background = darkBlue, Padding = new Thickness(10) };
            footer.Child = new TextBlock { Text = "PRO GLASS AUTOMATION | Dubai, UAE | jubersaleem01@gmail.com", FontSize = 9, Foreground = Brushes.White, TextAlignment = TextAlignment.Center };
            content.Children.Add(footer);

            grid.Children.Add(content);
            return grid;
        }

        private TextBlock CreateSectionHeader(string text)
        {
            var blue = new SolidColorBrush(Color.FromRgb(37, 99, 235));
            return new TextBlock { Text = text, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = blue, Margin = new Thickness(0, 15, 0, 10) };
        }

        private StackPanel CreateDetailRow(string label, string value)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
            row.Children.Add(new TextBlock { Text = label, Width = 180, Foreground = Brushes.Gray });
            row.Children.Add(new TextBlock { Text = value, FontWeight = FontWeights.Bold });
            return row;
        }
    }

    // ================= RECORD MODEL =================
    public class LaminationRecord
    {
        public string DisplayText { get; set; } = "";
        public string DetailText { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public double Result { get; set; }
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