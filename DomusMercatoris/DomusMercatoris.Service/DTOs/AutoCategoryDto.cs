using System.Collections.Generic;

namespace DomusMercatoris.Service.DTOs
{
    public class AutoCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProductClusterId { get; set; }
        public int? ParentId { get; set; }
        public List<AutoCategoryDto> Children { get; set; } = new List<AutoCategoryDto>();
    }
}
