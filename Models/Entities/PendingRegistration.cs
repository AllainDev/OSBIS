using System.ComponentModel.DataAnnotations;

namespace ORBIS.Models.Entities
{
    public class PendingRegistration
    {
        public int PendingRegistrationId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string PasswordHash { get; set; } = string.Empty;

        public byte RoleId { get; set; }

        [Required]
        [MaxLength(128)]
        public string OtpHash { get; set; } = string.Empty;

        public DateTime OtpExpiresAtUtc { get; set; }

        public DateTime LastOtpSentAtUtc { get; set; }

        public int FailedAttempts { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
