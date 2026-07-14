namespace OSBIS.Models.Entities
{
    public class UserAddress
    {
        public int AddressId { get; set; }
        public int UserId { get; set; }
        public string ReceiverName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string StreetAddress { get; set; } = null!;
        public string City { get; set; } = null!;
        public string District { get; set; } = null!;
        public string Ward { get; set; } = null!;
        public bool? IsDefault { get; set; }

        public User User { get; set; } = null!;
    }
}