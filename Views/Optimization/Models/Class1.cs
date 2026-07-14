namespace ProGlassAutomation.Models
{
    public class PlacedPart
    {
        public string Label { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public double L { get; set; }
        public double W { get; set; }
        public bool Rotated { get; set; }
        public int SheetIndex { get; set; }

        // ✅ NEW: leftover dimensions for backfill heuristic
        public double LeftoverL { get; set; }
        public double LeftoverW { get; set; }
    }
}