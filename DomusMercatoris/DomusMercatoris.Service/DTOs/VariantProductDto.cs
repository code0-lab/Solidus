namespace DomusMercatoris.Service.DTOs
{
    /// <summary>
    /// Data Transfer Object for Product Variant details
    /// </summary>
    public class VariantProductDto
    {
        /// <summary>
        /// Unique identifier for the variant
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// ID of the parent product
        /// </summary>
        public long ProductId { get; set; }

        /// <summary>
        /// Name of the product
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Color of the variant
        /// </summary>
        public string Color { get; set; } = string.Empty;

        /// <summary>
        /// Price of the variant
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// URL of the cover image for this variant
        /// </summary>
        public string? CoverImage { get; set; }

        /// <summary>
        /// Indicates if the variant is customizable
        /// </summary>
        public bool IsCustomizable { get; set; }

        // Inherited properties
        /// <summary>
        /// Available stock quantity (inherited from product)
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// List of images (inherited from product)
        /// </summary>
        public List<string> Images { get; set; } = new List<string>();
    }
}
