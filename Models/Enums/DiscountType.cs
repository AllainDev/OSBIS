namespace OSBIS.Models.Enums
{
    /// <summary>
    /// Loại giảm giá voucher (theo OSBIS_PLAN.md).
    /// Tên enum đã được đổi từ "Percentage" → "Percent" cho khớp với plan.
    /// </summary>
    public enum DiscountType : byte
    {
        Percent = 0,        // Giảm theo %
        FixedAmount = 1,    // Giảm số tiền cố định (VND)
        FreeShipping = 2    // Giảm phí ship = 100% shipping fee
    }
}
