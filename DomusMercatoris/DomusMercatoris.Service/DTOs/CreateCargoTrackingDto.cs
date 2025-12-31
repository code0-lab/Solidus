using System;
using System.ComponentModel.DataAnnotations;

namespace DomusMercatoris.Service.DTOs
{
    public class CreateCargoTrackingDto
    {
        [Required]
        [StringLength(50)]
        public string TrackingNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CarrierName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime? EstimatedDeliveryDate { get; set; }

        public long? UserId { get; set; }
    }
}