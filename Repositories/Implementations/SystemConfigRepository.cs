using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OSBIS.Data;
using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;

namespace OSBIS.Repositories.Implementations
{
    public class SystemConfigRepository : ISystemConfigRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<SystemConfig> _dbSet;

        public SystemConfigRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.SystemConfigs;
        }

        public async Task<SystemConfig?> GetAsync(string key)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(c => c.ConfigKey == key);
        }

        public async Task<SystemConfig?> GetByKeyAsync(string key)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.ConfigKey == key);
        }

        public async Task<string?> GetStringAsync(string key)
        {
            var cfg = await GetAsync(key);
            return cfg?.ConfigValue;
        }

        public async Task<decimal?> GetDecimalAsync(string key)
        {
            var val = await GetStringAsync(key);
            if (string.IsNullOrWhiteSpace(val)) return null;
            if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d;
            return null;
        }

        public async Task<int?> GetIntAsync(string key)
        {
            var val = await GetStringAsync(key);
            if (string.IsNullOrWhiteSpace(val)) return null;
            if (int.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var i))
                return i;
            return null;
        }

        public async Task SetAsync(string key, string value, int? updatedBy = null)
        {
            var existing = await _dbSet.FirstOrDefaultAsync(c => c.ConfigKey == key);
            if (existing == null)
            {
                _dbSet.Add(new SystemConfig
                {
                    ConfigKey = key,
                    ConfigValue = value,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = updatedBy
                });
            }
            else
            {
                existing.ConfigValue = value;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = updatedBy;
                _dbSet.Update(existing);
            }
            await _context.SaveChangesAsync();
        }

        public void Update(SystemConfig config)
        {
            config.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(config);
        }

        public async Task<IReadOnlyList<SystemConfig>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().OrderBy(c => c.ConfigKey).ToListAsync();
        }

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
