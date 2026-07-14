using OSBIS.Models.Entities;

namespace OSBIS.Repositories.Interfaces
{
    /// <summary>
    /// Repository cho ProductImage — upload ảnh + đặt ảnh chính.
    /// </summary>
    public interface IProductImageRepository
    {
        Task<ProductImage?> GetByIdAsync(int id);
        Task<IReadOnlyList<ProductImage>> GetByProductAsync(int productId);
        Task<ProductImage?> GetPrimaryImageAsync(int productId);
        Task<int> CountByProductAsync(int productId);

        void Add(ProductImage image);
        void AddRange(IEnumerable<ProductImage> images);
        void Remove(ProductImage image);
        void RemoveRange(IEnumerable<ProductImage> images);

        /// <summary>Set ảnh primary cho sản phẩm (transaction-safe, clear các ảnh primary khác).</summary>
        void SetPrimary(int productId, int imageId);
    }
}
