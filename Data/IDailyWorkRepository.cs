using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProGlassAutomation.Data
{
    public interface IDailyWorkRepository
    {
        Task<List<DbDailyWork>> GetAllAsync(CancellationToken ct = default);
        Task<DbDailyWork?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<int> InsertAsync(DbDailyWork entity, CancellationToken ct = default);
        Task UpdateAsync(DbDailyWork entity, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
        Task<bool> IsDuplicateAsync(string customerReference, string piNumber, int? excludeId, CancellationToken ct = default);
        Task<List<DbDailyWork>> GetByCompanyAsync(string company, CancellationToken ct = default);
        Task<List<DbDailyWork>> GetByPINumberAsync(string piNumber, CancellationToken ct = default);
        Task<List<DbDailyWork>> GetByStatusAsync(string status, CancellationToken ct = default);
        Task<List<DbDailyWork>> GetByDateRangeAsync(DateTime start, DateTime end, CancellationToken ct = default);
    }
}