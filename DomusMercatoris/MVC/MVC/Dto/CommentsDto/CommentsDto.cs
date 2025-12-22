using System;
using System.ComponentModel.DataAnnotations;

namespace DomusMercatorisDotnetMVC.Dto.CommentsDto
{
    public class CommentsDto
    {
        public int Id { get; set; }
        public long ProductId { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; } = string.Empty; // Added for display purposes
        public string Comment { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateCommentDto
    {
        [Required]
        public long ProductId { get; set; }

        [Required]
        public string Comment { get; set; } = string.Empty;
        
        public long? UserId { get; set; } 
    }
}
