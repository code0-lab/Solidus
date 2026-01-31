using System;
using System.ComponentModel.DataAnnotations;

namespace DomusMercatoris.Core.Entities
{
    public class ApiKey
    {
        public int Id { get; set; }

        public int CompanyId { get; set; }
        public Company Company { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } // Description/Name for the key

        [Required]
        [MaxLength(256)]
        public string KeyHash { get; set; } // Stored as HASH only

        [Required]
        [MaxLength(10)]
        public string Prefix { get; set; } // First few chars to show user

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
