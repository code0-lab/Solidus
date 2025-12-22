using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomusMercatorisDotnetMVC.Models
{
    public class CommentModel
    {
        [Key]
        public int Id { get; set; }

        public long ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        public bool IsApproved { get; set; } = false;

        public long UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

        public string Comment { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
