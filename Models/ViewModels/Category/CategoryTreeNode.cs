using OSBIS.Models.Entities;
using CategoryEntity = OSBIS.Models.Entities.Category;

namespace OSBIS.Models.ViewModels.Category
{
    /// <summary>
    /// Flat-list dạng tree — dùng cho View menu sidebar Admin.
    /// </summary>
    public class CategoryTreeNode
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public int? ParentCategoryId { get; set; }
        public int Level { get; set; } // 0 = root
        public int ProductCount { get; set; }

        public List<CategoryTreeNode> Children { get; set; } = new();

        public static List<CategoryTreeNode> Flatten(IEnumerable<CategoryEntity> roots)
        {
            var list = new List<CategoryTreeNode>();
            void Walk(CategoryEntity c, int level)
            {
                list.Add(new CategoryTreeNode
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    ParentCategoryId = c.ParentCategoryId,
                    Level = level,
                    ProductCount = c.Products?.Count ?? 0
                });
                foreach (var sub in c.SubCategories ?? new List<CategoryEntity>())
                    Walk(sub, level + 1);
            }
            foreach (var r in roots) Walk(r, 0);
            return list;
        }
    }
}
