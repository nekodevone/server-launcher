using System.Text;

namespace ServerLauncher.Extensions;

public static class EnumerableExtensions
{
    /// <summary>
    /// Обьединяет аргументы
    /// </summary>
    /// <param name="arguments"></param>
    /// <returns></returns>
    public static string JoinArguments(this IEnumerable<string> arguments)
    {
        var argsStringBuilder = new StringBuilder();
        
        foreach (var argument in arguments)
        {
            if (argument == string.Empty)
                continue;

            // Escape escape characters (if not on Windows) and quotation marks
            var escapedArgument =
                OperatingSystem.IsWindows() ?
                    argument.Replace("\"", "\\\"") :
                    argument.Replace("\\", "\\\\").Replace("\"", "\\\"");

            // Separate with spaces
            if (argsStringBuilder.Length > 0)
            {
                argsStringBuilder.Append(' ');
            }

            // Handle spaces by surrounding with quotes
            if (escapedArgument.Contains(' '))
            {
                argsStringBuilder.Append('"');
                argsStringBuilder.Append(escapedArgument);
                argsStringBuilder.Append('"');
            }
            else
            {
                argsStringBuilder.Append(escapedArgument);
            }
        }

        return argsStringBuilder.ToString();
    }
}