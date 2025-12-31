using System.ComponentModel.DataAnnotations;
namespace DomusMercatoris.Service.DTOs
{
    public class SaleItemDto
    {
        [Required]
        public long ProductId { get; set; }
        public long? VariantProductId { get; set; }
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;
    }
}
