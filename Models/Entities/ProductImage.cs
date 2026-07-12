namespace ORBIS.Models.Entities
{
    public class ProductImage
    {
        public int ImageId { get; set; }
        public int ProductId { get; set; }
        public string ImageUrl { get; set; } = null!;
        public bool? IsPrimary { get; set; }
        public byte? SortOrder { get; set; }

        public Product Product { get; set; } = null!;
    }
}
