namespace DomusMercatoris.Core.Models
{
    public enum OrderStatus
    {
        Created = 0,
        PaymentPending = 1,
        PaymentApproved = 2,
        PaymentFailed = 3,
        Preparing = 4,
        Shipped = 5,
        Delivered = 6
    }
}