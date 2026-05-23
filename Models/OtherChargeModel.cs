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

        private string _type = "polish";  // lm, sqm, qty, polish, mitring, silicon, holes, fanhole, cutout, overlap, argon, amount
        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsHoleOrCutout));
                OnPropertyChanged(nameof(TypeDisplay));
                OnPropertyChanged(nameof(ValueDisplay));
                OnPropertyChanged(nameof(UnitDisplay));
                OnPropertyChanged(nameof(IsLMType));
            }
        }

        private double _value = 0;
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

        // ==================== DISPLAY PROPERTIES ====================

        // Value display with unit based on type
        public string ValueDisplay
        {
            get
            {
                return Type?.ToLower() switch
                {
                    "lm" or "polish" or "mitring" or "silicon" => $"{Value:F2} LM",
                    "sqm" => $"{Value:F2} SQM",
                    "qty" or "holes" or "cutout" or "overlap" => $"{Value:F0} pcs",
                    "fanhole" => $"{Value:F0} pcs (2X)",
                    "argon" => $"{Value:F0} %",
                    "amount" => "—",
                    _ => $"{Value:F2}"
                };
            }
        }

        // Unit display for Rate column
        public string UnitDisplay
        {
            get
            {
                return Type?.ToLower() switch
                {
                    "lm" or "polish" or "mitring" or "silicon" => "AED/LM",
                    "sqm" => "AED/SQM",
                    "qty" or "holes" or "cutout" or "overlap" or "fanhole" => "AED/pc",
                    "argon" => "AED/%",
                    "amount" => "AED",
                    _ => "AED"
                };
            }
        }

        // Rate display
        public string RateDisplay => Rate == 0 ? "0" : $"{Rate:F2}";

        // Amount display with currency
        public string AmountDisplay => $"AED {Amount:N2}";

        // Type display (friendly name)
        public string TypeDisplay
        {
            get
            {
                return Type?.ToLower() switch
                {
                    "lm" => "LM",
                    "sqm" => "SQM",
                    "qty" => "Qty",
                    "polish" => "Polish",
                    "mitring" => "Mitring",
                    "silicon" => "Silicon Bed",
                    "holes" => "Holes",
                    "fanhole" => "Fan Hole",
                    "cutout" => "Cutout",
                    "overlap" => "Overlap",
                    "argon" => "Argon Gas",
                    "amount" => "Fixed",
                    _ => Type?.ToUpper() ?? ""
                };
            }
        }

        public bool IsHoleOrCutout => Type == "holes" || Type == "fanhole" || Type == "cutout" || Type == "overlap";
        public bool IsLMType => Type == "lm" || Type == "sqm" || Type == "polish" || Type == "mitring" || Type == "silicon";

        // For LM type - which dimension to use
        private string _lmDimType = "w1h1";
        public string LmDimType
        {
            get => _lmDimType;
            set { _lmDimType = value; OnPropertyChanged(); }
        }

        // ==================== SPEC TARGETING ====================

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

        // Multi-spec support - comma-separated indices (e.g., "0,1,2")
        private string _linkedSpecIndices = "";
        public string LinkedSpecIndices
        {
            get => _linkedSpecIndices;
            set { _linkedSpecIndices = value; OnPropertyChanged(); OnPropertyChanged(nameof(LinkedSpecsDisplay)); }
        }

        // Display text for linked specs
        public string LinkedSpecsDisplay
        {
            get
            {
                if (string.IsNullOrEmpty(LinkedSpecIndices))
                    return "Single Spec";

                var indices = LinkedSpecIndices.Split(',')
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => $"Spec {int.Parse(s.Trim()) + 1}");

                return string.Join(", ", indices);
            }
        }

        private void CalculateAmount()
        {
            Amount = Math.Round(Value * Rate, 2);
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
                LinkedSpecIndex = LinkedSpecIndex,
                LinkedSpecIndices = LinkedSpecIndices
            };
        }
    }
}