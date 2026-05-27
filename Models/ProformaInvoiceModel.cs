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

        // ==================== STATIC RANDOM (Thread-Safe) ====================
        private static readonly object _invoiceLock = new object();
        private static int _lastGeneratedNumber;

        // ==================== CONSTRUCTOR ====================
        public ProformaInvoiceModel()
        {
            // Use field directly - property setter handles subscription
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
                    UnsubscribeFromSpecification(spec);
                }
            }

            _disposed = true;
        }

        // ==================== COLLECTION HANDLING ====================
        private void OnSpecificationsCollectionChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Subscribe to new specs
            if (e.NewItems != null)
            {
                foreach (SpecificationModel spec in e.NewItems)
                {
                    SubscribeToSpecification(spec);
                }
            }

            // Unsubscribe from old specs
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

            if (spec.OtherCharges != null)
            {
                spec.OtherCharges.CollectionChanged -= OnOtherChargesCollectionChanged;

                foreach (var charge in spec.OtherCharges)
                {
                    charge.PropertyChanged -= OnOtherChargePropertyChanged;
                }
            }
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

            CalculateTotals();
        }

        private void OnSpecificationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
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

        private string _status = "Draft";
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private bool _isConvertedToJobOrder;
        public bool IsConvertedToJobOrder
        {
            get => _isConvertedToJobOrder;
            set => SetProperty(ref _isConvertedToJobOrder, value);
        }

        private ObservableCollection<SpecificationModel> _specifications = new();
        public ObservableCollection<SpecificationModel> Specifications
        {
            get => _specifications;
            set
            {
                if (Equals(_specifications, value))
                    return;

                // UNSUBSCRIBE from old collection
                if (_specifications != null)
                {
                    _specifications.CollectionChanged -= OnSpecificationsCollectionChanged;
                    foreach (var spec in _specifications)
                    {
                        UnsubscribeFromSpecification(spec);
                    }
                }

                _specifications = value;

                // SUBSCRIBE to new collection
                if (_specifications != null)
                {
                    _specifications.CollectionChanged += OnSpecificationsCollectionChanged;
                    foreach (var spec in _specifications)
                    {
                        SubscribeToSpecification(spec);
                    }
                }

                OnPropertyChanged();
            }
        }

        // ==================== TOTALS ====================
        private double _totalSQM1 = 0;
        public double TotalSQM1
        {
            get => _totalSQM1;
            private set => SetProperty(ref _totalSQM1, value);
        }

        private double _totalSQM2 = 0;
        public double TotalSQM2
        {
            get => _totalSQM2;
            private set => SetProperty(ref _totalSQM2, value);
        }

        private double _totalSQM = 0;
        public double TotalSQM
        {
            get => _totalSQM;
            private set => SetProperty(ref _totalSQM, value);
        }

        private double _totalLM = 0;
        public double TotalLM
        {
            get => _totalLM;
            private set => SetProperty(ref _totalLM, value);
        }

        private double _totalLM1 = 0;
        public double TotalLM1
        {
            get => _totalLM1;
            private set => SetProperty(ref _totalLM1, value);
        }

        private int _totalQty = 0;
        public int TotalQty
        {
            get => _totalQty;
            private set => SetProperty(ref _totalQty, value);
        }

        private double _grandTotal = 0;
        public double GrandTotal
        {
            get => _grandTotal;
            private set => SetProperty(ref _grandTotal, value);
        }

        private double _otherChargesTotal = 0;
        public double OtherChargesTotal
        {
            get => _otherChargesTotal;
            private set => SetProperty(ref _otherChargesTotal, value);
        }

        private double _vatPercent = 5;
        public double VatPercent
        {
            get => _vatPercent;
            set
            {
                if (value < 0 || value > 100)
                    throw new ArgumentOutOfRangeException(nameof(VatPercent), "VAT must be between 0 and 100.");
                if (SetProperty(ref _vatPercent, value))
                {
                    CalculateTotals();
                }
            }
        }

        private double _vatAmount = 0;
        public double VatAmount
        {
            get => _vatAmount;
            private set => SetProperty(ref _vatAmount, value);
        }

        private double _netTotal = 0;
        public double NetTotal
        {
            get => _netTotal;
            private set => SetProperty(ref _netTotal, value);
        }

        private bool _isDirty = false;
        public bool IsDirty
        {
            get => _isDirty;
            set => SetProperty(ref _isDirty, value);
        }

        // ==================== CALCULATIONS ====================
        public void CalculateTotals()
        {
            double sqm1 = 0, sqm2 = 0, sqm = 0, lm = 0;
            int qty = 0;
            double specTotal = 0;
            double otherCharges = 0;

            foreach (var spec in Specifications)
            {
                spec.CalculateSpecTotals();
                spec.CalculateOtherChargesTotal();

                sqm1 += spec.SpecTotalSQM1;
                sqm2 += spec.SpecTotalSQM2;
                sqm += spec.SpecTotalSQM;
                lm += spec.SpecTotalLM1;  // ✅ Changed: TotalLM = LM1 only (not LM1+LM2)
                qty += spec.SpecTotalQty;
                specTotal += spec.SpecTotalPrice;
                otherCharges += spec.OtherChargesTotal;
            }

            TotalSQM1 = Math.Round(sqm1, 4);
            TotalSQM2 = Math.Round(sqm2, 4);
            TotalSQM = Math.Round(sqm, 4);
            TotalLM = Math.Round(lm, 4);
            TotalLM1 = Math.Round(lm, 4);  // ✅ Added: TotalLM1 for Invoice Summary
            TotalQty = qty;
            OtherChargesTotal = Math.Round(otherCharges, 2);
            GrandTotal = Math.Round(specTotal + otherCharges, 2);
            VatAmount = Math.Round(GrandTotal * VatPercent / 100.0, 2);
            NetTotal = Math.Round(GrandTotal + VatAmount, 2);
            IsDirty = true;
        }

        // ==================== INVOICE NUMBER GENERATION ====================
        public static string GenerateInvoiceNo()
        {
            lock (_invoiceLock)
            {
                int num;
                var random = new Random();

                // Ensure unique number within session
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
    }
}