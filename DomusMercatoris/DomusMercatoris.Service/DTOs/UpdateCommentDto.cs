using System.ComponentModel.DataAnnotations;

namespace DomusMercatoris.Service.DTOs
{
    public class UpdateCommentDto
    {
        [Required]
        public string Text { get; set; } = string.Empty;
        
        public bool IsApproved { get; set; }
    }
}
