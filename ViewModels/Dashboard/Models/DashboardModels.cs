using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProGlassAutomation.ViewModels.Dashboard.Models
{
    public class KPICard : INotifyPropertyChanged
    {
        private double _value;
        public string Title { get; set; } = "";
        public double Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }
        public string Suffix { get; set; } = "";
        public double ChangePercent { get; set; }
        public string ChangeLabel { get; set; } = "";
        public string Icon { get; set; } = "";
        public string ColorKey { get; set; } = "Blue";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class DailyWorkRecord
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Company { get; set; } = "";
        public string PiNumber { get; set; } = "";
        public string CustomerReference { get; set; } = "";
        public string TypeOfWork { get; set; } = "";
        public string ProductionStatus { get; set; } = "";
        public int Qty { get; set; }
        public double Sqm { get; set; }
        public string Status { get; set; } = "";
        public string Salesman { get; set; } = "";
        public string Color { get; set; } = "";
    }

    public class DeliveryRecord
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Company { get; set; } = "";
        public string PINumber { get; set; } = "";
        public string TypeOfWork { get; set; } = "";
        public string Color { get; set; } = "";
        public int OrderQty { get; set; }
        public int TotalDelivered { get; set; }
        public int TotalReturned { get; set; }
        public int Balance { get; set; }
        public double OrderSQM { get; set; }
        public string Salesman { get; set; } = "";
        public string Status { get; set; } = "";
    }

    public class SalesmanData
    {
        public string Name { get; set; } = "";
        public string Amount { get; set; } = "";
    }
}
