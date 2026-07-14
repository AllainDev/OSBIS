using System.ComponentModel.DataAnnotations;

namespace OSBIS.Models.ViewModels.Category
{
    /// <summary>
    /// ViewModel cho form Category Create/Edit.
    /// </summary>
    public class CategoryViewModel
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
        [StringLength(100)]
        [Display(Name = "Tên danh mục")]
        public string CategoryName { get; set; } = null!;

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Danh mục cha")]
        public int? ParentCategoryId { get; set; }
    }
}
