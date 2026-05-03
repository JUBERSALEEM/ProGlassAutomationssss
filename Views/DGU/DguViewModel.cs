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

namespace ProGlassAutomation.Views.DGU
{
    public class DguViewModel : INotifyPropertyChanged
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

        private static readonly string[] Cats = { "HD Clear", "HD Bronze", "HD Grey", "Belgium Clear", "PNA Clear", "Ramly Clear", "Sunlux Silver", "Reflite Silver", "Stopsol Classic", "Guardian Clear", "AGC Clear", "Şişecam Clear", "SGG Clear", "Pilkington Clear", "Tinted Bronze", "Low-E Clear", "Lacobel White" };
        private static readonly string[] Ths = { "2mm", "2.5mm", "3mm", "4mm", "5mm", "6mm", "8mm", "10mm", "12mm", "15mm", "19mm" };
        private static readonly string[] Clrs = { "Clear", "Bronze", "Dark Bronze", "Grey", "Dark Grey", "Green", "Blue", "Reflective Silver", "Reflective Gold", "Reflective Blue", "Mirror", "Mirror Silver", "White", "Black" };
        private static readonly string[] SpSizes = { "6mm Air", "8mm Air", "10mm Air", "12mm Air", "14mm Air", "16mm Air", "18mm Air", "20mm Air", "22mm Air", "24mm Air", "6mm Argon", "8mm Argon", "10mm Argon", "12mm Argon", "14mm Argon", "16mm Argon", "18mm Argon", "20mm Argon", "22mm Argon", "24mm Argon", "6mm Krypton", "8mm Krypton", "10mm Krypton", "12mm Krypton", "14mm Krypton", "16mm Krypton" };
        private static readonly string[] SpClrs = { "Silver", "White", "Black", "Dark Brown", "Champagne", "Bronze" };
        private static readonly string[] Sizes2_4 = { "300x300", "400x400", "500x500", "400x600", "500x600" };
        private static readonly string[] Sizes5_6 = { "300x300", "400x400", "500x500", "600x600", "400x600", "500x600", "600x800", "800x800" };
        private static readonly string[] Sizes8_10 = { "400x600", "500x600", "600x800", "800x800", "800x1000", "1000x1000", "600x1000", "800x1200", "1000x1200" };
        private static readonly string[] Sizes12p = { "600x800", "800x1000", "1000x1000", "600x1000", "800x1200", "1000x1200", "1000x1500", "1200x1500", "1000x2000", "1200x2000" };
        private static readonly string[] SizesTinted = { "600x800", "800x1000", "1000x1000", "600x1000", "800x1200" };

        private static readonly Dictionary<string, double> SpacerPrices = new()
        {
            ["6mm Air"] = 8,
            ["8mm Air"] = 10,
            ["10mm Air"] = 12,
            ["12mm Air"] = 45,
            ["14mm Air"] = 18,
            ["16mm Air"] = 50,
            ["18mm Air"] = 22,
            ["20mm Air"] = 55,
            ["22mm Air"] = 28,
            ["24mm Air"] = 60,
            ["6mm Argon"] = 12,
            ["8mm Argon"] = 15,
            ["10mm Argon"] = 18,
            ["12mm Argon"] = 22,
            ["14mm Argon"] = 26,
            ["16mm Argon"] = 30,
            ["18mm Argon"] = 34,
            ["20mm Argon"] = 38,
            ["22mm Argon"] = 42,
            ["24mm Argon"] = 45,
            ["6mm Krypton"] = 18,
            ["8mm Krypton"] = 22,
            ["10mm Krypton"] = 26,
            ["12mm Krypton"] = 32,
            ["14mm Krypton"] = 38,
            ["16mm Krypton"] = 45
        };

        private static readonly Dictionary<string, double> SpacerColorPrices = new()
        {
            ["Silver"] = 0,
            ["White"] = 3,
            ["Black"] = 5,
            ["Dark Brown"] = 5,
            ["Champagne"] = 4,
            ["Bronze"] = 4
        };

