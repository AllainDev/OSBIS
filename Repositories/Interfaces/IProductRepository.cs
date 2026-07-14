using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Repositories.Specifications;

namespace OSBIS.Repositories.Interfaces
{
    /// <summary>
    /// Repository cho Product. Phase 2 — kèm Specification Pattern để filter đa tiêu chí.
    /// Phase 3 bổ sung: GetByIdsWithPrimaryImageAsync dùng cho Cart/Checkout.
    /// </summary>
    public interface IProductRepository
    {
        // Read
        Task<Product?> GetByIdAsync(int id);
        Task<Product?> GetByIdWithDetailsAsync(int id); // include Images, Category, Reviews
        Task<Product?> GetBySKUAsync(string sku);
        Task<bool> IsSKUExistsAsync(string sku, int? excludeId = null);

        Task<IReadOnlyList<Product>> GetByCategoryAsync(int categoryId);
        Task<IReadOnlyList<Product>> GetRelatedAsync(int categoryId, int excludeProductId, int take = 4);

        /// <summary>Lấy nhiều product theo danh sách ID, kèm ảnh chính. Dùng cho cart/checkout.</summary>
        Task<IReadOnlyList<Product>> GetByIdsWithPrimaryImageAsync(IEnumerable<int> productIds);

        // Paged + Filter
        Task<PagedResult<Product>> GetPagedAsync(ProductFilterSpec spec);

        // Write
        void Add(Product product);
        void Update(Product product);
        void SoftDelete(Product product);
    }
}
