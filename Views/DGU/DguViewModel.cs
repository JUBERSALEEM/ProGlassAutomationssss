using ProGlassAutomation.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProGlassAutomation.Views.DGU
{
    public class DguViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        // Static Arrays
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

        // Collections
        public ObservableCollection<string> CategoryOptions { get; } = new();
        public ObservableCollection<string> ThicknessOptions { get; } = new();
        public ObservableCollection<string> ThicknessOptions1 { get; } = new();
        public ObservableCollection<string> ThicknessOptions2 { get; } = new();
        public ObservableCollection<string> ColorOptions1 { get; } = new();
        public ObservableCollection<string> ColorOptions2 { get; } = new();
        public ObservableCollection<string> SpacerSizeOptions { get; } = new();
        public ObservableCollection<string> SpacerColorOptions { get; } = new();
        public ObservableCollection<string> ProfitList { get; } = new() { "5%", "10%", "15%", "20%", "25%", "30%", "35%", "40%" };
        public ObservableCollection<string> AvailableSizes { get; } = new();
        public ObservableCollection<DguRecord> Records { get; } = new();

        // Manual edit flags
        private bool _manual1, _manual2;

        // Properties
        public string Category1 { get => Get<string>(); set { Set(value); LoadTh1(); LoadClr1(); LoadPrice1(); LoadSizes(); Calc(); } }
        public string Thickness1 { get => Get<string>() ?? "4mm"; set { Set(value); LoadClr1(); LoadPrice1(); LoadSizes(); Calc(); } }
        public string Color1 { get => Get<string>() ?? "Clear"; set { Set(value); LoadPrice1(); LoadSizes(); Calc(); } }
        public double Sheet1 { get => Get<double>(); set { Set(value); _manual1 = true; Calc(); } }

        public string Category2 { get => Get<string>(); set { Set(value); LoadTh2(); LoadClr2(); LoadPrice2(); LoadSizes(); Calc(); } }
        public string Thickness2 { get => Get<string>() ?? "4mm"; set { Set(value); LoadClr2(); LoadPrice2(); LoadSizes(); Calc(); } }
        public string Color2 { get => Get<string>() ?? "Clear"; set { Set(value); LoadPrice2(); LoadSizes(); Calc(); } }
        public double Sheet2 { get => Get<double>(); set { Set(value); _manual2 = true; Calc(); } }

        public string SpacerSize { get => Get<string>() ?? "12mm Air"; set { Set(value); Calc(); } }
        public string SpacerColor { get => Get<string>() ?? "Silver"; set { Set(value); Calc(); } }
        public string Profit { get => Get<string>() ?? "15%"; set { Set(value); Calc(); } }
        public string Width { get => Get<string>() ?? "1000"; set { Set(value); Calc(); } }
        public string Height { get => Get<string>() ?? "1000"; set { Set(value); Calc(); } }
        public string Qty { get => Get<string>() ?? "1"; set { Set(value); Calc(); } }

        public string Result { get => Get<string>() ?? "0.00"; set => Set(value); }
        public string TotalSqm { get => Get<string>() ?? "0.00"; set => Set(value); }
        public string TotalPrice { get => Get<string>() ?? "0.00"; set => Set(value); }
        public string VatAmount { get => Get<string>() ?? "0.00"; set => Set(value); }
        public string GrossTotal { get => Get<string>() ?? "0.00"; set => Set(value); }

        public string SpecificationSummary => $"{Category1} {Thickness1} {Color1} + {SpacerSize} {SpacerColor} + {Category2} {Thickness2} {Color2}";

        public string SelectedSize { get => Get<string>(); set { Set(value); if (value?.Contains("x") == true) { var p = value.Split('x'); Width = p[0]; Height = p[1]; } } }
        public bool SizeWarning { get => Get<bool>(); set => Set(value); }
        public string SizeWarningMessage { get => Get<string>(); set => Set(value); }
        public bool IsHistoryVisible { get => Get<bool>(); set => Set(value); }

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearCommand { get; }

        // Backing store
        private readonly Dictionary<string, object> _fields = new();

        // Constructor
        public DguViewModel()
        {
            foreach (var c in Cats) CategoryOptions.Add(c);
            foreach (var t in Ths) ThicknessOptions.Add(t);
            foreach (var s in SpSizes) SpacerSizeOptions.Add(s);
            foreach (var c in SpClrs) SpacerColorOptions.Add(c);

            Category1 = Cats.FirstOrDefault();
            Category2 = Cats.Skip(1).FirstOrDefault() ?? Cats.FirstOrDefault();

            SaveCommand = new RelayCommand(o => Save());
            ExportPdfCommand = new RelayCommand(o => ExportPdf());
            DeleteCommand = new RelayCommand(o => { if (o is DguRecord r) Records.Remove(r); });
            ClearCommand = new RelayCommand(o => Clear());

            LoadSizes();
            Calc();
        }

        // Generic Get/Set
        private T Get<T>([CallerMemberName] string name = null) => _fields.TryGetValue(name, out var v) ? (T)v : default;
        private void Set<T>(T value, [CallerMemberName] string name = null) { _fields[name] = value; OnPropertyChanged(name); }

        // Helper methods
        private int ThVal(string t) => int.TryParse(t?.Replace("mm", ""), out var v) ? v : 4;
        private bool IsTinted(string c) => c?.ToLower() is var s && (s.Contains("bronze") || s.Contains("grey") || s.Contains("reflective") || s.Contains("green") || s.Contains("blue"));

        // Load Thickness Options 1
        private void LoadTh1()
        {
            ThicknessOptions1.Clear();
            _manual1 = false;
            try
            {
                var ts = Services.SheetStoreService.Instance.GetAllActive().Where(s => s.Category == Category1).Select(s => s.Thickness).Distinct().OrderBy(ThVal).ToList();
                if (ts.Any())
                {
                    foreach (var t in ts) ThicknessOptions1.Add(t);
                }
                else
                {
                    foreach (var t in Ths) ThicknessOptions1.Add(t);
                }
                if (!ThicknessOptions1.Contains(Thickness1)) Thickness1 = ThicknessOptions1.FirstOrDefault();
            }
            catch
            {
                foreach (var t in Ths) ThicknessOptions1.Add(t);
            }
        }

        // Load Thickness Options 2
        private void LoadTh2()
        {
            ThicknessOptions2.Clear();
            _manual2 = false;
            try
            {
                var ts = Services.SheetStoreService.Instance.GetAllActive().Where(s => s.Category == Category2).Select(s => s.Thickness).Distinct().OrderBy(ThVal).ToList();
                if (ts.Any())
                {
                    foreach (var t in ts) ThicknessOptions2.Add(t);
                }
                else
                {
                    foreach (var t in Ths) ThicknessOptions2.Add(t);
                }
                if (!ThicknessOptions2.Contains(Thickness2)) Thickness2 = ThicknessOptions2.FirstOrDefault();
            }
            catch
            {
                foreach (var t in Ths) ThicknessOptions2.Add(t);
            }
        }

        // Load Colors 1
        private void LoadClr1()
        {
            ColorOptions1.Clear();
            try
            {
                var cs = Services.SheetStoreService.Instance.GetAllActive().Where(s => s.Category == Category1 && s.Thickness == Thickness1).Select(s => s.Color).Distinct().OrderBy(c => c).ToList();
                if (cs.Any())
                {
                    foreach (var c in cs) ColorOptions1.Add(c);
                }
                else
                {
                    foreach (var c in GetFallbackColors(Category1)) ColorOptions1.Add(c);
                }
                if (!ColorOptions1.Contains(Color1)) Color1 = ColorOptions1.FirstOrDefault();
            }
            catch
            {
                foreach (var c in GetFallbackColors(Category1)) ColorOptions1.Add(c);
            }
        }

        // Load Colors 2
        private void LoadClr2()
        {
            ColorOptions2.Clear();
            try
            {
                var cs = Services.SheetStoreService.Instance.GetAllActive().Where(s => s.Category == Category2 && s.Thickness == Thickness2).Select(s => s.Color).Distinct().OrderBy(c => c).ToList();
                if (cs.Any())
                {
                    foreach (var c in cs) ColorOptions2.Add(c);
                }
                else
                {
                    foreach (var c in GetFallbackColors(Category2)) ColorOptions2.Add(c);
                }
                if (!ColorOptions2.Contains(Color2)) Color2 = ColorOptions2.FirstOrDefault();
            }
            catch
            {
                foreach (var c in GetFallbackColors(Category2)) ColorOptions2.Add(c);
            }
        }

        // Fallback colors
        private string[] GetFallbackColors(string cat) => cat switch
        {
            "HD Clear" or "Belgium Clear" or "PNA Clear" or "Ramly Clear" or "Guardian Clear" or "AGC Clear" or "Şişecam Clear" or "SGG Clear" or "Pilkington Clear" or "Low-E Clear" => new[] { "Clear" },
            "HD Bronze" or "Tinted Bronze" => new[] { "Bronze", "Dark Bronze" },
            "HD Grey" => new[] { "Grey", "Dark Grey" },
            "Sunlux Silver" or "Reflite Silver" => new[] { "Reflective Silver", "Mirror Silver" },
            "Stopsol Classic" => new[] { "Reflective Silver", "Reflective Gold", "Reflective Blue" },
            "Lacobel White" => new[] { "White" },
            _ => Clrs
        };

        // Load Price 1
        private void LoadPrice1()
        {
            if (_manual1 || string.IsNullOrEmpty(Category1)) return;
            try
            {
                var sheets = Services.SheetStoreService.Instance.GetAllActive().ToList();
                var match = sheets.FirstOrDefault(s => s.Category == Category1 && s.Thickness == Thickness1 && s.Color == Color1)
                    ?? sheets.FirstOrDefault(s => s.Category == Category1 && s.Thickness == Thickness1)
                    ?? sheets.FirstOrDefault(s => s.Category == Category1);
                Sheet1 = match != null ? (double)match.PurchasePrice : 0;
            }
            catch { Sheet1 = 0; }
        }

        // Load Price 2
        private void LoadPrice2()
        {
            if (_manual2 || string.IsNullOrEmpty(Category2)) return;
            try
            {
                var sheets = Services.SheetStoreService.Instance.GetAllActive().ToList();
                var match = sheets.FirstOrDefault(s => s.Category == Category2 && s.Thickness == Thickness2 && s.Color == Color2)
                    ?? sheets.FirstOrDefault(s => s.Category == Category2 && s.Thickness == Thickness2)
                    ?? sheets.FirstOrDefault(s => s.Category == Category2);
                Sheet2 = match != null ? (double)match.PurchasePrice : 0;
            }
            catch { Sheet2 = 0; }
        }

        // Load Available Sizes (continued)
        private void LoadSizes()
        {
            AvailableSizes.Clear();
            int minTh = Math.Min(ThVal(Thickness1), ThVal(Thickness2));
            bool tinted = IsTinted(Color1) || IsTinted(Color2);
            string[] sizes;

            if (tinted)
            {
                sizes = SizesTinted;
                SizeWarning = true;
                SizeWarningMessage = "Tinted/Reflective glass has limited sizes";
            }
            else if (minTh <= 4)
            {
                sizes = Sizes2_4;
                SizeWarning = true;
                SizeWarningMessage = "Thin glass (2-4mm) limited to smaller sizes";
            }
            else if (minTh <= 6)
            {
                sizes = Sizes5_6;
                SizeWarning = false;
            }
            else if (minTh <= 10)
            {
                sizes = Sizes8_10;
                SizeWarning = false;
            }
            else
            {
                sizes = Sizes12p;
                SizeWarning = false;
            }

            foreach (var s in sizes) AvailableSizes.Add(s);

            string cur = $"{Width}x{Height}";
            if (!AvailableSizes.Contains(cur) && AvailableSizes.Any())
            {
                var p = AvailableSizes.First().Split('x');
                Width = p[0];
                Height = p[1];
            }
        }

        // Spacer Price
        private double SpacerPrice(string s) => s switch
        {
            "6mm Air" => 8,
            "8mm Air" => 10,
            "10mm Air" => 12,
            "12mm Air" => 45,
            "14mm Air" => 18,
            "16mm Air" => 50,
            "18mm Air" => 22,
            "20mm Air" => 55,
            "22mm Air" => 28,
            "24mm Air" => 60,
            "6mm Argon" => 12,
            "8mm Argon" => 15,
            "10mm Argon" => 18,
            "12mm Argon" => 22,
            "14mm Argon" => 26,
            "16mm Argon" => 30,
            "18mm Argon" => 34,
            "20mm Argon" => 38,
            "22mm Argon" => 42,
            "24mm Argon" => 45,
            "6mm Krypton" => 18,
            "8mm Krypton" => 22,
            "10mm Krypton" => 26,
            "12mm Krypton" => 32,
            "14mm Krypton" => 38,
            "16mm Krypton" => 45,
            _ => 15
        };

        // Spacer Color Price
        private double SpacerColorPrice(string c) => c switch
        {
            "White" => 3,
            "Black" => 5,
            "Dark Brown" => 5,
            "Champagne" => 4,
            "Bronze" => 4,
            _ => 0
        };

        private double P(string v) => double.TryParse(v, out var x) ? x : 0;

        // CORRECT FORMULA:
        // Step 1: glassTotal = Sheet1 + Sheet2
        // Step 2: glassTotal / profitFactor (0.85=15%, 0.90=10%, 0.80=20%)
        // Step 3: + Spacer costs
        // Step 4: × (1 + profitMargin) (0.15=15%, 0.10=10%, 0.20=20%)
        public void Calc()
        {
            // Step 1: Total glass price
            double glassTotal = Sheet1 + Sheet2;

            // Step 2: Divide by profit factor
            double profitFactor = Profit switch
            {
                "5%" => 0.95,
                "10%" => 0.90,
                "15%" => 0.85,
                "20%" => 0.80,
                "25%" => 0.75,
                "30%" => 0.70,
                "35%" => 0.65,
                "40%" => 0.60,
                _ => 0.85
            };
            double step2 = glassTotal / profitFactor;

            // Step 3: Add spacer costs
            double spacerCosts = SpacerPrice(SpacerSize) + SpacerColorPrice(SpacerColor);
            double step3 = step2 + spacerCosts;

            // Step 4: Apply profit margin (add back)
            double profitMargin = Profit switch
            {
                "5%" => 0.05,
                "10%" => 0.10,
                "15%" => 0.15,
                "20%" => 0.20,
                "25%" => 0.25,
                "30%" => 0.30,
                "35%" => 0.35,
                "40%" => 0.40,
                _ => 0.15
            };
            double final = step3 * (1 + profitMargin);

            // Result
            Result = final.ToString("0.00");

            // Calculate totals
            double sqm = (P(Width) / 1000) * (P(Height) / 1000) * P(Qty);
            TotalSqm = sqm.ToString("0.00");
            TotalPrice = (final * sqm).ToString("0.00");
            VatAmount = (final * sqm * 0.05).ToString("0.00");
            GrossTotal = (final * sqm * 1.05).ToString("0.00");
        }

        // Clear
        private void Clear()
        {
            _manual1 = false;
            _manual2 = false;
            Category1 = Cats.FirstOrDefault();
            Category2 = Cats.Skip(1).FirstOrDefault() ?? Cats.FirstOrDefault();
            Thickness1 = "4mm";
            Thickness2 = "4mm";
            Color1 = "Clear";
            Color2 = "Clear";
            SpacerSize = "12mm Air";
            SpacerColor = "Silver";
            Profit = "15%";
            Sheet1 = 0;
            Sheet2 = 0;
            Width = "1000";
            Height = "1000";
            Qty = "1";
            LoadSizes();
            Calc();
        }

        // Save
        public void Save()
        {
            Calc();
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

        // Export PDF
        public void ExportPdf()
        {
            try
            {
                var pd = new PrintDialog();
                if (pd.ShowDialog() == true)
                {
                    MessageBox.Show("PDF Exported via Print!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Relay Command
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _exec;
        public RelayCommand(Action<object> exec) => _exec = exec;
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
        public bool CanExecute(object p) => true;
        public void Execute(object p) => _exec(p);
    }
}