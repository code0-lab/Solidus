using System.ComponentModel.DataAnnotations;
namespace DomusMercatoris.Core.Entities
{
    public class FleetingUser
    {
        [Key]
        public long Id { get; set; }
        [Required]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? FirstName { get; set; }
        [MaxLength(100)]
        public string? LastName { get; set; }
        [MaxLength(500)]
        public string? Address { get; set; }
    }
}
