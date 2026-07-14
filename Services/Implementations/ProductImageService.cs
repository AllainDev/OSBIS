using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Helpers;
using OSBIS.Services.Interfaces;

namespace OSBIS.Services.Implementations
{
    public class ProductImageService : IProductImageService
    {
        private readonly IUnitOfWork _uow;
        private readonly ImageUploadHelper _uploadHelper;

        public ProductImageService(IUnitOfWork uow, ImageUploadHelper uploadHelper)
        {
            _uow = uow;
            _uploadHelper = uploadHelper;
        }

        public async Task<IReadOnlyList<ProductImage>> GetByProductAsync(int productId)
            => await _uow.ProductImages.GetByProductAsync(productId);

        public async Task<ProductImage?> GetPrimaryAsync(int productId)
            => await _uow.ProductImages.GetPrimaryImageAsync(productId);

        public async Task<ProductImage> UploadAsync(int productId, IFormFile file, bool isPrimary = false)
        {
            var uploadResult = await _uploadHelper.UploadProductImageAsync(file);
            if (!uploadResult.Success)
                throw new InvalidOperationException(uploadResult.ErrorMessage);

            await _uow.BeginTransactionAsync();
            try
            {
                // Nếu sản phẩm chưa có ảnh nào → ảnh upload lên đầu tiên tự động làm primary
                // để danh sách sản phẩm luôn hiển thị được ảnh.
                var existingCount = await _uow.ProductImages.CountByProductAsync(productId);
                var autoMakePrimary = isPrimary || existingCount == 0;

                var image = new ProductImage
                {
                    ProductId = productId,
                    ImageUrl = uploadResult.Url!,
                    IsPrimary = autoMakePrimary,
                    SortOrder = (byte)existingCount
                };

                _uow.ProductImages.Add(image);
                await _uow.SaveChangesAsync(); // cần id để set primary

                if (autoMakePrimary)
                {
                    _uow.ProductImages.SetPrimary(productId, image.ImageId);
                    await _uow.SaveChangesAsync();
                }

                await _uow.CommitTransactionAsync();
                return image;
            }
            catch
            {
                // nếu DB fail → xóa file đã upload
                _uploadHelper.DeleteImage(uploadResult.Url);
                await _uow.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> SetPrimaryAsync(int productId, int imageId)
        {
            _uow.ProductImages.SetPrimary(productId, imageId);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int imageId)
        {
            var image = await _uow.ProductImages.GetByIdAsync(imageId);
            if (image == null) return false;

            await _uow.BeginTransactionAsync();
            try
            {
                _uow.ProductImages.Remove(image);
                await _uow.SaveChangesAsync();

                _uploadHelper.DeleteImage(image.ImageUrl);
                await _uow.CommitTransactionAsync();
                return true;
            }
            catch
            {
                await _uow.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
