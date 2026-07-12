using System;
using System.Collections.Generic;

namespace ORBIS.Models.Entities
{
    public class User
    {
        public int UserId { get; set; }
        public byte RoleId { get; set; }
        public string Username { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public Role Role { get; set; } = null!;
        public ICollection<UserAddress> UserAddresses { get; set; } = new List<UserAddress>();
        public Cart? Cart { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
