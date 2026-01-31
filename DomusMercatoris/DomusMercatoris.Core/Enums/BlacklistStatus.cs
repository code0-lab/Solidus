namespace DomusMercatoris.Core.Enums
{
    [Flags]
    public enum BlacklistStatus
    {
        None = 0,
        CustomerBlockedCompany = 1,
        CompanyBlockedCustomer = 2,
        BothBlocked = CustomerBlockedCompany | CompanyBlockedCustomer
    }
}
