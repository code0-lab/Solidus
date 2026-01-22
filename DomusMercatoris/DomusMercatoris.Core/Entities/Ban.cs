using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomusMercatoris.Core.Entities
{
    public class Ban
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public User? User { get; set; }
        
        public int? CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public Company? Company { get; set; }
        
        public bool BannedForAllCompanies { get; set; } = false;
        public DateTime? BannedDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Reason { get; set; }
        public bool PermaBan { get; set; } = false;
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool ObjectToBan { get; set; } = false;
        public string? Object { get; set; }

        public bool IsBanned => EndDate > DateTime.UtcNow || PermaBan;
    }
}
