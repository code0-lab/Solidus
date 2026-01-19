using System.ComponentModel.DataAnnotations;

namespace DomusMercatoris.Service.DTOs
{
    public class UpdateUserProfileDto
    {
        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }
    }
}

