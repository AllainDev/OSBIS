using OSBIS.Models.Entities;

namespace OSBIS.Services.Interfaces
{
    /// <summary>
    /// ProductImage Service — Phase 2: upload + sắp xếp.
    /// </summary>
    public interface IProductImageService
    {
        Task<IReadOnlyList<ProductImage>> GetByProductAsync(int productId);
        Task<ProductImage?> GetPrimaryAsync(int productId);

        /// <summary>Upload và lưu 1 ảnh cho sản phẩm.</summary>
        Task<ProductImage> UploadAsync(int productId, IFormFile file, bool isPrimary = false);

        Task<bool> SetPrimaryAsync(int productId, int imageId);
        Task<bool> DeleteAsync(int imageId);
    }
}
