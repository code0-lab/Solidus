using System.ComponentModel.DataAnnotations;
using DomusMercatoris.Core.Constants;

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
        [RegularExpression(ValidationConstants.Password.Regex, ErrorMessage = ValidationConstants.Password.ErrorMessage)]
        public string Password { get; set; } = string.Empty; 

        [MaxLength(150)]
        public string? CompanyName { get; set; }
    }
}
