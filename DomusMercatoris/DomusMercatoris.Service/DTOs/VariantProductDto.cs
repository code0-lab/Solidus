namespace DomusMercatoris.Service.DTOs
{
    public class VariantProductDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? CoverImage { get; set; }
        public bool IsCustomizable { get; set; }

        // Inherited properties
        public string? BrandName { get; set; }
        public List<string> CategoryNames { get; set; } = new List<string>();
        public int Quantity { get; set; }
        public List<string> Images { get; set; } = new List<string>();
    }
}
