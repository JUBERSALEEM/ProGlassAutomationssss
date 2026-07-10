using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Views.Optimization
{
    public class OptimizationViewModel : INotifyPropertyChanged
    {
        private readonly OptimizationEngine _engine = new OptimizationEngine();

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<StockSheetViewModel> StockSheets { get; } = new ObservableCollection<StockSheetViewModel>();
        public ObservableCollection<CutPart> Parts { get; } = new ObservableCollection<CutPart>();
        public ObservableCollection<LayoutRowVM> Layouts { get; } = new ObservableCollection<LayoutRowVM>();

        private OptimizationResult? _lastResult;
        public OptimizationResult? LastResult
        {
            get => _lastResult;
            private set
            {
                _lastResult = value;
                OnPropertyChanged(nameof(LastResult));
                OnPropertyChanged(nameof(HasResult));
                OnPropertyChanged(nameof(Utilization));
                OnPropertyChanged(nameof(Waste));
                OnPropertyChanged(nameof(PlacedPartCount));
                OnPropertyChanged(nameof(SheetsUsed));
                OnPropertyChanged(nameof(AverageUtilization));
                OnPropertyChanged(nameof(LastSheets));
            }
        }

        public bool HasResult => _lastResult != null;
        public int PlacedPartCount => _lastResult?.PlacedParts?.Count ?? 0;

        private List<SheetResult> _lastSheets = new List<SheetResult>();
        public List<SheetResult> LastSheets
        {
            get => _lastSheets;
            private set
            {
                _lastSheets = value;
                OnPropertyChanged(nameof(LastSheets));
                OnPropertyChanged(nameof(SheetsUsed));
            }
        }

        private int _stockSheetCount = 0;
        public int StockSheetCount
        {
            get => _stockSheetCount;
            private set
            {
                _stockSheetCount = value;
                OnPropertyChanged(nameof(StockSheetCount));
            }
        }

        private int _partCount = 0;
        public int PartCount
        {
            get => _partCount;
            private set
            {
                _partCount = value;
                OnPropertyChanged(nameof(PartCount));
            }
        }

        private double _utilization;
        public double Utilization
        {
            get => _utilization;
            private set
            {
                _utilization = value;
                OnPropertyChanged(nameof(Utilization));
            }
        }

        private double _waste;
        public double Waste
        {
            get => _waste;
            private set
            {
                _waste = value;
                OnPropertyChanged(nameof(Waste));
            }
        }

        public int SheetsUsed => LastSheets?.Count ?? 0;
        public double AverageUtilization => _utilization;

        private bool _hasContent = false;
        public bool HasContent
        {
            get => _hasContent;
            private set
            {
                _hasContent = value;
                OnPropertyChanged(nameof(HasContent));
            }
        }

        public ICommand AddStockCommand { get; }
        public ICommand AddPartCommand { get; }

        public OptimizationViewModel()
        {
            AddStockCommand = new RelayCommand(AddStock);
            AddPartCommand = new RelayCommand(AddPart);
        }

        private void AddStock()
        {
            StockSheets.Add(new StockSheetViewModel { Index = StockSheets.Count + 1, L = 3210, W = 2250, Qty = 100 });
            UpdateCounts();
        }

        private void AddPart()
        {
            Parts.Add(new CutPart { L = 1000, W = 800, Qty = 1 });
            UpdateCounts();
        }

        private void UpdateCounts()
        {
            StockSheetCount = StockSheets.Sum(s => s.Qty);
            PartCount = Parts.Sum(p => p.Qty);
            HasContent = StockSheets.Count > 0 || Parts.Count > 0;
        }

        // ═══════════════════════════════════════════════════════
        // OPTIMIZATION
        // ═══════════════════════════════════════════════════════

        public bool RunOptimizationSync(
            double kerf, double trim,
            double breakL, double breakR,
            double breakT, double breakB,
            double minBreak,
            int rotationMode, int quality)
        {
            if (Parts.Count == 0) return false;

            if (StockSheets.Count == 0)
            {
                StockSheets.Add(new StockSheetViewModel
                {
                    Index = 1,
                    L = 3210,
                    W = 2250,
                    Qty = 9999,
                    PricePerM2 = 1559.86
                });
            }

            var validParts = Parts.Where(p => p.L > 0 && p.W > 0 && p.Qty > 0).ToList();
            if (validParts.Count == 0) return false;

            try
            {
                var stocks = StockSheets.Select(s => new StockSheet
                {
                    L = s.L,
                    W = s.W,
                    Qty = s.Qty
                }).ToList();

                var partsCopy = validParts.ToList();

                var rotMode = rotationMode switch
                {
                    0 => RotationMode.None,
                    1 => RotationMode.Rotate90,
                    _ => RotationMode.BestFit
                };

                var result = _engine.Execute(
                    stocks, partsCopy,
                    breakL, breakR, breakT, breakB,
                    kerf, rotMode);

                LastResult = result;
                Utilization = result?.Util ?? 0;
                Waste = result?.Waste ?? 0;

                LastSheets = GenerateSheetsFromResult(
                    stocks, partsCopy, result,
                    breakL, breakR, breakT, breakB, kerf, rotMode);

                PopulateLayoutsGrid();
                UpdateCounts();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OptimizationViewModel] RunOptimizationSync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RunOptimizationAsync()
        {
            if (Parts.Count == 0) return false;

            if (StockSheets.Count == 0)
            {
                StockSheets.Add(new StockSheetViewModel
                {
                    Index = 1,
                    L = 3210,
                    W = 2250,
                    Qty = 9999,
                    PricePerM2 = 1559.86
                });
            }

            var validParts = Parts.Where(p => p.L > 0 && p.W > 0 && p.Qty > 0).ToList();
            if (validParts.Count == 0) return true;

            try
            {
                var stocks = StockSheets.Select(s => new StockSheet
                {
                    L = s.L,
                    W = s.W,
                    Qty = s.Qty
                }).ToList();

                var partsCopy = validParts.ToList();

                var result = await Task.Run(() =>
                {
                    var r = _engine.Execute(
                        stocks, partsCopy,
                        15, 15, 15, 15, 4, RotationMode.BestFit);
                    return r;
                }).ConfigureAwait(true);

                LastResult = result;
                Utilization = result?.Util ?? 0;
                Waste = result?.Waste ?? 0;
                LastSheets = GenerateSheetsFromResult(
                    stocks, partsCopy, result,
                    15, 15, 15, 15, 4, RotationMode.BestFit);

                PopulateLayoutsGrid();
                UpdateCounts();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OptimizationViewModel] RunOptimizationAsync: {ex.Message}");
                return false;
            }
        }

        // ✅ Group by SheetIndex from engine (not Y-coordinate heuristic)
        private List<SheetResult> GenerateSheetsFromResult(
            List<StockSheet> stocks, List<CutPart> parts, OptimizationResult? result,
            double breakL, double breakR, double breakT, double breakB,
            double kerf, RotationMode mode)
        {
            var sheets = new List<SheetResult>();
            if (result == null || result.PlacedParts == null || result.PlacedParts.Count == 0)
                return sheets;

            if (stocks == null || stocks.Count == 0)
                return sheets;

            double stockL = stocks[0].L;
            double stockW = stocks[0].W;

            // Group by SheetIndex (each part knows which physical sheet it belongs to)
            var sheetGroups = result.PlacedParts
                .GroupBy(p => p.SheetIndex)
                .OrderBy(g => g.Key)
                .ToList();

            for (int i = 0; i < sheetGroups.Count; i++)
            {
                var group = sheetGroups[i].ToList();
                double usedArea = group.Sum(p => p.L * p.W);
                double util = stockL * stockW > 0
                    ? (usedArea / (stockL * stockW)) * 100
                    : 0;

                sheets.Add(new SheetResult
                {
                    SheetNum = i + 1,
                    StockId = $"S{i + 1}",
                    StockWidth = stockL,
                    StockHeight = stockW,
                    SheetCost = 0,
                    UsedArea = usedArea,
                    WasteArea = Math.Max(0, stockL * stockW - usedArea),
                    Utilization = util,
                    PlacedParts = group
                });
            }

            return sheets;
        }

        private void PopulateLayoutsGrid()
        {
            Layouts.Clear();
            for (int i = 0; i < LastSheets.Count; i++)
            {
                var s = LastSheets[i];
                int rotated = s.PlacedParts?.Count(p => p.Rotated) ?? 0;
                double util = s.Utilization;

                Layouts.Add(new LayoutRowVM
                {
                    SheetNum = s.SheetNum,
                    Dimensions = $"{s.StockWidth:N0}×{s.StockHeight:N0} mm",
                    GlassCount = s.PlacedParts?.Count ?? 0,
                    Rotated90 = rotated,
                    YieldPct = util,
                    UsedNet = s.UsedArea / 1_000_000.0,
                    ScrapWaste = Math.Max(0, s.WasteArea / 1_000_000.0)
                });
            }
        }

        // ═══════════════════════════════════════════════════════
        // PROFORMA INVOICE IMPORT - ALL items are PARTS, stock set separately
        // ═══════════════════════════════════════════════════════
        public void ImportInvoiceItems(List<InvoiceItemModel> items)
        {
            if (items == null) return;

            // ✅ FIX: All items are PARTS. Stock is set separately via SetStockSheet.
            // The old code wrongly classified items > 2000mm as stock, which
            // caused 1000x3100 parts to be lost.
            Parts.Clear();
            foreach (var item in items)
            {
                if (item == null) continue;
                Parts.Add(new CutPart
                {
                    L = item.Width1,
                    W = item.Height1,
                    Qty = item.Qty > 0 ? item.Qty : 1
                });
            }

            UpdateCounts();
        }

        public void RunOptimizationFromInvoice()
        {
            try
            {
                RunOptimizationSync(4, 10, 15, 15, 15, 15, 15, 2, 95);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OptimizationViewModel] RunOptimizationFromInvoice: {ex.Message}");
            }
        }

        public void SetStockSheet(double width, double height)
        {
            if (width <= 0 || height <= 0) return;

            // ✅ FIX: If matching stock exists, preserve it. Don't clear.
            var existing = StockSheets.FirstOrDefault(s =>
                Math.Abs(s.L - width) < 0.01 && Math.Abs(s.W - height) < 0.01);

            if (existing != null)
            {
                UpdateCounts();
                return;
            }

            StockSheets.Clear();
            StockSheets.Add(new StockSheetViewModel
            {
                Index = 1,
                L = width,
                W = height,
                Qty = 100,
                PricePerM2 = 1559.86
            });
            UpdateCounts();
        }

        public void SetTrimSettings(double lr, double br, double tr, double rm, double kerf, double breakout) { }

        public List<OptimizationResult> GetResultsList()
        {
            if (LastResult != null) return new List<OptimizationResult> { LastResult };
            return new List<OptimizationResult>();
        }

        public void ExportCsv(string filePath)
        {
            if (LastResult == null || string.IsNullOrEmpty(filePath)) return;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== MaxNest Optimization Report ===");
            sb.AppendLine($"Generated,{DateTime.Now}");
            sb.AppendLine($"Runtime,{LastResult.RuntimeMs} ms");
            sb.AppendLine();
            sb.AppendLine($"Sheet,{LastResult.L} x {LastResult.W} mm");
            sb.AppendLine($"Utilization,{LastResult.Util}%");
            sb.AppendLine($"Waste,{LastResult.Waste}%");
            sb.AppendLine($"Parts Placed,{LastResult.PlacedParts?.Count ?? 0}");

            System.IO.File.WriteAllText(filePath, sb.ToString());
        }

        public string BuildReport()
        {
            if (LastResult == null) return "No optimization result available.";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== MaxNest Report ===");
            sb.AppendLine($"Sheet,{LastResult.L} x {LastResult.W} mm");
            sb.AppendLine($"Utilization,{LastResult.Util}%");
            sb.AppendLine($"Waste,{LastResult.Waste}%");
            sb.AppendLine($"Parts Placed,{LastResult.PlacedParts?.Count ?? 0}");
            sb.AppendLine($"Runtime,{LastResult.RuntimeMs} ms");
            return sb.ToString();
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
    }

    public class StockSheetViewModel : INotifyPropertyChanged
    {
        private int _index;
        private double _l;
        private double _w;
        private int _qty;
        private double _pricePerM2;

        public int Index
        {
            get => _index;
            set { _index = value; OnPropertyChanged(); }
        }

        public double L
        {
            get => _l;
            set
            {
                _l = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Area));
                OnPropertyChanged(nameof(UnitPrice));
            }
        }

        public double W
        {
            get => _w;
            set
            {
                _w = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Area));
                OnPropertyChanged(nameof(UnitPrice));
            }
        }

        public int Qty
        {
            get => _qty;
            set { _qty = value; OnPropertyChanged(); }
        }

        public double PricePerM2
        {
            get => _pricePerM2;
            set
            {
                _pricePerM2 = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UnitPrice));
            }
        }

        public double Area => (L * W) / 1_000_000.0;
        public double UnitPrice => Area * PricePerM2;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class LayoutRowVM : INotifyPropertyChanged
    {
        private int _sheetNum;
        private string _dimensions = "";
        private int _glassCount;
        private int _rotated90;
        private double _yieldPct;
        private double _usedNet;
        private double _scrapWaste;

        public int SheetNum
        {
            get => _sheetNum;
            set { _sheetNum = value; OnPropertyChanged(); }
        }

        public string Dimensions
        {
            get => _dimensions;
            set { _dimensions = value; OnPropertyChanged(); }
        }

        public int GlassCount
        {
            get => _glassCount;
            set { _glassCount = value; OnPropertyChanged(); }
        }

        public int Rotated90
        {
            get => _rotated90;
            set { _rotated90 = value; OnPropertyChanged(); }
        }

        public double YieldPct
        {
            get => _yieldPct;
            set
            {
                _yieldPct = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(YieldText));
            }
        }

        public string YieldText => $"{YieldPct:0.0}%";

        public double UsedNet
        {
            get => _usedNet;
            set { _usedNet = value; OnPropertyChanged(); }
        }

        public double ScrapWaste
        {
            get => _scrapWaste;
            set { _scrapWaste = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class SheetResult
    {
        public int SheetNum { get; set; }
        public string StockId { get; set; } = "";
        public double StockWidth { get; set; }
        public double StockHeight { get; set; }
        public double SheetCost { get; set; }
        public double UsedArea { get; set; }
        public double WasteArea { get; set; }
        public double Utilization { get; set; }
        public List<PlacedPart> PlacedParts { get; set; } = new List<PlacedPart>();
    }
}