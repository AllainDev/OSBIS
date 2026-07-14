using System.Collections.Generic;

namespace OSBIS.Common
{
    /// <summary>
    /// Kết quả phân trang dùng chung cho mọi danh sách (Phase 2+).
    /// </summary>
    /// <typeparam name="T">Kiểu item</typeparam>
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public int TotalPages =>
            PageSize <= 0 ? 0 : (TotalCount + PageSize - 1) / PageSize;

        public bool HasPrevious => PageNumber > 1;
        public bool HasNext => PageNumber < TotalPages;

        public static PagedResult<T> Empty(int page = 1, int size = 10) => new()
        {
            PageNumber = page,
            PageSize = size
        };
    }
}
