namespace ServerLauncher.Utility
{
    public static class EmptyExtensions
    {
        public static bool IsEmpty<T>(this T[] array)
        {
            return array.Length <= 0;
        }

        public static bool IsNullOrEmpty<T>(this T[] array)
        {
            return array?.IsEmpty() ?? true;
        }
    }
}