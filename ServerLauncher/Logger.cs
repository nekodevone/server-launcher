using ServerLauncher.Utility;

namespace ServerLauncher
{
    /// <summary>
    /// Класс логгера
    /// </summary>
    public class Logger : IDisposable
    {
        /// <summary>
        /// Инициализирует класс Logger
        /// </summary>
        /// <param name="directory">Путь где будет находится файл логов</param>
        public Logger(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory ?? throw new ArgumentNullException(nameof(directory)));
            }

            _path = Path.Combine(directory, $"{Utilities.DateTime}.log");

            _streamWriter = File.AppendText(_path);
        }

        private readonly StreamWriter _streamWriter;
        
        private readonly string _path;

        /// <summary>
        /// Выводит в консоль сообщение
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="message"></param>
        public void Log(object tag, object message)
        {
            Send($"[INFO] [{tag}] {message}", ConsoleColor.Blue);
        }

        /// <summary>
        /// Выводит в консоль сообщение
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="message"></param>
        public void Debug(object tag, object message)
        {
            Send($"[INFO] [{tag}] {message}", ConsoleColor.DarkGray);
        }
        
        /// <summary>
        /// Выводит в консоль сообщение
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="message"></param>
        public void Error(object tag, object message)
        {
            Send($"[ERROR] [{tag}] {message}", ConsoleColor.Red);
        }

        /// <summary>
        /// Выводит в консоль сообщение
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="message"></param>
        public void Warn(object tag, object message)
        {
            Send($"[WARN] [{tag}] {message}", ConsoleColor.Yellow);
        }

        public void Message(object tag, object message, ConsoleColor consoleColor)
        {
            Send($"[{tag}] {message}", consoleColor);
        }

        /// <summary>
        /// Очистка
        /// </summary>
        public void Dispose() => GC.SuppressFinalize(this);

        /// <summary>
        /// Добавляет к сообщению время, выводит в консоль и записывает в файл 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="consoleColor"></param>
        private void Send(string message, ConsoleColor consoleColor)
        {
            _streamWriter.Write(message);

            if (!message.EndsWith(Environment.NewLine))
            {
                _streamWriter.WriteLine();
            }

            _streamWriter.Flush();
            Console.ForegroundColor = consoleColor;
            Console.WriteLine($"[{DateTime.Now}] {message}");
        }
    }
}