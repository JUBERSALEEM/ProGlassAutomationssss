using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ProGlassAutomation.Models
{
    public class ProformaInvoiceModel : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private bool _disposed;

        // ==================== ID (For DB Linkage) ====================
        private int _id = 0;
        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        // ==================== STATIC RANDOM (Thread-Safe) ====================
        private static readonly object _invoiceLock = new object();
        private static int _lastGeneratedNumber;

        // ==================== BULK UPDATE MODE (PATCH 8) ====================
        private bool _isBulkUpdating = false;
        public bool IsBulkUpdating
        {
            get => _isBulkUpdating;
            set
            {
                if (_isBulkUpdating != value)
                {
                    _isBulkUpdating = value;
                    OnPropertyChanged();

                    if (!value)
                        CalculateTotals();
                }
            }
        }

        // ==================== CONSTRUCTOR ====================
        public ProformaInvoiceModel()
        {
            _specifications = new ObservableCollection<SpecificationModel>();
            _specifications.CollectionChanged += OnSpecificationsCollectionChanged;
            InvoiceNo = GenerateInvoiceNo();
        }

        // ==================== DISPOSAL ====================
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                Specifications.CollectionChanged -= OnSpecificationsCollectionChanged;

                foreach (var spec in Specifications)
                {
                    spec?.Dispose();
                }

                Specifications.Clear();
            }

            _disposed = true;
        }

        // ==================== COLLECTION HANDLING ====================
        private void OnSpecificationsCollectionChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (IsBulkUpdating) return;

            if (e.NewItems != null)
            {
                foreach (SpecificationModel spec in e.NewItems)
                {
                    SubscribeToSpecification(spec);
                }
            }

            if (e.OldItems != null)
            {
                foreach (SpecificationModel spec in e.OldItems)
                {
                    UnsubscribeFromSpecification(spec);
                }
            }

            CalculateTotals();
        }

        private void SubscribeToSpecification(SpecificationModel spec)
        {
            if (spec == null) return;

            spec.PropertyChanged += OnSpecificationPropertyChanged;
            spec.Items.CollectionChanged += OnItemsCollectionChanged;

            if (spec.OtherCharges != null)
            {
                spec.OtherCharges.CollectionChanged += OnOtherChargesCollectionChanged;

                foreach (var charge in spec.OtherCharges)
                {
                    charge.PropertyChanged += OnOtherChargePropertyChanged;
                }
            }
        }

        private void UnsubscribeFromSpecification(SpecificationModel spec)
        {
            if (spec == null) return;

            spec.PropertyChanged -= OnSpecificationPropertyChanged;

            if (spec.Items != null)
            {
                spec.Items.CollectionChanged -= OnItemsCollectionChanged;
            }

            if (spec.OtherCharges != null)
            {
                spec.OtherCharges.CollectionChanged -= OnOtherChargesCollectionChanged;

                foreach (var charge in spec.OtherCharges)
                {
                    charge.PropertyChanged -= OnOtherChargePropertyChanged;
                }
            }
        }

        private void OnItemsCollectionChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (IsBulkUpdating) return;
            CalculateTotals();
        }

        private void OnOtherChargesCollectionChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (OtherChargeModel charge in e.NewItems)
                {
                    charge.PropertyChanged += OnOtherChargePropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (OtherChargeModel charge in e.OldItems)
                {
                    charge.PropertyChanged -= OnOtherChargePropertyChanged;
                }
            }

            if (!IsBulkUpdating)
                CalculateTotals();
        }

        private void OnSpecificationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (IsBulkUpdating) return;

            if (e.PropertyName == nameof(SpecificationModel.SpecTotalSQM) ||
                e.PropertyName == nameof(SpecificationModel.SpecTotalLM) ||
                e.PropertyName == nameof(SpecificationModel.SpecTotalQty) ||
                e.PropertyName == nameof(SpecificationModel.SpecTotalPrice) ||
                e.PropertyName == nameof(SpecificationModel.OtherChargesTotal))
            {
                CalculateTotals();
            }
        }

        private void OnOtherChargePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (IsBulkUpdating) return;
            CalculateTotals();
        }

        // ==================== PROPERTY CHANGED ====================
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);

            if (!IsBulkUpdating && propertyName != nameof(IsDirty) && propertyName != nameof(IsBulkUpdating))
            {
                IsDirty = true;
            }

            return true;
        }

        // ==================== INVOICE DETAILS ====================
        private string _invoiceNo = "";
        public string InvoiceNo
        {
            get => _invoiceNo;
            set => SetProperty(ref _invoiceNo, value);
        }

        private DateTime _invoiceDate = DateTime.Now;
        public DateTime InvoiceDate
        {
            get => _invoiceDate;
            set => SetProperty(ref _invoiceDate, value);
        }

        private DateTime _validUntil = DateTime.Now.AddDays(30);
        public DateTime ValidUntil
        {
            get => _validUntil;
            set => SetProperty(ref _validUntil, value);
        }

        private string _customerName = "";
        public string CustomerName
        {
            get => _customerName;
            set => SetProperty(ref _customerName, value);
        }

        private string _customerTRN = "";
        public string CustomerTRN
        {
            get => _customerTRN;
            set => SetProperty(ref _customerTRN, value);
        }

        private string _customerAddress = "";
        public string CustomerAddress
        {
            get => _customerAddress;
            set => SetProperty(ref _customerAddress, value);
        }

        private string _customerReference = "";
        public string CustomerReference
        {
            get => _customerReference;
            set => SetProperty(ref _customerReference, value);
        }

        private string _salesman = "";
        public string Salesman
        {
            get => _salesman;
            set => SetProperty(ref _salesman, value);
        }

        private string _projectName = "";
        public string ProjectName
        {
            get => _projectName;
            set => SetProperty(ref _projectName, value);
        }

        private string _projectNo = "";
        public string ProjectNo
        {
            get => _projectNo;
            set => SetProperty(ref _projectNo, value);
        }

        private string _projectLocation = "";
        public string ProjectLocation
        {
            get => _projectLocation;
            set => SetProperty(ref _projectLocation, value);
        }

        private string _lPONo = "";
        public string LPONo
        {
            get => _lPONo;
            set => SetProperty(ref _lPONo, value);
        }

        private string _attentionName = "";
        public string AttentionName
        {
            get => _attentionName;
            set => SetProperty(ref _attentionName, value);
        }

        private string _contactNo = "";
        public string ContactNo
        {
            get => _contactNo;
            set => SetProperty(ref _contactNo, value);
        }

        private string _color = "";
        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        private string _notes = "";
        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        private string _companyName = "PROGLASS AUTOMATION";
        public string CompanyName
        {
            get => _companyName;
            set => SetProperty(ref _companyName, value);
        }

        private string _companyTRN = "100458979400003";
        public string CompanyTRN
        {
            get => _companyTRN;
            set => SetProperty(ref _companyTRN, value);
        }

        private string _companyLocation = "Dubai, UAE";
        public string CompanyLocation
        {
            get => _companyLocation;
            set => SetProperty(ref _companyLocation, value);
        }

        private string _companyPhone = "+971-50-123-4567";
        public string CompanyPhone
        {
            get => _companyPhone;
            set => SetProperty(ref _companyPhone, value);
        }

        // ============ STATUS MANAGEMENT (PATCH 16) ============
        private string _status = "Draft";
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    if (!IsValidStatusTransition(_status, value))
                    {
                        throw new InvalidOperationException($"Invalid status transition from '{_status}' to '{value}'");
                    }
                    SetProperty(ref _status, value);
                    IsDirty = true;
                }
            }
        }

        private static readonly Dictionary<string, string[]> _statusTransitions = new()
        {
            { "Draft", new[] { "Pending", "Confirmed", "Cancelled" } },
            { "Pending", new[] { "Draft", "Confirmed", "In Progress", "Cancelled" } },
            { "Confirmed", new[] { "In Progress", "Completed", "Cancelled" } },
            { "In Progress", new[] { "Confirmed", "Completed", "Cancelled" } },
            { "Completed", new[] { "In Progress", "Cancelled" } },
            { "Cancelled", new[] { "Draft" } }
        };

        public static bool IsValidStatusTransition(string from, string to)
        {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) return false;
            if (from == to) return true;
            return _statusTransitions.TryGetValue(from, out var transitions) && transitions.Contains(to);
        }

        // ============ JOB ORDER TRACKING (PATCH 1) ============
        private bool _isConvertedToJobOrder;
        public bool IsConvertedToJobOrder
        {
            get => _isConvertedToJobOrder;
            set
            {
                if (_isConvertedToJobOrder != value)
                {
                    _isConvertedToJobOrder = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ConversionStatus));
                    OnPropertyChanged(nameof(IsLocked));

                    if (value && !_convertedDate.HasValue)
                    {
                        ConvertedDate = DateTime.Now;
                        ConvertedBy = Environment.UserName;
                    }

                    IsDirty = true;
                }
            }
        }

        public bool IsLocked => IsConvertedToJobOrder && !string.IsNullOrEmpty(_sourceJobOrderNo);

        private string _jobOrderId = "";
        public string JobOrderId
        {
            get => _jobOrderId;
            set { if (_jobOrderId != value) { _jobOrderId = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConversionStatus)); IsDirty = true; } }
        }

        private string _sourceJobOrderNo = "";
        public string SourceJobOrderNo
        {
            get => _sourceJobOrderNo;
            set { if (_sourceJobOrderNo != value) { _sourceJobOrderNo = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConversionStatus)); OnPropertyChanged(nameof(IsLocked)); IsDirty = true; } }
        }

        private DateTime? _convertedDate;
        public DateTime? ConvertedDate
        {
            get => _convertedDate;
            set { if (_convertedDate != value) { _convertedDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConversionStatus)); IsDirty = true; } }
        }

        private string _convertedBy = "";
        public string ConvertedBy
        {
            get => _convertedBy;
            set { if (_convertedBy != value) { _convertedBy = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConversionStatus)); IsDirty = true; } }
        }

        private int _revision = 0;
        public int Revision
        {
            get => _revision;
            set => SetProperty(ref _revision, value);
        }

        public string ConversionStatus
        {
            get
            {
                if (!IsConvertedToJobOrder) return "";
                var status = $"Converted to JO: {SourceJobOrderNo}";
                if (!string.IsNullOrEmpty(SourceJobOrderNo) && Revision > 0)
                    status += $" (Rev {Revision})";
                if (ConvertedDate.HasValue)
                    status += $" on {ConvertedDate:dd MMM yyyy}";
                return status;
            }
        }

        // ==================== SPECIFICATIONS ====================
        private ObservableCollection<SpecificationModel> _specifications = new();
        public ObservableCollection<SpecificationModel> Specifications
        {
            get => _specifications;
            set
            {
                if (Equals(_specifications, value)) return;

                if (_specifications != null)
                {
                    _specifications.CollectionChanged -= OnSpecificationsCollectionChanged;
                    foreach (var spec in _specifications) { UnsubscribeFromSpecification(spec); spec?.Dispose(); }
                }

                _specifications = value;

                if (_specifications != null)
                {
                    _specifications.CollectionChanged += OnSpecificationsCollectionChanged;
                    foreach (var spec in _specifications)
                    {
                        if (spec != null) { spec.Invoice = this; SubscribeToSpecification(spec); }
                    }
                }

                OnPropertyChanged();
            }
        }

        // ==================== TOTALS ====================
        private double _totalSQM1 = 0;
        public double TotalSQM1 { get => _totalSQM1; private set => SetProperty(ref _totalSQM1, value); }

        private double _totalSQM2 = 0;
        public double TotalSQM2 { get => _totalSQM2; private set => SetProperty(ref _totalSQM2, value); }

        private double _totalSQM = 0;
        public double TotalSQM { get => _totalSQM; private set => SetProperty(ref _totalSQM, value); }

        private double _totalLM = 0;
        public double TotalLM { get => _totalLM; private set => SetProperty(ref _totalLM, value); }

        private double _totalLM1 = 0;
        public double TotalLM1 { get => _totalLM1; private set => SetProperty(ref _totalLM1, value); }

        private int _totalQty = 0;
        public int TotalQty { get => _totalQty; private set => SetProperty(ref _totalQty, value); }

        private double _grandTotal = 0;
        public double GrandTotal { get => _grandTotal; private set => SetProperty(ref _grandTotal, value); }

        private double _otherChargesTotal = 0;
        public double OtherChargesTotal { get => _otherChargesTotal; private set => SetProperty(ref _otherChargesTotal, value); }

        private double _vatPercent = 5;
        public double VatPercent
        {
            get => _vatPercent;
            set
            {
                if (value < 0 || value > 100)
                    throw new ArgumentOutOfRangeException(nameof(VatPercent), "VAT must be between 0 and 100.");
                if (SetProperty(ref _vatPercent, value))
                    CalculateTotals();
            }
        }

        private double _vatAmount = 0;
        public double VatAmount { get => _vatAmount; private set => SetProperty(ref _vatAmount, value); }

        private double _netTotal = 0;
        public double NetTotal { get => _netTotal; private set => SetProperty(ref _netTotal, value); }

        private bool _isDirty = false;
        public bool IsDirty
        {
            get => _isDirty;
            set => SetProperty(ref _isDirty, value);
        }

        // ==================== VALIDATION (PATCH 17) ====================
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(InvoiceNo))
                result.AddError("InvoiceNo", "Invoice number is required");

            if (string.IsNullOrWhiteSpace(CustomerName))
                result.AddError("CustomerName", "Customer name is required");

            if (InvoiceDate > ValidUntil)
                result.AddError("ValidUntil", "Valid until date must be after invoice date");

            if (VatPercent < 0 || VatPercent > 100)
                result.AddError("VatPercent", "VAT percentage must be between 0 and 100");

            if (Specifications == null || Specifications.Count == 0)
            {
                result.AddError("Specifications", "At least one specification is required");
            }
            else
            {
                for (int i = 0; i < Specifications.Count; i++)
                {
                    var spec = Specifications[i];
                    if (spec == null)
                    {
                        result.AddError($"Specifications[{i}]", "Specification cannot be null");
                        continue;
                    }

                    if (spec.Items == null || spec.Items.Count == 0)
                        result.AddError($"Specifications[{i}].Items", $"Specification '{spec.SpecificationName}' has no items");
                }
            }

            return result;
        }

        public bool IsValid => Validate().IsValid;

        // ==================== CALCULATIONS ====================
        public void CalculateTotals()
        {
            if (IsBulkUpdating) return;

            double sqm1 = 0, sqm2 = 0, sqm = 0, lm = 0;
            int qty = 0;
            double specTotal = 0;
            double otherCharges = 0;

            foreach (var spec in Specifications)
            {
                if (spec == null) continue;

                spec.CalculateSpecTotals();
                spec.CalculateOtherChargesTotal();

                sqm1 += spec.SpecTotalSQM1;
                sqm2 += spec.SpecTotalSQM2;
                sqm += spec.SpecTotalSQM;
                lm += spec.SpecTotalLM1;
                qty += spec.SpecTotalQty;
                specTotal += spec.SpecTotalPrice;
                otherCharges += spec.OtherChargesTotal;
            }

            TotalSQM1 = Math.Round(sqm1, 4);
            TotalSQM2 = Math.Round(sqm2, 4);
            TotalSQM = Math.Round(sqm, 4);
            TotalLM = Math.Round(lm, 4);
            TotalLM1 = Math.Round(lm, 4);
            TotalQty = qty;
            OtherChargesTotal = Math.Round(otherCharges, 2);
            GrandTotal = Math.Round(specTotal + otherCharges, 2);
            VatAmount = Math.Round(GrandTotal * VatPercent / 100.0, 2);
            NetTotal = Math.Round(GrandTotal + VatAmount, 2);
            IsDirty = true;
        }

        // ==================== BULK OPERATIONS (PATCH 8) ====================
        public void BeginBulkUpdate()
        {
            IsBulkUpdating = true;
        }

        public void EndBulkUpdate()
        {
            IsBulkUpdating = false;
            CalculateTotals();
        }

        public IDisposable BulkUpdateScope()
        {
            return new ProformaBulkUpdateScope(this);
        }

        private class ProformaBulkUpdateScope : BulkUpdateScope
        {
            private readonly ProformaInvoiceModel _invoice;

            public ProformaBulkUpdateScope(ProformaInvoiceModel invoice)
            {
                _invoice = invoice;
                _invoice.BeginBulkUpdate();
            }

            protected override void OnDispose()
            {
                _invoice.EndBulkUpdate();
            }
        }

        // ==================== DEEP CLONE (PATCH 15) ====================
        public ProformaInvoiceModel DeepClone()
        {
            var clone = new ProformaInvoiceModel
            {
                InvoiceNo = InvoiceNo,
                InvoiceDate = InvoiceDate,
                ValidUntil = ValidUntil,
                CustomerName = CustomerName,
                CustomerTRN = CustomerTRN,
                CustomerAddress = CustomerAddress,
                CustomerReference = CustomerReference,
                Salesman = Salesman,
                ProjectName = ProjectName,
                ProjectNo = ProjectNo,
                ProjectLocation = ProjectLocation,
                LPONo = LPONo,
                AttentionName = AttentionName,
                ContactNo = ContactNo,
                Color = Color,
                Notes = Notes,
                CompanyName = CompanyName,
                CompanyTRN = CompanyTRN,
                CompanyLocation = CompanyLocation,
                CompanyPhone = CompanyPhone,
                Status = Status,
                VatPercent = VatPercent,
                IsConvertedToJobOrder = IsConvertedToJobOrder,
                JobOrderId = JobOrderId,
                SourceJobOrderNo = SourceJobOrderNo,
                ConvertedDate = ConvertedDate,
                ConvertedBy = ConvertedBy,
                Revision = Revision
            };

            // Clone specifications
            foreach (var spec in Specifications)
            {
                var clonedSpec = spec.DeepClone();
                clonedSpec.Invoice = clone;
                clone.Specifications.Add(clonedSpec);
            }

            clone.CalculateTotals();
            clone.IsDirty = false;

            return clone;
        }

        // ==================== INVOICE NUMBER GENERATION ====================
        public static string GenerateInvoiceNo()
        {
            lock (_invoiceLock)
            {
                int num;
                var random = new Random();

                do
                {
                    num = random.Next(1000, 9999);
                } while (num == _lastGeneratedNumber && random.Next(10) > 0);

                _lastGeneratedNumber = num;
                return $"PI-{DateTime.Now:yyyyMMdd}-{num}";
            }
        }

        public static string GenerateSequentialInvoiceNo(int number)
        {
            return $"PI-{DateTime.Now:yyyyMMdd}-{number:D4}";
        }

        public string GenerateRevisionNumber()
        {
            Revision++;
            return $"{InvoiceNo}-R{Revision:D2}";
        }

        // ==================== CAN CONVERT CHECK ====================
        public bool CanConvertToJobOrder()
        {
            if (IsConvertedToJobOrder && !string.IsNullOrEmpty(JobOrderId))
                return false;

            if (Specifications == null || Specifications.Count == 0)
                return false;

            return Specifications.Any(s => s.Items != null && s.Items.Count > 0);
        }

        // ==================== RESET FOR NEW PI ====================
        public void ResetConversionStatus()
        {
            IsConvertedToJobOrder = false;
            JobOrderId = "";
            SourceJobOrderNo = "";
            ConvertedDate = null;
            ConvertedBy = "";
            Revision = 0;
        }
    }
}