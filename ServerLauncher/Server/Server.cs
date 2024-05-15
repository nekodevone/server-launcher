using System.Diagnostics;
using ServerLauncher.Config;
using ServerLauncher.Exceptions;
using ServerLauncher.Extensions;
using ServerLauncher.Interfaces;
using ServerLauncher.Interfaces.Events;
using ServerLauncher.Server.Enums;
using ServerLauncher.Server.Handlers;

namespace ServerLauncher.Server;

public class Server
{
    public static List<Feature> Features { get; } = new();
    
    public Server(string id, uint port, string logDirectory, string[] arguments)
    {
        Id = id;
        Port = port;
        LogDirectory = logDirectory;
        Arguments = arguments;
    }
    
    /// <summary>
    /// Айди
    /// </summary>
    public string Id { get; }
    
    /// <summary>
    /// Сокет
    /// </summary>
    public ServerSocket Socket { get; private set; }
    
    /// <summary>
    /// Процесс игры
    /// </summary>
    public Process GameProcess { get; private set; }
    
    /// <summary>
    /// Конфиг
    /// </summary>
    public LaunchConfig Config { get; private set; }
    
    /// <summary>
    /// Команды
    /// </summary>
    public Dictionary<string, ICommand> Commands { get; } = new Dictionary<string, ICommand>();

    /// <summary>
    /// Запущен ли процесс игры
    /// </summary>
    public bool IsGameProcessRunning
    {
        get
        {
            if (GameProcess is null)
                return false;

            GameProcess.Refresh();

            return !GameProcess.HasExited;
        }
    }
    
    /// <summary>
    /// Путь к логам
    /// </summary>
    public string LogDirectory { get; private set; }
    
    /// <summary>
    /// Порт
    /// </summary>
    public uint Port { get; }

    /// <summary>
    /// Аргументы
    /// </summary>
    public string[] Arguments { get; }
    
    /// <summary>
    /// Поддерживаемые фичи
    /// </summary>
    public ModFeatures SupportModFeatures { get; set; }

    /// <summary>
    /// Статус сервера
    /// </summary>
    public ServerStatusType Status
    {
        get => _serverStatus;
        set
        {
            LastStatus = _serverStatus;
            _serverStatus = value;
        }
    }
    
    /// <summary>
    /// Статус сервера
    /// </summary>
    public ServerStatusType LastStatus { get; private set; }

    /// <summary>
    /// Выключен ли
    /// </summary>
    public bool IsStopped => Status is ServerStatusType.NotStarted || Status is ServerStatusType.Stopped ||
                             Status is ServerStatusType.StoppedUnexpectedly;

    /// <summary>
    /// Запущен ли
    /// </summary>
    public bool IsRunning => !IsStopped;
    
    /// <summary>
    /// Включен ли
    /// </summary>
    public bool IsStarted => !IsStopped && !IsStarting;

    /// <summary>
    /// Включается ли
    /// </summary>
    public bool IsStarting => Status is ServerStatusType.Starting;

    /// <summary>
    /// Выключается ли
    /// </summary>
    public bool IsStopping => Status is ServerStatusType.Stopping || Status is ServerStatusType.ForceStopping ||
                              Status is ServerStatusType.Restarting;
    
    /// <summary>
    /// Загружается ли
    /// </summary>
    public bool IsLoading { get; set; }
    
    /// <summary>
    /// Время запуска
    /// </summary>
    public DateTime StartTime { get; private set; }
    
    /// <summary>
    /// Время запуска в виде строки
    /// </summary>
    public string StartDateTime
    {
        get => _startDateTime;

        private set
        {
            _startDateTime = value;

            // Update related variables
            LogDirectoryFile = string.IsNullOrEmpty(value) || string.IsNullOrEmpty(LogDirectory)
                ? null
                : $"{Path.Combine(LogDirectory.EscapeFormat(), value)}_{{0}}_log_{Port}.txt";

            lock (this)
            {
                LogDirectory = string.IsNullOrEmpty(LogDirectoryFile) ? null : string.Format(LogDirectoryFile, "MA");
                GameLogDirectoryFile = string.IsNullOrEmpty(LogDirectoryFile) ? null : string.Format(LogDirectoryFile, "SCP");
            }
        }
    }
    
    /// <summary>
    /// Путь к файлу логов
    /// </summary>
    public string LogDirectoryFile { get; private set; }
    
    /// <summary>
    /// Путь к файлу логов игры
    /// </summary>
    public string GameLogDirectoryFile { get; private set; }
    
    public bool CheckStopTimeout =>
        (DateTime.Now - _initStopTimeoutTime).Seconds > Config.Storage.ServerStopTimeout.Value;

