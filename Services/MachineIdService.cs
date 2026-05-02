using System;
using System.Security.Cryptography;
using System.Text;

namespace ProGlassAutomation.Services
{
    public class MachineIdService
    {
        private static MachineIdService? _instance;
        private string? _cachedMachineId;

        public static MachineIdService Instance
        {
            get { return _instance ??= new MachineIdService(); }
        }

        private MachineIdService() { }

        public string GetMachineId()
        {
            if (_cachedMachineId != null) return _cachedMachineId;

            try
            {
                string cpuInfo = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "CPU";
                string machineName = Environment.MachineName;
                string userName = Environment.UserName;
                string domainName = Environment.UserDomainName;

                string combined = $"{cpuInfo}|{machineName}|{userName}|{domainName}";
                _cachedMachineId = HashString(combined);
                return _cachedMachineId;
            }
            catch
            {
                _cachedMachineId = HashString($"{Environment.MachineName}-{Environment.UserName}-{Guid.NewGuid()}");
                return _cachedMachineId;
            }
        }

        public string GetShortMachineId()
        {
            string fullId = GetMachineId();
            return fullId.Length >= 8 ? fullId.Substring(0, 8).ToUpper() : fullId.ToUpper();
        }

        private string HashString(string input)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").Substring(0, 32);
        }
    }
}