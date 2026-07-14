using OSBIS.Models.Entities;

namespace OSBIS.Services.Interfaces
{
    /// <summary>
    /// Service quản lý Voucher:
    /// - Validate voucher (IsActive, DateRange, Limit, MinOrder, per-user usage)
    /// - Tính discount amount
    /// - Ghi nhận voucher usage khi place order
    /// </summary>
    public interface IVoucherService
    {
        Task<VoucherValidationResult> ValidateAsync(string code, int userId, decimal orderValue);
        decimal CalculateDiscount(Voucher voucher, decimal orderValue, decimal shippingFee);

        /// <summary>Ghi VoucherUsage + tăng UsedCount. Gọi bên trong transaction.</summary>
        Task<VoucherUsage> UseAsync(int voucherId, int userId, int orderId);

        /// <summary>Hoàn voucher khi cancel order.</summary>
        Task RestoreAsync(int orderId);
    }

    public class VoucherValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Voucher? Voucher { get; set; }

        public static VoucherValidationResult Ok(Voucher v) =>
            new() { IsValid = true, Voucher = v };

        public static VoucherValidationResult Fail(string msg) =>
            new() { IsValid = false, ErrorMessage = msg };
    }
}
