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
            set => SetProperty(ref _qty, Math.Max(1, value));
        }

        private double _sqm = 0;
        public double SQM
        {
            get => _sqm;
            set
            {
                if (_sqm != value)
                {
                    _sqm = value;
                    OnPropertyChanged(nameof(SQM));
                }
            }
        }

        private double _totalSQM = 0;
        public double TotalSQM
        {
            get => _totalSQM;
            set
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
            set
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
            set
            {
                if (_totalLM != value)
                {
                    _totalLM = value;
                    OnPropertyChanged(nameof(TotalLM));
                }
            }
        }

        private double _price = 0;
        public double Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        private double _totalPrice = 0;
        public double TotalPrice
        {
            get => _totalPrice;
            set
            {
                if (_totalPrice != value)
                {
                    _totalPrice = value;
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        private void CalculateAll()
        {
            // Calculate SQM: Average of (W1+H1) and (W2+H2) / 2
            double avgWidth = (_width1 + _width2) / 2.0;
            double avgHeight = (_height1 + _height2) / 2.0;

            _sqm = Math.Round((avgWidth * avgHeight) / 1000000.0, 4);
            _totalSQM = Math.Round(_sqm * _qty, 4);

            // Calculate LM
            double perimeter = _width1 + _width2 + _height1 + _height2;
            _lm = Math.Round(perimeter * 2.0 / 1000.0, 4);
            _totalLM = Math.Round(_lm * _qty, 4);

            // Calculate Total Price
            _totalPrice = Math.Round(_totalSQM * _price, 2);

            OnPropertyChanged(nameof(SQM));
            OnPropertyChanged(nameof(TotalSQM));
            OnPropertyChanged(nameof(LM));
            OnPropertyChanged(nameof(TotalLM));
            OnPropertyChanged(nameof(TotalPrice));
        }

        public InvoiceItemModel()
        {
            _qty = 1;
            CalculateAll();
        }
    }
}