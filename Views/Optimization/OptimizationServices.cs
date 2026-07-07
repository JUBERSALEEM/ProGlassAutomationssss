using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using static ProGlassAutomation.Views.Optimization.OptimizationEngine;

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
        // Centralize the "base rates" used by this service (keeps existing costing workflow intact)
        private const double DefaultStockRatePerSqm = 250.0;
        private const double DefaultWastePenaltyPerSqm = 50.0;
        private const double DefaultRemnantCreditPerSqm = 25.0;

        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            AllowTrailingCommas = true
        };

        public void ExportResultsCsv(string filePath, List<OptimizationResult> results, List<PlacedPart> allPlacedParts, double overallUtilization, double overallWastage)
        {
            if (results == null || results.Count == 0) return;

            // Fixes:
            // - TOTAL row column alignment
            // - Culture-invariant numeric formatting (prevents comma decimal separators breaking CSV)
            // - Quote escaping for CSV-safe output
            var inv = CultureInfo.InvariantCulture;

            try
            {
                using var writer = new StreamWriter(filePath, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

                writer.WriteLine("Sheet Ref,Length mm,Width mm,Used Qty,Util %,Waste %,Area sqm");

                double totalArea = 0;
                int sheetCount = 0;

                foreach (var r in results.Where(x => x.Ref != "TOTAL"))
                {
                    sheetCount++;
                    totalArea += r.Area;

                    writer.WriteLine(string.Join(",",
                        EscapeCsv(r.Ref),
                        r.L.ToString(inv),
                        r.W.ToString(inv),
                        r.Used.ToString(inv),
                        r.Util.ToString("0.00", inv),
                        r.Waste.ToString("0.00", inv),
                        r.Area.ToString("0.0000", inv)
                    ));
                }

                // TOTAL row: keep 7 columns exactly as header defines
                writer.WriteLine(string.Join(",",
                    EscapeCsv("TOTAL"),
                    "",                      // Length mm
                    "",                      // Width mm
                    sheetCount.ToString(inv),// Used Qty (sheets used)
                    overallUtilization.ToString("0.00", inv), // Util %
                    overallWastage.ToString("0.00", inv),     // Waste %
                    totalArea.ToString("0.0000", inv)         // Area sqm (sum)
                ));
            }
            catch (Exception ex)
            {
                // Preserve calling workflow (caller has try/catch), but also log for diagnostics
                Debug.WriteLine($"[OptimizationServices] ExportResultsCsv Error: {ex}");
                throw;
            }
        }

        public void ExportPdf(string filePath, List<OptimizationResult> results, double overallUtilization)
        {
            if (results == null || results.Count == 0) return;

            // Fix: Previously wrote a .txt file by replacing ".pdf".
            // Now:
            // 1) Write a simple single-page PDF to the provided filePath.
            // 2) Also emit a companion .txt for readability (keeps legacy behavior without breaking PDF export).
            try
            {
                string content = BuildTextReport(results, overallUtilization);

                // Write PDF to the exact user-chosen path
                WriteSimplePdf(filePath, content);

                // Legacy companion output (does not replace the PDF)
                try
                {
                    string txtPath = Path.ChangeExtension(filePath, ".txt");
                    File.WriteAllText(txtPath, content, Encoding.UTF8);
                }
                catch (Exception exTxt)
                {
                    Debug.WriteLine($"[OptimizationServices] ExportPdf companion .txt failed: {exTxt.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OptimizationServices] ExportPdf Error: {ex}");
                throw;
            }
        }

        private string BuildTextReport(List<OptimizationResult> results, double overallUtilization)
        {
            return "Glass Cutting Layouts\n" +
                   $"Generated: {DateTime.Now}\n\n" +
                   $"Total Sheets: {results.Count(x => x.Ref != "TOTAL")}\n" +
                   $"Utilization: {overallUtilization:N2}%\n\n" +
                   string.Join("\n", results.Where(x => x.Ref != "TOTAL")
                                            .Select(x => $"{x.Ref}: {x.L}x{x.W}mm - U:{x.Util:N2}%"));
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

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Fix: tolerate common header rows (skip if no numeric data)
                // Fix: tolerate separators: tab, comma, semicolon, multiple spaces
                var tokens = SplitTokens(line);
                if (tokens.Count < 3) continue;

                // Support optional REF column:
                // - 3 cols: L W QTY
                // - 4 cols: REF L W QTY
                string? refToken = null;
                int offset = 0;

                if (tokens.Count >= 4 && !TryParseDouble(tokens[0], out _))
                {
                    refToken = tokens[0];
                    offset = 1;
                }

                if (tokens.Count < offset + 3) continue;

                if (!TryParseDouble(tokens[offset + 0], out double w)) continue;
                if (!TryParseDouble(tokens[offset + 1], out double h)) continue;
                if (!TryParseInt(tokens[offset + 2], out int qty)) continue;

                if (w <= 0 || h <= 0 || qty <= 0) continue;

                sheets.Add(new StockSheet
                {
                    Ref = !string.IsNullOrWhiteSpace(refToken) ? refToken : $"S{startIndex++}",
                    L = w,
                    W = h,
                    Qty = qty
                });
            }

            return sheets;
        }

        public List<CutPart> ParsePartsData(string text, int startIndex = 1)
        {
            var parts = new List<CutPart>();
            if (string.IsNullOrWhiteSpace(text)) return parts;

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var tokens = SplitTokens(line);
                if (tokens.Count < 3) continue;

                // Support optional REF column:
                // - 3 cols: L W QTY
                // - 4 cols: REF L W QTY
                string? refToken = null;
                int offset = 0;

                if (tokens.Count >= 4 && !TryParseDouble(tokens[0], out _))
                {
                    refToken = tokens[0];
                    offset = 1;
                }

                if (tokens.Count < offset + 3) continue;

                if (!TryParseDouble(tokens[offset + 0], out double w)) continue;
                if (!TryParseDouble(tokens[offset + 1], out double h)) continue;
                if (!TryParseInt(tokens[offset + 2], out int qty)) continue;

                if (w <= 0 || h <= 0 || qty <= 0) continue;

                parts.Add(new CutPart
                {
                    Ref = !string.IsNullOrWhiteSpace(refToken) ? refToken : $"P{startIndex++}",
                    L = w,
                    W = h,
                    Rot = true,
                    Qty = qty
                });
            }

            return parts;
        }

        public double CalculateCost(double usedSQM, List<OptimizationResult> results, List<RemnantPiece> remnants)
        {
            if (usedSQM <= 0) return 0;

            double stockCost = usedSQM * DefaultStockRatePerSqm;

            double wasteArea = 0;
            if (results != null)
            {
                foreach (var r in results.Where(x => x.Ref != "TOTAL"))
                {
                    double sheetArea = r.L * r.W / 1000000.0;
                    wasteArea += sheetArea - r.Area;
                }
            }

            // FIXED: Removed the "/ 1000" mathematical conversion error (kept as stated by comment)
            stockCost += wasteArea * DefaultWastePenaltyPerSqm;

            double remnantCredit = 0;
            if (remnants != null)
            {
                foreach (var rem in remnants.Where(x => !x.IsReused))
                {
                    remnantCredit += (rem.Area / 1000000.0) * DefaultRemnantCreditPerSqm;
                }
            }

            stockCost -= remnantCredit;

            // Fix: prevent negative cost
            if (stockCost < 0) stockCost = 0;

            return stockCost;
        }

        // =====================================================
        // JSON SERIALIZATION (robust) + LEGACY FALLBACK
        // =====================================================

        private string SerializeStockSheets(List<StockSheet> sheets)
        {
            if (sheets == null || sheets.Count == 0) return "[]";

            try
            {
                return JsonSerializer.Serialize(sheets, JsonOpts);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OptimizationServices] SerializeStockSheets JSON failed, using legacy: {ex.Message}");
                return SerializeStockSheetsLegacy(sheets);
            }
        }

        private string SerializeCutParts(List<CutPart> parts)
        {
            if (parts == null || parts.Count == 0) return "[]";

            try
            {
                return JsonSerializer.Serialize(parts.Select(p => new CutPart
                {
                    Ref = p.Ref,
                    L = p.L,
                    W = p.W,
                    Rot = p.Rot,
                    Qty = p.Qty
                }).ToList(), JsonOpts);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OptimizationServices] SerializeCutParts JSON failed, using legacy: {ex.Message}");
                return SerializeCutPartsLegacy(parts);
            }
        }

        private List<StockSheet> DeserializeStockSheets(string json)
        {
            var sheets = new List<StockSheet>();
            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]") return sheets;

            // Try robust JSON first
            try
            {
                var parsed = JsonSerializer.Deserialize<List<StockSheet>>(json, JsonOpts);
                if (parsed != null) return parsed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OptimizationServices] DeserializeStockSheets JSON failed, trying legacy: {ex.Message}");
            }

            // Legacy fallback
            try
            {
                return DeserializeStockSheetsLegacy(json);
            }
            catch (Exception ex2)
            {
                Debug.WriteLine($"[OptimizationServices] DeserializeStockSheets legacy failed: {ex2.Message}");
            }

            return sheets;
        }

        private List<CutPart> DeserializeCutParts(string json)
        {
            var parts = new List<CutPart>();
            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]") return parts;

            // Try robust JSON first
            try
            {
                var parsed = JsonSerializer.Deserialize<List<CutPart>>(json, JsonOpts);
                if (parsed != null) return parsed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OptimizationServices] DeserializeCutParts JSON failed, trying legacy: {ex.Message}");
            }

            // Legacy fallback
            try
            {
                return DeserializeCutPartsLegacy(json);
            }
            catch (Exception ex2)
            {
                Debug.WriteLine($"[OptimizationServices] DeserializeCutParts legacy failed: {ex2.Message}");
            }

            return parts;
        }

        // =====================================================
        // LEGACY SERIALIZATION (preserved; made safer)
        // =====================================================

        private string SerializeStockSheetsLegacy(List<StockSheet> sheets)
        {
            if (sheets == null || sheets.Count == 0) return "[]";
            var inv = CultureInfo.InvariantCulture;

            var sb = new StringBuilder("[");
            for (int i = 0; i < sheets.Count; i++)
            {
                var s = sheets[i];
                sb.Append("{");
                sb.Append($"\"Ref\":\"{JsonEscape(s.Ref)}\",");
                sb.Append($"\"L\":{s.L.ToString(inv)},");
                sb.Append($"\"W\":{s.W.ToString(inv)},");
                sb.Append($"\"Qty\":{s.Qty.ToString(inv)}");
                sb.Append("}");
                if (i < sheets.Count - 1) sb.Append(",");
            }
            sb.Append("]");
            return sb.ToString();
        }

        private string SerializeCutPartsLegacy(List<CutPart> parts)
        {
            if (parts == null || parts.Count == 0) return "[]";
            var inv = CultureInfo.InvariantCulture;

            var sb = new StringBuilder("[");
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                sb.Append("{");
                sb.Append($"\"Ref\":\"{JsonEscape(p.Ref)}\",");
                sb.Append($"\"L\":{p.L.ToString(inv)},");
                sb.Append($"\"W\":{p.W.ToString(inv)},");
                sb.Append($"\"Rot\":{p.Rot.ToString().ToLowerInvariant()},");
                sb.Append($"\"Qty\":{p.Qty.ToString(inv)}");
                sb.Append("}");
                if (i < parts.Count - 1) sb.Append(",");
            }
            sb.Append("]");
            return sb.ToString();
        }

        private List<StockSheet> DeserializeStockSheetsLegacy(string json)
        {
            var sheets = new List<StockSheet>();
            if (string.IsNullOrWhiteSpace(json) || json == "[]") return sheets;

            json = json.Trim();

            // Keep old behavior, but do not swallow errors silently
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

                    string key = kv[0].Trim().Trim('"');
                    string val = kv[1].Trim().Trim('"', ' ');

                    if (key == "Ref") sheet.Ref = val;
                    else if (key == "L")
                    {
                        if (TryParseDouble(val, out double d)) sheet.L = d;
                    }
                    else if (key == "W")
                    {
                        if (TryParseDouble(val, out double d)) sheet.W = d;
                    }
                    else if (key == "Qty")
                    {
                        if (TryParseInt(val, out int q)) sheet.Qty = q;
                    }
                }

                sheets.Add(sheet);
            }

            return sheets;
        }

        private List<CutPart> DeserializeCutPartsLegacy(string json)
        {
            var parts = new List<CutPart>();
            if (string.IsNullOrWhiteSpace(json) || json == "[]") return parts;

            json = json.Trim();

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

                    string key = kv[0].Trim().Trim('"');
                    string val = kv[1].Trim().Trim('"', ' ');

                    if (key == "Ref") part.Ref = val;
                    else if (key == "L")
                    {
                        if (TryParseDouble(val, out double d)) part.L = d;
                    }
                    else if (key == "W")
                    {
                        if (TryParseDouble(val, out double d)) part.W = d;
                    }
                    else if (key == "Qty")
                    {
                        if (TryParseInt(val, out int q)) part.Qty = q;
                    }
                    else if (key == "Rot")
                    {
                        if (bool.TryParse(val, out bool b)) part.Rot = b;
                    }
                }

                parts.Add(part);
            }

            return parts;
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private static string EscapeCsv(string value)
        {
            value ??= string.Empty;
            bool mustQuote = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
            if (!mustQuote) return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string JsonEscape(string value)
        {
            if (value == null) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static List<string> SplitTokens(string line)
        {
            // Split on common separators, but keep it simple and backward compatible
            var parts = line.Split(new[] { '\t', ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
        }

        private static bool TryParseDouble(string text, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;

            text = text.Trim();

            // Try current culture then invariant for robustness
            return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
                   || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseInt(string text, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;

            text = text.Trim();

            return int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value)
                   || int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        // =====================================================
        // Minimal PDF writer (single page, text only)
        // =====================================================

        private static void WriteSimplePdf(string filePath, string text)
        {
            // Minimal PDF implementation to satisfy "ExportPdf" workflow without external libraries.
            // Supports ASCII text reliably; non-ASCII characters are replaced with '?'.
            // This preserves features without introducing third-party dependencies.

            string safeText = MakeAsciiSafe(text ?? string.Empty);
            var lines = safeText.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

            // Build PDF content stream
            // A4 page: 595 x 842 points
            var sb = new StringBuilder();
            sb.AppendLine("BT");
            sb.AppendLine("/F1 10 Tf");
            sb.AppendLine("12 TL");
            sb.AppendLine("50 800 Td");

            foreach (var line in lines.Take(80)) // keep within one page (simple, predictable)
            {
                sb.AppendLine($"({PdfEscape(line)}) Tj");
                sb.AppendLine("T*");
            }

            sb.AppendLine("ET");

            byte[] contentBytes = Encoding.ASCII.GetBytes(sb.ToString());
            int len = contentBytes.Length;

            // PDF objects
            var pdf = new StringBuilder();
            pdf.AppendLine("%PDF-1.4");

            var xref = new List<int>();

            void Obj(int id, string body)
            {
                xref.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
                pdf.AppendLine($"{id} 0 obj");
                pdf.AppendLine(body);
                pdf.AppendLine("endobj");
            }

            Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
            Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
            Obj(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>");
            Obj(4, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

            // Stream object
            xref.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
            pdf.AppendLine("5 0 obj");
            pdf.AppendLine($"<< /Length {len} >>");
            pdf.AppendLine("stream");
            // stream bytes appended after converting pdf header to bytes
            string beforeStream = pdf.ToString();
            byte[] beforeBytes = Encoding.ASCII.GetBytes(beforeStream);
            byte[] afterBytes = Encoding.ASCII.GetBytes("\nendstream\nendobj\n");

            // Now write actual file with correct offsets
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            fs.Write(beforeBytes, 0, beforeBytes.Length);
            fs.Write(contentBytes, 0, contentBytes.Length);
            fs.Write(afterBytes, 0, afterBytes.Length);

            // xref table
            int xrefStart = (int)fs.Position;

            var xrefSb = new StringBuilder();
            xrefSb.AppendLine("xref");
            xrefSb.AppendLine($"0 {xref.Count + 1}");
            xrefSb.AppendLine("0000000000 65535 f ");

            foreach (int offset in xref)
            {
                xrefSb.AppendLine(offset.ToString("D10", CultureInfo.InvariantCulture) + " 00000 n ");
            }

            xrefSb.AppendLine("trailer");
            xrefSb.AppendLine($"<< /Size {xref.Count + 1} /Root 1 0 R >>");
            xrefSb.AppendLine("startxref");
            xrefSb.AppendLine(xrefStart.ToString(CultureInfo.InvariantCulture));
            xrefSb.AppendLine("%%EOF");

            byte[] xrefBytes = Encoding.ASCII.GetBytes(xrefSb.ToString());
            fs.Write(xrefBytes, 0, xrefBytes.Length);
        }

        private static string MakeAsciiSafe(string input)
        {
            var sb = new StringBuilder(input.Length);
            foreach (char c in input)
            {
                if (c >= 32 && c <= 126) sb.Append(c);
                else if (c == '\n' || c == '\r' || c == '\t') sb.Append(c);
                else sb.Append('?');
            }
            return sb.ToString();
        }

        private static string PdfEscape(string line)
        {
            if (line == null) return string.Empty;
            return line.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        }
    }
}