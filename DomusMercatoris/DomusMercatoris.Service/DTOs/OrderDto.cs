using DomusMercatoris.Core.Models;

namespace DomusMercatoris.Service.DTOs
{
    public class OrderDto
    {
        public long Id { get; set; }
        public bool IsPaid { get; set; }
        public decimal TotalPrice { get; set; }
        public OrderStatus Status { get; set; }
        public int CompanyId { get; set; }
        public long? UserId { get; set; }
        public long? FleetingUserId { get; set; }
        public int? CargoTrackingId { get; set; }
        public string? CargoTrackingNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? PaymentCode { get; set; }
        public UserDto? User { get; set; }
        public FleetingUserDto? FleetingUser { get; set; }
        public List<OrderItemDto> OrderItems { get; set; } = new();
    }
}
