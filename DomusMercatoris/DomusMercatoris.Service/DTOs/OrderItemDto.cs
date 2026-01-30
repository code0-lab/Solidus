using System.ComponentModel.DataAnnotations;
namespace DomusMercatoris.Service.DTOs
{
    public class OrderItemDto
    {
        public long Id { get; set; }
        [Required]
        public long ProductId { get; set; }
        public string? ProductName { get; set; }
        public long? VariantProductId { get; set; }
        public string? VariantName { get; set; }
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;
        public decimal? UnitPrice { get; set; }
    }
}
