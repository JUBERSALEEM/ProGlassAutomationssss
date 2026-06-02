using ProGlassAutomation.Data.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProGlassAutomation.Data
{
    public class DailyWorkRepository : IDailyWorkRepository
    {
        // PATCH 03: Repository Pattern - abstracts DB access from ViewModel
        // DbHelper used internally - OK (PATCH 04 intent: VM doesn't call DbHelper)

        private List<DbDailyWork> GetAllData()
        {
            try
            {
                var allWorks = DbHelper.GetAllDailyWork();
                if (allWorks == null)
                    return new List<DbDailyWork>();

                var result = new List<DbDailyWork>();
                foreach (var w in allWorks)
                {
                    result.Add(new DbDailyWork
                    {
                        Id = w.Id,
                        Date = w.Date,
                        UpdateDate = w.UpdateDate,
                        Company = w.Company,
                        PINumber = w.PINumber,
                        CustomerReference = w.CustomerReference,
                        TypeOfWork = w.TypeOfWork,
                        ProductionStatus = w.ProductionStatus,
                        DailyReportStatus = w.DailyReportStatus,
                        Qty = w.Qty,
                        SQM = w.SQM,
                        Status = w.Status,
                        Salesman = w.Salesman,
                        Color = w.Color,
                        Notes = w.Notes,
                        CreatedDate = w.CreatedDate
                    });
                }
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DailyWorkRepository.GetAllData error: {ex.Message}");
                return new List<DbDailyWork>();
            }
        }

        public Task<List<DbDailyWork>> GetAllAsync(CancellationToken ct = default)
        {
            return Task.FromResult(GetAllData());
        }

        public Task<DbDailyWork?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var data = GetAllData();
            var item = data.FirstOrDefault(w => w.Id == id);
            return Task.FromResult<DbDailyWork?>(item);
        }

        public Task<int> InsertAsync(DbDailyWork entity, CancellationToken ct = default)
        {
            var dbWork = new Database.DailyWork
            {
                Id = entity.Id,
                Date = entity.Date,
                UpdateDate = entity.UpdateDate,
                Company = entity.Company,
                PINumber = entity.PINumber,
                CustomerReference = entity.CustomerReference,
                TypeOfWork = entity.TypeOfWork,
                ProductionStatus = entity.ProductionStatus,
                DailyReportStatus = entity.DailyReportStatus,
                Qty = entity.Qty,
                SQM = entity.SQM,
                Status = entity.Status,
                Salesman = entity.Salesman,
                Color = entity.Color,
                Notes = entity.Notes,
                CreatedDate = entity.CreatedDate
            };
            DbHelper.SaveDailyWork(dbWork);
            return Task.FromResult(entity.Id);
        }

        public Task UpdateAsync(DbDailyWork entity, CancellationToken ct = default)
        {
            var dbWork = new Database.DailyWork
            {
                Id = entity.Id,
                Date = entity.Date,
                UpdateDate = entity.UpdateDate,
                Company = entity.Company,
                PINumber = entity.PINumber,
                CustomerReference = entity.CustomerReference,
                TypeOfWork = entity.TypeOfWork,
                ProductionStatus = entity.ProductionStatus,
                DailyReportStatus = entity.DailyReportStatus,
                Qty = entity.Qty,
                SQM = entity.SQM,
                Status = entity.Status,
                Salesman = entity.Salesman,
                Color = entity.Color,
                Notes = entity.Notes,
                CreatedDate = entity.CreatedDate
            };
            DbHelper.UpdateDailyWork(dbWork);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id, CancellationToken ct = default)
        {
            DbHelper.DeleteDailyWork(id);
            return Task.CompletedTask;
        }

        public Task<bool> IsDuplicateAsync(string customerReference, string piNumber, int? excludeId, CancellationToken ct = default)
        {
            var result = DbHelper.IsDuplicateDailyWork(customerReference, piNumber, excludeId ?? 0);
            return Task.FromResult(result);
        }

        public Task<List<DbDailyWork>> GetByCompanyAsync(string company, CancellationToken ct = default)
        {
            var data = GetAllData()
                .Where(w => w.Company == company)
                .ToList();
            return Task.FromResult(data);
        }

        public Task<List<DbDailyWork>> GetByPINumberAsync(string piNumber, CancellationToken ct = default)
        {
            var data = GetAllData()
                .Where(w => w.PINumber == piNumber)
                .ToList();
            return Task.FromResult(data);
        }

        public Task<List<DbDailyWork>> GetByStatusAsync(string status, CancellationToken ct = default)
        {
            var data = GetAllData()
                .Where(w => w.Status == status)
                .ToList();
            return Task.FromResult(data);
        }

        public Task<List<DbDailyWork>> GetByDateRangeAsync(DateTime start, DateTime end, CancellationToken ct = default)
        {
            var data = GetAllData()
                .Where(w => w.Date >= start && w.Date <= end)
                .ToList();
            return Task.FromResult(data);
        }
    }
}