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
        public event Action<string>? StatusChanged;

        public void ExportToCsv(ProformaInvoiceModel invoice, string? filePath = null)
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
                lines.Add("# Version: 1.0");
                lines.Add($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                lines.Add("");

                // ==================== INVOICE DETAILS (CSV format) ====================
                lines.Add("Invoice No," + EscapeCsv(invoice.InvoiceNo ?? ""));
                lines.Add("Invoice Date," + invoice.InvoiceDate.ToString("yyyy-MM-dd"));
                lines.Add("Valid Until," + invoice.ValidUntil.ToString("yyyy-MM-dd"));
                lines.Add("Status," + EscapeCsv(invoice.Status ?? "Pending"));
                lines.Add("Customer Name," + EscapeCsv(invoice.CustomerName ?? ""));
                lines.Add("Customer TRN," + EscapeCsv(invoice.CustomerTRN ?? ""));
                lines.Add("Customer Reference," + EscapeCsv(invoice.CustomerReference ?? ""));
                lines.Add("Salesman," + EscapeCsv(invoice.Salesman ?? ""));
                lines.Add("Customer Address," + EscapeCsv(invoice.CustomerAddress ?? ""));
                lines.Add("Project Name," + EscapeCsv(invoice.ProjectName ?? ""));
                lines.Add("Project No.," + EscapeCsv(invoice.ProjectNo ?? ""));
                lines.Add("Project Location," + EscapeCsv(invoice.ProjectLocation ?? ""));
                lines.Add("LPO No.," + EscapeCsv(invoice.LPONo ?? ""));
                lines.Add("Attention," + EscapeCsv(invoice.AttentionName ?? ""));
                lines.Add("Contact No.," + EscapeCsv(invoice.ContactNo ?? ""));
                lines.Add("Color," + EscapeCsv(invoice.Color ?? ""));
                lines.Add("Notes," + EscapeCsv(invoice.Notes ?? ""));
                lines.Add("");

                // ==================== SPECIFICATIONS ====================
                lines.Add("[SPECIFICATIONS]");

                int specIndex = 1;
                var specs = invoice.Specifications ?? new ObservableCollection<SpecificationModel>();

                foreach (var spec in specs)
                {
                    lines.Add("SPECIFICATION," + EscapeCsv(spec.SpecificationName ?? ""));
                    lines.Add("Module Type," + EscapeCsv(spec.ModuleType ?? ""));
                    lines.Add("Work Type," + EscapeCsv(spec.WorkType ?? ""));
                    lines.Add("Base Price," + spec.BasePrice.ToString("F2"));
                    lines.Add("Surcharge %," + spec.SurchargePercent.ToString("F2"));
                    lines.Add("Outer Thickness," + EscapeCsv(spec.OuterThickness ?? ""));
                    lines.Add("Outer Color," + EscapeCsv(spec.OuterColor ?? ""));
                    lines.Add("Inner Thickness," + EscapeCsv(spec.InnerThickness ?? ""));
                    lines.Add("Inner Color," + EscapeCsv(spec.InnerColor ?? ""));
                    lines.Add("Spacer Thickness," + EscapeCsv(spec.SpacerThickness ?? ""));
                    lines.Add("PVB Thickness," + EscapeCsv(spec.PVBThickness ?? ""));
                    lines.Add("PVB Color," + EscapeCsv(spec.PVBColor ?? ""));
                    lines.Add("");

                    // ==================== ITEMS ====================
                    lines.Add("[ITEMS]");

                    var items = spec.Items ?? new ObservableCollection<InvoiceItemModel>();
                    foreach (var item in items)
                    {
                        // CSV format: Spec,SR,GlassRef,W1,H1,W2,H2,Qty,Price,Surcharge
                        lines.Add($"{specIndex},{item.SrNo},{EscapeCsv(item.GlassRef ?? "")},{item.Width1},{item.Height1},{item.Width2},{item.Height2},{item.Qty},{item.Price},{item.SurchargePercent}");
                    }

                    lines.Add("");
                    specIndex++;
                }

                // ==================== OTHER CHARGES ====================
                lines.Add("[OTHER CHARGES]");

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
                            // CSV format: Spec No,Charge Name,Type,Linked Specs,Value,Rate,Amount
                            lines.Add($"{chargeSpecIndex},{EscapeCsv(charge.Name ?? "")},{EscapeCsv(charge.Type ?? "lm")},{EscapeCsv(charge.LinkedSpecIndices ?? chargeSpecIndex.ToString())},{charge.Value},{charge.Rate},{charge.Amount}");
                        }
                    }
                    chargeSpecIndex++;
                }

                if (!hasCharges)
                {
                    lines.Add("No other charges defined");
                }

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

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }

        public ProformaInvoiceModel? ImportFromCsv()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Select Invoice CSV"
            };

            if (dialog.ShowDialog() != true) return null;

            return ImportFromCsvFile(dialog.FileName);
        }

        public ProformaInvoiceModel? ImportFromCsvFile(string filePath)
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

                SpecificationModel? currentSpec = null;
                bool inItemsSection = false;
                bool inOtherChargesSection = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Skip comments
                    if (line.StartsWith("#"))
                        continue;

                    // Detect sections
                    if (line.StartsWith("[ITEMS]"))
                    {
                        inItemsSection = true;
                        inOtherChargesSection = false;
                        continue;
                    }
                    if (line.StartsWith("[OTHER CHARGES]"))
                    {
                        inItemsSection = false;
                        inOtherChargesSection = true;
                        continue;
                    }
                    if (line.StartsWith("[SPECIFICATIONS]"))
                    {
                        inItemsSection = false;
                        inOtherChargesSection = false;
                        continue;
                    }

                    // Parse CSV line
                    var parts = ParseCsvLine(line);
                    if (parts.Length == 0) continue;

                    var firstCol = parts[0].ToUpper();

                    // Invoice fields
                    if (firstCol == "INVOICE NO" && parts.Length > 1)
                    {
                        invoice.InvoiceNo = parts[1];
                    }
                    else if (firstCol == "INVOICE DATE" && parts.Length > 1)
                    {
                        if (DateTime.TryParse(parts[1], out var dt)) invoice.InvoiceDate = dt;
                    }
                    else if (firstCol == "VALID UNTIL" && parts.Length > 1)
                    {
                        if (DateTime.TryParse(parts[1], out var dt)) invoice.ValidUntil = dt;
                    }
                    else if (firstCol == "STATUS" && parts.Length > 1)
                    {
                        invoice.Status = parts[1];
                    }
                    else if (firstCol == "CUSTOMER NAME" && parts.Length > 1)
                    {
                        invoice.CustomerName = parts[1];
                    }
                    else if (firstCol == "CUSTOMER TRN" && parts.Length > 1)
                    {
                        invoice.CustomerTRN = parts[1];
                    }
                    else if (firstCol == "CUSTOMER REFERENCE" && parts.Length > 1)
                    {
                        invoice.CustomerReference = parts[1];
                    }
                    else if (firstCol == "SALESMAN" && parts.Length > 1)
                    {
                        invoice.Salesman = parts[1];
                    }
                    else if (firstCol == "CUSTOMER ADDRESS" && parts.Length > 1)
                    {
                        invoice.CustomerAddress = parts[1];
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
                    else if (firstCol == "COLOR" && parts.Length > 1)
                    {
                        invoice.Color = parts[1];
                    }
                    else if (firstCol == "NOTES" && parts.Length > 1)
                    {
                        invoice.Notes = parts[1];
                    }

                    // Specifications
                    else if (firstCol == "SPECIFICATION" && parts.Length > 1)
                    {
                        if (currentSpec != null)
                        {
                            invoice.Specifications.Add(currentSpec);
                        }

                        currentSpec = new SpecificationModel
                        {
                            SpecificationName = parts[1]
                        };
                    }
                    else if (currentSpec != null && !inItemsSection && !inOtherChargesSection)
                    {
                        if (firstCol == "MODULE TYPE" && parts.Length > 1)
                        {
                            currentSpec.ModuleType = parts[1];
                        }
                        else if (firstCol == "WORK TYPE" && parts.Length > 1)
                        {
                            currentSpec.WorkType = parts[1];
                        }
                        else if (firstCol == "BASE PRICE" && parts.Length > 1)
                        {
                            if (double.TryParse(parts[1], out var bp))
                                currentSpec.BasePrice = bp;
                        }
                        else if (firstCol == "SURCHARGE %" && parts.Length > 1)
                        {
                            if (double.TryParse(parts[1], out var sp))
                                currentSpec.SurchargePercent = sp;
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
                    }

                    // Items section
                    else if (inItemsSection && currentSpec != null)
                    {
                        // CSV format: Spec,SR,GlassRef,W1,H1,W2,H2,Qty,Price,Surcharge
                        if (parts.Length >= 6 && int.TryParse(parts[1], out var srNo))
                        {
                            var item = new InvoiceItemModel();
                            item.SrNo = srNo;
                            item.GlassRef = parts.Length > 2 ? parts[2] : "";
                            item.Width1 = parts.Length > 3 && double.TryParse(parts[3], out var w1) ? w1 : 0;
                            item.Height1 = parts.Length > 4 && double.TryParse(parts[4], out var h1) ? h1 : 0;
                            item.Width2 = parts.Length > 5 && double.TryParse(parts[5], out var w2) ? w2 : 0;
                            item.Height2 = parts.Length > 6 && double.TryParse(parts[6], out var h2) ? h2 : 0;
                            item.Qty = parts.Length > 7 && int.TryParse(parts[7], out var qty) ? qty : 1;
                            item.Price = parts.Length > 8 && double.TryParse(parts[8], out var price) ? price : 0;
                            item.SurchargePercent = parts.Length > 9 && double.TryParse(parts[9], out var surcharge) ? surcharge : 20;

                            currentSpec.Items.Add(item);

                            System.Diagnostics.Debug.WriteLine($"[Item Added] SR={item.SrNo}, Glass={item.GlassRef}, W1={item.Width1}, H1={item.Height1}, Qty={item.Qty}");
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

                invoice.CalculateTotals();

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

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString().Trim());
            return result.ToArray();
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

                if (dialog.ShowDialog() != true) return new ObservableCollection<InvoiceItemModel>();

                var items = new ObservableCollection<InvoiceItemModel>();
                var lines = File.ReadAllLines(dialog.FileName, Encoding.UTF8);

                bool skipHeader = true;
                foreach (var line in lines)
                {
                    var parts = ParseCsvLine(line);

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
                return new ObservableCollection<InvoiceItemModel>();
            }
        }
    }
}