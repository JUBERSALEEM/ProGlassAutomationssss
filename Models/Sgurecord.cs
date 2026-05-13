using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProGlassAutomation.Models
{
    public class SguRecord : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public int Id { get; set; }
        public string Category { get; set; }
        public string Thickness { get; set; }
        public string Color { get; set; }
        public double SheetPrice { get; set; }
        public double Cutting { get; set; }
        public double TemperingCharge { get; set; }
        public double OtherCharges { get; set; }
        public string Wastage { get; set; }
        public string ProfitMargin { get; set; }
        public string EdgeWork { get; set; }
        public string Drilling { get; set; }
        public string Tempering { get; set; }
        public string Coating { get; set; }
        public string SurfaceTreatment { get; set; }
        public string Cutout { get; set; }
        public string Unit { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Quantity { get; set; }
        public double TotalArea { get; set; }
        public double TotalPrice { get; set; }
        public double Result { get; set; }
        public string CustomNotes { get; set; }
        public string CreatedAt { get; set; }

        // Computed display properties
        public string DisplayText => $"{Category} | {Thickness} | {Color}";
        public string GlassDetails => $"{Category} | {Thickness} | {Color} | {SheetPrice:F2}";
        public string ProcessingDetails => $"{EdgeWork} | {Drilling} | {Tempering}";
        public string TreatmentDetails => $"{Coating} | {SurfaceTreatment} | {Cutout}";

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }
    }
}