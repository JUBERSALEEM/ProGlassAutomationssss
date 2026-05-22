// Services/ExcelCsvService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Services
{
    public class ExcelCsvService
    {
        public event Action<string> StatusChanged;

        #region Export to CSV
        public bool ExportToCsv(ProformaInvoiceModel invoice, string filePath = null)
        {
            try
            {
                if (invoice == null || invoice.Specifications.Count == 0)
                {
                    StatusChanged?.Invoke("❌ No data to export");
                    return false;
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    var dialog = new SaveFileDialog
                    {
                        Filter = "CSV Files (*.csv)|*.csv",
                        DefaultExt = "csv",
                        FileName = $"Invoice_{invoice.InvoiceNo}_{DateTime.Now:yyyyMMdd}"
                    };

                    if (dialog.ShowDialog() != true)
                        return false;

                    filePath = dialog.FileName;
                }

                using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);

                // Write Invoice Header
                writer.WriteLine("PROFORMA INVOICE");
                writer.WriteLine($"Invoice No,{invoice.InvoiceNo}");
                writer.WriteLine($"Date,{invoice.InvoiceDate:yyyy-MM-dd}");
                writer.WriteLine($"Valid Until,{invoice.ValidUntil:yyyy-MM-dd}");
                writer.WriteLine();

                // Write Customer Details
                writer.WriteLine("CUSTOMER DETAILS");
                writer.WriteLine($"Company Name,{EscapeCsv(invoice.CustomerName)}");
                writer.WriteLine($"TRN,{invoice.CustomerTRN}");
                writer.WriteLine($"Address,{EscapeCsv(invoice.CustomerAddress)}");
                writer.WriteLine();

                // Write Project Details
                writer.WriteLine("PROJECT DETAILS");
                writer.WriteLine($"Project Name,{EscapeCsv(invoice.ProjectName)}");
                writer.WriteLine($"Location,{EscapeCsv(invoice.ProjectLocation)}");
                writer.WriteLine();

                // Write Other Details
                writer.WriteLine("OTHER DETAILS");
                writer.WriteLine($"LPO No,{invoice.LPONo}");
                writer.WriteLine($"Attention,{EscapeCsv(invoice.AttentionName)}");
                writer.WriteLine($"Contact,{invoice.ContactNo}");
                writer.WriteLine();

                // Write Specifications
                writer.WriteLine("SPECIFICATIONS");

                foreach (var spec in invoice.Specifications)
                {
                    writer.WriteLine($"Specification,{EscapeCsv(spec.SpecificationName)}");
                    writer.WriteLine($"Base Price,{spec.BasePrice:F2}");
                    writer.WriteLine();

                    // Write CSV Header
                    writer.WriteLine("SR,GLASS REF,W1(mm),H1(mm),W2(mm),H2(mm),QTY,PRICE");

                    // Write Data Rows
                    foreach (var item in spec.Items)
                    {
                        writer.WriteLine($"{item.SrNo},{EscapeCsv(item.GlassRef)},{item.Width1:F0},{item.Height1:F0},{item.Width2:F0},{item.Height2:F0},{item.Qty},{item.Price:F2}");
                    }

                    writer.WriteLine();
                }

                writer.Close();
                StatusChanged?.Invoke($"✅ Exported to {Path.GetFileName(filePath)}");
                return true;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Export failed: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Import from CSV - ROBUST VERSION
        public ProformaInvoiceModel ImportFromCsv()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    Title = "Select CSV File to Import"
                };

                if (dialog.ShowDialog() != true)
                    return null;

                return ImportFromCsvFile(dialog.FileName);
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Import failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[Import Error] {ex}");
                return null;
            }
        }

        public ProformaInvoiceModel ImportFromCsvFile(string filePath)
        {
            try
            {
                var invoice = new ProformaInvoiceModel();
                var lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);

                System.Diagnostics.Debug.WriteLine($"[CSV Import] File has {lines.Length} lines");

                if (lines.Length == 0)
                {
                    StatusChanged?.Invoke("❌ CSV file is empty");
                    return null;
                }

                // Find where specifications section starts
                int specStartIndex = -1;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().ToUpper() == "SPECIFICATIONS")
                    {
                        specStartIndex = i;
                        break;
                    }
                }

                // Parse header section (before SPECIFICATIONS)
                for (int i = 0; i < (specStartIndex >= 0 ? specStartIndex : lines.Length); i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    var parts = ParseCsvLine(line);
                    if (parts.Count < 2) continue;

                    var key = parts[0].Trim().ToUpper();
                    var value = parts[1].Trim();

                    switch (key)
                    {
                        case "INVOICE NO":
                        case "INVOICENO":
                            invoice.InvoiceNo = value;
                            System.Diagnostics.Debug.WriteLine($"[CSV] Invoice No: {value}");
                            break;
                        case "DATE":
                            if (DateTime.TryParse(value, out var date))
                                invoice.InvoiceDate = date;
                            break;
                        case "VALID UNTIL":
                        case "VALIDUNTIL":
                            if (DateTime.TryParse(value, out var validUntil))
                                invoice.ValidUntil = validUntil;
                            break;
                        case "COMPANY NAME":
                        case "COMPANYNAME":
                            invoice.CustomerName = value;
                            break;
                        case "TRN":
                            invoice.CustomerTRN = value;
                            break;
                        case "ADDRESS":
                            invoice.CustomerAddress = value;
                            break;
                        case "PROJECT NAME":
                        case "PROJECTNAME":
                            invoice.ProjectName = value;
                            break;
                        case "LOCATION":
                            invoice.ProjectLocation = value;
                            break;
                        case "LPO NO":
                        case "LPONO":
                            invoice.LPONo = value;
                            break;
                        case "ATTENTION":
                            invoice.AttentionName = value;
                            break;
                        case "CONTACT":
                            invoice.ContactNo = value;
                            break;
                    }
                }

                // Parse specifications section
                if (specStartIndex >= 0)
                {
                    int currentSpecIndex = -1;
                    bool foundDataHeader = false;
                    List<InvoiceItemModel> currentItems = new();
                    string currentSpecName = "";
                    double currentBasePrice = 0;

                    for (int i = specStartIndex + 1; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();
                        if (string.IsNullOrEmpty(line)) continue;

                        var parts = ParseCsvLine(line);
                        if (parts.Count == 0) continue;

                        var firstCol = parts[0].Trim().ToUpper();

                        // Check if it's a specification name line
                        if (firstCol == "SPECIFICATION" && parts.Count > 1)
                        {
                            // Save previous spec if exists
                            if (currentSpecIndex >= 0 && currentItems.Count > 0)
                            {
                                AddSpecificationToInvoice(invoice, currentSpecName, currentBasePrice, currentItems);
                                currentItems = new();
                            }

                            currentSpecName = parts[1].Trim();
                            currentSpecIndex++;
                            foundDataHeader = false;
                            System.Diagnostics.Debug.WriteLine($"[CSV] Spec {currentSpecIndex}: {currentSpecName}");
                        }
                        // Check if it's a base price line
                        else if (firstCol == "BASE PRICE" && parts.Count > 1)
                        {
                            if (double.TryParse(parts[1].Trim(), out var bp))
                                currentBasePrice = bp;
                        }
                        // Check if it's the data header row (SR, GLASS REF, W1, H1, etc.)
                        else if (firstCol == "SR" || firstCol.Contains("GLASS REF"))
                        {
                            foundDataHeader = true;
                            System.Diagnostics.Debug.WriteLine($"[CSV] Found data header row");
                        }
                        // It's a data row
                        else if (foundDataHeader && currentSpecIndex >= 0 && IsNumericRow(firstCol))
                        {
                            var item = ParseItemFromCsvParts(parts);
                            if (item != null)
                            {
                                currentItems.Add(item);
                                System.Diagnostics.Debug.WriteLine($"[CSV] Item: {item.SrNo}, {item.GlassRef}, W={item.Width1}, H={item.Height1}");
                            }
                        }
                    }

                    // Add last specification
                    if (currentSpecIndex >= 0 && currentItems.Count > 0)
                    {
                        AddSpecificationToInvoice(invoice, currentSpecName, currentBasePrice, currentItems);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[CSV] Total specs: {invoice.Specifications.Count}");
                int totalItems = invoice.Specifications.Sum(s => s.Items.Count);
                System.Diagnostics.Debug.WriteLine($"[CSV] Total items: {totalItems}");

                StatusChanged?.Invoke($"✅ Imported {totalItems} items from {Path.GetFileName(filePath)}");
                return invoice;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CSV Import Error] {ex}");
                StatusChanged?.Invoke($"❌ Import failed: {ex.Message}");
                return null;
            }
        }

        private void AddSpecificationToInvoice(ProformaInvoiceModel invoice, string specName, double basePrice, List<InvoiceItemModel> items)
        {
            var spec = new SpecificationModel
            {
                SpecificationName = specName,
                BasePrice = basePrice
            };

            foreach (var item in items)
            {
                // IMPORTANT: Set dimensions AFTER creation to trigger CalculateAll()
                var newItem = new InvoiceItemModel
                {
                    SrNo = item.SrNo,
                    GlassRef = item.GlassRef,
                    Qty = item.Qty,
                    Price = item.Price > 0 ? item.Price : basePrice,
                    SurchargePercent = item.SurchargePercent // SurchargeThreshold is fixed at 4
                };

                // Set dimensions AFTER to trigger CalculateAll() in InvoiceItemModel
                newItem.Width1 = item.Width1;
                newItem.Height1 = item.Height1;
                newItem.Width2 = item.Width2;
                newItem.Height2 = item.Height2;

                spec.Items.Add(newItem);
            }

            invoice.Specifications.Add(spec);
            System.Diagnostics.Debug.WriteLine($"[CSV] Added spec '{specName}' with {items.Count} items");
        }

        private InvoiceItemModel ParseItemFromCsvParts(List<string> parts)
        {
            try
            {
                var item = new InvoiceItemModel();

                // Column 0: SR No
                if (parts.Count > 0 && double.TryParse(parts[0].Trim(), out var sr))
                    item.SrNo = (int)sr;

                // Column 1: Glass Ref
                if (parts.Count > 1)
                    item.GlassRef = parts[1].Trim();

                // Column 2: Width1
                if (parts.Count > 2 && double.TryParse(parts[2].Trim(), out var w1))
                    item.Width1 = w1;

                // Column 3: Height1
                if (parts.Count > 3 && double.TryParse(parts[3].Trim(), out var h1))
                    item.Height1 = h1;

                // Column 4: Width2
                if (parts.Count > 4 && double.TryParse(parts[4].Trim(), out var w2))
                    item.Width2 = w2;

                // Column 5: Height2
                if (parts.Count > 5 && double.TryParse(parts[5].Trim(), out var h2))
                    item.Height2 = h2;

                // Column 6: Qty
                if (parts.Count > 6 && int.TryParse(parts[6].Trim(), out var qty))
                    item.Qty = qty;
                else
                    item.Qty = 1;

                // Column 7: Price
                if (parts.Count > 7 && double.TryParse(parts[7].Trim(), out var price))
                    item.Price = price;

                return item;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ParseItem Error] {ex}");
                return null;
            }
        }

        private bool IsNumericRow(string firstColumn)
        {
            return double.TryParse(firstColumn, out _);
        }
        #endregion

        #region Import Items from CSV
        public List<InvoiceItemModel> ImportItemsFromCsv()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    Title = "Select CSV File with Items to Import"
                };

                if (dialog.ShowDialog() != true)
                    return null;

                return ImportItemsFromCsvFile(dialog.FileName);
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Item import failed: {ex.Message}");
                return null;
            }
        }

        public List<InvoiceItemModel> ImportItemsFromCsvFile(string filePath)
        {
            var items = new List<InvoiceItemModel>();
            var lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);

            System.Diagnostics.Debug.WriteLine($"[Items Import] File has {lines.Length} lines");

            bool foundHeader = false;
            int startSrNo = 1;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine)) continue;

                var parts = ParseCsvLine(trimmedLine);
                if (parts.Count == 0) continue;

                var firstCol = parts[0].Trim();

                // Skip header row
                if (!foundHeader)
                {
                    if (firstCol.ToUpper() == "SR" || firstCol.ToUpper() == "SR NO" ||
                        firstCol.ToUpper() == "SRNO" || firstCol.ToUpper() == "NO" ||
                        IsNumericRow(firstCol))
                    {
                        // Check if it looks like a header
                        if (parts.Count >= 2 && parts[1].Trim().ToUpper().Contains("GLASS"))
                        {
                            foundHeader = true;
                            continue;
                        }
                        else if (IsNumericRow(firstCol))
                        {
                            // It's a numeric first row, but might be data
                            // Check if second column looks like glass ref
                            if (parts.Count >= 2 && !IsNumericRow(parts[1].Trim()))
                            {
                                // It's data, not header
                            }
                            else
                            {
                                foundHeader = true;
                                continue;
                            }
                        }
                    }
                    foundHeader = true;
                    continue;
                }

                // Check if first column is a number
                if (!IsNumericRow(firstCol)) continue;

                var item = ParseItemFromCsvParts(parts);
                if (item != null)
                {
                    item.SrNo = startSrNo++;
                    items.Add(item);
                    System.Diagnostics.Debug.WriteLine($"[Items] Added: {item.SrNo}, {item.GlassRef}, W={item.Width1}, H={item.Height1}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[Items Import] Total items: {items.Count}");
            StatusChanged?.Invoke($"✅ Imported {items.Count} items");
            return items;
        }
        #endregion

        #region Helper Methods
        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }

        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = "";
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current += '"';
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            result.Add(current);

            return result;
        }
        #endregion
    }
}