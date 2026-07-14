using OSBIS.Models.Entities;
using OSBIS.Models.Enums;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Helpers;
using OSBIS.Services.Interfaces;

namespace OSBIS.Services.Implementations
{
    /// <summary>
    /// Service quản lý Voucher:
    /// - Validate: IsActive, DateRange, UsageLimit, MinOrderValue, per-user usage.
    /// - Calculate: dùng VoucherCalculator static helper.
    /// - Use: insert VoucherUsage + tăng UsedCount (gọi trong transaction).
    /// - Restore: dùng khi cancel order.
    /// </summary>
    public class VoucherService : IVoucherService
    {
        private readonly IUnitOfWork _uow;

        public VoucherService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<VoucherValidationResult> ValidateAsync(string code, int userId, decimal orderValue)
        {
            if (string.IsNullOrWhiteSpace(code))
                return VoucherValidationResult.Fail("Vui lòng nhập mã giảm giá.");

            var voucher = await _uow.Vouchers.GetByCodeAsync(code);
            if (voucher == null)
                return VoucherValidationResult.Fail("Mã giảm giá không tồn tại.");

            if (voucher.IsActive != true)
                return VoucherValidationResult.Fail("Mã giảm giá đã bị vô hiệu hóa.");

            var now = DateTime.UtcNow;
            if (now < voucher.StartDate)
                return VoucherValidationResult.Fail("Mã giảm giá chưa đến ngày áp dụng.");
            if (now > voucher.EndDate)
                return VoucherValidationResult.Fail("Mã giảm giá đã hết hạn.");

            if ((voucher.UsedCount ?? 0) >= voucher.UsageLimit)
                return VoucherValidationResult.Fail("Mã giảm giá đã hết lượt sử dụng.");

            if (voucher.MinOrderValue.HasValue && orderValue < voucher.MinOrderValue.Value)
                return VoucherValidationResult.Fail($"Đơn hàng tối thiểu {voucher.MinOrderValue.Value:N0}đ để dùng mã này.");

            // Per-user quota (1 user = 1 lần)
            var alreadyUsed = await _uow.VoucherUsages.HasUserUsedVoucherAsync(userId, voucher.VoucherId);
            if (alreadyUsed)
                return VoucherValidationResult.Fail("Bạn đã sử dụng mã giảm giá này rồi.");

            return VoucherValidationResult.Ok(voucher);
        }

        public decimal CalculateDiscount(Voucher voucher, decimal orderValue, decimal shippingFee)
            => VoucherCalculator.Calculate(voucher, orderValue, shippingFee);

        public async Task<VoucherUsage> UseAsync(int voucherId, int userId, int orderId)
        {
            var usage = new VoucherUsage
            {
                VoucherId = voucherId,
                UserId = userId,
                OrderId = orderId,
                UsedAt = DateTime.UtcNow
            };
            await _uow.VoucherUsages.AddAsync(usage);

            var voucher = await _uow.Vouchers.GetByIdAsync(voucherId);
            if (voucher != null)
            {
                voucher.UsedCount = (voucher.UsedCount ?? 0) + 1;
                _uow.Vouchers.Update(voucher);
                await _uow.SaveChangesAsync();
            }
            return usage;
        }

        public async Task RestoreAsync(int orderId)
        {
            var usage = await _uow.VoucherUsages.GetByOrderIdAsync(orderId);
            if (usage == null) return;

            var voucher = await _uow.Vouchers.GetByIdAsync(usage.VoucherId);
            if (voucher != null && (voucher.UsedCount ?? 0) > 0)
            {
                voucher.UsedCount = voucher.UsedCount - 1;
                _uow.Vouchers.Update(voucher);
            }
            _uow.VoucherUsages.Remove(usage);
            await _uow.SaveChangesAsync();
        }
    }
}
