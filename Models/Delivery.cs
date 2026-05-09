using System;
using System.Collections.ObjectModel;

namespace ProGlassAutomation.Models
{
    public class Delivery
    {
        public int Id { get; set; }
        public int SourceId { get; set; }
        public DateTime Date { get; set; }
        public string Company { get; set; }
        public string PINumber { get; set; }
        public string CustomerReference { get; set; }
        public string TypeOfWork { get; set; }
        public int OrderQty { get; set; }
        public double OrderSQM { get; set; }
        public string Salesman { get; set; }
        public string Color { get; set; }
        public string ProductionStatus { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public ObservableCollection<DeliveryItem> DeliveryItems { get; set; } = new ObservableCollection<DeliveryItem>();

        public int TotalDelivered => DeliveryItems?.Sum(x => x.DeliveredQty) ?? 0;
        public int TotalReturned => DeliveryItems?.Sum(x => x.ReturnedQty) ?? 0;
        public int Balance => OrderQty - TotalDelivered + TotalReturned;
        public double BalanceSQM => OrderQty > 0 ? Math.Round((double)Balance / OrderQty * OrderSQM, 2) : 0;

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

    public class DeliveryItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public DateTime DeliveryDate { get; set; }
        public int DeliveredQty { get; set; }
        public double DeliveredSQM { get; set; }
        public int ReturnedQty { get; set; }
        public double ReturnedSQM { get; set; }
        public string Driver { get; set; }
        public string Vehicle { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}