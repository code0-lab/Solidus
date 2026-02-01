using System.ComponentModel.DataAnnotations;
using DomusMercatoris.Core.Constants;

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
        [StringLength(ValidationConstants.Password.MaxLength)]
        [RegularExpression(ValidationConstants.Password.Regex, ErrorMessage = ValidationConstants.Password.ErrorMessage)]
        public string Password { get; set; } = string.Empty;

        public int? CompanyId { get; set; }
    }
}
