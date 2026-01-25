using System.ComponentModel.DataAnnotations;

namespace DomusMercatoris.Service.DTOs
{
    /// <summary>
    /// Data transfer object for changing user email.
    /// </summary>
    public class ChangeEmailDto
    {
        /// <summary>
        /// New email address to set.
        /// </summary>
        [Required]
        [EmailAddress]
        public string NewEmail { get; set; } = string.Empty;

        /// <summary>
        /// Current password for verification.
        /// </summary>
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;
    }
}
