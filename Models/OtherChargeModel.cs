using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

        // ==================== BASIC PROPERTIES ====================

        private string _name = "New Charge";
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        private string _type = "lm";
        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TypeDisplay));
                OnPropertyChanged(nameof(ValueDisplay));
                OnPropertyChanged(nameof(UnitDisplay));
                OnPropertyChanged(nameof(IsLMBased));
                OnPropertyChanged(nameof(IsHoleType));
            }
        }

        private double _value = 0;
        public double Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); OnPropertyChanged(nameof(ValueDisplay)); CalculateAmount(); }
        }

        private double _rate = 0;
        public double Rate
        {
            get => _rate;
            set { _rate = value; OnPropertyChanged(); OnPropertyChanged(nameof(RateDisplay)); CalculateAmount(); }
        }

        private double _amount = 0;
        public double Amount
        {
            get => _amount;
            set { _amount = value; OnPropertyChanged(); OnPropertyChanged(nameof(AmountDisplay)); }
        }

        private string _lmDimType = "w1h1";
        public string LmDimType
        {
            get => _lmDimType;
            set { _lmDimType = value; OnPropertyChanged(); }
        }

        // ==================== DISPLAY PROPERTIES ====================

        public string ValueDisplay
        {
            get
            {
                return Type?.ToLower() switch
                {
                    "lm" => $"{Value:F2} LM",
                    "sqm" => $"{Value:F2} SQM",
                    "sqm1" => $"{Value:F2} SQM1",
                    "sqm2" => $"{Value:F2} SQM2",
                    "qty" => $"{Value:F0} pcs",
                    "1x" => $"{Value:F0} pcs",
                    "2x" => $"{Value:F0} pcs (2X)",
                    _ => $"{Value:F2}"
                };
            }
        }

        public string UnitDisplay
        {
            get
            {
                return Type?.ToLower() switch
                {
                    "lm" => "AED/LM",
                    "sqm" or "sqm1" or "sqm2" => "AED/SQM",
                    "qty" or "1x" or "2x" => "AED/pc",
                    _ => "AED"
                };
            }
        }

        public string RateDisplay => Rate == 0 ? "0" : $"{Rate:F2}";
        public string AmountDisplay => $"AED {Amount:N2}";

        public string TypeDisplay
        {
            get
            {
                return Type?.ToLower() switch
                {
                    "lm" => "LM",
                    "sqm" => "SQM",
                    "sqm1" => "SQM1",
                    "sqm2" => "SQM2",
                    "qty" => "QTY",
                    "1x" => "1X",
                    "2x" => "2X",
                    _ => Type?.ToUpper() ?? ""
                };
            }
        }

        public bool IsLMBased => Type == "lm" || Type == "sqm" || Type == "sqm1" || Type == "sqm2";
        public bool IsHoleType => Type == "1x" || Type == "2x";

        // ==================== CALCULATE AMOUNT ====================

        private void CalculateAmount()
        {
            Amount = Math.Round(Value * Rate, 2);
        }

        // ==================== SPEC TARGETING ====================

        private int _linkedSpecIndex = -1;
        public int LinkedSpecIndex
        {
            get => _linkedSpecIndex;
            set { _linkedSpecIndex = value; OnPropertyChanged(); }
        }

        private string _linkedSpecIndices = "";
        public string LinkedSpecIndices
        {
            get => _linkedSpecIndices;
            set
            {
                _linkedSpecIndices = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LinkedSpecsDisplay));
                OnPropertyChanged(nameof(TargetsAllSpecs));
            }
        }

        private bool _targetsAllSpecs = true;
        public bool TargetsAllSpecs
        {
            get => _targetsAllSpecs;
            set
            {
                if (_targetsAllSpecs != value)
                {
                    _targetsAllSpecs = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LinkedSpecsDisplay));
                    if (value)
                    {
                        LinkedSpecIndices = "";
                    }
                }
            }
        }

        public string LinkedSpecsDisplay
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_linkedSpecIndices))
                    return "All Specs";

                var indices = SpecIndexList;
                if (indices.Count == 0)
                    return "All Specs";

                return string.Join(" + ", indices.Select(i => $"Spec {i + 1}"));
            }
        }

        public List<int> SpecIndexList
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_linkedSpecIndices))
                    return new List<int>();

                return _linkedSpecIndices
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out int i) ? i : -1)
                    .Where(i => i >= 0)
                    .ToList();
            }
        }

        // ==================== AUTO VALUE ====================

        private List<SpecificationModel> _boundSpecs = new List<SpecificationModel>();
        public List<SpecificationModel> BoundSpecs
        {
            get => _boundSpecs;
            set { _boundSpecs = value ?? new List<SpecificationModel>(); }
        }

        private bool _isManualOverride = false;
        public bool IsManualOverride
        {
            get => _isManualOverride;
            set { _isManualOverride = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsAutoMode)); }
        }

        public bool IsAutoMode => !_isManualOverride;

        // ==================== SPEC INDEX MANAGEMENT ====================

        public void AddSpec(int index)
        {
            var indices = SpecIndexList;
            if (!indices.Contains(index))
            {
                indices.Add(index);
                indices.Sort();
                LinkedSpecIndices = string.Join(",", indices);
            }
        }

        public void RemoveSpec(int index)
        {
            var indices = SpecIndexList;
            if (indices.Contains(index))
            {
                indices.Remove(index);
                LinkedSpecIndices = indices.Count > 0 ? string.Join(",", indices) : "";
            }
        }

        public void ToggleSpec(int index)
        {
            if (SpecIndexList.Contains(index))
                RemoveSpec(index);
            else
                AddSpec(index);
        }

        public void SetAllSpecs()
        {
            LinkedSpecIndices = "";
        }

        // ==================== CLONE ====================

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
                LinkedSpecIndices = LinkedSpecIndices,
                IsManualOverride = IsManualOverride
            };
        }
    }
}