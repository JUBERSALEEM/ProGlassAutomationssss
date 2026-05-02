using System;
using System.IO;
using System.Text.Json;

namespace ProGlassAutomation.Services
{
    public class SubscriptionService
    {
        private static SubscriptionService? _instance;
        private string _configPath = "";
        private SubscriptionConfig _config = new SubscriptionConfig();

        public static SubscriptionService Instance
        {
            get { return _instance ??= new SubscriptionService(); }
        }

        private SubscriptionService()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ProGlassAutomation"
            );
            Directory.CreateDirectory(appDataPath);
            _configPath = Path.Combine(appDataPath, "subscription.json");
            LoadConfig();
        }

        public void ReloadConfig()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    _config = JsonSerializer.Deserialize<SubscriptionConfig>(json) ?? new SubscriptionConfig();
                }
            }
            catch
            {
                _config = new SubscriptionConfig();
            }
        }

        private void SaveConfig()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_config, options);
                File.WriteAllText(_configPath, json);
            }
            catch { }
        }

        public bool IsActive()
        {
            if (string.IsNullOrEmpty(_config.LicenseKey)) return false;
            if (!_config.IsActive) return false;

            if (DateTime.TryParse(_config.ExpiryDate, out DateTime expiry))
            {
                return expiry >= DateTime.Now.Date;
            }

            return false;
        }

        public int GetDaysRemaining()
        {
            if (!IsActive()) return 0;

            if (DateTime.TryParse(_config.ExpiryDate, out DateTime expiry))
            {
                int days = (expiry - DateTime.Now.Date).Days;
                return days > 0 ? days : 0;
            }

            return 0;
        }

        public string GetPlanName()
        {
            if (!IsActive()) return "NONE";
            return _config.PlanName;
        }

        public string GetLicenseKey()
        {
            return _config.LicenseKey ?? "";
        }

        public string GetErrorMessage()
        {
            return _config.LastErrorMessage ?? "";
        }

        public bool ActivateLicense(string key)
        {
            try
            {
                key = key.Trim().ToUpper();
                string machineId = MachineIdService.Instance.GetMachineId();

                if (!KeyGeneratorService.Instance.ValidateKeyWithActivation(key, machineId, out string errorMessage))
                {
                    _config.LastErrorMessage = errorMessage;
                    SaveConfig();
                    return false;
                }

                var keyInfo = KeyGeneratorService.Instance.GetKeyInfo(key);

                if (!keyInfo.IsValid)
                {
                    _config.LastErrorMessage = "License key has expired";
                    SaveConfig();
                    return false;
                }

                KeyGeneratorService.Instance.ActivateKey(key, machineId);

                _config.LicenseKey = key;
                _config.PlanId = keyInfo.PlanCode;
                _config.PlanTag = keyInfo.PlanTag;
                _config.PlanName = keyInfo.PlanName;
                _config.ExpiryDate = keyInfo.ExpiryDate;
                _config.ActivatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                _config.IsActive = true;
                _config.UniqueId = keyInfo.UniqueId;
                _config.MaxPCs = keyInfo.MaxPCs;
                _config.CurrentPCs = keyInfo.CurrentActivations;
                _config.LastErrorMessage = "";

                SaveConfig();
                return true;
            }
            catch (Exception ex)
            {
                _config.LastErrorMessage = "Activation failed: " + ex.Message;
                SaveConfig();
                return false;
            }
        }

        public void DeactivateLicense()
        {
            string key = _config.LicenseKey;
            string machineId = MachineIdService.Instance.GetMachineId();

            if (!string.IsNullOrEmpty(key))
            {
                LicenseActivationService.Instance.RemoveActivation(key, machineId);
            }

            _config.LicenseKey = "";
            _config.PlanId = "";
            _config.PlanTag = "";
            _config.PlanName = "NONE";
            _config.ExpiryDate = "";
            _config.ActivatedDate = "";
            _config.IsActive = false;
            _config.UniqueId = "";
            _config.MaxPCs = 0;
            _config.CurrentPCs = 0;
            _config.LastErrorMessage = "";
            SaveConfig();
        }

        public SubscriptionStatus GetStatus()
        {
            string machineId = MachineIdService.Instance.GetMachineId();
            bool isOnThisMachine = false;
            int currentPCs = _config.CurrentPCs;

            if (!string.IsNullOrEmpty(_config.LicenseKey))
            {
                var keyInfo = KeyGeneratorService.Instance.GetKeyInfo(_config.LicenseKey);
                isOnThisMachine = keyInfo.IsActivated;
                currentPCs = keyInfo.CurrentActivations;
            }

            var status = new SubscriptionStatus
            {
                IsActive = IsActive(),
                DaysRemaining = GetDaysRemaining(),
                PlanName = GetPlanName(),
                PlanId = _config.PlanId,
                PlanTag = _config.PlanTag,
                ExpiryDate = _config.ExpiryDate,
                ActivatedDate = _config.ActivatedDate,
                UniqueId = _config.UniqueId,
                MaxPCs = _config.MaxPCs,
                CurrentPCs = currentPCs,
                IsActivatedOnThisPC = isOnThisMachine,
                ErrorMessage = _config.LastErrorMessage
            };

            return status;
        }

        public void RefreshStatus()
        {
            LoadConfig();
        }

        public string GetStatusText()
        {
            if (!IsActive())
            {
                string error = GetErrorMessage();
                if (!string.IsNullOrEmpty(error))
                {
                    return $"❌ {error}";
                }
                return "⚠️ No active subscription";
            }

            int days = GetDaysRemaining();
            string daysText = days == 1 ? "1 day" : $"{days} days";

            string pcInfo = $"{_config.CurrentPCs} / {_config.MaxPCs} PCs";

            if (days <= 7)
            {
                return $"⚠️ Expires in {daysText} ({pcInfo})";
            }

            return $"✅ {GetPlanName()} - {daysText} left ({pcInfo})";
        }

        public int GetMaxPCs()
        {
            return _config.MaxPCs;
        }

        public int GetCurrentPCs()
        {
            return _config.CurrentPCs;
        }

        public string GetUniqueId()
        {
            return _config.UniqueId;
        }
    }

    public class SubscriptionConfig
    {
        public string LicenseKey { get; set; } = "";
        public string PlanId { get; set; } = "";
        public string PlanTag { get; set; } = "";
        public string PlanName { get; set; } = "NONE";
        public string ExpiryDate { get; set; } = "";
        public string ActivatedDate { get; set; } = "";
        public bool IsActive { get; set; } = false;
        public string UniqueId { get; set; } = "";
        public int MaxPCs { get; set; } = 0;
        public int CurrentPCs { get; set; } = 0;
        public string LastErrorMessage { get; set; } = "";
    }

    public class SubscriptionStatus
    {
        public bool IsActive { get; set; }
        public int DaysRemaining { get; set; }
        public string PlanName { get; set; } = "";
        public string PlanId { get; set; } = "";
        public string PlanTag { get; set; } = "";
        public string ExpiryDate { get; set; } = "";
        public string ActivatedDate { get; set; } = "";
        public string UniqueId { get; set; } = "";
        public int MaxPCs { get; set; } = 0;
        public int CurrentPCs { get; set; } = 0;
        public bool IsActivatedOnThisPC { get; set; } = false;
        public string ErrorMessage { get; set; } = "";
    }
}