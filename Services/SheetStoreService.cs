using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Services
{
    public class SheetStoreService
    {
        private static SheetStoreService? _instance;
        public static SheetStoreService Instance => _instance ??= new SheetStoreService();

        private readonly string _dataFile;
        private List<Sheet> _sheets;

        private SheetStoreService()
        {
            _dataFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sheets.json");
            _sheets = LoadSheets();
        }

        // ==================== GET METHODS ====================

        public List<Sheet> GetAllSheets()
        {
            return _sheets.Where(s => s.IsActive).OrderBy(s => s.Thickness).ThenBy(s => s.Color).ToList();
        }

        public Sheet? GetSheet(string id)
        {
            return _sheets.FirstOrDefault(s => s.Id == id && s.IsActive);
        }

        public decimal GetPrice(string thickness, string color)
        {
            var sheet = _sheets.FirstOrDefault(s =>
                s.IsActive && s.Thickness == thickness &&
                s.Color.ToLower() == color.ToLower());
            return sheet?.SellPrice ?? 0;
        }

        public decimal GetPurchasePrice(string thickness, string color)
        {
            var sheet = _sheets.FirstOrDefault(s =>
                s.IsActive && s.Thickness == thickness &&
                s.Color.ToLower() == color.ToLower());
            return sheet?.PurchasePrice ?? 0;
        }

        public List<string> GetAllThicknesses()
        {
            return _sheets.Where(s => s.IsActive).Select(s => s.Thickness).Distinct().OrderBy(t => t).ToList();
        }

        public List<string> GetAllColors()
        {
            return _sheets.Where(s => s.IsActive).Select(s => s.Color).Distinct().OrderBy(c => c).ToList();
        }

        public List<string> GetAllCategories()
        {
            return _sheets.Where(s => s.IsActive).Select(s => s.Category).Distinct().OrderBy(c => c).ToList();
        }

        public List<PriceHistory> GetPurchaseHistory(string id)
        {
            var sheet = _sheets.FirstOrDefault(s => s.Id == id);
            if (sheet != null)
            {
                return sheet.PriceHistory.OrderByDescending(p => p.Date).ToList();
            }
            return new List<PriceHistory>();
        }

        public List<Sheet> SearchSheets(string? searchTerm, string? thickness, string? color, string? category)
        {
            var query = _sheets.Where(s => s.IsActive);

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(s =>
                    s.Thickness.Contains(searchTerm) ||
                    s.Color.ToLower().Contains(searchTerm.ToLower()) ||
                    s.Category.ToLower().Contains(searchTerm.ToLower()));

            if (!string.IsNullOrEmpty(thickness))
                query = query.Where(s => s.Thickness == thickness);

            if (!string.IsNullOrEmpty(color))
                query = query.Where(s => s.Color.ToLower().Contains(color.ToLower()));

            if (!string.IsNullOrEmpty(category))
                query = query.Where(s => s.Category.ToLower().Contains(category.ToLower()));

            return query.OrderBy(s => s.Thickness).ThenBy(s => s.Color).ToList();
        }

        // ==================== CRUD OPERATIONS ====================

        public (bool Success, string Message) AddSheet(Sheet sheet)
        {
            try
            {
                sheet.Id = Guid.NewGuid().ToString();
                sheet.CreatedDate = DateTime.Now;
                sheet.IsActive = true;
                _sheets.Add(sheet);
                SaveSheets();
                return (true, "Sheet added successfully");
            }
            catch (Exception ex)
            {
                return (false, "Error: " + ex.Message);
            }
        }

        public (bool Success, string Message) UpdateSheet(Sheet sheet)
        {
            try
            {
                var existing = _sheets.FirstOrDefault(s => s.Id == sheet.Id);
                if (existing != null)
                {
                    existing.Thickness = sheet.Thickness;
                    existing.Color = sheet.Color;
                    existing.Category = sheet.Category;
                    existing.Width = sheet.Width;
                    existing.Height = sheet.Height;
                    existing.PurchasePrice = sheet.PurchasePrice;
                    existing.SellPrice = sheet.SellPrice;
                    existing.LatestPurchaseDate = sheet.LatestPurchaseDate;
                    existing.PricePerSqft = sheet.PricePerSqft;
                    existing.PricePerSqmeter = sheet.PricePerSqmeter;
                    existing.Description = sheet.Description;
                    SaveSheets();
                    return (true, "Sheet updated successfully");
                }
                return (false, "Sheet not found");
            }
            catch (Exception ex)
            {
                return (false, "Error: " + ex.Message);
            }
        }

        public (bool Success, string Message) DeleteSheet(string id)
        {
            try
            {
                var sheet = _sheets.FirstOrDefault(s => s.Id == id);
                if (sheet != null)
                {
                    sheet.IsActive = false;
                    SaveSheets();
                    return (true, "Sheet deleted successfully");
                }
                return (false, "Sheet not found");
            }
            catch (Exception ex)
            {
                return (false, "Error: " + ex.Message);
            }
        }

        // ==================== PRICE UPDATE METHODS ====================

        public (bool Success, string Message) UpdatePurchasePrice(string id, decimal purchasePrice, string supplier = "", string notes = "")
        {
            try
            {
                var sheet = _sheets.FirstOrDefault(s => s.Id == id);
                if (sheet != null)
                {
                    var history = new PriceHistory
                    {
                        Date = DateTime.Now,
                        PurchasePrice = purchasePrice,
                        SellingPrice = sheet.SellPrice,
                        SupplierName = supplier,
                        Notes = notes
                    };
                    sheet.PriceHistory.Add(history);

                    sheet.PurchasePrice = purchasePrice;
                    sheet.LatestPurchaseDate = DateTime.Now;
                    sheet.LastPurchasePrice = purchasePrice;
                    sheet.LastPurchaseDate = DateTime.Now;
                    sheet.SupplierName = supplier;

                    SaveSheets();
                    return (true, "Purchase price updated successfully");
                }
                return (false, "Sheet not found");
            }
            catch (Exception ex)
            {
                return (false, "Error: " + ex.Message);
            }
        }

        public (bool Success, string Message) UpdateSellingPrice(string id, decimal sellingPrice)
        {
            try
            {
                var sheet = _sheets.FirstOrDefault(s => s.Id == id);
                if (sheet != null)
                {
                    sheet.SellPrice = sellingPrice;
                    sheet.PricePerSqft = sellingPrice;
                    sheet.PricePerSqmeter = sellingPrice * 10.764m;

                    if (sheet.PriceHistory.Count > 0)
                    {
                        var latest = sheet.PriceHistory.OrderByDescending(h => h.Date).First();
                        latest.SellingPrice = sellingPrice;
                    }

                    SaveSheets();
                    return (true, "Selling price updated successfully");
                }
                return (false, "Sheet not found");
            }
            catch (Exception ex)
            {
                return (false, "Error: " + ex.Message);
            }
        }

        public (bool Success, string Message) UpdateBothPrices(string id, decimal purchasePrice, decimal sellingPrice, string supplier = "", string notes = "")
        {
            try
            {
                var sheet = _sheets.FirstOrDefault(s => s.Id == id);
                if (sheet != null)
                {
                    var history = new PriceHistory
                    {
                        Date = DateTime.Now,
                        PurchasePrice = purchasePrice,
                        SellingPrice = sellingPrice,
                        SupplierName = supplier,
                        Notes = notes
                    };
                    sheet.PriceHistory.Add(history);

                    sheet.PurchasePrice = purchasePrice;
                    sheet.SellPrice = sellingPrice;
                    sheet.PricePerSqft = sellingPrice;
                    sheet.PricePerSqmeter = sellingPrice * 10.764m;
                    sheet.LatestPurchaseDate = DateTime.Now;
                    sheet.LastPurchasePrice = purchasePrice;
                    sheet.LastPurchaseDate = DateTime.Now;
                    sheet.SupplierName = supplier;

                    SaveSheets();
                    return (true, "Prices updated successfully");
                }
                return (false, "Sheet not found");
            }
            catch (Exception ex)
            {
                return (false, "Error: " + ex.Message);
            }
        }

        // ==================== EXCEL METHODS ====================

        public async Task<(bool Success, string Message)> ExportToExcelAsync(string filePath)
        {
            try
            {
                await Task.Run(() =>
                {
                    var sheets = GetAllSheets();
                    var lines = new List<string>
                    {
                        "Thickness,Color,Category,Width,Height,PurchasePrice,SellPrice,Supplier,LatestPurchaseDate,Description"
                    };

                    foreach (var sheet in sheets)
                    {
                        lines.Add($"\"{sheet.Thickness}\",\"{sheet.Color}\",\"{sheet.Category}\",{sheet.Width},{sheet.Height},{sheet.PurchasePrice},{sheet.SellPrice},\"{sheet.SupplierName}\",\"{sheet.LatestPurchaseDate:yyyy-MM-dd}\",\"{sheet.Description}\"");
                    }

                    File.WriteAllLines(filePath, lines);
                });

                return (true, "Exported successfully to " + filePath);
            }
            catch (Exception ex)
            {
                return (false, "Export error: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> ImportFromExcelAsync(string filePath)
        {
            try
            {
                var result = await Task.Run<(bool Success, string Message)>(() =>
                {
                    try
                    {
                        var lines = File.ReadAllLines(filePath);
                        if (lines.Length <= 1)
                            return (false, "No data found in file");

                        int imported = 0;
                        for (int i = 1; i < lines.Length; i++)
                        {
                            var parts = ParseCSVLine(lines[i]);
                            if (parts.Length >= 7)
                            {
                                var sheet = new Sheet
                                {
                                    Thickness = parts[0].Trim('"'),
                                    Color = parts[1].Trim('"'),
                                    Category = parts[2].Trim('"'),
                                    Width = decimal.TryParse(parts[3], out var w) ? w : 2440,
                                    Height = decimal.TryParse(parts[4], out var h) ? h : 1830,
                                    PurchasePrice = decimal.TryParse(parts[5], out var p) ? p : 0,
                                    SellPrice = decimal.TryParse(parts[6], out var s) ? s : 0,
                                    SupplierName = parts.Length > 7 ? parts[7].Trim('"') : "",
                                    Description = parts.Length > 9 ? parts[9].Trim('"') : ""
                                };

                                if (!string.IsNullOrEmpty(sheet.Thickness) && !string.IsNullOrEmpty(sheet.Color))
                                {
                                    var existing = _sheets.FirstOrDefault(x =>
                                        x.IsActive && x.Thickness == sheet.Thickness && x.Color == sheet.Color);

                                    if (existing != null)
                                    {
                                        existing.PurchasePrice = sheet.PurchasePrice;
                                        existing.SellPrice = sheet.SellPrice;
                                        existing.SupplierName = sheet.SupplierName;
                                        existing.Description = sheet.Description;
                                    }
                                    else
                                    {
                                        AddSheet(sheet);
                                    }
                                    imported++;
                                }
                            }
                        }

                        SaveSheets();
                        return (true, $"Imported {imported} sheets successfully");
                    }
                    catch (Exception ex)
                    {
                        return (false, "Import error: " + ex.Message);
                    }
                });

                return result;
            }
            catch (Exception ex)
            {
                return (false, "Import error: " + ex.Message);
            }
        }

        private string[] ParseCSVLine(string line)
        {
            var result = new List<string>();
            var current = "";
            var inQuotes = false;

            foreach (var c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
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
            return result.ToArray();
        }

        // ==================== LOAD / SAVE ====================

        private List<Sheet> LoadSheets()
        {
            try
            {
                if (File.Exists(_dataFile))
                {
                    string json = File.ReadAllText(_dataFile);
                    var sheets = JsonSerializer.Deserialize<List<Sheet>>(json);
                    if (sheets != null && sheets.Count > 0) return sheets;
                }
            }
            catch { }
            return GetDefaultSheets();
        }

        private void SaveSheets()
        {
            try
            {
                string json = JsonSerializer.Serialize(_sheets, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_dataFile, json);
            }
            catch { }
        }

        private List<Sheet> GetDefaultSheets()
        {
            return new List<Sheet>
            {
                new Sheet { Thickness = "4", Color = "Clear", Category = "Clear Glass", Width = 2440, Height = 1830, PurchasePrice = 100, SellPrice = 120 },
                new Sheet { Thickness = "5", Color = "Clear", Category = "Clear Glass", Width = 2440, Height = 1830, PurchasePrice = 110, SellPrice = 130 },
                new Sheet { Thickness = "6", Color = "Clear", Category = "Clear Glass", Width = 2440, Height = 1830, PurchasePrice = 130, SellPrice = 150 },
                new Sheet { Thickness = "8", Color = "Clear", Category = "Clear Glass", Width = 2440, Height = 1830, PurchasePrice = 160, SellPrice = 180 },
                new Sheet { Thickness = "4", Color = "Grey", Category = "Tinted Glass", Width = 2440, Height = 1830, PurchasePrice = 110, SellPrice = 130 },
                new Sheet { Thickness = "6", Color = "Grey", Category = "Tinted Glass", Width = 2440, Height = 1830, PurchasePrice = 140, SellPrice = 160 },
                new Sheet { Thickness = "4", Color = "Bronze", Category = "Tinted Glass", Width = 2440, Height = 1830, PurchasePrice = 110, SellPrice = 130 },
                new Sheet { Thickness = "6", Color = "Bronze", Category = "Tinted Glass", Width = 2440, Height = 1830, PurchasePrice = 140, SellPrice = 160 },
                new Sheet { Thickness = "4", Color = "Green", Category = "Tinted Glass", Width = 2440, Height = 1830, PurchasePrice = 120, SellPrice = 140 },
                new Sheet { Thickness = "6", Color = "Green", Category = "Tinted Glass", Width = 2440, Height = 1830, PurchasePrice = 150, SellPrice = 170 },
            };
        }
    }
}