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

        /// <summary>
        /// User's physical address.
        /// </summary>
        [StringLength(500)]
        public string? Address { get; set; }
    }
}

