using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OSBIS.Models.ViewModels.Product;
using OSBIS.Services.Interfaces;
using ProductEntity = OSBIS.Models.Entities.Product;
using CategoryEntity = OSBIS.Models.Entities.Category;

namespace OSBIS.Controllers.Customer
{
    /// <summary>
    /// Controller cho Customer duyệt sản phẩm (Phase 2).
    /// Route: /Customer/Product/...
    /// </summary>
    [Area("Customer")]
    [AllowAnonymous]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;

        public ProductController(IProductService productService, ICategoryService categoryService)
        {
            _productService = productService;
            _categoryService = categoryService;
        }

        // GET: /Customer/Product hoặc /Customer/Product/Index
        [HttpGet]
        public async Task<IActionResult> Index(
            int? categoryId,
            string? keyword,
            decimal? minPrice,
            decimal? maxPrice,
            bool inStockOnly = false,
            string sortBy = "CreatedDesc",
            int pageNumber = 1,
            int pageSize = 12)
        {
            if (!Enum.TryParse<OSBIS.Repositories.Specifications.ProductSortBy>(sortBy, true, out var sort))
                sort = OSBIS.Repositories.Specifications.ProductSortBy.CreatedDesc;

            var vm = new ProductListViewModel
            {
                CategoryId = categoryId,
                Keyword = keyword,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                InStockOnly = inStockOnly,
                SortBy = sort,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize
            };

            vm.Products = await _productService.GetProductsAsync(vm.ToSpec());
            vm.Categories = await _categoryService.GetAllWithChildrenAsync();

            return View(vm);
        }

        // GET: /Customer/Product/Detail/{id}
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var product = await _productService.GetProductDetailAsync(id);
            if (product == null) return NotFound();

            var related = await _productService.GetRelatedAsync(product.CategoryId, product.ProductId, 4);
            var vm = new ProductDetailViewModel
            {
                Product = product,
                RelatedProducts = related
            };
            return View(vm);
        }
    }
}
