namespace DomusMercatoris.Service.Interfaces
{
    public interface ICurrentUserService
    {
        long? UserId { get; }
        bool IsAuthenticated { get; }
        bool IsManager { get; }
        Task<bool> HasPermissionAsync(string permission);
    }
}
