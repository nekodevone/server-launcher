namespace ServerLauncher.Server.Handlers;

public static class InputHandler
{
    private static readonly char[] Separator = {' '};
    
    private static readonly List<string> _messages = new();

    private static int _selectedMessage;

    public static async void Write(Server server, CancellationToken cancellationToken)
    {
        try
        {
            while (server.IsRunning)
            {
                if (Program.Headless)
                {
                    break;
                }

                //Получаем то что ввёл пользователь
                var message = await ReadMessage(cancellationToken);

                Console.WriteLine();

                _messages.Add(message);

                //Скипаем если ввёл ничего
                if (string.IsNullOrEmpty(message))
                {
                    continue;
                }

                var separatorIndex = message.IndexOfAny(Separator);

                //Если в команде есть аргументы
                //Получаем команду
                var commandName = message.Contains(' ')
                    ? message.Split(' ').FirstOrDefault()
                    :
                    //Если в сообщении нет аргументов, то значит само сообщение команда 
                    message;

                if (commandName is null)
                {
                    server.Error($"Command name is null in {message}");

                    continue;
                }

                //\r знак нужен, чтобы очистить строку и заменить её этой (очищаем строку с командой которую мы ввели)
                server.Message($">>> {message}", ConsoleColor.DarkMagenta);

                //Является ли команда не лаунчера
                var isServerCommand = true;

                if (server.Commands.TryGetValue(commandName, out var command))
                {
                    isServerCommand = command.IsPassToGame;

                    command.Execute(separatorIndex < 0 || separatorIndex + 1 >= message.Length
                        ? Array.Empty<string>()
                        : Utilities.StringToArgs(message, separatorIndex + 1, escapeChar: '\"', quoteChar: '\"'));
                }

                if (isServerCommand)
                {
                    //Команды нету словаре, значит не наша.
                    server.SendSocketMessage(message);
                }
            }
        }
        catch (TaskCanceledException)
        {
            server.Log("Reading commands was stopped");
        }
        catch (Exception exception)
        {
            server.Error($"Error reading command: {exception.Message}");
        }
    }

    private static async Task<string> ReadMessage(CancellationToken cancellationToken)
    {
        var isReading = true;
        
        var command = string.Empty;

        while (isReading)
        {
            await WaitForKey(cancellationToken);
            
            var key = Console.ReadKey(true);
            
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    isReading = false;
                    break;
                case ConsoleKey.UpArrow:
                    _selectedMessage++;

                    if (_selectedMessage > _messages.Count - 1)
                    {
                        _selectedMessage = _messages.Count - 1;
                    }

                    command = _messages[_selectedMessage];
                    
                    Console.Write($"\r{command}");

                    break;
                case ConsoleKey.DownArrow:
                    _selectedMessage--;

                    if (_selectedMessage < 0)
                    {
                        _selectedMessage = 0;
                    }
                    
                    command = _messages[_selectedMessage];
                    
                    Console.Write($"\r{command}");
                    
                    break;
                default:
                    command += key.KeyChar;
                    Console.Write(key.KeyChar);
                    break;
            }
        }

        return command;
    }
    
    private static async Task WaitForKey(CancellationToken cancellationToken)
    {
        while (!Console.KeyAvailable)
        {
            await Task.Delay(10, cancellationToken);
        }
    }
}

