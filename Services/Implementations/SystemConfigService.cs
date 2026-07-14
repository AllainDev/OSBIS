using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Interfaces;

namespace OSBIS.Services.Implementations
{
    /// <summary>
    /// Service cho SystemConfig. Hiện chưa cache (Phase 5 sẽ thêm IMemoryCache).
    /// </summary>
    public class SystemConfigService : ISystemConfigService
    {
        private readonly ISystemConfigRepository _repo;

        public SystemConfigService(ISystemConfigRepository repo)
        {
            _repo = repo;
        }

        public Task<string?> GetStringAsync(string key) => _repo.GetStringAsync(key);
        public Task<decimal?> GetDecimalAsync(string key) => _repo.GetDecimalAsync(key);
        public Task<int?> GetIntAsync(string key) => _repo.GetIntAsync(key);
        public Task SetAsync(string key, string value, int? updatedBy = null)
            => _repo.SetAsync(key, value, updatedBy);
        public Task<IReadOnlyList<SystemConfig>> GetAllAsync() => _repo.GetAllAsync();
    }
}
