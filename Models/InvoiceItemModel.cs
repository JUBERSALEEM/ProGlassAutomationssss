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
        public double Price
        {
            get => _price;
            set
            {
                if (SetProperty(ref _price, value))
                {
                    TriggerCalculationAsync();
                    OnPropertyChanged(nameof(BasePrice));
                    OnPropertyChanged(nameof(UnitPrice));
                    OnPropertyChanged(nameof(DisplayPrice));
                    OnPropertyChanged(nameof(FinalPrice));
                }
            }
        }

        public double BasePrice => _price;

        private double _surchargePercent = 20;
        public double SurchargePercent
        {
            get => _surchargePercent;
            set
            {
                if (value < 0 || value > 100)
                    throw new ArgumentOutOfRangeException(nameof(SurchargePercent), "Surcharge must be between 0 and 100.");
                SetProperty(ref _surchargePercent, value);
            }
        }

        public const double SurchargeThreshold = 4;
        private const double MinSQM = 0.5;
        private const double MinLM = 0.5;

        public bool HasSurcharge => SQM >= SurchargeThreshold && _surchargePercent > 0;

        public double UnitPrice => HasSurcharge
            ? Math.Round(_price * (1 + _surchargePercent / 100.0), 2)
            : _price;

        public double DisplayPrice => UnitPrice;
        public double FinalPrice => UnitPrice;

        private double _sqm = 0;
        public double SQM
        {
            get => _sqm;
            private set => SetProperty(ref _sqm, value);
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

        private double _totalPrice = 0;
        public double TotalPrice
        {
            get => _totalPrice;
            private set => SetProperty(ref _totalPrice, value);
        }

        public void CalculateAll()
        {
            double sqm1 = CalculateSingleSQM(_width1, _height1);
            double sqm2 = CalculateSingleSQM(_width2, _height2);
            double sqm = sqm1 + sqm2;

            SQM = Math.Round(sqm, 4);
            TotalSQM = Math.Round(SQM * _qty, 4);

            double lm1 = CalculateSingleLM(_width1, _height1);
            double lm2 = CalculateSingleLM(_width2, _height2);
            double lm = lm1 + lm2;

            LM = Math.Round(lm, 4);
            TotalLM = Math.Round(LM * _qty, 4);

            double unitPrice = HasSurcharge
                ? Math.Round(_price * (1 + _surchargePercent / 100.0), 2)
                : _price;

            TotalPrice = Math.Round(unitPrice * TotalSQM, 2);
            TotalAmount = TotalPrice;
        }

        private double CalculateSingleSQM(double width, double height)
        {
            if (width <= 0 || height <= 0) return 0;
            double sqm = (width * height) / 1_000_000.0;
            return sqm < MinSQM ? MinSQM : sqm;
        }

        private double CalculateSingleLM(double width, double height)
        {
            if (width <= 0 || height <= 0) return 0;
            double lm = ((width + height) * 2) / 1000.0;
            return lm < MinLM ? MinLM : lm;
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