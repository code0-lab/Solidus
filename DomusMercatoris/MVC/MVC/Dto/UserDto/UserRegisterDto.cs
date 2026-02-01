using System.ComponentModel.DataAnnotations;

namespace DomusMercatorisDotnetMVC.Dto.UserDto
{
    public class UserRegisterDto
    {
        [Required]
        [MaxLength(50)]
        [MinLength(2)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [MinLength(2)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        [MinLength(6)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$", ErrorMessage = "Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string Password { get; set; } = string.Empty; 

        [MaxLength(150)]
        public string? CompanyName { get; set; }
    }
}
