using System;
using System.Collections.Generic;

namespace ProGlassAutomation.Views.Optimization.Algorithms
{
    public static class SmartRotation
    {
        private static readonly string[] RestrictedKeywords = new[]
        {
            "Reeded", "Rain", "Satin", "Fluted", "Grain", "Linear", "Texture", "Design", "Direction"
        };

        public static bool IsRotationAllowed(CutPart part)
        {
            if (part == null) return false;

            // If explicit rotation is disabled for the part
            if (!part.Rot) return false;

            // Check for patterned, textured, or linear glass items in the reference
            if (!string.IsNullOrEmpty(part.Ref))
            {
                foreach (var keyword in RestrictedKeywords)
                {
                    if (part.Ref.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return false; // Rotations disabled to preserve texture alignment consistency
                    }
                }
            }

            return true;
        }

        public static List<Orientation> GetOrientations(CutPart part)
        {
            var orientations = new List<Orientation>();
            // Normal orientation is always allowed
            orientations.Add(new Orientation(part.L, part.W, false));

            if (IsRotationAllowed(part))
            {
                // Rotated orientation is also allowed
                orientations.Add(new Orientation(part.W, part.L, true));
            }

            return orientations;
        }
    }

    public struct Orientation
    {
        public double Width { get; }
        public double Height { get; }
        public bool IsRotated { get; }

        public Orientation(double width, double height, bool isRotated)
        {
            Width = width;
            Height = height;
            IsRotated = isRotated;
        }
    }
}
