using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
namespace DomusMercatoris.Service.DTOs
{
    public class SaleCreateDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int CompanyId { get; set; }
        public long? UserId { get; set; }
        public FleetingUserDto? FleetingUser { get; set; }
        [MinLength(1)]
        public List<SaleItemDto> Items { get; set; } = new List<SaleItemDto>();
    }
}
