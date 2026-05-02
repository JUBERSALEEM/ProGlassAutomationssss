using System;
using System.Collections.Generic;
using System.IO;

namespace ProGlassAutomation.Services
{
    public class LicenseActivationService
    {
        private static LicenseActivationService? _instance;
        private Dictionary<string, List<ActivationRecord>> _activations = new Dictionary<string, List<ActivationRecord>>();
        private string _activationFile = "license_activations.txt";

        public static LicenseActivationService Instance
        {
            get { return _instance ??= new LicenseActivationService(); }
        }

        private LicenseActivationService()
        {
            LoadActivations();
        }

        public bool CanActivate(string key, string machineId, int maxActivations)
        {
            if (_activations.ContainsKey(key))
            {
                var records = _activations[key];

                var existing = records.Find(r => r.MachineId == machineId);
                if (existing != null) return true;

                if (records.Count >= maxActivations) return false;
            }

            return true;
        }

        public bool Activate(string key, string machineId)
        {
            if (!_activations.ContainsKey(key))
            {
                _activations[key] = new List<ActivationRecord>();
            }
            else
            {
                var existing = _activations[key].Find(r => r.MachineId == machineId);
                if (existing != null)
                {
                    existing.LastActivated = DateTime.Now;
                    SaveActivations();
                    return true;
                }
            }

            var record = new ActivationRecord
            {
                MachineId = machineId,
                ActivatedDate = DateTime.Now,
                LastActivated = DateTime.Now
            };

            _activations[key].Add(record);
            SaveActivations();
            return true;
        }

        public bool IsActivated(string key, string machineId)
        {
            if (_activations.ContainsKey(key))
            {
                return _activations[key].Exists(r => r.MachineId == machineId);
            }
            return false;
        }

        public int GetActivationCount(string key)
        {
            return _activations.ContainsKey(key) ? _activations[key].Count : 0;
        }

        public List<ActivationRecord> GetActivations(string key)
        {
            return _activations.ContainsKey(key) ? _activations[key] : new List<ActivationRecord>();
        }

        public void RemoveActivation(string key, string machineId)
        {
            if (_activations.ContainsKey(key))
            {
                _activations[key].RemoveAll(r => r.MachineId == machineId);
                if (_activations[key].Count == 0)
                {
                    _activations.Remove(key);
                }
                SaveActivations();
            }
        }

        public void ClearAllActivations()
        {
            _activations.Clear();
            SaveActivations();
        }

        private void LoadActivations()
        {
            try
            {
                if (File.Exists(_activationFile))
                {
                    var lines = File.ReadAllLines(_activationFile);
                    _activations.Clear();

                    foreach (var line in lines)
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 3)
                        {
                            string k = parts[0];
                            string mId = parts[1];
                            DateTime activatedDate = DateTime.TryParse(parts[2], out DateTime d1) ? d1 : DateTime.Now;
                            DateTime lastActivated = parts.Length > 3 && DateTime.TryParse(parts[3], out DateTime d2) ? d2 : activatedDate;

                            if (!_activations.ContainsKey(k))
                            {
                                _activations[k] = new List<ActivationRecord>();
                            }

                            _activations[k].Add(new ActivationRecord
                            {
                                MachineId = mId,
                                ActivatedDate = activatedDate,
                                LastActivated = lastActivated
                            });
                        }
                    }
                }
            }
            catch { }
        }

        private void SaveActivations()
        {
            try
            {
                var lines = new List<string>();
                foreach (var kvp in _activations)
                {
                    foreach (var record in kvp.Value)
                    {
                        lines.Add($"{kvp.Key}|{record.MachineId}|{record.ActivatedDate:yyyy-MM-dd HH:mm}|{record.LastActivated:yyyy-MM-dd HH:mm}");
                    }
                }
                File.WriteAllLines(_activationFile, lines);
            }
            catch { }
        }
    }

    public class ActivationRecord
    {
        public string MachineId { get; set; } = "";
        public DateTime ActivatedDate { get; set; } = DateTime.Now;
        public DateTime LastActivated { get; set; } = DateTime.Now;
    }
}