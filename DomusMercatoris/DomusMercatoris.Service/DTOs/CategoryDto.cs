using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace DomusMercatoris.Service.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? ParentId { get; set; }
        public List<CategoryDto> Children { get; set; } = new List<CategoryDto>();
    }
}
