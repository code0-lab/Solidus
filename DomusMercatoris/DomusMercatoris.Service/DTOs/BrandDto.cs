namespace DomusMercatoris.Service.DTOs
{
    public class BrandDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CompanyId { get; set; }
    }

    public class CreateBrandDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CompanyId { get; set; }
    }

    public class UpdateBrandDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
