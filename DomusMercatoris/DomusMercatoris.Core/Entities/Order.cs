using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace DomusMercatoris.Core.Entities
{
    public class Order
    {
        [Key]
        public long Id { get; set; }
        public long UserId { get; set; }
        public User? User { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public decimal TotalPrice { get; set; } = 0;
        public DomusMercatoris.Core.Models.OrderStatus Status { get; set; } = DomusMercatoris.Core.Models.OrderStatus.Created;
        public bool IsPaid { get; set; } = false;
        public DateTime? PaidAt { get; set; } = null;
        public string? PaymentMethod { get; set; } = null;
        public bool IsRefunded { get; set; } = false;
        public int CompanyId { get; set; }
        public Company? Company { get; set; }
        public int? CargoTrackingId { get; set; }
        public CargoTracking? CargoTracking { get; set; }
        public long? FleetingUserId { get; set; }
        public FleetingUser? FleetingUser { get; set; }
        public string? PaymentCode { get; set; }
    }
}
