using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace ProGlassAutomation.Models
{
    public class Sheet
    {
        // ═══════════════════════════════════════════════════════════════
        // INSTANCE PROPERTIES
        // ═══════════════════════════════════════════════════════════════
        public int Id { get; set; }
        public int SrNo { get; set; }

        public string Category { get; set; } = "";
        public string Thickness { get; set; } = "";
        public string Color { get; set; } = "";
        public string ColorHex { get; set; } = "";

        public int Width { get; set; }
        public int Height { get; set; }

        // One sheet area
        public double SquareMeter { get; set; }

        public decimal PurchasePrice { get; set; }
        public decimal SellPrice { get; set; }

        public int TotalStock { get; set; }
        public int UsedSheets { get; set; }
        public int BalanceSheets { get; set; }

        public bool IsActive { get; set; } = true;

        public string Supplier { get; set; } = "";
        public string SupplierName { get; set; } = "";
        public string Description { get; set; } = "";

        public DateTime CreatedDate { get; set; }
        public DateTime? LatestPurchaseDate { get; set; }

        public List<SheetPurchase> PurchaseHistory { get; set; } = new();
        public List<SheetUsage> UseHistory { get; set; } = new();

        // ═══════════════════════════════════════════════════════════════
        // DISPLAY / COMPUTED PROPERTIES
        // ═══════════════════════════════════════════════════════════════
        public string DisplayDateTime => CreatedDate.ToString("dd/MM/yyyy HH:mm");

        public decimal TotalInvested => PurchaseHistory?.Sum(p => p.TotalAmt) ?? 0;

        public decimal AveragePurchasePrice => PurchaseHistory?.Count > 0
            ? PurchaseHistory.Average(p => p.UnitPrice)
            : PurchasePrice;

        // Full stock values
        public decimal TotalStockPurchaseValue => TotalStock * PurchasePrice;
        public decimal TotalStockSellValue => TotalStock * SellPrice;

        // Balance stock values - used in Sheet Store grid
        public decimal TotalPurchaseValue => BalanceSheets * PurchasePrice;
        public decimal TotalSellValue => BalanceSheets * SellPrice;
        public decimal TotalProfitValue => TotalSellValue - TotalPurchaseValue;

        // Compatibility property
        public decimal TotalSaleValue => TotalStock * SellPrice;

        // SQM totals
        public double TotalStockSQM => SquareMeter * TotalStock;
        public double UsedSQM => SquareMeter * UsedSheets;
        public double BalanceSQM => SquareMeter * BalanceSheets;

        public SolidColorBrush ColorBrush
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(ColorHex))
                {
                    try
                    {
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(ColorHex));
                    }
                    catch
                    {
                        // fallback below
                    }
                }

                var colorItem = ColorItems.FirstOrDefault(c =>
                    string.Equals(c.Name, Color, StringComparison.OrdinalIgnoreCase));

                if (colorItem != null && !string.IsNullOrWhiteSpace(colorItem.Hex))
                {
                    try
                    {
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorItem.Hex));
                    }
                    catch
                    {
                        // fallback below
                    }
                }

                return new SolidColorBrush(Colors.LightGray);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // GLASS CATEGORIES / SERIES
        // Current practical stock list with Guardian, AGC, SGG, Planitherm,
        // Planibel, CAVS, tinted, reflective, Low-E and common custom series.
        // ═══════════════════════════════════════════════════════════════
        public static List<string> Categories { get; } = new()
        {
            // General / common stock
            "Clear Float",
            "Clear Glass",
            "Extra Clear",
            "Ultra Clear",
            "Low Iron",
            "Crystal Clear",
            "Optiwhite",
            "Starphire",
            "Tinted Glass",
            "Reflective Glass",
            "Mirror Glass",
            "Low-E Glass",
            "Solar Control Glass",
            "Tempered Glass",
            "Laminated Glass",
            "IGU / DGU",
            "Frosted Glass",
            "Acid Etched Glass",
            "Lacobel Glass",
            "Pattern Glass",
            "Obscure Glass",
            "Spandrel Glass",
            "Back Painted Glass",

            // Tinted series
            "Tinted Blue",
            "Tinted Green",
            "Tinted Bronze",
            "Tinted Grey",
            "Tinted Dark Grey",
            "Tinted Black",
            "Blue Float",
            "Green Float",
            "Bronze Float",
            "Grey Float",
            "Dark Grey Float",
            "Ocean Blue",
            "Sky Blue",
            "Arctic Blue",
            "Forest Green",
            "Emerald Green",
            "Dark Green",
            "Light Bronze",
            "Dark Bronze",
            "Charcoal Grey",
            "Smoke Grey",

            // Reflective / mirror series
            "Reflective Silver",
            "Reflective Gold",
            "Reflective Blue",
            "Reflective Green",
            "Reflective Bronze",
            "Reflective Grey",
            "Reflective Dark Grey",
            "Mirror Silver",
            "Mirror Gold",
            "Mirror Bronze",
            "Mirror Grey",
            "One Way Mirror",

            // Guardian series
            "Guardian Clear",
            "Guardian UltraClear",
            "Guardian ExtraClear",
            "Guardian CrystalClear",
            "Guardian Float",
            "Guardian Tinted Blue",
            "Guardian Tinted Green",
            "Guardian Tinted Bronze",
            "Guardian Tinted Grey",
            "Guardian Reflective",
            "Guardian Low-E",
            "Guardian ClimaGuard",
            "Guardian ClimaGuard Premium",
            "Guardian ClimaGuard 70",
            "Guardian ClimaGuard 80",
            "Guardian SunGuard",
            "Guardian SunGuard SN",
            "Guardian SunGuard SN 51",
            "Guardian SunGuard SN 63",
            "Guardian SunGuard SN 70",
            "Guardian SunGuard SNX",
            "Guardian SunGuard SNX 50",
            "Guardian SunGuard SNX 60",
            "Guardian SunGuard SNX 70",
            "Guardian SunGuard HP",
            "Guardian SunGuard HP Neutral",
            "Guardian SunGuard Solar",
            "Guardian SunGuard Silver",
            "Guardian SunGuard Grey",
            "Guardian SunGuard Blue",
            "Guardian SunGuard Bronze",

            // AGC / Planibel / Stopray / Sunergy / Lacobel / Matelux
            "AGC Clear Float",
            "AGC Planibel Clear",
            "AGC Planibel",
            "Planibel Clear",
            "Planibel Bronze",
            "Planibel Grey",
            "Planibel Green",
            "Planibel Blue",
            "Planibel Top N",
            "Planibel Top N+",
            "Planibel A",
            "Planibel G",
            "AGC Stopray",
            "AGC Stopray Vision",
            "AGC Stopray Vision 50",
            "AGC Stopray Vision 60",
            "AGC Stopray Vision 70",
            "AGC Stopray Smart",
            "AGC Stopray Silver",
            "AGC Stopray Neutral",
            "AGC Stopray Titanium",
            "AGC Sunergy",
            "AGC Sunergy Clear",
            "AGC Sunergy Blue",
            "AGC Sunergy Green",
            "AGC Sunergy Bronze",
            "AGC Sunergy Grey",
            "AGC Lacobel",
            "Lacobel White",
            "Lacobel Black",
            "Lacobel Grey",
            "Lacobel Red",
            "Lacobel Blue",
            "Lacobel Green",
            "Lacobel Brown",
            "AGC Matelux",
            "Matelux Clear",
            "Matelux Bronze",
            "Matelux Grey",

            // Saint-Gobain / SGG / Planitherm / Cool-Lite / Stadip
            "Saint-Gobain Clear",
            "SGG Planiclear",
            "SGG Planilux",
            "SGG Diamant",
            "SGG Parsol Blue",
            "SGG Parsol Green",
            "SGG Parsol Bronze",
            "SGG Parsol Grey",
            "SGG Antelio",
            "SGG Miralite",
            "SGG Cool-Lite",
            "SGG Cool-Lite SKN",
            "SGG Cool-Lite ST",
            "SGG Cool-Lite Xtreme",
            "SGG Planitherm",
            "Planitherm Clear",
            "Planitherm One",
            "Planitherm One II",
            "Planitherm Total+",
            "Planitherm XN",
            "Planitherm XN II",
            "Planitherm 4S",
            "Planitherm Ultra N",
            "Planitherm Low-E",
            "SGG Stadip",
            "SGG Stadip Silence",
            "SGG Stadip Protect",

            // Pilkington / NSG
            "Pilkington Clear",
            "Pilkington Optifloat",
            "Pilkington Optiwhite",
            "Pilkington K Glass",
            "Pilkington Low-E",
            "Pilkington Suncool",
            "Pilkington Solar-E",
            "Pilkington Arctic Blue",
            "Pilkington Activ",
            "Pilkington Laminated",
            "Pilkington Toughened",

            // Sisecam / Trakya
            "Şişecam Clear",
            "Şişecam Extra Clear",
            "Şişecam Low-E",
            "Şişecam Temperable Low-E",
            "Şişecam Solar Low-E",
            "Şişecam Tinted Blue",
            "Şişecam Tinted Green",
            "Şişecam Tinted Bronze",
            "Şişecam Tinted Grey",
            "Şişecam Reflective",
            "Şişecam Stopray",
            "Trakya Clear",
            "Trakya Tinted",
            "Trakya Reflective",
            "Trakya Low-E",

            // CAVS series
            "CAVS Clear",
            "CAVS Blue",
            "CAVS Green",
            "CAVS Bronze",
            "CAVS Grey",
            "CAVS Dark Grey",
            "CAVS Silver",
            "CAVS Gold",
            "CAVS Reflective",
            "CAVS Solar",
            "CAVS Low-E",
            "CAVS 120",
            "CAVS S120",
            "CAVS 140",
            "CAVS S160",
            "CAVS S180",
            "CAVS S200",
            "CAVS S220",
            "CAVS S240",
            "CAVS S280",
            "CAVS S320",
            "CAVS S360",
            "CAVS S400",

            // AGS / local stock
            "AGS Clear",
            "AGS Ultra Clear",
            "AGS Blue",
            "AGS Green",
            "AGS Bronze",
            "AGS Grey",
            "AGS Dark Grey",
            "AGS Reflective",
            "AGS Mirror",
            "AGS Low-E",
            "AGS Solar Control",
            "AGS Tempered",
            "AGS Laminated",

            // GCC / UAE stock
            "Emirates Glass",
            "Emirates Glass EmiCool",
            "Emirates Glass EmiCool Sun",
            "Emirates Glass EmiCool Super",
            "Dubai Glass",
            "Gulf Glass",
            "Saudi Glass",
            "Qatar Glass",
            "Kuwait Glass",
            "Local Clear",
            "Local Tinted",
            "Local Reflective",
            "Local Mirror",

            // Chinese / import
            "Xinyi Clear",
            "Xinyi Low-E",
            "Xinyi Reflective",
            "Xinyi Tinted",
            "Taiwan Glass Clear",
            "Taiwan Glass Tinted",
            "Taiwan Glass Reflective",
            "CSG Clear",
            "CSG Low-E",
            "Jinjing Clear",
            "Jinjing Low Iron",

            // Functional
            "Annealed",
            "Heat Strengthened",
            "Fully Tempered",
            "Heat Soaked",
            "PVB Laminated",
            "SGP Laminated",
            "Acoustic Laminated",
            "Security Laminated",
            "Fire Rated",
            "Ceramic Frit",
            "Digital Printed",
            "Sandblasted",
            "Custom",
            "Special Order",
            "Other"
        };

        // ═══════════════════════════════════════════════════════════════
        // CURRENT THICKNESS OPTIONS
        // Practical SGU / laminated / DGU build-ups
        // ═══════════════════════════════════════════════════════════════
        public static string[] Thicknesses { get; } =
        {
            // Standard monolithic
            "3mm",
            "4mm",
            "5mm",
            "6mm",
            "8mm",
            "10mm",
            "12mm",
            "15mm",
            "19mm",
            "25mm",

            // Laminated
            "6.38mm",
            "6.76mm",
            "8.38mm",
            "8.76mm",
            "10.38mm",
            "10.76mm",
            "12.38mm",
            "12.76mm",
            "13.52mm",
            "16.76mm",
            "17.52mm",
            "20.76mm",
            "21.52mm",
            "25.52mm",

            // Tempered
            "4mm T",
            "5mm T",
            "6mm T",
            "8mm T",
            "10mm T",
            "12mm T",
            "15mm T",
            "19mm T",

            // DGU / IGU common build-ups
            "5+6+5",
            "5+9+5",
            "5+12+5",
            "5+16+5",
            "6+6+6",
            "6+9+6",
            "6+12+6",
            "6+16+6",
            "8+12+8",
            "8+16+8",
            "10+12+10",
            "10+16+10",
            "12+12+12",
            "12+16+12",

            // Laminated DGU common build-ups
            "6.38+12+6",
            "6.38+16+6",
            "6.76+12+6",
            "6.76+16+6",
            "8.38+12+8",
            "8.38+16+8",
            "8.76+12+8",
            "8.76+16+8",
            "10.38+12+10",
            "10.38+16+10",
            "10.76+12+10",
            "10.76+16+10"
        };

        // ═══════════════════════════════════════════════════════════════
        // LATEST / COMMON SHEET SIZES
        // Width x Height in mm
        // Includes requested custom sizes:
        // 2500×1875, 3210×2600, 3660×2600, 3210×2500
        // ═══════════════════════════════════════════════════════════════
        public static List<(int Width, int Height, string Desc)> StandardSizes { get; } = new()
        {
            // Common jumbo / stock sizes
            (3210, 2250, "3210 × 2250"),
            (3210, 2400, "3210 × 2400"),
            (3210, 2440, "3210 × 2440"),
            (3210, 2500, "3210 × 2500"),
            (3210, 2550, "3210 × 2550"),
            (3210, 2600, "3210 × 2600"),
            (3210, 2800, "3210 × 2800"),
            (3210, 3000, "3210 × 3000"),

            (3300, 2140, "3300 × 2140"),
            (3300, 2250, "3300 × 2250"),
            (3300, 2440, "3300 × 2440"),
            (3300, 2500, "3300 × 2500"),
            (3300, 2600, "3300 × 2600"),

            (3350, 2140, "3350 × 2140"),
            (3350, 2250, "3350 × 2250"),
            (3350, 2440, "3350 × 2440"),
            (3350, 2500, "3350 × 2500"),
            (3350, 2600, "3350 × 2600"),

            (3660, 2140, "3660 × 2140"),
            (3660, 2250, "3660 × 2250"),
            (3660, 2400, "3660 × 2400"),
            (3660, 2440, "3660 × 2440"),
            (3660, 2500, "3660 × 2500"),
            (3660, 2600, "3660 × 2600"),
            (3660, 2800, "3660 × 2800"),
            (3660, 3000, "3660 × 3000"),

            (4500, 2440, "4500 × 2440"),
            (4500, 2500, "4500 × 2500"),
            (4500, 2600, "4500 × 2600"),
            (4500, 2800, "4500 × 2800"),
            (4500, 3000, "4500 × 3000"),

            (5100, 2440, "5100 × 2440"),
            (5100, 2600, "5100 × 2600"),
            (5100, 2800, "5100 × 2800"),
            (5100, 3000, "5100 × 3000"),
            (5100, 3210, "5100 × 3210"),

            (6000, 2440, "6000 × 2440"),
            (6000, 2600, "6000 × 2600"),
            (6000, 2800, "6000 × 2800"),
            (6000, 3000, "6000 × 3000"),
            (6000, 3210, "6000 × 3210"),

            // Medium/custom stock
            (2000, 1000, "2000 × 1000"),
            (2000, 1500, "2000 × 1500"),
            (2134, 1220, "2134 × 1220"),
            (2134, 1524, "2134 × 1524"),
            (2134, 1830, "2134 × 1830"),
            (2140, 3300, "2140 × 3300"),
            (2140, 3350, "2140 × 3350"),
            (2140, 3660, "2140 × 3660"),

            (2250, 1600, "2250 × 1600"),
            (2250, 1830, "2250 × 1830"),
            (2250, 3210, "2250 × 3210"),
            (2250, 3350, "2250 × 3350"),
            (2250, 3660, "2250 × 3660"),

            (2440, 1220, "2440 × 1220"),
            (2440, 1830, "2440 × 1830"),
            (2440, 2134, "2440 × 2134"),
            (2440, 3210, "2440 × 3210"),
            (2440, 3350, "2440 × 3350"),
            (2440, 3660, "2440 × 3660"),

            (2500, 1250, "2500 × 1250"),
            (2500, 1500, "2500 × 1500"),
            (2500, 1750, "2500 × 1750"),
            (2500, 1875, "2500 × 1875"),
            (2500, 2000, "2500 × 2000"),
            (2500, 2250, "2500 × 2250"),
            (2500, 3210, "2500 × 3210"),
            (2500, 3350, "2500 × 3350"),
            (2500, 3660, "2500 × 3660"),

            (2600, 3210, "2600 × 3210"),
            (2600, 3350, "2600 × 3350"),
            (2600, 3660, "2600 × 3660"),
            (2600, 4500, "2600 × 4500"),
            (2600, 5100, "2600 × 5100"),
            (2600, 6000, "2600 × 6000"),

            (2800, 3210, "2800 × 3210"),
            (2800, 3660, "2800 × 3660"),
            (2800, 4500, "2800 × 4500"),
            (2800, 5100, "2800 × 5100"),

            (3000, 1500, "3000 × 1500"),
            (3000, 2000, "3000 × 2000"),
            (3000, 2250, "3000 × 2250"),
            (3000, 2500, "3000 × 2500"),
            (3000, 3210, "3000 × 3210"),
            (3000, 3660, "3000 × 3660"),
            (3000, 4500, "3000 × 4500"),
            (3000, 5100, "3000 × 5100"),

            // Smaller practical stock/offcut sizes
            (1830, 1220, "1830 × 1220"),
            (1830, 1524, "1830 × 1524"),
            (1830, 2440, "1830 × 2440"),
            (1524, 1220, "1524 × 1220"),
            (1500, 1000, "1500 × 1000"),
            (1220, 915, "1220 × 915"),

            // Generic custom placeholders
            (1000, 1000, "Custom 1000 × 1000"),
            (1250, 1250, "Custom 1250 × 1250"),
            (1500, 1500, "Custom 1500 × 1500"),
            (2000, 2000, "Custom 2000 × 2000"),
            (2500, 2500, "Custom 2500 × 2500"),
            (3000, 3000, "Custom 3000 × 3000")
        };

        // ═══════════════════════════════════════════════════════════════
        // CURRENT COLORS
        // ═══════════════════════════════════════════════════════════════
        public static List<GlassColorItem> ColorItems { get; } = new()
        {
            new GlassColorItem { Name = "Clear", Hex = "#E8F4F8" },
            new GlassColorItem { Name = "Ultra Clear", Hex = "#F5FDFE" },
            new GlassColorItem { Name = "Extra Clear", Hex = "#F5FDFE" },
            new GlassColorItem { Name = "Low Iron", Hex = "#F0FAFC" },
            new GlassColorItem { Name = "Optiwhite", Hex = "#F8FFFD" },
            new GlassColorItem { Name = "Starphire", Hex = "#F5FFFC" },

            new GlassColorItem { Name = "Blue", Hex = "#4169E1" },
            new GlassColorItem { Name = "Light Blue", Hex = "#ADD8E6" },
            new GlassColorItem { Name = "Dark Blue", Hex = "#00008B" },
            new GlassColorItem { Name = "Arctic Blue", Hex = "#D6FFFC" },
            new GlassColorItem { Name = "Ocean Blue", Hex = "#4682B4" },

            new GlassColorItem { Name = "Green", Hex = "#228B22" },
            new GlassColorItem { Name = "Light Green", Hex = "#90EE90" },
            new GlassColorItem { Name = "Dark Green", Hex = "#006400" },
            new GlassColorItem { Name = "Forest Green", Hex = "#228B22" },
            new GlassColorItem { Name = "Emerald Green", Hex = "#50C878" },

            new GlassColorItem { Name = "Bronze", Hex = "#8B4513" },
            new GlassColorItem { Name = "Light Bronze", Hex = "#A0522D" },
            new GlassColorItem { Name = "Dark Bronze", Hex = "#5C3317" },
            new GlassColorItem { Name = "Golden Bronze", Hex = "#CD7F32" },

            new GlassColorItem { Name = "Grey", Hex = "#696969" },
            new GlassColorItem { Name = "Light Grey", Hex = "#A9A9A9" },
            new GlassColorItem { Name = "Dark Grey", Hex = "#36454F" },
            new GlassColorItem { Name = "Charcoal Grey", Hex = "#36454F" },
            new GlassColorItem { Name = "Smoke Grey", Hex = "#738276" },
            new GlassColorItem { Name = "Silver Grey", Hex = "#C0C0C0" },

            new GlassColorItem { Name = "Silver", Hex = "#C0C0C0" },
            new GlassColorItem { Name = "Reflective Silver", Hex = "#D4D4D4" },
            new GlassColorItem { Name = "Gold", Hex = "#FFD700" },
            new GlassColorItem { Name = "Reflective Gold", Hex = "#FFE44D" },
            new GlassColorItem { Name = "Mirror", Hex = "#D4D4D4" },
            new GlassColorItem { Name = "Mirror Silver", Hex = "#D4D4D4" },

            new GlassColorItem { Name = "Black", Hex = "#1C1C1C" },
            new GlassColorItem { Name = "Jet Black", Hex = "#0A0A0A" },
            new GlassColorItem { Name = "White", Hex = "#FFFFFF" },
            new GlassColorItem { Name = "Cream", Hex = "#FFFDD0" },

            new GlassColorItem { Name = "Frosted", Hex = "#E8F4F8" },
            new GlassColorItem { Name = "Acid Etched", Hex = "#DCE9EF" },
            new GlassColorItem { Name = "Matelux", Hex = "#DCE9EF" },

            new GlassColorItem { Name = "Lacobel White", Hex = "#FFFFFF" },
            new GlassColorItem { Name = "Lacobel Black", Hex = "#1C1C1C" },
            new GlassColorItem { Name = "Lacobel Grey", Hex = "#808080" },
            new GlassColorItem { Name = "Lacobel Red", Hex = "#DC2626" },
            new GlassColorItem { Name = "Lacobel Blue", Hex = "#2563EB" },
            new GlassColorItem { Name = "Lacobel Green", Hex = "#16A34A" },
            new GlassColorItem { Name = "Lacobel Brown", Hex = "#78350F" },

            new GlassColorItem { Name = "Neutral", Hex = "#D3D3D3" },
            new GlassColorItem { Name = "Custom", Hex = "#CBD5E1" }
        };

        // ═══════════════════════════════════════════════════════════════
        // CURRENT SUPPLIERS
        // ═══════════════════════════════════════════════════════════════
        public static List<string> Suppliers { get; } = new()
        {
            "Guardian Glass",
            "AGC Glass",
            "Saint-Gobain Glass",
            "SGG",
            "Pilkington",
            "NSG Group",
            "Şişecam",
            "Trakya Cam",
            "AGS Glass",
            "Emirates Glass",
            "Dubai Glass",
            "Gulf Glass",
            "Saudi Glass",
            "Qatar Glass",
            "Kuwait Glass",
            "Xinyi Glass",
            "Taiwan Glass",
            "CSG Glass",
            "Jinjing Glass",
            "Local Supplier",
            "Direct Import",
            "Other"
        };

        // ═══════════════════════════════════════════════════════════════
        // SUPPLIER METHODS
        // ═══════════════════════════════════════════════════════════════
        public static void AddSupplier(string supplierName)
        {
            if (string.IsNullOrWhiteSpace(supplierName))
                return;

            string trimmed = supplierName.Trim();

            if (!Suppliers.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            {
                Suppliers.Add(trimmed);
                Suppliers.Sort(StringComparer.OrdinalIgnoreCase);
            }
        }

        public static void RemoveSupplier(string supplierName)
        {
            if (string.IsNullOrWhiteSpace(supplierName))
                return;

            Suppliers.RemoveAll(s =>
                string.Equals(s, supplierName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public static bool SupplierExists(string supplierName)
        {
            return !string.IsNullOrWhiteSpace(supplierName) &&
                   Suppliers.Any(s =>
                       string.Equals(s, supplierName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        // ═══════════════════════════════════════════════════════════════
        // DGU / CALCULATOR OPTIONS
        // Kept for compatibility with existing calculator screens
        // ═══════════════════════════════════════════════════════════════
        public static List<string> AirspaceOptions { get; } = new()
        {
            "6mm",
            "8mm",
            "9mm",
            "10mm",
            "12mm",
            "14mm",
            "15mm",
            "16mm",
            "18mm",
            "20mm",
            "22mm",
            "24mm",
            "26mm",
            "28mm",
            "30mm"
        };

        public static List<string> Spacers { get; } = new()
        {
            "Aluminum 6mm",
            "Aluminum 8mm",
            "Aluminum 10mm",
            "Aluminum 12mm",
            "Aluminum 14mm",
            "Aluminum 15mm",
            "Aluminum 16mm",
            "Aluminum 18mm",
            "Aluminum 20mm",
            "Aluminum 22mm",
            "Aluminum 24mm",

            "Warm Edge 10mm",
            "Warm Edge 12mm",
            "Warm Edge 14mm",
            "Warm Edge 15mm",
            "Warm Edge 16mm",
            "Warm Edge 18mm",
            "Warm Edge 20mm",
            "Warm Edge 22mm",
            "Warm Edge 24mm",

            "Swiss Spacer 12mm",
            "Swiss Spacer 14mm",
            "Swiss Spacer 16mm",
            "Swiss Spacer 18mm",
            "Swiss Spacer 20mm",
            "Super Spacer 12mm",
            "Super Spacer 16mm",
            "Super Spacer 20mm"
        };

        public static List<string> SealantTypes { get; } = new()
        {
            "Primary Sealant",
            "Butyl Sealant",
            "Hot Melt Butyl",
            "Secondary Sealant",
            "Polysulfide Sealant",
            "Polyurethane Sealant",
            "Structural Silicone",
            "Neutral Silicone",
            "Dual Seal"
        };

        public static List<string> GasTypes { get; } = new()
        {
            "Air",
            "Dry Air",
            "Argon 80%",
            "Argon 90%",
            "Argon 95%",
            "Argon 99%",
            "Krypton",
            "Mix Gas"
        };

        public static List<string> PVBTypes { get; } = new()
        {
            "Clear PVB 0.38mm",
            "Clear PVB 0.76mm",
            "Clear PVB 1.14mm",
            "Clear PVB 1.52mm",
            "SentryGlas 0.89mm",
            "SentryGlas 1.52mm",
            "Acoustic PVB 0.76mm",
            "Acoustic PVB 1.52mm",
            "White PVB 0.76mm",
            "Grey PVB 0.76mm",
            "Bronze PVB 0.76mm",
            "Blue PVB 0.76mm",
            "Green PVB 0.76mm",
            "Custom PVB"
        };

        public static List<string> LaminationTypes { get; } = new()
        {
            "Standard Lamination",
            "Clear Lamination",
            "Tinted Lamination",
            "Low-E Lamination",
            "Solar Control Lamination",
            "Acoustic Lamination",
            "Security Lamination",
            "SentryGlas Lamination",
            "Custom Lamination"
        };

        public static List<string> EdgeWorkTypes { get; } = new()
        {
            "Clean Cut",
            "Arris Edge",
            "Flat Polish",
            "Pencil Polish",
            "Beveled Edge",
            "CNC Edge Work",
            "Waterjet Cut",
            "Custom Edge"
        };

        public static List<string> DrillingOptions { get; } = new()
        {
            "No Hole",
            "1 Hole",
            "2 Holes",
            "4 Holes",
            "Custom Holes",
            "Counter Sink Hole",
            "Notch Required",
            "Custom Cutout",
            "CNC Custom"
        };

        public static List<string> TemperingOptions { get; } = new()
        {
            "Annealed",
            "Heat Strengthened",
            "Fully Tempered",
            "Heat Soaked",
            "Tempered + Heat Soaked"
        };

        public static List<string> CoatingTypes { get; } = new()
        {
            "None",
            "Hard Coat Low-E",
            "Soft Coat Low-E",
            "Solar Control Coating",
            "Reflective Coating",
            "Guardian SunGuard",
            "Guardian ClimaGuard",
            "AGC Stopray",
            "AGC Planibel",
            "SGG Planitherm",
            "SGG Cool-Lite",
            "Custom Coating"
        };

        public static List<string> SurfaceTreatments { get; } = new()
        {
            "None",
            "Sandblasted",
            "Acid Etched",
            "Ceramic Frit",
            "Digital Print",
            "Silk Screen",
            "Easy Clean",
            "Custom"
        };

        public static List<string> CutoutOptions { get; } = new()
        {
            "No Cutout",
            "Rectangular Cutout",
            "Circular Cutout",
            "Door Hole",
            "Handle Hole",
            "Lock Hole",
            "Hinge Cutout",
            "Sensor Hole",
            "Custom Shape Cutout",
            "CNC Custom Cutout"
        };

        public static List<string> WastageOptions { get; } = new()
        {
            "0",
            "5",
            "10",
            "15",
            "20",
            "25",
            "30",
            "35",
            "40"
        };

        public static List<string> ProfitMarginOptions { get; } = new()
        {
            "0%",
            "5%",
            "10%",
            "15%",
            "20%",
            "25%",
            "30%",
            "35%",
            "40%",
            "45%",
            "50%",
            "60%",
            "70%",
            "80%",
            "100%"
        };

        public static List<string> UnitOptions { get; } = new()
        {
            "AED",
            "USD",
            "EUR",
            "AED/sqm",
            "USD/sqm"
        };

        public static List<string> StatusOptions { get; } = new()
        {
            "Active",
            "Inactive",
            "Discontinued",
            "Out of Stock",
            "On Order",
            "Low Stock"
        };

        public static List<string> PaymentMethods { get; } = new()
        {
            "Cash",
            "Bank Transfer",
            "Cheque",
            "Credit Card",
            "Credit / 7 Days",
            "Credit / 15 Days",
            "Credit / 30 Days",
            "Credit / 45 Days",
            "Credit / 60 Days",
            "Credit / 90 Days"
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPER CLASSES
    // ═══════════════════════════════════════════════════════════════
    public class SheetPurchase
    {
        public int Id { get; set; }
        public int SheetId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Supplier { get; set; } = "";
        public DateTime PurchasedOn { get; set; }
        public string Notes { get; set; } = "";
        public DateTime CreatedAt { get; set; }

        public decimal TotalAmt => Quantity * UnitPrice;
        public string DisplayDate => PurchasedOn.ToString("dd/MM/yyyy HH:mm");
    }

    public class SheetUsage
    {
        public int Id { get; set; }
        public int SheetId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; } = "";
        public DateTime UsedOn { get; set; }
        public DateTime CreatedAt { get; set; }

        public string DisplayDate => UsedOn.ToString("dd/MM/yyyy HH:mm");
    }

    public class GlassColorItem
    {
        public string Name { get; set; } = "";
        public string Hex { get; set; } = "";
    }

    // ═══════════════════════════════════════════════════════════════
    // CALCULATOR DATA CLASSES
    // Kept for compatibility with existing calculator screens
    // ═══════════════════════════════════════════════════════════════
    public static class SguCalculatorData
    {
        public static List<string> Categories => Sheet.Categories;
        public static string[] Thicknesses => Sheet.Thicknesses;
        public static List<GlassColorItem> Colors => Sheet.ColorItems;
        public static List<string> WastageOptions => Sheet.WastageOptions;
        public static List<string> ProfitMarginOptions => Sheet.ProfitMarginOptions;
    }

    public static class DguCalculatorData
    {
        public static List<string> Categories => Sheet.Categories;
        public static string[] Thicknesses => Sheet.Thicknesses;
        public static List<GlassColorItem> Colors => Sheet.ColorItems;
        public static List<string> Spacers => Sheet.Spacers;
        public static List<string> AirspaceOptions => Sheet.AirspaceOptions;
        public static List<string> SealantTypes => Sheet.SealantTypes;
        public static List<string> GasTypes => Sheet.GasTypes;
        public static List<string> PVBTypes => Sheet.PVBTypes;
        public static List<string> LaminationTypes => Sheet.LaminationTypes;
        public static List<string> WastageOptions => Sheet.WastageOptions;
        public static List<string> ProfitMarginOptions => Sheet.ProfitMarginOptions;
    }

    public static class LamiCalculatorData
    {
        public static List<string> Categories => Sheet.Categories;
        public static string[] Thicknesses => Sheet.Thicknesses;
        public static List<GlassColorItem> Colors => Sheet.ColorItems;
        public static List<string> PVBTypes => Sheet.PVBTypes;
        public static List<string> LaminationTypes => Sheet.LaminationTypes;
        public static List<string> TemperingOptions => Sheet.TemperingOptions;
        public static List<string> CoatingTypes => Sheet.CoatingTypes;
        public static List<string> EdgeWorkTypes => Sheet.EdgeWorkTypes;
        public static List<string> DrillingOptions => Sheet.DrillingOptions;
        public static List<string> SurfaceTreatments => Sheet.SurfaceTreatments;
        public static List<string> WastageOptions => Sheet.WastageOptions;
        public static List<string> ProfitMarginOptions => Sheet.ProfitMarginOptions;
    }

    public static class DguLamiCalculatorData
    {
        public static List<string> Categories => Sheet.Categories;
        public static string[] Thicknesses => Sheet.Thicknesses;
        public static List<GlassColorItem> Colors => Sheet.ColorItems;
        public static List<string> Spacers => Sheet.Spacers;
        public static List<string> AirspaceOptions => Sheet.AirspaceOptions;
        public static List<string> SealantTypes => Sheet.SealantTypes;
        public static List<string> GasTypes => Sheet.GasTypes;
        public static List<string> PVBTypes => Sheet.PVBTypes;
        public static List<string> LaminationTypes => Sheet.LaminationTypes;
        public static List<string> TemperingOptions => Sheet.TemperingOptions;
        public static List<string> CoatingTypes => Sheet.CoatingTypes;
        public static List<string> EdgeWorkTypes => Sheet.EdgeWorkTypes;
        public static List<string> DrillingOptions => Sheet.DrillingOptions;
        public static List<string> SurfaceTreatments => Sheet.SurfaceTreatments;
        public static List<string> WastageOptions => Sheet.WastageOptions;
        public static List<string> ProfitMarginOptions => Sheet.ProfitMarginOptions;
    }
}