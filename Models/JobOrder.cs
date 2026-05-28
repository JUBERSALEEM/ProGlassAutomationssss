using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ProGlassAutomation.Models
{
    public class JobOrder : INotifyPropertyChanged
    {
        private int _id;
        private string _jobNumber = "";
        private string _piNumber = "";
        private string _customerName = "";
        private string _customerTRN = "";
        private string _customerAddress = "";
        private string _projectName = "";
        private string _projectLocation = "";
        private string _lpoNumber = "";
        private DateTime _date = DateTime.Now;
        private DateTime _requiredDate = DateTime.Now.AddDays(7);
        private DateTime _createdDate = DateTime.Now;
        private string _status = "Pending";
        private string _color = "";
        private string _salesman = "";
        private string _customerReference = "";
        private string _notes = "";
        private bool _isConvertedToDelivery;
        private int _totalQty;
        private int _releasedQty;
        private int _balanceQty;
        private double _totalSQM;
        private double _totalAmount;
        private ObservableCollection<JobOrderSpecification> _specifications = new ObservableCollection<JobOrderSpecification>();
        private string _specificationsJson = "";

        public int Id { get => _id; set { _id = value; OnPropertyChanged(); } }
        public string JobNumber { get => _jobNumber; set { _jobNumber = value ?? ""; OnPropertyChanged(); } }
        public string PINumber { get => _piNumber; set { _piNumber = value ?? ""; OnPropertyChanged(); } }
        public string CustomerName { get => _customerName; set { _customerName = value ?? ""; OnPropertyChanged(); } }
        public string CustomerTRN { get => _customerTRN; set { _customerTRN = value ?? ""; OnPropertyChanged(); } }
        public string CustomerAddress { get => _customerAddress; set { _customerAddress = value ?? ""; OnPropertyChanged(); } }
        public string ProjectName { get => _projectName; set { _projectName = value ?? ""; OnPropertyChanged(); } }
        public string ProjectLocation { get => _projectLocation; set { _projectLocation = value ?? ""; OnPropertyChanged(); } }
        public string LPONumber { get => _lpoNumber; set { _lpoNumber = value ?? ""; OnPropertyChanged(); } }
        public DateTime Date { get => _date; set { _date = value; OnPropertyChanged(); } }
        public DateTime RequiredDate { get => _requiredDate; set { _requiredDate = value; OnPropertyChanged(); } }
        public DateTime CreatedDate { get => _createdDate; set { _createdDate = value; OnPropertyChanged(); } }
        public string Status { get => _status; set { _status = value ?? ""; OnPropertyChanged(); } }
        public string Color { get => _color; set { _color = value ?? ""; OnPropertyChanged(); } }
        public string Salesman { get => _salesman; set { _salesman = value ?? ""; OnPropertyChanged(); } }
        public string CustomerReference { get => _customerReference; set { _customerReference = value ?? ""; OnPropertyChanged(); } }
        public string Notes { get => _notes; set { _notes = value ?? ""; OnPropertyChanged(); } }
        public bool IsConvertedToDelivery { get => _isConvertedToDelivery; set { _isConvertedToDelivery = value; OnPropertyChanged(); } }
        public int TotalQty { get => _totalQty; set { _totalQty = value; OnPropertyChanged(); } }
        public int ReleasedQty { get => _releasedQty; set { _releasedQty = value; OnPropertyChanged(); } }
        public int BalanceQty { get => _balanceQty; set { _balanceQty = value; OnPropertyChanged(); } }
        public double TotalSQM { get => _totalSQM; set { _totalSQM = value; OnPropertyChanged(); } }
        public double TotalAmount { get => _totalAmount; set { _totalAmount = value; OnPropertyChanged(); } }
        public ObservableCollection<JobOrderSpecification> Specifications { get => _specifications; set { _specifications = value; OnPropertyChanged(); } }
        public string SpecificationsJson { get => _specificationsJson; set { _specificationsJson = value ?? ""; OnPropertyChanged(); } }

        public void CalculateTotals()
        {
            TotalQty = Specifications?.Sum(s => s.TotalQty) ?? 0;
            ReleasedQty = Specifications?.Sum(s => s.Items.Sum(i => i.DeliveredQty)) ?? 0;
            BalanceQty = TotalQty - ReleasedQty;
            TotalSQM = Specifications?.Sum(s => s.TotalSQM) ?? 0;
            TotalAmount = Specifications?.Sum(s => s.TotalAmount) ?? 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class JobOrderSpecification : INotifyPropertyChanged
    {
        private int _id;
        private string _specificationName = "";
        private string _moduleType = "SGU";
        private string _workType = "Annealed";
        private string _outerThickness = "6mm";
        private string _outerColor = "Clear";
        private string _innerThickness = "6mm";
        private string _innerColor = "Clear";
        private string _spacerThickness = "12mm";
        private string _pvbThickness = "0.76mm";
        private string _pvbColor = "Clear";
        private bool _includeInSpec = true;
        private double _basePrice = 0;
        private double _surchargePercent = 20;
        private ObservableCollection<JobOrderItem> _items = new ObservableCollection<JobOrderItem>();
        private ObservableCollection<JobOrderOtherCharge> _otherCharges = new ObservableCollection<JobOrderOtherCharge>();

        public JobOrderSpecification()
        {
            _items.CollectionChanged += (s, e) => CalculateTotals();
            _otherCharges.CollectionChanged += (s, e) => CalculateTotals();
        }

        public int Id { get => _id; set { _id = value; OnPropertyChanged(); } }
        public string SpecificationName { get => _specificationName; set { _specificationName = value ?? ""; OnPropertyChanged(); } }
        public string ModuleType { get => _moduleType; set { _moduleType = value ?? ""; OnPropertyChanged(); } }
        public string WorkType { get => _workType; set { _workType = value ?? ""; OnPropertyChanged(); } }
        public string OuterThickness { get => _outerThickness; set { _outerThickness = value ?? ""; OnPropertyChanged(); } }
        public string OuterColor { get => _outerColor; set { _outerColor = value ?? ""; OnPropertyChanged(); } }
        public string InnerThickness { get => _innerThickness; set { _innerThickness = value ?? ""; OnPropertyChanged(); } }
        public string InnerColor { get => _innerColor; set { _innerColor = value ?? ""; OnPropertyChanged(); } }
        public string SpacerThickness { get => _spacerThickness; set { _spacerThickness = value ?? ""; OnPropertyChanged(); } }
        public string PVBThickness { get => _pvbThickness; set { _pvbThickness = value ?? ""; OnPropertyChanged(); } }
        public string PVBColor { get => _pvbColor; set { _pvbColor = value ?? ""; OnPropertyChanged(); } }
        public bool IncludeInSpec { get => _includeInSpec; set { _includeInSpec = value; OnPropertyChanged(); } }
        public double BasePrice { get => _basePrice; set { _basePrice = value; OnPropertyChanged(); } }
        public double SurchargePercent { get => _surchargePercent; set { _surchargePercent = value; OnPropertyChanged(); } }

        public ObservableCollection<JobOrderItem> Items
        {
            get => _items;
            set { _items = value; OnPropertyChanged(); }
        }

        public ObservableCollection<JobOrderOtherCharge> OtherCharges
        {
            get => _otherCharges;
            set { _otherCharges = value; OnPropertyChanged(); }
        }

        // Calculated properties
        public int TotalItems => Items?.Count ?? 0;
        public int TotalQty => Items?.Sum(i => i.Qty) ?? 0;
        public double TotalSQM1 => Items?.Sum(i => i.SQM1 * i.Qty) ?? 0;
        public double TotalSQM2 => Items?.Sum(i => i.SQM2 * i.Qty) ?? 0;
        public double TotalSQM => Items?.Sum(i => i.TotalSQM) ?? 0;
        public double TotalLM1 => Items?.Sum(i => i.LM1 * i.Qty) ?? 0;
        public double TotalLM2 => Items?.Sum(i => i.LM2 * i.Qty) ?? 0;
        public double OtherChargesTotal => OtherCharges?.Sum(c => c.Amount) ?? 0;
        public double TotalAmount => (Items?.Sum(i => i.TotalAmount) ?? 0) + OtherChargesTotal;

        public void CalculateTotals()
        {
            OnPropertyChanged(nameof(TotalItems));
            OnPropertyChanged(nameof(TotalQty));
            OnPropertyChanged(nameof(TotalSQM1));
            OnPropertyChanged(nameof(TotalSQM2));
            OnPropertyChanged(nameof(TotalSQM));
            OnPropertyChanged(nameof(TotalLM1));
            OnPropertyChanged(nameof(TotalLM2));
            OnPropertyChanged(nameof(OtherChargesTotal));
            OnPropertyChanged(nameof(TotalAmount));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class JobOrderItem : INotifyPropertyChanged
    {
        private int _id;
        private int _srNo = 1;
        private string _glassRef = "";
        private double _width1;
        private double _height1;
        private double _width2;
        private double _height2;
        private int _qty = 1;
        private int _deliveredQty;
        private double _price;
        private double _sqm1;
        private double _sqm2;
        private double _lm1;
        private double _lm2;

        public JobOrderItem()
        {
            Recalculate();
        }

        public int Id { get => _id; set { _id = value; OnPropertyChanged(); } }
        public int SrNo { get => _srNo; set { _srNo = value; OnPropertyChanged(); } }
        public string GlassRef { get => _glassRef; set { _glassRef = value ?? ""; OnPropertyChanged(); } }

        public double Width1
        {
            get => _width1;
            set { _width1 = value; OnPropertyChanged(); Recalculate(); }
        }

        public double Height1
        {
            get => _height1;
            set { _height1 = value; OnPropertyChanged(); Recalculate(); }
        }

        public double Width2
        {
            get => _width2;
            set { _width2 = value; OnPropertyChanged(); Recalculate(); }
        }

        public double Height2
        {
            get => _height2;
            set { _height2 = value; OnPropertyChanged(); Recalculate(); }
        }

        public int Qty
        {
            get => _qty;
            set { _qty = value; OnPropertyChanged(); Recalculate(); OnPropertyChanged(nameof(RemainingQty)); }
        }

        public int DeliveredQty
        {
            get => _deliveredQty;
            set { _deliveredQty = value; OnPropertyChanged(); OnPropertyChanged(nameof(RemainingQty)); }
        }

        public int RemainingQty => Qty - DeliveredQty;

        public double Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(); Recalculate(); }
        }

        public double SQM1
        {
            get => _sqm1;
            private set { _sqm1 = value; OnPropertyChanged(); }
        }

        public double SQM2
        {
            get => _sqm2;
            private set { _sqm2 = value; OnPropertyChanged(); }
        }

        public double TotalSQM => Math.Round((_sqm1 + _sqm2) * Qty, 4);

        public double LM1
        {
            get => _lm1;
            private set { _lm1 = value; OnPropertyChanged(); }
        }

        public double LM2
        {
            get => _lm2;
            private set { _lm2 = value; OnPropertyChanged(); }
        }

        public double TotalLM => Math.Round((_lm1 + _lm2) * Qty, 4);

        public double TotalAmount => Math.Round(TotalSQM * _price, 2);

        public void Recalculate()
        {
            _sqm1 = CalculateSQM(_width1, _height1);
            SQM1 = _sqm1;

            _sqm2 = CalculateSQM(_width2, _height2);
            SQM2 = _sqm2;

            _lm1 = CalculateLM(_width1, _height1);
            LM1 = _lm1;

            _lm2 = CalculateLM(_width2, _height2);
            LM2 = _lm2;

            OnPropertyChanged(nameof(TotalSQM));
            OnPropertyChanged(nameof(TotalLM));
            OnPropertyChanged(nameof(TotalAmount));
        }

        private double CalculateSQM(double width, double height)
        {
            if (width <= 0 || height <= 0) return 0;
            double sqm = (width * height) / 1_000_000.0;
            return sqm < 0.5 ? 0.5 : Math.Round(sqm, 4);
        }

        private double CalculateLM(double width, double height)
        {
            if (width <= 0 || height <= 0) return 0;
            double lm = ((width + height) * 2) / 1000.0;
            return lm < 0.5 ? 0.5 : Math.Round(lm, 4);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class JobOrderOtherCharge : INotifyPropertyChanged
    {
        private string _name = "New Charge";
        private string _type = "lm";
        private double _value = 0;
        private double _rate = 0;
        private double _amount = 0;
        private string _lmDimType = "w1h1";
        private int _linkedSpecIndex = -1;
        private string _linkedSpecIndices = "";
        private bool _targetsAllSpecs = true;
        private bool _isManualOverride = false;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string Name
        {
            get => _name;
            set { _name = value ?? ""; OnPropertyChanged(); }
        }

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

        public double Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); OnPropertyChanged(nameof(ValueDisplay)); CalculateAmount(); }
        }

        public double Rate
        {
            get => _rate;
            set { _rate = value; OnPropertyChanged(); OnPropertyChanged(nameof(RateDisplay)); CalculateAmount(); }
        }

        public double Amount
        {
            get => _amount;
            set { _amount = value; OnPropertyChanged(); OnPropertyChanged(nameof(AmountDisplay)); }
        }

        public string LmDimType
        {
            get => _lmDimType;
            set { _lmDimType = value ?? "w1h1"; OnPropertyChanged(); }
        }

        public int LinkedSpecIndex
        {
            get => _linkedSpecIndex;
            set { _linkedSpecIndex = value; OnPropertyChanged(); }
        }

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

        public bool IsManualOverride
        {
            get => _isManualOverride;
            set { _isManualOverride = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsAutoMode)); }
        }

        public bool IsAutoMode => !_isManualOverride;

        private void CalculateAmount()
        {
            Amount = Math.Round(_value * _rate, 2);
        }

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

        public JobOrderOtherCharge Clone()
        {
            return new JobOrderOtherCharge
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