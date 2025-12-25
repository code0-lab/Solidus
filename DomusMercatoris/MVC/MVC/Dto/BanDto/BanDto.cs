using System;
using System.ComponentModel.DataAnnotations;

namespace DomusMercatorisDotnetMVC.Dto.BanDto
{
    public class BanDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string? UserName { get; set; }
        public long? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public bool BannedForAllCompanies { get; set; }
        public DateTime? BanedDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Reason { get; set; }
        public bool PermaBan { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool ObjectToBan { get; set; }
        public string? Object { get; set; }
        public bool IsBaned { get; set; }
    }

    public class CreateBanDto
    {
        [Required]
        public long UserId { get; set; }
        public long? CompanyId { get; set; }
        public bool BannedForAllCompanies { get; set; } = false;
        public DateTime? EndDate { get; set; }
        public string? Reason { get; set; }
        public bool PermaBan { get; set; } = false;
        public bool ObjectToBan { get; set; } = false;
        public string? Object { get; set; }
    }
}
