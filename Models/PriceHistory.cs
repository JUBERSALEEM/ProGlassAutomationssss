using System;

namespace ProGlassAutomation.Models
{
    public class PriceHistory
    {
        public DateTime Date { get; set; }
        public DateTime Time { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SellingPrice { get; set; }
        public string SupplierName { get; set; } = "";
        public string Notes { get; set; } = "";

        public string DisplayDateTime => Date > DateTime.MinValue
            ? $"{Date:dd-MMM-yyyy} {Time:HH:mm}"
            : "-";
    }
}