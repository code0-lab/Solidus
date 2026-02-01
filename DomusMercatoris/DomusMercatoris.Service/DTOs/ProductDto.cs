using System.Text.Json.Serialization;

namespace DomusMercatoris.Service.DTOs
{
    /// <summary>
    /// Data Transfer Object for Product details
    /// </summary>
    public class ProductDto
    {
        /// <summary>
        /// Unique identifier for the product
        /// </summary>
        public long Id { get; set; }
        
        /// <summary>
        /// Name of the product
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        [JsonIgnore]
        public string Sku { get; set; } = string.Empty;
        
        /// <summary>
        /// Detailed description of the product
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Unit price of the product
        /// </summary>
        public decimal Price { get; set; }
        
        /// <summary>
        /// ID of the company selling this product
        /// </summary>
        public int CompanyId { get; set; }
        
        /// <summary>
        /// Available stock quantity
        /// </summary>
        public int Quantity { get; set; } 
        
        /// <summary>
        /// Threshold for low stock alert
        /// </summary>
        public int LowStockThreshold { get; set; }
        
        [JsonIgnore]
        public string? ShelfNumber { get; set; }
        
        /// <summary>
        /// List of product image URLs
        /// </summary>
        public List<string> Images { get; set; } = new List<string>();
        
        /// <summary>
        /// ID of the brand associated with the product
        /// </summary>
        public int? BrandId { get; set; }
        
        [JsonIgnore]
        public string? BrandName { get; set; }
        
        /// <summary>
        /// Primary Category ID
        /// </summary>
        public int? CategoryId { get; set; }
        
        /// <summary>
        /// Sub-Category ID (if applicable)
        /// </summary>
        public int? SubCategoryId { get; set; }
        
        /// <summary>
        /// Auto-generated Category ID from AI analysis
        /// </summary>
        public int? AutoCategoryId { get; set; }
        
        /// <summary>
        /// List of associated categories
        /// </summary>
        public List<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
        
        /// <summary>
        /// List of product variants (color, size, etc.)
        /// </summary>
        public List<VariantProductDto> Variants { get; set; } = new List<VariantProductDto>();
        
        /// <summary>
        /// Indicates if the product is blocked by the current user's company settings
        /// </summary>
        public bool IsBlockedByCompany { get; set; }
    }
}
