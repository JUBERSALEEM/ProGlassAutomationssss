using System;

namespace ProGlassAutomation.Models
{
    public class PurchaseRecord
    {
        public int Id { get; set; }
        public int SheetId { get; set; }
        public int Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public string? Supplier { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        public decimal TotalAmount => Quantity * PurchasePrice;
        public string DisplayDate => PurchaseDate.ToString("dd/MM/yyyy HH:mm");
    }
}