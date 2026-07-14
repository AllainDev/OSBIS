using OSBIS.Models.Entities;

namespace OSBIS.Repositories.Interfaces
{
    public interface IUserAddressRepository : IGenericRepository<UserAddress>
    {
        Task<IEnumerable<UserAddress>> GetByUserIdAsync(int userId);
        Task<UserAddress?> GetDefaultAddressAsync(int userId);
    }
}