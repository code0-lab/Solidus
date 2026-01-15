using System;

namespace DomusMercatoris.Service.DTOs
{
    public class CommentDto
    {
        public int Id { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public long UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public bool IsApproved { get; set; } = false;
        public int ModerationStatus { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
