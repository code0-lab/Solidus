using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DomusMercatoris.Core.Enums;

namespace DomusMercatoris.Core.Entities
{
    public class CompanyCustomerBlacklist
    {
        [Key]
        public int Id { get; set; }

        public int CompanyId { get; set; }
        
        [ForeignKey(nameof(CompanyId))]
        public virtual Company? Company { get; set; }

        public long CustomerId { get; set; }
        
        [ForeignKey(nameof(CustomerId))]
        public virtual User? Customer { get; set; }

        public BlacklistStatus Status { get; set; } = BlacklistStatus.None;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
