using Microsoft.EntityFrameworkCore;
using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Models.Enums;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Interfaces;

namespace OSBIS.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _uow;

        public ReportService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<DashboardData> GetDashboardDataAsync()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var todayOrders = await _uow.Orders.GetPagedAsync(1, 1000);
            var todayFiltered = todayOrders.Items
                .Where(o => o.OrderDate >= today && o.OrderDate < tomorrow)
                .ToList();

            var todayRevenue = todayFiltered
                .Where(o => o.OrderStatus != OrderStatus.Cancelled && o.OrderStatus != OrderStatus.Refunded)
                .Sum(o => o.TotalAmount);

            var allOrders = await _uow.Orders.GetPagedAsync(1, 1000);
            var summary = BuildOrderSummary(allOrders.Items);

            var last30 = await GetRevenueByDateRangeAsync(today.AddDays(-29), today.AddDays(1));
            var topProducts = await GetTopSellingProductsAsync(10, today.AddDays(-29), today.AddDays(1));
            var inventory = await GetInventoryStatusAsync();

            var customers = await _uow.Users.GetUsersByRoleAsync("Customer");
            var totalProducts = await _uow.Products.GetPagedAsync(new Repositories.Specifications.ProductFilterSpec { PageNumber = 1, PageSize = 1000 });

            return new DashboardData
            {
                TodayRevenue = todayRevenue,
                TodayOrders = todayFiltered.Count,
                PendingOrders = summary.Pending,
                TotalCustomers = customers.Count(),
                TotalProducts = totalProducts.TotalCount,
                LowStockCount = inventory.Count(i => i.Available < 10),
                Last30Days = last30.ToList(),
                TopProducts = topProducts.ToList(),
                OrderSummary = summary
            };
        }

        public async Task<IReadOnlyList<(DateTime Date, decimal Revenue, int OrderCount)>> GetRevenueByDateRangeAsync(DateTime from, DateTime to)
        {
            var orders = await _uow.Orders.GetPagedAsync(1, 10000);
            var filtered = orders.Items
                .Where(o => o.OrderDate >= from && o.OrderDate < to)
                .Where(o => o.OrderStatus != OrderStatus.Cancelled && o.OrderStatus != OrderStatus.Refunded)
                .ToList();

            var result = filtered
                .GroupBy(o => o.OrderDate!.Value.Date)
                .Select(g => (Date: g.Key, Revenue: g.Sum(o => o.TotalAmount), OrderCount: g.Count()))
                .OrderBy(x => x.Date)
                .ToList();

            return result;
        }

        public async Task<IReadOnlyList<(int ProductId, string ProductName, int TotalSold, decimal Revenue)>>
            GetTopSellingProductsAsync(int topN, DateTime from, DateTime to)
        {
            var orders = await _uow.Orders.GetPagedAsync(1, 10000);
            var orderIds = orders.Items
                .Where(o => o.OrderDate >= from && o.OrderDate < to)
                .Where(o => o.OrderStatus != OrderStatus.Cancelled && o.OrderStatus != OrderStatus.Refunded)
                .Select(o => o.OrderId)
                .ToList();

            var details = new List<OrderDetail>();
            foreach (var oid in orderIds)
            {
                var order = await _uow.Orders.GetWithDetailsAsync(oid);
                if (order?.OrderDetails != null)
                    details.AddRange(order.OrderDetails);
            }

            var grouped = details
                .GroupBy(d => d.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(d => d.Quantity),
                    Revenue = g.Sum(d => d.Quantity * d.UnitPrice),
                    ProductName = g.First().ProductNameSnapshot
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(topN)
                .ToList();

            return grouped
                .Select(g => (g.ProductId, g.ProductName, g.TotalSold, g.Revenue))
                .ToList();
        }

        public async Task<IReadOnlyList<InventoryItem>> GetInventoryStatusAsync()
        {
            var paged = await _uow.Products.GetPagedAsync(new Repositories.Specifications.ProductFilterSpec { PageNumber = 1, PageSize = 1000 });
            var items = paged.Items.Select(p => new InventoryItem
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                SKU = p.SKU,
                TotalStock = p.TotalStock,
                ReservedQuantity = p.ReservedQuantity
            }).ToList();

            return items;
        }

        public async Task<OrderSummary> GetOrderSummaryAsync()
        {
            var paged = await _uow.Orders.GetPagedAsync(1, 10000);
            return BuildOrderSummary(paged.Items);
        }

        private static OrderSummary BuildOrderSummary(IEnumerable<Order> orders)
        {
            var list = orders.ToList();
            return new OrderSummary
            {
                Total = list.Count,
                Pending = list.Count(o => o.OrderStatus == OrderStatus.Pending),
                Confirmed = list.Count(o => o.OrderStatus == OrderStatus.Confirmed),
                Processing = list.Count(o => o.OrderStatus == OrderStatus.Processing),
                Shipped = list.Count(o => o.OrderStatus == OrderStatus.Shipped),
                Delivered = list.Count(o => o.OrderStatus == OrderStatus.Delivered),
                Completed = list.Count(o => o.OrderStatus == OrderStatus.Completed),
                Cancelled = list.Count(o => o.OrderStatus == OrderStatus.Cancelled),
                Refunded = list.Count(o => o.OrderStatus == OrderStatus.Refunded)
            };
        }
    }
}
