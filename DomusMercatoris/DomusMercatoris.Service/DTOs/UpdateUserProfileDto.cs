using System.ComponentModel.DataAnnotations;

namespace DomusMercatoris.Service.DTOs
{
    /// <summary>
    /// Data transfer object for updating user profile information.
    /// </summary>
    public class UpdateUserProfileDto
    {
        /// <summary>
        /// User's phone number.
        /// </summary>
        [StringLength(50)]
        public string? Phone { get; set; }
        
        [StringLength(50)]
        public string? FirstName { get; set; }
        
        [StringLength(50)]
        public string? LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Required if changing email.
        /// </summary>
        public string? CurrentPassword { get; set; }

        /// <summary>
        /// User's physical address.
        /// </summary>
        [StringLength(500)]
        public string? Address { get; set; }
    }
}

