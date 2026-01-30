using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DomusMercatoris.Core.Enums;

namespace DomusMercatoris.Core.Entities
{
    public class WorkTask
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public TaskType Type { get; set; } = TaskType.General;
        
        public string? Description { get; set; }

        public long? OrderId { get; set; }
        public Order? Order { get; set; }

        public long AssignedToUserId { get; set; }
        public User? AssignedToUser { get; set; }

        public long CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        public long? ParentId { get; set; }
        public WorkTask? Parent { get; set; }
        public ICollection<WorkTask> Children { get; set; } = new List<WorkTask>();

        public bool IsCompleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public int CompanyId { get; set; }
    }
}
