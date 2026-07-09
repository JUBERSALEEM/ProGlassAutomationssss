using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace ProGlassAutomation.Views.Optimization
{
    public class OptimizationServices
    {
        private const double DefaultCostPerSquareMeter = 250.0;

        public double CalculateCost(double usedAreaSquareMillimeters)
        {
            if (usedAreaSquareMillimeters <= 0)
                return 0;

            double usedSquareMeters = usedAreaSquareMillimeters / 1_000_000.0;

            double cost = usedSquareMeters * DefaultCostPerSquareMeter;

            return Math.Round(cost, 2);
        }

        public void ExportToCsv(string filePath, OptimizationResult result)
        {
            if (result == null)
                return;

            var inv = CultureInfo.InvariantCulture;

            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

            writer.WriteLine("Ref,SheetLength,SheetWidth,UsedArea,Utilization,Waste");

            writer.WriteLine(string.Join(",",
                Escape(result.Ref),
                result.L.ToString(inv),
                result.W.ToString(inv),
                result.Area.ToString(inv),
                result.Util.ToString(inv),
                result.Waste.ToString(inv)
            ));
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Contains(",") || value.Contains("\""))
                return "\"" + value.Replace("\"", "\"\"") + "\"";

            return value;
        }
    }
}