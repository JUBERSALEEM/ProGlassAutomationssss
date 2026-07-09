using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace ProGlassAutomation.Views.Optimization
{
    public class OptimizationServices
    {
        private const double DefaultCostPerSquareMillimeter = 0.00025;

        public double CalculateCost(double usedAreaSquareMillimeters)
        {
            if (usedAreaSquareMillimeters <= 0)
                return 0;

            return Math.Round(usedAreaSquareMillimeters * DefaultCostPerSquareMillimeter, 2);
        }

        public void ExportToCsv(string filePath, OptimizationResult result)
        {
            if (result == null || string.IsNullOrEmpty(filePath))
                return;

            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();

            sb.AppendLine("Ref,L,W,Area,Utilization,Waste");

            sb.AppendLine(string.Join(",",
                Escape(result.Ref),
                result.L.ToString(inv),
                result.W.ToString(inv),
                result.Area.ToString(inv),
                result.Util.ToString(inv),
                result.Waste.ToString(inv)
            ));

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private string Escape(string v)
        {
            if (string.IsNullOrEmpty(v))
                return "";

            if (v.Contains(",") || v.Contains("\"") || v.Contains("\n"))
                return "\"" + v.Replace("\"", "\"\"") + "\"";

            return v;
        }
    }
}