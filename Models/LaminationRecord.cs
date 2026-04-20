using System;

namespace ProGlassAutomation.Models
{
    public class LaminationRecord
    {
        public int Id { get; set; }

        public string Thickness1 { get; set; }
        public string Color1 { get; set; }

        public string Thickness2 { get; set; }
        public string Color2 { get; set; }

        public string PVBType { get; set; }

        // ✔ safer as decimal consistency
        public double Result { get; set; }

        public string CreatedAt { get; set; }

        public string DisplayText
        {
            get
            {
                string t1 = Thickness1 ?? "-";
                string c1 = Color1 ?? "-";
                string t2 = Thickness2 ?? "-";
                string c2 = Color2 ?? "-";
                string pvb = PVBType ?? "-";

                return $"{t1} {c1} FT Glass + " +
                       $"{pvb} PVB + " +
                       $"{t2} {c2} FT Glass - " +
                       $"{Result:0.00} AED - {FormatDate()}";
            }
        }

        private string FormatDate()
        {
            if (string.IsNullOrWhiteSpace(CreatedAt))
                return DateTime.Now.ToString("dd-MMMM-yyyy - hh:mmtt");

            if (DateTime.TryParse(CreatedAt, out var d))
                return d.ToString("dd-MMMM-yyyy - hh:mmtt");

            return CreatedAt;
        }
    }
}