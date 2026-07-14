using Microsoft.EntityFrameworkCore;
using OSBIS.Data;
using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;

namespace OSBIS.Repositories.Implementations
{
    public class CartRepository : ICartRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<Cart> _dbSet;
        private readonly DbSet<CartItem> _itemSet;

        public CartRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Carts;
            _itemSet = context.CartItems;
        }

        public async Task<Cart?> GetByUserIdAsync(int userId)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task<Cart?> GetByUserIdWithItemsAsync(int userId)
        {
            return await _dbSet
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task<Cart?> GetBySessionIdAsync(string sessionId)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.SessionId == sessionId);
        }

        public async Task<Cart?> GetBySessionIdWithItemsAsync(string sessionId)
        {
            return await _dbSet
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.SessionId == sessionId);
        }

        public async Task<Cart?> GetByIdWithItemsAsync(int cartId)
        {
            return await _dbSet
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.CartId == cartId);
        }

        public async Task<CartItem?> GetCartItemAsync(int cartItemId)
        {
            return await _itemSet.FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);
        }

        public async Task<CartItem?> GetCartItemAsync(int cartId, int productId)
        {
            return await _itemSet.FirstOrDefaultAsync(ci => ci.CartId == cartId && ci.ProductId == productId);
        }

        public async Task<IReadOnlyList<Cart>> GetAbandonedAsync(DateTime threshold)
        {
            return await _dbSet
                .Include(c => c.CartItems)
                .Where(c => c.UpdatedAt < threshold)
                .ToListAsync();
        }

        public async Task AddCartAsync(Cart cart)
        {
            await _dbSet.AddAsync(cart);
            // KHÔNG SaveChanges — để UnitOfWork commit transaction
        }

        public async Task AddCartItemAsync(CartItem item)
        {
            await _itemSet.AddAsync(item);
            // KHÔNG SaveChanges — để UnitOfWork commit transaction
        }

        public void UpdateCart(Cart cart)
        {
            cart.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(cart);
            // KHÔNG SaveChanges — để UnitOfWork commit transaction
        }

        public void UpdateCartItem(CartItem item)
        {
            _itemSet.Update(item);
            // KHÔNG SaveChanges — để UnitOfWork commit transaction
        }

        public void RemoveCartItem(CartItem item)
        {
            _itemSet.Remove(item);
            // KHÔNG SaveChanges — để UnitOfWork commit transaction
        }

        public void RemoveCart(Cart cart)
        {
            _dbSet.Remove(cart);
            // KHÔNG SaveChanges — để UnitOfWork commit transaction
        }

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
