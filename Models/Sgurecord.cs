using System;

namespace ProGlassAutomation.Models
{
    public class SguRecord
    {
        public int Id { get; set; }

        public string Thickness { get; set; }
        public string Color { get; set; }

        public double Result { get; set; }

        public string CreatedAt { get; set; }

        public string DisplayText =>
            $"{Thickness} {Color} SGU Last Price - {Result:0.00}   |   {CreatedAt}";
    }
}