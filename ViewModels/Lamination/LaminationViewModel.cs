using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Views.Lamination
{
    public class LaminationViewModel : INotifyPropertyChanged
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
        // LOAD DATA FROM SHEET.CS
        // ═══════════════════════════════════════════════════════════
        public ObservableCollection<string> CategoryOptions { get; }
            = new(Sheet.Categories);

        public ObservableCollection<string> ThicknessOptions { get; }
            = new(Sheet.Thicknesses);

        public ObservableCollection<GlassColorItem> ColorOptions { get; }
            = new(Sheet.ColorItems);

        public ObservableCollection<string> WastageOptions { get; }
            = new(Sheet.WastageOptions);

        public ObservableCollection<string> ProfitMarginOptions { get; }
            = new(Sheet.ProfitMarginOptions);

        // ═══════════════════════════════════════════════════════════
        // LAMINATION ADVANCED OPTIONS FROM SHEET.CS
        // ═══════════════════════════════════════════════════════════
        public ObservableCollection<string> PVBTypes { get; }
            = new(Sheet.PVBTypes);

        public ObservableCollection<string> LaminationTypes { get; }
            = new(Sheet.LaminationTypes);

        public ObservableCollection<string> EdgeWorkTypes { get; }
            = new(Sheet.EdgeWorkTypes);

        public ObservableCollection<string> DrillingOptions { get; }
            = new(Sheet.DrillingOptions);

        public ObservableCollection<string> TemperingOptions { get; }
            = new(Sheet.TemperingOptions);

        public ObservableCollection<string> CoatingTypes { get; }
            = new(Sheet.CoatingTypes);

        public ObservableCollection<string> SurfaceTreatments { get; }
            = new(Sheet.SurfaceTreatments);

        public ObservableCollection<string> SealantTypes { get; }
            = new(Sheet.SealantTypes);

        public ObservableCollection<string> UnitOptions { get; }
            = new(Sheet.UnitOptions);

        public ObservableCollection<LaminationRecord> Records { get; } = new();

        // ═══════════════════════════════════════════════════════════
        // INVENTORY SUMMARY
        // ═══════════════════════════════════════════════════════════
        public int CategoryTotal => Sheet.Categories.Count;
        public int ThicknessTotal => Sheet.Thicknesses.Length;
        public int ColorTotal => Sheet.ColorItems.Count;
        public int PVBTypeTotal => Sheet.PVBTypes.Count;
        public int LaminationTypeTotal => Sheet.LaminationTypes.Count;
        public int EdgeWorkTotal => Sheet.EdgeWorkTypes.Count;
        public int TemperingTotal => Sheet.TemperingOptions.Count;
        public int CoatingTotal => Sheet.CoatingTypes.Count;
        public int DrillingTotal => Sheet.DrillingOptions.Count;

        // ═══════════════════════════════════════════════════════════
        // OUTER GLASS PROPERTIES
        // ═══════════════════════════════════════════════════════════
        private string _outerCategory = "Clear Float";
        public string OuterCategory
        {
            get => _outerCategory;
            set { _outerCategory = value; OnPropertyChanged(); Calculate(); }
        }

        private string _outerThickness = "6mm";
        public string OuterThickness
        {
            get => _outerThickness;
            set { _outerThickness = value; OnPropertyChanged(); Calculate(); }
        }

        private string _outerColor = "Clear";
        public string OuterColor
        {
            get => _outerColor;
            set { _outerColor = value; OnPropertyChanged(); Calculate(); }
        }

        private double? _outerPrice;
        public double? OuterPrice
        {
            get => _outerPrice;
            set { _outerPrice = value; OnPropertyChanged(); Calculate(); }
        }

        // ═══════════════════════════════════════════════════════════
        // INNER GLASS PROPERTIES
        // ═══════════════════════════════════════════════════════════
        private string _innerCategory = "Clear Float";
        public string InnerCategory
        {
            get => _innerCategory;
            set { _innerCategory = value; OnPropertyChanged(); Calculate(); }
        }

        private string _innerThickness = "4mm";
        public string InnerThickness
        {
            get => _innerThickness;
            set { _innerThickness = value; OnPropertyChanged(); Calculate(); }
        }

        private string _innerColor = "Clear";
        public string InnerColor
        {
            get => _innerColor;
            set { _innerColor = value; OnPropertyChanged(); Calculate(); }
        }

        private double? _innerPrice;
        public double? InnerPrice
        {
            get => _innerPrice;
            set { _innerPrice = value; OnPropertyChanged(); Calculate(); }
        }

        // ═══════════════════════════════════════════════════════════
        // PVB & LAMINATION PROPERTIES
        // ═══════════════════════════════════════════════════════════
        private string _pvbType = "Clear PVB 0.76mm";
        public string PVBType
        {
            get => _pvbType;
            set { _pvbType = value; OnPropertyChanged(); Calculate(); }
        }

        private double? _pvbPrice;
        public double? PVBPrice
        {
            get => _pvbPrice;
            set { _pvbPrice = value; OnPropertyChanged(); Calculate(); }
        }

        private string _laminationType = "Standard Lamination";
        public string LaminationType
        {
            get => _laminationType;
            set { _laminationType = value; OnPropertyChanged(); }
        }

        // ═══════════════════════════════════════════════════════════
        // PROCESSING CHARGES
        // ═══════════════════════════════════════════════════════════
        private double? _cuttingCharge;
        public double? CuttingCharge
        {
            get => _cuttingCharge;
            set { _cuttingCharge = value; OnPropertyChanged(); Calculate(); }
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
        // WASTAGE & PROFIT PROPERTIES
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
        // ADVANCED OPTIONS PROPERTIES (LAMINATION)
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

        private string _sealantType = "Butyl Sealant";
        public string SealantType
        {
            get => _sealantType;
            set { _sealantType = value; OnPropertyChanged(); }
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

        // Display Properties
        public string WastageFactorDisplay => $"÷ {1 - (double.Parse(_wastage) / 100.0):F2}";
        public string ProfitMarginDisplay => $"× {1 + (double.Parse(_profitMargin.Replace("%", "")) / 100.0):F2}";

        // History
        public bool IsHistoryVisible => Records.Count > 0;
        public bool IsHistoryEmpty => Records.Count == 0;

        // ═══════════════════════════════════════════════════════════
        // COMMANDS
        // ═══════════════════════════════════════════════════════════
        public ICommand SaveCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand ExportExcelCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand AddRowCommand { get; }

        // ═══════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════
        public LaminationViewModel()
        {
            // Set defaults from Sheet.cs data
            if (CategoryOptions.Count > 0)
            {
                _outerCategory = CategoryOptions[0];
                _innerCategory = CategoryOptions[0];
            }
            if (ThicknessOptions.Count > 0) _outerThickness = ThicknessOptions[0];
            if (ThicknessOptions.Count > 1) _innerThickness = ThicknessOptions[1];
            if (ColorOptions.Count > 0)
            {
                _outerColor = ColorOptions[0].Name;
                _innerColor = ColorOptions[0].Name;
            }
            if (WastageOptions.Count > 1) _wastage = WastageOptions[1];
            if (ProfitMarginOptions.Count > 1) _profitMargin = ProfitMarginOptions[1];
            if (PVBTypes.Count > 0) _pvbType = PVBTypes[0];
            if (LaminationTypes.Count > 0) _laminationType = LaminationTypes[0];
            if (EdgeWorkTypes.Count > 0) _edgeWork = EdgeWorkTypes[0];
            if (DrillingOptions.Count > 0) _drilling = DrillingOptions[0];
            if (TemperingOptions.Count > 0) _tempering = TemperingOptions[0];
            if (CoatingTypes.Count > 0) _coating = CoatingTypes[0];
            if (SurfaceTreatments.Count > 0) _surfaceTreatment = SurfaceTreatments[0];
            if (SealantTypes.Count > 0) _sealantType = SealantTypes[0];
            if (UnitOptions.Count > 0) _unit = UnitOptions[0];

            SaveCommand = new RelayCommand(o =>
            {
                var record = new LaminationRecord
                {
                    DisplayText = $"{OuterCategory} | {OuterThickness} | {OuterColor} + {InnerCategory} | {InnerThickness} | {InnerColor}",
                    OuterDetails = $"{OuterCategory} | {OuterThickness} | {OuterColor} | {OuterPrice:F2}",
                    InnerDetails = $"{InnerCategory} | {InnerThickness} | {InnerColor} | {InnerPrice:F2}",
                    PVBDetails = $"{PVBType} | {PVBPrice:F2}",
                    LaminationDetails = $"{LaminationType} | {EdgeWork} | {Drilling}",
                    TreatmentDetails = $"{Tempering} | {Coating} | {SurfaceTreatment}",
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
                };
                Records.Insert(0, record);
                OnPropertyChanged(nameof(IsHistoryVisible));
                OnPropertyChanged(nameof(IsHistoryEmpty));
                MessageBox.Show($"Lamination saved!\n\nUnit Price: {Result:F2} {Unit}\nTotal: {TotalPrice:F2} {Unit}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            ExportPdfCommand = new RelayCommand(o =>
            {
                MessageBox.Show("PDF export coming soon!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            ExportExcelCommand = new RelayCommand(o =>
            {
                MessageBox.Show("Excel export coming soon!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            ClearCommand = new RelayCommand(o =>
            {
                OuterPrice = null;
                InnerPrice = null;
                PVBPrice = null;
                CuttingCharge = null;
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
                if (o is LaminationRecord r) Records.Remove(r);
                OnPropertyChanged(nameof(IsHistoryVisible));
                OnPropertyChanged(nameof(IsHistoryEmpty));
            });

            AddRowCommand = new RelayCommand(o =>
            {
                ClearCommand.Execute(null);
            });

            Calculate();
        }

        // ═══════════════════════════════════════════════════════════
        // CALCULATE DIMENSIONS
        // ═══════════════════════════════════════════════════════════
        private void CalculateDimensions()
        {
            if (Width > 0 && Height > 0)
            {
                _totalArea = (Width * Height * Quantity) / 1000000.0; // Convert mm² to m²
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
        // CALCULATE (KEEP FORMULA AS IS)
        // ═══════════════════════════════════════════════════════════
        public void Calculate()
        {
            // STEP 1: Glass Cost = Sheet 1 + Sheet 2
            double sheet1 = OuterPrice ?? 0;
            double sheet2 = InnerPrice ?? 0;
            _glassCost = sheet1 + sheet2;

            // STEP 2: Base Cost = Glass Cost ÷ Wastage Factor
            double wastageFactor = 1 - (double.Parse(_wastage) / 100.0);
            _baseCost = wastageFactor > 0 ? _glassCost / wastageFactor : _glassCost;

            // STEP 3: Processing Cost = Base Cost + PVB + Cutting + Tempering + Other
            double pvb = PVBPrice ?? 0;
            double cutting = CuttingCharge ?? 0;
            double tempering = TemperingCharge ?? 0;
            double other = OtherCharges ?? 0;
            _processingCost = _baseCost + pvb + cutting + tempering + other;

            // STEP 4: Unit Price = Processing Cost × Profit Factor
            double profitFactor = 1 + (double.Parse(_profitMargin.Replace("%", "")) / 100.0);
            _result = _processingCost * profitFactor;

            // VAT (5%) and Gross Total
            _vatAmount = _result * 0.05;
            _grossTotal = _result + _vatAmount;

        // Notify all changes
        OnPropertyChanged(nameof(GlassCost));
            OnPropertyChanged(nameof(ProcessingCost));
            OnPropertyChanged(nameof(BaseCost));
            OnPropertyChanged(nameof(Result));
            OnPropertyChanged(nameof(VatAmount));
            OnPropertyChanged(nameof(GrossTotal));
            OnPropertyChanged(nameof(WastageFactorDisplay));
            OnPropertyChanged(nameof(ProfitMarginDisplay));
        }

        // ═══════════════════════════════════════════════════════════
        // TOTAL GLASS COST (for display)
        // ═══════════════════════════════════════════════════════════
        public double TotalGlassCost => _glassCost;
    }

    // ═══════════════════════════════════════════════════════════
    // RECORD CLASS
    // ═══════════════════════════════════════════════════════════
    public class LaminationRecord : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public string DisplayText { get; set; }
        public string OuterDetails { get; set; }
        public string InnerDetails { get; set; }
        public string PVBDetails { get; set; }
        public string LaminationDetails { get; set; }
        public string TreatmentDetails { get; set; }
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

    // ═══════════════════════════════════════════════════════════
    // RELAY COMMAND
    // ═══════════════════════════════════════════════════════════
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

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}