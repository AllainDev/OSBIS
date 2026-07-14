using OSBIS.Models.Entities;

namespace OSBIS.Repositories.Interfaces
{
    public interface IVoucherUsageRepository
    {
        Task<VoucherUsage?> GetByIdAsync(int id);
        Task<VoucherUsage?> GetByOrderIdAsync(int orderId);
        Task<IReadOnlyList<VoucherUsage>> GetByVoucherIdAsync(int voucherId);
        Task<bool> HasUserUsedVoucherAsync(int userId, int voucherId);

        Task AddAsync(VoucherUsage usage);
        void Update(VoucherUsage usage);
        void Remove(VoucherUsage usage);

        Task<int> SaveChangesAsync();
    }
}
