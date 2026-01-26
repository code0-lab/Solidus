using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
namespace DomusMercatoris.Service.DTOs
{
    public class OrderCreateDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int CompanyId { get; set; }
        public long? UserId { get; set; }
        public FleetingUserDto? FleetingUser { get; set; }
        [MinLength(1)]
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }
}
