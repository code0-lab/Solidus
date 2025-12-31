using System;
using System.ComponentModel.DataAnnotations;
using DomusMercatoris.Core.Models;

namespace DomusMercatoris.Core.Entities
{
    public class CargoTracking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string TrackingNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CarrierName { get; set; } = string.Empty;

        public CargoStatus Status { get; set; } = CargoStatus.Pending;

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }

        public long? UserId { get; set; }
        public User? User { get; set; }
        public long? FleetingUserId { get; set; }
        public FleetingUser? FleetingUser { get; set; }
    }
}
