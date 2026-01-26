using System.ComponentModel.DataAnnotations;
namespace DomusMercatoris.Service.DTOs
{
    public class CartItemDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductImage { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public long? VariantProductId { get; set; }
        public string? VariantColor { get; set; }
        public int Quantity { get; set; }
        public int CompanyId { get; set; }
    }

    public class AddToCartDto
    {
        [Required]
        public long ProductId { get; set; }
        public long? VariantProductId { get; set; }
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartItemDto
    {
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public class SyncCartItemDto
    {
        public long ProductId { get; set; }
        public long? VariantProductId { get; set; }
        public int Quantity { get; set; }
    }
}