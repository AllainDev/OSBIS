using OSBIS.Models.Entities;

namespace OSBIS.Repositories.Interfaces
{
    /// <summary>
    /// Repository cho Category — hỗ trợ cây đa cấp.
    /// </summary>
    public interface ICategoryRepository
    {
        Task<Category?> GetByIdAsync(int id);
        Task<Category?> GetByIdWithChildrenAsync(int id);

        Task<IReadOnlyList<Category>> GetRootsAsync();
        Task<IReadOnlyList<Category>> GetAllActiveAsync();
        Task<IReadOnlyList<Category>> GetChildrenAsync(int parentId);
        Task<IReadOnlyList<Category>> GetAllWithChildrenAsync(); // tree cho menu

        Task<bool> HasProductsAsync(int id);
        Task<bool> HasChildrenAsync(int id);

        void Add(Category category);
        void Update(Category category);
        void SoftDelete(Category category);
    }
}
