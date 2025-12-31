using System.ComponentModel.DataAnnotations;
namespace DomusMercatoris.Service.DTOs
{
    public class FleetingUserDto
    {
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;
        [StringLength(100)]
        public string? FirstName { get; set; }
        [StringLength(100)]
        public string? LastName { get; set; }
        [StringLength(500)]
        public string? Address { get; set; }
    }
}