        private static readonly Dictionary<string, double> ProfitFactors = new()
        {
            ["5%"] = 0.95,
            ["10%"] = 0.90,
            ["15%"] = 0.85,
            ["20%"] = 0.80,
            ["25%"] = 0.75,
            ["30%"] = 0.70,
            ["35%"] = 0.65,
            ["40%"] = 0.60
        };

        private static readonly Dictionary<string, double> ProfitMargins = new()
        {
            ["5%"] = 0.05,
            ["10%"] = 0.10,
            ["15%"] = 0.15,
            ["20%"] = 0.20,
            ["25%"] = 0.25,
            ["30%"] = 0.30,
            ["35%"] = 0.35,
            ["40%"] = 0.40
        };

        private readonly struct SheetKey
        {
            public readonly string Category, Thickness, Color;
            private readonly int _hash;
            public SheetKey(string c, string t, string col) { Category = c; Thickness = t; Color = col; _hash = HashCode.Combine(c, t, col); }
            public override int GetHashCode() => _hash;
            public bool Equals(SheetKey o) => Category == o.Category && Thickness == o.Thickness && Color == o.Color;
            public override bool Equals(object o) => o is SheetKey k && Equals(k);
            public static bool operator ==(SheetKey a, SheetKey b) => a.Equals(b);
            public static bool operator !=(SheetKey a, SheetKey b) => !a.Equals(b);
        }

        private static volatile CacheSnapshot _cache;

        private sealed class CacheSnapshot
        {
            public List<Sheet> Sheets { get; init; } = new();
            public Dictionary<SheetKey, Sheet> ExactLookup { get; init; } = new();
            public Dictionary<string, Sheet> CtLookup { get; init; } = new();
            public Dictionary<string, List<string>> ThicknessLookup { get; init; } = new();
            public Dictionary<string, List<string>> ColorLookup { get; init; } = new();
            public DateTime Timestamp { get; init; }
            public static CacheSnapshot Empty => new() { Timestamp = DateTime.MinValue };
        }

        private static CacheSnapshot Cache => _cache;

        private static void SetCache()
        {
            var sheets = SheetStoreService.Instance.GetAllActive().ToList();
            if (sheets.Count == 0) return;

            var exactLookup = new Dictionary<SheetKey, Sheet>(sheets.Count * 2);
            var ctLookup = new Dictionary<string, Sheet>(sheets.Count * 2);
            var thicknessLookup = new Dictionary<string, List<string>>(Cats.Length);
            var colorLookup = new Dictionary<string, List<string>>(sheets.Count * 2);

            foreach (var s in sheets)
            {
                var key = new SheetKey(s.Category, s.Thickness, s.Color);
                if (!exactLookup.ContainsKey(key)) exactLookup[key] = s;

                string ctKey = $"{s.Category}|{s.Thickness}";
                if (!ctLookup.ContainsKey(ctKey)) ctLookup[ctKey] = s;

                if (!thicknessLookup.TryGetValue(s.Category, out var tl))
                    thicknessLookup[s.Category] = tl = new List<string>();
                if (!tl.Contains(s.Thickness)) tl.Add(s.Thickness);

                if (!colorLookup.TryGetValue(ctKey, out var cl))
                    colorLookup[ctKey] = cl = new List<string>();
                if (!cl.Contains(s.Color)) cl.Add(s.Color);
            }

            foreach (var l in thicknessLookup.Values) l.Sort(CompareTh);
            foreach (var l in colorLookup.Values) l.Sort(StringComparer.OrdinalIgnoreCase);

            _cache = new CacheSnapshot
            {
                Sheets = sheets,
                ExactLookup = exactLookup,
                CtLookup = ctLookup,
                ThicknessLookup = thicknessLookup,
                ColorLookup = colorLookup,
                Timestamp = DateTime.Now
            };
        }

        private static int CompareTh(string a, string b)
        {
            int av = int.TryParse(a?.Replace("mm", ""), out var ai) ? ai : 999;
            int bv = int.TryParse(b?.Replace("mm", ""), out var bi) ? bi : 999;
            return av.CompareTo(bv);
        }

        private static void EnsureCache()
        {
            if ((DateTime.Now - Cache.Timestamp).TotalMinutes < 5) return;
            SetCache();
        }

