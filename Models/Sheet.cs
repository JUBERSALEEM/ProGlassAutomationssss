using System;
using System.Collections.Generic;

namespace ProGlassAutomation.Models
{
    public class Sheet
    {
        public string Id { get; set; } = "";
        public string Thickness { get; set; } = "";
        public string Color { get; set; } = "";
        public string Category { get; set; } = "";
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SellPrice { get; set; }
        public decimal PricePerSqft { get; set; }
        public decimal PricePerSqmeter { get; set; }
        public DateTime LatestPurchaseDate { get; set; }
        public DateTime LatestPurchaseTime { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string Description { get; set; } = "";
        public decimal LastPurchasePrice { get; set; }
        public DateTime LastPurchaseDate { get; set; }
        public DateTime LastPurchaseTime { get; set; }
        public string SupplierName { get; set; } = "";
        public List<PriceHistory> PriceHistory { get; set; } = new List<PriceHistory>();

        public string DisplayName => $"{Thickness} - {Color}";
        public string DisplayDimensions => $"{Width} x {Height} mm";
        public decimal Area => (Width / 1000) * (Height / 1000);

        public string DisplayPurchaseInfo => LastPurchaseDate > DateTime.MinValue
            ? $"AED {LastPurchasePrice:N2} | {LastPurchaseDate:dd-MMM-yyyy} {LastPurchaseTime:HH:mm}"
            : "No purchase";
    }
}