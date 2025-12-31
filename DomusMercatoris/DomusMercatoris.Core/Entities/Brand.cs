using System.ComponentModel.DataAnnotations;

namespace DomusMercatoris.Core.Entities
{
    public class Brand
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
