using ServerLauncher.Logger;

namespace ServerLauncher.Server.Handlers;

public static class InputHandler
{
    private static readonly char[] Separator = {' '};
    
    private static readonly List<string> Messages = new();

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

                Messages.Add(message);

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
                    Log.Error($"Command name is null in {message}");

                    continue;
                }
                
                Log.Info($">>> {message}", color: ConsoleColor.DarkMagenta);

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
            Log.Info("Reading commands was stopped");
        }
        catch (Exception exception)
        {
            Log.Error($"Error reading command: {exception.Message}");
        }
    }
    
    private static async Task<string> ReadMessage(CancellationToken cancellationToken)
    {
        var isReading = true;
        
        var message = string.Empty;

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

                    if (_selectedMessage > Messages.Count - 1)
                    {
                        _selectedMessage = Messages.Count - 1;
                    }

                    message = Messages[_selectedMessage];
                    
                    //\r знак нужен, чтобы очистить строку и заменить её этой (очищаем строку с командой которую мы ввели)
                    Console.Write($"\r{message}");

                    break;
                case ConsoleKey.DownArrow:
                    _selectedMessage--;

                    if (_selectedMessage < 0)
                    {
                        _selectedMessage = 0;
                    }
                    
                    message = Messages[_selectedMessage];
                    
                    //\r знак нужен, чтобы очистить строку и заменить её этой (очищаем строку с командой которую мы ввели)
                    Console.Write($"\r{message}");
                    
                    break;
                case ConsoleKey.Backspace or ConsoleKey.Delete:
                    if (message.Length <= 0 || Console.CursorLeft <= 0)
                    {
                        break;
                    }

                    message = message[..^1];
                    
                    Console.CursorLeft -= 1;
                    Console.Write(' ');
                    Console.CursorLeft -= 1;
                    
                    break;
                default:
                    message += key.KeyChar;
                    Console.Write(key.KeyChar);
                    break;
            }
        }

        return message;
    }
    
    private static async Task WaitForKey(CancellationToken cancellationToken)
    {
        while (!Console.KeyAvailable)
        {
            await Task.Delay(10, cancellationToken);
        }
    }
}

