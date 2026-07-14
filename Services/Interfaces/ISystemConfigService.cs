using OSBIS.Models.Entities;

namespace OSBIS.Services.Interfaces
{
    /// <summary>
    /// Service cho SystemConfig — wrapper thân thiện, có cache ngắn hạn.
    /// </summary>
    public interface ISystemConfigService
    {
        Task<string?> GetStringAsync(string key);
        Task<decimal?> GetDecimalAsync(string key);
        Task<int?> GetIntAsync(string key);
        Task SetAsync(string key, string value, int? updatedBy = null);
        Task<IReadOnlyList<SystemConfig>> GetAllAsync();
    }
}
