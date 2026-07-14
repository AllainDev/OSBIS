using System;
using System.ComponentModel.DataAnnotations;
using OSBIS.Models.Enums;

namespace OSBIS.Models.Entities
{
    /// <summary>
    /// Audit Log - Ghi lại các hành động quan trọng của user (BR09)
    /// </summary>
    public class AuditLog
    {
        public long AuditLogId { get; set; }

        public int? UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = null!;

        public AuditAction Action { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [MaxLength(100)]
        public string? Controller { get; set; }

        [MaxLength(100)]
        public string? ActionName { get; set; }

        public bool IsSuccess { get; set; }

        [MaxLength(2000)]
        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public User? User { get; set; }
    }
}