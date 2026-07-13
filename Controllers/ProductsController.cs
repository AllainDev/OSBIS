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
        public async Task<IActionResult> Index(string searchString, int? categoryId)
        {
            // Lấy danh sách danh mục để đưa vào Dropdown lọc
            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsDeleted != true)
                .ToListAsync();

            // Khởi tạo Query lấy sản phẩm, bao gồm cả Hình ảnh và Danh mục
            var productsQuery = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .Where(p => p.IsDeleted != true)
                .AsQueryable();

            // Lọc theo từ khóa (Tìm trong tên hoặc mô tả)
            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(p =>
                    p.ProductName.Contains(searchString) ||
                    (p.Description != null && p.Description.Contains(searchString)));
            }

            // Lọc theo Category
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            // Trả về dữ liệu cho View
            var products = await productsQuery.ToListAsync();
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
