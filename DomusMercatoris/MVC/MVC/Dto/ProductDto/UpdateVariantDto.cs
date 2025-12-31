using System.ComponentModel.DataAnnotations;

namespace DomusMercatorisDotnetMVC.Dto.ProductDto
{
    public class UpdateVariantDto
    {
        [Required]
        public long Id { get; set; }

        [Required]
        public long ProductId { get; set; }

        [Required]
        public string Color { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        public string? CoverImage { get; set; }
        
        public bool IsCustomizable { get; set; }
    }
}
