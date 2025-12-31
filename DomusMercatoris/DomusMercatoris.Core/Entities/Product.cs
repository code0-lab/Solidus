using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace DomusMercatoris.Core.Entities
{
    public class Product
    {
        [Key]
        public long Id { get; set; }

        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string Sku { get; set; } = string.Empty;

        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        public int? CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public ICollection<Category> Categories { get; set; } = new List<Category>();

        public int? BrandId { get; set; }
        public Brand? Brand { get; set; }

        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; } = 0;

        public List<string> Images { get; set; } = new List<string>();

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<VariantProduct> Variants { get; set; } = new List<VariantProduct>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
