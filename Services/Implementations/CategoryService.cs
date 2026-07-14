using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Interfaces;

namespace OSBIS.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _uow;

        public CategoryService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IReadOnlyList<Category>> GetRootsAsync()
            => await _uow.Categories.GetRootsAsync();

        public async Task<IReadOnlyList<Category>> GetAllActiveAsync()
            => await _uow.Categories.GetAllActiveAsync();

        public async Task<IReadOnlyList<Category>> GetAllWithChildrenAsync()
            => await _uow.Categories.GetAllWithChildrenAsync();

        public async Task<Category?> GetByIdAsync(int id)
            => await _uow.Categories.GetByIdAsync(id);

        public async Task<int> CreateAsync(Category category)
        {
            await _uow.BeginTransactionAsync();
            try
            {
                _uow.Categories.Add(category);
                await _uow.SaveChangesAsync();

                // Nếu là root (ParentCategoryId null) và chưa có slug-friendly thì skip
                await _uow.CommitTransactionAsync();
                return category.CategoryId;
            }
            catch
            {
                await _uow.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> UpdateAsync(Category category)
        {
            _uow.Categories.Update(category);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string? Error)> DeleteAsync(int id)
        {
            if (await _uow.Categories.HasProductsAsync(id))
                return (false, "Không thể xóa danh mục còn sản phẩm.");

            if (await _uow.Categories.HasChildrenAsync(id))
                return (false, "Không thể xóa danh mục còn danh mục con.");

            var category = await _uow.Categories.GetByIdAsync(id);
            if (category == null) return (false, "Danh mục không tồn tại.");

            _uow.Categories.SoftDelete(category);
            await _uow.SaveChangesAsync();
            return (true, null);
        }
    }
}
