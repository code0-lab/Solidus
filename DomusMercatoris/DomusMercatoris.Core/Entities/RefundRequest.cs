using System;
using System.ComponentModel.DataAnnotations;
using DomusMercatoris.Core.Models;

namespace DomusMercatoris.Core.Entities
{
    public class RefundRequest
    {
        [Key]
        public long Id { get; set; }

        public long OrderItemId { get; set; }
        public OrderItem OrderItem { get; set; } = null!;

        public int Quantity { get; set; }
        public string Reason { get; set; } = string.Empty; // User's reason

        public RefundStatus Status { get; set; } = RefundStatus.Pending;

        public string? RejectionReason { get; set; } // Company's reason if rejected

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
