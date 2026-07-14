using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSBIS.Models.Entities
{
    public class User
    {
        public int UserId { get; set; }

        public byte RoleId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = null!;

        [MaxLength(20)]
        public string? Phone { get; set; }

        public bool? IsActive { get; set; }

        // ============================================================
        // Phase 1 - Authentication enhancements (BR06: Lockout)
        // ============================================================
        /// <summary>Số lần đăng nhập sai liên tiếp</summary>
        public int FailedLoginCount { get; set; } = 0;

        /// <summary>Thời điểm kết thúc khóa tài khoản (null = không bị khóa)</summary>
        public DateTime? LockoutEnd { get; set; }

        /// <summary>Lần đăng nhập thành công gần nhất</summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>IP lần đăng nhập thành công gần nhất</summary>
        [MaxLength(50)]
        public string? LastLoginIp { get; set; }

        // ============================================================
        // Original timestamps
        // ============================================================
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // ============================================================
        // Navigation properties
        // ============================================================
        public Role Role { get; set; } = null!;
        public ICollection<UserAddress> UserAddresses { get; set; } = new List<UserAddress>();
        public Cart? Cart { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}