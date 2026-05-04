using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace ProGlassAutomation.Models
{
    public class Sheet
    {
        // ═══════════════════════════════════════════════════════
        // BASIC PROPERTIES
        // ═══════════════════════════════════════════════════════
        public int Id { get; set; }
        public int SrNo { get; set; }
        public string Category { get; set; }
        public string Thickness { get; set; }
        public string Color { get; set; }
        public string ColorHex { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double SquareMeter { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SellPrice { get; set; }
        public int TotalStock { get; set; }
        public int UsedSheets { get; set; }
        public int BalanceSheets { get; set; }
        public bool IsActive { get; set; }
        public string Supplier { get; set; }
        public string SupplierName { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LatestPurchaseDate { get; set; }

        // ═══════════════════════════════════════════════════════
        // PURCHASE & USE HISTORY
        // ═══════════════════════════════════════════════════════
        public List<SheetPurchase> PurchaseHistory { get; set; } = new();
        public List<SheetUsage> UseHistory { get; set; } = new();

        // ═══════════════════════════════════════════════════════
        // COMPUTED PROPERTIES
        // ═══════════════════════════════════════════════════════
        public string DisplayDateTime => CreatedDate.ToString("dd/MM/yyyy HH:mm");

        public decimal TotalInvested => PurchaseHistory?.Sum(p => p.TotalAmt) ?? 0;

        public decimal AveragePurchasePrice => PurchaseHistory?.Count > 0
            ? PurchaseHistory.Average(p => p.UnitPrice)
            : PurchasePrice;

        public decimal TotalSaleValue => TotalStock * SellPrice;

        // ═══════════════════════════════════════════════════════
        // COLOR BRUSH
        // ═══════════════════════════════════════════════════════
        public SolidColorBrush ColorBrush
        {
            get
            {
                if (!string.IsNullOrEmpty(ColorHex))
                {
                    try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(ColorHex)); }
                    catch { }
                }
                return new SolidColorBrush(System.Windows.Media.Colors.LightGray);
            }
        }

        // ═══════════════════════════════════════════════════════
        // CATEGORIES (200+)
        // ═══════════════════════════════════════════════════════
        public static List<string> Categories = new List<string>
        {
            // ═══════════════════════════════════════════════════════
            // CLEAR SERIES
            // ═══════════════════════════════════════════════════════
            "Clear Float", "Ultra Clear", "Crystal Clear", "Optifloat Clear",
            "Starphire Ultra Clear", "Ultra White",

            // ═══════════════════════════════════════════════════════
            // BLUE SERIES
            // ═══════════════════════════════════════════════════════
            "Blue Float", "Azure Blue", "Ocean Blue", "Sky Blue", "Royal Blue",
            "Navy Blue", "Cobalt Blue", "Steel Blue", "Denim Blue",
            "HD Blue", "PNA Blue", "Ramly Blue",
            "Sunlux Blue", "Reflite Blue",
            "Stopsol Classic Blue", "Stopsol Superburn Blue",
            "Stopray Classic Blue",
            "Chromafloat Blue",
            "Sunergy Blue", "Sunergy Plus",
            "Tinted Blue",
            "SunGuard Blue",
            "AGC Tinted Blue",
            "Şişecam Blue",
            "SGG Reflective Blue", "SGG Tinted Blue",
            "Pilkington Arctic Blue",
            "PGI Reflective Blue",
            "Taiwan Reflective Blue", "Taiwan Tinted Blue",
            "Antelio Blue",
            "Spectran Blue",
            "Lacobel Blue",
            "Reflective Blue", "Mirror Blue",

            // ═══════════════════════════════════════════════════════
            // GREEN SERIES
            // ═══════════════════════════════════════════════════════
            "Green Float", "Forest Green", "Emerald Green", "Jade Green",
            "Mint Green", "Sage Green", "Olive Green", "Teal Green",
            "HD Green", "PNA Green", "Ramly Green",
            "Sunlux Green", "Reflite Green",
            "Belgium Green",
            "Planibel Green",
            "Stopsol Classic Green", "Stopsol Superburn Green",
            "Stopray Classic Green",
            "Chromafloat Green",
            "Sunergy Green",
            "Tinted Green",
            "SunGuard Green",
            "AGC Tinted Green",
            "Şişecam Stopray Green", "Şişecam Tinted Green",
            "SGG Tinted Green",
            "PGI Tinted Green",
            "Taiwan Tinted Green",
            "Antelio Green",
            "Lacobel Green",
            "Reflective Green", "Mirror Green",

            // ═══════════════════════════════════════════════════════
            // BRONZE SERIES
            // ═══════════════════════════════════════════════════════
            "Bronze Float", "Dark Bronze", "Light Bronze", "Medium Bronze",
            "HD Bronze", "PNA Bronze", "Ramly Bronze",
            "Sunlux Bronze", "Reflite Bronze",
            "Belgium Bronze",
            "Planibel Bronze",
            "Stopsol Classic Bronze", "Stopsol Superburn Bronze",
            "Stopray Classic Bronze",
            "Tinted Bronze",
            "SunGuard Bronze",
            "AGC Tinted Bronze",
            "Şişecam Stopray Bronze", "Şişecam Tinted Bronze",
            "Şişecam Reflective Bronze",
            "SGG Tinted Bronze",
            "Miralite Bronze",
            "Lacobel Brown",

            // ═══════════════════════════════════════════════════════
            // GREY SERIES
            // ═══════════════════════════════════════════════════════
            "Grey Float", "Dark Grey", "Light Grey", "Medium Grey",
            "Charcoal Grey", "Smoke Grey", "Ash Grey",
            "HD Grey", "PNA Grey", "Ramly Grey",
            "Sunlux Grey", "Reflite Grey",
            "Belgium Grey",
            "Planibel Grey",
            "Stopsol Classic Grey", "Stopsol Superburn Grey",
            "Stopray Classic Grey",
            "Tinted Grey",
            "SunGuard Grey",
            "AGC Tinted Grey",
            "Şişecam Stopray Grey", "Şişecam Tinted Grey",
            "SGG Tinted Grey",
            "Lacobel Grey",

            // ═══════════════════════════════════════════════════════
            // SILVER / REFLECTIVE SERIES
            // ═══════════════════════════════════════════════════════
            "Reflective Silver", "Mirror Silver", "Silver Mirror",
            "PNA Reflective Silver",
            "Sunlux Silver", "Reflite Silver",
            "Stopsol Silver Lite", "Stopsol Silver Dark",
            "Stopray Silver",
            "Chromafloat Silver",
            "Sunergy Silver",
            "Guardian Reflective Silver",
            "Şişecam Reflective Silver",
            "SGG Reflective Silver",
            "PGI Reflective Silver",
            "Taiwan Reflective Silver",
            "Antelio Silver",
            "Miralite Silver",
            "Spectran Silver",

            // ═══════════════════════════════════════════════════════
            // GOLD / BRONZE REFLECTIVE SERIES
            // ═══════════════════════════════════════════════════════
            "Reflective Gold", "Mirror Gold", "Gold Mirror",
            "PNA Reflective Gold",
            "Sunlux Gold", "Reflite Gold",
            "Stopray Gold",
            "Chromafloat Gold",
            "Guardian Reflective Gold",
            "Şişecam Reflective Gold",
            "SGG Reflective Gold",
            "Antelio Gold",
            "Miralite Gold",

            // ═══════════════════════════════════════════════════════
            // BLACK / DARK SERIES
            // ═══════════════════════════════════════════════════════
            "Black Float", "Dark Black", "Jet Black",
            "HD Black",
            "Tinted Black",
            "Lacobel Black", "Lacobel Extra Black",

            // ═══════════════════════════════════════════════════════
            // PLANIBEL / BELGIUM SERIES
            // ═══════════════════════════════════════════════════════
            "Belgium Clear", "Belgium Bronze", "Belgium Grey", "Belgium Green",
            "Planibel Clear", "Planibel A", "Planibel Top N+",
            "Planibel Grey", "Planibel Bronze", "Planibel Green",

            // ═══════════════════════════════════════════════════════
            // STOPSOL SERIES (AGC)
            // ═══════════════════════════════════════════════════════
            "Stopsol Classic Clear", "Stopsol Classic Bronze", "Stopsol Classic Grey",
            "Stopsol Classic Green", "Stopsol Classic Blue",
            "Stopsol Superburn Clear", "Stopsol Superburn Bronze", "Stopsol Superburn Grey",
            "Stopsol Superburn Green", "Stopsol Superburn Blue",
            "Stopsol Silver Lite", "Stopsol Silver Dark",

            // ═══════════════════════════════════════════════════════
            // STOPRAY SERIES (AGC)
            // ═══════════════════════════════════════════════════════
            "Stopray Classic Clear", "Stopray Classic Bronze", "Stopray Classic Grey",
            "Stopray Classic Green", "Stopray Classic Blue",
            "Stopray Silver", "Stopray Gold", "Stopray Vision",

            // ═══════════════════════════════════════════════════════
            // CHROMAFLOAT SERIES (AGC)
            // ═══════════════════════════════════════════════════════
            "Chromafloat Silver", "Chromafloat Gold", "Chromafloat Blue", "Chromafloat Green",

            // ═══════════════════════════════════════════════════════
            // SUNERGY SERIES (AGC)
            // ═══════════════════════════════════════════════════════
            "Sunergy Clear", "Sunergy Bronze", "Sunergy Grey", "Sunergy Green", "Sunergy Plus",

            // ═══════════════════════════════════════════════════════
            // GUARDIAN / SUNGARD SERIES
            // ═══════════════════════════════════════════════════════
            "Guardian Clear", "Guardian Ultra Clear",
            "SunGuard Clear", "SunGuard Blue", "SunGuard Green", "SunGuard Bronze", "SunGuard Grey",
            "SunGuard Neutral 63", "SunGuard Neutral 70",
            "Solarban 60", "Solarban 70", "Solarban 70XL", "Solarban 90",
            "Guardian Reflective Silver", "Guardian Reflective Gold",

            // ═══════════════════════════════════════════════════════
            // AGC SERIES
            // ═══════════════════════════════════════════════════════
            "AGC Clear", "AGC Ultra Clear", "AGC Low Iron",
            "AGC Tinted Bronze", "AGC Tinted Grey", "AGC Tinted Green", "AGC Tinted Blue",

            // ═══════════════════════════════════════════════════════
            // ŞIŞECAM / TRAKYA SERIES (Turkey)
            // ═══════════════════════════════════════════════════════
            "Şişecam Clear", "Şişecam Ultra Clear",
            "Şişecam Stopray Bronze", "Şişecam Stopray Grey", "Şişecam Stopray Green",
            "Şişecam Tinted Bronze", "Şişecam Tinted Grey", "Şişecam Tinted Green",
            "Şişecam Reflective Silver", "Şişecam Reflective Gold",
            "Şişecam Blue",
            "Trakya Clear", "Trakya Tinted", "Trakya Stopray",

            // ═══════════════════════════════════════════════════════
            // SGG SERIES (Saint Gobain)
            // ═══════════════════════════════════════════════════════
            "SGG Clear", "SGG Ultra Clear",
            "SGG Planitherm One", "SGG Planitherm Total", "SGG Planitherm Ultra N",
            "SGG Reflective Silver", "SGG Reflective Gold", "SGG Reflective Blue",
            "SGG Tinted Bronze", "SGG Tinted Grey", "SGG Tinted Green",
            "SGG Climalit", "SGG Antelio",

            // ═══════════════════════════════════════════════════════
            // PILKINGTON SERIES
            // ═══════════════════════════════════════════════════════
            "Pilkington Optifloat Clear", "Pilkington Optifloat Tinted",
            "Pilkington K Glass", "Pilkington Low-E",
            "Pilkington Sunshade", "Pilkington Arctic Blue",

            // ═══════════════════════════════════════════════════════
            // PGI SERIES (South Africa)
            // ═══════════════════════════════════════════════════════
            "PGI Clear", "PGI Tinted Bronze", "PGI Tinted Grey", "PGI Tinted Green",
            "PGI Reflective Silver", "PGI Reflective Blue",

            // ═══════════════════════════════════════════════════════
            // TAIWAN GLASS SERIES
            // ═══════════════════════════════════════════════════════
            "Taiwan Clear", "Taiwan Tinted Bronze", "Taiwan Tinted Grey", "Taiwan Tinted Green",
            "Taiwan Tinted Blue", "Taiwan Reflective Silver", "Taiwan Reflective Blue",

            // ═══════════════════════════════════════════════════════
            // XINYI SERIES (China)
            // ═══════════════════════════════════════════════════════
            "Xinyi Clear", "Xinyi Tinted", "Xinyi Low-E",

            // ═══════════════════════════════════════════════════════
            // LOW-E SERIES
            // ═══════════════════════════════════════════════════════
            "Low-E Clear", "Low-E Neutral", "Low-E Silver",
            "iPlus 1.0", "iPlus 1.1", "iPlus 1.2",
            "Comfort Plus", "Energy Advantage",

            // ═══════════════════════════════════════════════════════
            // ANTELIO / MIRALITE / SPECTRAN SERIES
            // ═══════════════════════════════════════════════════════
            "Antelio Silver", "Antelio Gold", "Antelio Blue", "Antelio Green",
            "Miralite Silver", "Miralite Gold", "Miralite Bronze",
            "Spectran Silver", "Spectran Blue",

            // ═══════════════════════════════════════════════════════
            // SUNFILM / COMFILM SERIES
            // ═══════════════════════════════════════════════════════
            "Sunfilm Clear", "Sunfilm Ceramic", "Sunfilm Privacy",
            "Comfilm Safety", "Comfilm UV",

            // ═══════════════════════════════════════════════════════
            // PRIVACY / DECORATIVE SERIES
            // ═══════════════════════════════════════════════════════
            "Matelux Clear", "Matelux Bronze", "Matelux Grey", "Matelux Green",
            "Decormatt", "Mastercote", "Satinato", "Masterglass",

            // ═══════════════════════════════════════════════════════
            // LACOBEL SERIES (Painted)
            // ═══════════════════════════════════════════════════════
            "Lacobel White", "Lacobel Black", "Lacobel Grey", "Lacobel Red",
            "Lacobel Blue", "Lacobel Brown", "Lacobel Green",
            "Lacobel Extra White", "Lacobel Extra Black", "Lacobel Easy Clean",

            // ═══════════════════════════════════════════════════════
            // PYROGLASS SERIES (Fire Rated)
            // ═══════════════════════════════════════════════════════
            "Pyrobel Clear", "Pyrobel Bronze",
            "Pyrostop 30", "Pyrostop 60", "Pyrostop 90",
            "Pyrodur", "Pyroguard",

            // ═══════════════════════════════════════════════════════
            // CUSTOM / OTHER
            // ═══════════════════════════════════════════════════════
            "Custom", "Other"
        };

        // ═══════════════════════════════════════════════════════
        // THICKNESS
        // ═══════════════════════════════════════════════════════
        public static string[] Thicknesses { get; } = new string[]
        {
            "2mm", "2.5mm", "3mm", "4mm", "5mm", "6mm", "8mm", "10mm", "12mm", "15mm", "19mm"
        };

        // ═══════════════════════════════════════════════════════
        // COLORS
        // ═══════════════════════════════════════════════════════
        public static List<GlassColorItem> ColorItems = new List<GlassColorItem>
        {
            new GlassColorItem { Name = "Clear", Hex = "#E8F4F8" },
            new GlassColorItem { Name = "Ultra Clear", Hex = "#F0F8FF" },
            new GlassColorItem { Name = "Bronze", Hex = "#8B4513" },
            new GlassColorItem { Name = "Grey", Hex = "#696969" },
            new GlassColorItem { Name = "Green", Hex = "#228B22" },
            new GlassColorItem { Name = "Blue", Hex = "#4169E1" },
            new GlassColorItem { Name = "Black", Hex = "#1C1C1C" },
            new GlassColorItem { Name = "Silver", Hex = "#C0C0C0" },
            new GlassColorItem { Name = "Gold", Hex = "#FFD700" },
            new GlassColorItem { Name = "Pink", Hex = "#FFB6C1" },
            new GlassColorItem { Name = "Ocean Blue", Hex = "#008B8B" },
            new GlassColorItem { Name = "Dark Grey", Hex = "#36454F" },
            new GlassColorItem { Name = "Amber", Hex = "#FFBF00" },
            new GlassColorItem { Name = "White", Hex = "#FFFFFF" },
            new GlassColorItem { Name = "Red", Hex = "#DC2626" },
            new GlassColorItem { Name = "Brown", Hex = "#A52A2A" },
            new GlassColorItem { Name = "Neutral", Hex = "#D3D3D3" },
            new GlassColorItem { Name = "Arctic Blue", Hex = "#ADD8E6" }
        };

        // ═══════════════════════════════════════════════════════
        // STANDARD SIZES
        // ═══════════════════════════════════════════════════════
        public static List<(int Width, int Height, string Desc)> StandardSizes = new List<(int, int, string)>
        {
            // 8FT
            (2440, 1830, "8'x6'"), (2440, 2134, "8'x7'"), (2440, 2250, "8'x7.4'"), (2440, 2440, "8'x8'"),
            // 2500
            (2500, 1830, ""), (2500, 1875, ""), (2500, 2000, ""), (2500, 2134, ""), (2500, 2250, ""), (2500, 2440, ""), (2500, 2500, ""),
            // 2700
            (2700, 1830, ""), (2700, 2000, ""), (2700, 2134, ""), (2700, 2250, ""), (2700, 2440, ""), (2700, 2500, ""),
            // 3000
            (3000, 1830, ""), (3000, 2000, ""), (3000, 2134, ""), (3000, 2250, ""), (3000, 2440, ""), (3000, 2500, ""),
            // 10FT
            (3050, 1830, "10'x6'"), (3050, 2134, "10'x7'"), (3050, 2250, "10'x7.4'"), (3050, 2440, "10'x8'"),
            // 10.5FT
            (3210, 1830, "10.5'x6'"), (3210, 2134, "10.5'x7'"), (3210, 2250, "10.5'x7.4'"), (3210, 2440, "10.5'x8'"),
            // 3300
            (3300, 1830, ""), (3300, 2000, ""), (3300, 2134, ""), (3300, 2250, ""), (3300, 2440, ""), (3300, 2500, ""),
            // 11FT
            (3350, 1830, "11'x6'"), (3350, 2134, "11'x7'"), (3350, 2250, "11'x7.4'"), (3350, 2440, "11'x8'"), (3350, 2500, ""),
            // 3500
            (3500, 1830, ""), (3500, 2000, ""), (3500, 2134, ""), (3500, 2250, ""), (3500, 2440, ""), (3500, 2500, ""),
            // 3600
            (3600, 1830, ""), (3600, 2000, ""), (3600, 2134, ""), (3600, 2250, ""), (3600, 2440, ""), (3600, 2500, ""),
            // 12FT
            (3660, 1830, "12'x6'"), (3660, 2134, "12'x7'"), (3660, 2250, "12'x7.4'"), (3660, 2440, "12'x8'"), (3660, 2500, ""),
            // 4000
            (4000, 1830, ""), (4000, 2134, ""), (4000, 2250, ""), (4000, 2440, ""), (4000, 2500, ""),
            // 4200
            (4200, 1830, ""), (4200, 2134, ""), (4200, 2250, ""), (4200, 2440, ""), (4200, 2500, ""),
            // 14FT
            (4270, 1830, "14'x6'"), (4270, 2134, "14'x7'"), (4270, 2250, "14'x7.4'"), (4270, 2440, "14'x8'"), (4270, 2500, ""),
            // 4500
            (4500, 1830, ""), (4500, 2134, ""), (4500, 2250, ""), (4500, 2440, ""), (4500, 2500, ""),
            // 16FT
            (4800, 1830, "16'x6'"), (4800, 2134, "16'x7'"), (4800, 2250, "16'x7.4'"), (4800, 2440, "16'x8'"), (4800, 2500, ""),
            (4880, 1830, "16'x6'"), (4880, 2134, "16'x7'"), (4880, 2250, "16'x7.4'"), (4880, 2440, "16'x8'"), (4880, 2500, ""),
            // 5000
            (5000, 1830, ""), (5000, 2134, ""), (5000, 2250, ""), (5000, 2440, ""), (5000, 2500, ""),
            // 5400
            (5400, 1830, ""), (5400, 2134, ""), (5400, 2250, ""), (5400, 2440, ""), (5400, 2500, ""),
            // 18FT
            (5490, 1830, "18'x6'"), (5490, 2134, "18'x7'"), (5490, 2250, "18'x7.4'"), (5490, 2440, "18'x8'"), (5490, 2500, ""),
            // 6000
            (6000, 1830, ""), (6000, 2134, ""), (6000, 2250, ""), (6000, 2440, ""), (6000, 2500, ""),
            // 20FT
            (6100, 1830, "20'x6'"), (6100, 2134, "20'x7'"), (6100, 2250, "20'x7.4'"), (6100, 2440, "20'x8'"), (6100, 2500, "")
        };
    }

    // ═══════════════════════════════════════════════════════
    // SHEET PURCHASE RECORD
    // ═══════════════════════════════════════════════════════
    public class SheetPurchase
    {
        public int Id { get; set; }
        public int SheetId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Supplier { get; set; }
        public DateTime PurchasedOn { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        public decimal TotalAmt => Quantity * UnitPrice;
        public string DisplayDate => PurchasedOn.ToString("dd/MM/yyyy HH:mm");
    }

    // ═══════════════════════════════════════════════════════
    // SHEET USAGE RECORD
    // ═══════════════════════════════════════════════════════
    public class SheetUsage
    {
        public int Id { get; set; }
        public int SheetId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; }
        public DateTime UsedOn { get; set; }
        public DateTime CreatedAt { get; set; }

        public string DisplayDate => UsedOn.ToString("dd/MM/yyyy HH:mm");
    }

    // ═══════════════════════════════════════════════════════
    // GLASS COLOR ITEM
    // ═══════════════════════════════════════════════════════
    public class GlassColorItem
    {
        public string Name { get; set; }
        public string Hex { get; set; }
    }
}