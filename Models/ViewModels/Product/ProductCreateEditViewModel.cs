using System.ComponentModel.DataAnnotations;

namespace OSBIS.Models.ViewModels.Product
{
    /// <summary>
    /// ViewModel cho form Staff Create/Edit Product.
    /// </summary>
    public class ProductCreateEditViewModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập SKU")]
        [StringLength(50)]
        [Display(Name = "Mã SKU")]
        public string SKU { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [StringLength(200)]
        [Display(Name = "Tên sản phẩm")]
        public string ProductName { get; set; } = null!;

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập đơn vị")]
        [StringLength(20)]
        [Display(Name = "Đơn vị tính")]
        public string UnitOfMeasure { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập cân nặng")]
        [Range(0.01, 9999.99, ErrorMessage = "Cân nặng phải lớn hơn 0")]
        [Display(Name = "Cân nặng (kg)")]
        public decimal Weight { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [Display(Name = "Đơn giá (VND)")]
        public decimal UnitPrice { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Tồn kho phải ≥ 0")]
        [Display(Name = "Tồn kho ban đầu")]
        public int TotalStock { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [Display(Name = "Ảnh sản phẩm (tối đa 5 ảnh, mỗi ảnh ≤ 5MB)")]
        public List<IFormFile>? Images { get; set; }

        [Display(Name = "Chọn ảnh chính")]
        public int? PrimaryImageIndex { get; set; }
    }
}
