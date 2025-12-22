using System.Collections.Generic;

namespace DomusMercatoris.Service.DTOs
{
    public class ProductDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public List<string> Images { get; set; } = new List<string>();
        public List<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    }
}
