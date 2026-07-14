using OSBIS.Common;
using OSBIS.Models.Entities;

namespace OSBIS.Repositories.Interfaces
{
    public interface IVoucherRepository
    {
        Task<Voucher?> GetByIdAsync(int id);
        Task<Voucher?> GetByCodeAsync(string code);
        Task<PagedResult<Voucher>> GetPagedAsync(int pageNumber, int pageSize);
        Task<IReadOnlyList<Voucher>> GetActiveVouchersAsync();

        Task AddAsync(Voucher voucher);
        void Update(Voucher voucher);
        void Remove(Voucher voucher);

        Task<int> SaveChangesAsync();
    }
}
