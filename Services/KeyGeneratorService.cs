using System;
using System.Collections.Generic;

namespace ProGlassAutomation.Services
{
    public class KeyGeneratorService
    {
        private static KeyGeneratorService? _instance;
        private List<LicensePlan> _plans;

        public static KeyGeneratorService Instance
        {
            get { return _instance ??= new KeyGeneratorService(); }
        }

        private KeyGeneratorService()
        {
            InitializePlans();
        }

        private void InitializePlans()
        {
            _plans = new List<LicensePlan>
            {
                new LicensePlan { Id = "WK1", Name = "Weekly Starter", Tag = "WK", Days = 7, MaxPCs = 1, Price = 100, ColorHex = "#F59E0B" },
                new LicensePlan { Id = "WK3", Name = "Weekly Basic", Tag = "WK", Days = 7, MaxPCs = 3, Price = 150, ColorHex = "#F59E0B" },
                new LicensePlan { Id = "WK5", Name = "Weekly Pro", Tag = "WK", Days = 7, MaxPCs = 5, Price = 200, ColorHex = "#F59E0B" },
                new LicensePlan { Id = "MT1", Name = "Monthly Starter", Tag = "MT", Days = 30, MaxPCs = 1, Price = 250, ColorHex = "#3B82F6" },
                new LicensePlan { Id = "MT3", Name = "Monthly Basic", Tag = "MT", Days = 30, MaxPCs = 3, Price = 300, ColorHex = "#3B82F6" },
                new LicensePlan { Id = "MT5", Name = "Monthly Pro", Tag = "MT", Days = 30, MaxPCs = 5, Price = 400, ColorHex = "#3B82F6" },
                new LicensePlan { Id = "6M1", Name = "6M Starter", Tag = "6M", Days = 180, MaxPCs = 1, Price = 1200, ColorHex = "#8B5CF6" },
                new LicensePlan { Id = "6M3", Name = "6M Basic", Tag = "6M", Days = 180, MaxPCs = 3, Price = 1500, ColorHex = "#8B5CF6" },
                new LicensePlan { Id = "6M5", Name = "6M Pro", Tag = "6M", Days = 180, MaxPCs = 5, Price = 2000, ColorHex = "#8B5CF6" },
                new LicensePlan { Id = "YR1", Name = "Yearly Starter", Tag = "YR", Days = 365, MaxPCs = 1, Price = 2000, ColorHex = "#10B981" },
                new LicensePlan { Id = "YR3", Name = "Yearly Basic", Tag = "YR", Days = 365, MaxPCs = 3, Price = 2400, ColorHex = "#10B981" },
                new LicensePlan { Id = "YR5", Name = "Yearly Pro", Tag = "YR", Days = 365, MaxPCs = 5, Price = 3000, ColorHex = "#10B981" },
            };
        }

        public List<LicensePlan> GetAllPlans() => _plans;

        public LicensePlan? GetPlan(string planId) => _plans.Find(p => p.Id == planId);

        public string GenerateKey(string planId)
        {
            var plan = GetPlan(planId);
            if (plan == null) return "";

            string prefix = "PRO";
            string planCode = plan.Id;
            string uniqueId = new Random().Next(100000, 999999).ToString();
            string expiry = DateTime.Now.AddDays(plan.Days).ToString("yyyyMMdd");
            string maxPCs = plan.MaxPCs.ToString();

            string data = $"{planCode}{expiry}{plan.Days}{uniqueId}{maxPCs}";
            int checksum = CalculateChecksum(data);

            return $"{prefix}_{planCode}_{expiry}_{plan.Days}_{uniqueId}_{maxPCs}_{checksum}";
        }

        public int CalculateChecksum(string data)
        {
            int checksum = 0;
            foreach (char c in data)
            {
                if (char.IsDigit(c)) checksum += (c - '0');
            }
            return checksum;
        }

        public bool ValidateKey(string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key)) return false;
                key = key.Trim().ToUpper();

                string[] parts = key.Split('_');
                if (parts.Length != 7) return false;
                if (parts[0] != "PRO") return false;

                string planCode = parts[1];
                string expiry = parts[2];
                string days = parts[3];
                string uniqueId = parts[4];
                string maxPCs = parts[5];
                string checksum = parts[6];

                var plan = GetPlan(planCode);
                if (plan == null) return false;

                if (expiry.Length != 8) return false;
                string formattedDate = $"{expiry.Substring(0, 4)}-{expiry.Substring(4, 2)}-{expiry.Substring(6, 2)}";

                if (!DateTime.TryParse(formattedDate, out DateTime expiryDate))
                    return false;

                if (expiryDate.Date < DateTime.Now.Date)
                    return false;

                if (!int.TryParse(days, out int keyDays)) return false;
                if (keyDays != plan.Days) return false;

