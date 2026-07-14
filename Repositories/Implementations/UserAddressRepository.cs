using Microsoft.EntityFrameworkCore;
using OSBIS.Data;
using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;

namespace OSBIS.Repositories.Implementations
{
    public class UserAddressRepository : GenericRepository<UserAddress>, IUserAddressRepository
    {
        public UserAddressRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<UserAddress>> GetByUserIdAsync(int userId)
        {
            return await _dbSet.AsNoTracking()
                .Where(ua => ua.UserId == userId)
                .ToListAsync();
        }

        public async Task<UserAddress?> GetDefaultAddressAsync(int userId)
        {
            return await _dbSet.AsNoTracking()
                .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.IsDefault == true);
        }
    }
}