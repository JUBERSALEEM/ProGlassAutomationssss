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

        private readonly string _filePath;
        private List<Sheet> _sheets;
        private readonly object _lock = new object();

        // ═══════════════════════════════════════════════════════
        // Cache system
        // ═══════════════════════════════════════════════════════
        private List<Sheet> _cachedSheets;
        private HashSet<string> _cachedCategories;

        public event EventHandler DataChanged;

        // ═══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════
        private SheetStoreService()
        {
            string appFolder = AppDomain.CurrentDomain.BaseDirectory;
            _filePath = Path.Combine(appFolder, "sheets_data.xml");

            _sheets = LoadFromFile();
            RefreshCache();

            System.Diagnostics.Debug.WriteLine($"SheetStoreService initialized. Loaded {_sheets.Count} sheets.");
        }

        // ═══════════════════════════════════════════════════════
        // CACHE MANAGEMENT
        // ═══════════════════════════════════════════════════════
        private void RefreshCache()
        {
            _cachedSheets = _sheets.Where(s => s.IsActive).OrderBy(s => s.SrNo).ToList();
            _cachedCategories = new HashSet<string>(_cachedSheets.Select(s => s.Category));

            System.Diagnostics.Debug.WriteLine($"Cache refreshed: {_cachedSheets.Count} sheets, {_cachedCategories.Count} categories");
        }

        private void NotifyDataChanged()
        {
            System.Diagnostics.Debug.WriteLine("NotifyDataChanged: Firing event...");
            DataChanged?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine("NotifyDataChanged: Event fired!");
        }

        // ═══════════════════════════════════════════════════════
        // DATA RETRIEVAL
        // ═══════════════════════════════════════════════════════
        public ObservableCollection<Sheet> GetAllActive()
        {
            _sheets = LoadFromFile();
            RefreshCache();

            System.Diagnostics.Debug.WriteLine($"GetAllActive: Returning {_cachedSheets.Count} sheets");
            return new ObservableCollection<Sheet>(_cachedSheets);
        }

        public Sheet? GetById(int id)
        {
            return _cachedSheets?.FirstOrDefault(s => s.Id == id);
        }

        public ObservableCollection<Sheet> GetByCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
                return GetAllActive();

            var sheets = _cachedSheets?.Where(s =>
                s.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();

            return new ObservableCollection<Sheet>(sheets ?? new List<Sheet>());
        }

        public ObservableCollection<Sheet> Search(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return GetAllActive();

            keyword = keyword.ToLower();
            var results = _cachedSheets?.Where(s =>
                s.Category.ToLower().Contains(keyword) ||
                s.Thickness.ToLower().Contains(keyword) ||
                s.Color.ToLower().Contains(keyword) ||
                (s.Supplier?.ToLower().Contains(keyword) ?? false)).ToList();

            return new ObservableCollection<Sheet>(results ?? new List<Sheet>());
        }

        public ObservableCollection<string> GetCategories()
        {
            if (_cachedCategories == null)
            {
                RefreshCache();
            }
            return new ObservableCollection<string>(_cachedCategories.OrderBy(c => c));
        }

        // ═══════════════════════════════════════════════════════
        // ADD SHEET
        // ═══════════════════════════════════════════════════════
        public void AddSheet(Sheet sheet)
        {
            lock (_lock)
            {
                try
                {
                    int newId = (_sheets.Count > 0) ? _sheets.Max(s => s.Id) + 1 : 1;
                    int newSrNo = (_sheets.Count > 0) ? _sheets.Max(s => s.SrNo) + 1 : 1;

                    var newSheet = new Sheet
                    {
                        Id = newId,
                        SrNo = newSrNo,
                        Category = sheet.Category ?? "",
                        Thickness = sheet.Thickness ?? "",
                        Color = sheet.Color ?? "",
                        ColorHex = sheet.ColorHex ?? "#E8F4F8",
                        Width = sheet.Width,
                        Height = sheet.Height,
                        SquareMeter = sheet.SquareMeter,
                        PurchasePrice = sheet.PurchasePrice,
                        SellPrice = sheet.SellPrice,
                        TotalStock = sheet.TotalStock,
                        UsedSheets = sheet.UsedSheets,
                        BalanceSheets = sheet.TotalStock - sheet.UsedSheets,
                        IsActive = true,
                        Supplier = sheet.Supplier ?? "",
                        SupplierName = sheet.SupplierName ?? "",
                        Description = sheet.Description ?? "",
                        CreatedDate = DateTime.Now,
                        LatestPurchaseDate = sheet.LatestPurchaseDate
                    };

                    _sheets.Add(newSheet);
                    System.Diagnostics.Debug.WriteLine($"AddSheet: Added sheet ID={newId}, Category={newSheet.Category}");

                    SaveToFile();
                    RefreshCache();
                    NotifyDataChanged();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adding sheet: {ex.Message}");
                    throw;
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        // UPDATE SHEET
        // ═══════════════════════════════════════════════════════
        public void UpdateSheet(Sheet sheet)
        {
            lock (_lock)
            {
                try
                {
                    int index = -1;
                    for (int i = 0; i < _sheets.Count; i++)
                    {
                        if (_sheets[i].Id == sheet.Id)
                        {
                            index = i;
                            break;
                        }
                    }

                    if (index >= 0)
                    {
                        _sheets[index].Category = sheet.Category ?? _sheets[index].Category;
                        _sheets[index].Thickness = sheet.Thickness ?? _sheets[index].Thickness;
                        _sheets[index].Color = sheet.Color ?? _sheets[index].Color;
                        _sheets[index].ColorHex = sheet.ColorHex ?? _sheets[index].ColorHex;
                        _sheets[index].Width = sheet.Width;
                        _sheets[index].Height = sheet.Height;
                        _sheets[index].SquareMeter = sheet.SquareMeter;
                        _sheets[index].PurchasePrice = sheet.PurchasePrice;
                        _sheets[index].SellPrice = sheet.SellPrice;
                        _sheets[index].TotalStock = sheet.TotalStock;
                        _sheets[index].UsedSheets = sheet.UsedSheets;
                        _sheets[index].BalanceSheets = sheet.TotalStock - sheet.UsedSheets;
                        _sheets[index].Supplier = sheet.Supplier ?? _sheets[index].Supplier;
                        _sheets[index].SupplierName = sheet.SupplierName ?? _sheets[index].SupplierName;
                        _sheets[index].Description = sheet.Description ?? _sheets[index].Description;
                        _sheets[index].LatestPurchaseDate = sheet.LatestPurchaseDate ?? _sheets[index].LatestPurchaseDate;

                        System.Diagnostics.Debug.WriteLine($"UpdateSheet: Updated ID={sheet.Id}, Stock={sheet.TotalStock}");

                        SaveToFile();
                        RefreshCache();
                        NotifyDataChanged();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"UpdateSheet: Sheet not found ID={sheet.Id}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating sheet: {ex.Message}");
                    throw;
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        // DELETE SHEET
        // ═══════════════════════════════════════════════════════
        public void DeleteSheet(int id)
        {
            lock (_lock)
            {
                try
                {
                    for (int i = 0; i < _sheets.Count; i++)
                    {
                        if (_sheets[i].Id == id)
                        {
                            _sheets[i].IsActive = false;
                            System.Diagnostics.Debug.WriteLine($"DeleteSheet: Soft deleted ID={id}");

                            SaveToFile();
                            RefreshCache();
                            NotifyDataChanged();
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deleting sheet: {ex.Message}");
                    throw;
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        // FILE OPERATIONS
        // ═══════════════════════════════════════════════════════
        private List<Sheet> LoadFromFile()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"File not found: {_filePath}, creating new");
                    return new List<Sheet>();
                }

                var doc = XDocument.Load(_filePath);
                var sheets = new List<Sheet>();

                if (doc.Root == null)
                    return sheets;

                foreach (var el in doc.Root.Elements("Sheet"))
                {
                    try
                    {
                        var sheet = new Sheet
                        {
                            Id = int.Parse(el.Attribute("Id")?.Value ?? "0"),
                            SrNo = int.Parse(el.Attribute("SrNo")?.Value ?? "0"),
                            Category = el.Attribute("Category")?.Value ?? "",
                            Thickness = el.Attribute("Thickness")?.Value ?? "",
                            Color = el.Attribute("Color")?.Value ?? "",
                            ColorHex = el.Attribute("ColorHex")?.Value ?? "#E8F4F8",
                            Width = int.TryParse(el.Attribute("Width")?.Value, out int w) ? w : 0,
                            Height = int.TryParse(el.Attribute("Height")?.Value, out int h) ? h : 0,
                            SquareMeter = double.TryParse(el.Attribute("SquareMeter")?.Value, out double sm) ? sm : 0,
                            PurchasePrice = decimal.TryParse(el.Attribute("PurchasePrice")?.Value, out decimal pp) ? pp : 0,
                            SellPrice = decimal.TryParse(el.Attribute("SellPrice")?.Value, out decimal sp) ? sp : 0,
                            TotalStock = int.TryParse(el.Attribute("TotalStock")?.Value, out int ts) ? ts : 0,
                            UsedSheets = int.TryParse(el.Attribute("UsedSheets")?.Value, out int us) ? us : 0,
                            BalanceSheets = int.TryParse(el.Attribute("BalanceSheets")?.Value, out int bs) ? bs : 0,
                            IsActive = bool.TryParse(el.Attribute("IsActive")?.Value, out bool ia) ? ia : true,
                            Supplier = el.Attribute("Supplier")?.Value ?? "",
                            SupplierName = el.Attribute("SupplierName")?.Value ?? "",
                            Description = el.Attribute("Description")?.Value ?? "",
                            CreatedDate = DateTime.TryParse(el.Attribute("CreatedDate")?.Value, out DateTime cd) ? cd : DateTime.Now,
                            LatestPurchaseDate = DateTime.TryParse(el.Attribute("LatestPurchaseDate")?.Value, out DateTime lpd) ? lpd : (DateTime?)null
                        };
                        sheets.Add(sheet);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error parsing element: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"LoadFromFile: Loaded {sheets.Count} sheets");
                return sheets;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading file: {ex.Message}");
                return new List<Sheet>();
            }
        }

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
                System.Diagnostics.Debug.WriteLine($"SaveToFile: Saved {_sheets.Count} sheets");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving file: {ex.Message}");
                throw new Exception($"Failed to save: {ex.Message}", ex);
            }
        }

        // ═══════════════════════════════════════════════════════
        // EXPORT / IMPORT
        // ═══════════════════════════════════════════════════════
        public void ExportToExcel(string filePath)
        {
            try
            {
                var lines = new List<string>
                {
                    "SrNo,Category,Thickness,Color,Width,Height,SQM,Purchase,Sell,Stock,Used,Balance,Supplier,Date"
                };

                foreach (var s in _cachedSheets ?? Enumerable.Empty<Sheet>())
                {
                    lines.Add($"{s.SrNo},{s.Category},{s.Thickness},{s.Color},{s.Width},{s.Height},{s.SquareMeter},{s.PurchasePrice},{s.SellPrice},{s.TotalStock},{s.UsedSheets},{s.BalanceSheets},{s.Supplier},{s.CreatedDate}");
                }

                File.WriteAllLines(filePath, lines);
                System.Diagnostics.Debug.WriteLine($"Exported {lines.Count - 1} sheets");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting: {ex.Message}");
                throw;
            }
        }

        public int ImportFromExcel(string filePath)
        {
            try
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

                System.Diagnostics.Debug.WriteLine($"Imported {count} sheets");
                return count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error importing: {ex.Message}");
                throw;
            }
        }
    }
}