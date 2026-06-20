using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace ProGlassAutomation.Views.Optimization
{
    /*
     * ===================================================================================
     * OPTIMIZATION SERVICES HELPER
     * ===================================================================================
     * FIX LOG:
     * - Fixed commercial valuation math bug in CalculateCost. Removed '/ 1000' which was 
     *   understating wastage costing penalties by 1000x since the area is already in SQM.
     * - Maintained robust custom string serialization parsers for backward-compatible 
     *   job serialization without external dependencies.
     * ===================================================================================
     */
    public class OptimizationServices
    {
        public void ExportResultsCsv(string filePath, List<OptimizationResult> results, List<PlacedPart> allPlacedParts, double overallUtilization, double overallWastage)
        {
            if (results == null || results.Count == 0) return;
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Sheet Ref,Length mm,Width mm,Used Qty,Util %,Waste %,Area sqm");
                foreach (var r in results.Where(x => x.Ref != "TOTAL"))
                {
                    writer.WriteLine($"{r.Ref},{r.L},{r.W},{r.Used},{r.Util:N2},{r.Waste:N2},{r.Area:N4}");
                }
                writer.WriteLine($"TOTAL,,,{results.Count(x => x.Ref != "TOTAL")},,{overallUtilization:N2},{overallWastage:N2}");
            }
        }

        public void ExportPdf(string filePath, List<OptimizationResult> results, double overallUtilization)
        {
            if (results == null || results.Count == 0) return;
            string content = "Glass Cutting Layouts\n" +
                $"Generated: {DateTime.Now}\n\n" +
                $"Total Sheets: {results.Count(x => x.Ref != "TOTAL")}\n" +
                $"Utilization: {overallUtilization:N2}%\n\n" +
                string.Join("\n", results.Where(x => x.Ref != "TOTAL").Select(x => $"{x.Ref}: {x.L}x{x.W}mm - U:{x.Util:N2}%"));
            File.WriteAllText(filePath.Replace(".pdf", ".txt"), content);
        }

        public (List<StockSheet> stocks, List<CutPart> parts) LoadJob(OptimizationJob job)
        {
            var stocks = new List<StockSheet>();
            var parts = new List<CutPart>();
            if (job != null)
            {
                stocks = DeserializeStockSheets(job.StockJson ?? "[]");
                parts = DeserializeCutParts(job.PartsJson ?? "[]");
            }
            return (stocks, parts);
        }

        public OptimizationJob CreateJob(string name, int sheetsUsed, double utilization, List<StockSheet> stockSheets, List<CutPart> cutParts)
        {
            return new OptimizationJob
            {
                Id = 0,
                Name = name,
                CreatedDate = DateTime.Now,
                SheetsUsed = sheetsUsed,
                Utilization = utilization,
                StockJson = SerializeStockSheets(stockSheets),
                PartsJson = SerializeCutParts(cutParts)
            };
        }

        public List<StockSheet> ParseStockData(string text, int startIndex = 1)
        {
            var sheets = new List<StockSheet>();
            if (string.IsNullOrWhiteSpace(text)) return sheets;
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    double w = 0, h = 0;
                    int qty = 0;
                    if (double.TryParse(parts[0].Trim(), out w) &&
                        double.TryParse(parts[1].Trim(), out h) &&
                        int.TryParse(parts[2].Trim(), out qty))
                    {
                        sheets.Add(new StockSheet { Ref = $"S{startIndex++}", L = w, W = h, Qty = qty });
                    }
                }
            }
            return sheets;
        }

        public List<CutPart> ParsePartsData(string text, int startIndex = 1)
        {
            var parts = new List<CutPart>();
            if (string.IsNullOrWhiteSpace(text)) return parts;
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var lineParts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (lineParts.Length >= 3)
                {
                    double w = 0, h = 0;
                    int qty = 0;
                    if (double.TryParse(lineParts[0].Trim(), out w) &&
                        double.TryParse(lineParts[1].Trim(), out h) &&
                        int.TryParse(lineParts[2].Trim(), out qty))
                    {
                        parts.Add(new CutPart { Ref = $"P{startIndex++}", L = w, W = h, Rot = true, Qty = qty });
                    }
                }
            }
            return parts;
        }

        public double CalculateCost(double usedSQM, List<OptimizationResult> results, List<RemnantPiece> remnants)
        {
            if (usedSQM <= 0) return 0;
            double stockCost = usedSQM * 250;
            double wasteArea = 0;
            if (results != null)
            {
                foreach (var r in results.Where(x => x.Ref != "TOTAL"))
                {
                    double sheetArea = r.L * r.W / 1000000.0;
                    wasteArea += sheetArea - r.Area;
                }
            }
            // FIXED: Removed the "/ 1000" mathematical conversion error
            stockCost += wasteArea * 50;
            double remnantCredit = 0;
            if (remnants != null)
            {
                foreach (var rem in remnants.Where(x => !x.IsReused))
                {
                    remnantCredit += (rem.Area / 1000000) * 25;
                }
            }
            stockCost -= remnantCredit;
            return stockCost;
        }

        private string SerializeStockSheets(List<StockSheet> sheets)
        {
            if (sheets == null || sheets.Count == 0) return "[]";
            var sb = new StringBuilder("[");
            for (int i = 0; i < sheets.Count; i++)
            {
                var s = sheets[i];
                sb.Append($"{{\"Ref\":\"{s.Ref}\",\"L\":{s.L},\"W\":{s.W},\"Qty\":{s.Qty}}}");
                if (i < sheets.Count - 1) sb.Append(",");
            }
            sb.Append("]"); return sb.ToString();
        }

        private string SerializeCutParts(List<CutPart> parts)
        {
            if (parts == null || parts.Count == 0) return "[]";
            var sb = new StringBuilder("[");
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                sb.Append($"{{\"Ref\":\"{p.Ref}\",\"L\":{p.L},\"W\":{p.W},\"Rot\":{p.Rot.ToString().ToLower()},\"Qty\":{p.Qty}}}");
                if (i < parts.Count - 1) sb.Append(",");
            }
            sb.Append("]"); return sb.ToString();
        }

        private List<StockSheet> DeserializeStockSheets(string json)
        {
            var sheets = new List<StockSheet>();
            if (string.IsNullOrWhiteSpace(json) || json == "[]") return sheets;
            try
            {
                json = json.Trim('[', ']');
                if (string.IsNullOrEmpty(json)) return sheets;
                var items = json.Split(new[] { "},{" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in items)
                {
                    var clean = item.Trim('{', '}');
                    var sheet = new StockSheet();
                    var pairs = clean.Split(',');
                    foreach (var pair in pairs)
                    {
                        var kv = pair.Split(':');
                        if (kv.Length != 2) continue;
                        string key = kv[0].Trim('"');
                        string val = kv[1].Trim('"', ' ');

                        if (key == "Ref") sheet.Ref = val;
                        else if (key == "L")
                        {
                            double d = 0;
                            if (double.TryParse(val, out d)) sheet.L = d;
                        }
                        else if (key == "W")
                        {
                            double d = 0;
                            if (double.TryParse(val, out d)) sheet.W = d;
                        }
                        else if (key == "Qty")
                        {
                            int q = 0;
                            if (int.TryParse(val, out q)) sheet.Qty = q;
                        }
                    }
                    sheets.Add(sheet);
                }
            }
            catch { }
            return sheets;
        }

        private List<CutPart> DeserializeCutParts(string json)
        {
            var parts = new List<CutPart>();
            if (string.IsNullOrWhiteSpace(json) || json == "[]") return parts;
            try
            {
                json = json.Trim('[', ']');
                if (string.IsNullOrEmpty(json)) return parts;
                var items = json.Split(new[] { "},{" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in items)
                {
                    var clean = item.Trim('{', '}');
                    var part = new CutPart();
                    var pairs = clean.Split(',');
                    foreach (var pair in pairs)
                    {
                        var kv = pair.Split(':');
                        if (kv.Length != 2) continue;
                        string key = kv[0].Trim('"');
                        string val = kv[1].Trim('"', ' ');

                        if (key == "Ref") part.Ref = val;
                        else if (key == "L")
                        {
                            double d = 0;
                            if (double.TryParse(val, out d)) part.L = d;
                        }
                        else if (key == "W")
                        {
                            double d = 0;
                            if (double.TryParse(val, out d)) part.W = d;
                        }
                        else if (key == "Qty")
                        {
                            int q = 0;
                            if (int.TryParse(val, out q)) part.Qty = q;
                        }
                        else if (key == "Rot")
                        {
                            bool b = false;
                            if (bool.TryParse(val, out b)) part.Rot = b;
                        }
                    }
                    parts.Add(part);
                }
            }
            catch { }
            return parts;
        }
    }
}