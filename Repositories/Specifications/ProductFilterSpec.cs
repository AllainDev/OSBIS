using System.Linq.Expressions;
using OSBIS.Models.Entities;

namespace OSBIS.Repositories.Specifications
{
    /// <summary>
    /// Specification pattern cho Product. Phase 2 — dùng để build query filter động.
    /// </summary>
    public class ProductFilterSpec
    {
        public int? CategoryId { get; set; }
        public string? Keyword { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool InStockOnly { get; set; }
        public bool IncludeDeleted { get; set; } = false;

        // Paging
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;

        // Sorting
        public ProductSortBy SortBy { get; set; } = ProductSortBy.CreatedDesc;

        public Expression<Func<Product, bool>> ToPredicate()
        {
            Expression<Func<Product, bool>> filter = p => true;

            if (!IncludeDeleted)
                filter = Combine(filter, p => p.IsDeleted != true);

            if (CategoryId.HasValue)
                filter = Combine(filter, p => p.CategoryId == CategoryId.Value);

            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                var kw = Keyword.Trim().ToLower();
                filter = Combine(filter, p =>
                    p.ProductName.ToLower().Contains(kw) ||
                    p.SKU.ToLower().Contains(kw) ||
                    (p.Description != null && p.Description.ToLower().Contains(kw)));
            }

            if (MinPrice.HasValue)
                filter = Combine(filter, p => p.UnitPrice >= MinPrice.Value);

            if (MaxPrice.HasValue)
                filter = Combine(filter, p => p.UnitPrice <= MaxPrice.Value);

            if (InStockOnly)
                filter = Combine(filter, p => p.TotalStock - p.ReservedQuantity > 0);

            return filter;
        }

        public Func<IQueryable<Product>, IOrderedQueryable<Product>> ToOrderBy()
        {
            return SortBy switch
            {
                ProductSortBy.PriceAsc => q => q.OrderBy(p => p.UnitPrice),
                ProductSortBy.PriceDesc => q => q.OrderByDescending(p => p.UnitPrice),
                ProductSortBy.NameAsc => q => q.OrderBy(p => p.ProductName),
                ProductSortBy.NameDesc => q => q.OrderByDescending(p => p.ProductName),
                ProductSortBy.Oldest => q => q.OrderBy(p => p.CreatedAt),
                _ => q => q.OrderByDescending(p => p.CreatedAt)
            };
        }

        /// <summary>
        /// Kết hợp hai biểu thức Lambda bằng AND, đồng thời thay thế ParameterExpression
        /// của cả hai về cùng một parameter duy nhất. Đây là bước BẮT BUỘC, nếu không
        /// EF Core sẽ không dịch được biểu thức sang SQL (lỗi "could not be translated").
        /// </summary>
        private static Expression<Func<Product, bool>> Combine(
            Expression<Func<Product, bool>> left,
            Expression<Func<Product, bool>> right)
        {
            // Tạo parameter chung duy nhất "p"
            var param = Expression.Parameter(typeof(Product), "p");

            // Replace tất cả ParameterReference trong left.Body và right.Body về param
            var leftBody = new ParameterReplacer(param).Visit(left.Body)!;
            var rightBody = new ParameterReplacer(param).Visit(right.Body)!;

            var body = Expression.AndAlso(leftBody, rightBody);
            return Expression.Lambda<Func<Product, bool>>(body, param);
        }

        /// <summary>
        /// ExpressionVisitor thay thế tất cả ParameterExpression bằng một parameter thống nhất.
        /// </summary>
        private sealed class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _target;

            public ParameterReplacer(ParameterExpression target) => _target = target;

            protected override Expression VisitParameter(ParameterExpression node) => _target;
        }
    }

    public enum ProductSortBy
    {
        CreatedDesc = 0,
        PriceAsc = 1,
        PriceDesc = 2,
        NameAsc = 3,
        NameDesc = 4,
        Oldest = 5
    }
}
