using System.ComponentModel.DataAnnotations;

namespace OSBIS.Models.ViewModels
{
    public class RoleViewModel
    {
        public byte RoleId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên vai trò")]
        [StringLength(50)]
        [Display(Name = "Tên vai trò")]
        public string RoleName { get; set; } = null!;

        [Display(Name = "Số người dùng")]
        public int UserCount { get; set; }
    }
}