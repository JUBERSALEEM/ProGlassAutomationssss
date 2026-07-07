using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ProGlassAutomation.Services
{
    public class PaymentService
    {
        private static PaymentService? _instance;
        private static readonly object _lock = new object();

        // Payment Configuration
        private string _paypalClientId = "YOUR_PAYPAL_CLIENT_ID";
        private string _paypalSecret = "YOUR_PAYPAL_SECRET";
        private string _paypalSandboxUrl = "https://api.sandbox.paypal.com";
        private string _paypalLiveUrl = "https://api.paypal.com";

        private string _stripeApiKey = "YOUR_STRIPE_API_KEY";

        private string _razorpayKeyId = "YOUR_RAZORPAY_KEY_ID";
        private string _razorpayKeySecret = "YOUR_RAZORPAY_KEY_SECRET";

        private bool _isSandbox = true; // Set to false for live payments

        public static PaymentService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new PaymentService();
                    }
                }
                return _instance;
            }
        }

        private PaymentService()
        {
            LoadPaymentConfig();
        }

        private void LoadPaymentConfig()
        {
            // Load from config file or environment variables
            try
            {
                string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "payment.json");
                if (System.IO.File.Exists(configPath))
                {
                    string json = System.IO.File.ReadAllText(configPath);
                    var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                    if (config != null)
                    {
                        if (config.ContainsKey("PAYPAL_CLIENT_ID"))
                            _paypalClientId = config["PAYPAL_CLIENT_ID"];
                        if (config.ContainsKey("PAYPAL_SECRET"))
                            _paypalSecret = config["PAYPAL_SECRET"];
                        if (config.ContainsKey("STRIPE_API_KEY"))
                            _stripeApiKey = config["STRIPE_API_KEY"];
                        if (config.ContainsKey("RAZORPAY_KEY_ID"))
                            _razorpayKeyId = config["RAZORPAY_KEY_ID"];
                        if (config.ContainsKey("RAZORPAY_KEY_SECRET"))
                            _razorpayKeySecret = config["RAZORPAY_KEY_SECRET"];
                        if (config.ContainsKey("IS_SANDBOX"))
                            _isSandbox = config["IS_SANDBOX"] == "false";
                    }
                }
            }
            catch { }
        }

        public class PaymentPlan
        {
            public string PlanId { get; set; } = "";
            public string Name { get; set; } = "";
            public decimal Amount { get; set; }
            public string Currency { get; set; } = "USD";
            public int Days { get; set; }
            public string PlanTag { get; set; } = "";
        }

        public List<PaymentPlan> GetAvailablePlans()
        {
            return new List<PaymentPlan>
            {
                new PaymentPlan
                {
                    PlanId = "weekly",
                    Name = "Weekly Plan",
                    Amount = 20, // AED
                    Currency = "AED",
                    Days = 7,
                    PlanTag = "WK"
                },
                new PaymentPlan
                {
                    PlanId = "monthly",
                    Name = "Monthly Plan",
                    Amount = 60, // AED
                    Currency = "AED",
                    Days = 30,
                    PlanTag = "MT"
                },
                new PaymentPlan
                {
                    PlanId = "6months",
                    Name = "6 Months Plan",
                    Amount = 300, // AED
                    Currency = "AED",
                    Days = 180,
                    PlanTag = "6M"
                },
                new PaymentPlan
                {
                    PlanId = "yearly",
                    Name = "Yearly Plan",
                    Amount = 500, // AED
                    Currency = "AED",
                    Days = 365,
                    PlanTag = "YR"
                }
            };
        }

        public string GenerateLicenseKey(string planTag)
        {
            string prefix = "PROGLASS";
            string timestamp = DateTime.Now.ToString("yyyyMMdd");
            Random random = new Random();
            string part1 = random.Next(1000, 9999).ToString();
            string part2 = random.Next(1000, 9999).ToString();

            return $"{prefix}-{planTag}-{timestamp}-{part1}-{part2}";
        }

        // ========== PAYPAL PAYMENT ==========

        public async Task<string> CreatePaypalPayment(decimal amount, string planTag)
        {
            try
            {
                string accessToken = await GetPaypalAccessToken();
                if (string.IsNullOrEmpty(accessToken))
                    return "";

                string baseUrl = _isSandbox ? _paypalSandboxUrl : _paypalLiveUrl;

                string orderData = JsonConvert.SerializeObject(new
                {
                    intent = "CAPTURE",
                    purchase_units = new[]
                    {
                        new
                        {
                            amount = new
                            {
                                currency_code = "AED",
                                                            value = amount.ToString("F2")
                            },
                            description = $"ProGlass ERP - {planTag} Plan Activation"
                        }
                    }
                });

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                    var content = new StringContent(orderData, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync($"{baseUrl}/v2/checkout/orders", content);

                    string result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var order = JsonConvert.DeserializeObject<dynamic>(result);
                        string approvalUrl = "";
                        if (order?.links != null)
                        {
                            foreach (var link in order.links)
                            {
                                if (link?.rel == "approve")
                                {
                                    approvalUrl = link?.href ?? "";
                                    break;
                                }
                            }
                        }
                        return approvalUrl;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"PayPal Error: {ex.Message}");
            }
            return "";
        }

        private async Task<string> GetPaypalAccessToken()
        {
            try
            {
                string baseUrl = _isSandbox ? _paypalSandboxUrl : _paypalLiveUrl;

                using (HttpClient client = new HttpClient())
                {
                    string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_paypalClientId}:{_paypalSecret}"));
                    client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");

                    var content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", "client_credentials")
                    });

                    var response = await client.PostAsync($"{baseUrl}/v1/oauth2/token", content);
                    string result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var token = JsonConvert.DeserializeObject<dynamic>(result);
                        // Guard against possible nulls in the deserialized token
                        try
                        {
                            return token?.access_token?.ToString() ?? string.Empty;
                        }
                        catch
                        {
                            return string.Empty;
                        }
                    }
                }
            }
            catch { }
            return "";
        }

        // ========== STRIPE PAYMENT ==========

        public string CreateStripeCheckoutSession(decimal amount, string planTag)
        {
            try
            {
                // For Stripe, you typically need server-side code
                // This creates a checkout session URL

                long amountCents = (long)(amount * 100);

                string sessionData = $"amount={amountCents}&currency=aed&product=ProGlass-{planTag}";

                // In production, call your backend API to create Stripe session
                // For demo, return a simulated URL
                return $"https://checkout.stripe.com/pay/stripe_demo?{sessionData}";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Stripe Error: {ex.Message}");
            }
            return "";
        }

        // ========== RAZORPAY PAYMENT ==========

        public string CreateRazorpayOrder(decimal amount, string planTag)
        {
            try
            {
                // Razorpay integration
                // In production, call your backend API to create Razorpay order

                string orderData = $"amount={amount * 100}&currency=AED&receipt=pg_{planTag}_{DateTime.Now.Ticks}";

                // Return Razorpay checkout URL
                return $"https://razorpay.com/pay?key={_razorpayKeyId}&amount={amount * 100}&currency=AED&name=ProGlass ERP&description={planTag} Plan";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Razorpay Error: {ex.Message}");
            }
            return "";
        }

        // ========== MANUAL PAYMENT / KEY GENERATION ==========

        public string GenerateActivationKey(string planTag)
        {
            return GenerateLicenseKey(planTag);
        }

        public bool ValidateKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            key = key.Trim().ToUpper();

            if (!key.StartsWith("PROGLASS-")) return false;

            string[] parts = key.Split('-');
            if (parts.Length != 5) return false;

            return parts[0] == "PROGLASS" &&
                   parts[1].Length == 2 &&
                   parts[2].Length == 8 &&
                   parts[3].Length == 4 &&
                   parts[4].Length == 4;
        }

        public int GetDaysFromPlanTag(string planTag)
        {
            switch (planTag.ToUpper())
            {
                case "WK": return 7;
                case "MT": return 30;
                case "6M": return 180;
                case "YR": return 365;
                default: return 0;
            }
        }
    }
}