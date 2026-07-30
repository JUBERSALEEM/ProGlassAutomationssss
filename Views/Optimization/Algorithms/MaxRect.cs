using System;

namespace ProGlassAutomation.Views.Optimization.Algorithms
{
    public class MaxRect
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Area => Width * Height;

        public MaxRect() { }
        public MaxRect(double x, double y, double w, double h)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
        }

        public bool Fits(double w, double h) => Width >= w && Height >= h;
    }
}
