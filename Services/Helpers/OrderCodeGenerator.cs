using System.Globalization;
using OSBIS.Repositories.Interfaces;

namespace OSBIS.Services.Helpers
{
    /// <summary>
    /// Sinh OrderCode theo format OSB-yyyyMMdd-####, reset sequence mỗi ngày.
    /// Sequence lưu trong SystemConfig (key = "OrderSequence").
    /// Phải được gọi bên trong 1 transaction (UnitOfWork) để tránh trùng sequence.
    /// </summary>
    public class OrderCodeGenerator
    {
        private readonly IUnitOfWork _uow;

        // Key lưu sequence trong SystemConfig
        public const string SequenceConfigKey = "OrderSequence";
        // Key lưu ngày cuối cùng reset (yyyyMMdd)
        public const string LastResetDateKey = "OrderSequenceDate";

        public OrderCodeGenerator(IUnitOfWork uow)
        {
            _uow = uow;
        }

        /// <summary>
        /// Sinh OrderCode mới. Tăng sequence trong SystemConfig lên 1.
        /// Nếu ngày đổi → reset sequence về 1.
        /// Trả về OrderCode kiểu "OSB-20260713-0001".
        /// </summary>
        public async Task<string> GenerateAsync()
        {
            var now = DateTime.UtcNow;
            var todayKey = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

            // Đọc sequence + ngày reset hiện tại
            var currentSequence = await _uow.SystemConfigs.GetIntAsync(SequenceConfigKey) ?? 0;
            var lastResetDate = await _uow.SystemConfigs.GetStringAsync(LastResetDateKey) ?? string.Empty;

            int nextSequence;
            if (!string.Equals(lastResetDate, todayKey, StringComparison.Ordinal))
            {
                // Ngày mới → reset về 1
                nextSequence = 1;
                await _uow.SystemConfigs.SetAsync(LastResetDateKey, todayKey);
            }
            else
            {
                // Cùng ngày → tăng 1
                nextSequence = currentSequence + 1;
            }

            // Lưu sequence mới
            await _uow.SystemConfigs.SetAsync(SequenceConfigKey, nextSequence.ToString());
            await _uow.SaveChangesAsync();

            // Format: OSB-yyyyMMdd-####
            var orderCode = $"OSB-{todayKey}-{nextSequence:D4}";
            return orderCode;
        }
    }
}
