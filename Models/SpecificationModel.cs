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
            Items.CollectionChanged += Items_CollectionChanged;
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

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // ==================== PATCH 7: Added Id Property ====================
        private int _id = 0;
        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
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
            }
        }

        public ObservableCollection<InvoiceItemModel> Items { get; }

        // ==================== NEW: Module Type ====================
        // SGU, DGU, LAM
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

        // ==================== NEW: Work Type ====================
        // Annealed, FT Glass
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

        // ==================== NEW: U-Insert ====================
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

        // ==================== NEW: DGU Specific Properties ====================

        // Outer Glass
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
            set
            {
                _outerPrice = value;
                OnPropertyChanged();
            }
        }

        // Inner Glass
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
            set
            {
                _innerPrice = value;
                OnPropertyChanged();
            }
        }

        // Air Spacer
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
            set
            {
                _aspPrice = value;
                OnPropertyChanged();
            }
        }

        // ==================== NEW: LAM Specific Properties ====================

        // PVB Layer
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
            set
            {
                _pvbPrice = value;
                OnPropertyChanged();
            }
        }

        // ==================== NEW: Generated Description ====================
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

        // Base Price - updates all items when changed
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

        // Surcharge Percent - updates all items when changed
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

                    foreach (var item in Items)
                    {
                        item.SurchargePercent = value;
                    }
                }
            }
        }

        public double SurchargeThreshold => 4;

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
                }
            }
        }

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
    }
}