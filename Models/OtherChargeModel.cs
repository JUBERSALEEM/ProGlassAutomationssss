using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
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

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // ==================== BASIC PROPERTIES ====================

        private string _name = "New Charge";
        public string Name
        {
            get => _name;
            set { _name = value ?? ""; OnPropertyChanged(); }
        }

        private string _type = "lm";
        public string Type
        {
            get => _type;
            set
            {
                _type = value ?? "lm";
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
            set
            {
                if (SetProperty(ref _value, value))
                {
                    OnPropertyChanged(nameof(ValueDisplay));
                    CalculateAmount();
                }
            }
        }

        private double _rate = 0;
        public double Rate
        {
            get => _rate;
            set
            {
                if (SetProperty(ref _rate, value))
                {
                    OnPropertyChanged(nameof(RateDisplay));
                    CalculateAmount();
                }
            }
        }

        private double _amount = 0;
        public double Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);  // Made public
        }

        private string _lmDimType = "w1h1";
        public string LmDimType
        {
            get => _lmDimType;
            set => SetProperty(ref _lmDimType, value ?? "w1h1");
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

        // ==================== CALCULATE AMOUNT (PATCH: Add PropertyChanged) ====================

        public void CalculateAmount()
        {
            Amount = Math.Round(Value * Rate, 2);
            // PATCH: Notify Amount changed for UI update
            OnPropertyChanged(nameof(Amount));
            OnPropertyChanged(nameof(AmountDisplay));
        }

        // ==================== SPEC TARGETING ====================

        private int _linkedSpecIndex = -1;
        public int LinkedSpecIndex
        {
            get => _linkedSpecIndex;
            set => SetProperty(ref _linkedSpecIndex, value);
        }

        private string _linkedSpecIndices = "";
        public string LinkedSpecIndices
        {
            get => _linkedSpecIndices;
            set
            {
                _linkedSpecIndices = value ?? "";
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
                        LinkedSpecIndices = "";
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

        [JsonIgnore]
        private List<SpecificationModel> _boundSpecs = new List<SpecificationModel>();
        [JsonIgnore]
        public List<SpecificationModel> BoundSpecs
        {
            get => _boundSpecs;
            set => _boundSpecs = value ?? new List<SpecificationModel>();
        }

        private bool _isManualOverride = false;
        public bool IsManualOverride
        {
            get => _isManualOverride;
            set
            {
                if (SetProperty(ref _isManualOverride, value))
                    OnPropertyChanged(nameof(IsAutoMode));
            }
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

        // ==================== CLONE (PATCH 15) ====================

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

        // PATCH 15: Deep clone for proper cloning
        public OtherChargeModel DeepClone()
        {
            var clone = new OtherChargeModel
            {
                Name = Name,
                Type = Type,
                Value = Value,
                Rate = Rate,
                LmDimType = LmDimType,
                LinkedSpecIndex = LinkedSpecIndex,
                LinkedSpecIndices = LinkedSpecIndices,
                IsManualOverride = IsManualOverride
            };

            // Amount will be recalculated from Value * Rate when used
            clone.CalculateAmount();
            return clone;
        }

        // PATCH 15: Copy values from another charge
        public void CopyFrom(OtherChargeModel other)
        {
            if (other == null) return;

            Name = other.Name;
            Type = other.Type;
            Value = other.Value;
            Rate = other.Rate;
            LmDimType = other.LmDimType;
            LinkedSpecIndex = other.LinkedSpecIndex;
            LinkedSpecIndices = other.LinkedSpecIndices;
            IsManualOverride = other.IsManualOverride;

            CalculateAmount();
        }
    }
}