        private static double FastLookupPrice(string cat, string th, string clr)
        {
            EnsureCache();
            var key = new SheetKey(cat, th, clr);
            if (Cache.ExactLookup.TryGetValue(key, out var exact))
                return (double)exact.PurchasePrice;

            string ctKey = $"{cat}|{th}";
            if (Cache.CtLookup.TryGetValue(ctKey, out var ctMatch))
                return (double)ctMatch.PurchasePrice;

            return 0;
        }

        // FIX: Lambda with explicit parameters
        static DguViewModel()
        {
            SheetStoreService.Instance.DataChanged += (object sender, EventArgs e) => InvalidateCache();
        }

        public static void InvalidateCache()
        {
            _cache = CacheSnapshot.Empty;
        }

        private CancellationTokenSource _debounceCts;
        private readonly object _updateLock = new();
        private bool _isUpdating;

        private void SafeUpdate(Action a)
        {
            lock (_updateLock)
            {
                if (_isUpdating) return;
                _isUpdating = true;
            }
            try { a(); }
            finally { _isUpdating = false; }
        }

        private async void ScheduleCalc()
        {
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            try { await Task.Delay(150, _debounceCts.Token); CalcInternal(); }
            catch (TaskCanceledException) { }
        }

        private double _cachedW, _cachedH, _cachedQ;
        private string _lastW = "", _lastH = "", _lastQ = "";

        private void UpdateCachedInputs()
        {
            if (_lastW == Width && _lastH == Height && _lastQ == Qty) return;
            _lastW = Width; _lastH = Height; _lastQ = Qty;
            double.TryParse(Width, out _cachedW);
            double.TryParse(Height, out _cachedH);
            int.TryParse(Qty, out var q);
            _cachedQ = q;
        }

        public ObservableCollection<string> CategoryOptions { get; } = new();
        public ObservableCollection<string> ThicknessOptions { get; } = new();
        public ObservableCollection<string> ThicknessOptions1 { get; } = new();
        public ObservableCollection<string> ThicknessOptions2 { get; } = new();
        public ObservableCollection<string> ColorOptions1 { get; } = new();
        public ObservableCollection<string> ColorOptions2 { get; } = new();
        public ObservableCollection<string> SpacerSizeOptions { get; } = new();
        public ObservableCollection<string> SpacerColorOptions { get; } = new();
        public ObservableCollection<string> AvailableSizes { get; } = new();
        public ObservableCollection<DguRecord> Records { get; } = new();

        private string _cat1, _th1 = "4mm", _clr1 = "Clear", _cat2, _th2 = "4mm", _clr2 = "Clear", _spSize = "12mm Air", _spClr = "Silver", _profit = "15%", _w = "1000", _h = "1000", _qty = "1";
        private double _sh1, _sh2;
        private bool _sizeWarning, _isHistoryVisible;
        private string _sizeWarningMessage;
        private bool _manual1, _manual2;
        private string _result = "0.00", _totalSqm = "0.00", _totalPrice = "0.00", _vatAmount = "0.00", _grossTotal = "0.00";
        private readonly Dictionary<string, object> _fields = new();

