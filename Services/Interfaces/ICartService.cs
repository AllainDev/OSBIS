using OSBIS.Models.Entities;
using OSBIS.Models.ViewModels.Cart;

namespace OSBIS.Services.Interfaces
{
    /// <summary>
    /// Service quản lý giỏ hàng:
    /// - Thêm/sửa/xóa item, reserve stock
    /// - Merge cart khi login
    /// - Tính subtotal/quantity
    /// </summary>
    public interface ICartService
    {
        Task<Cart?> GetCurrentCartAsync();
        Task<CartSummaryViewModel> GetCartSummaryAsync();

        Task<CartResult> AddItemAsync(int productId, int quantity);
        Task<CartResult> UpdateQuantityAsync(int cartItemId, int newQuantity);
        Task<CartResult> RemoveItemAsync(int cartItemId);
        Task ClearCartAsync();

        /// <summary>Merge giỏ guest (theo sessionId) vào giỏ user (theo userId) khi login. Cộng dồn quantity, cap = stock.</summary>
        Task MergeCartOnLoginAsync(int userId, string sessionId);

        Task<int> GetCartItemCountAsync();
    }

    public class CartResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? CartItemCount { get; set; }
    }
}
