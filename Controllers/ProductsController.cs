using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ORBIS.Data;

namespace ORBIS.Controllers
{
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;

        // Inject DbContext vào Controller
        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Products (Trang danh sách sản phẩm có tính năng lọc)

        public async Task<IActionResult> Index(
    string searchString,
    int? categoryId,
    decimal? minPrice,
    decimal? maxPrice,
    string sortOrder,
    int page = 1)
        {
            int pageSize = 20;

            // Lấy danh sách danh mục
            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsDeleted != true)
                .ToListAsync();

            var productsQuery = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .Where(p => p.IsDeleted != true)
                .AsQueryable();

            // 1. Lọc theo từ khóa
            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(p =>
                    p.ProductName.Contains(searchString) ||
                    (p.Description != null && p.Description.Contains(searchString)));
            }

            // 2. Lọc theo Category
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            // 3. Lọc theo khoảng giá (Min - Max)
            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.UnitPrice >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.UnitPrice <= maxPrice.Value);
            }

            // 4. Sắp xếp (Sort)
            switch (sortOrder)
            {
                case "price_asc":
                    productsQuery = productsQuery.OrderBy(p => p.UnitPrice).ThenBy(p => p.ProductId);
                    break;
                case "price_desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.UnitPrice).ThenBy(p => p.ProductId);
                    break;
                case "name_asc":
                    productsQuery = productsQuery.OrderBy(p => p.ProductName).ThenBy(p => p.ProductId);
                    break;
                case "name_desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.ProductName).ThenBy(p => p.ProductId);
                    break;
                default: // Mặc định: Mới nhất
                    productsQuery = productsQuery.OrderByDescending(p => p.ProductId);
                    break;
            }

            // Đếm tổng số sản phẩm và tính số trang
            int totalItems = await productsQuery.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Cắt dữ liệu cho trang hiện tại
            var products = await productsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lưu trạng thái để truyền ra View
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchString = searchString;
            ViewBag.CategoryId = categoryId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SortOrder = sortOrder; // Giữ trạng thái của dropdown sắp xếp

            return View(products);
        }

        // GET: /Products/Detail/5 (Trang chi tiết sản phẩm)
        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Lấy chi tiết sản phẩm cùng với hình ảnh, danh mục
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id && m.IsDeleted != true);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}