    public bool CheckRestartTimeout =>
        (DateTime.Now - _initRestartTimeoutTime).Seconds > Config.Storage.ServerRestartTimeout.Value;

    private ServerStatusType _serverStatus = ServerStatusType.NotStarted;
    
    private readonly List<IEventServerTick> _tick = new();
    
    private DateTime _initStopTimeoutTime;
    private DateTime _initRestartTimeoutTime;
    private string _startDateTime;

    public void Start(bool restartOnCrash = true)
    {
        if (Status is ServerStatusType.Running)
        {
            throw new ServerAlreadyRunningException();
        }

        var shouldRestart = false;

        do
        {
            StartTime = DateTime.Now;
            Status = ServerStatusType.Starting;
            try
            {
//Тут будет вызов в конфиге чё нибудь

                Log($"{Id} is executing...");

                var socket = new ServerSocket();

                Socket = socket;

                SetLogsDirectories();

                //Аргуменыт доделать надо, конфиг нужен а его нет
                var arguments = GetArguments(socket.Port);

                var exe = Utilities.GetExecutablePath();

                var startInfo = new ProcessStartInfo(exe, arguments.JoinArguments())
                {
                    CreateNoWindow = true, UseShellExecute = false
                };

                Log($"Starting server with the following parameters:\n{exe} {startInfo.Arguments}");

                SupportModFeatures = ModFeatures.None;

                ForEachHandler<IEventServerStarting>(eventPreStart => eventPreStart.OnServerStarting());

                // Start the input reader
                var inputHandlerCancellation = new CancellationTokenSource();
                Task inputHandler = null;

                if (!Program.Headless)
                {
                    inputHandler = Task.Run(() => InputHandler.Write(this, inputHandlerCancellation.Token),
                        inputHandlerCancellation.Token);
                }

                var outputHandler = new OutputHandler(this);
                // Assign the socket events to the OutputHandler
                socket.OnReceiveMessage += outputHandler.HandleMessage;
                socket.OnReceiveAction += outputHandler.HandleAction;

                GameProcess = Process.Start(startInfo);

                Status = ServerStatusType.Running;

                MainLoop();

                try
                {
                    switch (Status)
                    {
                        case ServerStatusType.Stopping:
                        case ServerStatusType.ForceStopping:
                        case ServerStatusType.ExitActionStop:
                            Status = ServerStatusType.Stopped;

                            shouldRestart = false;
                            break;

                        case ServerStatusType.Restarting:
                        case ServerStatusType.ExitActionRestart:
                            shouldRestart = true;
                            break;

                        default:
                            Status = ServerStatusType.StoppedUnexpectedly;

                            ForEachHandler<IEventServerCrashed>(eventCrash => eventCrash.OnServerCrashed());

                            Error("Game engine exited unexpectedly");

                            shouldRestart = restartOnCrash;
                            break;
                    }

                    GameProcess?.Dispose();
                    GameProcess = null;

                    if (inputHandler != null)
                    {
                        inputHandlerCancellation.Cancel();
                        try
                        {
                            inputHandler.Wait();
                        }
                        catch (Exception)
                        {
                            // Task was cancelled or disposed, this is fine since we're waiting for that
                        }

                        inputHandler.Dispose();
                        inputHandlerCancellation.Dispose();
                    }

                    socket.Disconnect();

                    // Remove the socket events for OutputHandler
                    socket.OnReceiveMessage -= outputHandler.HandleMessage;
                    socket.OnReceiveAction -= outputHandler.HandleAction;

                    Socket = null;
                    StartDateTime = null;
                }
                catch (Exception exception)
                {
                    Error(exception.Message);
                    Program.Logger.Error(nameof(Start), exception.Message);
                    Error("Shutdown failed...");
                }
            }
            catch (Exception exception)
            {
                Error(exception.Message);
                Program.Logger.Error(nameof(Start), exception.Message);

                // If the server should try to start up again
                if (Config.Storage.ServerStartRetry.Value)
                {
                    shouldRestart = true;

                    var waitDelayMs = Config.Storage.ServerStartRetryDelay.Value;

                    if (waitDelayMs > 0)
                    {
                        Error($"Startup failed! Waiting for {waitDelayMs} ms before retrying...");
                        Thread.Sleep(waitDelayMs);
                    }
                    else
                    {
                        Error("Startup failed! Retrying...");
                    }
                }
                else
                {
                    Error("Startup failed! Exiting...");
                }
            }
        } while (shouldRestart) ;
    }

    public void SetRestartStatus()
    {
        _initRestartTimeoutTime = DateTime.Now;
        Status = ServerStatusType.Restarting;
    }

