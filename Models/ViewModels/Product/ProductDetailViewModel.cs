using System.ComponentModel.DataAnnotations;
using OSBIS.Models.Entities;
using ProductEntity = OSBIS.Models.Entities.Product;

namespace OSBIS.Models.ViewModels.Product
{
    /// <summary>
    /// ViewModel cho trang chi tiết sản phẩm (Customer/Detail).
    /// </summary>
    public class ProductDetailViewModel
    {
        public ProductEntity Product { get; set; } = null!;
        public IReadOnlyList<ProductEntity> RelatedProducts { get; set; } = new List<ProductEntity>();

        public ProductImage? PrimaryImage =>
            Product.ProductImages?.FirstOrDefault(i => i.IsPrimary == true)
            ?? Product.ProductImages?.FirstOrDefault();

        public IEnumerable<ProductImage> OtherImages =>
            (Product.ProductImages ?? Enumerable.Empty<ProductImage>())
            .Where(i => i != PrimaryImage)
            .OrderBy(i => i.SortOrder);

        public int AvailableStock =>
            Product.GetAvailableStock();

        public bool InStock => AvailableStock > 0;
    }
}
