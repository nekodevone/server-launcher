namespace ServerLauncher.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Escapes this <see cref="string"/> for use with <see cref="string.Format"/>
        /// </summary>
        /// <param name="input">The <see cref="string"/> to escape</param>
        /// <returns>A <see cref="string"/> escaped for use with <see cref="string.Format"/></returns>
        public static string EscapeFormat(this string input)
        {
            return input?.Replace("{", "{{").Replace("}", "}}");
        }
    }
}