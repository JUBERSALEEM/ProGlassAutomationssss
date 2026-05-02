using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Services
{
    public class SheetStoreService
    {
        private static SheetStoreService? _instance;
        public static SheetStoreService Instance => _instance ??= new SheetStoreService();

        private readonly string _dataFile;
        private List<Sheet> _sheets;

        public List<string> ThicknessOptions { get; } = new List<string> { "4mm", "6mm", "8mm", "10mm", "12mm", "15mm", "19mm" };

        public List<string> ColorOptions { get; } = new List<string> { "Clear", "HD Grey", "HD Blue", "HD Brown", "HD Green" };

        public List<string> CategoryOptions { get; } = new List<string> { "HD Series", "Belgium Series", "PNA", "Tinted Series", "Sunlux Series" };

        private SheetStoreService()
        {
            _dataFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sheets.json");
            _sheets = LoadSheets();
        }

        public List<Sheet> GetAllSheets()
        {
            return _sheets.Where(s => s.IsActive).OrderBy(s => s.Category).ThenBy(s => s.Thickness).ThenBy(s => s.Color).ToList();
        }

        public Sheet? GetSheet(string id)
        {
            return _sheets.FirstOrDefault(s => s.Id == id && s.IsActive);
        }

        public List<string> GetAllThicknesses()
        {
            return ThicknessOptions;
        }

        public List<string> GetAllColors()
        {
            return _sheets.Where(s => s.IsActive).Select(s => s.Color).Distinct().OrderBy(c => c).ToList();
        }

        public List<string> GetAllCategories()
        {
            return CategoryOptions;
        }

        public List<PriceHistory> GetPriceHistory(string id)
        {
            var sheet = _sheets.FirstOrDefault(s => s.Id == id);
            return sheet?.PriceHistory.OrderByDescending(p => p.Date).ThenByDescending(p => p.Time).ToList() ?? new List<PriceHistory>();
        }

        public bool AddSheet(Sheet sheet)
        {
            try
            {
                sheet.Id = Guid.NewGuid().ToString();
                sheet.CreatedDate = DateTime.Now;
                sheet.IsActive = true;
                _sheets.Add(sheet);
                SaveSheets();
                return true;
            }
            catch { return false; }
        }

        public bool UpdateSheet(Sheet sheet)
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
                    existing.LatestPurchaseTime = sheet.LatestPurchaseTime;
                    existing.PricePerSqft = sheet.PricePerSqft;
                    existing.PricePerSqmeter = sheet.PricePerSqmeter;
                    existing.Description = sheet.Description;
                    SaveSheets();
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        public bool DeleteSheet(string id)
        {
            try
            {
                var sheet = _sheets.FirstOrDefault(s => s.Id == id);
                if (sheet != null)
                {
                    sheet.IsActive = false;
                    SaveSheets();
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        public bool UpdateBothPrices(string id, decimal purchasePrice, decimal sellingPrice, string supplier = "", string notes = "")
        {
            try
            {
                var sheet = _sheets.FirstOrDefault(s => s.Id == id);
                if (sheet != null)
                {
                    var now = DateTime.Now;
                    var history = new PriceHistory
                    {
                        Date = now.Date,
                        Time = now,
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
                    sheet.LatestPurchaseDate = now.Date;
                    sheet.LatestPurchaseTime = now;
                    sheet.LastPurchasePrice = purchasePrice;
                    sheet.LastPurchaseDate = now.Date;
                    sheet.LastPurchaseTime = now;
                    sheet.SupplierName = supplier;

                    SaveSheets();
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

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
            var sheets = new List<Sheet>();
            decimal basePrice = 75;
            decimal priceIncrement = 15;

            foreach (var category in CategoryOptions)
            {
                foreach (var thickness in ThicknessOptions)
                {
                    foreach (var color in ColorOptions)
                    {
                        int thicknessIndex = ThicknessOptions.IndexOf(thickness);
                        decimal purchasePrice = basePrice + (thicknessIndex * priceIncrement);
                        decimal sellPrice = purchasePrice * 1.2m;

                        if (category == "Belgium Series")
                        {
                            purchasePrice *= 1.3m;
                            sellPrice *= 1.3m;
                        }
                        else if (category == "Sunlux Series")
                        {
                            purchasePrice *= 1.4m;
                            sellPrice *= 1.4m;
                        }

                        sheets.Add(new Sheet
                        {
                            Thickness = thickness,
                            Color = color,
                            Category = category,
                            Width = 2440,
                            Height = 1830,
                            PurchasePrice = Math.Round(purchasePrice, 2),
                            SellPrice = Math.Round(sellPrice, 2)
                        });
                    }
                }
            }

            return sheets;
        }
    }
}