using System;

namespace ProGlassAutomation.Models
{
    public class UseRecord
    {
        public int Id { get; set; }
        public int SheetId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; }
        public DateTime UseDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}