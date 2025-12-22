using System.ComponentModel.DataAnnotations;

namespace DomusMercatoris.Service.DTOs
{
    public class CreateCommentDto
    {
        [Required]
        public long ProductId { get; set; }
        
        [Required]
        public string Text { get; set; } = string.Empty;
    }
}
