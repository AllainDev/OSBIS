using System.ComponentModel.DataAnnotations;
using OSBIS.Models.Entities;

namespace OSBIS.Models.ViewModels.Product
{
    public class InventoryBatchViewModel
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }

        public IReadOnlyList<InventoryBatch> Batches { get; set; } = new List<InventoryBatch>();

        [StringLength(50)]
        [Display(Name = "Mã lô (tự động nếu để trống)")]
        public string? BatchCode { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày sản xuất")]
        public DateOnly ManufactureDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Hạn sử dụng")]
        public DateOnly ExpiryDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));

        [Required(ErrorMessage = "Nhập số lượng")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng ≥ 1")]
        [Display(Name = "Số lượng")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Nhập giá vốn")]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "Giá vốn (VND)")]
        public decimal CostPrice { get; set; }

        public bool IsExpiringSoon()
        {
            var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
            return ExpiryDate >= DateOnly.FromDateTime(DateTime.UtcNow) && ExpiryDate <= cutoff;
        }
    }
}
