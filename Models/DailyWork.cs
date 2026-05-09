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
        private string _company = "";
        private string _piNumber = "";
        private string _customerReference = "";
        private string _typeOfWork = "";
        private string _productionStatus = "";
        private string _dailyReportStatus = "";
        private int _qty;
        private double _sqm;
        private string _status = "";
        private string _salesman = "";
        private string _color = "";
        private string _notes = "";
        private DateTime _createdDate;

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

        public string ProductionStatus
        {
            get => _productionStatus;
            set { _productionStatus = value ?? ""; OnPropertyChanged(); }
        }

        public string DailyReportStatus
        {
            get => _dailyReportStatus;
            set { _dailyReportStatus = value ?? ""; OnPropertyChanged(); }
        }

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
            set { _status = value ?? ""; OnPropertyChanged(); }
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

        // Computed Properties for Display
        public string StatusColor
        {
            get
            {
                return Status switch
                {
                    "Release" => "#10B981",
                    "Hold" => "#F59E0B",
                    "Cancel" => "#EF4444",
                    "Confirmed" => "#6366F1",
                    _ => "#64748B"
                };
            }
        }

        public string ProductionStatusColor
        {
            get
            {
                return ProductionStatus switch
                {
                    "Completed" => "#10B981",
                    "In Production" => "#3B82F6",
                    "Quality Check" => "#8B5CF6",
                    "Pending" => "#F59E0B",
                    "Sent" => "#06B6D4",
                    "Confirmed" => "#6366F1",
                    "Prepared" => "#14B8A6",
                    "In Progress" => "#3B82F6",
                    _ => "#64748B"
                };
            }
        }

        public string DailyReportStatusColor
        {
            get
            {
                return DailyReportStatus switch
                {
                    "Completed" => "#10B981",
                    "In Progress" => "#3B82F6",
                    "On Hold" => "#F59E0B",
                    "Issue Found" => "#EF4444",
                    "Re-work Required" => "#F97316",
                    _ => "#64748B"
                };
            }
        }

        public string FormattedDate => Date.ToString("yyyy-MM-dd");
        public string FormattedUpdateDate => UpdateDate.ToString("yyyy-MM-dd");
        public string FormattedSQM => SQM.ToString("N2");
        public string FormattedQty => Qty.ToString();

        // Event
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Clone Method
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
                Notes = this.Notes,
                CreatedDate = this.CreatedDate
            };
        }
    }
}