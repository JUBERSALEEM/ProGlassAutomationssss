using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const double Eps = 1e-6;

        // ═══════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════

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
            var sw = Stopwatch.StartNew();
            var result = new OptimizationResult();

            if (stockSheets == null || parts == null || !stockSheets.Any() || !parts.Any())
            {
                sw.Stop();
                result.RuntimeMs = sw.ElapsedMilliseconds;
                return result;
            }

            // Expand stock inventory (each sheet instance separately)
            var stockQueue = new List<StockSheet>();
            foreach (var s in stockSheets)
            {
                int qty = Math.Max(1, s.Qty);
                for (int i = 0; i < qty; i++)
                {
                    stockQueue.Add(new StockSheet { L = s.L, W = s.W, Qty = 1 });
                }
            }

            // Sort stock by area descending (largest first)
            stockQueue = stockQueue.OrderByDescending(s => s.L * s.W).ToList();

            // Expand parts by quantity
            var expandedParts = ExpandParts(parts);

            // Sort parts by area descending (largest first), with rotation handling
            expandedParts = expandedParts.OrderByDescending(p => p.L * p.W).ToList();

            var allPlaced = new List<PlacedPart>();
            var sheetResults = new List<OptimizationResult>();
            int totalSheetsUsed = 0;
            int totalPartsPlaced = 0;
            double totalUsedArea = 0;
            double totalSheetArea = 0;

            int sheetIndex = 0;
            int partIndex = 0;

            while (partIndex < expandedParts.Count && stockQueue.Count > 0)
            {
                var stock = stockQueue[0];
                stockQueue.RemoveAt(0);

                double usableW = stock.L - trimLeft - trimRight;
                double usableH = stock.W - trimTop - trimBottom;

                if (usableW <= 0 || usableH <= 0)
                    continue;

                var sheetPlacements = new List<PlacedPart>();
                var freeRects = new List<RectF>
                {
                    new RectF(trimLeft, trimTop, usableW, usableH)
                };

                while (partIndex < expandedParts.Count)
                {
                    var part = expandedParts[partIndex];
                    var bestFit = FindBestPlacement(part, freeRects, rotationMode, kerf);

                    if (bestFit == null)
                        break; // No more parts fit on this sheet

                    var oriented = GetOrientedSize(part.L, part.W, rotationMode);
                    double pw = oriented.Width;
                    double ph = oriented.Height;

                    var placement = new PlacedPart
                    {
                        X = bestFit.X,
                        Y = bestFit.Y,
                        L = pw,
                        W = ph
                    };
                    sheetPlacements.Add(placement);
                    allPlaced.Add(placement);
                    totalPartsPlaced++;
                    totalUsedArea += pw * ph;

                    // Update free rects (guillotine-style split)
                    freeRects = UpdateFreeRects(freeRects, bestFit.X, bestFit.Y, pw, ph, kerf);
                    partIndex++;
                }

                if (sheetPlacements.Count > 0)
                {
                    totalSheetsUsed++;
                    totalSheetArea += stock.L * stock.W;

                    sheetResults.Add(new OptimizationResult
                    {
                        Ref = $"S{sheetIndex + 1}",
                        L = stock.L,
                        W = stock.W,
                        Area = sheetPlacements.Sum(p => p.L * p.W),
                        Util = Math.Round((sheetPlacements.Sum(p => p.L * p.W) / (stock.L * stock.W)) * 100, 2),
                        Waste = Math.Round(100 - (sheetPlacements.Sum(p => p.L * p.W) / (stock.L * stock.W)) * 100, 2),
                        PlacedParts = sheetPlacements
                    });
                    sheetIndex++;
                }
            }

            // Build consolidated result (first sheet as primary, with all placements)
            if (sheetResults.Count > 0)
            {
                result.Ref = sheetResults[0].Ref;
                result.L = sheetResults[0].L;
                result.W = sheetResults[0].W;
                result.Area = totalUsedArea;
                result.Util = totalSheetArea > 0 ? Math.Round((totalUsedArea / totalSheetArea) * 100, 2) : 0;
                result.Waste = totalSheetArea > 0 ? Math.Round(100 - (totalUsedArea / totalSheetArea) * 100, 2) : 0;
                result.PlacedParts = allPlaced;
            }

            sw.Stop();
            result.RuntimeMs = sw.ElapsedMilliseconds;
            return result;
        }

        // ═══════════════════════════════════════════════════════
        // PART EXPANSION
        // ═══════════════════════════════════════════════════════

        private List<CutPart> ExpandParts(List<CutPart> parts)
        {
            var list = new List<CutPart>();
            if (parts == null) return list;

            foreach (var p in parts)
            {
                int qty = Math.Max(1, p.Qty);
                for (int i = 0; i < qty; i++)
                {
                    list.Add(new CutPart
                    {
                        L = p.L,
                        W = p.W,
                        Qty = 1
                    });
                }
            }
            return list;
        }

        // ═══════════════════════════════════════════════════════
        // ROTATION
        // ═══════════════════════════════════════════════════════

        private (double Width, double Height) GetOrientedSize(double w, double h, RotationMode mode)
        {
            switch (mode)
            {
                case RotationMode.None:
                    return (w, h);
                case RotationMode.Rotate90:
                    return (h, w);
                case RotationMode.BestFit:
                default:
                    // Best fit = use orientation that minimizes wasted area
                    if (w <= h) return (w, h);
                    return (w, h); // prefer original unless it doesn't fit (handled by FindBestPlacement)
            }
        }

        // ═══════════════════════════════════════════════════════
        // PLACEMENT (Best Short Side Fit heuristic)
        // ═══════════════════════════════════════════════════════

        private PlacementFit? FindBestPlacement(CutPart part, List<RectF> freeRects, RotationMode mode, double kerf)
        {
            PlacementFit? best = null;
            double bestScore = double.MaxValue;

            var orientations = GetAllOrientations(part.L, part.W, mode);

            foreach (var rect in freeRects)
            {
                foreach (var o in orientations)
                {
                    double pw = o.Width + kerf;
                    double ph = o.Height + kerf;

                    if (pw <= rect.Width + Eps && ph <= rect.Height + Eps)
                    {
                        // Best Short Side Fit heuristic: minimize the leftover area
                        double leftoverW = rect.Width - pw;
                        double leftoverH = rect.Height - ph;
                        double score = Math.Min(leftoverW, leftoverH);

                        // Bonus: bottom-left positioning
                        score += (rect.Y + rect.X) * 0.001;

                        if (score < bestScore)
                        {
                            bestScore = score;
                            best = new PlacementFit
                            {
                                X = rect.X,
                                Y = rect.Y,
                                Width = o.Width,
                                Height = o.Height
                            };
                        }
                    }
                }
            }

            return best;
        }

        private List<(double Width, double Height)> GetAllOrientations(double w, double h, RotationMode mode)
        {
            var list = new List<(double, double)>();
            if (mode == RotationMode.None)
            {
                list.Add((w, h));
            }
            else if (mode == RotationMode.Rotate90)
            {
                list.Add((h, w));
            }
            else // BestFit
            {
                list.Add((w, h));
                if (Math.Abs(w - h) > Eps)
                    list.Add((h, w));
            }
            return list;
        }

        // ═══════════════════════════════════════════════════════
        // FREE RECT MANAGEMENT (Guillotine-style split)
        // ═══════════════════════════════════════════════════════

        private List<RectF> UpdateFreeRects(List<RectF> freeRects, double x, double y, double w, double h, double kerf)
        {
            var result = new List<RectF>();
            double usedRight = x + w + kerf;
            double usedBottom = y + h + kerf;

            foreach (var rect in freeRects)
            {
                // Check if used rect intersects this free rect
                if (usedRight <= rect.X + Eps || x >= rect.X + rect.Width - Eps ||
                    usedBottom <= rect.Y + Eps || y >= rect.Y + rect.Height - Eps)
                {
                    // No intersection, keep this free rect
                    result.Add(rect);
                    continue;
                }

                // Split: generate up to 4 sub-rectangles
                // Right split
                if (usedRight < rect.X + rect.Width - Eps)
                {
                    result.Add(new RectF(
                        usedRight,
                        rect.Y,
                        rect.X + rect.Width - usedRight,
                        rect.Height
                    ));
                }

                // Bottom split
                if (usedBottom < rect.Y + rect.Height - Eps)
                {
                    result.Add(new RectF(
                        rect.X,
                        usedBottom,
                        rect.Width,
                        rect.Y + rect.Height - usedBottom
                    ));
                }

                // Top split
                if (y > rect.Y + Eps)
                {
                    result.Add(new RectF(
                        rect.X,
                        rect.Y,
                        rect.Width,
                        y - rect.Y
                    ));
                }

                // Left split
                if (x > rect.X + Eps)
                {
                    result.Add(new RectF(
                        rect.X,
                        rect.Y,
                        x - rect.X,
                        rect.Height
                    ));
                }
            }

            // Prune: remove rects contained within others
            return PruneFreeRects(result);
        }

        private List<RectF> PruneFreeRects(List<RectF> rects)
        {
            var result = new List<RectF>();
            foreach (var r in rects)
            {
                bool contained = false;
                foreach (var other in rects)
                {
                    if (r == other) continue;
                    if (r.X >= other.X - Eps &&
                        r.Y >= other.Y - Eps &&
                        r.X + r.Width <= other.X + other.Width + Eps &&
                        r.Y + r.Height <= other.Y + other.Height + Eps)
                    {
                        contained = true;
                        break;
                    }
                }
                if (!contained && r.Width > Eps && r.Height > Eps)
                    result.Add(r);
            }
            return result;
        }

        // ═══════════════════════════════════════════════════════
        // INTERNAL TYPES
        // ═══════════════════════════════════════════════════════

        private class RectF
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }

            public RectF() { }
            public RectF(double x, double y, double w, double h)
            {
                X = x; Y = y; Width = w; Height = h;
            }
        }

        private class PlacementFit
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
        }
    }

    // ═══════════════════════════════════════════════════════
    // PUBLIC MODELS
    // ═══════════════════════════════════════════════════════

    public class StockSheet
    {
        public double L { get; set; }
        public double W { get; set; }
        public int Qty { get; set; } = 1;
    }

    public class CutPart
    {
        public double L { get; set; }
        public double W { get; set; }
        public int Qty { get; set; } = 1;
    }

    public class PlacedPart
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double L { get; set; }
        public double W { get; set; }
        public bool Rotated { get; set; }
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
        public long RuntimeMs { get; set; }
    }
}