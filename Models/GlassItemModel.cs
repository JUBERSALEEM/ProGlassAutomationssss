namespace ProGlassAutomation.Models
{
    public class GlassItemModel
    {
        public double OuterGlass { get; set; }
        public double LaminationGlass1 { get; set; }
        public double LaminationGlass2 { get; set; }

        public double ASP { get; set; }
        public double PVB { get; set; }

        public double ThicknessFactor { get; set; } = 1.0;
        public double ColorFactor { get; set; } = 1.0;
        public double ProfitFactor { get; set; } = 0.15;
    }
}