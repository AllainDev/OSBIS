using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Repositories.Specifications;

namespace OSBIS.Services.Interfaces
{
    /// <summary>
    /// Product Service — Phase 2: CRUD, filter, upload ảnh.
    /// </summary>
    public interface IProductService
    {
        Task<PagedResult<Product>> GetProductsAsync(ProductFilterSpec spec);
        Task<IReadOnlyList<Product>> GetAllActiveAsync();
        Task<IReadOnlyList<Product>> GetRelatedAsync(int categoryId, int excludeProductId, int take = 4);
        Task<Product?> GetProductDetailAsync(int id);

        Task<int> CreateAsync(Product product, IFormFile[]? images, int? primaryImageIndex);
        Task<bool> UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id);

        Task<bool> IsSKUExistsAsync(string sku, int? excludeId = null);
    }
}
