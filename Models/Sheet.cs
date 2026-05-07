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
        public List<SheetPurchase> PurchaseHistory { get; set; } = new();
        public List<SheetUsage> UseHistory { get; set; } = new();

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
                return new SolidColorBrush(Colors.LightGray);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // GLASS CATEGORIES (ALL SERIES - 500+)
        // ═══════════════════════════════════════════════════════════════
        public static List<string> Categories = new List<string>
        {
            // ─────────────────────────────────────────────────────────────
            // CLEAR / FLOAT GLASS
            // ─────────────────────────────────────────────────────────────
            "Clear Float", "Clear Sheet", "Clear Plate", "Clear Annealed",
            "Ultra Clear", "Ultra Clear Float", "Crystal Clear", "Optiwhite",
            "Starphire Ultra Clear", "Ultra White", "Low Iron Clear", "Starphire",
            "Diamant", "Planiclear", "SGG Ultra Clear", "Mister Prismatic",
            "Optifloat Clear", "SGG Planilux", "Guardian Ultra Clear",
            "Clear View", "Clear Premium", "Clear Elite", "Clear Pro",
            "Clear Plus", "Clear Max", "Clear Ultra",

            // ─────────────────────────────────────────────────────────────
            // TINTED GLASS - BLUE SERIES
            // ─────────────────────────────────────────────────────────────
            "Blue Float", "Azure Blue", "Ocean Blue", "Sky Blue", "Royal Blue",
            "Navy Blue", "Cobalt Blue", "Steel Blue", "Denim Blue", "Sapphire Blue",
            "Indigo Blue", "HD Blue", "PNA Blue", "Ramly Blue", "Sunlux Blue",
            "Reflite Blue", "Reflux Blue", "Tinted Blue", "Reflective Blue",
            "Mirror Blue", "Stopsol Classic Blue", "Stopsol Superburn Blue",
            "Stopray Classic Blue", "Stopray Blue", "Chromafloat Blue",
            "Sunergy Blue", "Sunergy Plus Blue", "SunGuard Blue", "Guardian Blue",
            "SGG Tinted Blue", "SGG Reflective Blue", "Pilkington Blue",
            "Pilkington Arctic Blue", "Şişecam Blue", "Sisecam Blue", "Trakya Blue",
            "AGC Tinted Blue", "PGI Tinted Blue", "PGI Reflective Blue",
            "Taiwan Tinted Blue", "Taiwan Reflective Blue", "Antelio Blue",
            "Spectran Blue", "Lacobel Blue", "Azur Blue", "Azurlite Blue",
            "Cool Blue", "Sun Blue", "Arctic Blue", "Arctic Ice Blue",
            "Lacobel Cobalt", "Lacobel Navy", "Blue Tinted", "Blue Reflective",
            "Blue Mirror", "Blue Privacy", "Blue Low-E", "Blue Solar Control",
            "Blue Tempered", "Blue Laminated", "Blue Insulating",
            "Blue 120", "Blue S120", "Blue 140", "Blue S160",
            "Blue S200", "Blue S220", "Blue S240",

            // ─────────────────────────────────────────────────────────────
            // TINTED GLASS - GREEN SERIES
            // ─────────────────────────────────────────────────────────────
            "Green Float", "Forest Green", "Emerald Green", "Jade Green",
            "Mint Green", "Sage Green", "Olive Green", "Teal Green", "Sea Green",
            "Dark Green", "Light Green", "HD Green", "PNA Green", "Ramly Green",
            "Sunlux Green", "Reflite Green", "Reflux Green", "Belgium Green",
            "Tinted Green", "Reflective Green", "Mirror Green", "Planibel Green",
            "Stopsol Classic Green", "Stopsol Superburn Green",
            "Stopray Classic Green", "Stopray Green", "Chromafloat Green",
            "Sunergy Green", "SunGuard Green", "Guardian Green",
            "SGG Tinted Green", "SGG Reflective Green", "Pilkington Green",
            "Şişecam Stopray Green", "Şişecam Tinted Green",
            "Trakya Stopray Green", "Trakya Tinted Green", "AGC Tinted Green",
            "PGI Tinted Green", "PGI Reflective Green", "Taiwan Tinted Green",
            "Antelio Green", "Lacobel Green", "Symmetry Green", "Evergreen",
            "Cool Green", "Nature Green", "Verdant Green", "Arctic Green",
            "Lacobel Forest", "Lacobel Emerald", "Green Tinted", "Green Reflective",
            "Green Mirror", "Green Privacy", "Green Low-E", "Green Solar Control",
            "Green Tempered", "Green Laminated", "Green Insulating",
            "Green 120", "Green S120", "Green 140", "Green S160",
            "Green S200", "Green S220", "Green S240",

            // ─────────────────────────────────────────────────────────────
            // TINTED GLASS - BRONZE SERIES
            // ─────────────────────────────────────────────────────────────
            "Bronze Float", "Dark Bronze", "Light Bronze", "Medium Bronze",
            "Golden Bronze", "Warm Bronze", "Cool Bronze", "HD Bronze",
            "PNA Bronze", "Ramly Bronze", "Sunlux Bronze", "Reflite Bronze",
            "Reflux Bronze", "Belgium Bronze", "Tinted Bronze", "Reflective Bronze",
            "Mirror Bronze", "Planibel Bronze", "Stopsol Classic Bronze",
            "Stopsol Superburn Bronze", "Stopray Classic Bronze", "Stopray Bronze",
            "Sunergy Bronze", "SunGuard Bronze", "Şişecam Stopray Bronze",
            "Şişecam Tinted Bronze", "Şişecam Reflective Bronze",
            "Trakya Stopray Bronze", "Trakya Tinted Bronze",
            "SGG Tinted Bronze", "SGG Reflective Bronze", "Miralite Bronze",
            "Lacobel Brown", "Lacobel Chocolate", "Lacobel Camel",
            "Symmetry Bronze", "Estate Bronze", "Cool Bronze", "Antique Bronze",
            "Arctic Bronze", "Bronze Tinted", "Bronze Reflective", "Bronze Mirror",
            "Bronze Privacy", "Bronze Low-E", "Bronze Solar Control",
            "Bronze Tempered", "Bronze Laminated", "Bronze Insulating",
            "Bronze 120", "Bronze S120", "Bronze 140", "Bronze S160",
            "Bronze S200", "Bronze S220", "Bronze S240",

            // ─────────────────────────────────────────────────────────────
            // TINTED GLASS - GREY SERIES
            // ─────────────────────────────────────────────────────────────
            "Grey Float", "Dark Grey", "Light Grey", "Medium Grey",
            "Charcoal Grey", "Smoke Grey", "Ash Grey", "Slate Grey",
            "Pearl Grey", "Silver Grey", "Gunmetal Grey", "HD Grey", "PNA Grey",
            "Ramly Grey", "Sunlux Grey", "Reflite Grey", "Reflux Grey",
            "Belgium Grey", "Tinted Grey", "Reflective Grey", "Mirror Grey",
            "Planibel Grey", "Stopsol Classic Grey", "Stopsol Superburn Grey",
            "Stopray Classic Grey", "Stopray Grey", "Sunergy Grey",
            "SunGuard Grey", "Şişecam Stopray Grey", "Şişecam Tinted Grey",
            "Trakya Stopray Grey", "Trakya Tinted Grey", "SGG Tinted Grey",
            "SGG Reflective Grey", "Lacobel Grey", "Lacobel Anthracite",
            "Lacobel Graphite", "Symmetry Grey", "Pewter Grey", "Cool Grey",
            "Storm Grey", "Granite Grey", "Arctic Grey", "Grey Tinted",
            "Grey Reflective", "Grey Mirror", "Grey Privacy", "Grey Low-E",
            "Grey Solar Control", "Grey Tempered", "Grey Laminated",
            "Grey Insulating", "Grey 120", "Grey S120", "Grey 140", "Grey S160",
            "Grey S200", "Grey S220", "Grey S240",

            // ─────────────────────────────────────────────────────────────
            // REFLECTIVE / MIRROR - SILVER SERIES
            // ─────────────────────────────────────────────────────────────
            "Reflective Silver", "Mirror Silver", "Silver Mirror", "Silver Reflective",
            "Hard Reflective Silver", "PNA Reflective Silver", "Sunlux Silver",
            "Reflite Silver", "Reflux Silver", "Stopsol Silver Lite",
            "Stopsol Silver Dark", "Stopsol Superburn Silver", "Stopray Silver",
            "Stopray Vision Silver", "Chromafloat Silver", "Sunergy Silver",
            "Guardian Reflective Silver", "SunGuard Solar Silver",
            "Şişecam Reflective Silver", "SGG Reflective Silver",
            "PGI Reflective Silver", "Taiwan Reflective Silver", "Antelio Silver",
            "Miralite Silver", "Spectran Silver", "Cool Silver", "Platinum Silver",
            "HD Reflective Silver", "Ramly Reflective Silver", "Arctic Silver",
            "Lacobel Pearl", "Silver Tinted", "Silver Mirror", "Silver Privacy",
            "Silver Low-E", "Silver Solar Control", "Silver Tempered",
            "Silver Laminated", "Silver Insulating", "Silver 120", "Silver S120",
            "Silver 140", "Silver S160", "Silver S200", "Silver S220", "Silver S240",

            // ─────────────────────────────────────────────────────────────
            // REFLECTIVE / MIRROR - GOLD SERIES
            // ─────────────────────────────────────────────────────────────
            "Reflective Gold", "Mirror Gold", "Gold Mirror", "Gold Reflective",
            "Hard Reflective Gold", "PNA Reflective Gold", "Sunlux Gold",
            "Reflite Gold", "Reflux Gold", "Stopray Gold", "Stopray Vision Gold",
            "Chromafloat Gold", "Guardian Reflective Gold", "SunGuard Solar Gold",
            "Şişecam Reflective Gold", "SGG Reflective Gold", "Antelio Gold",
            "Miralite Gold", "Cool Gold", "Champagne Gold", "HD Reflective Gold",
            "Ramly Reflective Gold", "Arctic Gold", "Lacobel Gold",
            "Gold Tinted", "Gold Mirror", "Gold Privacy", "Gold Low-E",
            "Gold Solar Control", "Gold Tempered", "Gold Laminated",
            "Gold Insulating", "Gold 120", "Gold S120", "Gold 140", "Gold S160",
            "Gold S200", "Gold S220", "Gold S240",

            // ─────────────────────────────────────────────────────────────
            // BLACK / DARK SERIES
            // ─────────────────────────────────────────────────────────────
            "Black Float", "Dark Black", "Jet Black", "Pure Black", "HD Black",
            "PNA Black", "Tinted Black", "Lacobel Black", "Lacobel Extra Black",
            "Lacobel Jet Black", "Dark Grey Float", "Graphite", "Obsidian", "Onyx",
            "Lacobel Anthracite", "Lacobel Graphite", "Midnight", "Arctic Black",
            "Black Reflective", "Black Mirror", "Black Privacy", "Black Low-E",
            "Black Solar Control", "Black Tempered", "Black Laminated",
            "Black Insulating", "Black 120", "Black S120", "Black 140", "Black S160",

            // ─────────────────────────────────────────────────────────────
            // PRIVACY / OBSCURE GLASS
            // ─────────────────────────────────────────────────────────────
            "Frosted Clear", "Frosted", "Acid Etched", "Satin Glass",
            "Matelux Clear", "Matelux Bronze", "Matelux Grey", "Matelux Green",
            "Decormatt", "Mastercote", "Satinato", "Masterglass",
            "Patterned Glass", "Rolled Glass", "Obscure Glass", "Rain Glass",
            "Reeded Glass", "Fluted Glass", "Bubble Glass", "Coral Glass",
            "Euro Greek", "Baroque", "Chinchilla", "M Wire", "Laminated Frosted",
            "Sandblasted", "SunGuard Privacy", "Frost Ice", "Frost Natural",
            "Frost Silk", "Reed Decorative", "Glue Chip", "Woven Pattern",
            "Custom Pattern", "Special Pattern",

            // ─────────────────────────────────────────────────────────────
            // LOW-E GLASS
            // ─────────────────────────────────────────────────────────────
            "Low-E Clear", "Low-E Neutral", "Low-E Silver", "Low-E Tempered",
            "Low-E Laminated", "iPlus 1.0", "iPlus 1.1", "iPlus 1.2", "iPlus 1.3",
            "iPlus 2.0", "iPlus 3.0", "iPlus 4.0", "Comfort Plus",
            "Energy Advantage", "Energy Smart", "SGG Planitherm One",
            "SGG Planitherm Total", "SGG Planitherm Ultra N", "SGG Planitherm Ultra",
            "SunGuard Neutral 63", "SunGuard Neutral 70", "SunGuard Neutral 78",
            "Solarban 60", "Solarban 70", "Solarban 70XL", "Solarban 90",
            "Solarban 100", "Solarban 110", "Solarban N", "Solarban Z", "Solarban U",
            "Guardian Low-E", "Guardian Ultra Low-E", "Guardian ClimaGuard",
            "Guardian SunGuard", "Guardian Ultra", "Guardian Ultra Plus",
            "Pilkington K Glass", "Pilkington Low-E", "Pilkington Optifloat Low-E",
            "Pilkington Activ", "Şişecam Low-E", "Şişecam Temper Low-E",
            "Planibel Top N+", "Planibel A", "Planibel G", "Thermafoil",
            "Thermoplus", "Thermobel", "Thermoseal", "Thermobar",
            "AGC Stopray", "AGC Planibel", "AGC Clear", "NSG Pyrolytic",
            "NSG Low-E", "NSG Solar", "Energy Plus", "Energy Saver", "Energy Control",

            // ─────────────────────────────────────────────────────────────
            // TEMPERED / TOUGHENED GLASS
            // ─────────────────────────────────────────────────────────────
            "Tempered Clear", "Tempered Tinted", "Tempered Low-E", "Tempered Bronze",
            "Tempered Grey", "Tempered Green", "Tempered Blue", "Tempered Reflective",
            "Toughened Clear", "Toughened Tinted", "Toughened Low-E",
            "Fully Tempered", "Heat Strengthened", "Şişecam Tempered", "AGC Tempered",
            "Pilkington Tempered", "Guardian Tempered", "SGG Temperit",
            "Temp Clear", "Temp Tinted", "Temp Reflective", "Temp Low-E",
            "Temper Clear", "Temper Tinted", "Temper Reflective", "Temper Low-E",
            "Tough Clear", "Tough Tinted", "Tough Low-E",

            // ─────────────────────────────────────────────────────────────
            // LAMINATED GLASS
            // ─────────────────────────────────────────────────────────────
            "Laminated Clear", "Laminated Tinted", "Laminated Low-E", "Laminated Bronze",
            "Laminated Grey", "Laminated Green", "Laminated Blue", "Laminated Reflective",
            "Laminated Acoustic", "Laminated Security", "Laminated UV Filter",
            "Laminated PVB", "SGG Stadip", "SGG Stadip Silence", "Pilkington Laminate",
            "Guardian Laminated", "Şişecam Laminated", "AGC Laminated",
            "Multilaminate", "Bi-Laminate", "Tri-Laminate", "Lami Clear", "Lami Tinted",
            "Lami Reflective", "Lami Low-E", "Lami Solar", "Lami Acoustic", "Lami UV",
            "Stadip Clear", "Stadip Silence", "Stadip Plus",

                        // ─────────────────────────────────────────────────────────────
            // FIRE RATED GLASS
            // ─────────────────────────────────────────────────────────────
            "Pyrobel Clear", "Pyrobel Bronze", "Pyrostop 30", "Pyrostop 45",
            "Pyrostop 60", "Pyrostop 90", "Pyrostop 120", "Pyrodur", "Pyroguard",
            "Pyroglass", "Firelite", "Fire Plus", "Contraflan", "Promat",
            "Fire Clear", "Fire Rated Clear", "Fire Rated Tinted", "Fire Plus",
            "Fire Pro", "Fire Max", "Fire Guard", "Fire Shield", "Fire Protect",
            "Safety Clear", "Safety Tinted", "Safety Reflective", "Safety Plus",

            // ─────────────────────────────────────────────────────────────
            // AUTO GLASS
            // ─────────────────────────────────────────────────────────────
            "Automotive Clear", "Automotive Tinted", "Automotive Tempered",
            "Automotive Laminated", "Windscreen", "Rear Lite", "Side Lite",
            "Sunroof Glass", "Heated Glass", "Acoustic Glass", "UV Cut Glass",
            "Privacy Glass", "IR Rejection", "Ceramic Frit", "Enameled Glass",
            "Auto Clear", "Auto Tinted", "Auto Privacy", "Auto Solar", "Auto UV",
            "Auto IR", "Auto Plus", "Auto Pro", "Auto Max", "Auto Premium",
            "Auto Elite", "Auto Select", "Auto Ceramic", "Auto Enamel", "Auto Frit",
            "Windscreen Clear", "Windscreen Tinted", "Side Clear", "Side Tinted",
            "Side Privacy", "Rear Clear", "Rear Tinted", "Rear Privacy",
            "Sunroof Clear", "Sunroof Tinted", "Sunroof Privacy",
            "Privacy Black", "Privacy Grey", "Sunstrip", "Solar Green",
            "Limo Tint", "UV Filter Clear", "IR Reject", "Ceramic Tint",

            // ─────────────────────────────────────────────────────────────
            // INSULATED / DOUBLE GLAZING
            // ─────────────────────────────────────────────────────────────
            "IGU Clear", "IGU Tinted", "IGU Low-E", "Double Glazing Unit",
            "Double Glazed", "Triple Glazing Unit", "Triple Glazed",
            "SGG Climalit", "SGG Climalit Plus", "Pilkington Insulight",
            "Thermoseal", "Thermobar", "Spacer Bar", "Warm Edge Spacer",
            "Desiccant Filled", "Insulating Clear", "Insulating Tinted",
            "Insulating Low-E", "DGU Clear", "DGU Tinted", "DGU Low-E",
            "TGU Clear", "TGU Tinted", "TGU Low-E",

            // ─────────────────────────────────────────────────────────────
            // SOLAR CONTROL / ENERGY SAVING
            // ─────────────────────────────────────────────────────────────
            "Solar Control Clear", "Solar Control Tinted", "Solar Control Reflective",
            "Energy Saving Clear", "Energy Saving Low-E", "SunCool", "SunStop",
            "Reflectasol", "Reflexasol", "EcoClear", "EnviroShield",
            "Solar 50", "Solar 60", "Solar 70", "Solar 80", "Solar 90", "Solar 100",
            "Solar Control Plus", "Solar Control Pro", "Solar Control Max",
            "Solar Shield", "Solar Guard", "Solar Defender", "Solar Pro",
            "Solar Ultra", "Solar Max", "Solar Select", "Solar Premium", "Solar Elite",
            "Cool Lite 40", "Cool Lite 50", "Cool Lite 60", "Cool Lite 70",
            "Cool Lite 80", "Cool Lite 90", "Cool Lite 100", "Cool Lite Clear",
            "Cool Lite Tinted", "Cool Lite Bronze", "Cool Lite Blue", "Cool Lite Green",
            "Cool Lite Grey", "Cool Lite Pro", "Cool Lite Plus", "Cool Lite Ultra",
            "Cool Glass 50", "Cool Glass 60", "Cool Glass 70", "Cool Glass 80",
            "Cool Glass 90", "Cool Glass 100", "Cool Glass Clear", "Cool Glass Solar",
            "Cool Glass Energy", "Cool Glass Plus", "Cool Glass Pro", "Cool Glass Max",

            // ─────────────────────────────────────────────────────────────
            // LACOBEL SERIES
            // ─────────────────────────────────────────────────────────────
            "Lacobel White", "Lacobel Pure White", "Lacobel Super White",
            "Lacobel Extra White", "Lacobel Pearl", "Lacobel Black", "Lacobel Extra Black",
            "Lacobel Jet Black", "Lacobel Grey", "Lacobel Anthracite", "Lacobel Graphite",
            "Lacobel Red", "Lacobel Burgundy", "Lacobel Cherry", "Lacobel Blue",
            "Lacobel Cobalt", "Lacobel Navy", "Lacobel Green", "Lacobel Forest",
            "Lacobel Emerald", "Lacobel Brown", "Lacobel Chocolate", "Lacobel Camel",
            "Lacobel Yellow", "Lacobel Orange", "Lacobel Coral", "Lacobel Pink",
            "Lacobel Purple", "Lacobel Violet", "Lacobel Easy Clean", "Lacobel T",
            "Lacobel Cream", "Lacobel Mint", "Lacobel Lavender", "Lacobel Peach",
            "Lacobel Sky", "Lacobel Ocean", "Lacobel Moss", "Lacobel Olive",
            "Lacobel Terracotta", "Lacobel Rust", "Lacobel Maroon", "Lacobel Wine",
            "Lacobel Magenta", "Lacobel Fuchsia", "Lacobel Lilac", "Lacobel Orchid",

            // ─────────────────────────────────────────────────────────────
            // MIRALITE SERIES
            // ─────────────────────────────────────────────────────────────
            "Miralite Clear", "Miralite Silver", "Miralite Gold", "Miralite Bronze",
            "Miralite Grey", "Miralite Blue", "Miralite Black", "Miralite Copper",
            "Miralite Rose", "Miralite Green", "Miralite Champagne",

            // ─────────────────────────────────────────────────────────────
            // ANTELIO SERIES
            // ─────────────────────────────────────────────────────────────
            "Antelio Silver", "Antelio Gold", "Antelio Bronze", "Antelio Blue",
            "Antelio Grey", "Antelio Green", "Antelio Clear", "Antelio Copper",
            "Antelio Rose", "Antelio Champagne",

            // ─────────────────────────────────────────────────────────────
            // SPECTRAN SERIES
            // ─────────────────────────────────────────────────────────────
            "Spectran Clear", "Spectran Silver", "Spectran Blue", "Spectran Grey",
            "Spectran Bronze", "Spectran Green", "Spectran Gold", "Spectran Black",

            // ─────────────────────────────────────────────────────────────
            // CHROMAFLOAT SERIES
            // ─────────────────────────────────────────────────────────────
            "Chromafloat Blue", "Chromafloat Green", "Chromafloat Bronze",
            "Chromafloat Grey", "Chromafloat Silver", "Chromafloat Gold",
            "Chromafloat Clear", "Chromafloat Black", "Chromafloat Red",

            // ─────────────────────────────────────────────────────────────
            // SUNERGY SERIES
            // ─────────────────────────────────────────────────────────────
            "Sunergy Clear", "Sunergy Blue", "Sunergy Green", "Sunergy Bronze",
            "Sunergy Grey", "Sunergy Plus", "Sunergy Silver", "Sunergy Gold",

            // ─────────────────────────────────────────────────────────────
            // PLANIBEL SERIES
            // ─────────────────────────────────────────────────────────────
            "Planibel Clear", "Planibel A", "Planibel G", "Planibel Top N",
            "Planibel Top N+", "Planibel Green", "Planibel Bronze", "Planibel Grey",
            "Planibel Blue", "Planibel Silver", "Planibel Gold",

            // ─────────────────────────────────────────────────────────────
            // SUNGUARD SERIES
            // ─────────────────────────────────────────────────────────────
            "SunGuard Platinum", "SunGuard Solar", "SunGuard Super", "SunGuard Premium",
            "SunGuard Natural", "SunGuard Neutral", "SunGuard Blue", "SunGuard Grey",
            "SunGuard Bronze", "SunGuard Silver", "SunGuard Gold", "SunGuard Clear",
            "SunGuard 70", "SunGuard 65", "SunGuard 60", "SunGuard 50", "SunGuard 40",
            "SunGuard HS", "SunGuard SN", "SunGuard ES", "SunGuard HP",

            // ─────────────────────────────────────────────────────────────
            // STOPSOL / STOPRAY SERIES
            // ─────────────────────────────────────────────────────────────
            "Stopsol Clear", "Stopsol Silver Lite", "Stopsol Silver Dark",
            "Stopsol Classic", "Stopsol Superburn", "Stopsol Blue", "Stopsol Grey",
            "Stopsol Bronze", "Stopsol Green", "Stopsol Gold", "Stopsol Black",
            "Stopsol Reflective", "Stopsol Solar", "Stopsol Energy",
            "Stopray Vision", "Stopray Classic", "Stopray Select", "Stopray Silver",
            "Stopray Gold", "Stopray Bronze", "Stopray Blue", "Stopray Grey",
            "Stopray Green", "Stopray Clear", "Stopray Reflective",

            // ─────────────────────────────────────────────────────────────
            // CAVS SERIES
            // ─────────────────────────────────────────────────────────────
            "Cavs 120", "Cavs S120", "Cavs 140", "Cavs S160", "Cavs S180",
            "Cavs S200", "Cavs S220", "Cavs S240", "Cavs S280", "Cavs S320",
            "Cavs S360", "Cavs S400", "Cavs S500", "Cavs S600",
            "Cavs 150", "Cavs 200", "Cavs 250", "Cavs 300",
            "Cavs Classic", "Cavs Premium", "Cavs Ultra", "Cavs Plus", "Cavs Pro",
            "Cavs Max", "Cavs Reflective", "Cavs Solar", "Cavs Energy",
            "Cavs 58", "Cavs 65", "Cavs 70", "Cavs 75", "Cavs 80",
            "Cavs Clear", "Cavs Tinted", "Cavs Blue", "Cavs Green", "Cavs Bronze",
            "Cavs Grey", "Cavs Silver", "Cavs Gold", "Cavs Black",

            // ─────────────────────────────────────────────────────────────
            // NOVA SERIES
            // ─────────────────────────────────────────────────────────────
            "Nova 10", "Nova 20", "Nova 30", "Nova 40", "Nova 50",
            "Nova 58", "Nova 60", "Nova 65", "Nova 70", "Nova 75", "Nova 80",
            "Nova 85", "Nova 90", "Nova 95", "Nova 100",
            "Nova Pro", "Nova Plus", "Nova Max", "Nova Ultra", "Nova Classic",
            "Nova Premium", "Nova Elite", "Nova Solar", "Nova Energy",
            "Nova Comfort", "Nova Reflective", "Nova Clear", "Nova Tinted",
            "Nova Grey", "Nova Bronze", "Nova Green", "Nova Blue", "Nova Black",
            "Nova Silver", "Nova Gold", "Nova AS", "Nova AF", "Nova AR", "Nova AZ",
            "Nova 58S", "Nova 65S", "Nova 70S", "Nova 75S", "Nova 80S",

            // ─────────────────────────────────────────────────────────────
            // PNA SERIES
            // ─────────────────────────────────────────────────────────────
            "PNA 120", "PNA S120", "PNA 140", "PNA S160", "PNA S180",
            "PNA S200", "PNA S220", "PNA S240", "PNA S280", "PNA S320",
            "PNA S360", "PNA S400", "PNA S500", "PNA S600",
            "PNA 150", "PNA 200", "PNA 250", "PNA 300",
            "PNA Classic", "PNA Premium", "PNA Ultra", "PNA Plus", "PNA Pro",
            "PNA Max", "PNA Reflective", "PNA Solar", "PNA Energy",
            "PNA 58", "PNA 65", "PNA 70", "PNA 75", "PNA 80",
            "PNA Clear", "PNA Tinted", "PNA Blue", "PNA Green", "PNA Bronze",
            "PNA Grey", "PNA Silver", "PNA Gold", "PNA Black",
            "PNA Low-E", "PNA Privacy", "PNA Frosted", "PNA Satin",

            // ─────────────────────────────────────────────────────────────
            // REFLITE SERIES
            // ─────────────────────────────────────────────────────────────
            "Reflite 120", "Reflite S120", "Reflite 140", "Reflite S160", "Reflite S180",
            "Reflite S200", "Reflite S220", "Reflite S240", "Reflite S280", "Reflite S320",
            "Reflite S360", "Reflite S400", "Reflite S500", "Reflite S600",
            "Reflite 150", "Reflite 200", "Reflite 250", "Reflite 300",
            "Reflite Classic", "Reflite Premium", "Reflite Ultra", "Reflite Plus",
            "Reflite Pro", "Reflite Max", "Reflite Reflective", "Reflite Solar",
            "Reflite Energy", "Reflite 58", "Reflite 65", "Reflite 70", "Reflite 75",
            "Reflite 80", "Reflite Clear", "Reflite Tinted", "Reflite Blue",
            "Reflite Green", "Reflite Bronze", "Reflite Grey", "Reflite Silver",
            "Reflite Gold", "Reflite Black", "Reflite Low-E", "Reflite Privacy",
            "Reflite Frosted", "Reflite Satin", "Reflite Arctic Blue",

            // ─────────────────────────────────────────────────────────────
            // REFLUX SERIES
            // ─────────────────────────────────────────────────────────────
            "Reflux 120", "Reflux S120", "Reflux 140", "Reflux S160", "Reflux S180",
            "Reflux S200", "Reflux S220", "Reflux S240", "Reflux S280", "Reflux S320",
            "Reflux S360", "Reflux S400", "Reflux S500", "Reflux S600",
            "Reflux 150", "Reflux 200", "Reflux 250", "Reflux 300",
            "Reflux Classic", "Reflux Premium", "Reflux Ultra", "Reflux Plus",
            "Reflux Pro", "Reflux Max", "Reflux Reflective", "Reflux Solar",
            "Reflux Energy", "Reflux 58", "Reflux 65", "Reflux 70", "Reflux 75",
            "Reflux 80", "Reflux Clear", "Reflux Tinted", "Reflux Blue",
            "Reflux Green", "Reflux Bronze", "Reflux Grey", "Reflux Silver",
            "Reflux Gold", "Reflux Black", "Reflux Low-E", "Reflux Privacy",
            "Reflux Frosted", "Reflux Satin", "Reflux Arctic Blue",

            // ─────────────────────────────────────────────────────────────
            // SUNLUX SERIES
            // ─────────────────────────────────────────────────────────────
            "Sunlux 120", "Sunlux S120", "Sunlux 140", "Sunlux S160", "Sunlux S180",
            "Sunlux S200", "Sunlux S220", "Sunlux S240", "Sunlux S280", "Sunlux S320",
            "Sunlux S360", "Sunlux S400", "Sunlux S500", "Sunlux S600",
            "Sunlux 150", "Sunlux 200", "Sunlux 250", "Sunlux 300",
            "Sunlux Classic", "Sunlux Premium", "Sunlux Ultra", "Sunlux Plus",
            "Sunlux Pro", "Sunlux Max", "Sunlux Reflective", "Sunlux Solar",
            "Sunlux Energy", "Sunlux 58", "Sunlux 65", "Sunlux 70", "Sunlux 75",
            "Sunlux 80", "Sunlux Clear", "Sunlux Tinted", "Sunlux Blue",
            "Sunlux Green", "Sunlux Bronze", "Sunlux Grey", "Sunlux Silver",
            "Sunlux Gold", "Sunlux Black", "Sunlux Low-E", "Sunlux Privacy",
            "Sunlux Frosted", "Sunlux Satin", "Sunlux Arctic Blue",

            // ─────────────────────────────────────────────────────────────
            // HD SERIES
            // ─────────────────────────────────────────────────────────────
            "HD 120", "HD S120", "HD 140", "HD S160", "HD S180",
            "HD S200", "HD S220", "HD S240", "HD S280", "HD S320",
            "HD S360", "HD S400", "HD S500", "HD S600",
            "HD 150", "HD 200", "HD 250", "HD 300",
            "HD Classic", "HD Premium", "HD Ultra", "HD Plus", "HD Pro",
            "HD Max", "HD Reflective", "HD Solar", "HD Energy",
            "HD 58", "HD 65", "HD 70", "HD 75", "HD 80",
            "HD Clear", "HD Tinted", "HD Blue", "HD Green", "HD Bronze",
            "HD Grey", "HD Silver", "HD Gold", "HD Black",
            "HD Low-E", "HD Privacy", "HD Frosted", "HD Satin", "HD Arctic Blue",

                        // ─────────────────────────────────────────────────────────────
            // RAMLY SERIES
            // ─────────────────────────────────────────────────────────────
            "Ramly 120", "Ramly S120", "Ramly 140", "Ramly S160", "Ramly S180",
            "Ramly S200", "Ramly S220", "Ramly S240", "Ramly S280", "Ramly S320",
            "Ramly S360", "Ramly S400", "Ramly S500", "Ramly S600",
            "Ramly 150", "Ramly 200", "Ramly 250", "Ramly 300",
            "Ramly Classic", "Ramly Premium", "Ramly Ultra", "Ramly Plus", "Ramly Pro",
            "Ramly Max", "Ramly Reflective", "Ramly Solar", "Ramly Energy",
            "Ramly 58", "Ramly 65", "Ramly 70", "Ramly 75", "Ramly 80",
            "Ramly Clear", "Ramly Tinted", "Ramly Blue", "Ramly Green", "Ramly Bronze",
            "Ramly Grey", "Ramly Silver", "Ramly Gold", "Ramly Black",
            "Ramly Low-E", "Ramly Privacy", "Ramly Frosted", "Ramly Satin",

            // ─────────────────────────────────────────────────────────────
            // ARCTIC SERIES
            // ─────────────────────────────────────────────────────────────
            "Arctic Clear", "Arctic Ultra Clear", "Arctic Low Iron", "Arctic Crystal Clear",
            "Arctic Blue", "Arctic Light Blue", "Arctic Dark Blue", "Arctic Ocean Blue",
            "Arctic Sky Blue", "Arctic Navy Blue", "Arctic Cobalt Blue", "Arctic Ice Blue",
            "Arctic Steel Blue", "Arctic Denim Blue", "Arctic Sapphire Blue",
            "Arctic Green", "Arctic Light Green", "Arctic Dark Green", "Arctic Forest Green",
            "Arctic Emerald Green", "Arctic Jade Green", "Arctic Teal Green",
            "Arctic Sea Green", "Arctic Mint Green", "Arctic Sage Green",
            "Arctic Bronze", "Arctic Light Bronze", "Arctic Dark Bronze",
            "Arctic Golden Bronze", "Arctic Medium Bronze", "Arctic Warm Bronze",
            "Arctic Cool Bronze", "Arctic Grey", "Arctic Light Grey", "Arctic Dark Grey",
            "Arctic Charcoal Grey", "Arctic Medium Grey", "Arctic Smoke Grey",
            "Arctic Slate Grey", "Arctic Pearl Grey", "Arctic Ash Grey",
            "Arctic Silver", "Arctic Bright Silver", "Arctic Mirror Silver",
            "Arctic Reflective Silver", "Arctic Hard Silver", "Arctic Platinum",
            "Arctic Chrome", "Arctic Gold", "Arctic Bright Gold", "Arctic Mirror Gold",
            "Arctic Reflective Gold", "Arctic Hard Gold", "Arctic Champagne",
            "Arctic Black", "Arctic Jet Black", "Arctic Dark Black", "Arctic Pure Black",
            "Arctic Obsidian", "Arctic Onyx", "Arctic White", "Arctic Pure White",
            "Arctic Off White", "Arctic Cream", "Arctic Pearl White", "Arctic Snow White",
            "Arctic Red", "Arctic Light Red", "Arctic Dark Red", "Arctic Crimson",
            "Arctic Burgundy", "Arctic Wine", "Arctic Ruby", "Arctic Rose",
            "Arctic Pink", "Arctic Hot Pink", "Arctic Lavender", "Arctic Purple",
            "Arctic Violet", "Arctic Orchid", "Arctic Orange", "Arctic Tangerine",
            "Arctic Yellow", "Arctic Lemon", "Arctic Gold Yellow",
            "Arctic Solar", "Arctic Energy", "Arctic Low-E", "Arctic Reflective",
            "Arctic Privacy", "Arctic Frosted", "Arctic Satin", "Arctic Mirror",
            "Arctic 58", "Arctic 65", "Arctic 70", "Arctic 75", "Arctic 80",
            "Arctic 120", "Arctic S120", "Arctic 140", "Arctic S160",
            "Arctic S200", "Arctic S220", "Arctic S240",

            // ─────────────────────────────────────────────────────────────
            // K • CLEAR / COOL SERIES
            // ─────────────────────────────────────────────────────────────
            "Klear 58", "Klear 65", "Klear 70", "Klear 75", "Klear 80",
            "Klear 85", "Klear 90", "Klear 95", "Klear 100",
            "Klear Plus", "Klear Ultra", "Klear Max", "Klear Clear",
            "Klear Tinted", "Klear Reflective", "Klear Low-E", "Klear Solar",
            "Klear Energy", "Klear Privacy", "Klear Frosted",
            "Klear 120", "Klear S120", "Klear 140", "Klear S160",
            "Cool Clear", "Cool Lite", "Cool Glass", "Cool Pro", "Cool Max",
            "Cool 50", "Cool 60", "Cool 70", "Cool 80", "Cool 90", "Cool 100",

            // ─────────────────────────────────────────────────────────────
            // NEUTRAL / NATURAL SERIES
            // ─────────────────────────────────────────────────────────────
            "Neutral 50", "Neutral 60", "Neutral 70", "Neutral 78", "Neutral 80",
            "Neutral Clear", "Neutral Plus", "Neutral Pro", "Neutral Ultra",
            "Natural View", "Natural Light", "Natural Tint", "Natural Clear",
            "Natural Grey", "Natural Bronze", "Natural Blue", "Natural Green",
            "Clarity Clear", "Clarity Plus", "Clarity Ultra", "Clarity Low-E",
            "Clarity Solar", "Clarity Energy", "Clear View", "Clear Plus", "Clear Pro",

            // ─────────────────────────────────────────────────────────────
            // CLIMA / CLIMATE SERIES
            // ─────────────────────────────────────────────────────────────
            "Clima Guard", "Clima Plus", "Clima Pro", "Clima Ultra",
            "Clima Control", "Clima Comfort", "Clima Energy", "Clima Select",
            "Clima Premium", "Clima Elite", "Climate Plus", "Climate Pro", "Climate Max",
            "Climate Control", "Climate Shield", "Climate Guard", "Climate Energy",
            "Comfort Plus", "Comfort Select", "Comfort Ultra", "Comfort Clear",
            "Comfort Tinted", "Comfort Reflective", "Comfort Low-E", "Comfort Solar",
            "Comfort Energy", "Comfort 60", "Comfort 70", "Comfort 80", "Comfort 90",

            // ─────────────────────────────────────────────────────────────
            // PREMIUM / ELITE / PRO SERIES
            // ─────────────────────────────────────────────────────────────
            "Premium Clear", "Premium Tinted", "Premium Reflective", "Premium Low-E",
            "Premium Solar", "Premium Energy", "Premium Plus", "Premium Pro", "Premium Max",
            "Elite Clear", "Elite Tinted", "Elite Reflective", "Elite Low-E",
            "Elite Solar", "Elite Energy", "Elite Plus", "Elite Pro", "Elite Max",
            "Pro Clear", "Pro Tinted", "Pro Reflective", "Pro Low-E", "Pro Solar",
            "Pro Energy", "Pro Plus", "Pro Max", "Pro Ultra",
            "Ultra Clear", "Ultra Plus", "Ultra Pro", "Ultra Max", "Ultra Low-E",
            "Ultra Solar", "Ultra Energy", "Extra Clear", "Extra Plus", "Extra Pro",
            "Extra Low-E", "Extra Solar", "Extra Energy",
            "Select Clear", "Select Tinted", "Select Reflective", "Select Low-E",
            "Max Clear", "Max Tinted", "Max Reflective", "Max Low-E",
            "Master Clear", "Master Tinted", "Master Reflective", "Master Low-E",

            // ─────────────────────────────────────────────────────────────
            // SMART / ELECTRONIC SERIES
            // ─────────────────────────────────────────────────────────────
            "Smart Clear", "Smart Tinted", "Smart Dimming", "Smart Plus", "Smart Pro",
            "Smart Max", "PDLC Clear", "PDLC Privacy", "PDLC Switch",
            "Switch Glass", "Switch Privacy", "Switch Dimming", "Electro Clear",
            "Electro Tinted", "Electro Chromic", "ESG Switch", "Sage Glass", "View Glass",
            "Smart Glass", "Electrochromic Glass", "PDLC Glass", "Privacy Glass Smart",

            // ─────────────────────────────────────────────────────────────
            // SPECIALTY / CUSTOM / DECORATIVE
            // ─────────────────────────────────────────────────────────────
            "Antique Clear", "Antique Bronze", "Antique Silver", "Bent Clear",
            "Bent Tinted", "Curved Clear", "Curved Tinted", "Slumped Clear",
            "Fused Clear", "Cast Clear", "Back Painted White", "Back Painted Black",
            "Back Painted Grey", "Back Painted Custom", "Color Coated Clear",
            "Enamel Clear", "Ceramic Coated Clear", "Textured Clear", "Decorative Clear",
            "Glass Block", "Glass Brick", "ESG Switchglass", "PDLC Smart",
            "Self-Cleaning Glass", "Bioclean", "Nano Coating Glass",
            "Anti-Reflective Glass", "AR Glass", "Anti-Graffiti Glass",
            "Security Glass", "Ballistic Glass", "Blast Resistant", "X-Ray Protective",
            "Mirrored Glass", "One-Way Mirror", "Spandrel Glass",
            "Custom", "Special Order", "Made to Order", "Other", "Mixed", "Various",
            "Sample", "Display", "Test Glass", "Non-Standard", "Special Size",
            "Remnant", "Offcut", "Leftover"
        };

        // ═══════════════════════════════════════════════════════════════
        // THICKNESS OPTIONS (mm)
        // ═══════════════════════════════════════════════════════════════
        public static string[] Thicknesses { get; } = new string[]
        {
            // Standard
            "2mm", "2.5mm", "3mm", "3.2mm", "3.5mm", "4mm", "4.5mm", "5mm", "5.5mm",
            "6mm", "6.38mm", "6.5mm", "6.76mm", "8mm", "8.38mm", "8.76mm",
            "10mm", "10.38mm", "10.76mm", "12mm", "12.38mm", "12.76mm",
            "15mm", "19mm", "20mm", "25mm", "30mm",
            // Laminated
            "6.38mm (3+0.38+3)", "6.76mm (3+0.38+3.76)",
            "8.38mm (4+0.38+4)", "8.76mm (4+0.38+4.76)",
            "10.38mm (5+0.38+5)", "10.76mm (5+0.38+5.76)",
            "12.38mm (6+0.38+6)", "12.76mm (6+0.38+6.76)",
            "16.76mm (8+0.38+8.38)", "20.76mm (10+0.38+10.38)",
            // Insulating
            "14mm", "16mm", "18mm", "20mm", "22mm", "24mm", "26mm", "28mm", "30mm", "32mm", "34mm", "36mm",
            // Tempered
            "4mm T", "5mm T", "6mm T", "8mm T", "10mm T", "12mm T",
            // Auto
            "3.2mm Auto", "3.5mm Auto", "4mm Auto", "5mm Auto"
        };

        // ═══════════════════════════════════════════════════════════════
        // AIRSPACE OPTIONS (mm) - DGU Cavity
        // ═══════════════════════════════════════════════════════════════
        public static List<string> AirspaceOptions { get; } = new List<string>
        {
            "2mm", "3mm", "4mm", "5mm", "6mm", "7mm", "8mm", "9mm", "10mm",
            "11mm", "12mm", "13mm", "14mm", "15mm", "16mm", "17mm", "18mm",
            "19mm", "20mm", "21mm", "22mm", "23mm", "24mm", "25mm", "26mm", "27mm", "28mm", "29mm", "30mm"
        };

        // ═══════════════════════════════════════════════════════════════
        // SPACER OPTIONS (mm)
        // ═══════════════════════════════════════════════════════════════
        public static List<string> Spacers { get; } = new List<string>
        {
            // Aluminum
            "Aluminum 2mm", "Aluminum 3mm", "Aluminum 4mm", "Aluminum 5mm",
            "Aluminum 6mm", "Aluminum 8mm", "Aluminum 10mm", "Aluminum 12mm",
            "Aluminum 14mm", "Aluminum 15mm", "Aluminum 16mm", "Aluminum 18mm",
            "Aluminum 20mm", "Aluminum 22mm", "Aluminum 24mm", "Aluminum 25mm",
            "Aluminum 26mm", "Aluminum 28mm", "Aluminum 30mm",
            // Warm Edge
            "Warm Edge 2mm", "Warm Edge 3mm", "Warm Edge 4mm", "Warm Edge 5mm",
            "Warm Edge 6mm", "Warm Edge 8mm", "Warm Edge 10mm", "Warm Edge 12mm",
            "Warm Edge 14mm", "Warm Edge 15mm", "Warm Edge 16mm", "Warm Edge 18mm",
            "Warm Edge 20mm", "Warm Edge 22mm", "Warm Edge 24mm", "Warm Edge 25mm",
            "Warm Edge 26mm", "Warm Edge 28mm", "Warm Edge 30mm",
            // Swiss Spacer / TPS
            "Swiss Spacer 6mm", "Swiss Spacer 8mm", "Swiss Spacer 10mm",
            "Swiss Spacer 12mm", "Swiss Spacer 14mm", "Swiss Spacer 15mm",
            "Swiss Spacer 16mm", "Swiss Spacer 18mm", "Swiss Spacer 20mm",
            "Swiss Spacer 22mm", "Swiss Spacer 24mm", "Swiss Spacer 25mm",
            "Swiss Spacer 26mm", "Swiss Spacer 28mm", "Swiss Spacer 30mm",
            // Super Spacer
            "Super Spacer 6mm", "Super Spacer 8mm", "Super Spacer 10mm",
            "Super Spacer 12mm", "Super Spacer 14mm", "Super Spacer 15mm",
            "Super Spacer 16mm", "Super Spacer 18mm", "Super Spacer 20mm",
            "Super Spacer 22mm", "Super Spacer 24mm", "Super Spacer 25mm",
            "Super Spacer 26mm", "Super Spacer 28mm", "Super Spacer 30mm",
            // Thermix
            "Thermix 6mm", "Thermix 8mm", "Thermix 10mm", "Thermix 12mm",
            "Thermix 14mm", "Thermix 15mm", "Thermix 16mm", "Thermix 18mm",
            "Thermix 20mm", "Thermix 22mm", "Thermix 24mm", "Thermix 25mm",
            "Thermix 26mm", "Thermix 28mm", "Thermix 30mm",
            // Chromatech
            "Chromatech 6mm", "Chromatech 8mm", "Chromatech 10mm",
            "Chromatech 12mm", "Chromatech 14mm", "Chromatech 15mm",
            "Chromatech 16mm", "Chromatech 18mm", "Chromatech 20mm",
            // Foam Spacer
            "Foam Spacer 6mm", "Foam Spacer 8mm", "Foam Spacer 10mm",
            "Foam Spacer 12mm", "Foam Spacer 14mm", "Foam Spacer 15mm",
            "Foam Spacer 16mm", "Foam Spacer 18mm", "Foam Spacer 20mm",
            // Stainless Steel
            "SS Spacer 6mm", "SS Spacer 8mm", "SS Spacer 10mm", "SS Spacer 12mm",
            "SS Spacer 14mm", "SS Spacer 15mm", "SS Spacer 16mm", "SS Spacer 18mm",
            "SS Spacer 20mm", "SS Spacer 22mm", "SS Spacer 24mm", "SS Spacer 25mm",
            "SS Spacer 26mm", "SS Spacer 28mm", "SS Spacer 30mm"
        };

        // ═══════════════════════════════════════════════════════════════
        // SEALANT TYPES
        // ═══════════════════════════════════════════════════════════════
        public static List<string> SealantTypes { get; } = new List<string>
        {
            "Primary Sealant", "Secondary Sealant", "Butyl Sealant", "Hot Melt Butyl",
            "Polysulfide Sealant", "Two-Part Polysulfide", "Polyurethane Sealant",
            "Two-Part Polyurethane", "Silicone Sealant", "Neutral Silicone",
            "Acetic Silicone", "Structural Silicone", "Hot Melt Sealant", "PIB Sealant",
            "Hot Melt PIB", "Warm Seal", "Cold Seal", "Dual Seal", "Triple Seal",
            "Silicone/Polysulfide", "Silicone/Polyurethane", "Butyl/Polysulfide",
            "Butyl/Silicone", "UV Resistant Sealant", "Weather Resistant Sealant",
            "High Modulus Sealant", "Low Modulus Sealant", "Flexible Sealant",
            "RTV Silicone", "Solvent Based Sealant", "Water Based Sealant", "Epoxy Sealant"
        };

        // ═══════════════════════════════════════════════════════════════
        // GAS TYPES
        // ═══════════════════════════════════════════════════════════════
        public static List<string> GasTypes { get; } = new List<string>
        {
            "Air", "Dry Air",
            "Argon 80%", "Argon 85%", "Argon 90%", "Argon 95%", "Argon 99%", "Argon 99.9%",
            "Krypton 80%", "Krypton 90%", "Krypton 95%", "Krypton 99%",
            "Xenon 80%", "Xenon 90%", "Xenon 95%",
            "SF6 (Sulfur Hexafluoride)",
            "Mix Gas Argon+Krypton", "Mix Gas Custom",
            "CO2", "Helium", "Neon"
        };

        // ═══════════════════════════════════════════════════════════════
        // PVB TYPES
        // ═══════════════════════════════════════════════════════════════
        public static List<string> PVBTypes { get; } = new List<string>
        {
            // Clear PVB
            "Clear PVB 0.38mm", "Clear PVB 0.50mm", "Clear PVB 0.76mm",
            "Clear PVB 0.89mm", "Clear PVB 1.14mm", "Clear PVB 1.52mm",
            "Clear PVB 2.28mm", "Clear PVB 2.54mm",
            // SentryGlas
            "SentryGlas 0.89mm", "SentryGlas 1.52mm", "SentryGlas 2.28mm", "SentryGlas 3.04mm",
            "SentryGlas Plus 1.52mm", "SentryGlas Plus 2.28mm",
            // Tinted PVB
            "Grey PVB 0.38mm", "Grey PVB 0.76mm", "Grey PVB 1.14mm", "Grey PVB 1.52mm",
            "Bronze PVB 0.38mm", "Bronze PVB 0.76mm", "Bronze PVB 1.14mm", "Bronze PVB 1.52mm",
            "Blue PVB 0.38mm", "Blue PVB 0.76mm", "Blue PVB 1.14mm", "Blue PVB 1.52mm",
            "Green PVB 0.38mm", "Green PVB 0.76mm", "Green PVB 1.14mm", "Green PVB 1.52mm",
            "Black PVB 0.38mm", "Black PVB 0.76mm", "Black PVB 1.14mm", "Black PVB 1.52mm",
            "Red PVB 0.76mm", "Yellow PVB 0.76mm",
            // Special PVB
            "White PVB 0.76mm", "White PVB 1.52mm",
            "Acoustic PVB", "Acoustic PVB 0.76mm", "Acoustic PVB 1.52mm",
            "Sound Acoustic PVB", "UV Blocking PVB", "UV Blocking PVB 0.76mm",
            "Solar Control PVB", "Heat Reflective PVB",
            "Decorative PVB", "Printed PVB", "Digitally Printed PVB",
            "Matte PVB", "Satin PVB", "Embossed PVB", "Textured PVB",
            "Fire Rated PVB", "Bullet Resistant PVB", "Security PVB", "Anti-Bandit PVB",
            "Low-E PVB", "Thermo PVB", "Color Match PVB", "Custom Color PVB"
        };

        // ═══════════════════════════════════════════════════════════════
        // LAMINATION TYPES
        // ═══════════════════════════════════════════════════════════════
        public static List<string> LaminationTypes { get; } = new List<string>
        {
            "Standard Lamination", "Clear Lamination", "Tinted Lamination",
            "Reflective Lamination", "Low-E Lamination", "Solar Control Lamination",
            "DGU + Lamination", "IGU + Lamination", "Triple Glaze Lamination",
            "SentryGlas Lamination", "SGP Lamination",
            "Acoustic Lamination", "Sound Control Lamination",
            "Bullet Resistant Lamination", "Ballistic Lamination",
            "Hurricane Resistant Lamination", "Security Lamination",
            "Fire Rated Lamination", "Fire Protection Lamination",
            "UV Blocking Lamination", "UV Protection Lamination",
            "Decorative Lamination", "Pattern Lamination",
            "Two-Ply Lamination", "Three-Ply Lamination", "Multi-Ply Lamination",
            "Custom Lamination"
        };

        // ═══════════════════════════════════════════════════════════════
        // EDGE WORK TYPES
        // ═══════════════════════════════════════════════════════════════
        public static List<string> EdgeWorkTypes { get; } = new List<string>
        {
            "Clean Cut", "Plain Cut", "Machine Cut", "Polished Edge", "Flat Polish",
            "Pencil Polish", "Beveled Edge", "Decorative Bevel", "Art Deco Bevel",
            "Arris Edge", "Arrised Edge", "Grind Edge", "Ground Edge", "CNC Edge Work",
            "CNC Polished", "Diamond Cut", "Laser Cut", "Waterjet Cut",
            "Sandblasted Edge", "Seamed Edge", "Mitered Edge", "Radius Corner",
            "Cut Corner", "Notch Cut", "Hole Cut", "Custom Edge", "Special Edge"
        };

        // ═══════════════════════════════════════════════════════════════
        // DRILLING / NOTCH OPTIONS
        // ═══════════════════════════════════════════════════════════════
        public static List<string> DrillingOptions { get; } = new List<string>
        {
            "No Hole", "No Processing", "1 Hole", "2 Holes", "3 Holes", "4 Holes",
            "6 Holes", "8 Holes", "Custom Holes", "Counter Sink Hole", "Through Hole",
            "Blind Hole", "Step Hole", "Notch Required", "Corner Notch", "Edge Notch",
            "Center Notch", "Special Shape Cutout", "Custom Cutout", "Rectangle Cutout",
            "Circle Cutout", "Slot Cutout", "Keyhole Cutout", "Vent Hole", "Drain Hole",
            "Wire Entry Hole", "Sensor Hole", "Handle Hole", "Lock Hole", "CNC Custom",
            "Waterjet Custom"
        };

        // ═══════════════════════════════════════════════════════════════
        // TEMPERING / HEAT TREATMENT
        // ═══════════════════════════════════════════════════════════════
        public static List<string> TemperingOptions { get; } = new List<string>
        {
            "Annealed (Plain)", "Standard Annealed", "Heat Strengthened", "Semi-Tempered",
            "Fully Tempered", "Fully Toughened", "Heat Soaked", "Heat Soak Tested",
            "Thermo Tempered", "Chemically Tempered", "Ion Exchange Tempered",
            "Tempered + Heat Soaked", "Tempered + Polished Edge", "Tempered + CNC Edge",
            "Tempered + Holes", "Tempered + Notches"
        };

        // ═══════════════════════════════════════════════════════════════
        // COATING TYPES
        // ═══════════════════════════════════════════════════════════════
        public static List<string> CoatingTypes { get; } = new List<string>
        {
            "Hard Coat (Pyrolytic)", "Soft Coat (Sputtered)", "On-Line Coating",
            "Off-Line Coating", "Low-E Coating", "Solar Low-E", "High Performance Low-E",
            "Solar Control Coating", "Solar Control Plus", "Reflective Coating",
            "Hard Reflective", "Soft Reflective", "Mirror Coating",
            "Anti-Reflective Coating", "AR Coating", "Self-Cleaning Coating",
            "Photocatalytic Coating", "Anti-Graffiti Coating", "Easy Clean Coating",
            "UV Blocking Coating", "UV Protection Coating", "IR Rejection Coating",
            "Heat Mirror Coating", "Privacy Coating", "Switchable Coating",
            "Electrochromic Coating", "Thermochromic Coating", "PDLC Coating", "Smart Coating",
            "Ceramic Coating", "Diamond Coating", "Titanium Coating", "Silver Nano Coating",
            "Copper Nano Coating", "Graphene Coating", "Color Coating", "Decorative Coating",
            "Safety Coating", "Security Coating"
        };

        // ═══════════════════════════════════════════════════════════════
        // SURFACE TREATMENTS
        // ═══════════════════════════════════════════════════════════════
        public static List<string> SurfaceTreatments { get; } = new List<string>
        {
            "None", "Plain", "Standard", "Sandblasted", "Full Sandblast", "Partial Sandblast",
            "Acid Etched", "Full Etch", "Partial Etch", "Satin Etched", "Frost Etch",
            "Silk Screen Printed", "Screen Print", "Ceramic Frit", "Ceramic Print",
            "Digital Print", "UV Print", "Vinyl Applied", "Vinyl Print", "Lamination Film",
            "Decorative Film", "Mirror Backing", "One-Way Mirror", "Textured", "Embossed",
            "Rolled Pattern", "Cast Pattern", "Tinted (In Mass)", "Coated (In Mass)",
            "Electrochromic", "PDLC Smart", "Anti-Slip Treatment", "Non-Slip",
            "Anti-Fingerprint", "Easy Clean", "Antibacterial", "Antimicrobial"
        };

        // ═══════════════════════════════════════════════════════════════
        // CUTOUT OPTIONS (SGU)
        // ═══════════════════════════════════════════════════════════════
        public static List<string> CutoutOptions { get; } = new List<string>
        {
            "No Cutout",
            "Rectangular Cutout",
            "Circular Cutout",
            "Oval Cutout",
            "Arch Cutout",
            "Triangle Cutout",
            "Trapezoid Cutout",
            "Custom Shape Cutout",
            "Door Hole",
            "Vent Hole",
            "Handle Hole",
            "Lock Hole",
            "Hinge Hole",
            "Sensor Hole",
            "Wire Entry Hole",
            "Drain Hole",
            "Top Hinge Cutout",
            "Bottom Hinge Cutout",
            "Multiple Cutouts",
            "Corner Notch",
            "Edge Notch",
            "Center Notch",
            "Slot Cutout",
            "Keyhole Cutout",
            "CNC Custom Cutout",
            "Waterjet Custom Cutout"
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
            "0%", "5%", "10%", "15%", "20%", "25%", "30%", "35%", "40%", "45%", "50%",
            "55%", "60%", "65%", "70%", "75%", "80%", "85%", "90%", "95%", "100%"
        };

        // ═══════════════════════════════════════════════════════════════
        // STANDARD SIZES (mm)
        // ═══════════════════════════════════════════════════════════════
        public static List<(int Width, int Height, string Desc)> StandardSizes = new List<(int, int, string)>
        {
            // 6 FT
            (1830, 1220, "6'x4'"), (1830, 1524, "6'x5'"), (1830, 1600, "6'x5.25'"),
            (1830, 1830, "6'x6'"), (1830, 2134, "6'x7'"), (1830, 2250, "6'x7.4'"),
            (1830, 2440, "6'x8'"), (1830, 2500, "6'x8.2'"), (1830, 2745, "6'x9'"),
            (1830, 3050, "6'x10'"), (1830, 3300, "6'x10.8'"), (1830, 3660, "6'x12'"),
            // 7 FT
            (2134, 1220, "7'x4'"), (2134, 1524, "7'x5'"), (2134, 1830, "7'x6'"),
            (2134, 2134, "7'x7'"), (2134, 2440, "7'x8'"), (2134, 2745, "7'x9'"),
            (2134, 3050, "7'x10'"), (2134, 3300, "7'x10.8'"), (2134, 3660, "7'x12'"),
            // 8 FT
            (2440, 1220, "8'x4'"), (2440, 1524, "8'x5'"), (2440, 1830, "8'x6'"),
            (2440, 2134, "8'x7'"), (2440, 2440, "8'x8'"), (2440, 2745, "8'x9'"),
            (2440, 3050, "8'x10'"), (2440, 3300, "8'x10.8'"), (2440, 3660, "8'x12'"),
            (2440, 4270, "8'x14'"), (2440, 4880, "8'x16'"),
            // 9 FT
            (2745, 1220, "9'x4'"), (2745, 1524, "9'x5'"), (2745, 1830, "9'x6'"),
            (2745, 2134, "9'x7'"), (2745, 2440, "9'x8'"), (2745, 2745, "9'x9'"),
            (2745, 3050, "9'x10'"), (2745, 3300, "9'x10.8'"), (2745, 3660, "9'x12'"),
            // 10 FT
            (3050, 1220, "10'x4'"), (3050, 1524, "10'x5'"), (3050, 1830, "10'x6'"),
            (3050, 2134, "10'x7'"), (3050, 2440, "10'x8'"), (3050, 2745, "10'x9'"),
            (3050, 3050, "10'x10'"), (3050, 3300, "10'x10.8'"), (3050, 3660, "10'x12'"),
            // 12 FT
            (3660, 1220, "12'x4'"), (3660, 1524, "12'x5'"), (3660, 1830, "12'x6'"),
            (3660, 2134, "12'x7'"), (3660, 2440, "12'x8'"), (3660, 2745, "12'x9'"),
            (3660, 3050, "12'x10'"), (3660, 3300, "12'x10.8'"), (3660, 3660, "12'x12'"),
                        // Metric
            (2000, 1000, ""), (2000, 1500, ""), (2000, 2000, ""),
            (2250, 1250, ""), (2250, 1500, ""), (2250, 1750, ""), (2250, 2000, ""),
            (2500, 1250, ""), (2500, 1500, ""), (2500, 1750, ""), (2500, 2000, ""),
            (2750, 1500, ""), (2750, 1750, ""), (2750, 2000, ""),
            (3000, 1500, ""), (3000, 1750, ""), (3000, 2000, ""), (3000, 2500, ""),
            (3300, 1500, ""), (3300, 1750, ""), (3300, 2000, ""), (3300, 2500, ""),
            (3600, 1500, ""), (3600, 1750, ""), (3600, 2000, ""), (3600, 2500, ""),
            (4000, 1500, ""), (4000, 1750, ""), (4000, 2000, ""), (4000, 2500, ""),
            (4500, 1500, ""), (4500, 1750, ""), (4500, 2000, ""), (4500, 2500, ""),
            (5000, 1500, ""), (5000, 1750, ""), (5000, 2000, ""), (5000, 2500, ""),
            (5500, 1750, ""), (5500, 2000, ""), (5500, 2500, ""),
            (6000, 1750, ""), (6000, 2000, ""), (6000, 2500, ""),
            (6500, 2000, ""), (6500, 2500, ""),
            (7000, 2000, ""), (7000, 2500, ""),
            (7500, 2000, ""), (7500, 2500, ""),
            (8000, 2000, ""), (8000, 2500, ""),
            // Auto
            (1400, 900, "Auto"), (1500, 900, "Auto"), (1600, 900, "Auto"),
            (1700, 900, "Auto"), (1800, 900, "Auto"), (1900, 900, "Auto"),
            (1400, 1000, "Auto"), (1500, 1000, "Auto"), (1600, 1000, "Auto"),
            (1700, 1000, "Auto"), (1800, 1000, "Auto"),
            (1200, 800, "Auto"), (1300, 800, "Auto"), (1400, 800, "Auto"),
            (1100, 700, "Auto"), (1200, 700, "Auto"), (1300, 700, "Auto"),
            (1000, 600, "Auto"), (1100, 600, "Auto"), (1200, 600, "Auto"),
            // Round
            (500, 500, "Round"), (600, 600, "Round"), (700, 700, "Round"),
            (800, 800, "Round"), (900, 900, "Round"), (1000, 1000, "Round"),
            (1200, 1200, "Round"), (1500, 1500, "Round"), (1800, 1800, "Round"),
            (2000, 2000, "Round"), (2500, 2500, "Round"), (3000, 3000, "Round"),
            // Custom
            (1000, 1000, "Custom"), (1250, 1250, "Custom"), (1500, 1500, "Custom"),
            (1750, 1750, "Custom"), (2000, 1500, "Custom"), (2250, 1750, "Custom"),
            (2500, 1750, "Custom"), (2750, 2000, "Custom"), (3000, 2000, "Custom"),
            (3250, 2250, "Custom"), (3500, 2250, "Custom"), (3750, 2500, "Custom"),
            (4000, 2250, "Custom"), (4250, 2500, "Custom"), (4500, 2500, "Custom"),
            (4750, 2750, "Custom"), (5000, 2500, "Custom"), (5250, 2750, "Custom"),
            (5500, 2750, "Custom"), (5750, 3000, "Custom"), (6000, 3000, "Custom")
        };

        // ═══════════════════════════════════════════════════════════════
        // COLOR ITEMS (with Hex)
        // ═══════════════════════════════════════════════════════════════
        public static List<GlassColorItem> ColorItems = new List<GlassColorItem>
        {
            // CLEAR
            new GlassColorItem { Name = "Clear", Hex = "#E8F4F8" },
            new GlassColorItem { Name = "Ultra Clear", Hex = "#F5FDFE" },
            new GlassColorItem { Name = "Crystal Clear", Hex = "#F0F8FF" },
            new GlassColorItem { Name = "Low Iron", Hex = "#F0FAFC" },
            new GlassColorItem { Name = "Pure Clear", Hex = "#F8FCFD" },
            new GlassColorItem { Name = "Optiwhite", Hex = "#F8FFFD" },
            new GlassColorItem { Name = "Starphire", Hex = "#F5FFFC" },
            new GlassColorItem { Name = "Starphire Ultra", Hex = "#F5FFFC" },

            // GREY
            new GlassColorItem { Name = "Grey", Hex = "#696969" },
            new GlassColorItem { Name = "Light Grey", Hex = "#A9A9A9" },
            new GlassColorItem { Name = "Dark Grey", Hex = "#36454F" },
            new GlassColorItem { Name = "Charcoal Grey", Hex = "#36454F" },
            new GlassColorItem { Name = "Medium Grey", Hex = "#808080" },
            new GlassColorItem { Name = "Smoke Grey", Hex = "#738276" },
            new GlassColorItem { Name = "Slate Grey", Hex = "#708090" },
            new GlassColorItem { Name = "Pearl Grey", Hex = "#C0C0C0" },
            new GlassColorItem { Name = "Gunmetal Grey", Hex = "#2C3539" },
            new GlassColorItem { Name = "Graphite", Hex = "#383838" },
            new GlassColorItem { Name = "Steel Grey", Hex = "#8D9BA7" },
            new GlassColorItem { Name = "Ash Grey", Hex = "#B2BEB5" },
            new GlassColorItem { Name = "Silver Grey", Hex = "#C0C0C0" },
            new GlassColorItem { Name = "Warm Grey", Hex = "#A89F91" },
            new GlassColorItem { Name = "Cool Grey", Hex = "#9BA5B0" },

            // BRONZE / BROWN
            new GlassColorItem { Name = "Bronze", Hex = "#8B4513" },
            new GlassColorItem { Name = "Light Bronze", Hex = "#A0522D" },
            new GlassColorItem { Name = "Dark Bronze", Hex = "#5C3317" },
            new GlassColorItem { Name = "Medium Bronze", Hex = "#8B4513" },
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
            new GlassColorItem { Name = "Camel", Hex = "#C19A6B" },
            new GlassColorItem { Name = "Rust", Hex = "#B7410E" },
            new GlassColorItem { Name = "Terracotta", Hex = "#E2725B" },
            new GlassColorItem { Name = "Burgundy", Hex = "#800020" },
            new GlassColorItem { Name = "Wine", Hex = "#722F37" },
            new GlassColorItem { Name = "Maroon", Hex = "#800000" },

            // GREEN
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
            new GlassColorItem { Name = "Moss", Hex = "#8A9A5B" },
            new GlassColorItem { Name = "Hunter Green", Hex = "#355E3B" },

            // BLUE
            new GlassColorItem { Name = "Blue", Hex = "#4169E1" },
            new GlassColorItem { Name = "Light Blue", Hex = "#ADD8E6" },
            new GlassColorItem { Name = "Dark Blue", Hex = "#00008B" },
            new GlassColorItem { Name = "Navy Blue", Hex = "#000080" },
            new GlassColorItem { Name = "Ocean Blue", Hex = "#4682B4" },
            new GlassColorItem { Name = "Sky Blue", Hex = "#87CEEB" },
            new GlassColorItem { Name = "Royal Blue", Hex = "#4169E1" },
            new GlassColorItem { Name = "Cobalt Blue", Hex = "#0047AB" },
            new GlassColorItem { Name = "Steel Blue", Hex = "#4682B4" },
            new GlassColorItem { Name = "Azure Blue", Hex = "#007FFF" },
            new GlassColorItem { Name = "Arctic Blue", Hex = "#D6FFFC" },
            new GlassColorItem { Name = "Ice Blue", Hex = "#D6FFFC" },
            new GlassColorItem { Name = "Denim Blue", Hex = "#1560BD" },
            new GlassColorItem { Name = "Sapphire Blue", Hex = "#0F52BA" },
            new GlassColorItem { Name = "Indigo Blue", Hex = "#4B0082" },
            new GlassColorItem { Name = "Cerulean", Hex = "#007BA7" },
            new GlassColorItem { Name = "Powder Blue", Hex = "#B0E0E6" },
            new GlassColorItem { Name = "Cadet Blue", Hex = "#5F9EA0" },
            new GlassColorItem { Name = "Prussian Blue", Hex = "#003153" },
            new GlassColorItem { Name = "Cyan", Hex = "#00FFFF" },
            new GlassColorItem { Name = "Teal Blue", Hex = "#367588" },

            // SILVER / MIRROR
            new GlassColorItem { Name = "Silver", Hex = "#C0C0C0" },
            new GlassColorItem { Name = "Bright Silver", Hex = "#E8E8E8" },
            new GlassColorItem { Name = "Mirror Silver", Hex = "#D4D4D4" },
            new GlassColorItem { Name = "Platinum", Hex = "#E5E4E2" },
            new GlassColorItem { Name = "Chrome", Hex = "#B5B5B5" },
            new GlassColorItem { Name = "Titanium", Hex = "#878681" },
            new GlassColorItem { Name = "Pewter", Hex = "#8E9196" },
            new GlassColorItem { Name = "Antique Silver", Hex = "#A9B8C4" },

            // GOLD
            new GlassColorItem { Name = "Gold", Hex = "#FFD700" },
            new GlassColorItem { Name = "Bright Gold", Hex = "#FFE44D" },
            new GlassColorItem { Name = "Dark Gold", Hex = "#B8860B" },
            new GlassColorItem { Name = "Champagne", Hex = "#F7E7CE" },
            new GlassColorItem { Name = "Rose Gold", Hex = "#B76E79" },
            new GlassColorItem { Name = "Copper", Hex = "#B87333" },
            new GlassColorItem { Name = "Brass", Hex = "#B5A642" },
            new GlassColorItem { Name = "Antique Gold", Hex = "#D4AF37" },
            new GlassColorItem { Name = "Bronze Mirror", Hex = "#8C7853" },

            // BLACK
            new GlassColorItem { Name = "Black", Hex = "#1C1C1C" },
            new GlassColorItem { Name = "Jet Black", Hex = "#0A0A0A" },
            new GlassColorItem { Name = "Pure Black", Hex = "#000000" },
            new GlassColorItem { Name = "Dark Black", Hex = "#1A1A1A" },
            new GlassColorItem { Name = "Obsidian", Hex = "#1F1F24" },
            new GlassColorItem { Name = "Onyx", Hex = "#353839" },
            new GlassColorItem { Name = "Midnight", Hex = "#191970" },
            new GlassColorItem { Name = "Charcoal", Hex = "#36454F" },
            new GlassColorItem { Name = "Graphite Black", Hex = "#1C1C1C" },

            // WHITE / CREAM
            new GlassColorItem { Name = "White", Hex = "#FFFFFF" },
            new GlassColorItem { Name = "Pure White", Hex = "#FFFAFA" },
            new GlassColorItem { Name = "Off White", Hex = "#FFFFF0" },
            new GlassColorItem { Name = "Cream", Hex = "#FFFDD0" },
            new GlassColorItem { Name = "Ivory", Hex = "#FFFFF0" },
            new GlassColorItem { Name = "Pearl White", Hex = "#F0F0F0" },
            new GlassColorItem { Name = "Snow White", Hex = "#FFFAFA" },
            new GlassColorItem { Name = "Antique White", Hex = "#FAEBD7" },
            new GlassColorItem { Name = "Beige", Hex = "#F5F5DC" },
            new GlassColorItem { Name = "Almond", Hex = "#EFDECD" },
            new GlassColorItem { Name = "Wheat", Hex = "#F5DEB3" },
            new GlassColorItem { Name = "Tan", Hex = "#D2B48C" },
            new GlassColorItem { Name = "Khaki", Hex = "#F0E68C" },

                        // RED / PINK
            new GlassColorItem { Name = "Red", Hex = "#DC2626" },
            new GlassColorItem { Name = "Light Red", Hex = "#F87171" },
            new GlassColorItem { Name = "Dark Red", Hex = "#991B1B" },
            new GlassColorItem { Name = "Cherry Red", Hex = "#DE3163" },
            new GlassColorItem { Name = "Crimson", Hex = "#DC143C" },
            new GlassColorItem { Name = "Ruby", Hex = "#9B111E" },
            new GlassColorItem { Name = "Scarlet", Hex = "#FF2400" },
            new GlassColorItem { Name = "Coral Red", Hex = "#F08080" },
            new GlassColorItem { Name = "Salmon", Hex = "#FA8072" },
            new GlassColorItem { Name = "Rose", Hex = "#FF007F" },
            new GlassColorItem { Name = "Pink", Hex = "#FFB6C1" },
            new GlassColorItem { Name = "Hot Pink", Hex = "#FF69B4" },
            new GlassColorItem { Name = "Magenta", Hex = "#FF00FF" },
            new GlassColorItem { Name = "Fuchsia", Hex = "#FF00FF" },
            new GlassColorItem { Name = "Blush", Hex = "#DE5D83" },
            new GlassColorItem { Name = "Burgundy", Hex = "#800020" },
            new GlassColorItem { Name = "Wine", Hex = "#722F37" },

            // PURPLE / VIOLET
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

            // ORANGE / YELLOW
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

            // SPECIALTY
            new GlassColorItem { Name = "Ice", Hex = "#E0F0F0" },
            new GlassColorItem { Name = "Frost", Hex = "#E8F4F8" },
            new GlassColorItem { Name = "Smoked", Hex = "#696969" },
            new GlassColorItem { Name = "Mirror", Hex = "#D4D4D4" },
            new GlassColorItem { Name = "One-Way Mirror", Hex = "#B8CCE4" },
            new GlassColorItem { Name = "Textured", Hex = "#A0A0A0" },
            new GlassColorItem { Name = "Metallic", Hex = "#B5B5B5" },
            new GlassColorItem { Name = "Pearl", Hex = "#EAE3D5" },
            new GlassColorItem { Name = "Tinted", Hex = "#B8B8B8" },
            new GlassColorItem { Name = "Neutral", Hex = "#D3D3D3" },
            new GlassColorItem { Name = "Sand", Hex = "#C2B280" },
            new GlassColorItem { Name = "Oatmeal", Hex = "#D9C7A2" },
            new GlassColorItem { Name = "Natural", Hex = "#E8DCC8" },
            new GlassColorItem { Name = "Nude", Hex = "#E3BC9A" },
            new GlassColorItem { Name = "Blush Beige", Hex = "#D4A889" },
            new GlassColorItem { Name = "Lacobel White", Hex = "#FFFFFF" },
            new GlassColorItem { Name = "Lacobel Black", Hex = "#1C1C1C" },
            new GlassColorItem { Name = "Lacobel Grey", Hex = "#808080" },
            new GlassColorItem { Name = "Lacobel Cream", Hex = "#FFFDD0" },
            new GlassColorItem { Name = "Lacobel Red", Hex = "#DC2626" },
            new GlassColorItem { Name = "Lacobel Blue", Hex = "#2563EB" },
            new GlassColorItem { Name = "Lacobel Green", Hex = "#16A34A" },
            new GlassColorItem { Name = "Lacobel Yellow", Hex = "#FACC15" },
            new GlassColorItem { Name = "Lacobel Orange", Hex = "#F97316" },
            new GlassColorItem { Name = "Lacobel Pink", Hex = "#F472B6" },
            new GlassColorItem { Name = "Lacobel Purple", Hex = "#9333EA" },
            new GlassColorItem { Name = "Lacobel Anthracite", Hex = "#3F3F46" },
            new GlassColorItem { Name = "Lacobel Graphite", Hex = "#4A4A4A" },
            new GlassColorItem { Name = "Lacobel Brown", Hex = "#78350F" },
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
            // Major International
            "AGC (A Glaverbel)", "Saint-Gobain Glass (SGG)", "Guardian Glass",
            "Pilkington", "NSG Group", "Şişecam", "Trakya Cam", "Sisecam",
            // Regional
            "Taiwan Glass", "Xinyi Glass", "CSG Holding", "Yaohua Glass",
            "Jiangsu Tianda", "Bengal Glass",
            // Middle East / GCC
            "Alfa Glass", "Saudi Glass", "Gulf Glass", "National Glass",
            "Emirates Glass", "Dubai Glass", "Abu Dhabi Glass", "Qatar Glass",
            "Muscat Glass", "Bahrain Glass", "Kuwait Glass",
            // South Asia
            "Gujarat Glass", "Modi Glass", "Saint-Gobain India",
            "Asahi India Glass", "CGC Glass", "Borosil Glass",
            "Piramal Glass", "HNG Float Glass",
            // Africa
            "PGI Glass", "Guardian South Africa", "Modisa Glass",
            "Nigerian Glass", "Egyptian Glass",
            // Southeast Asia
            "Viglacera", "Vinh Hoan Glass", "Thai Glass Industry",
            "Masan Resources", "Asia Glass", "Malay Glass",
            // China
            "CSG", "Yaohua", "Xinyi", "Jinjing", "Shandong Glass",
            "Beijing Xinying", "Heibei Glass", "Sino Glass", "K.clear",
            // Europe
            "Saint-Gobain Glass Europe", "AGC Europe", "Guardian Europe",
            "NSG Pilkington Europe",
            // USA
            "Guardian Industries", "Vitro", "Saint-Gobain North America",
            "Pilkington North America", "NSG North America",
            // Distributors
            "Global Glass Distributors", "Glass World", "Glass Masters",
            "Crystal Glass Co.", "Prime Glass", "Elite Glass", "Metro Glass",
            "City Glass", "United Glass", "Premium Glass Co.", "Royal Glass",
            "Alpha Glass LLC",
            // Others
            "Local Supplier", "Direct Import", "OEM", "Wholesale",
            "Retail Supplier", "Custom Manufacturer", "Other"
        };

        // ═══════════════════════════════════════════════════════════════
        // SUPPLIER METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Add a new supplier to the list (keeps sorted, prevents duplicates)
        /// </summary>
        public static void AddSupplier(string supplierName)
        {
            if (string.IsNullOrWhiteSpace(supplierName)) return;

            string trimmed = supplierName.Trim();
            if (!Suppliers.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            {
                Suppliers.Add(trimmed);
                Suppliers.Sort(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Remove a supplier from the list
        /// </summary>
        public static void RemoveSupplier(string supplierName)
        {
            if (string.IsNullOrWhiteSpace(supplierName)) return;
            Suppliers.RemoveAll(s => string.Equals(s, supplierName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Check if supplier exists
        /// </summary>
        public static bool SupplierExists(string supplierName)
        {
            return !string.IsNullOrWhiteSpace(supplierName) &&
                   Suppliers.Any(s => string.Equals(s, supplierName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        // ═══════════════════════════════════════════════════════════════
        // UNIT OPTIONS
        // ═══════════════════════════════════════════════════════════════
        public static List<string> UnitOptions { get; } = new List<string>
        {
            "AED", "USD", "EUR", "GBP", "SAR", "KWD", "QAR", "BHD", "OMR", "AED/sqm", "USD/sqm"
        };

        // ═══════════════════════════════════════════════════════════════
        // STATUS OPTIONS
        // ═══════════════════════════════════════════════════════════════
        public static List<string> StatusOptions { get; } = new List<string>
        {
            "Active", "Inactive", "Discontinued", "Out of Stock", "On Order", "Low Stock"
        };

        // ═══════════════════════════════════════════════════════════════
        // PAYMENT METHODS
        // ═══════════════════════════════════════════════════════════════
        public static List<string> PaymentMethods { get; } = new List<string>
        {
            "Cash", "Bank Transfer", "Cheque", "Credit Card", "Online Payment",
            "Credit / 7 Days", "Credit / 15 Days", "Credit / 30 Days",
            "Credit / 45 Days", "Credit / 60 Days", "Credit / 90 Days"
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
    // CALCULATOR DATA CLASSES
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