    public void Restart(bool killGame = false)
    {
        if (!IsRunning)
        {
            //throw new Exceptions.ServerNotRunningException();
        }

        SetRestartStatus();

        if ((killGame || !SendMessage("SOFTRESTART")) && IsGameProcessRunning)
            GameProcess.Kill();
    }
    
    public void SetStopStatus(bool killGame = false)
    {
        _initStopTimeoutTime = DateTime.Now;
        Status = killGame ? ServerStatusType.ForceStopping : ServerStatusType.Stopping;

        ForEachHandler<IEventServerStopped>(stopEvent => stopEvent.OnServerStopped());
    }

    public void Stop(bool killGame = false)
    {
        if (!IsRunning)
        {
            //throw new Exceptions.ServerNotRunningException();
        }

        SetStopStatus(killGame);

        if ((killGame || !SendMessage("QUIT")) && IsGameProcessRunning)
        {
            GameProcess.Kill();
        }
    }

    public bool SetServerRequestedStatus(ServerStatusType status)
    {
        // Don't override the console's own requests
        if (IsStopping)
        {
            return false;
        }

        Status = status;

        return true;
    }

    public void Log(string message, ConsoleColor consoleColor = ConsoleColor.Blue)
    {
        Program.Logger.Log("SERVER", message, consoleColor);
    }
    
    public void Error(string message)
    {
        Program.Logger.Error("SERVER", message);
    }
    
    /// <summary>
    /// Sends the string <paramref name="message" /> to the SCP: SL server process.
    /// </summary>
    /// <param name="message"></param>
    public bool SendMessage(string message)
    {
        if (Socket is null || !Socket.IsConnected)
        {
            Program.Logger.Error("SERVER", "Unable to send command to server, the console is disconnected");
            return false;
        }

        Socket.SendMessage(message);
        return true;
    }

    public void ForEachHandler<T>(Action<T> action) where T : IEvent
    {
        foreach (var feature in Features)
            if (feature is T eventHandler)
                action.Invoke(eventHandler);
    }
    
    private void MainLoop()
    {
        // Creates and starts a timer
        var timer = new Stopwatch();
        timer.Restart();

        while (IsGameProcessRunning)
        {
            foreach (var tickEvent in _tick)
            {
                tickEvent.OnServerTick();
            }

            timer.Stop();

            // Wait the delay per tick (calculating how long the tick took and compensating)
            //Thread.Sleep(Math.Max(ServerConfig.MultiAdminTickDelay.Value - timer.Elapsed.Milliseconds, 0));

            timer.Restart();

            if (Status is ServerStatusType.Restarting && CheckRestartTimeout)
            {
                Error("Server restart timed out, killing the server process...");
                Restart(true);
            }

            if (Status is ServerStatusType.Stopping && CheckStopTimeout)
            {
                Error("Server exit timed out, killing the server process...");
                Stop(true);
            }

            if (Status is not ServerStatusType.ForceStopping)
            {
                continue;
            }
            
            Error("Force stopping the server process...");
            Stop(true);
        }
    }
    
    /// <summary>
    /// Устанавливает пути к файлам логов
    /// </summary>
    private void SetLogsDirectories()
    {
        var time = StartTime.ToString();
            
        var directory = string.IsNullOrEmpty(time) || string.IsNullOrEmpty(LogDirectory)
            ? null
            : $"{Path.Combine(LogDirectory.EscapeFormat(), time)}_{{0}}_log_{Port}.txt";

        lock (this)
        {
            LogDirectoryFile = string.IsNullOrEmpty(directory) ? null : string.Format(directory, "MA");
            GameLogDirectoryFile = string.IsNullOrEmpty(directory) ? null : string.Format(directory, "SCP");
        }
    }
    
    private List<string> GetArguments(int port)
    {
        var arguments = new List<string>
        {
            $"-multiadmin:{Program.Version}:{(int)ModFeatures.All}",
            "-batchmode",
            "-nographics",
            "-silent-crashes",
            "-nodedicateddelete",
            $"-id{Process.GetCurrentProcess().Id}",
            $"-console{port}",
            $"-port{Port}"
        };
        
        //кто это читает, добавь || ServerConfig.NoLog.Value
        if (string.IsNullOrEmpty(GameLogDirectoryFile))
        {
            arguments.Add("-nolog");

            if (OperatingSystem.IsLinux())
            {
                arguments.Add("-logFile");
                arguments.Add("/dev/null");
            }
            else if (OperatingSystem.IsWindows())
            {
                arguments.Add("-logFile");
                arguments.Add("NUL");
            }
        }
        else
        {
            arguments.Add("-logFile");
            arguments.Add(GameLogDirectoryFile);
        }

        return arguments;
    }
}