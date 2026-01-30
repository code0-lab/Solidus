namespace DomusMercatoris.Core.Constants
{
    public static class AppConstants
    {
        public static class CustomClaimTypes
        {
            public const string CompanyId = "CompanyId";
            public const string UserId = "UserId";
        }

        public static class Roles
        {
            public const string Customer = "Customer";
            public const string User = "User";
            public const string Manager = "Manager";
            public const string Rex = "Rex";
            public const string Moderator = "Moderator";
            public const string Banned = "Baned"; // Intentionally "Baned" to match existing typo
        }

        public static class SessionKeys
        {
            public const string Role = "Role";
            public const string Email = "Email";
        }
    }
}
