using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;
using OSBIS.Repositories.Specifications;
using OSBIS.Services.Helpers;
using OSBIS.Services.Interfaces;

namespace OSBIS.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _uow;
        private readonly IProductImageService _productImageService;
        private readonly ImageUploadHelper _imageUploadHelper;

        public ProductService(IUnitOfWork uow, IProductImageService productImageService, ImageUploadHelper imageUploadHelper)
        {
            _uow = uow;
            _productImageService = productImageService;
            _imageUploadHelper = imageUploadHelper;
        }

        public async Task<PagedResult<Product>> GetProductsAsync(ProductFilterSpec spec)
        {
            return await _uow.Products.GetPagedAsync(spec);
        }

        public async Task<IReadOnlyList<Product>> GetAllActiveAsync()
        {
            var spec = new ProductFilterSpec { PageSize = 100, IncludeDeleted = false };
            var page = await _uow.Products.GetPagedAsync(spec);
            return page.Items;
        }

        public async Task<IReadOnlyList<Product>> GetRelatedAsync(int categoryId, int excludeProductId, int take = 4)
        {
            return await _uow.Products.GetRelatedAsync(categoryId, excludeProductId, take);
        }

        public async Task<Product?> GetProductDetailAsync(int id)
        {
            return await _uow.Products.GetByIdWithDetailsAsync(id);
        }

        public async Task<int> CreateAsync(Product product, IFormFile[]? images, int? primaryImageIndex)
        {
            if (await _uow.Products.IsSKUExistsAsync(product.SKU))
                throw new InvalidOperationException($"SKU '{product.SKU}' đã tồn tại.");

            await _uow.BeginTransactionAsync();
            try
            {
                _uow.Products.Add(product);
                await _uow.SaveChangesAsync();

                // Upload ảnh nếu có
                if (images != null)
                {
                    for (int i = 0; i < images.Length; i++)
                    {
                        var file = images[i];
                        if (file == null || file.Length == 0) continue;
                        var isPrimary = primaryImageIndex.HasValue && primaryImageIndex.Value == i;
                        await _productImageService.UploadAsync(product.ProductId, file, isPrimary);
                    }
                }

                await _uow.CommitTransactionAsync();
                return product.ProductId;
            }
            catch
            {
                await _uow.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            if (await _uow.Products.IsSKUExistsAsync(product.SKU, product.ProductId))
                throw new InvalidOperationException($"SKU '{product.SKU}' đã tồn tại.");

            _uow.Products.Update(product);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _uow.Products.GetByIdAsync(id);
            if (product == null) return false;

            _uow.Products.SoftDelete(product);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsSKUExistsAsync(string sku, int? excludeId = null)
            => await _uow.Products.IsSKUExistsAsync(sku, excludeId);
    }
}
