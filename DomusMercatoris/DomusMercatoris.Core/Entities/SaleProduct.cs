using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace DomusMercatoris.Core.Entities
{
    public class SaleProduct
    {
        [Key]
        public long Id { get; set; }
        public long SaleId { get; set; }
        public Sale Sale { get; set; } = null!;
        public long ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public long? VariantProductId { get; set; }
        public VariantProduct? VariantProduct { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
