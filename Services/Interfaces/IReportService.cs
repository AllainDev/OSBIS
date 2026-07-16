using OSBIS.Models.Entities;

namespace OSBIS.Services.Interfaces
{
    /// <summary>Service thống kê báo cáo cho Admin (Phase 5).</summary>
    public interface IReportService
    {
        Task<DashboardData> GetDashboardDataAsync();
        Task<IReadOnlyList<(DateTime Date, decimal Revenue, int OrderCount)>> GetRevenueByDateRangeAsync(DateTime from, DateTime to);
        Task<IReadOnlyList<(int ProductId, string ProductName, int TotalSold, decimal Revenue)>>
            GetTopSellingProductsAsync(int topN, DateTime from, DateTime to);
        Task<IReadOnlyList<InventoryItem>> GetInventoryStatusAsync();
        Task<OrderSummary> GetOrderSummaryAsync();
    }

    public class DashboardData
    {
        public decimal TodayRevenue { get; set; }
        public int TodayOrders { get; set; }
        public int PendingOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalProducts { get; set; }
        public int LowStockCount { get; set; }
        public List<(DateTime Date, decimal Revenue, int OrderCount)> Last30Days { get; set; } = new();
        public List<(int ProductId, string ProductName, int TotalSold, decimal Revenue)> TopProducts { get; set; } = new();
        public OrderSummary OrderSummary { get; set; } = new();
        public IReadOnlyList<InventoryBatch> ExpiringBatches { get; set; } = new List<InventoryBatch>();
    }

    public class InventoryItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int TotalStock { get; set; }
        public int ReservedQuantity { get; set; }
        public int ExpiredQuantity { get; set; }
        public int Available => Math.Max(0, TotalStock - ReservedQuantity - ExpiredQuantity);
    }

    public class OrderSummary
    {
        public int Pending { get; set; }
        public int Confirmed { get; set; }
        public int Processing { get; set; }
        public int Shipped { get; set; }
        public int Delivered { get; set; }
        public int Completed { get; set; }
        public int Cancelled { get; set; }
        public int Refunded { get; set; }
        public int Total { get; set; }
    }
}
