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

            // Expand stock inventory (each sheet instance separately by Qty)
            var stockQueue = new List<StockSheet>();
            foreach (var s in stockSheets)
            {
                int qty = Math.Max(1, s.Qty);
                for (int i = 0; i < qty; i++)
                {
                    stockQueue.Add(new StockSheet { L = s.L, W = s.W, Qty = 1 });
                }
            }

            var expandedParts = ExpandParts(parts);
            expandedParts = expandedParts.OrderByDescending(p => p.L * p.W).ToList();

            var sheetResults = new List<OptimizationResult>();
            int totalSheetsUsed = 0;
            int totalPartsPlaced = 0;
            double totalUsedArea = 0;
            double totalSheetArea = 0;
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

                int currentSheetIndex = totalSheetsUsed;

                bool placedAny = true;
                while (partIndex < expandedParts.Count && placedAny)
                {
                    placedAny = false;

                    PlacementFit? best = null;
                    int bestPartIndex = -1;

                    for (int pIdx = partIndex; pIdx < expandedParts.Count; pIdx++)
                    {
                        var part = expandedParts[pIdx];
                        var fit = FindBestPlacement(part, freeRects, rotationMode);
                        if (fit == null) continue;

                        if (best == null || fit.Score < best.Score)
                        {
                            best = fit;
                            bestPartIndex = pIdx;
                        }
                    }

                    if (best == null || bestPartIndex < 0)
                        break;

                    double pw = best.Width;
                    double ph = best.Height;

                    if (best.X + pw > stock.L - trimRight + Eps ||
                        best.Y + ph > stock.W - trimBottom + Eps)
                        break;

                    // ✅ FIX: Copy Label from source CutPart into PlacedPart
                    var sourcePart = expandedParts[bestPartIndex];
                    sheetPlacements.Add(new PlacedPart
                    {
                        Label = sourcePart.Label ?? "",
                        X = best.X,
                        Y = best.Y,
                        L = pw - kerf,
                        W = ph - kerf,
                        Rotated = best.IsRotated,
                        SheetIndex = currentSheetIndex
                    });

                    totalPartsPlaced++;
                    totalUsedArea += pw * ph;

                    freeRects = UpdateFreeRects(freeRects, best.X, best.Y, pw, ph);
                    partIndex++;
                    placedAny = true;
                }

                if (sheetPlacements.Count > 0)
                {
                    totalSheetsUsed++;
                    totalSheetArea += stock.L * stock.W;

                    double usedArea = sheetPlacements.Sum(p => p.L * p.W);
                    sheetResults.Add(new OptimizationResult
                    {
                        Ref = $"S{totalSheetsUsed}",
                        L = stock.L,
                        W = stock.W,
                        Area = usedArea,
                        Util = Math.Round((usedArea / (stock.L * stock.W)) * 100, 2),
                        Waste = Math.Round(100 - (usedArea / (stock.L * stock.W)) * 100, 2),
                        PlacedParts = sheetPlacements
                    });
                }
            }

            if (sheetResults.Count > 0)
            {
                result.Ref = sheetResults[0].Ref;
                result.L = sheetResults[0].L;
                result.W = sheetResults[0].W;
                result.Area = result.PlacedParts.Sum(p => p.L * p.W);
                result.Util = totalSheetArea > 0 ? Math.Round((totalUsedArea / totalSheetArea) * 100, 2) : 0;
                result.Waste = totalSheetArea > 0 ? Math.Round(100 - (totalUsedArea / totalSheetArea) * 100, 2) : 0;
                result.PlacedParts = sheetResults.SelectMany(s => s.PlacedParts).ToList();
            }

            sw.Stop();
            result.RuntimeMs = sw.ElapsedMilliseconds;
            return result;
        }

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
                        Label = p.Label ?? "",
                        L = p.L,
                        W = p.W,
                        Qty = 1
                    });
                }
            }
            return list;
        }

        private List<(double W, double H, bool Rotated)> GetOrientations(double w, double h, RotationMode mode)
        {
            var list = new List<(double, double, bool)>();

            if (mode == RotationMode.None)
            {
                list.Add((w, h, false));
            }
            else if (mode == RotationMode.Rotate90)
            {
                list.Add((h, w, true));
            }
            else
            {
                list.Add((w, h, false));
                if (Math.Abs(w - h) > Eps)
                    list.Add((h, w, true));
            }

            return list;
        }

        private class PlacementFit
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public bool IsRotated { get; set; }
            public double Score { get; set; }
        }

        private PlacementFit? FindBestPlacement(
            CutPart part,
            List<RectF> freeRects,
            RotationMode mode)
        {
            PlacementFit? best = null;
            double bestScore = double.MaxValue;

            var orientations = GetOrientations(part.L, part.W, mode);

            foreach (var rect in freeRects)
            {
                foreach (var o in orientations)
                {
                    if (o.W > rect.Width + Eps || o.H > rect.Height + Eps)
                        continue;

                    double leftoverW = rect.Width - o.W;
                    double leftoverH = rect.Height - o.H;
                    double shortSide = Math.Min(leftoverW, leftoverH);
                    double longSide = Math.Max(leftoverW, leftoverH);

                    double score = (rect.Y * 1000)
                                 + (rect.X * 0.1)
                                 + (shortSide * 1)
                                 + (longSide * 0.01)
                                 + (o.Rotated ? 50 : 0);

                    if (score < bestScore - Eps)
                    {
                        bestScore = score;
                        best = new PlacementFit
                        {
                            X = rect.X,
                            Y = rect.Y,
                            Width = o.W,
                            Height = o.H,
                            IsRotated = o.Rotated,
                            Score = score
                        };
                    }
                }
            }

            return best;
        }

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

        private List<RectF> UpdateFreeRects(List<RectF> freeRects, double x, double y, double w, double h)
        {
            var result = new List<RectF>();
            double usedRight = x + w;
            double usedBottom = y + h;

            foreach (var rect in freeRects)
            {
                if (usedRight <= rect.X + Eps || x >= rect.X + rect.Width - Eps ||
                    usedBottom <= rect.Y + Eps || y >= rect.Y + rect.Height - Eps)
                {
                    result.Add(rect);
                    continue;
                }

                if (x > rect.X + Eps)
                    result.Add(new RectF(rect.X, rect.Y, x - rect.X, rect.Height));

                if (usedRight < rect.X + rect.Width - Eps)
                    result.Add(new RectF(usedRight, rect.Y, rect.X + rect.Width - usedRight, rect.Height));

                if (y > rect.Y + Eps)
                    result.Add(new RectF(rect.X, rect.Y, rect.Width, y - rect.Y));

                if (usedBottom < rect.Y + rect.Height - Eps)
                    result.Add(new RectF(rect.X, usedBottom, rect.Width, rect.Y + rect.Height - usedBottom));
            }

            return PruneFreeRects(result);
        }

        private List<RectF> PruneFreeRects(List<RectF> rects)
        {
            if (rects == null || rects.Count == 0) return rects ?? new List<RectF>();

            var kept = new List<RectF>();
            foreach (var r in rects)
            {
                if (r.Width <= Eps || r.Height <= Eps) continue;

                bool contained = false;
                for (int j = 0; j < rects.Count; j++)
                {
                    var b = rects[j];
                    if (ReferenceEquals(r, b)) continue;
                    if (b.Width <= Eps || b.Height <= Eps) continue;

                    if (r.X >= b.X - Eps &&
                        r.Y >= b.Y - Eps &&
                        r.X + r.Width <= b.X + b.Width + Eps &&
                        r.Y + r.Height <= b.Y + b.Height + Eps)
                    {
                        contained = true;
                        break;
                    }
                }

                if (!contained) kept.Add(r);
            }

            return kept;
        }
    }

    // ══════════════════════════════════════════════════════
    // MODEL CLASSES
    // ══════════════════════════════════════════════════════

    public class StockSheet
    {
        public double L { get; set; }
        public double W { get; set; }
        public int Qty { get; set; } = 1;
    }

    public class CutPart
    {
        public string Label { get; set; } = "";
        public double L { get; set; }
        public double W { get; set; }
        public int Qty { get; set; } = 1;
    }

    // ✅ FIX: Added Label property (was missing — caused CS0117 in VM)
    public class PlacedPart
    {
        public string Label { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public double L { get; set; }
        public double W { get; set; }
        public bool Rotated { get; set; }
        public int SheetIndex { get; set; }
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