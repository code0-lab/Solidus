using System;

namespace DomusMercatoris.Service.DTOs
{
    public class MembershipDto
    {
        public long Id { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }

    public class CompanySummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsMember { get; set; }
    }
}
