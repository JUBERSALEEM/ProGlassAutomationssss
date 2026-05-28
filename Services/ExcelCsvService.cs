using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Services
{
    public class ExcelCsvService
    {
        public event Action<string> StatusChanged;

        public void ExportToCsv(ProformaInvoiceModel invoice, string filePath = null)
        {
            try
            {
                if (invoice == null)
                {
                    StatusChanged?.Invoke("❌ No invoice to export");
                    return;
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    var dialog = new SaveFileDialog
                    {
                        Filter = "CSV Files (*.csv)|*.csv",
                        DefaultExt = ".csv",
                        FileName = $"Invoice_{invoice.InvoiceNo}_{DateTime.Now:yyyyMMdd}"
                    };

                    if (dialog.ShowDialog() != true) return;
                    filePath = dialog.FileName;
                }

                invoice.CalculateTotals();

                var lines = new List<string>();

                // ==================== VERSION ====================
                lines.Add($"# Version: 1.0");
                lines.Add($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                lines.Add("");

                // ==================== COMPANY DETAILS ====================
                lines.Add("[COMPANY DETAILS]");
                lines.Add($"Company Name        : {invoice.CompanyName ?? "PROGLASS AUTOMATION"}");
                lines.Add($"Company TRN         : {invoice.CompanyTRN ?? "100458979400003"}");
                lines.Add($"Company Location    : {invoice.CompanyLocation ?? "Dubai, UAE"}");
                lines.Add($"Company Phone       : +971-50-123-4567");
                lines.Add("");

                // ==================== INVOICE DETAILS ====================
                lines.Add("[INVOICE DETAILS]");
                lines.Add($"Invoice No          : {invoice.InvoiceNo ?? ""}");
                lines.Add($"Invoice Date        : {invoice.InvoiceDate:yyyy-MM-dd}");
                lines.Add($"Valid Until         : {invoice.ValidUntil:yyyy-MM-dd}");
                lines.Add($"Status              : {invoice.Status ?? "Pending"}");
                lines.Add("");

                // ==================== CUSTOMER DETAILS ====================
                lines.Add("[CUSTOMER DETAILS]");
                lines.Add($"Customer Name       : {invoice.CustomerName ?? ""}");
                lines.Add($"Customer TRN        : {invoice.CustomerTRN ?? ""}");
                lines.Add($"Customer Reference  : {invoice.CustomerReference ?? ""}");
                lines.Add($"Salesman            : {invoice.Salesman ?? ""}");
                lines.Add($"Customer Address    : {invoice.CustomerAddress ?? ""}");
                lines.Add("");

                // ==================== PROJECT DETAILS ====================
                lines.Add("[PROJECT DETAILS]");
                lines.Add($"Project Name        : {invoice.ProjectName ?? ""}");
                lines.Add($"Project No.         : {invoice.ProjectNo ?? ""}");
                lines.Add($"Project Location    : {invoice.ProjectLocation ?? ""}");
                lines.Add($"LPO No.             : {invoice.LPONo ?? ""}");
                lines.Add($"Attention           : {invoice.AttentionName ?? ""}");
                lines.Add($"Contact No.         : {invoice.ContactNo ?? ""}");
                lines.Add("");

                // ==================== ADDITIONAL INFO ====================
                lines.Add("[ADDITIONAL INFO]");
                lines.Add($"Color               : {invoice.Color ?? ""}");
                lines.Add($"Notes               : {invoice.Notes ?? ""}");
                lines.Add("");

                // ==================== SPECIFICATIONS ====================
                lines.Add("[SPECIFICATIONS]");
                lines.Add("--------------------------------------------------------------------------------------------------------------------------");
                lines.Add($"{"Spec No.",-8} {"Module Type",-8} {"Work Type",-15} {"Outer Glass",-20} {"Spacer",-25} {"Inner Glass",-20} {"PVB Layer",-25}");
                lines.Add("--------------------------------------------------------------------------------------------------------------------------");

                int specIndex = 1;
                var specs = invoice.Specifications ?? new ObservableCollection<SpecificationModel>();

                foreach (var spec in specs)
                {
                    string outerGlass = $"{spec.OuterThickness ?? ""}mm {spec.OuterColor ?? ""}".Trim();
                    string innerGlass = $"{spec.InnerThickness ?? ""}mm {spec.InnerColor ?? ""}".Trim();
                    string spacer = !string.IsNullOrEmpty(spec.SpacerThickness) ? spec.SpacerThickness : "N/A";
                    string pvbLayer = !string.IsNullOrEmpty(spec.PVBThickness)
                        ? $"{spec.PVBThickness}mm {spec.PVBColor ?? ""}".Trim()
                        : "N/A";

                    lines.Add($"{specIndex,-8} {PadRight(spec.ModuleType ?? "", 8)} {PadRight(spec.WorkType ?? "", 15)} {PadRight(outerGlass, 20)} {PadRight(spacer, 25)} {PadRight(innerGlass, 20)} {PadRight(pvbLayer, 25)}");
                    specIndex++;
                }
                lines.Add("--------------------------------------------------------------------------------------------------------------------------");
                lines.Add("");

                // ==================== ITEMS ====================
                lines.Add("[ITEMS]");
                lines.Add("--------------------------------------------------------------------------------------------------------------------------------");
                lines.Add($"{"Spec",-5} {"SR",-4} {"Glass Ref",-15} {"W1 (mm)",-10} {"H1 (mm)",-10} {"W2 (mm)",-10} {"H2 (mm)",-10} {"Qty",-6} {"SQM1",-12} {"SQM2",-12} {"Total SQM",-12} {"LM1",-12} {"LM2",-12}");
                lines.Add("--------------------------------------------------------------------------------------------------------------------------------");

                int itemSpecIndex = 1;
                foreach (var spec in specs)
                {
                    var items = spec.Items ?? new ObservableCollection<InvoiceItemModel>();
                    foreach (var item in items)
                    {
                        lines.Add($"{itemSpecIndex,-5} {item.SrNo,-4} {PadRight(item.GlassRef ?? "", 15)} {item.Width1,10:F0} {item.Height1,10:F0} {item.Width2,10:F0} {item.Height2,10:F0} {item.Qty,6:F0} {item.SQM1,12:F4} {item.SQM2,12:F4} {item.TotalSQM,12:F4} {item.LM1,12:F4} {item.LM2,12:F4}");
                    }
                    itemSpecIndex++;
                }
                lines.Add("--------------------------------------------------------------------------------------------------------------------------------");
                lines.Add("");

                // ==================== OTHER CHARGES ====================
                lines.Add("[OTHER CHARGES]");
                lines.Add("--------------------------------------------------------------------------------------");
                lines.Add($"{"Spec No.",-8} {"Charge Name",-20} {"Type",-8} {"Linked Specs",-15} {"Value",-15} {"Rate",-10} {"Amount",-12}");
                lines.Add("--------------------------------------------------------------------------------------");

                int chargeSpecIndex = 1;
                bool hasCharges = false;

                foreach (var spec in specs)
                {
                    var charges = spec.OtherCharges ?? new ObservableCollection<OtherChargeModel>();
                    if (charges.Count > 0)
                    {
                        hasCharges = true;
                        foreach (var charge in charges)
                        {
                            lines.Add($"{chargeSpecIndex,-8} {PadRight(charge.Name ?? "", 20)} {(charge.Type ?? "lm").ToUpper(),-8} {PadRight(charge.LinkedSpecIndices ?? chargeSpecIndex.ToString(), 15)} {charge.Value,15:F4} {charge.Rate,10:F2} {charge.Amount,12:F2}");
                        }
                    }
                    chargeSpecIndex++;
                }

                if (!hasCharges)
                {
                    lines.Add("No other charges defined");
                }
                lines.Add("--------------------------------------------------------------------------------------");

                File.WriteAllLines(filePath, lines, Encoding.UTF8);

                int totalItems = specs.Sum(s => s.Items?.Count ?? 0);
                StatusChanged?.Invoke($"✅ Exported to: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Export failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CSV Export Error] {ex}");
            }
        }

        private string PadRight(string value, int length)
        {
            if (string.IsNullOrEmpty(value)) value = "";
            if (value.Length > length)
                return value.Substring(0, length - 3) + "...";
            return value.PadRight(length);
        }

        public ProformaInvoiceModel ImportFromCsv()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Select Invoice CSV"
            };

            if (dialog.ShowDialog() != true) return null;

            return ImportFromCsvFile(dialog.FileName);
        }

        public ProformaInvoiceModel ImportFromCsvFile(string filePath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CSV Import] Loading: {filePath}");

                var invoice = new ProformaInvoiceModel();
                var lines = File.ReadAllLines(filePath, Encoding.UTF8);

                System.Diagnostics.Debug.WriteLine($"[CSV Import] Total lines: {lines.Length}");

                if (lines.Length == 0)
                {
                    StatusChanged?.Invoke("❌ CSV file is empty");
                    return null;
                }

                SpecificationModel currentSpec = null;
                bool inItemsSection = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Skip version and generated comments
                    if (line.StartsWith("#"))
                        continue;

                    // Skip section headers and separators
                    if (line.StartsWith("[") || line.StartsWith("---") || line.StartsWith("==="))
                        continue;

                    var parts = line.Split(',').Select(p => p.Trim()).ToArray();
                    if (parts.Length == 0) continue;

                    var firstCol = parts[0].ToUpper();

                    System.Diagnostics.Debug.WriteLine($"[Line {i}] {firstCol}");

                    // Invoice Header
                    if (firstCol == "INVOICE")
                    {
                        continue;
                    }
                    else if (firstCol == "INVOICE NO" && parts.Length > 1)
                    {
                        invoice.InvoiceNo = parts[1];
                    }
                    else if (firstCol == "INVOICE DATE" && parts.Length > 1)
                    {
                        if (DateTime.TryParse(parts[1], out var dt)) invoice.InvoiceDate = dt;
                    }
                    else if (firstCol == "CUSTOMER NAME" && parts.Length > 1)
                    {
                        invoice.CustomerName = parts[1];
                    }
                    else if (firstCol == "CUSTOMER TRN" && parts.Length > 1)
                    {
                        invoice.CustomerTRN = parts[1];
                    }
                    else if (firstCol == "VALID UNTIL" && parts.Length > 1)
                    {
                        if (DateTime.TryParse(parts[1], out var dt)) invoice.ValidUntil = dt;
                    }
                    else if (firstCol == "STATUS" && parts.Length > 1)
                    {
                        invoice.Status = parts[1];
                    }
                    else if (firstCol == "SALESMAN" && parts.Length > 1)
                    {
                        invoice.Salesman = parts[1];
                    }
                    else if (firstCol == "PROJECT NAME" && parts.Length > 1)
                    {
                        invoice.ProjectName = parts[1];
                    }
                    else if (firstCol == "PROJECT NO." && parts.Length > 1)
                    {
                        invoice.ProjectNo = parts[1];
                    }
                    else if (firstCol == "PROJECT LOCATION" && parts.Length > 1)
                    {
                        invoice.ProjectLocation = parts[1];
                    }
                    else if (firstCol == "LPO NO." && parts.Length > 1)
                    {
                        invoice.LPONo = parts[1];
                    }
                    else if (firstCol == "ATTENTION" && parts.Length > 1)
                    {
                        invoice.AttentionName = parts[1];
                    }
                    else if (firstCol == "CONTACT NO." && parts.Length > 1)
                    {
                        invoice.ContactNo = parts[1];
                    }

                    // Specifications Section
                    else if (firstCol == "SPECIFICATION" && parts.Length > 1)
                    {
                        // Save previous spec
                        if (currentSpec != null)
                        {
                            invoice.Specifications.Add(currentSpec);
                        }

                        currentSpec = new SpecificationModel
                        {
                            SpecificationName = parts[1]
                        };
                        inItemsSection = false;
                    }
                    else if (currentSpec != null && !inItemsSection)
                    {
                        if (firstCol == "BASE PRICE" && parts.Length > 1)
                        {
                            if (double.TryParse(parts[1], out var bp))
                                currentSpec.BasePrice = bp;
                        }
                        else if (firstCol == "SURCHARGE %" && parts.Length > 1)
                        {
                            if (double.TryParse(parts[1], out var sp))
                                currentSpec.SurchargePercent = sp;
                        }
                        else if (firstCol == "MODULE TYPE" && parts.Length > 1)
                        {
                            currentSpec.ModuleType = parts[1];
                        }
                        else if (firstCol == "WORK TYPE" && parts.Length > 1)
                        {
                            currentSpec.WorkType = parts[1];
                        }
                        else if (firstCol == "OUTER THICKNESS" && parts.Length > 1)
                        {
                            currentSpec.OuterThickness = parts[1];
                        }
                        else if (firstCol == "OUTER COLOR" && parts.Length > 1)
                        {
                            currentSpec.OuterColor = parts[1];
                        }
                        else if (firstCol == "INNER THICKNESS" && parts.Length > 1)
                        {
                            currentSpec.InnerThickness = parts[1];
                        }
                        else if (firstCol == "INNER COLOR" && parts.Length > 1)
                        {
                            currentSpec.InnerColor = parts[1];
                        }
                        else if (firstCol == "SPACER THICKNESS" && parts.Length > 1)
                        {
                            currentSpec.SpacerThickness = parts[1];
                        }
                        else if (firstCol == "PVB THICKNESS" && parts.Length > 1)
                        {
                            currentSpec.PVBThickness = parts[1];
                        }
                        else if (firstCol == "PVB COLOR" && parts.Length > 1)
                        {
                            currentSpec.PVBColor = parts[1];
                        }
                        else if (firstCol == "ITEMS START")
                        {
                            inItemsSection = true;
                        }
                    }
                    else if (currentSpec != null && inItemsSection)
                    {
                        if (firstCol == "ITEMS END")
                        {
                            inItemsSection = false;
                        }
                        else if (firstCol == "ITEM" && parts.Length >= 10)
                        {
                            var item = new InvoiceItemModel();

                            // Parse: Item,SR,GlassRef,W1,H1,W2,H2,Qty,Price,Surcharge
                            if (int.TryParse(parts[1], out var srNo)) item.SrNo = srNo;
                            item.GlassRef = parts[2];

                            if (double.TryParse(parts[3], out var w1)) item.Width1 = w1;
                            if (double.TryParse(parts[4], out var h1)) item.Height1 = h1;
                            if (double.TryParse(parts[5], out var w2)) item.Width2 = w2;
                            if (double.TryParse(parts[6], out var h2)) item.Height2 = h2;
                            if (int.TryParse(parts[7], out var qty)) item.Qty = qty;
                            if (double.TryParse(parts[8], out var price)) item.Price = price;
                            if (double.TryParse(parts[9], out var surcharge)) item.SurchargePercent = surcharge;

                            currentSpec.Items.Add(item);

                            System.Diagnostics.Debug.WriteLine($"[Item Added] SR={item.SrNo}, Glass={item.GlassRef}, W1={item.Width1}, H1={item.Height1}");
                        }
                    }
                }

                // Save last spec
                if (currentSpec != null)
                {
                    invoice.Specifications.Add(currentSpec);
                }

                // Ensure at least one specification
                if (invoice.Specifications.Count == 0)
                {
                    invoice.Specifications.Add(new SpecificationModel
                    {
                        SpecificationName = "Specification 1"
                    });
                }

                int totalSpecs = invoice.Specifications.Count;
                int totalItems = invoice.Specifications.Sum(s => s.Items.Count);

                System.Diagnostics.Debug.WriteLine($"[CSV Import] Complete: {totalSpecs} specs, {totalItems} items");

                StatusChanged?.Invoke($"✅ Imported {totalItems} items from {totalSpecs} specification(s)");
                return invoice;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CSV Import] ERROR: {ex.Message}\n{ex.StackTrace}");
                StatusChanged?.Invoke($"❌ Import failed: {ex.Message}");
                return null;
            }
        }

        public ObservableCollection<InvoiceItemModel> ImportItemsFromCsv()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    Title = "Select Items CSV"
                };

                if (dialog.ShowDialog() != true) return null;

                var items = new ObservableCollection<InvoiceItemModel>();
                var lines = File.ReadAllLines(dialog.FileName, Encoding.UTF8);

                bool skipHeader = true;
                foreach (var line in lines)
                {
                    var parts = line.Split(',').Select(p => p.Trim()).ToArray();

                    if (skipHeader)
                    {
                        skipHeader = false;
                        continue;
                    }

                    if (parts.Length < 5) continue;

                    var item = new InvoiceItemModel();

                    if (parts.Length > 0) item.GlassRef = parts[0];
                    if (parts.Length > 1 && double.TryParse(parts[1], out var w1)) item.Width1 = w1;
                    if (parts.Length > 2 && double.TryParse(parts[2], out var h1)) item.Height1 = h1;
                    if (parts.Length > 3 && int.TryParse(parts[3], out var qty)) item.Qty = qty;
                    if (parts.Length > 4 && double.TryParse(parts[4], out var price)) item.Price = price;

                    items.Add(item);
                }

                StatusChanged?.Invoke($"✅ Imported {items.Count} items");
                return items;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Item import failed: {ex.Message}");
                return null;
            }
        }
    }
}