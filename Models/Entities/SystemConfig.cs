using System;

namespace OSBIS.Models.Entities
{
    /// <summary>
    /// Cấu hình hệ thống dạng key-value (cache 10 phút trong memory ở Phase 5).
    /// </summary>
    public class SystemConfig
    {
        public string ConfigKey { get; set; } = null!;
        public string ConfigValue { get; set; } = null!;
        public string? Description { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public User? UpdatedByUser { get; set; }
    }
}
