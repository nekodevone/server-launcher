using ServerLauncher.Utility;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace ServerLauncher.Logger
{
    public sealed class Log : IDisposable
    {
        public static Log Instance;

        private readonly StreamWriter _streamWriter;

        private readonly string _path;

        private readonly ConcurrentDictionary<string, string> _classNameCache = new();

        public Log(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory ?? throw new ArgumentNullException(nameof(directory)));
            }

            _path = Path.Combine(directory, $"{Utilities.DateTime}.log");
            _streamWriter = File.AppendText(_path);
        }

        /// <summary>
        /// Очистка
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Информационное сообщение в логи
        /// </summary>
        /// <param name="message">Сообщение</param>
        public static void Info(string message,
            [CallerFilePath] string filePath = "",
            ConsoleColor color = ConsoleColor.Cyan)
        {
            var className = Instance._classNameCache.GetOrAdd(filePath, path =>
                Path.GetFileNameWithoutExtension(path).ToUpper());

            Send("[" + className + "] " + message, LogLevel.Info, color);
        }

        /// <summary>
        /// Предупреждающее сообщение в логи
        /// </summary>
        /// <param name="message">Сообщение</param>
        public static void Warning(string message,
            [CallerFilePath] string filePath = "")
        {
            var className = Instance._classNameCache.GetOrAdd(filePath, path =>
                Path.GetFileNameWithoutExtension(path).ToUpper());

            Send("[" + className + "] " + message, LogLevel.Warn, ConsoleColor.Yellow);
        }

        /// <summary>
        /// Сообщение об ошибке в логи
        /// </summary>
        /// <param name="message">Сообщение</param>
        public static void Error(string message,
            [CallerFilePath] string filePath = "")
        {
            var className = Instance._classNameCache.GetOrAdd(filePath, path =>
                Path.GetFileNameWithoutExtension(path).ToUpper());

            Send("[" + className + "] " + message, LogLevel.Error, ConsoleColor.Red);
        }

        /// <summary>
        /// Дебаг сообщение
        /// </summary>
        /// <param name="message">Сообщение</param>
        public static void Debug(string message,
            [CallerFilePath] string filePath = "")
        {
            var className = Instance._classNameCache.GetOrAdd(filePath, path =>
                Path.GetFileNameWithoutExtension(path).ToUpper());

            Send("[" + className + "] " + message, LogLevel.Debug, ConsoleColor.DarkGray);
        }

        private static void Send(string message, LogLevel level, ConsoleColor color)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var formattedMessage = "[" + timestamp + "] [" + level.ToString().ToUpper() + "] " + message;
            SendRaw(formattedMessage, color);
        }

        private static void SendRaw(string message, ConsoleColor color)
        {
            Instance._streamWriter.Write(message);

            if (!message.EndsWith(Environment.NewLine))
            {
                Instance._streamWriter.WriteLine();
            }

            Instance._streamWriter.Flush();

            Console.ForegroundColor = color;
            Console.WriteLine(message, color);
            Console.ResetColor();
        }
    }
}
