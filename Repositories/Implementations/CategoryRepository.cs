using Microsoft.EntityFrameworkCore;
using OSBIS.Data;
using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;
using ProductEntity = OSBIS.Models.Entities.Product;

namespace OSBIS.Repositories.Implementations
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<Category> _dbSet;

        public CategoryRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Categories;
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.CategoryId == id && c.IsDeleted != true);
        }

        public async Task<Category?> GetByIdWithChildrenAsync(int id)
        {
            return await _dbSet
                .Include(c => c.SubCategories.Where(sub => sub.IsDeleted != true))
                .FirstOrDefaultAsync(c => c.CategoryId == id);
        }

        public async Task<IReadOnlyList<Category>> GetRootsAsync()
        {
            return await _dbSet.AsNoTracking()
                .Where(c => c.ParentCategoryId == null && c.IsDeleted != true)
                .Include(c => c.SubCategories.Where(sub => sub.IsDeleted != true))
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Category>> GetAllActiveAsync()
        {
            return await _dbSet.AsNoTracking()
                .Where(c => c.IsDeleted != true)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Category>> GetChildrenAsync(int parentId)
        {
            return await _dbSet.AsNoTracking()
                .Where(c => c.ParentCategoryId == parentId && c.IsDeleted != true)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Category>> GetAllWithChildrenAsync()
        {
            // Lấy tất cả category (cả root + sub) trong 1 query rồi ghép cây trên memory.
            // Cách này tránh lỗi NullReferenceException khi EF Core Include filter
            // không khởi tạo collection SubCategories (một số edge case của filtered Include).
            var all = await _dbSet.AsNoTracking()
                .Where(c => c.IsDeleted != true)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            // Build tree trên memory: lấy các root (ParentCategoryId == null) và gắn sub vào.
            var lookup = all.ToDictionary(c => c.CategoryId);
            var roots = new List<Category>();
            foreach (var c in all)
            {
                // đảm bảo collection không null
                c.SubCategories ??= new List<Category>();
                c.Products ??= new List<ProductEntity>();

                if (c.ParentCategoryId == null)
                {
                    roots.Add(c);
                }
                else if (lookup.TryGetValue(c.ParentCategoryId.Value, out var parent))
                {
                    parent.SubCategories ??= new List<Category>();
                    parent.SubCategories.Add(c);
                }
            }

            return roots;
        }

        public async Task<bool> HasProductsAsync(int id)
        {
            return await _context.Products.AnyAsync(p => p.CategoryId == id && p.IsDeleted != true);
        }

        public async Task<bool> HasChildrenAsync(int id)
        {
            return await _dbSet.AnyAsync(c => c.ParentCategoryId == id && c.IsDeleted != true);
        }

        public void Add(Category category)
        {
            _dbSet.Add(category);
        }

        public void Update(Category category)
        {
            category.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(category);
        }

        public void SoftDelete(Category category)
        {
            category.IsDeleted = true;
            category.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(category);
        }
    }
}
