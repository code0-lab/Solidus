using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomusMercatoris.Core.Entities
{
    public class AutoCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public ICollection<ProductCluster> ProductClusters { get; set; } = new List<ProductCluster>();

        public int? ParentId { get; set; }
        [ForeignKey("ParentId")]
        public AutoCategory? Parent { get; set; }
        public ICollection<AutoCategory> Children { get; set; } = new List<AutoCategory>();
        public bool IsSubCategory => ParentId.HasValue;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
