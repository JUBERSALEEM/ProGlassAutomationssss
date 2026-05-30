using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace ProGlassAutomation.Models
{
    // ==================== JOB ORDER ====================
    public class JobOrder : INotifyPropertyChanged
    {
        private int _id;
        private string _jobNumber = "";
        private string _piNumber = "";
        private string _clientName = "";
        private string _clientTRN = "";
        private string _clientAddress = "";
        private string _projectName = "";
        private string _projectLocation = "";
        private string _lpoNumber = "";
        private DateTime _date = DateTime.Now;
        private DateTime _requiredDate = DateTime.Now.AddDays(7);
        private DateTime _createdDate = DateTime.Now;
        private string _status = "Pending";
        private string _color = "";
        private string _salesman = "";
        private string _clientReference = "";
        private string _notes = "";
        private bool _isConvertedToDelivery;
        private int _totalQty;
        private int _releasedQty;
        private int _balanceQty;
        private double _totalSQM;
        private double _totalAmount;
        private ObservableCollection<JobOrderSpecification> _specifications = new ObservableCollection<JobOrderSpecification>();
        private string _specificationsJson = "";

        public int Id { get => _id; set => SetField(ref _id, value); }
        public string JobNumber { get => _jobNumber; set => SetField(ref _jobNumber, value ?? ""); }
        public string PINumber { get => _piNumber; set => SetField(ref _piNumber, value ?? ""); }
        public string ClientName { get => _clientName; set => SetField(ref _clientName, value ?? ""); }
        public string ClientTRN { get => _clientTRN; set => SetField(ref _clientTRN, value ?? ""); }
        public string ClientAddress { get => _clientAddress; set => SetField(ref _clientAddress, value ?? ""); }
        public string ProjectName { get => _projectName; set => SetField(ref _projectName, value ?? ""); }
        public string ProjectLocation { get => _projectLocation; set => SetField(ref _projectLocation, value ?? ""); }
        private string _projectNo = "";
        public string ProjectNo { get => _projectNo; set => SetField(ref _projectNo, value ?? ""); }
        public string LPONumber { get => _lpoNumber; set => SetField(ref _lpoNumber, value ?? ""); }
        public DateTime Date { get => _date; set => SetField(ref _date, value); }
        public DateTime RequiredDate { get => _requiredDate; set => SetField(ref _requiredDate, value); }
        public DateTime CreatedDate { get => _createdDate; set => SetField(ref _createdDate, value); }
        public string Status { get => _status; set => SetField(ref _status, value ?? ""); }
        public string Color { get => _color; set => SetField(ref _color, value ?? ""); }
        public string Salesman { get => _salesman; set => SetField(ref _salesman, value ?? ""); }
        public string ClientReference { get => _clientReference; set => SetField(ref _clientReference, value ?? ""); }
        public string Notes { get => _notes; set => SetField(ref _notes, value ?? ""); }
        public bool IsConvertedToDelivery { get => _isConvertedToDelivery; set => SetField(ref _isConvertedToDelivery, value); }
        public int TotalQty { get => _totalQty; set => SetField(ref _totalQty, value); }
        public int ReleasedQty { get => _releasedQty; set => SetField(ref _releasedQty, value); }
        public int BalanceQty { get => _balanceQty; set => SetField(ref _balanceQty, value); }
        public double TotalSQM { get => _totalSQM; set => SetField(ref _totalSQM, value); }
        public double TotalAmount { get => _totalAmount; set => SetField(ref _totalAmount, value); }
        public ObservableCollection<JobOrderSpecification> Specifications { get => _specifications; set => SetField(ref _specifications, value); }
        public string SpecificationsJson { get => _specificationsJson; set => SetField(ref _specificationsJson, value ?? ""); }

        public void CalculateTotals()
        {
            TotalQty = Specifications?.Sum(s => s.TotalQty) ?? 0;
            ReleasedQty = Specifications?
    .Where(s => s?.Items != null)
    .Sum(s => s.Items.Sum(i => i?.DeliveredQty ?? 0)) ?? 0;
            BalanceQty = TotalQty - ReleasedQty;
            TotalSQM = Specifications?.Sum(s => s.TotalSQM) ?? 0;
            TotalAmount = Specifications?.Sum(s => s.TotalAmount) ?? 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string name = null) { if (Equals(field, value)) return false; field = value; OnPropertyChanged(name); return true; }
    }

    // ==================== JOB ORDER SPECIFICATION ====================
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

        // ✅ Cached totals
        private int _cachedTotalQty;
        private double _cachedTotalSQM1;
        private double _cachedTotalSQM2;
        private double _cachedTotalSQM;
        private double _cachedTotalLM1;
        private double _cachedTotalLM2;
        private double _cachedTotalAmount;
        private double _cachedOtherChargesTotal;
        private bool _totalsValid;

        private ObservableCollection<JobOrderItem> _items;
        private ObservableCollection<JobOrderOtherCharge> _otherCharges;

        public JobOrderSpecification()
        {
            _items = new ObservableCollection<JobOrderItem>();
            _otherCharges = new ObservableCollection<JobOrderOtherCharge>();
            _items.CollectionChanged += (s, e) => InvalidateTotals();
            _otherCharges.CollectionChanged += (s, e) => InvalidateTotals();
        }

        public int Id { get => _id; set => SetField(ref _id, value); }
        public string SpecificationName { get => _specificationName; set => SetField(ref _specificationName, value ?? ""); }
        public string ModuleType { get => _moduleType; set => SetField(ref _moduleType, value ?? ""); }
        public string WorkType { get => _workType; set => SetField(ref _workType, value ?? ""); }
        public string OuterThickness { get => _outerThickness; set => SetField(ref _outerThickness, value ?? ""); }
        public string OuterColor { get => _outerColor; set => SetField(ref _outerColor, value ?? ""); }
        public string InnerThickness { get => _innerThickness; set => SetField(ref _innerThickness, value ?? ""); }
        public string InnerColor { get => _innerColor; set => SetField(ref _innerColor, value ?? ""); }
        public string SpacerThickness { get => _spacerThickness; set => SetField(ref _spacerThickness, value ?? ""); }
        public string PVBThickness { get => _pvbThickness; set => SetField(ref _pvbThickness, value ?? ""); }
        public string PVBColor { get => _pvbColor; set => SetField(ref _pvbColor, value ?? ""); }
        public bool IncludeInSpec { get => _includeInSpec; set => SetField(ref _includeInSpec, value); }
        public double BasePrice { get => _basePrice; set => SetField(ref _basePrice, value); }
        public double SurchargePercent { get => _surchargePercent; set => SetField(ref _surchargePercent, value); }

        public ObservableCollection<JobOrderItem> Items
        {
            get => _items;
            set
            {
                if (_items != null) _items.CollectionChanged -= Items_CollectionChanged;
                _items = value;
                OnPropertyChanged();
                if (_items != null) _items.CollectionChanged += Items_CollectionChanged;
                InvalidateTotals();
            }
        }

        public ObservableCollection<JobOrderOtherCharge> OtherCharges
        {
            get => _otherCharges;
            set
            {
                if (_otherCharges != null) _otherCharges.CollectionChanged -= OtherCharges_CollectionChanged;
                _otherCharges = value;
                OnPropertyChanged();
                if (_otherCharges != null) _otherCharges.CollectionChanged += OtherCharges_CollectionChanged;
                InvalidateTotals();
            }
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => InvalidateTotals();
        private void OtherCharges_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => InvalidateTotals();

        public int TotalItems => _items?.Count ?? 0;
        public int TotalQty { get { EnsureTotalsValid(); return _cachedTotalQty; } }
        public double TotalSQM1 { get { EnsureTotalsValid(); return _cachedTotalSQM1; } }
        public double TotalSQM2 { get { EnsureTotalsValid(); return _cachedTotalSQM2; } }
        public double TotalSQM { get { EnsureTotalsValid(); return _cachedTotalSQM; } }
        public double TotalLM1 { get { EnsureTotalsValid(); return _cachedTotalLM1; } }
        public double TotalLM2 { get { EnsureTotalsValid(); return _cachedTotalLM2; } }
        public double OtherChargesTotal { get { EnsureTotalsValid(); return _cachedOtherChargesTotal; } }
        public double TotalAmount { get { EnsureTotalsValid(); return _cachedTotalAmount; } }

        private void EnsureTotalsValid()
        {
            if (!_totalsValid) { RefreshTotals(); _totalsValid = true; }
        }

        private void InvalidateTotals()
        {
            _totalsValid = false;

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

        private void RefreshTotals()
        {
            _cachedTotalQty = 0; _cachedTotalSQM1 = 0; _cachedTotalSQM2 = 0;
            _cachedTotalSQM = 0; _cachedTotalLM1 = 0; _cachedTotalLM2 = 0;
            _cachedOtherChargesTotal = 0; _cachedTotalAmount = 0;

            if (_items != null)
            {
                foreach (var item in _items)
                {
                    _cachedTotalQty += item.Qty;
                    _cachedTotalSQM1 += item.SQM1;
                    _cachedTotalSQM2 += item.SQM2;
                    _cachedTotalSQM += item.TotalSQM;
                    _cachedTotalLM1 += item.LM1;
                    _cachedTotalLM2 += item.LM2;
                    _cachedTotalAmount += item.TotalAmount;
                }
            }

            if (_otherCharges != null)
            {
                foreach (var charge in _otherCharges)
                    _cachedOtherChargesTotal += charge?.Amount ?? 0;
            }

            _cachedTotalAmount = _cachedTotalAmount + _cachedOtherChargesTotal;
        }

        public void CalculateTotals() { RefreshTotals(); _totalsValid = true; InvalidateTotals(); }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string name = null) { if (Equals(field, value)) return false; field = value; OnPropertyChanged(name); return true; }
    }

    // ==================== JOB ORDER ITEM (DEBOUNCED) ====================
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

        // ✅ Debounce timer
        private readonly DispatcherTimer _recalculateTimer;
        private bool _pendingRecalculation;

        public JobOrderItem()
        {
            _recalculateTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _recalculateTimer.Tick += (s, e) =>
            {
                _recalculateTimer.Stop();
                if (_pendingRecalculation) { PerformRecalculation(); _pendingRecalculation = false; }
            };
            Recalculate();
        }

        public int Id { get => _id; set => SetField(ref _id, value); }
        public int SrNo { get => _srNo; set => SetField(ref _srNo, value); }
        public string GlassRef { get => _glassRef; set => SetField(ref _glassRef, value); }

        public double Width1 { get => _width1; set { if (SetField(ref _width1, value)) ScheduleRecalculation(); } }
        public double Height1 { get => _height1; set { if (SetField(ref _height1, value)) ScheduleRecalculation(); } }
        public double Width2 { get => _width2; set { if (SetField(ref _width2, value)) ScheduleRecalculation(); } }
        public double Height2 { get => _height2; set { if (SetField(ref _height2, value)) ScheduleRecalculation(); } }

        public int Qty
        {
            get => _qty;
            set { if (SetField(ref _qty, value)) { ScheduleRecalculation(); OnPropertyChanged(nameof(RemainingQty)); } }
        }

        public int DeliveredQty { get => _deliveredQty; set { if (SetField(ref _deliveredQty, value)) OnPropertyChanged(nameof(RemainingQty)); } }
        public int RemainingQty => Qty - DeliveredQty;

        public double Price { get => _price; set { if (SetField(ref _price, value)) ScheduleRecalculation(); } }

        public double SQM1 => _sqm1;
        public double SQM2 => _sqm2;
        public double LM1 => _lm1;
        public double LM2 => _lm2;
        public double TotalSQM => Math.Round((_sqm1 + _sqm2) * Qty, 4);
        public double TotalLM => Math.Round((_lm1 + _lm2) * Qty, 4);
        public double TotalAmount => Math.Round(TotalSQM * _price, 2);

        private void ScheduleRecalculation()
        {
            _pendingRecalculation = true;
            _recalculateTimer.Stop();
            _recalculateTimer.Start();
        }

        private void PerformRecalculation()
        {
            _sqm1 = CalculateSQM(_width1, _height1);
            _sqm2 = CalculateSQM(_width2, _height2);
            _lm1 = CalculateLM(_width1, _height1);
            _lm2 = CalculateLM(_width2, _height2);

            OnPropertyChanged(nameof(SQM1));
            OnPropertyChanged(nameof(SQM2));
            OnPropertyChanged(nameof(LM1));
            OnPropertyChanged(nameof(LM2));
            OnPropertyChanged(nameof(TotalSQM));
            OnPropertyChanged(nameof(TotalLM));
            OnPropertyChanged(nameof(TotalAmount));
        }

        public void Recalculate() => PerformRecalculation();

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
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string name = null) { if (Equals(field, value)) return false; field = value; OnPropertyChanged(name); return true; }
    }

    // ==================== JOB ORDER OTHER CHARGE ====================
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
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string name = null) { if (Equals(field, value)) return false; field = value; OnPropertyChanged(name); return true; }

        public string Name { get => _name; set => SetField(ref _name, value ?? ""); }

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

        public string LmDimType { get => _lmDimType; set => SetField(ref _lmDimType, value ?? "w1h1"); }
        public int LinkedSpecIndex { get => _linkedSpecIndex; set => SetField(ref _linkedSpecIndex, value); }

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
                    if (value) LinkedSpecIndices = "";
                }
            }
        }

        public bool IsManualOverride { get => _isManualOverride; set { _isManualOverride = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsAutoMode)); } }
        public bool IsAutoMode => !_isManualOverride;

        private void CalculateAmount()
        {
            Amount = Math.Round(_value * _rate, 2);
        }

        public string ValueDisplay => Type?.ToLower() switch
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

        public string UnitDisplay => Type?.ToLower() switch
        {
            "lm" => "AED/LM",
            "sqm" or "sqm1" or "sqm2" => "AED/SQM",
            "qty" or "1x" or "2x" => "AED/pc",
            _ => "AED"
        };

        public string RateDisplay => Rate == 0 ? "0" : $"{Rate:F2}";
        public string AmountDisplay => $"AED {Amount:N2}";

        public string TypeDisplay => Type?.ToLower() switch
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

        public bool IsLMBased => Type == "lm" || Type == "sqm" || Type == "sqm1" || Type == "sqm2";
        public bool IsHoleType => Type == "1x" || Type == "2x";

        public string LinkedSpecsDisplay
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_linkedSpecIndices)) return "All Specs";
                var indices = SpecIndexList;
                if (indices.Count == 0) return "All Specs";
                return string.Join(" + ", indices.Select(i => $"Spec {i + 1}"));
            }
        }

        public List<int> SpecIndexList
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_linkedSpecIndices)) return new List<int>();
                return _linkedSpecIndices.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out int i) ? i : -1).Where(i => i >= 0).ToList();
            }
        }

        public void AddSpec(int index)
        {
            var indices = SpecIndexList;
            if (!indices.Contains(index)) { indices.Add(index); indices.Sort(); LinkedSpecIndices = string.Join(",", indices); }
        }

        public void RemoveSpec(int index)
        {
            var indices = SpecIndexList;
            if (indices.Contains(index)) { indices.Remove(index); LinkedSpecIndices = indices.Count > 0 ? string.Join(",", indices) : ""; }
        }

        public void ToggleSpec(int index)
        {
            if (SpecIndexList.Contains(index)) RemoveSpec(index); else AddSpec(index);
        }

        public void SetAllSpecs() => LinkedSpecIndices = "";

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