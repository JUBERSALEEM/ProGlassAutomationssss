using System;
using System.IO;

namespace ProGlassAutomation.ViewModels.ProformaInvoice.Models
{
    public class DimensionOption
    {
        public string Value { get; set; } = "";
        public string Label { get; set; } = "";
    }

    public class ChargeTypeOption
    {
        public string Value { get; set; } = "";
        public string Label { get; set; } = "";
    }

    public enum ChargeType
    {
        LM, LM1, LM2,
        SQM, SQM1, SQM2,
        QTY, MULTI2
    }

    public class AirSpacerOption
    {
        public string Thickness { get; set; } = "";
        public string Type { get; set; } = "";
        public double Price { get; set; }
        public string Display => $"{Thickness}mm {Type} - AED {Price:F2}";
    }

    public class FileListItem
    {
        public string FilePath { get; set; } = "";
        public string InvoiceNo { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public DateTime InvoiceDate { get; set; }
        public double NetTotal { get; set; }
        public string FileName => Path.GetFileNameWithoutExtension(FilePath);
        public string DateDisplay => InvoiceDate.ToString("dd MMM yyyy");
        public string TotalDisplay => $"AED {NetTotal:N2}";
    }
}
