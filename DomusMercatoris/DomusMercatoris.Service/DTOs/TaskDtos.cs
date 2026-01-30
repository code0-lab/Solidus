using System;

namespace DomusMercatoris.Service.DTOs
{
    public class TaskDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public long? OrderId { get; set; }
        public long AssignedToUserId { get; set; }
        public string? AssignedToUserEmail { get; set; }
        public long CreatedByUserId { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public long? ParentId { get; set; }
    }

    public class CreateTaskDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public long? OrderId { get; set; }
        public long AssignedToUserId { get; set; }
        public long? ParentId { get; set; }
    }

    public class UpdateTaskDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public long AssignedToUserId { get; set; }
        public long? OrderId { get; set; }
    }
}
