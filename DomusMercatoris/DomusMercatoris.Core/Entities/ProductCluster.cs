using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomusMercatoris.Core.Entities
{
    public class ProductCluster
    {
        [Key]
        public int Id { get; set; }

        [StringLength(100)]
        public string? Name { get; set; } // "Living Room", "Vintage", etc. assigned by Rex

        public int Version { get; set; } // 1, 2, 3...
        
        public string? CentroidJson { get; set; } // Center point of the cluster (List<float>)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProductClusterMember> Members { get; set; } = new List<ProductClusterMember>();
        public ICollection<AutoCategory> AutoCategories { get; set; } = new List<AutoCategory>();
    }
}