        public string Category1 { get => _cat1; set { if (_cat1 == value) return; _cat1 = value; Notify(nameof(Category1)); SafeUpdate(() => { LoadTh1(); LoadClr1(); LoadPrice1(); LoadSizes(); ScheduleCalc(); }); } }
        public string Thickness1 { get => _th1; set { if (_th1 == value) return; _th1 = value; Notify(nameof(Thickness1)); SafeUpdate(() => { LoadClr1(); LoadPrice1(); LoadSizes(); ScheduleCalc(); }); } }
        public string Color1 { get => _clr1; set { if (_clr1 == value) return; _clr1 = value; Notify(nameof(Color1)); SafeUpdate(() => { LoadPrice1(); LoadSizes(); ScheduleCalc(); }); } }
        public double Sheet1 { get => _sh1; set { if (Math.Abs(_sh1 - value) < 0.001) return; _sh1 = value; _manual1 = true; Notify(nameof(Sheet1)); ScheduleCalc(); } }
        public string Category2 { get => _cat2; set { if (_cat2 == value) return; _cat2 = value; Notify(nameof(Category2)); SafeUpdate(() => { LoadTh2(); LoadClr2(); LoadPrice2(); LoadSizes(); ScheduleCalc(); }); } }
        public string Thickness2 { get => _th2; set { if (_th2 == value) return; _th2 = value; Notify(nameof(Thickness2)); SafeUpdate(() => { LoadClr2(); LoadPrice2(); LoadSizes(); ScheduleCalc(); }); } }
        public string Color2 { get => _clr2; set { if (_clr2 == value) return; _clr2 = value; Notify(nameof(Color2)); SafeUpdate(() => { LoadPrice2(); LoadSizes(); ScheduleCalc(); }); } }
        public double Sheet2 { get => _sh2; set { if (Math.Abs(_sh2 - value) < 0.001) return; _sh2 = value; _manual2 = true; Notify(nameof(Sheet2)); ScheduleCalc(); } }
        public string SpacerSize { get => _spSize; set { if (_spSize == value) return; _spSize = value; Notify(nameof(SpacerSize)); ScheduleCalc(); } }
        public string SpacerColor { get => _spClr; set { if (_spClr == value) return; _spClr = value; Notify(nameof(SpacerColor)); ScheduleCalc(); } }
        public string Profit { get => _profit; set { if (_profit == value) return; _profit = value; Notify(nameof(Profit)); ScheduleCalc(); } }
        public string Width { get => _w; set { if (_w == value) return; _w = value; Notify(nameof(Width)); ScheduleCalc(); } }
        public string Height { get => _h; set { if (_h == value) return; _h = value; Notify(nameof(Height)); ScheduleCalc(); } }
        public string Qty { get => _qty; set { if (_qty == value) return; _qty = value; Notify(nameof(Qty)); ScheduleCalc(); } }

        public string Result { get => _result; set => Set(ref _result, value, nameof(Result)); }
        public string TotalSqm { get => _totalSqm; set => Set(ref _totalSqm, value, nameof(TotalSqm)); }
        public string TotalPrice { get => _totalPrice; set => Set(ref _totalPrice, value, nameof(TotalPrice)); }
        public string VatAmount { get => _vatAmount; set => Set(ref _vatAmount, value, nameof(VatAmount)); }
        public string GrossTotal { get => _grossTotal; set => Set(ref _grossTotal, value, nameof(GrossTotal)); }
        public string SpecificationSummary => $"{Category1} {Thickness1} {Color1} + {SpacerSize} {SpacerColor} + {Category2} {Thickness2} {Color2}";
        public string SelectedSize
        {
            get => Get<string>();
            set
            {
                if (Set(value) && value?.Contains("x") == true)
                {
                    var p = value.Split('x');
                    Width = p[0];
                    Height = p[1];
                }
            }
        }
        public bool SizeWarning { get => _sizeWarning; set => Set(ref _sizeWarning, value, nameof(SizeWarning)); }
        public string SizeWarningMessage { get => _sizeWarningMessage; set => Set(ref _sizeWarningMessage, value, nameof(SizeWarningMessage)); }
        public bool IsHistoryVisible { get => _isHistoryVisible; set => Set(ref _isHistoryVisible, value, nameof(IsHistoryVisible)); }

        public ICommand SaveCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearCommand { get; }

        public DguViewModel()
        {
            foreach (var c in Cats) CategoryOptions.Add(c);
            foreach (var t in Ths) ThicknessOptions.Add(t);
            foreach (var t in Ths) ThicknessOptions1.Add(t);
            foreach (var s in SpSizes) SpacerSizeOptions.Add(s);
            foreach (var c in SpClrs) SpacerColorOptions.Add(c);

            SetCache();

            _cat1 = Cats[0];
            _cat2 = Cats.Length > 1 ? Cats[1] : Cats[0];

            SaveCommand = new RelayCommand(o => Save());
            ExportPdfCommand = new RelayCommand(o => ExportPdf());
            DeleteCommand = new RelayCommand(o => { if (o is DguRecord r) Records.Remove(r); });
            ClearCommand = new RelayCommand(o => Clear());

            LoadSizes();
            CalcInternal();
        }

