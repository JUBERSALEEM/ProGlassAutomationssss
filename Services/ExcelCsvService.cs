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

                var sb = new StringBuilder();

                // Invoice Header
                sb.AppendLine("INVOICE");
                sb.AppendLine($"Invoice No,{invoice.InvoiceNo}");
                sb.AppendLine($"Invoice Date,{invoice.InvoiceDate:yyyy-MM-dd}");
                sb.AppendLine($"Customer Name,{invoice.CustomerName}");
                sb.AppendLine($"Customer TRN,{invoice.CustomerTRN}");
                sb.AppendLine($"Valid Until,{invoice.ValidUntil:yyyy-MM-dd}");
                sb.AppendLine();

                // Specifications with Items
                sb.AppendLine("SPECIFICATIONS");

                // Handle null specifications
                if (invoice.Specifications == null)
                {
                    invoice.Specifications = new ObservableCollection<SpecificationModel>();
                }

                int totalItemsExported = 0;

                foreach (var spec in invoice.Specifications)
                {
                    // Handle null spec name
                    string specName = spec?.SpecificationName ?? "Specification";
                    sb.AppendLine($"Specification,{specName}");
                    sb.AppendLine($"Base Price,{spec?.BasePrice ?? 0}");
                    sb.AppendLine($"Surcharge %,{spec?.SurchargePercent ?? 0}");
                    sb.AppendLine("Items Start");

                    // Handle items properly
                    if (spec?.Items != null && spec.Items.Count > 0)
                    {
                        foreach (var item in spec.Items)
                        {
                            string glassRef = item?.GlassRef ?? "";
                            sb.AppendLine($"Item,{item.SrNo},{glassRef},{item.Width1},{item.Height1},{item.Width2},{item.Height2},{item.Qty},{item.Price},{item.SurchargePercent}");
                            totalItemsExported++;
                        }
                    }

                    sb.AppendLine("Items End");
                    sb.AppendLine();
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

                System.Diagnostics.Debug.WriteLine($"[Export] Total items exported: {totalItemsExported}");
                StatusChanged?.Invoke($"✅ Exported {totalItemsExported} items");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Export Error] {ex.Message}\n{ex.StackTrace}");
                StatusChanged?.Invoke($"❌ Export failed: {ex.Message}");
                throw;
            }
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

                    var parts = line.Split(',').Select(p => p.Trim()).ToArray();
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

                    // Specifications Section
                    else if (firstCol == "SPECIFICATIONS")
                    {
                        continue;
                    }
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