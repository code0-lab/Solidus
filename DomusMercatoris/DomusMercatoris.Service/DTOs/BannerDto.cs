namespace DomusMercatoris.Service.DTOs
{
    public class BannerDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateBannerDto
    {
        public int CompanyId { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
    }

    public class UpdateBannerStatusDto
    {
        public bool IsApproved { get; set; }
        public bool IsActive { get; set; }
    }
}