        private T Get<T>([CallerMemberName] string name = null) => _fields.TryGetValue(name, out var v) ? (T)v : default;
        private bool Set<T>(T value, [CallerMemberName] string name = null)
        {
            if (Equals(_fields.TryGetValue(name, out var e) ? e : null, value)) return false;
            _fields[name] = value;
            Notify(name);
            return true;
        }

        private static int ThVal(string t) => int.TryParse(t?.Replace("mm", ""), out var v) ? v : 4;
        private static bool IsTinted(string c) => c?.ToLower() is var s && (s.Contains("bronze") || s.Contains("grey") || s.Contains("reflective") || s.Contains("green") || s.Contains("blue"));

        private void LoadTh1()
        {
            ThicknessOptions1.Clear();
            _manual1 = false;
            EnsureCache();

            if (Cache.ThicknessLookup.TryGetValue(Category1, out var ts) && ts.Count > 0)
                foreach (var t in ts) ThicknessOptions1.Add(t);
            else
                foreach (var t in Ths) ThicknessOptions1.Add(t);

            if (!ThicknessOptions1.Contains(Thickness1))
                Thickness1 = ThicknessOptions1.FirstOrDefault() ?? "4mm";
        }

        private void LoadTh2()
        {
            ThicknessOptions2.Clear();
            _manual2 = false;
            EnsureCache();

            if (Cache.ThicknessLookup.TryGetValue(Category2, out var ts) && ts.Count > 0)
                foreach (var t in ts) ThicknessOptions2.Add(t);
            else
                foreach (var t in Ths) ThicknessOptions2.Add(t);

            if (!ThicknessOptions2.Contains(Thickness2))
                Thickness2 = ThicknessOptions2.FirstOrDefault() ?? "4mm";
        }

        private void LoadClr1()
        {
            ColorOptions1.Clear();
            EnsureCache();

            string key = $"{Category1}|{Thickness1}";
            if (Cache.ColorLookup.TryGetValue(key, out var cs) && cs.Count > 0)
                foreach (var c in cs) ColorOptions1.Add(c);
            else
                foreach (var c in GetFallbackColors(Category1)) ColorOptions1.Add(c);

            if (!ColorOptions1.Contains(Color1))
                Color1 = ColorOptions1.FirstOrDefault() ?? "Clear";
        }

        private void LoadClr2()
        {
            ColorOptions2.Clear();
            EnsureCache();

            string key = $"{Category2}|{Thickness2}";
            if (Cache.ColorLookup.TryGetValue(key, out var cs) && cs.Count > 0)
                foreach (var c in cs) ColorOptions2.Add(c);
            else
                foreach (var c in GetFallbackColors(Category2)) ColorOptions2.Add(c);

            if (!ColorOptions2.Contains(Color2))
                Color2 = ColorOptions2.FirstOrDefault() ?? "Clear";
        }

        private static string[] GetFallbackColors(string cat) => cat switch
        {
            "HD Clear" or "Belgium Clear" or "PNA Clear" or "Ramly Clear" or "Guardian Clear" or "AGC Clear" or "Şişecam Clear" or "SGG Clear" or "Pilkington Clear" or "Low-E Clear" => new[] { "Clear" },
            "HD Bronze" or "Tinted Bronze" => new[] { "Bronze", "Dark Bronze" },
            "HD Grey" => new[] { "Grey", "Dark Grey" },
            "Sunlux Silver" or "Reflite Silver" => new[] { "Reflective Silver", "Mirror Silver" },
            "Stopsol Classic" => new[] { "Reflective Silver", "Reflective Gold", "Reflective Blue" },
            "Lacobel White" => new[] { "White" },
            _ => Clrs
        };

        private void LoadPrice1()
        {
            if (_manual1 || string.IsNullOrEmpty(Category1)) return;
            SafeUpdate(() => Sheet1 = FastLookupPrice(Category1, Thickness1, Color1));
        }

        private void LoadPrice2()
        {
            if (_manual2 || string.IsNullOrEmpty(Category2)) return;
            SafeUpdate(() => Sheet2 = FastLookupPrice(Category2, Thickness2, Color2));
        }

