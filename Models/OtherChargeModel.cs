using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProGlassAutomation.Models
{
    public class OtherChargeModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _name = "New Charge";
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        private string _type = "amount";  // amount, lm, sqm, qty, holes, cutout
        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsHoleOrCutout));
                OnPropertyChanged(nameof(TypeDisplay));
                OnPropertyChanged(nameof(Value));
            }
        }

        private double _value = 1;
        public double Value
        {
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ValueDisplay));
                CalculateAmount();
            }
        }

        private double _rate = 0;
        public double Rate
        {
            get => _rate;
            set
            {
                _rate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RateDisplay));
                CalculateAmount();
            }
        }

        // CHANGED: Made setter public so ViewModel can set Amount
        private double _amount = 0;
        public double Amount
        {
            get => _amount;
            set
            {
                _amount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AmountDisplay));
            }
        }

        // Display properties without unnecessary decimals
        public string ValueDisplay => FormatNumber(Value);
        public string RateDisplay => FormatNumber(Rate);
        public string AmountDisplay => FormatNumber(Amount);

        public bool IsHoleOrCutout => Type == "holes" || Type == "cutout";
        public string TypeDisplay => IsHoleOrCutout ? $"{Type.ToUpper()} (2X)" : Type.ToUpper();

        // For LM type - which dimension to use
        private string _lmDimType = "w1h1";
        public string LmDimType
        {
            get => _lmDimType;
            set { _lmDimType = value; OnPropertyChanged(); }
        }

        // Linked section indices (for multi-section calculation)
        private string _linkedSectionIndices = "";  // Comma-separated: "0,1,2"
        public string LinkedSectionIndices
        {
            get => _linkedSectionIndices;
            set { _linkedSectionIndices = value; OnPropertyChanged(); }
        }

        // Linked spec index for single spec
        private int _linkedSpecIndex = -1;
        public int LinkedSpecIndex
        {
            get => _linkedSpecIndex;
            set
            {
                _linkedSpecIndex = value;
                OnPropertyChanged();
            }
        }

        private void CalculateAmount()
        {
            Amount = Math.Round(Value * Rate, 2);
        }

        // Format number: 65 not 65.00, 65.5 not 65.50
        private string FormatNumber(double num)
        {
            if (num == 0) return "0";
            if (num % 1 == 0) return num.ToString("0");
            return num.ToString("0.####").TrimEnd('0').TrimEnd('.');
        }

        public OtherChargeModel Clone()
        {
            return new OtherChargeModel
            {
                Name = Name,
                Type = Type,
                Value = Value,
                Rate = Rate,
                Amount = Amount,
                LmDimType = LmDimType,
                LinkedSectionIndices = LinkedSectionIndices,
                LinkedSpecIndex = LinkedSpecIndex
            };
        }
    }
}