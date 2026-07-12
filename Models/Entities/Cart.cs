using System;
using System.Collections.Generic;

namespace ORBIS.Models.Entities
{
    public class Cart
    {
        public int CartId { get; set; }
        public int? UserId { get; set; }
        public string? SessionId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public User? User { get; set; }
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
