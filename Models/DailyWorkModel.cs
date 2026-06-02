using System;

namespace ProGlassAutomation.Models
{
    /// <summary>
    /// UI Model - Manual INotifyPropertyChanged (no source generators)
    /// </summary>
    public class DailyWorkModel : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        protected void Notify(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        // ═══════════════════════════════════════════════════════
        // FIELDS
        // ═══════════════════════════════════════════════════════

        private int _id;
        private DateTime _date = DateTime.Today;
        private DateTime _updateDate = DateTime.Today;
        private string _company = string.Empty;
        private string _piNumber = string.Empty;
        private string _customerReference = string.Empty;
        private string _typeOfWork = string.Empty;
        private string _productionStatus = string.Empty;
        private string _dailyReportStatus = string.Empty;
        private int _qty;
        private double _sqm;
        private string _status = string.Empty;
        private string _salesman = string.Empty;
        private string _color = string.Empty;
        private string _notes = string.Empty;
        private DateTime _createdDate = DateTime.Now;
        private bool _isFavorite;

        // ═══════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════

        public int Id
        {
            get => _id;
            set { _id = value; Notify(nameof(Id)); }
        }

        public DateTime Date
        {
            get => _date;
            set { _date = value; Notify(nameof(Date)); }
        }

        public DateTime UpdateDate
        {
            get => _updateDate;
            set { _updateDate = value; Notify(nameof(UpdateDate)); }
        }

        public string Company
        {
            get => _company;
            set { _company = value ?? string.Empty; Notify(nameof(Company)); }
        }

        public string PiNumber
        {
            get => _piNumber;
            set { _piNumber = value ?? string.Empty; Notify(nameof(PiNumber)); }
        }

        public string CustomerReference
        {
            get => _customerReference;
            set { _customerReference = value ?? string.Empty; Notify(nameof(CustomerReference)); }
        }

        public string TypeOfWork
        {
            get => _typeOfWork;
            set { _typeOfWork = value ?? string.Empty; Notify(nameof(TypeOfWork)); }
        }

        public string ProductionStatus
        {
            get => _productionStatus;
            set { _productionStatus = value ?? string.Empty; Notify(nameof(ProductionStatus)); }
        }

        public string DailyReportStatus
        {
            get => _dailyReportStatus;
            set { _dailyReportStatus = value ?? string.Empty; Notify(nameof(DailyReportStatus)); }
        }

        public int Qty
        {
            get => _qty;
            set { _qty = value; Notify(nameof(Qty)); }
        }

        public double Sqm
        {
            get => _sqm;
            set { _sqm = value; Notify(nameof(Sqm)); }
        }

        public string Status
        {
            get => _status;
            set { _status = value ?? string.Empty; Notify(nameof(Status)); }
        }

        public string Salesman
        {
            get => _salesman;
            set { _salesman = value ?? string.Empty; Notify(nameof(Salesman)); }
        }

        public string Color
        {
            get => _color;
            set { _color = value ?? string.Empty; Notify(nameof(Color)); }
        }

        public string Notes
        {
            get => _notes;
            set { _notes = value ?? string.Empty; Notify(nameof(Notes)); }
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set { _createdDate = value; Notify(nameof(CreatedDate)); }
        }

        // PATCH 161: IsFavorite property
        public bool IsFavorite
        {
            get => _isFavorite;
            set { _isFavorite = value; Notify(nameof(IsFavorite)); }
        }

        // ═══════════════════════════════════════════════════════
        // CLONE
        // ═══════════════════════════════════════════════════════

        public DailyWorkModel Clone()
        {
            return new DailyWorkModel
            {
                Id = this.Id,
                Date = this.Date,
                Company = this.Company,
                PiNumber = this.PiNumber,
                Color = this.Color,
                CustomerReference = this.CustomerReference,
                TypeOfWork = this.TypeOfWork,
                ProductionStatus = this.ProductionStatus,
                Qty = this.Qty,
                Sqm = this.Sqm,
                Status = this.Status,
                Salesman = this.Salesman,
                Notes = this.Notes,
                CreatedDate = this.CreatedDate,
                UpdateDate = this.UpdateDate,
                // PATCH 161: Clone IsFavorite
                IsFavorite = this.IsFavorite
            };
        }

        // ═══════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════

        public static DailyWorkModel CreateNew()
        {
            return new DailyWorkModel
            {
                Date = DateTime.Today,
                CreatedDate = DateTime.Now
            };
        }
    }
}