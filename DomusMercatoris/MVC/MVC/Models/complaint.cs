using System;
using System.ComponentModel.DataAnnotations;

namespace DomusMercatorisDotnetMVC.Models
{
    public class Complaint
    {
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string? Title { get; set; } 
        [Required]
        [StringLength(500)]
        public string? Description { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign keys
        public long UserId { get; set; }
        public int CompanyId { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public Company? Company { get; set; }
    }
}
