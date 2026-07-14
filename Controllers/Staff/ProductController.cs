using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Models.ViewModels.Product;
using OSBIS.Services.Interfaces;
using ProductEntity = OSBIS.Models.Entities.Product;

namespace OSBIS.Controllers.Staff
{
    /// <summary>
    /// Controller cho Staff quản lý sản phẩm (Phase 2).
    /// Authorize: Staff + Admin.
    /// </summary>
    [Area("Staff")]
    [Authorize(Roles = "Staff,Admin")]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IInventoryBatchService _inventoryBatchService;
        private readonly IProductImageService _productImageService;

        public ProductController(
            IProductService productService,
            ICategoryService categoryService,
            IInventoryBatchService inventoryBatchService,
            IProductImageService productImageService)
        {
            _productService = productService;
            _categoryService = categoryService;
            _inventoryBatchService = inventoryBatchService;
            _productImageService = productImageService;
        }

        // GET: /Staff/Product
        [HttpGet]
        public async Task<IActionResult> Index(
            int? categoryId,
            string? keyword,
            decimal? minPrice,
            decimal? maxPrice,
            bool inStockOnly = false,
            string sortBy = "CreatedDesc",
            int pageNumber = 1,
            int pageSize = 15)
        {
            if (!Enum.TryParse<OSBIS.Repositories.Specifications.ProductSortBy>(sortBy, true, out var sort))
                sort = OSBIS.Repositories.Specifications.ProductSortBy.CreatedDesc;

            var spec = new OSBIS.Repositories.Specifications.ProductFilterSpec
            {
                CategoryId = categoryId,
                Keyword = keyword,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                InStockOnly = inStockOnly,
                SortBy = sort,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                IncludeDeleted = true
            };

            var products = await _productService.GetProductsAsync(spec);
            var categories = await _categoryService.GetAllWithChildrenAsync();

            var vm = new ProductListViewModel
            {
                Products = products,
                CategoryId = categoryId,
                Keyword = keyword,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                InStockOnly = inStockOnly,
                SortBy = sort,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                Categories = categories
            };

            return View(vm);
        }

        // GET: /Staff/Product/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new ProductCreateEditViewModel();
            ViewBag.Categories = await _categoryService.GetAllActiveAsync();
            return View(vm);
        }

        // POST: /Staff/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateEditViewModel vm)
        {
            ViewBag.Categories = await _categoryService.GetAllActiveAsync();
            if (!ModelState.IsValid) return View(vm);

            try
            {
                var product = new ProductEntity
                {
                    CategoryId = vm.CategoryId,
                    SKU = vm.SKU.Trim(),
                    ProductName = vm.ProductName.Trim(),
                    Description = vm.Description,
                    UnitOfMeasure = vm.UnitOfMeasure.Trim(),
                    Weight = vm.Weight,
                    UnitPrice = vm.UnitPrice,
                    TotalStock = vm.TotalStock,
                    ReservedQuantity = 0
                };

                var images = vm.Images?.Where(i => i != null && i.Length > 0).ToArray();
                await _productService.CreateAsync(product, images, vm.PrimaryImageIndex);
                TempData["Success"] = "Đã thêm sản phẩm.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(vm);
            }
        }

        // GET: /Staff/Product/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productService.GetProductDetailAsync(id);
            if (product == null) return NotFound();

            var vm = new ProductCreateEditViewModel
            {
                ProductId = product.ProductId,
                CategoryId = product.CategoryId,
                SKU = product.SKU,
                ProductName = product.ProductName,
                Description = product.Description,
                UnitOfMeasure = product.UnitOfMeasure,
                Weight = product.Weight,
                UnitPrice = product.UnitPrice,
                TotalStock = product.TotalStock
            };

            ViewBag.Categories = await _categoryService.GetAllActiveAsync();
            ViewBag.Product = product;
            return View(vm);
        }

        // POST: /Staff/Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductCreateEditViewModel vm)
        {
            if (id != vm.ProductId) return BadRequest();
            ViewBag.Categories = await _categoryService.GetAllActiveAsync();

            if (!ModelState.IsValid)
            {
                var p = await _productService.GetProductDetailAsync(id);
                ViewBag.Product = p;
                return View(vm);
            }

            var product = await _productService.GetProductDetailAsync(id);
            if (product == null) return NotFound();

            product.CategoryId = vm.CategoryId;
            product.SKU = vm.SKU.Trim();
            product.ProductName = vm.ProductName.Trim();
            product.Description = vm.Description;
            product.UnitOfMeasure = vm.UnitOfMeasure.Trim();
            product.Weight = vm.Weight;
            product.UnitPrice = vm.UnitPrice;
            product.TotalStock = vm.TotalStock;

            try
            {
                await _productService.UpdateAsync(product);

                var newImages = vm.Images?.Where(i => i != null && i.Length > 0).ToArray();
                if (newImages != null && newImages.Length > 0)
                {
                    for (int i = 0; i < newImages.Length; i++)
                    {
                        var isPrimary = vm.PrimaryImageIndex.HasValue && vm.PrimaryImageIndex.Value == i;
                        await _productImageService.UploadAsync(product.ProductId, newImages[i], isPrimary);
                    }
                }

                TempData["Success"] = "Đã cập nhật sản phẩm.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Product = product;
                return View(vm);
            }
        }

        // POST: /Staff/Product/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _productService.DeleteAsync(id);
            TempData["Success"] = "Đã xóa sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Staff/Product/Batches/5
        [HttpGet]
        public async Task<IActionResult> Batches(int id)
        {
            var product = await _productService.GetProductDetailAsync(id);
            if (product == null) return NotFound();

            var batches = await _inventoryBatchService.GetByProductAsync(id);
            ViewBag.Product = product;
            return View(new InventoryBatchViewModel
            {
                ProductId = id,
                ProductName = product.ProductName,
                Batches = batches
            });
        }

        // POST: /Staff/Product/AddBatch
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBatch(InventoryBatchViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                var product = await _productService.GetProductDetailAsync(vm.ProductId);
                ViewBag.Product = product;
                vm.Batches = await _inventoryBatchService.GetByProductAsync(vm.ProductId);
                return View("Batches", vm);
            }

            try
            {
                await _inventoryBatchService.AddBatchAsync(new InventoryBatch
                {
                    ProductId = vm.ProductId,
                    BatchCode = vm.BatchCode.Trim(),
                    ManufactureDate = vm.ManufactureDate,
                    ExpiryDate = vm.ExpiryDate,
                    Quantity = vm.Quantity,
                    CostPrice = vm.CostPrice
                });
                TempData["Success"] = "Đã thêm lô hàng.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Batches), new { id = vm.ProductId });
        }

        // POST: /Staff/Product/DeleteImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId, int productId)
        {
            await _productImageService.DeleteAsync(imageId);
            TempData["Success"] = "Đã xóa ảnh.";
            return RedirectToAction(nameof(Edit), new { id = productId });
        }
    }
}
