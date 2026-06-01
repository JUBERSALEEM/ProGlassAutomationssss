using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ProGlassAutomation.Helpers
{
    public static class ColorExtractor
    {
        public static string ExtractColors(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return string.Empty;

            var found = new List<string>();

            // Split by "+" to handle each layer
            var layers = description.Split('+');

            for (int i = 0; i < layers.Length; i++)
            {
                var layer = layers[i].Trim();

                // Skip non-glass layers (asp, spacer, pvb, etc)
                if (IsNonGlassLayer(layer))
                    continue;

                // Find "Xmm [ColorName]" pattern - DYNAMIC extraction
                var match = Regex.Match(layer, @"^(\d+)\s*mm\s+(.+)$", RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    var thickness = match.Groups[1].Value.Trim();
                    var colorPart = match.Groups[2].Value.Trim();

                    // Clean the color name (remove glass types only)
                    colorPart = CleanColor(colorPart);

                    // Use any non-empty color name (DYNAMIC)
                    if (!string.IsNullOrWhiteSpace(colorPart))
                    {
                        var formatted = $"{thickness}mm {colorPart}";
                        if (!found.Contains(formatted))
                            found.Add(formatted);
                    }
                }
            }

            // Fallback: if nothing found, look for any color word in description
            if (found.Count == 0)
            {
                var fallbackColor = ExtractAnyColor(description);
                if (!string.IsNullOrEmpty(fallbackColor))
                    found.Add(fallbackColor);
            }

            if (found.Count == 0)
                return "Clear";

            return string.Join(" + ", found);
        }

        private static bool IsNonGlassLayer(string layer)
        {
            var nonGlass = new[] { "asp", "spacer", "pvb", "u-insert", "uinsert", "air" };
            foreach (var term in nonGlass)
            {
                if (layer.Contains(term, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string ExtractAnyColor(string description)
        {
            // Look for any word after "mm" that might be a color
            var match = Regex.Match(description, @"mm\s+(\w+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var color = match.Groups[1].Value.Trim();
                // Clean it
                color = CleanColor(color);
                if (!string.IsNullOrWhiteSpace(color))
                    return color;
            }
            return "Clear";
        }

        private static string CleanColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color))
                return "";

            // Only remove glass type words, keep everything else (including Brown, etc)
            var skip = new[] { "ft", "glass", "tempered", "annealed", "laminated", "toughened", "float", "coated" };
            var cleaned = color;

            foreach (var term in skip)
            {
                cleaned = Regex.Replace(cleaned, $@"\b{term}\b", "", RegexOptions.IgnoreCase);
            }

            cleaned = cleaned.Trim();

            if (string.IsNullOrEmpty(cleaned))
                return color.Trim();

            return cleaned.Replace("  ", " ");
        }
    }
}