using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProGlassAutomation.Models
{
    public class InvoiceItemModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            CalculateAll();
            return true;
        }

        private int _srNo = 1;
        public int SrNo
        {
            get => _srNo;
            set => SetProperty(ref _srNo, value);
        }

        private string _glassRef = "";
        public string GlassRef
        {
            get => _glassRef;
            set => SetProperty(ref _glassRef, value);
        }

        private double _width1 = 0;
        public double Width1
        {
            get => _width1;
            set => SetProperty(ref _width1, value);
        }

        private double _height1 = 0;
        public double Height1
        {
            get => _height1;
            set => SetProperty(ref _height1, value);
        }

        private double _width2 = 0;
        public double Width2
        {
            get => _width2;
            set => SetProperty(ref _width2, value);
        }

        private double _height2 = 0;
        public double Height2
        {
            get => _height2;
            set => SetProperty(ref _height2, value);
        }

        private int _qty = 1;
        public int Qty
        {
            get => _qty;
            set => SetProperty(ref _qty, value);
        }

        private double _price = 0;
        public double Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        public double BasePrice
        {
            get => _price;
        }

        private double _surchargePercent = 20;
        public double SurchargePercent
        {
            get => _surchargePercent;
            set
            {
                if (SetProperty(ref _surchargePercent, value))
                {
                    OnPropertyChanged(nameof(DisplayPrice));
                }
            }
        }

        // Fixed threshold at 4 SQM
        public double SurchargeThreshold => 4;

        public double DisplayPrice
        {
            get
            {
                if (SQM >= 4 && _surchargePercent > 0)
                {
                    return Math.Round(_price * (1 + _surchargePercent / 100), 2);
                }
                return _price;
            }
        }

        private double _sqm = 0;
        public double SQM
        {
            get => _sqm;
            private set
            {
                if (_sqm != value)
                {
                    _sqm = value;
                    OnPropertyChanged(nameof(SQM));
                    OnPropertyChanged(nameof(DisplayPrice));
                }
            }
        }

        private double _totalSQM = 0;
        public double TotalSQM
        {
            get => _totalSQM;
            private set
            {
                if (_totalSQM != value)
                {
                    _totalSQM = value;
                    OnPropertyChanged(nameof(TotalSQM));
                }
            }
        }

        private double _lm = 0;
        public double LM
        {
            get => _lm;
            private set
            {
                if (_lm != value)
                {
                    _lm = value;
                    OnPropertyChanged(nameof(LM));
                }
            }
        }

        private double _totalLM = 0;
        public double TotalLM
        {
            get => _totalLM;
            private set
            {
                if (_totalLM != value)
                {
                    _totalLM = value;
                    OnPropertyChanged(nameof(TotalLM));
                }
            }
        }

        private double _totalPrice = 0;
        public double TotalPrice
        {
            get => _totalPrice;
            private set
            {
                if (_totalPrice != value)
                {
                    _totalPrice = value;
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        public bool HasSurcharge
        {
            get => SQM >= 4 && _surchargePercent > 0;
        }

        public double FinalPrice
        {
            get
            {
                if (SQM >= 4 && _surchargePercent > 0)
                {
                    return Math.Round(_price * (1 + _surchargePercent / 100), 2);
                }
                return _price;
            }
        }

        private void CalculateAll()
        {
            // Calculate SQM (Square Meters)
            double sqm = 0;
            if (_width1 > 0 && _height1 > 0)
            {
                sqm = (_width1 * _height1) / 1000000.0;
                if (sqm < 0.5) sqm = 0.5;
            }
            if (_width2 > 0 && _height2 > 0)
            {
                double sqm2 = (_width2 * _height2) / 1000000.0;
                if (sqm2 < 0.5) sqm2 = 0.5;
                sqm += sqm2;
            }
            SQM = Math.Round(sqm, 4);
            TotalSQM = Math.Round(SQM * _qty, 4);

            // Calculate LM (Linear Meters)
            double lm = 0;
            if (_width1 > 0 && _height1 > 0)
            {
                lm = ((_width1 + _height1) * 2) / 1000.0;
                if (lm < 0.5) lm = 0.5;
            }
            if (_width2 > 0 && _height2 > 0)
            {
                double lm2 = ((_width2 + _height2) * 2) / 1000.0;
                if (lm2 < 0.5) lm2 = 0.5;
                lm += lm2;
            }
            LM = Math.Round(lm, 4);
            TotalLM = Math.Round(LM * _qty, 4);

            // Calculate Total Price
            TotalPrice = Math.Round(FinalPrice * TotalSQM, 2);

            OnPropertyChanged(nameof(HasSurcharge));
            OnPropertyChanged(nameof(FinalPrice));
            OnPropertyChanged(nameof(DisplayPrice));
        }

        public InvoiceItemModel()
        {
            _qty = 1;
            CalculateAll();
        }
    }
}