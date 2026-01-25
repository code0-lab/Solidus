using System.ComponentModel.DataAnnotations;

namespace DomusMercatoris.Service.DTOs
{
    /// <summary>
    /// Data transfer object for changing user password.
    /// </summary>
    public class ChangePasswordDto
    {
        /// <summary>
        /// Current password for verification.
        /// </summary>
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        /// <summary>
        /// New password to set.
        /// </summary>
        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Confirmation of the new password.
        /// </summary>
        [Required]
        [Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
