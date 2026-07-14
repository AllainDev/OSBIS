using Microsoft.EntityFrameworkCore;
using OSBIS.Data;
using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;

namespace OSBIS.Repositories.Implementations
{
    public class RoleRepository : GenericRepository<Role>, IRoleRepository
    {
        public RoleRepository(AppDbContext context) : base(context) { }

        public async Task<Role?> GetByNameAsync(string roleName)
        {
            return await _dbSet.AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoleName == roleName);
        }

        public async Task<Role?> GetWithUsersAsync(byte roleId)
        {
            return await _dbSet
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.RoleId == roleId);
        }
    }
}