using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DomusMercatorisDotnetMVC.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        public int CompanyId { get; set; }
        public int? ParentId { get; set; }
        public Category? Parent { get; set; }
        public ICollection<Category> Children { get; set; } = new List<Category>();
        public bool IsSubCategory => ParentId.HasValue;
        public Company? Company { get; set; }
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
