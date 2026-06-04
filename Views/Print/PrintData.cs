using System;

namespace ProGlassAutomation.Views.Print
{
    public class PrintData
    {
        public string DocumentType { get; set; } = "Job Order";
        public string DocumentNumber { get; set; } = "";
        public DateTime DocumentDate { get; set; } = DateTime.Now;
        public DateTime? ValidUntil { get; set; }

        public string CompanyName { get; set; } = "";
        public string CompanyTRN { get; set; } = "";
        public string CompanyLocation { get; set; } = "";

        public string CustomerName { get; set; } = "";
        public string CustomerTRN { get; set; } = "";
        public string CustomerAddress { get; set; } = "";
        public string CustomerReference { get; set; } = "";
        public string Salesman { get; set; } = "";

        public string ProjectName { get; set; } = "";
        public string ProjectNo { get; set; } = "";
        public string ProjectLocation { get; set; } = "";

        public string LPONo { get; set; } = "";
        public string AttentionName { get; set; } = "";
        public string ContactNo { get; set; } = "";

        public string ReferencePI { get; set; } = "";
        public string Status { get; set; } = "Pending";

        public int TotalQty { get; set; }
        public double TotalSQM { get; set; }
        public double TotalLM { get; set; }
        public double TotalSQM2 { get; set; }
        public double TotalLM2 { get; set; }
        public double TotalAmount { get; set; }
        public string Notes { get; set; } = "";

        public PrintItemData[] Items { get; set; } = Array.Empty<PrintItemData>();
    }

    public class PrintItemData
    {
        public int SrNo { get; set; }
        public string GlassRef { get; set; } = "";
        public double Width1 { get; set; }
        public double Height1 { get; set; }
        public double Width2 { get; set; }
        public double Height2 { get; set; }
        public int Qty { get; set; }
        public double SQM { get; set; }
        public double TotalSQM { get; set; }
    }
}