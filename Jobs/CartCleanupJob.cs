using Microsoft.EntityFrameworkCore;
using OSBIS.Data;
using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Interfaces;
using Serilog;

namespace OSBIS.Jobs
{
    /// <summary>
    /// Background job xóa cart abandoned sau X giờ (Phase 5).
    /// Mỗi giờ chạy 1 lần, giải phóng ReservedQuantity.
    /// </summary>
    public class CartCleanupJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public CartCleanupJob(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Information("[CartCleanupJob] Started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[CartCleanupJob] Error during cleanup");
                }

                // Mỗi 1 giờ
                try
                {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (TaskCanceledException) { /* shutdown */ }
            }

            Log.Information("[CartCleanupJob] Stopped");
        }

        private async Task CleanupAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var configService = scope.ServiceProvider.GetRequiredService<ISystemConfigService>();

            // Lấy threshold từ SystemConfig (mặc định 24h)
            var expiryHours = await configService.GetIntAsync("CartExpiryHours") ?? 24;
            var threshold = DateTime.UtcNow.AddHours(-expiryHours);

            Log.Information("[CartCleanupJob] Cleanup carts older than {Threshold}", threshold);

            // Lấy các cart abandoned (UpdatedAt < threshold)
            var carts = await uow.Carts.GetAbandonedAsync(threshold);
            if (carts.Count == 0)
            {
                Log.Information("[CartCleanupJob] No abandoned carts found");
                return;
            }

            await uow.BeginTransactionAsync();

            int cleanedCount = 0;
            foreach (var cart in carts)
            {
                foreach (var item in cart.CartItems)
                {
                    var product = await uow.Products.GetByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        product.ReservedQuantity = Math.Max(0, product.ReservedQuantity - item.Quantity);
                        uow.Products.Update(product);
                    }
                    uow.Carts.RemoveCartItem(item);
                }
                uow.Carts.RemoveCart(cart);
                cleanedCount++;
            }

            await uow.CommitTransactionAsync();

            Log.Information("[CartCleanupJob] Cleaned {Count} abandoned carts", cleanedCount);
        }
    }
}
