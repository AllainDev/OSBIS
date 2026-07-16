using Microsoft.EntityFrameworkCore;
using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Models.ViewModels.Cart;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Interfaces;

namespace OSBIS.Services.Implementations
{
    /// <summary>
    /// Service quản lý giỏ hàng:
    /// - Dùng UserId nếu đã login, ngược lại tạo/ghi SessionId vào Session.
    /// - Tăng Product.ReservedQuantity mỗi khi add to cart (reserve stock).
    /// - Merge cart khi login: cộng dồn quantity, cap = stock khả dụng.
    /// </summary>
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _uow;
        private readonly IHttpContextAccessor _httpContext;

        public const string CartSessionKey = "OSBIS_CartSessionId";

        public CartService(IUnitOfWork uow, IHttpContextAccessor httpContext)
        {
            _uow = uow;
            _httpContext = httpContext;
        }

        // ============================================================
        // Helpers
        // ============================================================
        private int? CurrentUserId() => _httpContext.HttpContext?.User.GetUserId();

        private async Task<Cart?> GetOrCreateUserCartAsync(int userId)
        {
            var cart = await _uow.Carts.GetByUserIdWithItemsAsync(userId);
            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _uow.Carts.AddCartAsync(cart);
                cart = await _uow.Carts.GetByUserIdWithItemsAsync(userId);
            }
            return cart;
        }

        private async Task<Cart?> GetOrCreateGuestCartAsync()
        {
            var session = _httpContext.HttpContext!.Session;
            var sessionId = session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                session.SetString(CartSessionKey, sessionId);
            }

            var cart = await _uow.Carts.GetBySessionIdWithItemsAsync(sessionId);
            if (cart == null)
            {
                cart = new Cart
                {
                    SessionId = sessionId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _uow.Carts.AddCartAsync(cart);
                cart = await _uow.Carts.GetBySessionIdWithItemsAsync(sessionId);
            }
            return cart;
        }

        // ============================================================
        // Public API
        // ============================================================
        public async Task<Cart?> GetCurrentCartAsync()
        {
            var userId = CurrentUserId();
            if (userId.HasValue)
                return await GetOrCreateUserCartAsync(userId.Value);
            return await GetOrCreateGuestCartAsync();
        }

        public async Task<CartSummaryViewModel> GetCartSummaryAsync()
        {
            var cart = await GetCurrentCartAsync();
            var summary = new CartSummaryViewModel { CartId = cart?.CartId ?? 0 };

            if (cart == null || cart.CartItems.Count == 0)
                return summary;

            // Load product info
            var productIds = cart.CartItems.Select(ci => ci.ProductId).ToList();
            var products = await _uow.Products.GetByIdsWithPrimaryImageAsync(productIds);

            foreach (var item in cart.CartItems)
            {
                var product = products.FirstOrDefault(p => p.ProductId == item.ProductId);
                if (product == null) continue;

                var primaryImage = product.ProductImages?.FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl;
                var available = product.GetAvailableStock();
                if (available < 0) available = 0;

                summary.Items.Add(new CartItemViewModel
                {
                    CartItemId = item.CartItemId,
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    SKU = product.SKU,
                    ImageUrl = primaryImage,
                    UnitOfMeasure = product.UnitOfMeasure ?? string.Empty,
                    UnitPrice = product.UnitPrice,
                    Quantity = item.Quantity,
                    AvailableStock = available,
                    Weight = product.Weight
                });
            }

            return summary;
        }

        public async Task<int> GetCartItemCountAsync()
        {
            var summary = await GetCartSummaryAsync();
            return summary.TotalQuantity;
        }

        public async Task<CartResult> AddItemAsync(int productId, int quantity)
        {
            if (quantity <= 0)
                return new CartResult { Success = false, Message = "Số lượng phải lớn hơn 0." };

            var cart = await GetCurrentCartAsync();
            if (cart == null)
                return new CartResult { Success = false, Message = "Không tìm thấy giỏ hàng." };

            var product = await _uow.Products.GetByIdAsync(productId);
            if (product == null || product.IsDeleted == true)
                return new CartResult { Success = false, Message = "Sản phẩm không tồn tại." };

            var available = product.GetAvailableStock();
            if (available < quantity)
                return new CartResult { Success = false, Message = $"Chỉ còn {available} sản phẩm trong kho." };

            try
            {
                await _uow.BeginTransactionAsync();

                // Update hoặc add cart item
                var existingItem = await _uow.Carts.GetCartItemAsync(cart.CartId, productId);
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                    _uow.Carts.UpdateCartItem(existingItem);
                }
                else
                {
                    await _uow.Carts.AddCartItemAsync(new CartItem
                    {
                        CartId = cart.CartId,
                        ProductId = productId,
                        Quantity = quantity
                    });
                }

                // Reserve stock
                product.ReservedQuantity += quantity;
                _uow.Products.Update(product);

                cart.UpdatedAt = DateTime.UtcNow;
                _uow.Carts.UpdateCart(cart);

                await _uow.CommitTransactionAsync();

                var newCount = await GetCartItemCountAsync();
                return new CartResult { Success = true, Message = "Đã thêm vào giỏ.", CartItemCount = newCount };
            }
            catch
            {
                await _uow.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<CartResult> UpdateQuantityAsync(int cartItemId, int newQuantity)
        {
            if (newQuantity <= 0)
                return await RemoveItemAsync(cartItemId);

            var item = await _uow.Carts.GetCartItemAsync(cartItemId);
            if (item == null)
                return new CartResult { Success = false, Message = "Không tìm thấy sản phẩm trong giỏ." };

            var product = await _uow.Products.GetByIdAsync(item.ProductId);
            if (product == null)
                return new CartResult { Success = false, Message = "Sản phẩm không tồn tại." };

            var delta = newQuantity - item.Quantity;
            var newAvailableForReserve = product.GetAvailableStock();

            if (delta > 0 && newAvailableForReserve < delta)
                return new CartResult { Success = false, Message = $"Chỉ thêm được {newAvailableForReserve} sản phẩm." };

            try
            {
                await _uow.BeginTransactionAsync();
                item.Quantity = newQuantity;
                _uow.Carts.UpdateCartItem(item);

                product.ReservedQuantity += delta;
                _uow.Products.Update(product);

                await _uow.CommitTransactionAsync();

                var count = await GetCartItemCountAsync();
                return new CartResult { Success = true, Message = "Đã cập nhật.", CartItemCount = count };
            }
            catch
            {
                await _uow.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<CartResult> RemoveItemAsync(int cartItemId)
        {
            var item = await _uow.Carts.GetCartItemAsync(cartItemId);
            if (item == null)
                return new CartResult { Success = false, Message = "Không tìm thấy sản phẩm." };

            var product = await _uow.Products.GetByIdAsync(item.ProductId);

            try
            {
                await _uow.BeginTransactionAsync();
                _uow.Carts.RemoveCartItem(item);

                if (product != null)
                {
                    product.ReservedQuantity = Math.Max(0, product.ReservedQuantity - item.Quantity);
                    _uow.Products.Update(product);
                }
                await _uow.CommitTransactionAsync();

                var count = await GetCartItemCountAsync();
                return new CartResult { Success = true, Message = "Đã xóa.", CartItemCount = count };
            }
            catch
            {
                await _uow.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task ClearCartAsync()
        {
            var cart = await GetCurrentCartAsync();
            if (cart == null) return;

            try
            {
                await _uow.BeginTransactionAsync();
                foreach (var item in cart.CartItems.ToList())
                {
                    var product = await _uow.Products.GetByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        product.ReservedQuantity = Math.Max(0, product.ReservedQuantity - item.Quantity);
                        _uow.Products.Update(product);
                    }
                    _uow.Carts.RemoveCartItem(item);
                }
                await _uow.CommitTransactionAsync();
            }
            catch
            {
                await _uow.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task MergeCartOnLoginAsync(int userId, string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return;

            var guestCart = await _uow.Carts.GetBySessionIdWithItemsAsync(sessionId);
            if (guestCart == null) return;

            var userCart = await _uow.Carts.GetByUserIdWithItemsAsync(userId);

            try
            {
                await _uow.BeginTransactionAsync();

                if (userCart == null)
                {
                    // Chỉ có guest cart → gán cho user
                    guestCart.UserId = userId;
                    guestCart.SessionId = null;
                    _uow.Carts.UpdateCart(guestCart);
                    await _uow.CommitTransactionAsync();
                    return;
                }

                // Có cả 2 cart → merge item
                foreach (var guestItem in guestCart.CartItems.ToList())
                {
                    var existingItem = userCart.CartItems.FirstOrDefault(ci => ci.ProductId == guestItem.ProductId);
                    var product = await _uow.Products.GetByIdAsync(guestItem.ProductId);
                    if (product == null)
                    {
                        _uow.Carts.RemoveCartItem(guestItem);
                        continue;
                    }

                    if (existingItem != null)
                    {
                        // Cộng dồn, cap = TotalStock
                        var maxQty = product.TotalStock;
                        var newQty = Math.Min(existingItem.Quantity + guestItem.Quantity, maxQty);

                        existingItem.Quantity = newQty;
                        _uow.Carts.UpdateCartItem(existingItem);

                        // Phần vượt cap (do cộng dồn nhiều hơn stock) sẽ KHÔNG tính thêm reserved.
                        var excess = (existingItem.Quantity + guestItem.Quantity) - newQty;
                        if (excess > 0)
                        {
                            product.ReservedQuantity = Math.Max(0, product.ReservedQuantity - excess);
                            _uow.Products.Update(product);
                        }
                    }
                    else
                    {
                        // Chuyển guest item sang user cart (giữ nguyên reserved)
                        guestItem.CartId = userCart.CartId;
                        _uow.Carts.UpdateCartItem(guestItem);
                    }
                }

                // Xóa guest cart (rỗng)
                if (!guestCart.CartItems.Any())
                {
                    _uow.Carts.RemoveCart(guestCart);
                }
                else
                {
                    guestCart.SessionId = null;
                    guestCart.UpdatedAt = DateTime.UtcNow;
                    _uow.Carts.UpdateCart(guestCart);
                }

                await _uow.CommitTransactionAsync();
            }
            catch
            {
                await _uow.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
