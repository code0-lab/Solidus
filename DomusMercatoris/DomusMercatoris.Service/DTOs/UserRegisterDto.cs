using System.ComponentModel.DataAnnotations;

namespace DomusMercatoris.Service.DTOs
{
    public class UserRegisterDto
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public int CompanyId { get; set; }
    }
}
