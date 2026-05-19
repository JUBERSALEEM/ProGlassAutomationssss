using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProGlassAutomation.Models
{
    public class ProformaInvoiceModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ProformaInvoiceModel()
        {
            Specifications = new ObservableCollection<SpecificationModel>();
            Specifications.CollectionChanged += Specs_CollectionChanged;
            InvoiceNo = GenerateInvoiceNo();
        }

        private void Specs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Subscribe to new specs
            if (e.NewItems != null)
            {
                foreach (SpecificationModel spec in e.NewItems)
                {
                    spec.PropertyChanged += Spec_PropertyChanged;
                }
            }
            // Unsubscribe from old specs
            if (e.OldItems != null)
            {
                foreach (SpecificationModel spec in e.OldItems)
                {
                    spec.PropertyChanged -= Spec_PropertyChanged;
                }
            }
            CalculateTotals();
        }

        private void Spec_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Recalculate totals when any spec totals change
            if (e.PropertyName == nameof(SpecificationModel.SpecTotalSQM) ||
                e.PropertyName == nameof(SpecificationModel.SpecTotalLM) ||
                e.PropertyName == nameof(SpecificationModel.SpecTotalQty) ||
                e.PropertyName == nameof(SpecificationModel.SpecTotalPrice))
            {
                CalculateTotals();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _invoiceNo = "";
        public string InvoiceNo
        {
            get => _invoiceNo;
            set { _invoiceNo = value; OnPropertyChanged(); }
        }

        private DateTime _invoiceDate = DateTime.Now;
        public DateTime InvoiceDate
        {
            get => _invoiceDate;
            set { _invoiceDate = value; OnPropertyChanged(); }
        }

        private DateTime _validUntil = DateTime.Now.AddDays(30);
        public DateTime ValidUntil
        {
            get => _validUntil;
            set { _validUntil = value; OnPropertyChanged(); }
        }

        private string _customerName = "";
        public string CustomerName
        {
            get => _customerName;
            set { _customerName = value; OnPropertyChanged(); }
        }

        private string _customerTRN = "";
        public string CustomerTRN
        {
            get => _customerTRN;
            set { _customerTRN = value; OnPropertyChanged(); }
        }

        private string _customerAddress = "";
        public string CustomerAddress
        {
            get => _customerAddress;
            set { _customerAddress = value; OnPropertyChanged(); }
        }

        private string _projectName = "";
        public string ProjectName
        {
            get => _projectName;
            set { _projectName = value; OnPropertyChanged(); }
        }

        private string _projectLocation = "";
        public string ProjectLocation
        {
            get => _projectLocation;
            set { _projectLocation = value; OnPropertyChanged(); }
        }

        private string _lPONo = "";
        public string LPONo
        {
            get => _lPONo;
            set { _lPONo = value; OnPropertyChanged(); }
        }

        private string _attentionName = "";
        public string AttentionName
        {
            get => _attentionName;
            set { _attentionName = value; OnPropertyChanged(); }
        }

        private string _contactNo = "";
        public string ContactNo
        {
            get => _contactNo;
            set { _contactNo = value; OnPropertyChanged(); }
        }

        public ObservableCollection<SpecificationModel> Specifications { get; }

        private double _totalSQM = 0;
        public double TotalSQM
        {
            get => _totalSQM;
            private set
            {
                if (_totalSQM != value)
                {
                    _totalSQM = value;
                    OnPropertyChanged();
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
                    OnPropertyChanged();
                }
            }
        }

        private int _totalQty = 0;
        public int TotalQty
        {
            get => _totalQty;
            private set
            {
                if (_totalQty != value)
                {
                    _totalQty = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _grandTotal = 0;
        public double GrandTotal
        {
            get => _grandTotal;
            private set
            {
                if (_grandTotal != value)
                {
                    _grandTotal = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _vatPercent = 5;
        public double VatPercent
        {
            get => _vatPercent;
            set
            {
                if (_vatPercent != value)
                {
                    _vatPercent = value;
                    OnPropertyChanged();
                    CalculateTotals();
                }
            }
        }

        private double _vatAmount = 0;
        public double VatAmount
        {
            get => _vatAmount;
            private set
            {
                if (_vatAmount != value)
                {
                    _vatAmount = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _netTotal = 0;
        public double NetTotal
        {
            get => _netTotal;
            private set
            {
                if (_netTotal != value)
                {
                    _netTotal = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isDirty = false;
        public bool IsDirty
        {
            get => _isDirty;
            set { _isDirty = value; OnPropertyChanged(); }
        }

        public void CalculateTotals()
        {
            double sqm = 0, lm = 0;
            int qty = 0;
            double price = 0;

            foreach (var spec in Specifications)
            {
                spec.CalculateSpecTotals();
                sqm += spec.SpecTotalSQM;
                lm += spec.SpecTotalLM;
                qty += spec.SpecTotalQty;
                price += spec.SpecTotalPrice;
            }

            TotalSQM = Math.Round(sqm, 4);
            TotalLM = Math.Round(lm, 4);
            TotalQty = qty;
            GrandTotal = Math.Round(price, 2);
            VatAmount = Math.Round(GrandTotal * VatPercent / 100, 2);
            NetTotal = Math.Round(GrandTotal + VatAmount, 2);
            IsDirty = true;
        }

        public string GenerateInvoiceNo()
        {
            return $"PI-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        }
    }
}