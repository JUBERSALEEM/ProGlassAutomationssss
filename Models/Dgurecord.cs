using System;

namespace ProGlassAutomation.Models
{
    public class DguRecord
    {
        public int Id { get; set; }

        public string? Thickness1 { get; set; }
        public string? Color1 { get; set; }

        public string? Thickness2 { get; set; }
        public string? Color2 { get; set; }

        public string? Spacer { get; set; }

        public double Result { get; set; }

        public string? CreatedAt { get; set; }

        // ================= FINAL DISPLAY FORMAT =================

        public string DisplayText =>
            $"{Thickness1} {Color1} FT Glass + {Spacer} ASP + {Thickness2} {Color2} FT Glass - " +
            $"{Result:0.00} AED - {FormatDateTime(CreatedAt)}";

        // ================= DATE FORMAT FIX =================

        private string FormatDateTime(string dt)
        {
            if (DateTime.TryParse(dt, out var d))
            {
                return d.ToString("dd-MMMM-yyyy - hh:mm tt");
            }

            return dt;
        }
    }
}