using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomusMercatorisDotnetMVC.Models
{
    public class Ban
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }
        [ForeignKey("CompanyId")]
        public Company? Company { get; set; }
        public bool BannedForAllCompanies { get; set; } = false;
        public DateTime? BanedDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Reason { get; set; }
        public bool PermaBan { get; set; } = false;
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool ObjectToBan { get; set; } = false;
        public string? Object { get; set; }

        public bool IsBaned => EndDate > DateTime.UtcNow || PermaBan;
    }
}