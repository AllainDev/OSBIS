using System.Collections.Generic;

namespace OSBIS.Models.Entities
{
    public class Role
    {
        public byte RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}