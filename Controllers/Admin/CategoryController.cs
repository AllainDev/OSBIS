using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSBIS.Models.Entities;
using OSBIS.Models.ViewModels.Category;
using OSBIS.Services.Interfaces;
using CategoryEntity = OSBIS.Models.Entities.Category;

namespace OSBIS.Controllers.Admin
{
    /// <summary>
    /// Controller cho Admin quản lý danh mục (Phase 2).
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // GET: /Admin/Category
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var roots = await _categoryService.GetAllWithChildrenAsync();
            var flat = CategoryTreeNode.Flatten(roots);
            return View(flat);
        }

        // GET: /Admin/Category/Create
        [HttpGet]
        public async Task<IActionResult> Create(int? parentId)
        {
            ViewBag.AllCategories = await _categoryService.GetAllActiveAsync();
            var vm = new CategoryViewModel { ParentCategoryId = parentId };
            return View(vm);
        }

        // POST: /Admin/Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel vm)
        {
            ViewBag.AllCategories = await _categoryService.GetAllActiveAsync();
            if (!ModelState.IsValid) return View(vm);

            var category = new CategoryEntity
            {
                CategoryName = vm.CategoryName.Trim(),
                Description = vm.Description,
                ParentCategoryId = vm.ParentCategoryId
            };

            await _categoryService.CreateAsync(category);
            TempData["Success"] = "Đã thêm danh mục.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Category/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category == null) return NotFound();

            var vm = new CategoryViewModel
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description,
                ParentCategoryId = category.ParentCategoryId
            };
            ViewBag.AllCategories = await _categoryService.GetAllActiveAsync();
            return View(vm);
        }

        // POST: /Admin/Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryViewModel vm)
        {
            if (id != vm.CategoryId) return BadRequest();
            ViewBag.AllCategories = await _categoryService.GetAllActiveAsync();

            if (!ModelState.IsValid) return View(vm);

            var category = await _categoryService.GetByIdAsync(id);
            if (category == null) return NotFound();

            // Chặn tự làm con của chính mình
            if (vm.ParentCategoryId == id)
            {
                ModelState.AddModelError(nameof(vm.ParentCategoryId), "Không thể chọn chính danh mục này làm danh mục cha.");
                return View(vm);
            }

            category.CategoryName = vm.CategoryName.Trim();
            category.Description = vm.Description;
            category.ParentCategoryId = vm.ParentCategoryId;

            await _categoryService.UpdateAsync(category);
            TempData["Success"] = "Đã cập nhật danh mục.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Category/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (ok, err) = await _categoryService.DeleteAsync(id);
            if (ok) TempData["Success"] = "Đã xóa danh mục.";
            else TempData["Error"] = err ?? "Không thể xóa.";
            return RedirectToAction(nameof(Index));
        }
    }
}
