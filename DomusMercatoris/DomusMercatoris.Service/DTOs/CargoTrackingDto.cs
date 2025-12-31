using DomusMercatoris.Core.Models;
using System;

namespace DomusMercatoris.Service.DTOs
{
    public class CargoTrackingDto
    {
        public int Id { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public string CarrierName { get; set; } = string.Empty;
        public CargoStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public long? UserId { get; set; }
        public string? UserName { get; set; }
    }
}