using Microsoft.EntityFrameworkCore;
using OSBIS.Data;
using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;

namespace OSBIS.Repositories.Implementations
{
    public class ProductImageRepository : IProductImageRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<ProductImage> _dbSet;

        public ProductImageRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.ProductImages;
        }

        public async Task<ProductImage?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(i => i.ImageId == id);
        }

        public async Task<IReadOnlyList<ProductImage>> GetByProductAsync(int productId)
        {
            return await _dbSet.AsNoTracking()
                .Where(i => i.ProductId == productId)
                .OrderBy(i => i.SortOrder).ThenBy(i => i.ImageId)
                .ToListAsync();
        }

        public async Task<ProductImage?> GetPrimaryImageAsync(int productId)
        {
            return await _dbSet.AsNoTracking()
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.IsPrimary == true);
        }

        public async Task<int> CountByProductAsync(int productId)
        {
            return await _dbSet.CountAsync(i => i.ProductId == productId);
        }

        public void Add(ProductImage image)
        {
            _dbSet.Add(image);
        }

        public void AddRange(IEnumerable<ProductImage> images)
        {
            _dbSet.AddRange(images);
        }

        public void Remove(ProductImage image)
        {
            _dbSet.Remove(image);
        }

        public void RemoveRange(IEnumerable<ProductImage> images)
        {
            _dbSet.RemoveRange(images);
        }

        public void SetPrimary(int productId, int imageId)
        {
            var images = _dbSet.Where(i => i.ProductId == productId).ToList();
            foreach (var img in images)
                img.IsPrimary = img.ImageId == imageId;
        }
    }
}
