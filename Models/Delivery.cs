using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProGlassAutomation.Models
{
    public class Delivery : INotifyPropertyChanged
    {
        private int _id;
        private int _sourceId;
        private DateTime _date;
        private string _company = "";
        private string _piNumber = "";
        private string _customerReference = "";
        private string _typeOfWork = "";
        private int _orderQty;
        private double _orderSQM;
        private string _salesman = "";
        private string _color = "";
        private string _productionStatus = "";
        private string _status = "";
        private string _notes = "";
        private DateTime _createdDate;
        private DateTime _updatedDate;

        // ✅ Backing field for DeliveryItems
        private ObservableCollection<DeliveryItem> _deliveryItems = new ObservableCollection<DeliveryItem>();

        #region Properties

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public int SourceId
        {
            get => _sourceId;
            set { _sourceId = value; OnPropertyChanged(); }
        }

        public DateTime Date
        {
            get => _date;
            set { _date = value; OnPropertyChanged(); }
        }

        public string Company
        {
            get => _company;
            set { _company = value ?? ""; OnPropertyChanged(); }
        }

        public string PINumber
        {
            get => _piNumber;
            set { _piNumber = value ?? ""; OnPropertyChanged(); }
        }

        public string CustomerReference
        {
            get => _customerReference;
            set { _customerReference = value ?? ""; OnPropertyChanged(); }
        }

        public string TypeOfWork
        {
            get => _typeOfWork;
            set { _typeOfWork = value ?? ""; OnPropertyChanged(); }
        }

        public int OrderQty
        {
            get => _orderQty;
            set { _orderQty = value; OnPropertyChanged(); OnPropertyChanged(nameof(Balance)); OnPropertyChanged(nameof(BalanceSQM)); }
        }

        public double OrderSQM
        {
            get => _orderSQM;
            set { _orderSQM = value; OnPropertyChanged(); OnPropertyChanged(nameof(BalanceSQM)); }
        }

        public string Salesman
        {
            get => _salesman;
            set { _salesman = value ?? ""; OnPropertyChanged(); }
        }

        public string Color
        {
            get => _color;
            set { _color = value ?? ""; OnPropertyChanged(); }
        }

        public string ProductionStatus
        {
            get => _productionStatus;
            set { _productionStatus = value ?? ""; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value ?? ""; OnPropertyChanged(); }
        }

        public string Notes
        {
            get => _notes;
            set { _notes = value ?? ""; OnPropertyChanged(); }
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set { _createdDate = value; OnPropertyChanged(); }
        }

        public DateTime UpdatedDate
        {
            get => _updatedDate;
            set { _updatedDate = value; OnPropertyChanged(); }
        }

        // ✅ Property with notification when collection is replaced
        public ObservableCollection<DeliveryItem> DeliveryItems
        {
            get => _deliveryItems;
            set
            {
                if (_deliveryItems != value)
                {
                    // Unsubscribe from old collection events
                    if (_deliveryItems != null)
                    {
                        _deliveryItems.CollectionChanged -= DeliveryItems_CollectionChanged;
                    }

                    _deliveryItems = value;

                    // Subscribe to new collection events
                    if (_deliveryItems != null)
                    {
                        _deliveryItems.CollectionChanged += DeliveryItems_CollectionChanged;
                    }

                    // Notify all calculated properties
                    OnPropertyChanged(nameof(DeliveryItems));
                    OnPropertyChanged(nameof(TotalDelivered));
                    OnPropertyChanged(nameof(TotalReturned));
                    OnPropertyChanged(nameof(Balance));
                    OnPropertyChanged(nameof(BalanceSQM));
                }
            }
        }

        // ✅ Handle collection changes (add/remove items)
        private void DeliveryItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(TotalDelivered));
            OnPropertyChanged(nameof(TotalReturned));
            OnPropertyChanged(nameof(Balance));
            OnPropertyChanged(nameof(BalanceSQM));
        }

        // ✅ Calculated properties (sum from all DeliveryItems)
        public int TotalDelivered => DeliveryItems?.Sum(x => x?.DeliveredQty ?? 0) ?? 0;
        public int TotalReturned => DeliveryItems?.Sum(x => x?.ReturnedQty ?? 0) ?? 0;
        public int Balance => OrderQty - TotalDelivered + TotalReturned;
        public double BalanceSQM => OrderQty > 0 ? Math.Round((double)Balance / OrderQty * OrderSQM, 2) : 0;

        #endregion

        // ✅ INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Delivery Clone()
        {
            return new Delivery
            {
                Id = this.Id,
                SourceId = this.SourceId,
                Date = this.Date,
                Company = this.Company,
                PINumber = this.PINumber,
                CustomerReference = this.CustomerReference,
                TypeOfWork = this.TypeOfWork,
                OrderQty = this.OrderQty,
                OrderSQM = this.OrderSQM,
                Salesman = this.Salesman,
                Color = this.Color,
                ProductionStatus = this.ProductionStatus,
                Status = this.Status,
                Notes = this.Notes,
                CreatedDate = this.CreatedDate,
                UpdatedDate = this.UpdatedDate
            };
        }
    }

    // ✅ Updated: Added INotifyPropertyChanged to DeliveryItem
    public class DeliveryItem : INotifyPropertyChanged
    {
        private int _id;
        private int _orderId;
        private DateTime _deliveryDate;
        private int _deliveredQty;
        private double _deliveredSQM;
        private int _returnedQty;
        private double _returnedSQM;
        private string _driver = "";
        private string _vehicle = "";
        private string _notes = "";
        private DateTime _createdDate;

        #region Properties

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public int OrderId
        {
            get => _orderId;
            set { _orderId = value; OnPropertyChanged(); }
        }

        public DateTime DeliveryDate
        {
            get => _deliveryDate;
            set { _deliveryDate = value; OnPropertyChanged(); }
        }

        public int DeliveredQty
        {
            get => _deliveredQty;
            set { _deliveredQty = value; OnPropertyChanged(); }
        }

        public double DeliveredSQM
        {
            get => _deliveredSQM;
            set { _deliveredSQM = value; OnPropertyChanged(); }
        }

        public int ReturnedQty
        {
            get => _returnedQty;
            set { _returnedQty = value; OnPropertyChanged(); }
        }

        public double ReturnedSQM
        {
            get => _returnedSQM;
            set { _returnedSQM = value; OnPropertyChanged(); }
        }

        public string Driver
        {
            get => _driver;
            set { _driver = value ?? ""; OnPropertyChanged(); }
        }

        public string Vehicle
        {
            get => _vehicle;
            set { _vehicle = value ?? ""; OnPropertyChanged(); }
        }

        public string Notes
        {
            get => _notes;
            set { _notes = value ?? ""; OnPropertyChanged(); }
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set { _createdDate = value; OnPropertyChanged(); }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Methods

        public DeliveryItem Clone()
        {
            return new DeliveryItem
            {
                Id = this.Id,
                OrderId = this.OrderId,
                DeliveryDate = this.DeliveryDate,
                DeliveredQty = this.DeliveredQty,
                DeliveredSQM = this.DeliveredSQM,
                ReturnedQty = this.ReturnedQty,
                ReturnedSQM = this.ReturnedSQM,
                Driver = this.Driver,
                Vehicle = this.Vehicle,
                Notes = this.Notes,
                CreatedDate = this.CreatedDate
            };
        }

        #endregion
    }
}