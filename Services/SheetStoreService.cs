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
        private static SheetStoreService? _instance;
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
            _cachedSheets = _sheets
                .Where(s => s.IsActive)
                .OrderBy(s => s.SrNo)
                .ToList();

            _cachedCategories = new HashSet<string>(
                _cachedSheets.Select(s => s.Category ?? "")
            );

            System.Diagnostics.Debug.WriteLine(
                $"Cache refreshed: {_cachedSheets.Count} active sheets, {_cachedCategories.Count} categories");
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

        // ✅ FIX C — Don't reload from disk on every call.
        // Use the in-memory cache as source of truth.
        public ObservableCollection<Sheet> GetAllActive()
        {
            lock (_lock)
            {
                if (_cachedSheets == null)
                    RefreshCache();

                System.Diagnostics.Debug.WriteLine(
                    $"GetAllActive: Returning {_cachedSheets.Count} sheets (from cache)");

                return new ObservableCollection<Sheet>(_cachedSheets);
            }
        }

        // Optional: keep an explicit force-reload-from-disk method if you ever need it
        public ObservableCollection<Sheet> ReloadFromDisk()
        {
            lock (_lock)
            {
                _sheets = LoadFromFile();
                RefreshCache();

                System.Diagnostics.Debug.WriteLine(
                    $"ReloadFromDisk: Reloaded {_sheets.Count} sheets, {_cachedSheets.Count} active");

                return new ObservableCollection<Sheet>(_cachedSheets);
            }
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
                RefreshCache();

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
                        LatestPurchaseDate = sheet.LatestPurchaseDate,
                        PurchaseHistory = new List<SheetPurchase>(),
                        UseHistory = new List<SheetUsage>()
                    };

                    _sheets.Add(newSheet);
                    System.Diagnostics.Debug.WriteLine(
                        $"AddSheet: Added sheet ID={newId}, Category={newSheet.Category}");

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

                        System.Diagnostics.Debug.WriteLine(
                            $"UpdateSheet: Updated ID={sheet.Id}, Stock={sheet.TotalStock}");

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
        // DELETE SHEET — SOFT DELETE (IsActive = false)
        // ═══════════════════════════════════════════════════════
        public void DeleteSheet(int id)
        {
            lock (_lock)
            {
                try
                {
                    var target = _sheets.FirstOrDefault(s => s.Id == id);
                    if (target == null)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"DeleteSheet: ID={id} not found in _sheets");
                        return;
                    }

                    target.IsActive = false;
                    System.Diagnostics.Debug.WriteLine(
                        $"DeleteSheet: Soft deleted ID={id} (IsActive=false)");

                    SaveToFile();       // persist
                    RefreshCache();     // exclude from cache
                    NotifyDataChanged();// notify VMs
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deleting sheet: {ex.Message}");
                    throw;
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        // PURCHASE SHEETS
        // ═══════════════════════════════════════════════════════
        public void AddPurchaseRecord(SheetPurchase record)
        {
            lock (_lock)
            {
                try
                {
                    var sheet = _sheets.FirstOrDefault(s => s.Id == record.SheetId);
                    if (sheet == null)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"AddPurchaseRecord: Sheet not found ID={record.SheetId}");
                        throw new Exception("Sheet not found!");
                    }

                    if (sheet.PurchaseHistory == null)
                        sheet.PurchaseHistory = new List<SheetPurchase>();

                    record.Id = sheet.PurchaseHistory.Count > 0
                        ? sheet.PurchaseHistory.Max(p => p.Id) + 1
                        : 1;
                    record.CreatedAt = DateTime.Now;

                    sheet.PurchaseHistory.Add(record);

                    sheet.TotalStock += record.Quantity;
                    sheet.LatestPurchaseDate = record.PurchasedOn;
                    sheet.PurchasePrice = record.UnitPrice;

                    if (!string.IsNullOrEmpty(record.Supplier))
                    {
                        sheet.Supplier = record.Supplier;
                        sheet.SupplierName = record.Supplier;
                    }

                    System.Diagnostics.Debug.WriteLine(
                        $"AddPurchaseRecord: Added {record.Quantity} sheets to ID={sheet.Id}, New Stock={sheet.TotalStock}");

                    SaveToFile();
                    RefreshCache();
                    NotifyDataChanged();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adding purchase record: {ex.Message}");
                    throw;
                }
            }
        }

        public List<SheetPurchase> GetPurchaseHistory(int sheetId)
        {
            var sheet = _sheets.FirstOrDefault(s => s.Id == sheetId);
            return sheet?.PurchaseHistory ?? new List<SheetPurchase>();
        }

        // ═══════════════════════════════════════════════════════
        // USE / DEDUCT SHEETS
        // ═══════════════════════════════════════════════════════
        public void AddUsageRecord(SheetUsage record)
        {
            lock (_lock)
            {
                try
                {
                    var sheet = _sheets.FirstOrDefault(s => s.Id == record.SheetId);
                    if (sheet == null)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"AddUsageRecord: Sheet not found ID={record.SheetId}");
                        throw new Exception("Sheet not found!");
                    }

                    int availableBalance = sheet.TotalStock - sheet.UsedSheets;
                    if (record.Quantity > availableBalance)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"AddUsageRecord: Insufficient stock. Requested={record.Quantity}, Available={availableBalance}");
                        throw new Exception($"Only {availableBalance} sheets available!");
                    }

                    if (sheet.UseHistory == null)
                        sheet.UseHistory = new List<SheetUsage>();

                    record.Id = sheet.UseHistory.Count > 0
                        ? sheet.UseHistory.Max(u => u.Id) + 1
                        : 1;
                    record.CreatedAt = DateTime.Now;

                    sheet.UseHistory.Add(record);

                    sheet.UsedSheets += record.Quantity;
                    sheet.BalanceSheets = sheet.TotalStock - sheet.UsedSheets;

                    System.Diagnostics.Debug.WriteLine(
                        $"AddUsageRecord: Used {record.Quantity} sheets from ID={sheet.Id}, New Balance={sheet.BalanceSheets}");

                    SaveToFile();
                    RefreshCache();
                    NotifyDataChanged();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adding usage record: {ex.Message}");
                    throw;
                }
            }
        }

        public List<SheetUsage> GetUsageHistory(int sheetId)
        {
            var sheet = _sheets.FirstOrDefault(s => s.Id == sheetId);
            return sheet?.UseHistory ?? new List<SheetUsage>();
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
                    System.Diagnostics.Debug.WriteLine(
                        $"File not found: {_filePath}, creating new");
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

                        // Load Purchase History
                        sheet.PurchaseHistory = new List<SheetPurchase>();
                        foreach (var purchEl in el.Elements("Purchase"))
                        {
                            sheet.PurchaseHistory.Add(new SheetPurchase
                            {
                                Id = int.TryParse(purchEl.Attribute("Id")?.Value, out int pid) ? pid : 0,
                                SheetId = sheet.Id,
                                Quantity = int.TryParse(purchEl.Attribute("Quantity")?.Value, out int pq) ? pq : 0,
                                UnitPrice = decimal.TryParse(purchEl.Attribute("UnitPrice")?.Value, out decimal upp) ? upp : 0,
                                Supplier = purchEl.Attribute("Supplier")?.Value ?? "",
                                PurchasedOn = DateTime.TryParse(purchEl.Attribute("PurchasedOn")?.Value, out DateTime ppo) ? ppo : DateTime.Now,
                                Notes = purchEl.Attribute("Notes")?.Value ?? "",
                                CreatedAt = DateTime.TryParse(purchEl.Attribute("CreatedAt")?.Value, out DateTime pca) ? pca : DateTime.Now
                            });
                        }

                        // Load Usage History — supports both "Usage" (new) and legacy "Use" tags
                        sheet.UseHistory = new List<SheetUsage>();

                        var usageElements = el.Elements("Usage").Concat(el.Elements("Use"));
                        foreach (var useEl in usageElements)
                        {
                            // Support both "Id" and lowercase legacy "id"
                            var idAttr = useEl.Attribute("Id") ?? useEl.Attribute("id");

                            sheet.UseHistory.Add(new SheetUsage
                            {
                                Id = int.TryParse(idAttr?.Value, out int uid) ? uid : 0,
                                SheetId = sheet.Id,
                                Quantity = int.TryParse(useEl.Attribute("Quantity")?.Value, out int uq) ? uq : 0,
                                Reason = useEl.Attribute("Reason")?.Value ?? "",
                                UsedOn = DateTime.TryParse(useEl.Attribute("UsedOn")?.Value, out DateTime uuo) ? uuo : DateTime.Now,
                                CreatedAt = DateTime.TryParse(useEl.Attribute("CreatedAt")?.Value, out DateTime uca) ? uca : DateTime.Now
                            });
                        }

                        // Recalculate BalanceSheets from TotalStock - UsedSheets
                        sheet.BalanceSheets = sheet.TotalStock - sheet.UsedSheets;

                        sheets.Add(sheet);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error parsing element: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine(
                    $"LoadFromFile: Loaded {sheets.Count} sheets total " +
                    $"({sheets.Count(s => s.IsActive)} active, {sheets.Count(s => !s.IsActive)} deleted)");
                return sheets;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading file: {ex.Message}");
                return new List<Sheet>();
            }
        }

        // ═══════════════════════════════════════════════════════
        // SAVE TO FILE — FIXED
        //   ✅ Fix A: Writes IsActive attribute
        //   ✅ Fix B: Writes <Usage> with "Id" (not <Use> with "id")
        //   ✅ Atomic file write with explicit flush
        // ═══════════════════════════════════════════════════════
        private void SaveToFile()
        {
            try
            {
                var doc = new XDocument(new XElement("Sheets"));

                foreach (var sheet in _sheets)
                {
                    var sheet1 = new XElement("Sheet",
                        new XAttribute("Id", sheet.Id),
                        new XAttribute("SrNo", sheet.SrNo),
                        new XAttribute("Category", sheet.Category ?? ""),
                        new XAttribute("Thickness", sheet.Thickness ?? ""),
                        new XAttribute("Color", sheet.Color ?? ""),
                        new XAttribute("ColorHex", sheet.ColorHex ?? "#E8F4F8"),
                        new XAttribute("Width", sheet.Width),
                        new XAttribute("Height", sheet.Height),
                        new XAttribute("SquareMeter", sheet.SquareMeter.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                        new XAttribute("PurchasePrice", sheet.PurchasePrice.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                        new XAttribute("SellPrice", sheet.SellPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                        new XAttribute("TotalStock", sheet.TotalStock),
                        new XAttribute("UsedSheets", sheet.UsedSheets),
                        new XAttribute("BalanceSheets", sheet.BalanceSheets),
                        new XAttribute("IsActive", sheet.IsActive),                  // ✅ FIX A
                        new XAttribute("Supplier", sheet.Supplier ?? ""),
                        new XAttribute("SupplierName", sheet.SupplierName ?? ""),
                        new XAttribute("Description", sheet.Description ?? ""),
                        new XAttribute("CreatedDate", sheet.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")),
                        new XAttribute("LatestPurchaseDate",
                            sheet.LatestPurchaseDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "")
                    );

                    // Save Purchase History
                    if (sheet.PurchaseHistory != null && sheet.PurchaseHistory.Count > 0)
                    {
                        foreach (var purch in sheet.PurchaseHistory)
                        {
                            sheet1.Add(new XElement("Purchase",
                                new XAttribute("Id", purch.Id),
                                new XAttribute("Quantity", purch.Quantity),
                                new XAttribute("UnitPrice", purch.UnitPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                                new XAttribute("Supplier", purch.Supplier ?? ""),
                                new XAttribute("PurchasedOn", purch.PurchasedOn.ToString("yyyy-MM-dd HH:mm:ss")),
                                new XAttribute("Notes", purch.Notes ?? ""),
                                new XAttribute("CreatedAt", purch.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"))
                            ));
                        }
                    }

                    // Save Usage History  — ✅ FIX B: write as <Usage> with "Id" (matches LoadFromFile)
                    if (sheet.UseHistory != null && sheet.UseHistory.Count > 0)
                    {
                        foreach (var use in sheet.UseHistory)
                        {
                            sheet1.Add(new XElement("Usage",
                                new XAttribute("Id", use.Id),
                                new XAttribute("Quantity", use.Quantity),
                                new XAttribute("Reason", use.Reason ?? ""),
                                new XAttribute("UsedOn", use.UsedOn.ToString("yyyy-MM-dd HH:mm:ss")),
                                new XAttribute("CreatedAt", use.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"))
                            ));
                        }
                    }

                    doc.Root.Add(sheet1);
                }

                // ✅ Atomic write: write to temp file then move into place
                var tempPath = _filePath + ".tmp";

                using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    doc.Save(fs);
                    fs.Flush(true); // force OS to flush to disk before close
                }

                if (File.Exists(_filePath))
                    File.Delete(_filePath);

                File.Move(tempPath, _filePath);

                System.Diagnostics.Debug.WriteLine(
                    $"SaveToFile: Saved {_sheets.Count} sheets to {_filePath}");
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

        // ═══════════════════════════════════════════════════════
        // STATISTICS
        // ═══════════════════════════════════════════════════════
        public int GetTotalSheets() => _cachedSheets?.Count ?? 0;

        public int GetTotalStock() => _cachedSheets?.Sum(s => s.TotalStock) ?? 0;

        public int GetTotalBalance() => _cachedSheets?.Sum(s => s.BalanceSheets) ?? 0;

        public decimal GetTotalValue() => _cachedSheets?.Sum(s => s.BalanceSheets * s.SellPrice) ?? 0;

        public List<Sheet> GetLowStockSheets(int threshold = 5)
        {
            return _cachedSheets?.Where(s => s.BalanceSheets <= threshold && s.BalanceSheets > 0).ToList()
                   ?? new List<Sheet>();
        }

        public List<Sheet> GetOutOfStockSheets()
        {
            return _cachedSheets?.Where(s => s.BalanceSheets == 0).ToList()
                   ?? new List<Sheet>();
        }
    }
}