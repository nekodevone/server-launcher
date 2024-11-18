namespace ServerLauncher.Utility
{
    public static class DateTimeUtils
    {
        public static string GetDateTime() => DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss");
    }
}