                string data = $"{planCode}{expiry}{days}{uniqueId}{maxPCs}";
                int calculatedChecksum = CalculateChecksum(data);

                if (calculatedChecksum.ToString() != checksum)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ValidateKeyWithActivation(string key, string machineId, out string errorMessage)
        {
            errorMessage = "";

            try
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    errorMessage = "License key is empty";
                    return false;
                }

                key = key.Trim().ToUpper();
                string[] parts = key.Split('_');

                if (parts.Length != 7)
                {
                    errorMessage = "Invalid license key format";
                    return false;
                }

                if (parts[0] != "PRO")
                {
                    errorMessage = "Invalid license key prefix";
                    return false;
                }

                string planCode = parts[1];
                string expiry = parts[2];
                string maxPCs = parts[5];

                var plan = GetPlan(planCode);
                if (plan == null)
                {
                    errorMessage = "Invalid license plan";
                    return false;
                }

                if (expiry.Length != 8)
                {
                    errorMessage = "Invalid expiry date";
                    return false;
                }

                string formattedDate = $"{expiry.Substring(0, 4)}-{expiry.Substring(4, 2)}-{expiry.Substring(6, 2)}";
                if (!DateTime.TryParse(formattedDate, out DateTime expiryDate))
                {
                    errorMessage = "Invalid date format";
                    return false;
                }

                if (expiryDate.Date < DateTime.Now.Date)
                {
                    int daysExpired = (int)(DateTime.Now.Date - expiryDate.Date).TotalDays;
                    errorMessage = $"License expired {daysExpired} days ago";
                    return false;
                }

                string data = $"{planCode}{expiry}{parts[3]}{parts[4]}{maxPCs}";
                int calculatedChecksum = CalculateChecksum(data);

                if (calculatedChecksum.ToString() != parts[6])
                {
                    errorMessage = "License checksum verification failed";
                    return false;
                }

                int maxActivations = int.TryParse(maxPCs, out int m) ? m : 1;
                if (!LicenseActivationService.Instance.CanActivate(key, machineId, maxActivations))
                {
                    errorMessage = $"Maximum activations reached ({maxActivations} PCs)";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "License validation error: " + ex.Message;
                return false;
            }
        }

        public bool ActivateKey(string key, string machineId)
        {
            return LicenseActivationService.Instance.Activate(key, machineId);
        }

        public KeyInfo GetKeyInfo(string key)
        {
            var info = new KeyInfo();
            try
            {
                key = key.Trim().ToUpper();
                string[] parts = key.Split('_');
                if (parts.Length != 7) return info;

                var plan = GetPlan(parts[1]);
                if (plan == null) return info;

                info.PlanCode = plan.Id;
                info.PlanTag = plan.Tag;
                info.PlanName = plan.Name;
                info.PlanColor = plan.ColorHex;
                info.Days = plan.Days;
                info.MaxPCs = plan.MaxPCs;
                info.Price = plan.Price;
                info.UniqueId = parts[4];
                info.ExpiryDate = $"{parts[2].Substring(0, 4)}-{parts[2].Substring(4, 2)}-{parts[2].Substring(6, 2)}";
                info.CurrentActivations = LicenseActivationService.Instance.GetActivationCount(key);
                info.PCsRemaining = $"{info.MaxPCs - info.CurrentActivations} / {info.MaxPCs}";

                if (DateTime.TryParse(info.ExpiryDate, out DateTime expiry))
                {
                    info.IsValid = expiry >= DateTime.Now.Date;
                    info.DaysRemaining = (int)(expiry.Date - DateTime.Now.Date).TotalDays;
                    info.IsActivated = LicenseActivationService.Instance.IsActivated(key, MachineIdService.Instance.GetMachineId());
                }
            }
            catch { }
            return info;
        }
    }

    public class LicensePlan
    {
        public string Id { get; set; } = "";
        public string Tag { get; set; } = "";
        public string Name { get; set; } = "";
        public int Days { get; set; } = 0;
        public int MaxPCs { get; set; } = 1;
        public decimal Price { get; set; } = 0;
        public string ColorHex { get; set; } = "#3B82F6";
    }

    public class KeyInfo
    {
        public string PlanCode { get; set; } = "";
        public string PlanTag { get; set; } = "";
        public string PlanName { get; set; } = "";
        public string PlanColor { get; set; } = "#3B82F6";
        public int Days { get; set; } = 0;
        public int MaxPCs { get; set; } = 1;
        public decimal Price { get; set; } = 0;
        public string ExpiryDate { get; set; } = "";
        public string UniqueId { get; set; } = "";
        public bool IsValid { get; set; } = false;
        public bool IsActivated { get; set; } = false;
        public int CurrentActivations { get; set; } = 0;
        public int DaysRemaining { get; set; } = 0;
        public string PCsRemaining { get; set; } = "";
    }
}