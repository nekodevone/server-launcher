﻿using ServerLauncher.Utility;

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

            var path = Path.Combine(directory, $"{Utilities.DateTime}.log");

            if (_streamWriters.TryGetValue(serverId, out var existingWriter))
            {
                existingWriter.Close();
                existingWriter.Dispose();
                _streamWriters.Remove(serverId);
            }

            _streamWriters.TryAdd(serverId, File.AppendText(path));
        }

        /// <summary>
        /// Очистка
        /// </summary>
        public void Dispose()
        {
            // ReSharper disable once GCSuppressFinalizeForTypeWithoutDestructor
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Информационное сообщение в логи
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="serverId"></param>
        /// <param name="color"></param>
        public static void Info(string message, string serverId = "default",
            ConsoleColor color = ConsoleColor.White)
        {
            Send(message, serverId, color);
        }

        /// <summary>
        /// Предупреждающее сообщение в логи
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="serverId"></param>
        public static void Warning(string message, string serverId = "default")
        {
            Send(message, serverId, ConsoleColor.Yellow);
        }

        /// <summary>
        /// Сообщение об ошибке в логи
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="serverId"></param>
        public static void Error(string message, string serverId = "default")
        {
            Send(message, serverId, ConsoleColor.Red);
        }

        /// <summary>
        /// Дебаг сообщение
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="serverId"></param>
        public static void Debug(string message, string serverId = "default")
        {
            Send(message, serverId, ConsoleColor.DarkGray);
        }

        /// <summary>
        /// Формирует сообщение для логов, добавляя ему цвет и время
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="serverId"></param>
        /// <param name="color">Цвет</param>
        private static void Send(string message, string serverId, ConsoleColor color)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var formattedMessage = "[" + timestamp + "] " + message;
            SendRaw(formattedMessage, serverId, color);
        }

        /// <summary>
        /// Отправляет лог в консоль и файл
        /// </summary>
        /// <param name="message">Лог</param>
        /// <param name="serverId"></param>
        /// <param name="color">Цвет для консоли</param>
        private static void SendRaw(string message, string serverId, ConsoleColor color)
        {
            if (Instance is null)
            {
                return;
            }

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
                    var errMessage =
                        $"Для сервера {serverId} не удалось найти файл логов чтобы доставить следующее сообщение:\n" +
                        $"{message}";
                    defaultWriter.Write(errMessage);

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(errMessage);
                    Console.ResetColor();
                }

                return;
            }

            try
            {
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
            catch (Exception)
            {
                Error("Error while logging for Laucnher", serverId);
            }
        }
    }
}