using OSBIS.Models.Entities;
using OSBIS.Models.Enums;

namespace OSBIS.Services.Helpers
{
    /// <summary>
    /// Tính số tiền giảm giá theo loại voucher.
    ///
    /// - Percent: discount = orderValue * DiscountValue / 100, cap bằng MaxDiscountAmount (nếu có)
    /// - FixedAmount: discount = min(DiscountValue, orderValue)
    /// - FreeShipping: discount = shippingFee (giảm 100% phí ship)
    /// </summary>
    public static class VoucherCalculator
    {
        public static decimal Calculate(Voucher voucher, decimal orderValue, decimal shippingFee)
        {
            if (voucher == null) return 0m;

            return voucher.DiscountType switch
            {
                DiscountType.Percent => CalculatePercent(voucher, orderValue),
                DiscountType.FixedAmount => CalculateFixedAmount(voucher, orderValue),
                DiscountType.FreeShipping => shippingFee,
                _ => 0m
            };
        }

        private static decimal CalculatePercent(Voucher voucher, decimal orderValue)
        {
            var rawDiscount = orderValue * voucher.DiscountValue / 100m;
            if (rawDiscount < 0) rawDiscount = 0;

            // Cap bằng MaxDiscountAmount nếu có
            if (voucher.MaxDiscountAmount.HasValue && voucher.MaxDiscountAmount.Value > 0)
            {
                rawDiscount = Math.Min(rawDiscount, voucher.MaxDiscountAmount.Value);
            }

            // Không giảm quá giá trị đơn
            rawDiscount = Math.Min(rawDiscount, orderValue);

            return Math.Round(rawDiscount, 0, MidpointRounding.AwayFromZero);
        }

        private static decimal CalculateFixedAmount(Voucher voucher, decimal orderValue)
        {
            var discount = Math.Min(voucher.DiscountValue, orderValue);
            if (discount < 0) discount = 0;
            return Math.Round(discount, 0, MidpointRounding.AwayFromZero);
        }
    }
}
