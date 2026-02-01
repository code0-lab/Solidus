namespace DomusMercatoris.Core.Constants
{
    public static class ValidationConstants
    {
        public static class Password
        {
            // Minimum 6 characters. No other strict requirements (Upper, Lower, Special etc. are optional)
            public const string Regex = @"^.{6,}$";
            public const string ErrorMessage = "Password must be at least 6 characters long.";
            public const int MinLength = 6;
            public const int MaxLength = 200;
        }
    }
}
