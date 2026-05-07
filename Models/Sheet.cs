// Models/Sheet.cs
using System.Collections.Generic;
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

        // ═══════════════════════════════════════════════════════════════
        // PURCHASE & USE HISTORY
        // ═══════════════════════════════════════════════════════════════
        public List<SheetPurchase> PurchaseHistory { get; set; } = new();
        public List<SheetUsage> UseHistory { get; set; } = new();

        // ═══════════════════════════════════════════════════════════════
        // COMPUTED / DISPLAY PROPERTIES
        // ═══════════════════════════════════════════════════════════════
        public string DisplayDateTime => CreatedDate.ToString("dd/MM/yyyy HH:mm");
        public decimal TotalInvested => PurchaseHistory?.Sum(p => p.TotalAmt) ?? 0;
        public decimal AveragePurchasePrice => PurchaseHistory?.Count > 0
            ? PurchaseHistory.Average(p => p.UnitPrice) : PurchasePrice;
        public decimal TotalSaleValue => TotalStock * SellPrice;

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

        // ═══════════════════════════════════════════════════════════════
        // GLASS CATEGORIES (300+)
        // ═══════════════════════════════════════════════════════════════
        public static List<string> Categories = new List<string>
        {
            // ─────────────────────────────────────────────────────────────
            // CLEAR / FLOAT GLASS
            // ─────────────────────────────────────────────────────────────
            "Clear Float", "Clear Sheet", "Clear Plate",
            "Ultra Clear", "Ultra Clear Float", "Crystal Clear",
            "Optifloat Clear", "Starphire Ultra Clear", "Ultra White",
            "Low Iron Clear", "Starphire", "Diamant",
            "Planiclear", "SGG Ultra Clear", "Optiwhite",
            "Mister Prismatic", "Stopsol Clear",

            // ─────────────────────────────────────────────────────────────
            // TINTED GLASS - BLUE SERIES
            // ─────────────────────────────────────────────────────────────
            "Blue Float", "Azure Blue", "Ocean Blue", "Sky Blue",
            "Royal Blue", "Navy Blue", "Cobalt Blue", "Steel Blue",
            "Denim Blue", "Sapphire Blue", "Indigo Blue",
            "HD Blue", "PNA Blue", "Ramly Blue",
            "Sunlux Blue", "Reflite Blue", "Reflux Blue",
            "Tinted Blue", "Reflective Blue", "Mirror Blue",
            "Stopsol Classic Blue", "Stopsol Superburn Blue",
            "Stopray Classic Blue", "Stopray Blue",
            "Chromafloat Blue", "Sunergy Blue", "Sunergy Plus Blue",
            "SunGuard Blue", "Guardian Blue",
            "SGG Tinted Blue", "SGG Reflective Blue",
            "Pilkington Blue", "Pilkington Arctic Blue",
            "Şişecam Blue", "Sisecam Blue",
            "Trakya Blue", "AGC Tinted Blue",
            "PGI Tinted Blue", "PGI Reflective Blue",
            "Taiwan Tinted Blue", "Taiwan Reflective Blue",
            "Antelio Blue",
            "Spectran Blue",
            "Lacobel Blue",
            "Azur Blue", "Azurlite Blue",
            "Cool Blue", "Sun Blue",

            // ─────────────────────────────────────────────────────────────
            // TINTED GLASS - GREEN SERIES
            // ─────────────────────────────────────────────────────────────
            "Green Float", "Forest Green", "Emerald Green", "Jade Green",
            "Mint Green", "Sage Green", "Olive Green", "Teal Green",
            "Sea Green", "Dark Green", "Light Green",
            "HD Green", "PNA Green", "Ramly Green",
            "Sunlux Green", "Reflite Green", "Reflux Green",
            "Belgium Green",
            "Tinted Green", "Reflective Green", "Mirror Green",
            "Planibel Green",
            "Stopsol Classic Green", "Stopsol Superburn Green",
            "Stopray Classic Green", "Stopray Green",
            "Chromafloat Green", "Sunergy Green",
            "SunGuard Green", "Guardian Green",
            "SGG Tinted Green", "SGG Reflective Green",
            "Pilkington Green",
            "Şişecam Stopray Green", "Şişecam Tinted Green",
            "Trakya Stopray Green", "Trakya Tinted Green",
            "AGC Tinted Green",
            "PGI Tinted Green", "PGI Reflective Green",
            "Taiwan Tinted Green",
            "Antelio Green",
            "Lacobel Green",
            "Symmetry Green", "Evergreen",
            "Cool Green", "Nature Green", "Verdant Green",

            // ─────────────────────────────────────────────────────────────
            // TINTED GLASS - BRONZE SERIES
            // ─────────────────────────────────────────────────────────────
            "Bronze Float", "Dark Bronze", "Light Bronze", "Medium Bronze",
            "Golden Bronze", "Warm Bronze", "Cool Bronze",
            "HD Bronze", "PNA Bronze", "Ramly Bronze",
            "Sunlux Bronze", "Reflite Bronze", "Reflux Bronze",
            "Belgium Bronze",
            "Tinted Bronze", "Reflective Bronze", "Mirror Bronze",
            "Planibel Bronze",
            "Stopsol Classic Bronze", "Stopsol Superburn Bronze",
            "Stopray Classic Bronze", "Stopray Bronze",
            "Sunergy Bronze",
            "SunGuard Bronze",
            "Şişecam Stopray Bronze", "Şişecam Tinted Bronze",
            "Şişecam Reflective Bronze",
            "Trakya Stopray Bronze", "Trakya Tinted Bronze",
            "SGG Tinted Bronze", "SGG Reflective Bronze",
            "Miralite Bronze",
            "Lacobel Brown",
            "Symmetry Bronze", "Estate Bronze",
            "Cool Bronze", "Antique Bronze",

            // ─────────────────────────────────────────────────────────────
            // TINTED GLASS - GREY SERIES
            // ─────────────────────────────────────────────────────────────
            "Grey Float", "Dark Grey", "Light Grey", "Medium Grey",
            "Charcoal Grey", "Smoke Grey", "Ash Grey", "Slate Grey",
            "Pearl Grey", "Silver Grey", "Gunmetal Grey",
            "HD Grey", "PNA Grey", "Ramly Grey",
            "Sunlux Grey", "Reflite Grey", "Reflux Grey",
            "Belgium Grey",
            "Tinted Grey", "Reflective Grey", "Mirror Grey",
            "Planibel Grey",
            "Stopsol Classic Grey", "Stopsol Superburn Grey",
            "Stopray Classic Grey", "Stopray Grey",
            "Sunergy Grey",
            "SunGuard Grey",
            "Şişecam Stopray Grey", "Şişecam Tinted Grey",
            "Trakya Stopray Grey", "Trakya Tinted Grey",
            "SGG Tinted Grey", "SGG Reflective Grey",
            "Lacobel Grey",
            "Symmetry Grey", "Pewter Grey",
            "Cool Grey", "Storm Grey", "Granite Grey",

            // ─────────────────────────────────────────────────────────────
            // REFLECTIVE / MIRROR - SILVER SERIES
            // ─────────────────────────────────────────────────────────────
            "Reflective Silver", "Mirror Silver", "Silver Mirror",
            "Silver Reflective", "Hard Reflective Silver",
            "PNA Reflective Silver",
            "Sunlux Silver", "Reflite Silver", "Reflux Silver",
            "Stopsol Silver Lite", "Stopsol Silver Dark",
            "Stopsol Superburn Silver",
            "Stopray Silver", "Stopray Vision Silver",
            "Chromafloat Silver",
            "Sunergy Silver",
            "Guardian Reflective Silver",
            "SunGuard Solar Silver",
            "Şişecam Reflective Silver",
            "SGG Reflective Silver",
            "PGI Reflective Silver",
            "Taiwan Reflective Silver",
            "Antelio Silver",
            "Miralite Silver",
            "Spectran Silver",
            "Cool Silver", "Platinum Silver",

            // ─────────────────────────────────────────────────────────────
            // REFLECTIVE / MIRROR - GOLD SERIES
            // ─────────────────────────────────────────────────────────────
            "Reflective Gold", "Mirror Gold", "Gold Mirror",
            "Gold Reflective", "Hard Reflective Gold",
            "PNA Reflective Gold",
            "Sunlux Gold", "Reflite Gold", "Reflux Gold",
            "Stopray Gold", "Stopray Vision Gold",
            "Chromafloat Gold",
            "Guardian Reflective Gold",
            "SunGuard Solar Gold",
            "Şişecam Reflective Gold",
            "SGG Reflective Gold",
            "Antelio Gold",
            "Miralite Gold",
            "Cool Gold", "Champagne Gold",

            // ─────────────────────────────────────────────────────────────
            // BLACK / DARK SERIES
            // ─────────────────────────────────────────────────────────────
            "Black Float", "Dark Black", "Jet Black", "Pure Black",
            "HD Black", "PNA Black",
            "Tinted Black",
            "Lacobel Black", "Lacobel Extra Black",
            "Lacobel Jet Black",
            "Dark Grey Float", "Graphite",
            "Obsidian", "Onyx",

            // ─────────────────────────────────────────────────────────────
            // PRIVACY / OBSCURE GLASS
            // ─────────────────────────────────────────────────────────────
            "Frosted Clear", "Frosted", "Acid Etched", "Satin Glass",
            "Matelux Clear", "Matelux Bronze", "Matelux Grey", "Matelux Green",
            "Decormatt", "Mastercote", "Satinato", "Masterglass",
            "Patterned Glass", "Rolled Glass", "Obscure Glass",
            "Rain Glass", "Reeded Glass", "Fluted Glass",
            "Bubble Glass", "Coral Glass", "Euro Greek",
            "Baroque", "Chinchilla", "M Wire",
            "Laminated Frosted", "Sandblasted",
            "SunGuard Privacy",

            // ─────────────────────────────────────────────────────────────
            // LOW-E GLASS
            // ─────────────────────────────────────────────────────────────
            "Low-E Clear", "Low-E Neutral", "Low-E Silver",
            "Low-E Tempered", "Low-E Laminated",
            "iPlus 1.0", "iPlus 1.1", "iPlus 1.2", "iPlus 1.3",
            "Comfort Plus", "Energy Advantage", "Energy Smart",
            "SGG Planitherm One", "SGG Planitherm Total",
            "SGG Planitherm Ultra N", "SGG Planitherm Ultra",
            "SunGuard Neutral 63", "SunGuard Neutral 70",
            "SunGuard Neutral 78",
            "Solarban 60", "Solarban 70", "Solarban 70XL",
            "Solarban 90", "Solarban 100",
            "Guardian Low-E", "Guardian Ultra Low-E",
            "Pilkington K Glass", "Pilkington Low-E",
            "Pilkington Optifloat Low-E",
            "Şişecam Low-E", "Şişecam Temper Low-E",
            "Planibel Top N+", "Planibel A",
            "Thermafoil",
            "Thermoplus", "Thermobel",

            // ─────────────────────────────────────────────────────────────
            // TEMPERED / TOUGHENED GLASS
            // ─────────────────────────────────────────────────────────────
            "Tempered Clear", "Tempered Tinted", "Tempered Low-E",
            "Toughened Clear", "Toughened Tinted", "Toughened Low-E",
            "Tempered Bronze", "Tempered Grey", "Tempered Green",
            "Tempered Blue", "Tempered Reflective",
            "Fully Tempered", "Heat Strengthened",
            "Şişecam Tempered", "AGC Tempered",
            "Pilkington Tempered", "Guardian Tempered",
            "SGG Temperit",

            // ─────────────────────────────────────────────────────────────
            // LAMINATED GLASS
            // ─────────────────────────────────────────────────────────────
            "Laminated Clear", "Laminated Tinted", "Laminated Low-E",
            "Laminated Bronze", "Laminated Grey", "Laminated Green",
            "Laminated Blue", "Laminated Reflective",
            "Laminated Acoustic", "Laminated Security",
            "Laminated UV Filter", "Laminated PVB",
            "SGG Stadip", "SGG Stadip Silence",
            "Pilkington Laminate", "Guardian Laminated",
            "Şişecam Laminated", "AGC Laminated",
            "Multilaminate", "Bi-Laminate", "Tri-Laminate",

            // ─────────────────────────────────────────────────────────────
            // FIRE RATED GLASS
            // ─────────────────────────────────────────────────────────────
            "Pyrobel Clear", "Pyrobel Bronze",
            "Pyrostop 30", "Pyrostop 45", "Pyrostop 60",
            "Pyrostop 90", "Pyrostop 120",
            "Pyrodur", "Pyroguard", "Pyroglass",
            "Firelite", "Fire Plus",
            "Contraflan", "Promat",

            // ─────────────────────────────────────────────────────────────
            // AUTO GLASS
            // ─────────────────────────────────────────────────────────────
            "Automotive Clear", "Automotive Tinted",
            "Automotive Tempered", "Automotive Laminated",
            "Windscreen", "Rear Lite", "Side Lite",
            "Sunroof Glass", "Heated Glass",
            "Acoustic Glass", "UV Cut Glass",
            "Privacy Glass", "IR Rejection",
            "Ceramic Frit", "Enameled Glass",

            // ─────────────────────────────────────────────────────────────
            // INSULATED / DOUBLE GLAZING
            // ─────────────────────────────────────────────────────────────
            "IGU Clear", "IGU Tinted", "IGU Low-E",
            "Double Glazing Unit", "Double Glazed",
            "Triple Glazing Unit", "Triple Glazed",
            "SGG Climalit", "SGG Climalit Plus",
            "Pilkington Insulight",
            "Thermoseal", "Thermobar",
            "Spacer Bar", "Warm Edge Spacer",
            "Desiccant Filled",

            // ─────────────────────────────────────────────────────────────
            // SOLAR CONTROL / ENERGY SAVING
            // ─────────────────────────────────────────────────────────────
            "Solar Control Clear", "Solar Control Tinted",
            "Solar Control Reflective",
            "Energy Saving Clear", "Energy Saving Low-E",
            "SunCool", "SunStop",
            "Reflectasol", "Reflexasol",
            "EcoClear", "EnviroShield",

            // ─────────────────────────────────────────────────────────────
            // SPECIALTY / DECORATIVE GLASS
            // ─────────────────────────────────────────────────────────────
            "Lacobel White", "Lacobel Black", "Lacobel Grey",
            "Lacobel Red", "Lacobel Blue", "Lacobel Brown",
            "Lacobel Green", "Lacobel Yellow",
            "Lacobel Orange", "Lacobel Purple",
            "Lacobel Extra White", "Lacobel Easy Clean",
            "Lacobel T",
            "ESG Switchglass", "PDLC Glass",
            "Smart Glass", "Electrochromic Glass",
            "Self-Cleaning Glass", "Bioclean",
            "Nano Coating Glass",
            "Anti-Reflective Glass", "AR Glass",
            "Anti-Graffiti Glass", "Security Glass",
            "Ballistic Glass", "Blast Resistant",
            "X-Ray Protective", "Radiation Shield",
            "Mirrored Glass", "One-Way Mirror",
            "Spandrel Glass", "Back painted Glass",
            "Color Coated Glass", " enameled Glass",
            "Ceramic Coated Glass",
            "Textured Glass", "Decorative Pattern",
            "Bent Glass", "Curved Glass", "Tempered Curved",
            "Slumped Glass", "Fused Glass",
            "Glass Blocks", "Glass Bricks",

            // ─────────────────────────────────────────────────────────────
            // OTHER / MISCELLANEOUS
            // ─────────────────────────────────────────────────────────────
            "Custom", "Special Order", "Made to Order",
            "Other", "Mixed", "Various",
                        "Sample", "Display", "Test Glass",
            "Non-Standard", "Special Size",
            "Remnant", "Offcut", "Leftover"
        };

        // ═══════════════════════════════════════════════════════════════
        // THICKNESS OPTIONS (mm)
        // ═══════════════════════════════════════════════════════════════
        public static string[] Thicknesses { get; } = new string[]
        {
            // Architectural / Standard
            "2mm", "2.5mm", "3mm", "4mm", "5mm", "6mm",
            "8mm", "10mm", "12mm", "15mm", "19mm",
            // Automotive
            "3.2mm", "3.5mm", "4mm Auto", "5mm Auto",
            // Heavy / Industrial
            "20mm", "25mm", "30mm",
            // Laminated (total thickness)
            "6.38mm", "6.76mm", "8.38mm", "8.76mm",
            "10.38mm", "10.76mm", "12.38mm", "12.76mm",
            // Insulating Unit (total)
            "14mm", "16mm", "18mm", "20mm", "22mm", "24mm",
            "26mm", "28mm", "30mm",
            // Tempered
            "4mm T", "5mm T", "6mm T", "8mm T", "10mm T", "12mm T"
        };

        // ═══════════════════════════════════════════════════════════════
        // STANDARD SIZES (mm) - Width x Height
        // ═══════════════════════════════════════════════════════════════
        public static List<(int Width, int Height, string Desc)> StandardSizes = new List<(int, int, string)>
        {
            // ─────────────────────────────────────────────────────────────
            // FEET-BASED SIZES (Imperial)
            // ─────────────────────────────────────────────────────────────
            
            // 6 FT
            (1830, 1220, "6'x4'"), (1830, 1524, "6'x5'"), (1830, 1600, "6'x5.25'"),
            (1830, 1830, "6'x6'"), (1830, 2134, "6'x7'"), (1830, 2250, "6'x7.4'"),
            (1830, 2440, "6'x8'"), (1830, 2500, "6'x8.2'"), (1830, 2745, "6'x9'"),
            (1830, 3050, "6'x10'"), (1830, 3300, "6'x10.8'"), (1830, 3660, "6'x12'"),
            
            // 7 FT
            (2134, 1220, "7'x4'"), (2134, 1524, "7'x5'"), (2134, 1600, "7'x5.25'"),
            (2134, 1830, "7'x6'"), (2134, 2134, "7'x7'"), (2134, 2250, "7'x7.4'"),
            (2134, 2440, "7'x8'"), (2134, 2500, "7'x8.2'"), (2134, 2745, "7'x9'"),
            (2134, 3050, "7'x10'"), (2134, 3300, "7'x10.8'"), (2134, 3660, "7'x12'"),
            
            // 8 FT
            (2440, 1220, "8'x4'"), (2440, 1524, "8'x5'"), (2440, 1600, "8'x5.25'"),
            (2440, 1830, "8'x6'"), (2440, 2134, "8'x7'"), (2440, 2250, "8'x7.4'"),
            (2440, 2440, "8'x8'"), (2440, 2500, "8'x8.2'"), (2440, 2745, "8'x9'"),
            (2440, 3050, "8'x10'"), (2440, 3300, "8'x10.8'"), (2440, 3660, "8'x12'"),
            (2440, 4270, "8'x14'"), (2440, 4880, "8'x16'"),
            
            // 9 FT
            (2745, 1220, "9'x4'"), (2745, 1524, "9'x5'"), (2745, 1600, "9'x5.25'"),
            (2745, 1830, "9'x6'"), (2745, 2134, "9'x7'"), (2745, 2250, "9'x7.4'"),
            (2745, 2440, "9'x8'"), (2745, 2500, "9'x8.2'"), (2745, 2745, "9'x9'"),
            (2745, 3050, "9'x10'"), (2745, 3300, "9'x10.8'"), (2745, 3660, "9'x12'"),
            (2745, 4270, "9'x14'"), (2745, 4880, "9'x16'"),
            
            // 10 FT
            (3050, 1220, "10'x4'"), (3050, 1524, "10'x5'"), (3050, 1600, "10'x5.25'"),
            (3050, 1830, "10'x6'"), (3050, 2134, "10'x7'"), (3050, 2250, "10'x7.4'"),
            (3050, 2440, "10'x8'"), (3050, 2500, "10'x8.2'"), (3050, 2745, "10'x9'"),
            (3050, 3050, "10'x10'"), (3050, 3300, "10'x10.8'"), (3050, 3660, "10'x12'"),
            (3050, 4270, "10'x14'"), (3050, 4880, "10'x16'"),
            
            // 10.5 FT
            (3210, 1220, "10.5'x4'"), (3210, 1524, "10.5'x5'"), (3210, 1600, "10.5'x5.25'"),
            (3210, 1830, "10.5'x6'"), (3210, 2134, "10.5'x7'"), (3210, 2250, "10.5'x7.4'"),
            (3210, 2440, "10.5'x8'"), (3210, 2500, "10.5'x8.2'"), (3210, 2745, "10.5'x9'"),
            (3210, 3050, "10.5'x10'"), (3210, 3300, "10.5'x10.8'"), (3210, 3660, "10.5'x12'"),
            
            // 11 FT
            (3350, 1220, "11'x4'"), (3350, 1524, "11'x5'"), (3350, 1600, "11'x5.25'"),
            (3350, 1830, "11'x6'"), (3350, 2134, "11'x7'"), (3350, 2250, "11'x7.4'"),
            (3350, 2440, "11'x8'"), (3350, 2500, "11'x8.2'"), (3350, 2745, "11'x9'"),
            (3350, 3050, "11'x10'"), (3350, 3300, "11'x10.8'"), (3350, 3660, "11'x12'"),
            
            // 12 FT
            (3660, 1220, "12'x4'"), (3660, 1524, "12'x5'"), (3660, 1600, "12'x5.25'"),
            (3660, 1830, "12'x6'"), (3660, 2134, "12'x7'"), (3660, 2250, "12'x7.4'"),
            (3660, 2440, "12'x8'"), (3660, 2500, "12'x8.2'"), (3660, 2745, "12'x9'"),
            (3660, 3050, "12'x10'"), (3660, 3300, "12'x10.8'"), (3660, 3660, "12'x12'"),
            
            // 13 FT
            (3960, 1220, "13'x4'"), (3960, 1524, "13'x5'"), (3960, 1600, "13'x5.25'"),
            (3960, 1830, "13'x6'"), (3960, 2134, "13'x7'"), (3960, 2250, "13'x7.4'"),
            (3960, 2440, "13'x8'"), (3960, 2500, "13'x8.2'"), (3960, 2745, "13'x9'"),
            (3960, 3050, "13'x10'"), (3960, 3300, "13'x10.8'"), (3960, 3660, "13'x12'"),
            
            // 14 FT
            (4270, 1220, "14'x4'"), (4270, 1524, "14'x5'"), (4270, 1600, "14'x5.25'"),
            (4270, 1830, "14'x6'"), (4270, 2134, "14'x7'"), (4270, 2250, "14'x7.4'"),
            (4270, 2440, "14'x8'"), (4270, 2500, "14'x8.2'"), (4270, 2745, "14'x9'"),
            (4270, 3050, "14'x10'"), (4270, 3300, "14'x10.8'"), (4270, 3660, "14'x12'"),
            
            // 15 FT
            (4570, 1220, "15'x4'"), (4570, 1524, "15'x5'"), (4570, 1600, "15'x5.25'"),
            (4570, 1830, "15'x6'"), (4570, 2134, "15'x7'"), (4570, 2250, "15'x7.4'"),
            (4570, 2440, "15'x8'"), (4570, 2500, "15'x8.2'"), (4570, 2745, "15'x9'"),
            (4570, 3050, "15'x10'"), (4570, 3300, "15'x10.8'"), (4570, 3660, "15'x12'"),
            
            // 16 FT
            (4880, 1220, "16'x4'"), (4880, 1524, "16'x5'"), (4880, 1600, "16'x5.25'"),
            (4880, 1830, "16'x6'"), (4880, 2134, "16'x7'"), (4880, 2250, "16'x7.4'"),
            (4880, 2440, "16'x8'"), (4880, 2500, "16'x8.2'"), (4880, 2745, "16'x9'"),
            (4880, 3050, "16'x10'"), (4880, 3300, "16'x10.8'"), (4880, 3660, "16'x12'"),
            
            // 18 FT
            (5490, 1220, "18'x4'"), (5490, 1524, "18'x5'"), (5490, 1600, "18'x5.25'"),
            (5490, 1830, "18'x6'"), (5490, 2134, "18'x7'"), (5490, 2250, "18'x7.4'"),
            (5490, 2440, "18'x8'"), (5490, 2500, "18'x8.2'"), (5490, 2745, "18'x9'"),
            (5490, 3050, "18'x10'"), (5490, 3300, "18'x10.8'"), (5490, 3660, "18'x12'"),
            
            // 20 FT
            (6100, 1220, "20'x4'"), (6100, 1524, "20'x5'"), (6100, 1600, "20'x5.25'"),
            (6100, 1830, "20'x6'"), (6100, 2134, "20'x7'"), (6100, 2250, "20'x7.4'"),
            (6100, 2440, "20'x8'"), (6100, 2500, "20'x8.2'"), (6100, 2745, "20'x9'"),
            (6100, 3050, "20'x10'"), (6100, 3300, "20'x10.8'"), (6100, 3660, "20'x12'"),

            // ─────────────────────────────────────────────────────────────
            // METRIC SIZES (mm)
            // ─────────────────────────────────────────────────────────────
            
            // 2000 Series
            (2000, 1000, ""), (2000, 1250, ""), (2000, 1500, ""),
            (2000, 1750, ""), (2000, 2000, ""), (2000, 2250, ""),
            (2000, 2500, ""), (2000, 2750, ""), (2000, 3000, ""),
            
            // 2100 Series
            (2100, 1000, ""), (2100, 1250, ""), (2100, 1500, ""),
            (2100, 1750, ""), (2100, 2000, ""), (2100, 2100, ""),
            (2100, 2250, ""), (2100, 2500, ""), (2100, 2750, ""),
            (2100, 3000, ""),
            
            // 2250 Series
            (2250, 1000, ""), (2250, 1250, ""), (2250, 1500, ""),
            (2250, 1750, ""), (2250, 2000, ""), (2250, 2250, ""),
            (2250, 2500, ""), (2250, 2750, ""), (2250, 3000, ""),
            
            // 2400 Series
            (2400, 1000, ""), (2400, 1250, ""), (2400, 1500, ""),
            (2400, 1750, ""), (2400, 2000, ""), (2400, 2400, ""),
            (2400, 2500, ""), (2400, 2750, ""), (2400, 3000, ""),
            
            // 2500 Series
            (2500, 1000, ""), (2500, 1250, ""), (2500, 1500, ""),
            (2500, 1750, ""), (2500, 2000, ""), (2500, 2500, ""),
            (2500, 2750, ""), (2500, 3000, ""),
            
            // 2700 Series
            (2700, 1000, ""), (2700, 1250, ""), (2700, 1500, ""),
            (2700, 1750, ""), (2700, 2000, ""), (2700, 2700, ""),
            (2700, 2500, ""), (2700, 3000, ""),
            
                        // 3000 Series (continued)
            (3000, 1750, ""), (3000, 2000, ""), (3000, 2500, ""),
            (3000, 3000, ""),

            // 3300 Series
            (3300, 1000, ""), (3300, 1250, ""), (3300, 1500, ""),
            (3300, 1750, ""), (3300, 2000, ""), (3300, 2500, ""),
            (3300, 2750, ""), (3300, 3000, ""), (3300, 3300, ""),

            // 3500 Series
            (3500, 1000, ""), (3500, 1250, ""), (3500, 1500, ""),
            (3500, 1750, ""), (3500, 2000, ""), (3500, 2500, ""),
            (3500, 3000, ""),

            // 3600 Series
            (3600, 1000, ""), (3600, 1250, ""), (3600, 1500, ""),
            (3600, 1750, ""), (3600, 2000, ""), (3600, 2500, ""),
            (3600, 3000, ""),

            // 4000 Series
            (4000, 1000, ""), (4000, 1250, ""), (4000, 1500, ""),
            (4000, 1750, ""), (4000, 2000, ""), (4000, 2500, ""),
            (4000, 3000, ""),

            // 4500 Series
            (4500, 1000, ""), (4500, 1250, ""), (4500, 1500, ""),
            (4500, 1750, ""), (4500, 2000, ""), (4500, 2500, ""),
            (4500, 3000, ""),

            // 5000 Series
            (5000, 1000, ""), (5000, 1250, ""), (5000, 1500, ""),
            (5000, 1750, ""), (5000, 2000, ""), (5000, 2500, ""),
            (5000, 3000, ""),

            // 5500 Series
            (5500, 1000, ""), (5500, 1250, ""), (5500, 1500, ""),
            (5500, 1750, ""), (5500, 2000, ""), (5500, 2500, ""),
            (5500, 3000, ""),

            // 6000 Series
            (6000, 1000, ""), (6000, 1250, ""), (6000, 1500, ""),
            (6000, 1750, ""), (6000, 2000, ""), (6000, 2500, ""),
            (6000, 3000, ""),

            // 6500 Series
            (6500, 1000, ""), (6500, 1250, ""), (6500, 1500, ""),
            (6500, 1750, ""), (6500, 2000, ""), (6500, 2500, ""),
            (6500, 3000, ""),

            // 7000 Series
            (7000, 1000, ""), (7000, 1250, ""), (7000, 1500, ""),
            (7000, 1750, ""), (7000, 2000, ""), (7000, 2500, ""),
            (7000, 3000, ""),

            // 7500 Series
            (7500, 1000, ""), (7500, 1250, ""), (7500, 1500, ""),
            (7500, 1750, ""), (7500, 2000, ""), (7500, 2500, ""),
            (7500, 3000, ""),

            // 8000 Series
            (8000, 1000, ""), (8000, 1250, ""), (8000, 1500, ""),
            (8000, 1750, ""), (8000, 2000, ""), (8000, 2500, ""),
            (8000, 3000, ""),

            // ─────────────────────────────────────────────────────────────
            // AUTOMOTIVE SIZES (mm)
            // ─────────────────────────────────────────────────────────────
            (1400, 900, "Auto"), (1500, 900, "Auto"), (1600, 900, "Auto"),
            (1700, 900, "Auto"), (1800, 900, "Auto"), (1900, 900, "Auto"),
            (2000, 900, "Auto"),
            (1400, 1000, "Auto"), (1500, 1000, "Auto"), (1600, 1000, "Auto"),
            (1700, 1000, "Auto"), (1800, 1000, "Auto"), (1900, 1000, "Auto"),
            (2000, 1000, "Auto"),
            (1200, 800, "Auto"), (1300, 800, "Auto"), (1400, 800, "Auto"),
            (1500, 800, "Auto"), (1600, 800, "Auto"),
            (1100, 700, "Auto"), (1200, 700, "Auto"), (1300, 700, "Auto"),
            (1400, 700, "Auto"), (1500, 700, "Auto"),
            (1000, 600, "Auto"), (1100, 600, "Auto"), (1200, 600, "Auto"),
            (1300, 600, "Auto"), (1400, 600, "Auto"),

            // ─────────────────────────────────────────────────────────────
            // ROUND / CIRCULAR SIZES (Diameter mm)
            // ─────────────────────────────────────────────────────────────
            (500, 500, "Round"), (600, 600, "Round"), (700, 700, "Round"),
            (800, 800, "Round"), (900, 900, "Round"), (1000, 1000, "Round"),
            (1100, 1100, "Round"), (1200, 1200, "Round"), (1500, 1500, "Round"),
            (1800, 1800, "Round"), (2000, 2000, "Round"), (2500, 2500, "Round"),
            (3000, 3000, "Round"),

            // ─────────────────────────────────────────────────────────────
            // CUSTOM / NON-STANDARD
            // ─────────────────────────────────────────────────────────────
            (1000, 1000, "Custom"), (1250, 1250, "Custom"),
            (1500, 1500, "Custom"), (1750, 1750, "Custom"),
            (2000, 1500, "Custom"), (2250, 1750, "Custom"),
            (2500, 1750, "Custom"), (2750, 2000, "Custom"),
            (3000, 2000, "Custom"), (3250, 2250, "Custom"),
            (3500, 2250, "Custom"), (3750, 2500, "Custom"),
            (4000, 2250, "Custom"), (4250, 2500, "Custom"),
            (4500, 2500, "Custom"), (4750, 2750, "Custom"),
            (5000, 2500, "Custom"), (5250, 2750, "Custom"),
            (5500, 2750, "Custom"), (5750, 3000, "Custom"),
            (6000, 3000, "Custom")
        };

        // ═══════════════════════════════════════════════════════════════
        // COLOR ITEMS (with Hex values)
        // ═══════════════════════════════════════════════════════════════
        public static List<GlassColorItem> ColorItems = new List<GlassColorItem>
        {
            // ─────────────────────────────────────────────────────────────
            // TRANSPARENT / CLEAR
            // ─────────────────────────────────────────────────────────────
            new GlassColorItem { Name = "Clear", Hex = "#E8F4F8" },
            new GlassColorItem { Name = "Ultra Clear", Hex = "#F5FDFE" },
            new GlassColorItem { Name = "Crystal Clear", Hex = "#F0F8FF" },
            new GlassColorItem { Name = "Pure Clear", Hex = "#F8FCFD" },
            new GlassColorItem { Name = "Low Iron", Hex = "#F0FAFC" },
            new GlassColorItem { Name = "Starphire", Hex = "#F5FFFC" },
            new GlassColorItem { Name = "Optiwhite", Hex = "#F8FFFD" },

            // ─────────────────────────────────────────────────────────────
            // GREY / CHARCOAL
            // ─────────────────────────────────────────────────────────────
            new GlassColorItem { Name = "Grey", Hex = "#696969" },
            new GlassColorItem { Name = "Light Grey", Hex = "#A9A9A9" },
            new GlassColorItem { Name = "Medium Grey", Hex = "#808080" },
            new GlassColorItem { Name = "Dark Grey", Hex = "#36454F" },
            new GlassColorItem { Name = "Charcoal Grey", Hex = "#36454F" },
            new GlassColorItem { Name = "Smoke Grey", Hex = "#738276" },
            new GlassColorItem { Name = "Ash Grey", Hex = "#B2BEB5" },
            new GlassColorItem { Name = "Slate Grey", Hex = "#708090" },
            new GlassColorItem { Name = "Pearl Grey", Hex = "#C0C0C0" },
            new GlassColorItem { Name = "Gunmetal Grey", Hex = "#2C3539" },
            new GlassColorItem { Name = "Graphite", Hex = "#383838" },
            new GlassColorItem { Name = "Steel Grey", Hex = "#8D9BA7" },

            // ─────────────────────────────────────────────────────────────
            // BRONZE / BROWN
            // ─────────────────────────────────────────────────────────────
            new GlassColorItem { Name = "Bronze", Hex = "#8B4513" },
            new GlassColorItem { Name = "Light Bronze", Hex = "#A0522D" },
            new GlassColorItem { Name = "Medium Bronze", Hex = "#8B4513" },
            new GlassColorItem { Name = "Dark Bronze", Hex = "#5C3317" },
            new GlassColorItem { Name = "Golden Bronze", Hex = "#CD7F32" },
            new GlassColorItem { Name = "Warm Bronze", Hex = "#B87333" },
            new GlassColorItem { Name = "Cool Bronze", Hex = "#6B4423" },
            new GlassColorItem { Name = "Brown", Hex = "#A52A2A" },
            new GlassColorItem { Name = "Light Brown", Hex = "#A0522D" },
            new GlassColorItem { Name = "Dark Brown", Hex = "#3E1F0A" },
            new GlassColorItem { Name = "Chocolate", Hex = "#7B3F00" },
            new GlassColorItem { Name = "Mahogany", Hex = "#C04000" },
            new GlassColorItem { Name = "Antique Bronze", Hex = "#665D1E" },
            new GlassColorItem { Name = "Amber", Hex = "#FFBF00" },
            new GlassColorItem { Name = "Tobacco", Hex = "#704214" },

            // ─────────────────────────────────────────────────────────────
            // GREEN
            // ─────────────────────────────────────────────────────────────
            new GlassColorItem { Name = "Green", Hex = "#228B22" },
            new GlassColorItem { Name = "Light Green", Hex = "#90EE90" },
            new GlassColorItem { Name = "Dark Green", Hex = "#006400" },
            new GlassColorItem { Name = "Forest Green", Hex = "#228B22" },
            new GlassColorItem { Name = "Emerald Green", Hex = "#50C878" },
            new GlassColorItem { Name = "Jade Green", Hex = "#00A86B" },
            new GlassColorItem { Name = "Mint Green", Hex = "#98FF98" },
            new GlassColorItem { Name = "Sage Green", Hex = "#9DC183" },
            new GlassColorItem { Name = "Olive Green", Hex = "#808000" },
            new GlassColorItem { Name = "Teal Green", Hex = "#008080" },
            new GlassColorItem { Name = "Sea Green", Hex = "#2E8B57" },
            new GlassColorItem { Name = "Evergreen", Hex = "#046307" },
            new GlassColorItem { Name = "Verdant", Hex = "#339E4D" },
            new GlassColorItem { Name = "Fjord Green", Hex = "#3A7D44" },

            // ─────────────────────────────────────────────────────────────
            // BLUE
            // ─────────────────────────────────────────────────────────────
            new GlassColorItem { Name = "Blue", Hex = "#4169E1" },
            new GlassColorItem { Name = "Light Blue", Hex = "#ADD8E6" },
            new GlassColorItem { Name = "Dark Blue", Hex = "#00008B" },
            new GlassColorItem { Name = "Navy Blue", Hex = "#000080" },
            new GlassColorItem { Name = "Royal Blue", Hex = "#4169E1" },
            new GlassColorItem { Name = "Sky Blue", Hex = "#87CEEB" },
            new GlassColorItem { Name = "Ocean Blue", Hex = "#4682B4" },
            new GlassColorItem { Name = "Steel Blue", Hex = "#4682B4" },
            new GlassColorItem { Name = "Azure Blue", Hex = "#007FFF" },
            new GlassColorItem { Name = "Cobalt Blue", Hex = "#0047AB" },
            new GlassColorItem { Name = "Denim Blue", Hex = "#1560BD" },
            new GlassColorItem { Name = "Sapphire Blue", Hex = "#0F52BA" },
            new GlassColorItem { Name = "Arctic Blue", Hex = "#D6FFFC" },
            new GlassColorItem { Name = "Indigo Blue", Hex = "#4B0082" },
            new GlassColorItem { Name = "Teal Blue", Hex = "#367588" },
            new GlassColorItem { Name = "Cerulean", Hex = "#007BA7" },
            new GlassColorItem { Name = "Powder Blue", Hex = "#B0E0E6" },
            new GlassColorItem { Name = "Cadet Blue", Hex = "#5F9EA0" },
            new GlassColorItem { Name = "Prussian Blue", Hex = "#003153" },

            // ─────────────────────────────────────────────────────────────
            // SILVER / MIRROR
            // ─────────────────────────────────────────────────────────────
            new GlassColorItem { Name = "Silver", Hex = "#C0C0C0" },
            new GlassColorItem { Name = "Bright Silver", Hex = "#E8E8E8" },
            new GlassColorItem { Name = "Medium Silver", Hex = "#C0C0C0" },
            new GlassColorItem { Name = "Dark Silver", Hex = "#A9A9A9" },
            new GlassColorItem { Name = "Platinum", Hex = "#E5E4E2" },
            new GlassColorItem { Name = "Chrome", Hex = "#B5B5B5" },
            new GlassColorItem { Name = "Mirror Silver", Hex = "#D4D4D4" },

            // ─────────────────────────────────────────────────────────────
            // GOLD
            // ─────────────────────────────────────────────────────────────
            new GlassColorItem { Name = "Gold", Hex = "#FFD700" },
            new GlassColorItem { Name = "Bright Gold", Hex = "#FFE44D" },
            new GlassColorItem { Name = "Medium Gold", Hex = "#DAA520" },
            new GlassColorItem { Name = "Dark Gold", Hex = "#B8860B" },
            new GlassColorItem { Name = "Champagne", Hex = "#F7E7CE" },
            new GlassColorItem { Name = "Mirror Gold", Hex = "#E6C65C" },

            // ─────────────────────────────────────────────────────────────
            // BLACK / DARK
            // ─────────────────────────────────────────────────────────────
            new GlassColorItem { Name = "Black", Hex = "#1C1C1C" },
            new GlassColorItem { Name = "Jet Black", Hex = "#0A0A0A" },
            new GlassColorItem { Name = "Pure Black", Hex = "#000000" },
            new GlassColorItem { Name = "Dark Black", Hex = "#1A1A1A" },
            new GlassColorItem { Name = "Obsidian", Hex = "#1F1F24" },
            new GlassColorItem { Name = "Onyx", Hex = "#353839" },
            new GlassColorItem { Name = "Midnight", Hex = "#191970" },

            // ─────────────────────────────────────────────────────────────
            // WHITE / CREAM
            // ─────────────────────────────────────────────────────────────
            new GlassColorItem { Name = "White", Hex = "#FFFFFF" },
            new GlassColorItem { Name = "Pure White", Hex = "#FFFAFA" },
            new GlassColorItem { Name = "Off White", Hex = "#FFFFF0" },
            new GlassColorItem { Name = "Cream", Hex = "#FFFDD0" },
            new GlassColorItem { Name = "Ivory", Hex = "#FFFFF0" },
            new GlassColorItem { Name = "Pearl White", Hex = "#F0F0F0" },
            new GlassColorItem { Name = "Snow White", Hex = "#FFFAFA" },

                        // ─────────────────────────────────────────────────────────────
            // RED / PINK
            // ─────────────────────────────────────────────────────────────
            new GlassColorItem { Name = "Red", Hex = "#DC2626" },
            new GlassColorItem { Name = "Light Red", Hex = "#F87171" },
            new GlassColorItem { Name = "Dark Red", Hex = "#991B1B" },
            new GlassColorItem { Name = "Cherry Red", Hex = "#DE3163" },
            new GlassColorItem { Name = "Crimson", Hex = "#DC143C" },
            new GlassColorItem { Name = "Maroon", Hex = "#800000" },
            new GlassColorItem { Name = "Burgundy", Hex = "#800020" },
            new GlassColorItem { Name = "Wine", Hex = "#722F37" },
            new GlassColorItem { Name = "Ruby", Hex = "#9B111E" },
            new GlassColorItem { Name = "Scarlet", Hex = "#FF2400" },
            new GlassColorItem { Name = "Coral Red", Hex = "#F08080" },
            new GlassColorItem { Name = "Rust Red", Hex = "#B7410E" },
            new GlassColorItem { Name = "Terracotta", Hex = "#E2725B" },
            new GlassColorItem { Name = "Rose", Hex = "#FF007F" },
            new GlassColorItem { Name = "Pink", Hex = "#FFB6C1" },
            new GlassColorItem { Name = "Light Pink", Hex = "#FFB6C1" },
            new GlassColorItem { Name = "Dark Pink", Hex = "#E75480" },
            new GlassColorItem { Name = "Hot Pink", Hex = "#FF69B4" },
            new GlassColorItem { Name = "Magenta", Hex = "#FF00FF" },
            new GlassColorItem { Name = "Fuchsia", Hex = "#FF00FF" },
            new GlassColorItem { Name = "Salmon", Hex = "#FA8072" },
            new GlassColorItem { Name = "Blush", Hex = "#DE5D83" },

            // ─────────────────────────────────────────────────────────────
            // PURPLE / VIOLET
            // ─────────────────────────────────────────────────────────────
            new GlassColorItem { Name = "Purple", Hex = "#800080" },
            new GlassColorItem { Name = "Light Purple", Hex = "#9370DB" },
            new GlassColorItem { Name = "Dark Purple", Hex = "#301934" },
            new GlassColorItem { Name = "Violet", Hex = "#8B00FF" },
            new GlassColorItem { Name = "Lavender", Hex = "#E6E6FA" },
            new GlassColorItem { Name = "Lilac", Hex = "#C8A2C8" },
            new GlassColorItem { Name = "Plum", Hex = "#8E4585" },
            new GlassColorItem { Name = "Grape", Hex = "#6F2DA8" },
            new GlassColorItem { Name = "Amethyst", Hex = "#9966CC" },
            new GlassColorItem { Name = "Mauve", Hex = "#E0B0FF" },
            new GlassColorItem { Name = "Orchid", Hex = "#DA70D6" },
            new GlassColorItem { Name = "Iris", Hex = "#5A4FCF" },

            // ─────────────────────────────────────────────────────────────
            // ORANGE / YELLOW
            // ─────────────────────────────────────────────────────────────
            new GlassColorItem { Name = "Orange", Hex = "#FFA500" },
            new GlassColorItem { Name = "Light Orange", Hex = "#FFA500" },
            new GlassColorItem { Name = "Dark Orange", Hex = "#FF8C00" },
            new GlassColorItem { Name = "Burnt Orange", Hex = "#CC5500" },
            new GlassColorItem { Name = "Tangerine", Hex = "#FF9966" },
            new GlassColorItem { Name = "Peach", Hex = "#FFDAB9" },
            new GlassColorItem { Name = "Apricot", Hex = "#FBCEB1" },
            new GlassColorItem { Name = "Coral", Hex = "#FF7F50" },
            new GlassColorItem { Name = "Pumpkin", Hex = "#FF7518" },
            new GlassColorItem { Name = "Nectar", Hex = "#FFD700" },
            new GlassColorItem { Name = "Yellow", Hex = "#FFFF00" },
            new GlassColorItem { Name = "Light Yellow", Hex = "#FFFFE0" },
            new GlassColorItem { Name = "Dark Yellow", Hex = "#FFD700" },
            new GlassColorItem { Name = "Canary", Hex = "#FFEF00" },
            new GlassColorItem { Name = "Lemon", Hex = "#FFF44F" },
            new GlassColorItem { Name = "Gold Yellow", Hex = "#FFD700" },
            new GlassColorItem { Name = "Mustard", Hex = "#FFDB58" },
            new GlassColorItem { Name = "Saffron", Hex = "#F4C430" },

            // ─────────────────────────────────────────────────────────────
            // NEUTRAL / BEIGE / CREAM
            // ─────────────────────────────────────────────────────────────
            new GlassColorItem { Name = "Neutral", Hex = "#D3D3D3" },
            new GlassColorItem { Name = "Beige", Hex = "#F5F5DC" },
            new GlassColorItem { Name = "Tan", Hex = "#D2B48C" },
            new GlassColorItem { Name = "Sand", Hex = "#C2B280" },
            new GlassColorItem { Name = "Khaki", Hex = "#F0E68C" },
            new GlassColorItem { Name = "Wheat", Hex = "#F5DEB3" },
            new GlassColorItem { Name = "Oatmeal", Hex = "#D9C7A2" },
            new GlassColorItem { Name = "Natural", Hex = "#E8DCC8" },
            new GlassColorItem { Name = "Champagne", Hex = "#F7E7CE" },
            new GlassColorItem { Name = "Almond", Hex = "#EFDECD" },
            new GlassColorItem { Name = "Nude", Hex = "#E3BC9A" },
            new GlassColorItem { Name = "Blush Beige", Hex = "#D4A889" },
            new GlassColorItem { Name = "Warm Grey", Hex = "#A89F91" },
            new GlassColorItem { Name = "Cool Grey", Hex = "#9BA5B0" },

            // ─────────────────────────────────────────────────────────────
            // SPECIALTY / UNIQUE
            // ─────────────────────────────────────────────────────────────
            new GlassColorItem { Name = "Antique Silver", Hex = "#A9B8C4" },
            new GlassColorItem { Name = "Antique Gold", Hex = "#D4AF37" },
            new GlassColorItem { Name = "Bronze Mirror", Hex = "#8C7853" },
            new GlassColorItem { Name = "Rose Gold", Hex = "#B76E79" },
            new GlassColorItem { Name = "Copper", Hex = "#B87333" },
            new GlassColorItem { Name = "Brass", Hex = "#B5A642" },
            new GlassColorItem { Name = "Titanium", Hex = "#878681" },
            new GlassColorItem { Name = "Gunmetal", Hex = "#2C3539" },
            new GlassColorItem { Name = "Pewter", Hex = "#8E9196" },
            new GlassColorItem { Name = "Satin Silver", Hex = "#C8C8C8" },
            new GlassColorItem { Name = "Satin Gold", Hex = "#D4B76A" },
            new GlassColorItem { Name = "Textured", Hex = "#A0A0A0" },
            new GlassColorItem { Name = "Metallic", Hex = "#B5B5B5" },
            new GlassColorItem { Name = "Pearl", Hex = "#EAE3D5" },
            new GlassColorItem { Name = "Ice", Hex = "#E0F0F0" },
            new GlassColorItem { Name = "Frost", Hex = "#E8F4F8" },
            new GlassColorItem { Name = "Mirror", Hex = "#D4D4D4" },
            new GlassColorItem { Name = "One-Way Mirror", Hex = "#B8CCE4" },
            new GlassColorItem { Name = "Smoked", Hex = "#696969" },
            new GlassColorItem { Name = "Tinted", Hex = "#B8B8B8" },

            // ─────────────────────────────────────────────────────────────
            // OPAQUE / PAINTED LACOBEL COLORS
            // ─────────────────────────────────────────────────────────────
            new GlassColorItem { Name = "Lacobel White", Hex = "#FFFFFF" },
            new GlassColorItem { Name = "Lacobel Black", Hex = "#1C1C1C" },
            new GlassColorItem { Name = "Lacobel Grey", Hex = "#808080" },
            new GlassColorItem { Name = "Lacobel Cream", Hex = "#FFFDD0" },
            new GlassColorItem { Name = "Lacobel Red", Hex = "#DC2626" },
            new GlassColorItem { Name = "Lacobel Blue", Hex = "#2563EB" },
            new GlassColorItem { Name = "Lacobel Green", Hex = "#16A34A" },
            new GlassColorItem { Name = "Lacobel Brown", Hex = "#78350F" },
            new GlassColorItem { Name = "Lacobel Yellow", Hex = "#FACC15" },
            new GlassColorItem { Name = "Lacobel Orange", Hex = "#F97316" },
            new GlassColorItem { Name = "Lacobel Pink", Hex = "#F472B6" },
            new GlassColorItem { Name = "Lacobel Purple", Hex = "#9333EA" },
            new GlassColorItem { Name = "Lacobel Anthracite", Hex = "#3F3F46" },
            new GlassColorItem { Name = "Lacobel Graphite", Hex = "#4A4A4A" },

            // ─────────────────────────────────────────────────────────────
            // AUTO / AUTOMOTIVE COLORS
            // ─────────────────────────────────────────────────────────────
            new GlassColorItem { Name = "Privacy Black", Hex = "#1A1A1A" },
            new GlassColorItem { Name = "Privacy Grey", Hex = "#4A4A4A" },
            new GlassColorItem { Name = "Sunstrip", Hex = "#333333" },
            new GlassColorItem { Name = "Solar Green", Hex = "#2E5A4C" },
            new GlassColorItem { Name = "Limo Tint", Hex = "#0A0A0A" },
            new GlassColorItem { Name = "UV Filter Clear", Hex = "#F0F8F0" },
            new GlassColorItem { Name = "IR Reject", Hex = "#E0F0E0" },
            new GlassColorItem { Name = "Ceramic Tint", Hex = "#2C3E50" }
        };

        // ═══════════════════════════════════════════════════════════════
        // SUPPLIERS
        // ═══════════════════════════════════════════════════════════════
        public static List<string> Suppliers = new List<string>
        {
            // Major International Manufacturers
            "AGC (A Glaverbel Company)", "Saint-Gobain Glass (SGG)",
            "Guardian Glass", "Pilkington", "NSG Group",
            "Şişecam", "Trakya Cam", "Sisecam",
            
            // Regional Manufacturers
            "Taiwan Glass", "Xinyi Glass", "CSG Holding",
            "Yaohua Glass", "Jiangsu Tianda", "Bengal Glass",
            
            // Middle East / GCC
            "Alfa Glass", "Saudi Glass", "Gulf Glass",
            "National Glass", "Emirates Glass", "Dubai Glass",
            "Abu Dhabi Glass", "Qatar Glass", "Muscat Glass",
            "Bahrain Glass", "Kuwait Glass",
            
            // South Asia
            "Gujarat Glass", "Modi Glass", "Saint-Gobain India",
            "Asahi India Glass", "CGC Glass", "Borosil Glass",
            "Piramal Glass", "HNG Float Glass", "Saint-Gobain Bangladesh",
            
            // Africa
            "PGI Glass", "Guardian South Africa", "Modisa Glass",
            "Nigerian Glass", "Egyptian Glass", "K年来 Glass",
            
            // Southeast Asia
            "Viglacera", "Vinh Hoan Glass", "Thai Glass Industry",
            "Masan Resources", "Asia Glass", "Malay Glass",
            
            // China
            "CSG", "Yaohua", "Xinyi", "Jinjing", "Shandong Glass",
            "Beijing Xinying", "Heibei Glass", "Sino Glass",
            "K.clear", "Interpane China",
            
            // Europe
            "Saint-Gobain Glass Europe", "AGC Europe", "Guardian Europe",
            "NSG Pilkington Europe", "Şişecam Europe",
            
            // USA / North America
            "Guardian Industries", "Vitro", "Saint-Gobain North America",
            "Pilkington North America", "NSG North America",
            
            // Distributors
            "Global Glass Distributors", "Glass World", "Glass Masters",
            "Crystal Glass Co.", "Prime Glass", "Elite Glass",
            "Metro Glass", "City Glass", "United Glass",
            "Premium Glass Co.", "Royal Glass", "Alpha Glass LLC",
            
            // Others
            "Local Supplier", "Direct Import", "OEM",
            "Wholesale", "Retail Supplier", "Custom Manufacturer",
            "Other"
        };

        // ═══════════════════════════════════════════════════════════════
        // WASTAGE OPTIONS (%)
        // ═══════════════════════════════════════════════════════════════
        public static List<string> WastageOptions { get; } = new List<string>
        {
            "0", "5", "10", "15", "20", "25", "30", "35", "40", "45", "50"
        };

        // ═══════════════════════════════════════════════════════════════
        // PROFIT MARGIN OPTIONS (%)
        // ═══════════════════════════════════════════════════════════════
        public static List<string> ProfitMarginOptions { get; } = new List<string>
        {
            "0%", "5%", "10%", "15%", "20%", "25%", "30%", "35%", "40%", "45%", "50%"
        };

        // ═══════════════════════════════════════════════════════════════
        // UNIT OPTIONS
        // ═══════════════════════════════════════════════════════════════
        public static List<string> UnitOptions { get; } = new List<string>
        {
            "AED", "USD", "EUR", "GBP", "SAR", "KWD", "QAR", "AED/sqm", "USD/sqm"
        };

        // ═══════════════════════════════════════════════════════════════
        // SHEET STATUS
        // ═══════════════════════════════════════════════════════════════
        public static List<string> StatusOptions { get; } = new List<string>
        {
            "Active", "Inactive", "Discontinued", "Out of Stock", "On Order"
        };

        // ═══════════════════════════════════════════════════════════════
        // PAYMENT METHODS
        // ═══════════════════════════════════════════════════════════════
        public static List<string> PaymentMethods { get; } = new List<string>
        {
            "Cash", "Bank Transfer", "Cheque", "Credit Card",
            "Online Payment", "Credit / 30 Days", "Credit / 60 Days"
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
        public string Supplier { get; set; }
        public DateTime PurchasedOn { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        public decimal TotalAmt => Quantity * UnitPrice;
        public string DisplayDate => PurchasedOn.ToString("dd/MM/yyyy HH:mm");
    }

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

    public class GlassColorItem
    {
        public string Name { get; set; }
        public string Hex { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // OPTIONAL: SGU CALCULATOR DATA CLASS
    // ═══════════════════════════════════════════════════════════════
    public static class SguCalculatorData
    {
        public static List<string> Categories => Sheet.Categories;
        public static string[] Thicknesses => Sheet.Thicknesses;
        public static List<GlassColorItem> Colors => Sheet.ColorItems;
        public static List<string> WastageOptions => Sheet.WastageOptions;
        public static List<string> ProfitMarginOptions => Sheet.ProfitMarginOptions;
    }
}