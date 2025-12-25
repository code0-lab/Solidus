using System;

namespace DomusMercatoris.Service.DTOs
{
    public class BanDto
    {
        public bool IsBaned { get; set; }
        public bool PermaBan { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Reason { get; set; }
        public bool ObjectToBan { get; set; }
        public string? Object { get; set; }
    }
}
