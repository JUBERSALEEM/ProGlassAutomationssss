using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProGlassAutomation.Models
{
    public class InvoiceItemModel : INotifyPropertyChanged
    {
        private readonly object _calculationLock = new object();
        private bool _isRecalculating;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;

            if (IsDimensionProperty(propertyName) && !ValidateDimension(value))
            {
                throw new ArgumentOutOfRangeException(propertyName, "Dimensions cannot be negative.");
            }

            field = value;
            OnPropertyChanged(propertyName);

            if (!_isRecalculating)
            {
                TriggerCalculationAsync();
            }

            return true;
        }

        private bool IsDimensionProperty(string propertyName)
        {
            return propertyName is nameof(Width1) or nameof(Height1)
                or nameof(Width2) or nameof(Height2) or nameof(Qty);
        }

        private bool ValidateDimension<T>(T value)
        {
            if (value is double d) return d >= 0;
            if (value is int i) return i >= 0;
            return true;
        }

        private void TriggerCalculationAsync()
        {
            lock (_calculationLock)
            {
                if (_isRecalculating)
                    return;

                _isRecalculating = true;

                try
                {
                    CalculateAll();
                }
                finally
                {
                    _isRecalculating = false;
                }
            }
        }

        // ==================== PARENT SPECIFICATION REFERENCE ====================
        private SpecificationModel? _specification;
        public SpecificationModel? Specification
        {
            get => _specification;
            set
            {
                if (_specification != value)
                {
                    _specification = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Price));
                    OnPropertyChanged(nameof(BasePrice));
                    OnPropertyChanged(nameof(SurchargePercent));
                    OnPropertyChanged(nameof(UnitPrice));
                    OnPropertyChanged(nameof(DisplayPrice));
                    CalculateAll();
                }
            }
        }

        private int _srNo = 1;
        public int SrNo { get => _srNo; set => SetProperty(ref _srNo, value); }

        private string _glassRef = "";
        public string GlassRef { get => _glassRef; set => SetProperty(ref _glassRef, value); }

        private double _width1 = 0;
        public double Width1 { get => _width1; set => SetProperty(ref _width1, value); }

        private double _height1 = 0;
        public double Height1 { get => _height1; set => SetProperty(ref _height1, value); }

        private double _width2 = 0;
        public double Width2 { get => _width2; set => SetProperty(ref _width2, value); }

        private double _height2 = 0;
        public double Height2 { get => _height2; set => SetProperty(ref _height2, value); }

        private int _qty = 1;
        public int Qty { get => _qty; set => SetProperty(ref _qty, value); }

        private double _price = 0;

        // ✅ Price = BasePrice + Surcharge ONLY when W1×H1 >= 4 sqm (W2×H2 not included)
        public double Price
        {
            get
            {
                double baseP = _specification?.BasePrice ?? _price;

                if (_specification != null)
                {
                    double primarySQM = CalculateSingleSQM(_width1, _height1);
                    if (primarySQM >= SurchargeThreshold)
                    {
                        double surcharge = _specification.SurchargePercent;
                        return baseP * (1 + surcharge / 100.0);
                    }
                }
                return baseP;
            }
            set
            {
                if (SetProperty(ref _price, value))
                {
                    OnPropertyChanged(nameof(BasePrice));
                    OnPropertyChanged(nameof(UnitPrice));
                    OnPropertyChanged(nameof(DisplayPrice));
                }
            }
        }

        public double BasePrice => _specification?.BasePrice ?? _price;

        private double _surchargePercent = 0;

        public double SurchargePercent
        {
            get => _specification?.SurchargePercent ?? _surchargePercent;
            set
            {
                if (value < 0 || value > 100)
                    throw new ArgumentOutOfRangeException(nameof(SurchargePercent), "Surcharge must be between 0 and 100.");
                if (SetProperty(ref _surchargePercent, value))
                {
                    OnPropertyChanged(nameof(HasSurcharge));
                }
            }
        }

        public const double SurchargeThreshold = 4;

        // ✅ HasSurcharge = true when W1×H1 >= 4 sqm AND surcharge > 0
        public bool HasSurcharge
        {
            get
            {
                if (_specification == null) return false;
                double primarySQM = CalculateSingleSQM(_width1, _height1);
                return primarySQM >= SurchargeThreshold && _specification.SurchargePercent > 0;
            }
        }

        public double UnitPrice => Math.Round(Price, 2);
        public double DisplayPrice => UnitPrice;
        public double FinalPrice => UnitPrice;

        private double _sqm1 = 0;
        public double SQM1
        {
            get => _sqm1;
            private set => SetProperty(ref _sqm1, value);
        }

        private double _sqm2 = 0;
        public double SQM2
        {
            get => _sqm2;
            private set => SetProperty(ref _sqm2, value);
        }

        private double _totalSQM = 0;
        public double TotalSQM
        {
            get => _totalSQM;
            private set => SetProperty(ref _totalSQM, value);
        }

        private double _totalAmount = 0;
        public double TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        private double _lm = 0;
        public double LM
        {
            get => _lm;
            private set => SetProperty(ref _lm, value);
        }

        private double _totalLM = 0;
        public double TotalLM
        {
            get => _totalLM;
            private set => SetProperty(ref _totalLM, value);
        }

        // ✅ LM1 = (W1 + H1) × 2 / 1000
        public double LM1
        {
            get
            {
                if (_width1 <= 0 || _height1 <= 0) return 0;
                double lm = ((_width1 + _height1) * 2) / 1000.0;
                return lm < 0.5 ? 0.5 : Math.Round(lm, 4);
            }
        }

        // ✅ LM2 = (W2 + H2) × 2 / 1000
        public double LM2
        {
            get
            {
                if (_width2 <= 0 || _height2 <= 0) return 0;
                double lm = ((_width2 + _height2) * 2) / 1000.0;
                return lm < 0.5 ? 0.5 : Math.Round(lm, 4);
            }
        }

        private double _totalPrice = 0;
        public double TotalPrice
        {
            get => _totalPrice;
            private set => SetProperty(ref _totalPrice, value);
        }

        // ==================== CALCULATE ALL ====================
        public void CalculateAll()
        {
            // ✅ SQM1 = W1×H1 (Glass 1 area)
            double glass1SQM = CalculateSingleSQM(_width1, _height1);
            SQM1 = Math.Round(glass1SQM, 4);

            // ✅ SQM2 = W2×H2 (Glass 2 area - overlap)
            double glass2SQM = CalculateSingleSQM(_width2, _height2);
            SQM2 = Math.Round(glass2SQM, 4);

            // ✅ TotalSQM = (SQM1 + SQM2) × Qty
            TotalSQM = Math.Round((glass1SQM + glass2SQM) * _qty, 4);

            // ✅ LM = Glass 1 perimeter only (W1×H1) - for Invoice Summary
            double lm1 = CalculateSingleLM(_width1, _height1);

            LM = Math.Round(lm1, 4);
            TotalLM = Math.Round(lm1 * _qty, 4);

            // ✅ Price = BasePrice + Surcharge ONLY when W1×H1 >= 4 sqm
            double unitPrice = Math.Round(Price, 2);

            // ✅ TotalPrice = UnitPrice × TotalSQM
            TotalPrice = Math.Round(unitPrice * TotalSQM, 2);
            TotalAmount = TotalPrice;

            // ✅ Notify price properties
            OnPropertyChanged(nameof(Price));
            OnPropertyChanged(nameof(UnitPrice));
            OnPropertyChanged(nameof(DisplayPrice));
            OnPropertyChanged(nameof(TotalPrice));
            OnPropertyChanged(nameof(HasSurcharge));
            OnPropertyChanged(nameof(LM1));
            OnPropertyChanged(nameof(LM2));
        }

        private double CalculateSingleSQM(double width, double height)
        {
            if (width <= 0 || height <= 0) return 0;
            double sqm = (width * height) / 1_000_000.0;
            return sqm < 0.5 ? 0.5 : sqm;
        }

        private double CalculateSingleLM(double width, double height)
        {
            if (width <= 0 || height <= 0) return 0;
            double lm = ((width + height) * 2) / 1000.0;
            return lm < 0.5 ? 0.5 : lm;
        }

        public void NotifySurchargeChanged()
        {
            OnPropertyChanged(nameof(SurchargePercent));
            OnPropertyChanged(nameof(Price));
            OnPropertyChanged(nameof(BasePrice));
            OnPropertyChanged(nameof(UnitPrice));
            OnPropertyChanged(nameof(DisplayPrice));
            CalculateAll();
        }

        public void CalculateSQM() => CalculateAll();
        public void CalculateTotalPrice() => CalculateAll();

        public InvoiceItemModel()
        {
            _qty = 1;
            CalculateAll();
        }
    }
}