using Microsoft.EntityFrameworkCore;
using OSBIS.Common;
using OSBIS.Data;
using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;
using OSBIS.Repositories.Specifications;

namespace OSBIS.Repositories.Implementations
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<Product> _dbSet;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Products;
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(p => p.InventoryBatches)
                .FirstOrDefaultAsync(p => p.ProductId == id && p.IsDeleted != true);
        }

        public async Task<Product?> GetByIdWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Category)
                .Include(p => p.ProductImages.OrderBy(i => i.SortOrder))
                .Include(p => p.InventoryBatches)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.ProductId == id);
        }

        public async Task<Product?> GetBySKUAsync(string sku)
        {
            return await _dbSet.FirstOrDefaultAsync(p => p.SKU == sku);
        }

        public async Task<bool> IsSKUExistsAsync(string sku, int? excludeId = null)
        {
            var query = _dbSet.Where(p => p.SKU == sku);
            if (excludeId.HasValue)
                query = query.Where(p => p.ProductId != excludeId.Value);
            return await query.AnyAsync();
        }

        public async Task<IReadOnlyList<Product>> GetByCategoryAsync(int categoryId)
        {
            return await _dbSet.AsNoTracking()
                .Where(p => p.CategoryId == categoryId && p.IsDeleted != true)
                .Include(p => p.ProductImages.Where(i => i.IsPrimary == true))
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Product>> GetRelatedAsync(int categoryId, int excludeProductId, int take = 4)
        {
            return await _dbSet.AsNoTracking()
                .Where(p => p.CategoryId == categoryId && p.ProductId != excludeProductId && p.IsDeleted != true)
                .Include(p => p.ProductImages.Where(i => i.IsPrimary == true))
                .OrderByDescending(p => p.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Product>> GetByIdsWithPrimaryImageAsync(IEnumerable<int> productIds)
        {
            var idList = productIds?.Distinct().ToList() ?? new List<int>();
            if (idList.Count == 0) return new List<Product>();

            return await _dbSet.AsNoTracking()
                .Where(p => idList.Contains(p.ProductId))
                .Include(p => p.ProductImages.Where(i => i.IsPrimary == true))
                .Include(p => p.InventoryBatches)
                .ToListAsync();
        }

        public async Task<PagedResult<Product>> GetPagedAsync(ProductFilterSpec spec)
        {
            // Load tất cả ProductImages để view có thể fallback sang ảnh đầu tiên
            // khi sản phẩm chưa có ảnh primary (PrimaryImageUrl ở ViewModel xử lý)
            var query = _dbSet.AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Where(spec.ToPredicate());

            var totalCount = await query.CountAsync();

            var items = await spec.ToOrderBy()(query)
                .Skip((spec.PageNumber - 1) * spec.PageSize)
                .Take(spec.PageSize)
                .ToListAsync();

            return new PagedResult<Product>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = spec.PageNumber,
                PageSize = spec.PageSize
            };
        }

        public void Add(Product product)
        {
            _dbSet.Add(product);
        }

        public void Update(Product product)
        {
            product.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(product);
        }

        public void SoftDelete(Product product)
        {
            product.IsDeleted = true;
            product.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(product);
        }
    }
}
