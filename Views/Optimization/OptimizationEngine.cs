using System;
using System.Collections.Generic;
using System.Linq;

namespace ProGlassAutomation.Views.Optimization
{
    public enum RotationMode { None, Rotate90, BestFit }

    public class OptimizationEngine
    {
        public OptimizationResult Execute(
            List<StockSheet> stockSheets, List<CutPart> parts,
            double trimLeft, double trimRight, double trimTop, double trimBottom,
            double kerf, RotationMode rotationMode)
        {
            var result = new OptimizationResult();
            if (stockSheets == null || parts == null || !stockSheets.Any() || !parts.Any()) return result;

            var sheet = stockSheets.First();
            double uw = sheet.L - trimLeft - trimRight;
            double uh = sheet.W - trimTop - trimBottom;
            if (uw <= 0 || uh <= 0) return result;

            var expanded = ExpandParts(parts);
            var placed = new List<PlacedPart>();
            double cx = 0, cy = 0, rh = 0;

            foreach (var part in expanded)
            {
                var o = GetOrientedSize(part.L, part.W, uw, uh, rotationMode);
                double pw = o.Width, ph = o.Height;

                if (cx + pw > uw) { cx = 0; cy += rh + kerf; rh = 0; }
                if (cy + ph > uh) break;

                placed.Add(new PlacedPart { X = cx, Y = cy, L = pw, W = ph });
                cx += pw + kerf;
                if (ph > rh) rh = ph;
            }

            double used = placed.Sum(p => p.L * p.W);
            double total = uw * uh;
            double util = total > 0 ? (used / total) * 100 : 0;

            result.Ref = "S1";
            result.L = sheet.L;
            result.W = sheet.W;
            result.Area = used;
            result.Util = Math.Round(util, 2);
            result.Waste = Math.Round(100 - util, 2);
            result.PlacedParts = placed;
            return result;
        }

        private List<CutPart> ExpandParts(List<CutPart> parts)
        {
            var list = new List<CutPart>();
            foreach (var p in parts)
                for (int i = 0; i < Math.Max(1, p.Qty); i++)
                    list.Add(new CutPart { L = p.L, W = p.W, Qty = 1 });
            return list;
        }

        private (double Width, double Height) GetOrientedSize(double w, double h, double mw, double mh, RotationMode mode)
        {
            switch (mode)
            {
                case RotationMode.None: return (w, h);
                case RotationMode.Rotate90: return (h, w);
                default:
                    bool nf = w <= mw && h <= mh;
                    bool rf = h <= mw && w <= mh;
                    if (nf && !rf) return (w, h);
                    if (!nf && rf) return (h, w);
                    return (w <= h) ? (w, h) : (h, w);
            }
        }
    }

    public class StockSheet { public double L { get; set; } public double W { get; set; } }
    public class CutPart { public double L { get; set; } public double W { get; set; } public int Qty { get; set; } }
    public class PlacedPart { public double X { get; set; } public double Y { get; set; } public double L { get; set; } public double W { get; set; } }

    public class OptimizationResult
    {
        public string Ref { get; set; } = "";
        public double L { get; set; }
        public double W { get; set; }
        public double Area { get; set; }
        public double Util { get; set; }
        public double Waste { get; set; }
        public List<PlacedPart> PlacedParts { get; set; } = new List<PlacedPart>();
    }
}