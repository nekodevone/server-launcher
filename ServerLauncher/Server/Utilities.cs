using System.Text;

namespace ServerLauncher.Server;

public static class Utilities
{
    public static object Lock { get; } = new object();
    
    /// <summary>
    /// Получает индекс нон-эксейпед символа в строке
    /// </summary>
    /// <param name="inString"></param>
    /// <param name="inChar"></param>
    /// <param name="startIndex"></param>
    /// <param name="count"></param>
    /// <param name="escapeChar"></param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static int IndexOfNonEscaped(string inString, char inChar, int startIndex, int count, char escapeChar = '\\')
    {
        if (inString == null)
        {
            throw new NullReferenceException();
        }

        if (startIndex < 0 || startIndex >= inString.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }

        if (count < 0 || startIndex + count > inString.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        bool escaped = false;
        for (int i = 0; i < count; i++)
        {
            int stringIndex = startIndex + i;
            char stringChar = inString[stringIndex];

            if (!escaped)
            {
                if (stringChar == escapeChar && (escapeChar != inChar || ((i + 1) < count && inString[startIndex + i + 1] == escapeChar)))
                {
                    escaped = true;
                    continue;
                }
            }

            // If the character isn't escaped or the character that's escaped is an escape character then check if it matches
            if ((!escaped || (stringChar == escapeChar && escapeChar != inChar)) && stringChar == inChar)
            {
                return stringIndex;
            }

            escaped = false;
        }

        return -1;
    }

    /// <summary>
    /// Получает индекс нон-эксейпед символа в строке
    /// </summary>
    /// <param name="inString"></param>
    /// <param name="inChar"></param>
    /// <param name="startIndex"></param>
    /// <param name="escapeChar"></param>
    /// <returns></returns>
    public static int IndexOfNonEscaped(string inString, char inChar, int startIndex, char escapeChar = '\\')
    {
        return IndexOfNonEscaped(inString, inChar, startIndex, inString.Length - startIndex, escapeChar);
    }

    /// <summary>
    /// Получает индекс нон-эксейпед символа в строке
    /// </summary>
    /// <param name="inString"></param>
    /// <param name="inChar"></param>
    /// <param name="escapeChar"></param>
    /// <returns></returns>
    public static int IndexOfNonEscaped(string inString, char inChar, char escapeChar = '\\')
    {
        return IndexOfNonEscaped(inString, inChar, 0, inString.Length, escapeChar);
    }
    
    /// <summary>
    /// Превращает строку аргументов в массив из аргументов
    /// </summary>
    /// <param name="inString"></param>
    /// <param name="startIndex"></param>
    /// <param name="count"></param>
    /// <param name="separator"></param>
    /// <param name="escapeChar"></param>
    /// <param name="quoteChar"></param>
    /// <param name="keepQuotes"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string[] StringToArgs(string inString, int startIndex, int count, char separator = ' ', char escapeChar = '\\', char quoteChar = '\"', bool keepQuotes = false)
    {
        if (inString == null)
        {
            return null;
        }

        if (startIndex < 0 || startIndex >= inString.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }

        if (count < 0 || startIndex + count > inString.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (inString == string.Empty)
            return Array.Empty<string>();

        var args = new List<string>();
        var strBuilder = new StringBuilder();
        var inQuotes = false;
        var escaped = false;

        for (var i = 0; i < count; i++)
        {
            var stringChar = inString[startIndex + i];

            if (!escaped)
            {
                if (stringChar == escapeChar && (escapeChar != quoteChar || ((i + 1) < count && inString[startIndex + i + 1] == escapeChar)))
                {
                    escaped = true;
                    continue;
                }

                if (stringChar == quoteChar && (inQuotes || ((i + 1) < count && IndexOfNonEscaped(inString, quoteChar, startIndex + (i + 1), count - (i + 1), escapeChar) > 0)))
                {
                    // Ignore quotes if there's no future non-escaped quotes

                    inQuotes = !inQuotes;
                    if (!keepQuotes)
                        continue;
                }
                else if (!inQuotes && stringChar == separator)
                {
                    args.Add(strBuilder.ToString());
                    strBuilder.Clear();
                    continue;
                }
            }

            strBuilder.Append(stringChar);
            escaped = false;
        }

        args.Add(strBuilder.ToString());

        return args.ToArray();
    }
    
    /// <summary>
    /// Получает название эксешника в зависимости от ОС
    /// </summary>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public static string GetExecutablePath()
    {
        string name;

        if (OperatingSystem.IsLinux())
        {
            name = "SCPSL.x86_64";
        }
        else if (OperatingSystem.IsWindows())
        {
            name = "SCPSL.exe";
        }
        else
        {
            throw new FileNotFoundException("Invalid OS, can't run executable");
        }
        
        if (!File.Exists(name))
        {
            throw new FileNotFoundException(
                $"Can't find game executable \"{name}\", the working directory must be the game directory");
        }
        
        return name;
    }
    
    /// <summary>
    /// Возвращает полный путь для указанного пути
    /// </summary>
    /// <param name="path">Путь</param>
    /// <returns>Полный путь</returns>
    public static string GetFullPathSafe(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(path);
    }
    
    /// <summary>
    /// Превращает строку аргументов в массив из аргументов
    /// </summary>
    /// <param name="inString"></param>
    /// <param name="startIndex"></param>
    /// <param name="separator"></param>
    /// <param name="escapeChar"></param>
    /// <param name="quoteChar"></param>
    /// <param name="keepQuotes"></param>
    /// <returns></returns>
    public static string[] StringToArgs(string inString, int startIndex, char separator = ' ', char escapeChar = '\\', char quoteChar = '\"', bool keepQuotes = false)
    {
        return StringToArgs(inString, startIndex, inString.Length - startIndex, separator, escapeChar, quoteChar, keepQuotes);
    }

    /// <summary>
    /// Превращает строку аргументов в массив из аргументов
    /// </summary>
    /// <param name="inString"></param>
    /// <param name="separator"></param>
    /// <param name="escapeChar"></param>
    /// <param name="quoteChar"></param>
    /// <param name="keepQuotes"></param>
    /// <returns></returns>
    public static string[] StringToArgs(string inString, char separator = ' ', char escapeChar = '\\', char quoteChar = '\"', bool keepQuotes = false)
    {
        return StringToArgs(inString, 0, inString.Length, separator, escapeChar, quoteChar, keepQuotes);
    }
}