using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Services
{
    public class SheetStoreService
    {
        private static SheetStoreService _instance;
        public static SheetStoreService Instance => _instance ??= new SheetStoreService();

        private readonly string _filePath = "sheets.xml";
        private ObservableCollection<Sheet> _sheets;

        // ═══════════════════════════════════════════════════════
        // FIX #2: ATOMIC SNAPSHOT
        // ═══════════════════════════════════════════════════════
        private static volatile CacheSnapshot _cache;

        private sealed class CacheSnapshot
        {
            public List<Sheet> Sheets { get; init; } = new();
            public Dictionary<int, Sheet> IdLookup { get; init; } = new();
            public Dictionary<string, List<Sheet>> CategoryLookup { get; init; } = new();
            public Dictionary<string, List<Sheet>> SearchLookup { get; init; } = new();
            public HashSet<string> Categories { get; init; } = new();
            public DateTime Timestamp { get; init; }
            public static CacheSnapshot Empty => new() { Timestamp = DateTime.MinValue };
        }

        private static CacheSnapshot Cache => _cache;

        // ═══════════════════════════════════════════════════════
        // FIX #7: EventHandler signature
        // ═══════════════════════════════════════════════════════
        public event EventHandler DataChanged;

        private SheetStoreService()
        {
            _sheets = LoadFromFile();
            SetCache();
        }

        // ═══════════════════════════════════════════════════════
        // FIX #3: Single SetCache() call
        // ═══════════════════════════════════════════════════════
        private void SetCache()
        {
            var sheets = _sheets.Where(s => s.IsActive).OrderBy(s => s.SrNo).ToList();
            if (sheets.Count == 0)
            {
                _cache = CacheSnapshot.Empty;
                return;
            }

            var idLookup = new Dictionary<int, Sheet>(sheets.Count);
            var categoryLookup = new Dictionary<string, List<Sheet>>(StringComparer.OrdinalIgnoreCase);
            var searchLookup = new Dictionary<string, List<Sheet>>(StringComparer.OrdinalIgnoreCase);
            var categories = new HashSet<string>();

            foreach (var s in sheets)
            {
                if (!idLookup.ContainsKey(s.Id))
                    idLookup[s.Id] = s;

                if (!categoryLookup.TryGetValue(s.Category, out var catList))
                    categoryLookup[s.Category] = catList = new List<Sheet>();
                catList.Add(s);

                string searchKey = $"{s.Category}|{s.Thickness}|{s.Color}|{s.Supplier}".ToLower();
                if (!searchLookup.TryGetValue(searchKey, out var searchList))
                    searchLookup[searchKey] = searchList = new List<Sheet>();
                searchList.Add(s);

                categories.Add(s.Category);
            }

            _cache = new CacheSnapshot
            {
                Sheets = sheets,
                IdLookup = idLookup,
                CategoryLookup = categoryLookup,
                SearchLookup = searchLookup,
                Categories = categories,
                Timestamp = DateTime.Now
            };
        }

        // ═══════════════════════════════════════════════════════
        // FIX #8: Atomic invalidation
        // ═══════════════════════════════════════════════════════
        public void InvalidateCache()
        {
            _cache = CacheSnapshot.Empty;
        }

        // ═══════════════════════════════════════════════════════
        // FIX #4: O(1) lookups
        // ═══════════════════════════════════════════════════════
        public ObservableCollection<Sheet> GetAllActive(bool forceRefresh = false)
        {
            if (!forceRefresh && Cache.Sheets.Count > 0)
                return new ObservableCollection<Sheet>(Cache.Sheets);

            if (forceRefresh || _cache == CacheSnapshot.Empty)
            {
                _sheets = LoadFromFile();
                SetCache();
            }

            return new ObservableCollection<Sheet>(Cache.Sheets);
        }

        public Sheet GetById(int id)
        {
            Cache.IdLookup.TryGetValue(id, out var sheet);
            return sheet;
        }

        public ObservableCollection<Sheet> Search(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return GetAllActive();

            keyword = keyword.ToLower();

            if (Cache.SearchLookup.TryGetValue(keyword, out var exact))
                return new ObservableCollection<Sheet>(exact);

            var results = Cache.Sheets.Where(s =>
                s.Category.ToLower().Contains(keyword) ||
                s.Thickness.ToLower().Contains(keyword) ||
                s.Color.ToLower().Contains(keyword) ||
                s.Supplier.ToLower().Contains(keyword)).ToList();

            return new ObservableCollection<Sheet>(results);
        }

        public ObservableCollection<Sheet> GetByCategory(string category)
        {
            if (Cache.CategoryLookup.TryGetValue(category, out var sheets))
                return new ObservableCollection<Sheet>(sheets);
            return new ObservableCollection<Sheet>();
        }

        public ObservableCollection<string> GetCategories()
        {
            return new ObservableCollection<string>(Cache.Categories.OrderBy(c => c));
        }

        // ═══════════════════════════════════════════════════════
        // FIX #7: EventHandler with correct signature
        // ═══════════════════════════════════════════════════════
        private void NotifyDataChanged(object sender, EventArgs e)
        {
            InvalidateCache();
            DataChanged?.Invoke(this, EventArgs.Empty);
        }

        // ==================== CRUD ====================
        public void AddSheet(Sheet sheet)
        {
            sheet.Id = _sheets.Count > 0 ? _sheets.Max(s => s.Id) + 1 : 1;
            sheet.SrNo = _sheets.Count > 0 ? _sheets.Max(s => s.SrNo) + 1 : 1;
            sheet.CreatedDate = DateTime.Now;
            sheet.BalanceSheets = sheet.TotalStock;
            _sheets.Add(sheet);
            SaveToFile();
            NotifyDataChanged(this, EventArgs.Empty);
        }

        public void UpdateSheet(Sheet sheet)
        {
            for (int i = 0; i < _sheets.Count; i++)
            {
                if (_sheets[i].Id == sheet.Id)
                {
                    _sheets[i] = sheet;
                    SaveToFile();
                    NotifyDataChanged(this, EventArgs.Empty);
                    return;
                }
            }
        }

        public void DeleteSheet(int id)
        {
            for (int i = 0; i < _sheets.Count; i++)
            {
                if (_sheets[i].Id == id)
                {
                    _sheets[i].IsActive = false;
                    SaveToFile();
                    NotifyDataChanged(this, EventArgs.Empty);
                    return;
                }
            }
        }

        public void BulkUpdatePrices(List<Sheet> sheets)
        {
            foreach (var sheet in sheets)
            {
                for (int i = 0; i < _sheets.Count; i++)
                {
                    if (_sheets[i].Id == sheet.Id)
                    {
                        _sheets[i] = sheet;
                        break;
                    }
                }
            }
            SaveToFile();
            NotifyDataChanged(this, EventArgs.Empty);
        }

        public void BulkUpdatePricesByCategory(string category, decimal purchasePrice, decimal sellPrice)
        {
            foreach (var sheet in _sheets.Where(s => s.IsActive && s.Category == category))
            {
                sheet.PurchasePrice = purchasePrice;
                sheet.SellPrice = sellPrice;
            }
            SaveToFile();
            NotifyDataChanged(this, EventArgs.Empty);
        }

        public void UpdatePurchasePrice(int id, decimal price)
        {
            var sheet = GetById(id);
            if (sheet != null)
            {
                sheet.PurchasePrice = price;
                SaveToFile();
                NotifyDataChanged(this, EventArgs.Empty);
            }
        }

        public void UpdatePurchasePriceByCategory(string category, decimal price)
        {
            foreach (var sheet in _sheets.Where(s => s.IsActive && s.Category == category))
                sheet.PurchasePrice = price;
            SaveToFile();
            NotifyDataChanged(this, EventArgs.Empty);
        }

        public void RecordUsage(int id, int used)
        {
            var sheet = GetById(id);
            if (sheet != null)
            {
                sheet.UsedSheets += used;
                sheet.BalanceSheets = sheet.TotalStock - sheet.UsedSheets;
                SaveToFile();
                NotifyDataChanged(this, EventArgs.Empty);
            }
        }

        // ==================== EXPORT / IMPORT ====================
        public void ExportToExcel(string filePath)
        {
            var lines = new List<string> { "SrNo,Category,Thickness,Color,Width,Height,SQM,Purchase,Sell,Stock,Used,Balance,Supplier,Date" };
            foreach (var s in Cache.Sheets)
            {
                lines.Add($"{s.SrNo},{s.Category},{s.Thickness},{s.Color},{s.Width},{s.Height},{s.SquareMeter},{s.PurchasePrice},{s.SellPrice},{s.TotalStock},{s.UsedSheets},{s.BalanceSheets},{s.Supplier},{s.CreatedDate}");
            }
            File.WriteAllLines(filePath, lines);
        }

        public int ImportFromExcel(string filePath)
        {
            if (!File.Exists(filePath)) return 0;
            var lines = File.ReadAllLines(filePath);
            int count = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 10)
                {
                    var sheet = new Sheet
                    {
                        Category = parts[1],
                        Thickness = parts[2],
                        Color = parts[3],
                        Width = int.TryParse(parts[4], out int w) ? w : 0,
                        Height = int.TryParse(parts[5], out int h) ? h : 0,
                        PurchasePrice = decimal.TryParse(parts[7], out decimal p) ? p : 0,
                        SellPrice = decimal.TryParse(parts[8], out decimal s) ? s : 0,
                        TotalStock = int.TryParse(parts[9], out int t) ? t : 0,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };
                    sheet.SquareMeter = Math.Round(sheet.Width * sheet.Height / 1000000.0, 2);
                    sheet.BalanceSheets = sheet.TotalStock;
                    AddSheet(sheet);
                    count++;
                }
            }
            return count;
        }

        // ==================== FILE LOAD ====================
        private ObservableCollection<Sheet> LoadFromFile()
        {
            try
            {
                if (!File.Exists(_filePath)) return new ObservableCollection<Sheet>();
                var doc = XDocument.Load(_filePath);
                var sheets = new ObservableCollection<Sheet>();
                foreach (var el in doc.Root.Elements("Sheet"))
                {
                    sheets.Add(new Sheet
                    {
                        Id = int.Parse(el.Attribute("Id")?.Value ?? "0"),
                        SrNo = int.Parse(el.Attribute("SrNo")?.Value ?? "0"),
                        Category = el.Attribute("Category")?.Value ?? "",
                        Thickness = el.Attribute("Thickness")?.Value ?? "",
                        Color = el.Attribute("Color")?.Value ?? "",
                        ColorHex = el.Attribute("ColorHex")?.Value ?? "#E8F4F8",
                        Width = int.Parse(el.Attribute("Width")?.Value ?? "0"),
                        Height = int.Parse(el.Attribute("Height")?.Value ?? "0"),
                        SquareMeter = double.Parse(el.Attribute("SquareMeter")?.Value ?? "0"),
                        PurchasePrice = decimal.Parse(el.Attribute("PurchasePrice")?.Value ?? "0"),
                        SellPrice = decimal.Parse(el.Attribute("SellPrice")?.Value ?? "0"),
                        TotalStock = int.Parse(el.Attribute("TotalStock")?.Value ?? "0"),
                        UsedSheets = int.Parse(el.Attribute("UsedSheets")?.Value ?? "0"),
                        BalanceSheets = int.Parse(el.Attribute("BalanceSheets")?.Value ?? "0"),
                        IsActive = bool.Parse(el.Attribute("IsActive")?.Value ?? "true"),
                        Supplier = el.Attribute("Supplier")?.Value ?? "",
                        SupplierName = el.Attribute("SupplierName")?.Value ?? "",
                        Description = el.Attribute("Description")?.Value ?? "",
                        CreatedDate = DateTime.Parse(el.Attribute("CreatedDate")?.Value ?? DateTime.Now.ToString()),
                        LatestPurchaseDate = !string.IsNullOrEmpty(el.Attribute("LatestPurchaseDate")?.Value)
                            ? DateTime.Parse(el.Attribute("LatestPurchaseDate").Value) : (DateTime?)null
                    });
                }
                return sheets;
            }
            catch
            {
                return new ObservableCollection<Sheet>();
            }
        }

        // ==================== FILE SAVE ====================
        private void SaveToFile()
        {
            try
            {
                var doc = new XDocument(new XElement("Sheets"));
                foreach (var sheet in _sheets)
                {
                    doc.Root.Add(new XElement("Sheet",
                        new XAttribute("Id", sheet.Id),
                        new XAttribute("SrNo", sheet.SrNo),
                        new XAttribute("Category", sheet.Category ?? ""),
                        new XAttribute("Thickness", sheet.Thickness ?? ""),
                        new XAttribute("Color", sheet.Color ?? ""),
                        new XAttribute("ColorHex", sheet.ColorHex ?? "#E8F4F8"),
                        new XAttribute("Width", sheet.Width),
                        new XAttribute("Height", sheet.Height),
                        new XAttribute("SquareMeter", sheet.SquareMeter),
                        new XAttribute("PurchasePrice", sheet.PurchasePrice),
                        new XAttribute("SellPrice", sheet.SellPrice),
                        new XAttribute("TotalStock", sheet.TotalStock),
                        new XAttribute("UsedSheets", sheet.UsedSheets),
                        new XAttribute("BalanceSheets", sheet.BalanceSheets),
                        new XAttribute("IsActive", sheet.IsActive),
                        new XAttribute("Supplier", sheet.Supplier ?? ""),
                        new XAttribute("SupplierName", sheet.SupplierName ?? ""),
                        new XAttribute("Description", sheet.Description ?? ""),
                        new XAttribute("CreatedDate", sheet.CreatedDate),
                        new XAttribute("LatestPurchaseDate", sheet.LatestPurchaseDate?.ToString() ?? "")
                    ));
                }
                doc.Save(_filePath);
            }
            catch { }
        }
    }
}