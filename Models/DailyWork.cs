// Models/DailyWork.cs
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProGlassAutomation.Models
{
    public class DailyWork : INotifyPropertyChanged
    {
        private int _id;
        private DateTime _date;
        private DateTime _updateDate;
        private string _company;
        private string _piNumber;
        private string _customerReference;
        private string _typeOfWork;
        private string _productionStatus;
        private string _dailyReportStatus;
        private int _qty;
        private double _sqm;
        private string _status;
        private string _salesman;
        private string _color;
        private string _notes;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public DateTime Date
        {
            get => _date;
            set { _date = value; OnPropertyChanged(); }
        }

        public DateTime UpdateDate
        {
            get => _updateDate;
            set { _updateDate = value; OnPropertyChanged(); }
        }

        public string Company
        {
            get => _company;
            set { _company = value; OnPropertyChanged(); }
        }

        public string PINumber
        {
            get => _piNumber;
            set { _piNumber = value; OnPropertyChanged(); }
        }

        public string CustomerReference
        {
            get => _customerReference;
            set { _customerReference = value; OnPropertyChanged(); }
        }

        public string TypeOfWork
        {
            get => _typeOfWork;
            set { _typeOfWork = value; OnPropertyChanged(); OnPropertyChanged(nameof(TypeOfWorkDisplay)); }
        }

        public string TypeOfWorkDisplay => TypeOfWork;

        public string ProductionStatus
        {
            get => _productionStatus;
            set { _productionStatus = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProductionStatusDisplay)); }
        }

        public string ProductionStatusDisplay => ProductionStatus;

        public string DailyReportStatus
        {
            get => _dailyReportStatus;
            set { _dailyReportStatus = value; OnPropertyChanged(); OnPropertyChanged(nameof(DailyReportStatusDisplay)); }
        }

        public string DailyReportStatusDisplay => DailyReportStatus;

        public int Qty
        {
            get => _qty;
            set { _qty = value; OnPropertyChanged(); }
        }

        public double SQM
        {
            get => _sqm;
            set { _sqm = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusDisplay)); OnPropertyChanged(nameof(StatusColor)); }
        }

        public string StatusDisplay => Status;

        public string StatusColor
        {
            get
            {
                return Status switch
                {
                    "Release" => "#10B981",
                    "Hold" => "#F59E0B",
                    "Cancel" => "#EF4444",
                    _ => "#64748B"
                };
            }
        }

        public string Salesman
        {
            get => _salesman;
            set { _salesman = value; OnPropertyChanged(); }
        }

        public string Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(); OnPropertyChanged(nameof(ColorDisplay)); }
        }

        public string ColorDisplay => Color;

        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(); }
        }

        // Computed Properties
        public string CombinedTypeDisplay
        {
            get
            {
                if (string.IsNullOrEmpty(TypeOfWork)) return "-";
                return TypeOfWork;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public DailyWork Clone()
        {
            return new DailyWork
            {
                Id = this.Id,
                Date = this.Date,
                UpdateDate = this.UpdateDate,
                Company = this.Company,
                PINumber = this.PINumber,
                CustomerReference = this.CustomerReference,
                TypeOfWork = this.TypeOfWork,
                ProductionStatus = this.ProductionStatus,
                DailyReportStatus = this.DailyReportStatus,
                Qty = this.Qty,
                SQM = this.SQM,
                Status = this.Status,
                Salesman = this.Salesman,
                Color = this.Color,
                Notes = this.Notes
            };
        }
    }
}