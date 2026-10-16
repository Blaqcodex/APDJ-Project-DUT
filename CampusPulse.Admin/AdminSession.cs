namespace CampusPulse.Admin
{
    public static class AdminSession
    {
        public static string? Token { get; private set; }
        public static string DisplayName { get; private set; } = "";

        public static bool IsAuthenticated =>
            !string.IsNullOrWhiteSpace(Token);

        public static void SignIn(string token, string displayName)
        {
            Token = token;
            DisplayName = displayName;
        }

        public static void SignOut()
        {
            Token = null;
            DisplayName = "";
        }
    }
}