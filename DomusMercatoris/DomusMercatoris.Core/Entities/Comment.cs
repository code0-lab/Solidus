using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomusMercatoris.Core.Entities
{
    [Table("Comments")]
    public class Comment
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

        [Column("Comment")]
        public string Text { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
