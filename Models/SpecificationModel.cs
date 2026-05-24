using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ProGlassAutomation.Models
{
    public class SpecificationModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public SpecificationModel()
        {
            Items = new ObservableCollection<InvoiceItemModel>();
            OtherCharges = new ObservableCollection<OtherChargeModel>();
            Items.CollectionChanged += Items_CollectionChanged;
            OtherCharges.CollectionChanged += OtherCharges_CollectionChanged;
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (InvoiceItemModel item in e.NewItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (InvoiceItemModel item in e.OldItems)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
            }
            CalculateSpecTotals();
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InvoiceItemModel.SQM) ||
                e.PropertyName == nameof(InvoiceItemModel.TotalSQM) ||
                e.PropertyName == nameof(InvoiceItemModel.LM) ||
                e.PropertyName == nameof(InvoiceItemModel.TotalLM) ||
                e.PropertyName == nameof(InvoiceItemModel.TotalPrice) ||
                e.PropertyName == nameof(InvoiceItemModel.Qty) ||
                e.PropertyName == nameof(InvoiceItemModel.DisplayPrice) ||
                e.PropertyName == nameof(InvoiceItemModel.FinalPrice))
            {
                CalculateSpecTotals();
            }
        }

        private void OtherCharges_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (OtherChargeModel charge in e.NewItems)
                {
                    charge.PropertyChanged += Charge_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (OtherChargeModel charge in e.OldItems)
                {
                    charge.PropertyChanged -= Charge_PropertyChanged;
                }
            }
            CalculateOtherChargesTotal();
        }

        private void Charge_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OtherChargeModel.Amount))
            {
                CalculateOtherChargesTotal();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // ==================== PARENT INVOICE REFERENCE ====================
        public ProformaInvoiceModel? Invoice { get; set; }

        // ==================== ID & INDEX ====================
        private int _id = 0;
        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SpecIndex));
                OnPropertyChanged(nameof(ShortName));
            }
        }

        private string _specificationName = "";
        public string SpecificationName
        {
            get => _specificationName;
            set
            {
                _specificationName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShortName));
            }
        }

        public ObservableCollection<InvoiceItemModel> Items { get; set; } = new();

        // ==================== SPEC INDEX FOR COMBOBOX ====================
        public int SpecIndex => Id;

        // ==================== SHORT NAME FOR UI ====================
        public string ShortName
        {
            get
            {
                // Try to find index in parent invoice
                if (Invoice?.Specifications != null)
                {
                    int index = Invoice.Specifications.IndexOf(this);
                    if (index >= 0)
                    {
                        return $"Spec {index + 1}";
                    }
                }
                // Fallback: extract number from SpecificationName
                if (!string.IsNullOrEmpty(SpecificationName))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(SpecificationName, @"(\d+)");
                    if (match.Success)
                        return $"Spec {match.Groups[1].Value}";
                }
                return "Spec";
            }
        }

        // ==================== MODULE TYPE ====================
        private string _moduleType = "SGU";
        public string ModuleType
        {
            get => _moduleType;
            set
            {
                _moduleType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GeneratedDescription));
            }
        }

        // ==================== WORK TYPE ====================
        private string _workType = "Annealed";
        public string WorkType
        {
            get => _workType;
            set
            {
                _workType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GeneratedDescription));
            }
        }

        // ==================== U-INSERT ====================
        private bool _includeInSpec = true;
        public bool IncludeInSpec
        {
            get => _includeInSpec;
            set
            {
                _includeInSpec = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GeneratedDescription));
            }
        }

        // ==================== DGU PROPERTIES ====================
        private string _outerThickness = "6mm";
        public string OuterThickness
        {
            get => _outerThickness;
            set
            {
                _outerThickness = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GeneratedDescription));
            }
        }

        private string _outerColor = "Clear";
        public string OuterColor
        {
            get => _outerColor;
            set
            {
                _outerColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GeneratedDescription));
            }
        }

        private string _outerPrice = "0";
        public string OuterPrice
        {
            get => _outerPrice;
            set { _outerPrice = value; OnPropertyChanged(); }
        }

        private string _innerThickness = "6mm";
        public string InnerThickness
        {
            get => _innerThickness;
            set
            {
                _innerThickness = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GeneratedDescription));
            }
        }

        private string _innerColor = "Clear";
        public string InnerColor
        {
            get => _innerColor;
            set
            {
                _innerColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GeneratedDescription));
            }
        }

        private string _innerPrice = "0";
        public string InnerPrice
        {
            get => _innerPrice;
            set { _innerPrice = value; OnPropertyChanged(); }
        }

        private string _spacerThickness = "12mm";
        public string SpacerThickness
        {
            get => _spacerThickness;
            set
            {
                _spacerThickness = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GeneratedDescription));
            }
        }

        private string _aspPrice = "0";
        public string ASPPrice
        {
            get => _aspPrice;
            set { _aspPrice = value; OnPropertyChanged(); }
        }

        // ==================== LAM PROPERTIES ====================
        private string _pvbThickness = "0.76mm";
        public string PVBThickness
        {
            get => _pvbThickness;
            set
            {
                _pvbThickness = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GeneratedDescription));
            }
        }

        private string _pvbColor = "Clear";
        public string PVBColor
        {
            get => _pvbColor;
            set
            {
                _pvbColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GeneratedDescription));
            }
        }

        private string _pvbPrice = "0";
        public string PVBPrice
        {
            get => _pvbPrice;
            set { _pvbPrice = value; OnPropertyChanged(); }
        }

        // ==================== OTHER CHARGES ====================
        public ObservableCollection<OtherChargeModel> OtherCharges { get; }

        private double _otherChargesTotal = 0;
        public double OtherChargesTotal
        {
            get => _otherChargesTotal;
            private set
            {
                if (_otherChargesTotal != value)
                {
                    _otherChargesTotal = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SpecTotalPriceWithOtherCharges));
                }
            }
        }

        public double SpecTotalPriceWithOtherCharges => Math.Round(SpecTotalPrice + OtherChargesTotal, 2);

        public void CalculateOtherChargesTotal()
        {
            double total = OtherCharges.Sum(c => c.Amount);
            OtherChargesTotal = Math.Round(total, 2);
        }

        // ==================== GENERATED DESCRIPTION ====================
        public string GeneratedDescription
        {
            get
            {
                return ModuleType switch
                {
                    "DGU" => GenerateDGUDescription(),
                    "LAM" => GenerateLAMDescription(),
                    _ => GenerateSGUDescription()
                };
            }
        }

        private string GenerateDGUDescription()
        {
            string workTypeShort = WorkType == "FT Glass" ? "FT Glass" : "Annealed";
            string uInsertText = IncludeInSpec ? "with U-Insert" : "";
            return $"{OuterThickness} {OuterColor} {workTypeShort} + {SpacerThickness} ASP {uInsertText} + {InnerThickness} {InnerColor} {workTypeShort}";
        }

        private string GenerateLAMDescription()
        {
            string workTypeShort = WorkType == "FT Glass" ? "FT Glass" : "Annealed";
            return $"{OuterThickness} {OuterColor} {workTypeShort} + {PVBThickness} PVB ({PVBColor}) + {InnerThickness} {InnerColor} {workTypeShort}";
        }

        private string GenerateSGUDescription()
        {
            string workTypeShort = WorkType == "FT Glass" ? "FT Glass" : "Annealed";
            return $"{OuterThickness} {OuterColor} {workTypeShort}";
        }

        // ==================== BASE PRICE ====================
        private double _basePrice = 0;
        public double BasePrice
        {
            get => _basePrice;
            set
            {
                if (_basePrice != value)
                {
                    _basePrice = value;
                    OnPropertyChanged();
                    foreach (var item in Items)
                    {
                        item.Price = value;
                    }
                }
            }
        }

        // ==================== SURCHARGE PERCENT (FIXED) ====================
        private double _surchargePercent = 20;
        public double SurchargePercent
        {
            get => _surchargePercent;
            set
            {
                if (_surchargePercent != value)
                {
                    _surchargePercent = value;
                    OnPropertyChanged();
                    // ✅ FIX: Recalculate all items when surcharge changes
                    RecalculateAllItems();
                }
            }
        }

        // ✅ NEW: Method to recalculate all items when surcharge changes
        private void RecalculateAllItems()
        {
            foreach (var item in Items)
            {
                item.Specification = this;  // Ensure link
                item.NotifySurchargeChanged();
            }
            CalculateSpecTotals();
        }

        public double SurchargeThreshold => 4;

        // ==================== TOTALS ====================
        private double _specTotalSQM = 0;
        public double SpecTotalSQM
        {
            get => _specTotalSQM;
            private set
            {
                if (_specTotalSQM != value)
                {
                    _specTotalSQM = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _specTotalLM = 0;
        public double SpecTotalLM
        {
            get => _specTotalLM;
            private set
            {
                if (_specTotalLM != value)
                {
                    _specTotalLM = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _specTotalQty = 0;
        public int SpecTotalQty
        {
            get => _specTotalQty;
            private set
            {
                if (_specTotalQty != value)
                {
                    _specTotalQty = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _specTotalPrice = 0;
        public double SpecTotalPrice
        {
            get => _specTotalPrice;
            private set
            {
                if (_specTotalPrice != value)
                {
                    _specTotalPrice = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SpecTotalPriceWithOtherCharges));
                }
            }
        }

        // ==================== CALCULATE SPEC TOTALS ====================
        public void CalculateSpecTotals()
        {
            double sqm = 0, lm = 0;
            int qty = 0;
            double price = 0;

            foreach (var item in Items)
            {
                sqm += item.TotalSQM;
                lm += item.TotalLM;
                qty += item.Qty;
                price += item.TotalPrice;
            }

            SpecTotalSQM = Math.Round(sqm, 4);
            SpecTotalLM = Math.Round(lm, 4);
            SpecTotalQty = qty;
            SpecTotalPrice = Math.Round(price, 2);
        }

        // ==================== ADD ITEM METHOD ====================
        public InvoiceItemModel AddItem()
        {
            var item = new InvoiceItemModel
            {
                Specification = this,  // ✅ Link to parent spec
                SrNo = Items.Count + 1
            };
            Items.Add(item);
            return item;
        }

        // ==================== CALCULATE TOTALS (Wrapper for ViewModel compatibility) ====================
        public void CalculateTotals()
        {
            CalculateSpecTotals();
            CalculateOtherChargesTotal();
        }
    }
}