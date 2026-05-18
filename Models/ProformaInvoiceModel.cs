using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProGlassAutomation.Models
{
    public class ProformaInvoiceModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void Notify(params string[] props)
        {
            foreach (var p in props)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }

        protected bool Set<T>(ref T field, T value, params string[] props)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            Notify(props);
            return true;
        }

        // ==================== HEADER ====================
        private string _invoiceNo = "";
        public string InvoiceNo
        {
            get => _invoiceNo;
            set => Set(ref _invoiceNo, value, nameof(InvoiceNo));
        }

        private DateTime _invoiceDate = DateTime.Now;
        public DateTime InvoiceDate
        {
            get => _invoiceDate;
            set => Set(ref _invoiceDate, value, nameof(InvoiceDate));
        }

        private DateTime _validUntil = DateTime.Now.AddDays(30);
        public DateTime ValidUntil
        {
            get => _validUntil;
            set => Set(ref _validUntil, value, nameof(ValidUntil));
        }

        // ==================== COMPANY DETAILS ====================
        private string _companyName = "PROGLASS AUTOMATION";
        public string CompanyName
        {
            get => _companyName;
            set => Set(ref _companyName, value, nameof(CompanyName));
        }

        private string _companyTRN = "";
        public string CompanyTRN
        {
            get => _companyTRN;
            set => Set(ref _companyTRN, value, nameof(CompanyTRN));
        }

        private string _companyAddress = "";
        public string CompanyAddress
        {
            get => _companyAddress;
            set => Set(ref _companyAddress, value, nameof(CompanyAddress));
        }

        private string _companyPhone = "";
        public string CompanyPhone
        {
            get => _companyPhone;
            set => Set(ref _companyPhone, value, nameof(CompanyPhone));
        }

        private string _companyEmail = "";
        public string CompanyEmail
        {
            get => _companyEmail;
            set => Set(ref _companyEmail, value, nameof(CompanyEmail));
        }

        // ==================== CUSTOMER DETAILS ====================
        private string _customerName = "";
        public string CustomerName
        {
            get => _customerName;
            set => Set(ref _customerName, value, nameof(CustomerName));
        }

        private string _customerTRN = "";
        public string CustomerTRN
        {
            get => _customerTRN;
            set => Set(ref _customerTRN, value, nameof(CustomerTRN));
        }

        private string _customerAddress = "";
        public string CustomerAddress
        {
            get => _customerAddress;
            set => Set(ref _customerAddress, value, nameof(CustomerAddress));
        }

        private string _customerPhone = "";
        public string CustomerPhone
        {
            get => _customerPhone;
            set => Set(ref _customerPhone, value, nameof(CustomerPhone));
        }

        // ==================== PROJECT DETAILS ====================
        private string _projectName = "";
        public string ProjectName
        {
            get => _projectName;
            set => Set(ref _projectName, value, nameof(ProjectName));
        }

        private string _projectLocation = "";
        public string ProjectLocation
        {
            get => _projectLocation;
            set => Set(ref _projectLocation, value, nameof(ProjectLocation));
        }

        // ==================== SPECIFICATIONS ====================
        public ObservableCollection<SpecificationModel> Specifications { get; set; } = new();

        // ==================== TOTALS ====================
        private double _totalSQM;
        public double TotalSQM
        {
            get => _totalSQM;
            set => Set(ref _totalSQM, value, nameof(TotalSQM));
        }

        private int _totalQty;
        public int TotalQty
        {
            get => _totalQty;
            set => Set(ref _totalQty, value, nameof(TotalQty));
        }

        private double _grandTotal;
        public double GrandTotal
        {
            get => _grandTotal;
            set => Set(ref _grandTotal, value, nameof(GrandTotal));
        }

        private double _vatPercent = 5;
        public double VatPercent
        {
            get => _vatPercent;
            set => Set(ref _vatPercent, value, nameof(VatPercent));
        }

        private double _vatAmount;
        public double VatAmount
        {
            get => _vatAmount;
            set => Set(ref _vatAmount, value, nameof(VatAmount));
        }

        private double _netTotal;
        public double NetTotal
        {
            get => _netTotal;
            set => Set(ref _netTotal, value, nameof(NetTotal));
        }

        // ==================== FILE INFO ====================
        private string _filePath = "";
        public string FilePath
        {
            get => _filePath;
            set => Set(ref _filePath, value, nameof(FilePath));
        }

        private bool _isDirty;
        public bool IsDirty
        {
            get => _isDirty;
            set => Set(ref _isDirty, value, nameof(IsDirty));
        }

        // ==================== METHODS ====================
        public void CalculateTotals()
        {
            TotalSQM = 0;
            TotalQty = 0;
            GrandTotal = 0;

            foreach (var spec in Specifications)
            {
                foreach (var item in spec.Items)
                {
                    TotalSQM += item.TotalSQM;
                    TotalQty += item.Qty;
                    GrandTotal += item.TotalPrice;
                }
            }

            VatAmount = GrandTotal * VatPercent / 100;
            NetTotal = GrandTotal + VatAmount;
            IsDirty = true;
        }

        public string GenerateInvoiceNo()
        {
            return $"PI-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        }
    }

    // ==================== SPECIFICATION MODEL ====================
    public class SpecificationModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void Notify(params string[] props)
        {
            foreach (var p in props)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }

        protected bool Set<T>(ref T field, T value, params string[] props)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            Notify(props);
            return true;
        }

        private string _specificationName = "";
        public string SpecificationName
        {
            get => _specificationName;
            set => Set(ref _specificationName, value, nameof(SpecificationName));
        }

        public ObservableCollection<InvoiceItemModel> Items { get; set; } = new();

        private double _specTotalSQM;
        public double SpecTotalSQM
        {
            get => _specTotalSQM;
            set => Set(ref _specTotalSQM, value, nameof(SpecTotalSQM));
        }

        private int _specTotalQty;
        public int SpecTotalQty
        {
            get => _specTotalQty;
            set => Set(ref _specTotalQty, value, nameof(SpecTotalQty));
        }

        private double _specTotalPrice;
        public double SpecTotalPrice
        {
            get => _specTotalPrice;
            set => Set(ref _specTotalPrice, value, nameof(SpecTotalPrice));
        }

        public void CalculateSpecTotals()
        {
            SpecTotalSQM = 0;
            SpecTotalQty = 0;
            SpecTotalPrice = 0;

            foreach (var item in Items)
            {
                SpecTotalSQM += item.TotalSQM;
                SpecTotalQty += item.Qty;
                SpecTotalPrice += item.TotalPrice;
            }
        }
    }

    // ==================== INVOICE ITEM MODEL ====================
    public class InvoiceItemModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void Notify(params string[] props)
        {
            foreach (var p in props)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }

        protected bool Set<T>(ref T field, T value, params string[] props)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            Notify(props);
            return true;
        }

        private int _srNo = 1;
        public int SrNo
        {
            get => _srNo;
            set => Set(ref _srNo, value, nameof(SrNo));
        }

        private string _glassRef = "";
        public string GlassRef
        {
            get => _glassRef;
            set => Set(ref _glassRef, value, nameof(GlassRef));
        }

        private double _width1;
        public double Width1
        {
            get => _width1;
            set { Set(ref _width1, value); CalculateSQM(); }
        }

        private double _height1;
        public double Height1
        {
            get => _height1;
            set { Set(ref _height1, value); CalculateSQM(); }
        }

        private double _width2;
        public double Width2
        {
            get => _width2;
            set { Set(ref _width2, value); CalculateSQM(); }
        }

        private double _height2;
        public double Height2
        {
            get => _height2;
            set { Set(ref _height2, value); CalculateSQM(); }
        }

        private int _qty = 1;
        public int Qty
        {
            get => _qty;
            set { Set(ref _qty, value); CalculateSQM(); }
        }

        private double _sqm;
        public double SQM
        {
            get => _sqm;
            set => Set(ref _sqm, value, nameof(SQM));
        }

        private double _totalSQM;
        public double TotalSQM
        {
            get => _totalSQM;
            set => Set(ref _totalSQM, value, nameof(TotalSQM));
        }

        private double _lm;
        public double LM
        {
            get => _lm;
            set => Set(ref _lm, value, nameof(LM));
        }

        private double _totalLM;
        public double TotalLM
        {
            get => _totalLM;
            set => Set(ref _totalLM, value, nameof(TotalLM));
        }

        private double _price;
        public double Price
        {
            get => _price;
            set { Set(ref _price, value); CalculateTotalPrice(); }
        }

        private double _totalPrice;
        public double TotalPrice
        {
            get => _totalPrice;
            set => Set(ref _totalPrice, value, nameof(TotalPrice));
        }

        public Action OnCalculationChanged { get; set; }

        private void CalculateSQM()
        {
            double avgWidth = (_width1 + _width2) / 2;
            double avgHeight = (_height1 + _height2) / 2;
            SQM = Math.Round((avgWidth * avgHeight) / 1000000, 4);
            TotalSQM = Math.Round(SQM * _qty, 4);

            LM = Math.Round((_width1 + _width2 + _height1 + _height2) * 2 / 1000, 4);
            TotalLM = Math.Round(LM * _qty, 4);

            CalculateTotalPrice();
        }

        private void CalculateTotalPrice()
        {
            TotalPrice = Math.Round(TotalSQM * _price, 2);
            OnCalculationChanged?.Invoke();
        }
    }
}