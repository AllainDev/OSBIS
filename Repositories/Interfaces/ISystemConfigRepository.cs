using OSBIS.Models.Entities;

namespace OSBIS.Repositories.Interfaces
{
    public interface ISystemConfigRepository
    {
        Task<SystemConfig?> GetAsync(string key);
        Task<SystemConfig?> GetByKeyAsync(string key);
        Task<string?> GetStringAsync(string key);
        Task<decimal?> GetDecimalAsync(string key);
        Task<int?> GetIntAsync(string key);

        Task SetAsync(string key, string value, int? updatedBy = null);

        /// <summary>Update một config đã tồn tại (không save, để UnitOfWork commit).</summary>
        void Update(SystemConfig config);

        Task<IReadOnlyList<SystemConfig>> GetAllAsync();
        Task<int> SaveChangesAsync();
    }
}