        private void LoadSizes()
        {
            AvailableSizes.Clear();
            int minTh = Math.Min(ThVal(Thickness1), ThVal(Thickness2));
            bool tinted = IsTinted(Color1) || IsTinted(Color2);
            string[] sizes;

            if (tinted) { sizes = SizesTinted; SizeWarning = true; SizeWarningMessage = "Tinted/Reflective glass has limited sizes"; }
            else if (minTh <= 4) { sizes = Sizes2_4; SizeWarning = true; SizeWarningMessage = "Thin glass (2-4mm) limited to smaller sizes"; }
            else if (minTh <= 6) { sizes = Sizes5_6; SizeWarning = false; }
            else if (minTh <= 10) { sizes = Sizes8_10; SizeWarning = false; }
            else { sizes = Sizes12p; SizeWarning = false; }

            foreach (var s in sizes) AvailableSizes.Add(s);

            string cur = $"{Width}x{Height}";
            if (!AvailableSizes.Contains(cur) && AvailableSizes.Any())
            {
                var p = AvailableSizes.First().Split('x');
                Width = p[0];
                Height = p[1];
            }
        }

        private void CalcInternal()
        {
            if (_isUpdating) return;
            UpdateCachedInputs();

            double glassTotal = Sheet1 + Sheet2;
            double profitFactor = ProfitFactors.GetValueOrDefault(Profit, 0.85);
            double step2 = glassTotal / profitFactor;
            double step3 = step2 + SpacerPrices.GetValueOrDefault(SpacerSize, 15) + SpacerColorPrices.GetValueOrDefault(SpacerColor, 0);
            double profitMargin = ProfitMargins.GetValueOrDefault(Profit, 0.15);
            double final = step3 * (1 + profitMargin);

            double sqm = (_cachedW / 1000) * (_cachedH / 1000) * _cachedQ;
            double total = final * sqm;
            double vat = total * 0.05;

            _result = final.ToString("0.00");
            _totalSqm = sqm.ToString("0.00");
            _totalPrice = total.ToString("0.00");
            _vatAmount = vat.ToString("0.00");
            _grossTotal = (total + vat).ToString("0.00");

            Notify(nameof(Result), nameof(TotalSqm), nameof(TotalPrice), nameof(VatAmount), nameof(GrossTotal));
        }

        public void Calc() => ScheduleCalc();

        public void Clear()
        {
            _manual1 = false;
            _manual2 = false;
            _cat1 = Cats[0];
            _cat2 = Cats.Length > 1 ? Cats[1] : Cats[0];
            _th1 = "4mm";
            _th2 = "4mm";
            _clr1 = "Clear";
            _clr2 = "Clear";
            _spSize = "12mm Air";
            _spClr = "Silver";
            _profit = "15%";
            _sh1 = 0;
            _sh2 = 0;
            _w = "1000";
            _h = "1000";
            _qty = "1";

            Notify(nameof(Category1), nameof(Category2), nameof(Thickness1), nameof(Thickness2),
                   nameof(Color1), nameof(Color2), nameof(SpacerSize), nameof(SpacerColor),
                   nameof(Profit), nameof(Sheet1), nameof(Sheet2), nameof(Width), nameof(Height), nameof(Qty));

            LoadSizes();
            ScheduleCalc();
        }

        public void Save()
        {
            CalcInternal();
            Records.Insert(0, new DguRecord
            {
                Thickness1 = Thickness1,
                Color1 = Color1,
                Thickness2 = Thickness2,
                Color2 = Color2,
                Spacer = $"{SpacerSize} {SpacerColor}",
                Result = double.TryParse(Result, out var r) ? r : 0,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            });
            MessageBox.Show("Saved Successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ExportPdf()
        {
            try
            {
                var pd = new PrintDialog();
                if (pd.ShowDialog() == true)
                    MessageBox.Show("PDF Exported via Print!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _exec;
        private readonly Func<object, bool> _canExec;

        public RelayCommand(Action<object> exec, Func<object, bool> canExec = null)
        {
            _exec = exec ?? throw new ArgumentNullException(nameof(exec));
            _canExec = canExec;
        }

        public RelayCommand(Action exec, Func<bool> canExec = null)
            : this(_ => exec(), canExec != null ? _ => canExec() : null) { }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object p) => _canExec?.Invoke(p) ?? true;
        public void Execute(object p) => _exec(p);
    }
}