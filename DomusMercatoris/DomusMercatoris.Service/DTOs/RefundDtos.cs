using System;
using DomusMercatoris.Core.Models;

namespace DomusMercatoris.Service.DTOs
{
    public class RefundRequestDto
    {
        public long Id { get; set; }
        public long OrderItemId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal RefundAmount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public RefundStatus Status { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateRefundRequestDto
    {
        public long OrderItemId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class UpdateRefundStatusDto
    {
        public long RefundRequestId { get; set; }
        public bool IsApproved { get; set; }
        public string? RejectionReason { get; set; }
    }
}
