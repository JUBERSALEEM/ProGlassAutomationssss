namespace ProGlassAutomation.Services.DGU
{
    public static class AspPricingService
    {
        public static double GetAspPrice(string thickness, string aspType)
        {
            double price = 45;

            switch (thickness)
            {
                case "6mm":
                case "8mm":
                case "10mm":
                case "12mm":
                    price = 45;
                    break;

                case "15mm":
                case "16mm":
                    price = 50;
                    break;

                case "19mm":
                case "20mm":
                    price = 55;
                    break;

                case "24mm":
                    price = 60;
                    break;
            }

            if (!string.IsNullOrEmpty(aspType) &&
                aspType.ToLower().Contains("black"))
            {
                price += 5;
            }

            return price;
        }
    }
}