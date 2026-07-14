using OSBIS.Models.Entities;

namespace OSBIS.Models.ViewModels
{
    public class UserListViewModel
    {
        public IEnumerable<User> Users { get; set; } = new List<User>();
        public string? SearchTerm { get; set; }
        public string? RoleFilter { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}