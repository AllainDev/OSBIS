using OSBIS.Repositories.Interfaces;

namespace OSBIS.Services.Helpers
{
    /// <summary>
    /// Tính phí vận chuyển theo cân nặng + subtotal, cấu hình lấy từ SystemConfig.
    ///
    /// Rules:
    /// - Subtotal >= FreeShipThreshold → Free (0 đ)
    /// - Ngược lại: shipping = max(MinShippingFee, totalWeight * ShippingFeePerKg)
    /// </summary>
    public class ShippingCalculator
    {
        private readonly IUnitOfWork _uow;

        // Config keys
        public const string ShippingFeePerKgKey = "ShippingFeePerKg";
        public const string FreeShipThresholdKey = "FreeShipThreshold";
        public const string MinShippingFeeKey = "MinShippingFee";

        // Defaults nếu SystemConfig chưa seed
        private const decimal DefaultFeePerKg = 25000m;
        private const decimal DefaultFreeShipThreshold = 500000m;
        private const decimal DefaultMinShippingFee = 30000m;

        public ShippingCalculator(IUnitOfWork uow)
        {
            _uow = uow;
        }

        /// <summary>
        /// Tính phí vận chuyển. Trả về 0 nếu subtotal >= FreeShipThreshold.
        /// </summary>
        public async Task<decimal> CalculateAsync(decimal subTotal, decimal totalWeight)
        {
            var feePerKg = await _uow.SystemConfigs.GetDecimalAsync(ShippingFeePerKgKey) ?? DefaultFeePerKg;
            var freeThreshold = await _uow.SystemConfigs.GetDecimalAsync(FreeShipThresholdKey) ?? DefaultFreeShipThreshold;
            var minFee = await _uow.SystemConfigs.GetDecimalAsync(MinShippingFeeKey) ?? DefaultMinShippingFee;

            // Đạt ngưỡng freeship
            if (subTotal >= freeThreshold)
                return 0m;

            // Tính theo cân nặng, có min
            var byWeight = totalWeight * feePerKg;
            return Math.Max(minFee, byWeight);
        }

        /// <summary>
        /// Tính synchronous với config đã biết trước (dùng trong CalculateOrderTotals).
        /// </summary>
        public static decimal Calculate(decimal subTotal, decimal totalWeight,
            decimal feePerKg, decimal freeThreshold, decimal minFee)
        {
            if (subTotal >= freeThreshold)
                return 0m;

            var byWeight = totalWeight * feePerKg;
            return Math.Max(minFee, byWeight);
        }
    }
}
