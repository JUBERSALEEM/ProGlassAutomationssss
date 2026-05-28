// File: Services/DataService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Services
{
    public class DataService
    {
        private readonly string _dataFilePath;

        public DataService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ProGlassAutomation");

            Directory.CreateDirectory(appDataPath);
            _dataFilePath = Path.Combine(appDataPath, "proformaInvoices.json");
        }

        public async Task<List<ProformaInvoiceModel>> LoadInvoicesAsync()
        {
            try
            {
                if (!File.Exists(_dataFilePath))
                    return new List<ProformaInvoiceModel>();

                var json = await File.ReadAllTextAsync(_dataFilePath);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<List<ProformaInvoiceModel>>(json)
                       ?? new List<ProformaInvoiceModel>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
                return new List<ProformaInvoiceModel>();
            }
        }

        public List<ProformaInvoiceModel> LoadInvoices()
        {
            try
            {
                if (!File.Exists(_dataFilePath))
                    return new List<ProformaInvoiceModel>();

                var json = File.ReadAllText(_dataFilePath);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<List<ProformaInvoiceModel>>(json)
                       ?? new List<ProformaInvoiceModel>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
                return new List<ProformaInvoiceModel>();
            }
        }

        public void SaveInvoices(List<ProformaInvoiceModel> invoices)
        {
            try
            {
                var settings = new Newtonsoft.Json.JsonSerializerSettings
                {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(invoices, Newtonsoft.Json.Formatting.Indented, settings);
                File.WriteAllText(_dataFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving data: {ex.Message}");
                throw;
            }
        }

        public async Task SaveInvoicesAsync(List<ProformaInvoiceModel> invoices)
        {
            try
            {
                var settings = new Newtonsoft.Json.JsonSerializerSettings
                {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(invoices, Newtonsoft.Json.Formatting.Indented, settings);
                await File.WriteAllTextAsync(_dataFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving data: {ex.Message}");
                throw;
            }
        }

        public string GetDataFilePath() => _dataFilePath;
        public bool DataFileExists() => File.Exists(_dataFilePath);

        public void ClearData()
        {
            if (File.Exists(_dataFilePath))
            {
                File.Delete(_dataFilePath);
            }
        }
    }
}