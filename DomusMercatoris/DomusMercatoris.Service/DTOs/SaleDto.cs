namespace DomusMercatoris.Service.DTOs
{
    public class SaleDto
    {
        public long Id { get; set; }
        public bool IsPaid { get; set; }
        public decimal TotalPrice { get; set; }
        public int CompanyId { get; set; }
        public long? UserId { get; set; }
        public long? FleetingUserId { get; set; }
        public int? CargoTrackingId { get; set; }
    }
}
