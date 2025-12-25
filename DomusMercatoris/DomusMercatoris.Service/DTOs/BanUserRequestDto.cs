using System;

namespace DomusMercatoris.Service.DTOs
{
    public class BanUserRequestDto
    {
        public long UserId { get; set; }
        public bool PermaBan { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Reason { get; set; }
    }
}
