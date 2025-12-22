using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace DomusMercatoris.Core.Entities
{
    public class Company
    {
        [Key]
        public int CompanyId { get; set; }

        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<User> Users { get; set; } = new List<User>();
        public List<Product> Products { get; set; } = new List<Product>();
        public List<Category> Categories { get; set; } = new List<Category>();
    }
}
