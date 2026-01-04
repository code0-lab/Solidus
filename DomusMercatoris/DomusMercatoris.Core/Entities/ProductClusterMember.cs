using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomusMercatoris.Core.Entities
{
    public class ProductClusterMember
    {
        [Key]
        public int Id { get; set; }

        public int ProductClusterId { get; set; }
        public ProductCluster ProductCluster { get; set; } = null!;

        public long ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}
