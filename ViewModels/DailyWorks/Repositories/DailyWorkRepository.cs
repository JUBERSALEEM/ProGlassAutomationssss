using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProGlassAutomation.Models;
using DbDailyWork = ProGlassAutomation.Data.DbDailyWork;

namespace ProGlassAutomation.ViewModels.DailyWorks.Repositories
{
    public interface IDailyWorkRepository
    {
        Task<List<DailyWorkModel>> GetAllAsync();
        Task<int> InsertAsync(DbDailyWork entity);
        Task UpdateAsync(DbDailyWork entity);
        Task DeleteAsync(int id);
    }

    public class DailyWorkRepository : IDailyWorkRepository
    {
        public Task<List<DailyWorkModel>> GetAllAsync()
        {
            return Task.Run(() =>
            {
                var dbRecords = ProGlassAutomation.Data.Database.DbHelper.GetAllDailyWork();
                var models = new List<DailyWorkModel>();

                foreach (var record in dbRecords)
                {
                    models.Add(new DailyWorkModel
                    {
                        Id = record.Id,
                        Date = record.Date,
                        Company = record.Company,
                        PiNumber = record.PINumber,
                        CustomerReference = record.CustomerReference,
                        TypeOfWork = record.TypeOfWork,
                        ProductionStatus = record.ProductionStatus,
                        Qty = record.Qty,
                        Sqm = record.SQM,
                        Status = record.Status,
                        Salesman = record.Salesman,
                        Color = record.Color,
                        Notes = record.Notes,
                        CreatedDate = record.CreatedDate,
                        UpdateDate = record.UpdateDate
                    });
                }

                return models;
            });
        }

        public Task<int> InsertAsync(DbDailyWork entity)
        {
            return Task.Run(() =>
            {
                var dbRecord = new ProGlassAutomation.Data.Database.DailyWork
                {
                    Date = entity.Date,
                    UpdateDate = entity.UpdateDate,
                    Company = entity.Company,
                    PINumber = entity.PINumber,
                    CustomerReference = entity.CustomerReference,
                    TypeOfWork = entity.TypeOfWork,
                    ProductionStatus = entity.ProductionStatus,
                    Qty = entity.Qty,
                    SQM = entity.SQM,
                    Status = entity.Status,
                    Salesman = entity.Salesman,
                    Color = entity.Color,
                    Notes = entity.Notes,
                    CreatedDate = entity.CreatedDate
                };

                ProGlassAutomation.Data.Database.DbHelper.SaveDailyWork(dbRecord);
                return dbRecord.Id;
            });
        }

        public Task UpdateAsync(DbDailyWork entity)
        {
            return Task.Run(() =>
            {
                var dbRecord = new ProGlassAutomation.Data.Database.DailyWork
                {
                    Id = entity.Id,
                    Date = entity.Date,
                    UpdateDate = entity.UpdateDate,
                    Company = entity.Company,
                    PINumber = entity.PINumber,
                    CustomerReference = entity.CustomerReference,
                    TypeOfWork = entity.TypeOfWork,
                    ProductionStatus = entity.ProductionStatus,
                    Qty = entity.Qty,
                    SQM = entity.SQM,
                    Status = entity.Status,
                    Salesman = entity.Salesman,
                    Color = entity.Color,
                    Notes = entity.Notes,
                    CreatedDate = entity.CreatedDate
                };

                ProGlassAutomation.Data.Database.DbHelper.UpdateDailyWork(dbRecord);
            });
        }

        public Task DeleteAsync(int id)
        {
            return Task.Run(() =>
            {
                ProGlassAutomation.Data.Database.DbHelper.DeleteDailyWork(id);
            });
        }
    }
}
