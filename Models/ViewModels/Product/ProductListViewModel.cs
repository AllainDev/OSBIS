using System.ComponentModel.DataAnnotations;
using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Repositories.Specifications;
using ProductEntity = OSBIS.Models.Entities.Product;
using CategoryEntity = OSBIS.Models.Entities.Category;

namespace OSBIS.Models.ViewModels.Product
{
    /// <summary>
    /// ViewModel cho trang danh sách sản phẩm (Customer/Index + Staff/Index).
    /// </summary>
    public class ProductListViewModel
    {
        public PagedResult<ProductEntity> Products { get; set; } = PagedResult<ProductEntity>.Empty();

        // Filter state
        public int? CategoryId { get; set; }
        public string? Keyword { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool InStockOnly { get; set; }
        public ProductSortBy SortBy { get; set; } = ProductSortBy.CreatedDesc;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;

        // Sidebar
        public IReadOnlyList<CategoryEntity> Categories { get; set; } = new List<CategoryEntity>();

        public ProductFilterSpec ToSpec() => new()
        {
            CategoryId = CategoryId,
            Keyword = Keyword,
            MinPrice = MinPrice,
            MaxPrice = MaxPrice,
            InStockOnly = InStockOnly,
            SortBy = SortBy,
            PageNumber = PageNumber,
            PageSize = PageSize
        };

        public string PrimaryImageUrl(ProductEntity p)
        {
            var primary = p.ProductImages?.FirstOrDefault(i => i.IsPrimary == true)
                        ?? p.ProductImages?.FirstOrDefault();
            return primary?.ImageUrl ?? "/images/no-image.png";
        }

        public bool IsAvailable(ProductEntity p) => (p.TotalStock - p.ReservedQuantity) > 0;
    }
}
