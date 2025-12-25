using System;
using System.ComponentModel.DataAnnotations;

namespace DomusMercatorisDotnetMVC.Dto.ComplaintDto
{
    public class ComplaintDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }
    }

    public class CreateComplaintDto
    {
        [Required]
        [StringLength(100)]
        public string? Title { get; set; }
        [Required]
        [StringLength(500)]
        public string? Description { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public int CompanyId { get; set; }
    }
}
