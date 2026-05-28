using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace ProGlassAutomation.Models
{
    public class SpecificationModel : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private bool _disposed;

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
                        CalculateSpecTotals();
                }
            }
        }

        // ==================== CONSTRUCTOR ====================
        public SpecificationModel()
        {
            Items = new ObservableCollection<InvoiceItemModel>();
            OtherCharges = new ObservableCollection<OtherChargeModel>();
            Items.CollectionChanged += Items_CollectionChanged;
            OtherCharges.CollectionChanged += OtherCharges_CollectionChanged;
        }

        // ==================== DISPOSAL (PATCH 6) ====================
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
                Items.CollectionChanged -= Items_CollectionChanged;
                OtherCharges.CollectionChanged -= OtherCharges_CollectionChanged;

                foreach (var item in Items)
                {
                    if (item != null)
                    {
                        item.PropertyChanged -= Item_PropertyChanged;
                        item.Specification = null;
                    }
                }

                foreach (var charge in OtherCharges)
                {
                    charge.PropertyChanged -= Charge_PropertyChanged;
                }

                Items.Clear();
                OtherCharges.Clear();
            }

            _disposed = true;
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

        // ==================== COLLECTION HANDLERS ====================
        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (IsBulkUpdating) return;

            if (e.NewItems != null)
            {
                foreach (InvoiceItemModel item in e.NewItems)
                {
                    if (item != null)
                    {
                        item.PropertyChanged += Item_PropertyChanged;
                        item.Specification = this;
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (InvoiceItemModel item in e.OldItems)
                {
                    if (item != null)
                    {
                        item.PropertyChanged -= Item_PropertyChanged;
                        item.Specification = null;
                    }
                }
            }

            CalculateSpecTotals();
            RenumberItems();
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (IsBulkUpdating) return;

            if (e.PropertyName == nameof(InvoiceItemModel.SQM1) ||
                e.PropertyName == nameof(InvoiceItemModel.SQM2) ||
                e.PropertyName == nameof(InvoiceItemModel.TotalSQM) ||
                e.PropertyName == nameof(InvoiceItemModel.LM) ||
                e.PropertyName == nameof(InvoiceItemModel.TotalLM) ||
                e.PropertyName == nameof(InvoiceItemModel.LM1) ||
                e.PropertyName == nameof(InvoiceItemModel.LM2) ||
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
            if (IsBulkUpdating) return;

            if (e.NewItems != null)
            {
                foreach (OtherChargeModel charge in e.NewItems)
                {
                    if (charge != null)
                        charge.PropertyChanged += Charge_PropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (OtherChargeModel charge in e.OldItems)
                {
                    if (charge != null)
                        charge.PropertyChanged -= Charge_PropertyChanged;
                }
            }

            CalculateOtherChargesTotal();
        }

        private void Charge_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (IsBulkUpdating) return;

            if (e.PropertyName == nameof(OtherChargeModel.Amount))
                CalculateOtherChargesTotal();
        }

        // ==================== PARENT INVOICE REFERENCE ====================
        [JsonIgnore]
        public ProformaInvoiceModel? Invoice { get; set; }

        // ==================== ID & INDEX ====================
        private int _id = 0;
        public int Id { get => _id; set { _id = value; OnPropertyChanged(); OnPropertyChanged(nameof(SpecIndex)); OnPropertyChanged(nameof(ShortName)); } }

        private string _specificationName = "";
        public string SpecificationName { get => _specificationName; set { _specificationName = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShortName)); } }

        public ObservableCollection<InvoiceItemModel> Items { get; set; } = new();

        public int SpecIndex => Id;

        public string ShortName
        {
            get
            {
                if (Invoice?.Specifications != null)
                {
                    int index = Invoice.Specifications.IndexOf(this);
                    if (index >= 0) return $"Spec {index + 1}";
                }
                if (!string.IsNullOrEmpty(SpecificationName))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(SpecificationName, @"(\d+)");
                    if (match.Success) return $"Spec {match.Groups[1].Value}";
                }
                return "Spec";
            }
        }

        // ==================== MODULE TYPE ====================
        private string _moduleType = "SGU";
        public string ModuleType { get => _moduleType; set { _moduleType = value; OnPropertyChanged(); OnPropertyChanged(nameof(GeneratedDescription)); } }

        // ==================== WORK TYPE ====================
        private string _workType = "Annealed";
        public string WorkType { get => _workType; set { _workType = value; OnPropertyChanged(); OnPropertyChanged(nameof(GeneratedDescription)); } }

        // ==================== U-INSERT ====================
        private bool _includeInSpec = true;
        public bool IncludeInSpec { get => _includeInSpec; set { _includeInSpec = value; OnPropertyChanged(); OnPropertyChanged(nameof(GeneratedDescription)); } }

        // ==================== DGU PROPERTIES ====================
        private string _outerThickness = "6mm";
        public string OuterThickness { get => _outerThickness; set { _outerThickness = value; OnPropertyChanged(); OnPropertyChanged(nameof(GeneratedDescription)); } }

        private string _outerColor = "Clear";
        public string OuterColor { get => _outerColor; set { _outerColor = value; OnPropertyChanged(); OnPropertyChanged(nameof(GeneratedDescription)); } }

        private string _outerPrice = "0";
        public string OuterPrice { get => _outerPrice; set { _outerPrice = value; OnPropertyChanged(); } }

        private string _innerThickness = "6mm";
        public string InnerThickness { get => _innerThickness; set { _innerThickness = value; OnPropertyChanged(); OnPropertyChanged(nameof(GeneratedDescription)); } }

        private string _innerColor = "Clear";
        public string InnerColor { get => _innerColor; set { _innerColor = value; OnPropertyChanged(); OnPropertyChanged(nameof(GeneratedDescription)); } }

        private string _innerPrice = "0";
        public string InnerPrice { get => _innerPrice; set { _innerPrice = value; OnPropertyChanged(); } }

        private string _spacerThickness = "12mm";
        public string SpacerThickness { get => _spacerThickness; set { _spacerThickness = value; OnPropertyChanged(); OnPropertyChanged(nameof(GeneratedDescription)); } }

        private string _aspPrice = "0";
        public string ASPPrice { get => _aspPrice; set { _aspPrice = value; OnPropertyChanged(); } }

        // ==================== LAM PROPERTIES ====================
        private string _pvbThickness = "0.76mm";
        public string PVBThickness { get => _pvbThickness; set { _pvbThickness = value; OnPropertyChanged(); OnPropertyChanged(nameof(GeneratedDescription)); } }

        private string _pvbColor = "Clear";
        public string PVBColor { get => _pvbColor; set { _pvbColor = value; OnPropertyChanged(); OnPropertyChanged(nameof(GeneratedDescription)); } }

        private string _pvbPrice = "0";
        public string PVBPrice { get => _pvbPrice; set { _pvbPrice = value; OnPropertyChanged(); } }

        // ==================== OTHER CHARGES ====================
        public ObservableCollection<OtherChargeModel> OtherCharges { get; }

        private double _otherChargesTotal = 0;
        public double OtherChargesTotal
        {
            get => _otherChargesTotal;
            private set { if (_otherChargesTotal != value) { _otherChargesTotal = value; OnPropertyChanged(); OnPropertyChanged(nameof(SpecTotalPriceWithOtherCharges)); } }
        }

        public double SpecTotalPriceWithOtherCharges => Math.Round(SpecTotalPrice + OtherChargesTotal, 2);

        public void CalculateOtherChargesTotal()
        {
            if (IsBulkUpdating) return;
            double total = OtherCharges.Sum(c => c?.Amount ?? 0);
            OtherChargesTotal = Math.Round(total, 2);
        }

        // ==================== GENERATED DESCRIPTION ====================
        public string GeneratedDescription
        {
            get
            {
                return ModuleType switch
                {
                    "DGU" => $"{OuterThickness} {OuterColor} {(WorkType == "FT Glass" ? "FT Glass" : "Annealed")} + {SpacerThickness} ASP {(IncludeInSpec ? "with U-Insert" : "")} + {InnerThickness} {InnerColor} {(WorkType == "FT Glass" ? "FT Glass" : "Annealed")}",
                    "LAM" => $"{OuterThickness} {OuterColor} {(WorkType == "FT Glass" ? "FT Glass" : "Annealed")} + {PVBThickness} PVB ({PVBColor}) + {PVBThickness}/{InnerThickness} {InnerColor} {(WorkType == "FT Glass" ? "FT Glass" : "Annealed")}",
                    _ => $"{OuterThickness} {OuterColor} {(WorkType == "FT Glass" ? "FT Glass" : "Annealed")}"
                };
            }
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

        // ==================== SURCHARGE PERCENT ====================
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
                    RecalculateAllItems();
                }
            }
        }

        private void RecalculateAllItems()
        {
            foreach (var item in Items)
            {
                item.NotifySurchargeChanged();
            }
            CalculateSpecTotals();
        }

        public double SurchargeThreshold => 4;

        // ==================== TOTALS ====================
        private double _specTotalSQM1 = 0;
        public double SpecTotalSQM1 { get => _specTotalSQM1; private set { if (_specTotalSQM1 != value) { _specTotalSQM1 = value; OnPropertyChanged(); } } }

        private double _specTotalSQM2 = 0;
        public double SpecTotalSQM2 { get => _specTotalSQM2; private set { if (_specTotalSQM2 != value) { _specTotalSQM2 = value; OnPropertyChanged(); } } }

        private double _specTotalSQM = 0;
        public double SpecTotalSQM { get => _specTotalSQM; private set { if (_specTotalSQM != value) { _specTotalSQM = value; OnPropertyChanged(); } } }

        private double _specTotalLM = 0;
        public double SpecTotalLM { get => _specTotalLM; private set { if (_specTotalLM != value) { _specTotalLM = value; OnPropertyChanged(); } } }

        private double _specTotalLM1 = 0;
        public double SpecTotalLM1 { get => _specTotalLM1; private set { if (_specTotalLM1 != value) { _specTotalLM1 = value; OnPropertyChanged(); } } }

        private double _specTotalLM2 = 0;
        public double SpecTotalLM2 { get => _specTotalLM2; private set { if (_specTotalLM2 != value) { _specTotalLM2 = value; OnPropertyChanged(); } } }

        private int _specTotalQty = 0;
        public int SpecTotalQty { get => _specTotalQty; private set { if (_specTotalQty != value) { _specTotalQty = value; OnPropertyChanged(); } } }

        private double _specTotalPrice = 0;
        public double SpecTotalPrice { get => _specTotalPrice; private set { if (_specTotalPrice != value) { _specTotalPrice = value; OnPropertyChanged(); OnPropertyChanged(nameof(SpecTotalPriceWithOtherCharges)); } } }

        // ==================== CALCULATE SPEC TOTALS ====================
        public void CalculateSpecTotals()
        {
            if (IsBulkUpdating) return;

            double sqm1 = 0, sqm2 = 0, sqm = 0, lm = 0, lm1 = 0, lm2 = 0;
            int qty = 0;
            double price = 0;

            foreach (var item in Items)
            {
                if (item == null) continue;

                sqm1 += item.SQM1 * item.Qty;
                sqm2 += item.SQM2 * item.Qty;
                sqm += item.TotalSQM;
                lm += item.TotalLM;
                lm1 += item.LM1 * item.Qty;
                lm2 += item.LM2 * item.Qty;
                qty += item.Qty;
                price += item.TotalPrice;
            }

            SpecTotalSQM1 = Math.Round(sqm1, 4);
            SpecTotalSQM2 = Math.Round(sqm2, 4);
            SpecTotalSQM = Math.Round(sqm, 4);
            SpecTotalLM = Math.Round(lm, 4);
            SpecTotalLM1 = Math.Round(lm1, 4);
            SpecTotalLM2 = Math.Round(lm2, 4);
            SpecTotalQty = qty;
            SpecTotalPrice = Math.Round(price, 2);
        }

        // ==================== RENUMBER ITEMS (PATCH 9) ====================
        public void RenumberItems()
        {
            int srNo = 1;
            foreach (var item in Items)
            {
                item.SrNo = srNo;
                srNo++;
            }
        }

        public void RenumberAllItemsGlobally()
        {
            if (Invoice?.Specifications == null) return;

            int srNo = 1;
            foreach (var spec in Invoice.Specifications)
            {
                foreach (var item in spec.Items)
                {
                    item.SrNo = srNo;
                    srNo++;
                }
            }
        }

        // ==================== ADD ITEM METHOD ====================
        public InvoiceItemModel AddItem(int nextSrNo)
        {
            var item = new InvoiceItemModel
            {
                Specification = this,
                SrNo = nextSrNo
            };
            Items.Add(item);
            return item;
        }

        // ==================== REMOVE ITEM METHOD ====================
        public void RemoveItem(InvoiceItemModel item)
        {
            if (item != null && Items.Contains(item))
            {
                item.PropertyChanged -= Item_PropertyChanged;
                Items.Remove(item);
                CalculateSpecTotals();
            }
        }

        // ==================== CLEAR ALL ITEMS (PATCH 6) ====================
        public void ClearAllItems()
        {
            foreach (var item in Items)
            {
                if (item != null)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                    item.Specification = null;
                }
            }
            Items.Clear();
            CalculateSpecTotals();
        }

        // ==================== ADD/REMOVE CHARGE ====================
        public OtherChargeModel AddCharge(string name = null, string type = "lm")
        {
            var charge = new OtherChargeModel
            {
                Name = name ?? $"Charge {OtherCharges.Count + 1}",
                Type = type,
                LinkedSpecIndices = Id.ToString()
            };
            OtherCharges.Add(charge);
            return charge;
        }

        public void RemoveCharge(OtherChargeModel charge)
        {
            if (charge != null && OtherCharges.Contains(charge))
            {
                charge.PropertyChanged -= Charge_PropertyChanged;
                OtherCharges.Remove(charge);
                CalculateOtherChargesTotal();
            }
        }

        // ==================== CLEAR ALL CHARGES (PATCH 6) ====================
        public void ClearAllCharges()
        {
            foreach (var charge in OtherCharges)
            {
                if (charge != null)
                    charge.PropertyChanged -= Charge_PropertyChanged;
            }
            OtherCharges.Clear();
            CalculateOtherChargesTotal();
        }

        // ==================== CALCULATE TOTALS ====================
        public void CalculateTotals()
        {
            CalculateSpecTotals();
            CalculateOtherChargesTotal();
        }

        // ==================== BULK OPERATIONS (PATCH 8) ====================
        public void BeginBulkUpdate()
        {
            IsBulkUpdating = true;
        }

        public void EndBulkUpdate()
        {
            IsBulkUpdating = false;
            CalculateSpecTotals();
            CalculateOtherChargesTotal();
        }

        public IDisposable BulkUpdateScope()
        {
            return new SpecBulkUpdateScope(this);
        }

        private class SpecBulkUpdateScope : IDisposable
        {
            private readonly SpecificationModel _spec;

            public SpecBulkUpdateScope(SpecificationModel spec)
            {
                _spec = spec;
                _spec.BeginBulkUpdate();
            }

            public void Dispose()
            {
                _spec.EndBulkUpdate();
            }
        }

        // ==================== DEEP CLONE (PATCH 15) ====================
        public SpecificationModel DeepClone()
        {
            var clone = new SpecificationModel
            {
                Id = Id,
                SpecificationName = SpecificationName,
                ModuleType = ModuleType,
                WorkType = WorkType,
                IncludeInSpec = IncludeInSpec,
                OuterThickness = OuterThickness,
                OuterColor = OuterColor,
                OuterPrice = OuterPrice,
                InnerThickness = InnerThickness,
                InnerColor = InnerColor,
                InnerPrice = InnerPrice,
                SpacerThickness = SpacerThickness,
                ASPPrice = ASPPrice,
                PVBThickness = PVBThickness,
                PVBColor = PVBColor,
                PVBPrice = PVBPrice,
                BasePrice = BasePrice,
                SurchargePercent = SurchargePercent
            };

            // Clone items
            foreach (var item in Items)
            {
                if (item != null)
                {
                    var clonedItem = item.DeepClone();
                    clonedItem.Specification = clone;
                    clone.Items.Add(clonedItem);
                }
            }

            // Clone other charges
            foreach (var charge in OtherCharges)
            {
                if (charge != null)
                {
                    var clonedCharge = charge.DeepClone();
                    clone.OtherCharges.Add(clonedCharge);
                }
            }

            clone.CalculateTotals();
            return clone;
        }

        // ==================== VALIDATION (PATCH 17) ====================
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(SpecificationName))
                result.AddError("SpecificationName", "Specification name is required");

            if (Items == null || Items.Count == 0)
                result.AddError("Items", "At least one item is required");

            int itemIndex = 0;
            foreach (var item in Items)
            {
                if (item == null)
                {
                    result.AddError($"Items[{itemIndex}]", "Item cannot be null");
                    continue;
                }

                if (item.Width1 <= 0 || item.Height1 <= 0)
                    result.AddError($"Items[{itemIndex}].Dimensions", $"Item {item.SrNo}: Width and Height must be greater than 0");

                if (item.Qty <= 0)
                    result.AddError($"Items[{itemIndex}].Qty", $"Item {item.SrNo}: Quantity must be greater than 0");

                itemIndex++;
            }

            return result;
        }

        public bool IsValid => Validate().IsValid;
    }
}