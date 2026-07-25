using System;
using System.Collections.Generic;

namespace ProGlassAutomation.ViewModels.JobOrders.Models
{
    public class JsonSpecModel
    {
        public int Id { get; set; }
        public string SpecificationName { get; set; }
        public string ModuleType { get; set; }
        public string WorkType { get; set; }
        public string OuterThickness { get; set; }
        public string OuterColor { get; set; }
        public string InnerThickness { get; set; }
        public string InnerColor { get; set; }
        public string SpacerThickness { get; set; }
        public string PVBThickness { get; set; }
        public string PVBColor { get; set; }
        public bool IncludeInSpec { get; set; } = true;
        public double BasePrice { get; set; }
        public double SurchargePercent { get; set; }
        public List<JsonItemModel> Items { get; set; }
        public List<JsonChargeModel> OtherCharges { get; set; }
    }

    public class JsonItemModel
    {
        public int Id { get; set; }
        public int SrNo { get; set; }
        public string GlassRef { get; set; }
        public double Width1 { get; set; }
        public double Height1 { get; set; }
        public double Width2 { get; set; }
        public double Height2 { get; set; }
        public int Qty { get; set; }
        public int DeliveredQty { get; set; }
        public double Price { get; set; }
    }

    public class JsonChargeModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public double Value { get; set; }
        public double Rate { get; set; }
        public double Amount { get; set; }
        public string LinkedSpecIndices { get; set; }
        public int LinkedSpecIndex { get; set; }
        public bool TargetsAllSpecs { get; set; }
        public string LmDimType { get; set; }
        public bool IsManualOverride { get; set; }
    }
}
