using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ProGlassAutomation.Models
{
    public class InvoiceItemModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

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
                }
            }
        }

        // ==================== PARENT SPECIFICATION REFERENCE (PATCH 10) ====================
        [JsonIgnore]
        public SpecificationModel? Specification { get; set; }

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

        // ==================== ID & SR NO ====================
        private int _id = 0;
        public int Id { get => _id; set => SetProperty(ref _id, value); }

        private int _srNo = 0;
        public int SrNo { get => _srNo; set => SetProperty(ref _srNo, value); }

        // ==================== GLASS REFERENCE ====================
        private string _glassRef = "";
        public string GlassRef { get => _glassRef; set => SetProperty(ref _glassRef, value); }

        // ==================== DIMENSIONS ====================
        private double _width1 = 0;
        public double Width1 { get => _width1; set { if (SetProperty(ref _width1, value)) Recalculate(); } }

        private double _height1 = 0;
        public double Height1 { get => _height1; set { if (SetProperty(ref _height1, value)) Recalculate(); } }

        private double _width2 = 0;
        public double Width2 { get => _width2; set { if (SetProperty(ref _width2, value)) Recalculate(); } }

        private double _height2 = 0;
        public double Height2 { get => _height2; set { if (SetProperty(ref _height2, value)) Recalculate(); } }

        // ==================== QUANTITY ====================
        private int _qty = 1;
        public int Qty { get => _qty; set { if (SetProperty(ref _qty, value)) { Recalculate(); OnPropertyChanged(nameof(RemainingQty)); } } }

        private int _deliveredQty = 0;
        public int DeliveredQty { get => _deliveredQty; set { if (SetProperty(ref _deliveredQty, value)) OnPropertyChanged(nameof(RemainingQty)); } }

        public int RemainingQty => Qty - DeliveredQty;

        // ==================== PRICE & CALCULATIONS ====================
        private double _price = 0;
        public double Price { get => _price; set { if (SetProperty(ref _price, value)) Recalculate(); } }

        private double _surchargePercent = 20;
        public double SurchargePercent { get => _surchargePercent; set { if (SetProperty(ref _surchargePercent, value)) Recalculate(); } }

        private double _surchargeAmount = 0;
        public double SurchargeAmount { get => _surchargeAmount; private set => SetProperty(ref _surchargeAmount, value); }

        // ==================== CALCULATED VALUES ====================
        private double _sqm1 = 0;
        public double SQM1 { get => _sqm1; private set => SetProperty(ref _sqm1, value); }

        private double _sqm2 = 0;
        public double SQM2 { get => _sqm2; private set => SetProperty(ref _sqm2, value); }

        private double _sqm = 0;
        public double TotalSQM { get => _sqm; private set => SetProperty(ref _sqm, value); }

        private double _lm1 = 0;
        public double LM1 { get => _lm1; private set => SetProperty(ref _lm1, value); }

        private double _lm2 = 0;
        public double LM2 { get => _lm2; private set => SetProperty(ref _lm2, value); }

        private double _lm = 0;
        public double LM { get => _lm; private set => SetProperty(ref _lm, value); }

        private double _totalLM = 0;
        public double TotalLM { get => _totalLM; private set => SetProperty(ref _totalLM, value); }

        private double _totalPrice = 0;
        public double TotalPrice { get => _totalPrice; private set => SetProperty(ref _totalPrice, value); }

        private double _displayPrice = 0;
        public double DisplayPrice { get => _displayPrice; private set => SetProperty(ref _displayPrice, value); }

        private double _finalPrice = 0;
        public double FinalPrice { get => _finalPrice; private set => SetProperty(ref _finalPrice, value); }

        // ==================== GLASS TYPES ====================
        private string _glassGlazingType = "";
        public string GlassGlazingType { get => _glassGlazingType; set => SetProperty(ref _glassGlazingType, value); }

        private string _glassType = "";
        public string GlassType { get => _glassType; set => SetProperty(ref _glassType, value); }

        // ==================== RECALCULATE ====================
        public void Recalculate()
        {
            if (_isBulkUpdating) return;

            double w1 = Width1 > 0 ? Width1 : 0;
            double h1 = Height1 > 0 ? Height1 : 0;
            double w2 = Width2 > 0 ? Width2 : 0;
            double h2 = Height2 > 0 ? Height2 : 0;
            double q = Qty > 0 ? Qty : 1;

            // SQM calculations
            double sqm1Val = (w1 * h1) / 1000000.0;
            double sqm2Val = (w2 * h2) / 1000000.0;
            SQM1 = Math.Round(sqm1Val, 4);
            SQM2 = Math.Round(sqm2Val, 4);
            TotalSQM = Math.Round((SQM1 + SQM2) * q, 4);

            // LM calculations (perimeter)
            double lm1Val = 2 * ((w1 / 1000.0) + (h1 / 1000.0));
            double lm2Val = 2 * ((w2 / 1000.0) + (h2 / 1000.0));
            LM1 = Math.Round(lm1Val, 4);
            LM2 = Math.Round(lm2Val, 4);
            LM = LM1 + LM2;
            TotalLM = Math.Round(LM * q, 4);

            // Price calculations
            double p = Price > 0 ? Price : 0;
            double sp = SurchargePercent > 0 ? SurchargePercent : 0;

            double surcharge = Math.Round((p * sp) / 100, 4);
            SurchargeAmount = surcharge;

            double dp = p + surcharge;
            DisplayPrice = dp;

            double tp = dp * q;
            TotalPrice = tp;
            FinalPrice = tp;
        }

        // ==================== NOTIFY SURCHARGE CHANGED ====================
        public void NotifySurchargeChanged()
        {
            Recalculate();
        }

        // ==================== BULK OPERATIONS (PATCH 8) ====================
        public void BeginBulkUpdate()
        {
            IsBulkUpdating = true;
        }

        public void EndBulkUpdate()
        {
            IsBulkUpdating = false;
            Recalculate();
        }

        public IDisposable BulkUpdateScope()
        {
            return new ItemBulkUpdateScope(this);
        }

        private class ItemBulkUpdateScope : BulkUpdateScope
        {
            private readonly InvoiceItemModel _item;

            public ItemBulkUpdateScope(InvoiceItemModel item)
            {
                _item = item;
                _item.BeginBulkUpdate();
            }

            protected override void OnDispose()
            {
                _item.EndBulkUpdate();
            }
        }

        // ==================== DEEP CLONE (PATCH 15) ====================
        public InvoiceItemModel DeepClone()
        {
            var clone = new InvoiceItemModel
            {
                Id = Id,
                SrNo = SrNo,
                GlassRef = GlassRef,
                Width1 = Width1,
                Height1 = Height1,
                Width2 = Width2,
                Height2 = Height2,
                Qty = Qty,
                DeliveredQty = DeliveredQty,
                Price = Price,
                SurchargePercent = SurchargePercent,
                GlassGlazingType = GlassGlazingType,
                GlassType = GlassType
            };

            clone.Recalculate();
            return clone;
        }

        // ==================== COPY FROM OTHER ITEM ====================
        public void CopyFrom(InvoiceItemModel other)
        {
            if (other == null) return;

            using (BulkUpdateScope())
            {
                GlassRef = other.GlassRef;
                Width1 = other.Width1;
                Height1 = other.Height1;
                Width2 = other.Width2;
                Height2 = other.Height2;
                Qty = other.Qty;
                Price = other.Price;
                SurchargePercent = other.SurchargePercent;
                GlassGlazingType = other.GlassGlazingType;
                GlassType = other.GlassType;
            }
        }

        // ==================== VALIDATION (PATCH 17) ====================
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (Width1 <= 0)
                result.AddError("Width1", "Width must be greater than 0");

            if (Height1 <= 0)
                result.AddError("Height1", "Height must be greater than 0");

            if (Qty <= 0)
                result.AddError("Qty", "Quantity must be greater than 0");

            if (Price < 0)
                result.AddError("Price", "Price cannot be negative");

            if (SurchargePercent < 0)
                result.AddError("SurchargePercent", "Surcharge cannot be negative");

            return result;
        }

        public bool IsValid => Validate().IsValid;

        // ==================== GET VALUES FOR OTHER CHARGES (PATCH 13) ====================
        public double GetValueForChargeType(string chargeType)
        {
            return chargeType?.ToLower() switch
            {
                "sqm1" => SQM1,
                "sqm2" => SQM2,
                "sqm" => TotalSQM,
                "lm1" => LM1,
                "lm2" => LM2,
                "lm" => TotalLM,
                "qty" => Qty,
                _ => 1
            };
        }
    }
}