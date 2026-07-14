using OSBIS.Models.Entities;

namespace OSBIS.Services.Interfaces
{
    /// <summary>
    /// Category Service — Phase 2: CRUD + tree.
    /// </summary>
    public interface ICategoryService
    {
        Task<IReadOnlyList<Category>> GetRootsAsync();
        Task<IReadOnlyList<Category>> GetAllActiveAsync();
        Task<IReadOnlyList<Category>> GetAllWithChildrenAsync(); // tree cho menu
        Task<Category?> GetByIdAsync(int id);

        Task<int> CreateAsync(Category category);
        Task<bool> UpdateAsync(Category category);
        /// <summary>Soft delete; fail nếu còn products/children.</summary>
        Task<(bool Success, string? Error)> DeleteAsync(int id);
    }
}
