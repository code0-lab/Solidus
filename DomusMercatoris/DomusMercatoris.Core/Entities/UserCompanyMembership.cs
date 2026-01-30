using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomusMercatoris.Core.Entities
{
    public class UserCompanyMembership
    {
        [Key]
        public long Id { get; set; }

        public long UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public int CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public Company Company { get; set; } = null!;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
