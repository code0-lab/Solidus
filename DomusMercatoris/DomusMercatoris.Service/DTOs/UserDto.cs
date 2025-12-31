using System.Collections.Generic;

namespace DomusMercatoris.Service.DTOs
{
    public class UserDto
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public BanDto? Ban { get; set; }
    }
}
