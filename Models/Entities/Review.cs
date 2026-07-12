using System;

namespace ORBIS.Models.Entities
{
    public class Review
    {
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int OrderId { get; set; }
        public byte Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime? ReviewDate { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public User User { get; set; } = null!;
        public Product Product { get; set; } = null!;
        public Order Order { get; set; } = null!;
    }
}
