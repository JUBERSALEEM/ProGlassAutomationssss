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
            // Recalculate when any of these properties change
            if (e.PropertyName == nameof(InvoiceItemModel.SQM) ||
                e.PropertyName == nameof(InvoiceItemModel.TotalSQM) ||
                e.PropertyName == nameof(InvoiceItemModel.LM) ||
                e.PropertyName == nameof(InvoiceItemModel.TotalLM) ||
                e.PropertyName == nameof(InvoiceItemModel.TotalPrice) ||
                e.PropertyName == nameof(InvoiceItemModel.Qty))
            {
                CalculateSpecTotals();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

                    // Update all items with new base price
                    foreach (var item in Items)
                    {
                        item.Price = value;
                    }
                }
            }
        }

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