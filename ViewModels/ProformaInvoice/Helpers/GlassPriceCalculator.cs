using System;

namespace ProGlassAutomation.ViewModels.ProformaInvoice.Helpers
{
    public class GlassPriceCalculator
    {
        public double CalculateSGU(double sheetPrice, double cutting, double tempering, double wasteFactor, double profitPercent, string thickness, string color, string workType, out string description, out string summary)
        {
            double step1 = sheetPrice / wasteFactor;
            double step2 = step1 + cutting + tempering;
            double final = step2 * (1 + profitPercent / 100.0);
            description = $"{thickness}mm {color} {workType}";
            summary = $"({sheetPrice} / {wasteFactor:F2}) + {cutting} + {tempering} = {step2:F2} × {1 + profitPercent / 100.0:F2} = {final:F2}";
            return Math.Round(final, 2);
        }

        public double CalculateDGU(double outerPrice, double innerPrice, double aspPrice, double wasteFactor, double profitPercent, string outerThickness, string outerColor, string workType, string spacerThickness, string spacerType, bool includeUInsert, string innerThickness, string innerColor, out string description, out string summary)
        {
            double glassTotal = outerPrice + innerPrice;
            double step1 = glassTotal / wasteFactor;
            double step2 = step1 + aspPrice;
            double final = step2 * (1 + profitPercent / 100.0);
            string uInsertText = includeUInsert ? " with U-Insert" : "";
            description = $"{outerThickness}mm {outerColor} {workType} + {spacerThickness}mm {spacerType} ASP{uInsertText} + {innerThickness}mm {innerColor} {workType}";
            summary = $"(({outerPrice} + {innerPrice}) / {wasteFactor:F2}) + {aspPrice} = {step2:F2} × {1 + profitPercent / 100.0:F2} = {final:F2}";
            return Math.Round(final, 2);
        }

        public double CalculateLAM(double outerPrice, double pvbPrice, double innerPrice, double cutting, double tempering, double wasteFactor, double profitPercent, string outerThickness, string outerColor, string workType, string pvbThickness, string pvbColor, string innerThickness, string innerColor, out string description, out string summary)
        {
            double glassTotal = outerPrice + pvbPrice + innerPrice;
            double step1 = glassTotal / wasteFactor;
            double step2 = step1 + cutting + tempering;
            double final = step2 * (1 + profitPercent / 100.0);
            description = $"{outerThickness}mm {outerColor} {workType} + {pvbThickness}mm PVB ({pvbColor}) + {innerThickness}mm {innerColor} {workType}";
            summary = $"({outerPrice} + {pvbPrice} + {innerPrice}) / {wasteFactor:F2} + {cutting} + {tempering} = {step2:F2} × {1 + profitPercent / 100.0:F2} = {final:F2}";
            return Math.Round(final, 2);
        }
    }
}
