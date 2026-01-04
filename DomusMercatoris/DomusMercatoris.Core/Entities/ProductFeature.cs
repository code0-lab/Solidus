using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomusMercatoris.Core.Entities
{
    public class ProductFeature
    {
        [Key]
        public long Id { get; set; }

        public long ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public string FeatureVectorJson { get; set; } = string.Empty; // Stored as JSON array of floats

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
