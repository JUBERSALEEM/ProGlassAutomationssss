using System;

namespace ProGlassAutomation.Data
{
    public class DbDailyWork
    {
        public int Id { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public DateTime UpdateDate { get; set; } = DateTime.Today;
        public string Company { get; set; } = string.Empty;

        // IMPORTANT: PINumber (not PiNumber) - matches original
        public string PINumber { get; set; } = string.Empty;

        public string CustomerReference { get; set; } = string.Empty;
        public string TypeOfWork { get; set; } = string.Empty;
        public string ProductionStatus { get; set; } = string.Empty;
        public string DailyReportStatus { get; set; } = string.Empty;
        public int Qty { get; set; }

        // IMPORTANT: SQM (not Sqm) - matches original  
        public double SQM { get; set; }

        public string Status { get; set; } = string.Empty;
        public string Salesman { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public Models.DailyWorkModel ToUiModel()
        {
            return new Models.DailyWorkModel
            {
                Id = this.Id,
                Date = this.Date,
                UpdateDate = this.UpdateDate,
                Company = this.Company,
                PiNumber = this.PINumber,
                CustomerReference = this.CustomerReference,
                TypeOfWork = this.TypeOfWork,
                ProductionStatus = this.ProductionStatus,
                DailyReportStatus = this.DailyReportStatus,
                Qty = this.Qty,
                Sqm = this.SQM,
                Status = this.Status,
                Salesman = this.Salesman,
                Color = this.Color,
                Notes = this.Notes,
                CreatedDate = this.CreatedDate
            };
        }

        public static DbDailyWork FromUiModel(Models.DailyWorkModel model)
        {
            return new DbDailyWork
            {
                Id = model.Id,
                Date = model.Date,
                UpdateDate = model.UpdateDate,
                Company = model.Company,
                PINumber = model.PiNumber,
                CustomerReference = model.CustomerReference,
                TypeOfWork = model.TypeOfWork,
                ProductionStatus = model.ProductionStatus,
                DailyReportStatus = model.DailyReportStatus,
                Qty = model.Qty,
                SQM = model.Sqm,
                Status = model.Status,
                Salesman = model.Salesman,
                Color = model.Color,
                Notes = model.Notes,
                CreatedDate = model.CreatedDate
            };
        }
    }
}