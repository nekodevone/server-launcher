using ServerLauncher.Utility;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;

namespace ServerLauncher.Logger
{
    public sealed class Log : IDisposable
    {
        /// <summary>
        /// Инстант логгера
        /// </summary>
        public static Log Instance;

        /// <summary>
        /// Инструмент для записи в файл логов
        /// </summary>
        private readonly Dictionary<string, StreamWriter> _streamWriters = new();

        /// <summary>
        /// Кэш тегов из имен классов, откуда вызываются логи
        /// </summary>
        private readonly ConcurrentDictionary<string, string> _classNameCache = new();

        public Log(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory ?? throw new ArgumentNullException(nameof(directory)));
            }

            var path = Path.Combine(directory, $"default-{Utilities.DateTime}.log");
            _streamWriters.Add("default", File.AppendText(path));
        }

        public void InitializeServerLogger(string serverId, string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory ?? throw new ArgumentNullException(nameof(directory)));
            }

            var path = Path.Combine(directory, $"{serverId}-{Utilities.DateTime}.log");

            if (!_streamWriters.TryAdd(serverId, File.AppendText(path)))
            {
                _streamWriters[serverId] = File.AppendText(path);
            }
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
        public static void Info(string message, string serverId = "default",
            [CallerFilePath] string filePath = "",
            ConsoleColor color = ConsoleColor.Cyan)
        {
            var className = Instance._classNameCache.GetOrAdd(filePath, path =>
                Path.GetFileNameWithoutExtension(path).ToUpper());

            Send("[" + className + "] " + message, serverId, LogLevel.Info, color);
        }

        /// <summary>
        /// Предупреждающее сообщение в логи
        /// </summary>
        /// <param name="message">Сообщение</param>
        public static void Warning(string message, string serverId = "default",
            [CallerFilePath] string filePath = "")
        {
            var className = Instance._classNameCache.GetOrAdd(filePath, path =>
                Path.GetFileNameWithoutExtension(path).ToUpper());

            Send("[" + className + "] " + message, serverId, LogLevel.Warn, ConsoleColor.Yellow);
        }

        /// <summary>
        /// Сообщение об ошибке в логи
        /// </summary>
        /// <param name="message">Сообщение</param>
        public static void Error(string message, string serverId = "default",
            [CallerFilePath] string filePath = "")
        {
            var className = Instance._classNameCache.GetOrAdd(filePath, path =>
                Path.GetFileNameWithoutExtension(path).ToUpper());

            Send("[" + className + "] " + message, serverId, LogLevel.Error, ConsoleColor.Red);
        }

        /// <summary>
        /// Дебаг сообщение
        /// </summary>
        /// <param name="message">Сообщение</param>
        public static void Debug(string message, string serverId = "default",
            [CallerFilePath] string filePath = "")
        {
            var className = Instance._classNameCache.GetOrAdd(filePath, path =>
                Path.GetFileNameWithoutExtension(path).ToUpper());

            Send("[" + className + "] " + message, serverId, LogLevel.Debug, ConsoleColor.DarkGray);
        }

        /// <summary>
        /// Формирует сообщение для логов, добавляя ему цвет, время и тег уровня логгирования
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="level">Уровень логгирования</param>
        /// <param name="color">Цвет</param>
        private static void Send(string message, string serverId, LogLevel level, ConsoleColor color)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var formattedMessage = "[" + timestamp + "] [" + level.ToString().ToUpper() + "] " + message;
            SendRaw(formattedMessage, serverId, color);
        }

        /// <summary>
        /// Отправляет лог в консоль и файл
        /// </summary>
        /// <param name="message">Лог</param>
        /// <param name="color">Цвет для консоли</param>
        private static void SendRaw(string message, string serverId, ConsoleColor color)
        {
            if (!Instance._streamWriters.TryGetValue(serverId, out var writer))
            {
                if (!Instance._streamWriters.TryGetValue("default", out var defaultWriter))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Не удалось найти общий файл логов");
                    Console.ResetColor();
                }
                else
                {
                    var errMessage = $"Для сервера {serverId} не удалось найти файл логов чтобы доставить следующее сообщение:\n" +
                        $"{message}";
                    defaultWriter.Write(errMessage);

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(errMessage);
                    Console.ResetColor();
                }

                return;
            }

            writer.Write(message);

            if (!message.EndsWith(Environment.NewLine))
            {
                writer.WriteLine();
            }

            writer.Flush();

            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
