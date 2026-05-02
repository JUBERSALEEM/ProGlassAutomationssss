using System;
using System.Collections.Generic;
using System.IO;

namespace ProGlassAutomation.Services
{
    public class ActivationService
    {
        private static ActivationService? _instance;
        private Dictionary<string, List<string>> _activations;
        private string _filePath = "activations.dat";

        public static ActivationService Instance
        {
            get { return _instance ??= new ActivationService(); }
        }

        private ActivationService()
        {
            _activations = new Dictionary<string, List<string>>();
            LoadActivations();
        }

        public bool CanActivate(string key, string machineId, int maxPCs)
        {
            if (!_activations.ContainsKey(key))
            {
                _activations[key] = new List<string>();
            }

            var activations = _activations[key];

            // Check if this machine is already activated
            if (activations.Contains(machineId))
            {
                return true;
            }

            // Check if max activations reached
            return activations.Count < maxPCs;
        }

        public bool Activate(string key, string machineId)
        {
            if (!_activations.ContainsKey(key))
            {
                _activations[key] = new List<string>();
            }

            var activations = _activations[key];

            if (!activations.Contains(machineId))
            {
                activations.Add(machineId);
                SaveActivations();
            }

            return true;
        }

        public int GetActivationCount(string key)
        {
            if (_activations.ContainsKey(key))
            {
                return _activations[key].Count;
            }
            return 0;
        }

        public bool IsActivated(string key, string machineId)
        {
            if (_activations.ContainsKey(key))
            {
                return _activations[key].Contains(machineId);
            }
            return false;
        }

        public void RemoveActivation(string key, string machineId)
        {
            if (_activations.ContainsKey(key))
            {
                _activations[key].Remove(machineId);
                SaveActivations();
            }
        }

        private void LoadActivations()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var lines = File.ReadAllLines(_filePath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 2)
                        {
                            var key = parts[0];
                            var machines = new List<string>();
                            for (int i = 1; i < parts.Length; i++)
                            {
                                if (!string.IsNullOrEmpty(parts[i]))
                                {
                                    machines.Add(parts[i]);
                                }
                            }
                            _activations[key] = machines;
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
                    var machines = string.Join("|", kvp.Value);
                    lines.Add($"{kvp.Key}|{machines}");
                }
                File.WriteAllLines(_filePath, lines);
            }
            catch { }
        }
    }
}