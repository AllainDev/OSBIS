using OSBIS.Models.Entities;

namespace OSBIS.Repositories.Interfaces
{
    public interface IRoleRepository : IGenericRepository<Role>
    {
        Task<Role?> GetByNameAsync(string roleName);
        Task<Role?> GetWithUsersAsync(byte roleId);
    }
}