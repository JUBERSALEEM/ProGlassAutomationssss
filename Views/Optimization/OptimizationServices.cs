using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace ProGlassAutomation.Views.Optimization
{
    public class OptimizationServices
    {
        private const double DefaultCostPerSquareMillimeter = 0.00025;

        // ═══════════════════════════════════════════════════════
        // COST CALCULATION
        // ═══════════════════════════════════════════════════════

        public double CalculateCost(double usedAreaSquareMillimeters)
        {
            if (usedAreaSquareMillimeters <= 0)
                return 0;

            return Math.Round(usedAreaSquareMillimeters * DefaultCostPerSquareMillimeter, 2);
        }

        public double CalculateTotalCost(OptimizationResult result)
        {
            if (result == null)
                return 0;

            return CalculateCost(result.Area);
        }

        // ═══════════════════════════════════════════════════════
        // CSV EXPORT
        // ═══════════════════════════════════════════════════════

        public void ExportToCsv(string filePath, OptimizationResult result)
        {
            if (result == null || string.IsNullOrEmpty(filePath))
                return;

            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();

            // ── HEADER ──────────────────────────────────────────
            sb.AppendLine("=== OPTIMIZATION RESULT ===");
            sb.AppendLine($"Generated,{DateTime.Now.ToString(inv)}");
            sb.AppendLine($"RuntimeMs,{result.RuntimeMs.ToString(inv)}");
            sb.AppendLine();

            // ── SUMMARY ─────────────────────────────────────────
            sb.AppendLine("=== SUMMARY ===");
            sb.AppendLine("Ref,L,W,Area,Utilization,Waste,PlacedCount");
            int placedCount = result.PlacedParts?.Count ?? 0;
            sb.AppendLine(string.Join(",",
                Escape(result.Ref),
                result.L.ToString(inv),
                result.W.ToString(inv),
                result.Area.ToString(inv),
                result.Util.ToString(inv),
                result.Waste.ToString(inv),
                placedCount.ToString(inv)
            ));
            sb.AppendLine();

            // ── PLACEMENTS DETAIL ──────────────────────────────
            if (result.PlacedParts != null && result.PlacedParts.Count > 0)
            {
                sb.AppendLine("=== PLACEMENTS ===");
                sb.AppendLine("X,Y,Width,Height");
                foreach (var p in result.PlacedParts)
                {
                    sb.AppendLine(string.Join(",",
                        p.X.ToString("F2", inv),
                        p.Y.ToString("F2", inv),
                        p.L.ToString("F2", inv),
                        p.W.ToString("F2", inv)
                    ));
                }
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        // ═══════════════════════════════════════════════════════
        // SUMMARY BUILDER
        // ═══════════════════════════════════════════════════════

        public string BuildSummary(OptimizationResult result)
        {
            if (result == null)
                return "No optimization result available.";

            var sb = new StringBuilder();
            sb.AppendLine($"Sheet Reference: {result.Ref}");
            sb.AppendLine($"Sheet Size: {result.L} x {result.W} mm");
            sb.AppendLine($"Area Used: {result.Area:N0} mm²");
            sb.AppendLine($"Utilization: {result.Util}%");
            sb.AppendLine($"Waste: {result.Waste}%");
            sb.AppendLine($"Parts Placed: {result.PlacedParts?.Count ?? 0}");
            sb.AppendLine($"Runtime: {result.RuntimeMs} ms");
            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════
        // CSV ESCAPING
        // ═══════════════════════════════════════════════════════

        private string Escape(string v)
        {
            if (string.IsNullOrEmpty(v))
                return "";

            if (v.Contains(",") || v.Contains("\"") || v.Contains("\n") || v.Contains("\r"))
                return "\"" + v.Replace("\"", "\"\"") + "\"";

            return v;
        }
    }
}