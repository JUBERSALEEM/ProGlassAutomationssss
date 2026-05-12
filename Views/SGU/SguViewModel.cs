using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.ViewModels
{
    public class SguViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // ═══════════════════════════════════════════════════════════
        // COLLECTIONS - Initialize empty, load in constructor
        // ═══════════════════════════════════════════════════════════

        public ObservableCollection<string> CategoryOptions { get; } = new();
        public ObservableCollection<string> ThicknessOptions { get; } = new();
        public ObservableCollection<GlassColorItem> ColorOptions { get; } = new();
        public ObservableCollection<string> WastageOptions { get; } = new();
        public ObservableCollection<string> ProfitMarginOptions { get; } = new();
        public ObservableCollection<string> EdgeWorkTypes { get; } = new();
        public ObservableCollection<string> DrillingOptions { get; } = new();
        public ObservableCollection<string> TemperingOptions { get; } = new();
        public ObservableCollection<string> CoatingTypes { get; } = new();
        public ObservableCollection<string> SurfaceTreatments { get; } = new();
        public ObservableCollection<string> CutoutOptions { get; } = new();
        public ObservableCollection<string> UnitOptions { get; } = new();

        public ObservableCollection<SguRecord> Records { get; } = new();

        // ═══════════════════════════════════════════════════════════
        // INVENTORY TOTALS
        // ═══════════════════════════════════════════════════════════

        public int CategoryTotal => CategoryOptions.Count;
        public int ThicknessTotal => ThicknessOptions.Count;
        public int ColorTotal => ColorOptions.Count;
        public int TemperingTotal => TemperingOptions.Count;
        public int EdgeWorkTotal => EdgeWorkTypes.Count;
        public int CoatingTotal => CoatingTypes.Count;
        public int DrillingTotal => DrillingOptions.Count;
        public int SurfaceTreatmentTotal => SurfaceTreatments.Count;
        public int CutoutTotal => CutoutOptions.Count;
        public int WastageTotal => WastageOptions.Count;
        public int ProfitTotal => ProfitMarginOptions.Count;

        // ═══════════════════════════════════════════════════════════
        // PROPERTIES - GLASS SELECTION
        // ═══════════════════════════════════════════════════════════

        private string _category = "";
        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(); Calculate(); }
        }

        private string _thickness = "";
        public string Thickness
        {
            get => _thickness;
            set { _thickness = value; OnPropertyChanged(); Calculate(); }
        }

        private string _colorName = "";
        public string ColorName
        {
            get => _colorName;
            set { _colorName = value; OnPropertyChanged(); Calculate(); }
        }

        private double? _sheetPrice;
        public double? SheetPrice
        {
            get => _sheetPrice;
            set { _sheetPrice = value; OnPropertyChanged(); Calculate(); }
        }

        // ═══════════════════════════════════════════════════════════
        // PROPERTIES - PROCESSING CHARGES
        // ═══════════════════════════════════════════════════════════

        private double? _cutting;
        public double? Cutting
        {
            get => _cutting;
            set { _cutting = value; OnPropertyChanged(); Calculate(); }
        }

        private double? _temperingCharge;
        public double? TemperingCharge
        {
            get => _temperingCharge;
            set { _temperingCharge = value; OnPropertyChanged(); Calculate(); }
        }

        private double? _otherCharges;
        public double? OtherCharges
        {
            get => _otherCharges;
            set { _otherCharges = value; OnPropertyChanged(); Calculate(); }
        }

        // ═══════════════════════════════════════════════════════════
        // PROPERTIES - WASTAGE & PROFIT
        // ═══════════════════════════════════════════════════════════

        private string _wastage = "15";
        public string Wastage
        {
            get => _wastage;
            set { _wastage = value; OnPropertyChanged(); Calculate(); }
        }

        private string _profitMargin = "15%";
        public string ProfitMargin
        {
            get => _profitMargin;
            set { _profitMargin = value; OnPropertyChanged(); Calculate(); }
        }

        // ═══════════════════════════════════════════════════════════
        // PROPERTIES - ADVANCED OPTIONS
        // ═══════════════════════════════════════════════════════════

        private string _edgeWork = "Clean Cut";
        public string EdgeWork
        {
            get => _edgeWork;
            set { _edgeWork = value; OnPropertyChanged(); }
        }

        private string _drilling = "No Hole";
        public string Drilling
        {
            get => _drilling;
            set { _drilling = value; OnPropertyChanged(); }
        }

        private string _tempering = "Annealed (Plain)";
        public string Tempering
        {
            get => _tempering;
            set { _tempering = value; OnPropertyChanged(); }
        }

        private string _coating = "None";
        public string Coating
        {
            get => _coating;
            set { _coating = value; OnPropertyChanged(); }
        }

        private string _surfaceTreatment = "None";
        public string SurfaceTreatment
        {
            get => _surfaceTreatment;
            set { _surfaceTreatment = value; OnPropertyChanged(); }
        }

        private string _cutout = "No Cutout";
        public string Cutout
        {
            get => _cutout;
            set { _cutout = value; OnPropertyChanged(); }
        }

        private string _customNotes = "";
        public string CustomNotes
        {
            get => _customNotes;
            set { _customNotes = value; OnPropertyChanged(); }
        }

        private string _unit = "AED";
        public string Unit
        {
            get => _unit;
            set { _unit = value; OnPropertyChanged(); }
        }

        // ═══════════════════════════════════════════════════════════
        // PROPERTIES - DIMENSIONS
        // ═══════════════════════════════════════════════════════════

        private int _width;
        public int Width
        {
            get => _width;
            set { _width = value; OnPropertyChanged(); CalculateDimensions(); }
        }

        private int _height;
        public int Height
        {
            get => _height;
            set { _height = value; OnPropertyChanged(); CalculateDimensions(); }
        }

        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); CalculateDimensions(); }
        }

        private double _totalArea;
        public double TotalArea
        {
            get => _totalArea;
            set { _totalArea = value; OnPropertyChanged(); }
        }

        private double _totalPrice;
        public double TotalPrice
        {
            get => _totalPrice;
            set { _totalPrice = value; OnPropertyChanged(); }
        }

        // ═══════════════════════════════════════════════════════════
        // CALCULATION RESULTS
        // ═══════════════════════════════════════════════════════════

        private double _glassCost;
        public double GlassCost
        {
            get => _glassCost;
            set { _glassCost = value; OnPropertyChanged(); }
        }

        private double _processingCost;
        public double ProcessingCost
        {
            get => _processingCost;
            set { _processingCost = value; OnPropertyChanged(); }
        }

        private double _baseCost;
        public double BaseCost
        {
            get => _baseCost;
            set { _baseCost = value; OnPropertyChanged(); }
        }

        private double _subtotal;
        public double Subtotal
        {
            get => _subtotal;
            set { _subtotal = value; OnPropertyChanged(); }
        }

        private double _result;
        public double Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(); }
        }

        private double _vatAmount;
        public double VatAmount
        {
            get => _vatAmount;
            set { _vatAmount = value; OnPropertyChanged(); }
        }

        private double _grossTotal;
        public double GrossTotal
        {
            get => _grossTotal;
            set { _grossTotal = value; OnPropertyChanged(); }
        }

        // ═══════════════════════════════════════════════════════════
        // HISTORY
        // ═══════════════════════════════════════════════════════════

        public bool IsHistoryVisible => Records.Count > 0;
        public bool IsHistoryEmpty => Records.Count == 0;

        // ═══════════════════════════════════════════════════════════
        // COMMANDS
        // ═══════════════════════════════════════════════════════════

        public ICommand SaveCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand DeleteCommand { get; }

        // ═══════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════

        public SguViewModel()
        {
            // Load data from Sheet.cs
            AddRange(CategoryOptions, Sheet.Categories);
            AddRange(ThicknessOptions, Sheet.Thicknesses);
            AddRange(ColorOptions, Sheet.ColorItems);
            AddRange(WastageOptions, Sheet.WastageOptions);
            AddRange(ProfitMarginOptions, Sheet.ProfitMarginOptions);
            AddRange(EdgeWorkTypes, Sheet.EdgeWorkTypes);
            AddRange(DrillingOptions, Sheet.DrillingOptions);
            AddRange(TemperingOptions, Sheet.TemperingOptions);
            AddRange(CoatingTypes, Sheet.CoatingTypes);
            AddRange(SurfaceTreatments, Sheet.SurfaceTreatments);
            AddRange(CutoutOptions, Sheet.CutoutOptions);
            AddRange(UnitOptions, Sheet.UnitOptions);

            // Set dropdown defaults
            if (CategoryOptions.Count > 0) _category = CategoryOptions[0];
            if (ThicknessOptions.Count > 0) _thickness = ThicknessOptions[0];
            if (ColorOptions.Count > 0) _colorName = ColorOptions[0].Name;
            if (WastageOptions.Count > 1) _wastage = WastageOptions[1];
            if (ProfitMarginOptions.Count > 1) _profitMargin = ProfitMarginOptions[1];
            if (EdgeWorkTypes.Count > 0) _edgeWork = EdgeWorkTypes[0];
            if (DrillingOptions.Count > 0) _drilling = DrillingOptions[0];
            if (TemperingOptions.Count > 0) _tempering = TemperingOptions[0];
            if (CoatingTypes.Count > 0) _coating = CoatingTypes[0];
            if (SurfaceTreatments.Count > 0) _surfaceTreatment = SurfaceTreatments[0];
            if (CutoutOptions.Count > 0) _cutout = CutoutOptions[0];
            if (UnitOptions.Count > 0) _unit = UnitOptions[0];

            SaveCommand = new RelayCommand(o =>
            {
                if (!_sheetPrice.HasValue)
                {
                    MessageBox.Show("Please enter sheet price", "Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Records.Insert(0, new SguRecord
                {
                    DisplayText = $"{Category} | {Thickness} | {ColorName}",
                    GlassDetails = $"{Category} | {Thickness} | {ColorName} | {SheetPrice:F2}",
                    ProcessingDetails = $"{EdgeWork} | {Drilling} | {Tempering}",
                    TreatmentDetails = $"{Coating} | {SurfaceTreatment} | {Cutout}",
                    SheetPrice = SheetPrice ?? 0,
                    Cutting = Cutting ?? 0,
                    TemperingCharge = TemperingCharge ?? 0,
                    OtherCharges = OtherCharges ?? 0,
                    Wastage = $"{_wastage}%",
                    ProfitMargin = _profitMargin,
                    CreatedAt = DateTime.Now.ToString("dd/MM HH:mm"),
                    Result = Result,
                    Unit = Unit,
                    Width = Width,
                    Height = Height,
                    Quantity = Quantity,
                    TotalArea = TotalArea,
                    TotalPrice = TotalPrice,
                    CustomNotes = CustomNotes
                });

                OnPropertyChanged(nameof(IsHistoryVisible));
                OnPropertyChanged(nameof(IsHistoryEmpty));
                MessageBox.Show($"SGU saved!\n\nUnit Price: {Result:F2} {Unit}\nTotal: {TotalPrice:F2} {Unit}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            ExportPdfCommand = new RelayCommand(o =>
            {
                MessageBox.Show("PDF export coming soon!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            ClearCommand = new RelayCommand(o =>
            {
                SheetPrice = null;
                Cutting = null;
                TemperingCharge = null;
                OtherCharges = null;
                Width = 0;
                Height = 0;
                Quantity = 1;
                CustomNotes = "";
            });

            ClearAllCommand = new RelayCommand(o =>
            {
                Records.Clear();
                OnPropertyChanged(nameof(IsHistoryVisible));
                OnPropertyChanged(nameof(IsHistoryEmpty));
            });

            DeleteCommand = new RelayCommand(o =>
            {
                if (o is SguRecord r) Records.Remove(r);
                OnPropertyChanged(nameof(IsHistoryVisible));
                OnPropertyChanged(nameof(IsHistoryEmpty));
            });

            Calculate();
        }

        // ═══════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════

        private void AddRange(ObservableCollection<string> collection, string[] array)
        {
            if (array == null) return;
            foreach (var item in array)
                collection.Add(item);
        }

        private void AddRange(ObservableCollection<string> collection, List<string> list)
        {
            if (list == null) return;
            foreach (var item in list)
                collection.Add(item);
        }

        private void AddRange(ObservableCollection<GlassColorItem> collection, List<GlassColorItem> list)
        {
            if (list == null) return;
            foreach (var item in list)
                collection.Add(item);
        }

        // ═══════════════════════════════════════════════════════════
        // CALCULATE DIMENSIONS
        // ═══════════════════════════════════════════════════════════

        private void CalculateDimensions()
        {
            if (Width > 0 && Height > 0)
            {
                _totalArea = (Width * Height * Quantity) / 1000000.0;
                _totalPrice = Result * Quantity;
            }
            else
            {
                _totalArea = 0;
                _totalPrice = Result * Quantity;
            }
            OnPropertyChanged(nameof(TotalArea));
            OnPropertyChanged(nameof(TotalPrice));
        }

        // ═══════════════════════════════════════════════════════════
        // CALCULATE
        // ═══════════════════════════════════════════════════════════

        public void Calculate()
        {
            _glassCost = SheetPrice ?? 0;
            double wastageFactor = 1 - (double.Parse(_wastage) / 100.0);
            _baseCost = wastageFactor > 0 ? _glassCost / wastageFactor : _glassCost;

            double cutting = Cutting ?? 0;
            double tempering = TemperingCharge ?? 0;
            double other = OtherCharges ?? 0;
            _processingCost = cutting + tempering + other;

            double profitFactor = 1 + (double.Parse(_profitMargin.Replace("%", "")) / 100.0);
            _subtotal = _baseCost + _processingCost;
            _result = _subtotal * profitFactor;

            _vatAmount = _result * 0.05;
            _grossTotal = _result + _vatAmount;

            CalculateDimensions();

            OnPropertyChanged(nameof(GlassCost));
            OnPropertyChanged(nameof(ProcessingCost));
            OnPropertyChanged(nameof(BaseCost));
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Result));
            OnPropertyChanged(nameof(VatAmount));
            OnPropertyChanged(nameof(GrossTotal));
        }
    }

    // ═══════════════════════════════════════════════════════════
    // RECORD CLASS
    // ═══════════════════════════════════════════════════════════

    public class SguRecord : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public string DisplayText { get; set; }
        public string GlassDetails { get; set; }
        public string ProcessingDetails { get; set; }
        public string TreatmentDetails { get; set; }
        public double SheetPrice { get; set; }
        public double Cutting { get; set; }
        public double TemperingCharge { get; set; }
        public double OtherCharges { get; set; }
        public string Wastage { get; set; }
        public string ProfitMargin { get; set; }
        public string CreatedAt { get; set; }
        public double Result { get; set; }
        public string Unit { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Quantity { get; set; }
        public double TotalArea { get; set; }
        public double TotalPrice { get; set; }
        public string CustomNotes { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }
    }
}