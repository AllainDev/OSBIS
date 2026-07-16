using Microsoft.EntityFrameworkCore;
using OSBIS.Data;
using OSBIS.Models.Entities;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Interfaces;
using Serilog;

namespace OSBIS.Jobs
{
    /// <summary>
    /// Background job kiểm tra lô hàng sắp hết hạn (dưới 30 ngày) và gửi thông báo cho Staff/Admin.
    /// Chạy mỗi 24 giờ.
    /// </summary>
    public class ExpiringBatchNotificationJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ExpiringBatchNotificationJob(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Information("[ExpiringBatchNotificationJob] Started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndNotifyAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[ExpiringBatchNotificationJob] Error during checking");
                }

                // Mỗi 24 giờ
                try
                {
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
                catch (TaskCanceledException) { /* shutdown */ }
            }

            Log.Information("[ExpiringBatchNotificationJob] Stopped");
        }

        private async Task CheckAndNotifyAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            
            Log.Information("[ExpiringBatchNotificationJob] Checking and generating notifications...");
            await notificationService.GenerateExpiringBatchNotificationsAsync();
        }
    }
}
