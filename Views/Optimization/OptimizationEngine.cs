using System;
using System.Collections.Generic;
using System.Linq;

namespace ProGlassAutomation.Views.Optimization
{
    public enum RotationMode
    {
        None,
        Rotate90,
        BestFit
    }

    public class OptimizationEngine
    {
        public OptimizationResult Execute(
            List<StockSheet> stockSheets,
            List<CutPart> parts,
            double trimLeft,
            double trimRight,
            double trimTop,
            double trimBottom,
            double kerf,
            RotationMode rotationMode)
        {
            var result = new OptimizationResult();

            if (stockSheets == null || parts == null)
                return result;

            if (!stockSheets.Any() || !parts.Any())
                return result;

            var sheet = stockSheets.First();

            double usableWidth = sheet.L - trimLeft - trimRight;
            double usableHeight = sheet.W - trimTop - trimBottom;

            if (usableWidth <= 0 || usableHeight <= 0)
                return result;

            var expandedParts = ExpandParts(parts);

            var placedParts = new List<PlacedPart>();

            double currentX = 0;
            double currentY = 0;
            double rowHeight = 0;

            foreach (var part in expandedParts)
            {
                var oriented = GetOrientedSize(part.L, part.W, usableWidth, usableHeight, rotationMode);

                double partWidth = oriented.Width;
                double partHeight = oriented.Height;

                if (currentX + partWidth > usableWidth)
                {
                    currentX = 0;
                    currentY += rowHeight + kerf;
                    rowHeight = 0;
                }

                if (currentY + partHeight > usableHeight)
                    break;

                placedParts.Add(new PlacedPart
                {
                    X = currentX,
                    Y = currentY,
                    L = partWidth,
                    W = partHeight
                });

                currentX += partWidth + kerf;

                if (partHeight > rowHeight)
                    rowHeight = partHeight;
            }

            double usedArea = placedParts.Sum(p => p.L * p.W);
            double totalArea = usableWidth * usableHeight;

            double utilization = totalArea > 0
                ? (usedArea / totalArea) * 100.0
                : 0;

            result.Ref = "S1";
            result.L = sheet.L;
            result.W = sheet.W;
            result.Area = usedArea;
            result.Util = Math.Round(utilization, 2);
            result.Waste = Math.Round(100 - utilization, 2);
            result.PlacedParts = placedParts;

            return result;
        }

        private List<CutPart> ExpandParts(List<CutPart> parts)
        {
            var result = new List<CutPart>();

            foreach (var p in parts)
            {
                int qty = Math.Max(1, p.Qty);

                for (int i = 0; i < qty; i++)
                {
                    result.Add(new CutPart
                    {
                        L = p.L,
                        W = p.W,
                        Qty = 1
                    });
                }
            }

            return result;
        }

        private (double Width, double Height) GetOrientedSize(
            double width,
            double height,
            double maxWidth,
            double maxHeight,
            RotationMode mode)
        {
            switch (mode)
            {
                case RotationMode.None:
                    return (width, height);

                case RotationMode.Rotate90:
                    return (height, width);

                case RotationMode.BestFit:
                default:
                    bool normalFits = width <= maxWidth && height <= maxHeight;
                    bool rotatedFits = height <= maxWidth && width <= maxHeight;

                    if (normalFits && !rotatedFits)
                        return (width, height);

                    if (!normalFits && rotatedFits)
                        return (height, width);

                    return (width <= height) ? (width, height) : (height, width);
            }
        }
    }

    public class StockSheet
    {
        public double L { get; set; }
        public double W { get; set; }
    }

    public class CutPart
    {
        public double L { get; set; }
        public double W { get; set; }
        public int Qty { get; set; }
    }

    public class PlacedPart
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double L { get; set; }
        public double W { get; set; }
    }

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