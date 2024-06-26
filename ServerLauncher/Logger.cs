﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerLauncher
{
    /// <summary>
    /// Класс логгера
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// Инициализирует класс Logger
        /// </summary>
        /// <param name="directory">Путь где будет находится файл логов</param>
        public Logger(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            _path = Path.Combine(directory, "logs.log");

            if (File.Exists(_path))
            {
                return;
            }

            File.Create(_path);
        }
        
        private readonly string _path;

        /// <summary>
        /// Выводит в консоль сообщение
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="message"></param>
        public void Log(string tag, string message)
        {
            Send($"[INFO] [{tag}] {message}", ConsoleColor.Blue);
        }
        
        /// <summary>
        /// Выводит в консоль сообщение
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="message"></param>
        public void Error(string tag, string message)
        {
            Send($"[ERROR] [{tag}] {message}", ConsoleColor.Red);
        }
        
        /// <summary>
        /// Выводит в консоль сообщение
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="message"></param>
        public void Warn(string tag, string message)
        {
            Send($"[WARN] [{tag}] {message}", ConsoleColor.Yellow);
        }

        /// <summary>
        /// Добавляет к сообщению время, выводит в консоль и записывает в файл 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="consoleColor"></param>
        private void Send(string message, ConsoleColor consoleColor)
        {
            var stream = File.AppendText(_path);
            stream.Write(message);
            
            if (!message.EndsWith(Environment.NewLine))
            {
                stream.WriteLine();
            }
            
            stream.Flush();
            Console.ForegroundColor = consoleColor;
            Console.WriteLine($"[{DateTime.Now}] {message}");
        }
    }
}
