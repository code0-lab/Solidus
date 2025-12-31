using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomusMercatoris.Core.Entities
{
    public class VariantProduct
    {
        [Key]
        public long Id { get; set; }

        public long ProductId { get; set; }
        public Product Product { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Color { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string? CoverImage { get; set; }

        public bool IsCustomizable { get; set; } = true;
    }
}
