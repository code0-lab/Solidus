using System.ComponentModel.DataAnnotations;

namespace DomusMercatoris.Service.DTOs
{
    public class CreateVariantProductDto
    {
        [Required]
        public long ProductId { get; set; }

        [Required]
        public string Color { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        public string? CoverImage { get; set; }
        
        public bool IsCustomizable { get; set; } = true;
    }
}
