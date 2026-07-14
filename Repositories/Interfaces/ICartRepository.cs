using OSBIS.Models.Entities;

namespace OSBIS.Repositories.Interfaces
{
    public interface ICartRepository
    {
        Task<Cart?> GetByUserIdAsync(int userId);
        Task<Cart?> GetByUserIdWithItemsAsync(int userId);
        Task<Cart?> GetBySessionIdAsync(string sessionId);
        Task<Cart?> GetBySessionIdWithItemsAsync(string sessionId);
        Task<Cart?> GetByIdWithItemsAsync(int cartId);

        Task<CartItem?> GetCartItemAsync(int cartItemId);
        Task<CartItem?> GetCartItemAsync(int cartId, int productId);

        /// <summary>Lấy cart abandoned (UpdatedAt < threshold, có hoặc không có item).</summary>
        Task<IReadOnlyList<Cart>> GetAbandonedAsync(DateTime threshold);

        Task AddCartAsync(Cart cart);
        Task AddCartItemAsync(CartItem item);
        void UpdateCart(Cart cart);
        void UpdateCartItem(CartItem item);
        void RemoveCartItem(CartItem item);
        void RemoveCart(Cart cart);

        Task<int> SaveChangesAsync();
    }
}
