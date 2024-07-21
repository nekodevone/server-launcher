using ServerLauncher.Exceptions;
using ServerLauncher.Extensions;
using ServerLauncher.Interfaces;
using ServerLauncher.Interfaces.Events;
using ServerLauncher.Server.Enums;
using ServerLauncher.Server.Features;
using ServerLauncher.Server.Features.Attributes;
using ServerLauncher.Server.Handlers;
using System.Diagnostics;
using System.Reflection;

namespace ServerLauncher.Server;

public class Server
{
    private readonly uint? port;

    public Server(string id = null, uint? port = null, string configLocation = null, string[] args = null)
    {
        Id = id;
        ServerDir = string.IsNullOrEmpty(Id)
            ? null
            : Utilities.GetFullPathSafe(Path.Combine(Program.GlobalConfig.ConfigLocation, Id));

        ConfigLocation = Utilities.GetFullPathSafe(configLocation) ??
                         Utilities.GetFullPathSafe(Program.GlobalConfig.ConfigLocation) ??
                         Utilities.GetFullPathSafe(ServerDir);

        this.port = port;

        Arguments = args;

        Config = new Config.Config(Path.Combine(this.ConfigLocation, "config.yml"));
        Config = Config.Load();

        LogDirectory = Utilities.GetFullPathSafe(Path.Combine(string.IsNullOrEmpty(ServerDir) ? "" : ServerDir,
            Config.LogLocation));

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var features = assembly.GetTypes().Where(type => type.GetCustomAttribute(typeof(ServerFeatureAttribute), true) is not null);

            foreach (var feature in features)
            {
                try
                {
                    var instance = Activator.CreateInstance(feature, this);

                    if (instance is not ServerFeature serverFeature)
                    {
                        continue;
                    }

                    RegisterFeature(serverFeature);
                }
                catch (Exception exception)
                {
                    Error(exception.Message);
                }
            }
        }
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
    public Config.Config Config { get; private set; }

    /// <summary>
    /// Фичи
    /// </summary>
    public IEnumerable<ServerFeature> Features => _features;

    /// <summary>
    /// Команды
    /// </summary>
    public Dictionary<string, ICommand> Commands => _commands;

    /// <summary>
    /// Лист методов фич
    /// </summary>
    public List<IEventServerTick> Ticks => _ticks;

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
    /// Папка сервера
    /// </summary>
    public string ServerDir { get; private set; }

    /// <summary>
    /// Путь к логам
    /// </summary>
    public string LogDirectory { get; private set; }

    /// <summary>
    /// Локация конфига
    /// </summary>
    public string ConfigLocation { get; }

    /// <summary>
    /// Порт
    /// </summary>
    public uint Port => port ?? Config.Port;

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
                GameLogDirectoryFile = string.IsNullOrEmpty(LogDirectoryFile)
                    ? null
                    : string.Format(LogDirectoryFile, "SCP");
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
        (DateTime.Now - _initStopTimeoutTime).Seconds > Config.ServerStopTimeout;

    public bool CheckRestartTimeout =>
        (DateTime.Now - _initRestartTimeoutTime).Seconds > Config.ServerRestartTimeout;

    private List<ServerFeature> _features = new();

    private static Dictionary<string, ICommand> _commands = new();

    private ServerStatusType _serverStatus = ServerStatusType.NotStarted;

    private readonly List<IEventServerTick> _ticks = new();

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
                Log($"{Id} is executing...");

                var socket = new ServerSocket((int)Port);
                socket.Connect();

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
                
                var inputHandlerCancellation = new CancellationTokenSource();
                Task inputHandler = null;

                if (!Program.Headless)
                {
                    inputHandler = Task.Run(() => InputHandler.Write(this, inputHandlerCancellation.Token),
                        inputHandlerCancellation.Token);
                }

                var outputHandler = new OutputHandler(this);

                socket.OnReceiveMessage += outputHandler.HandleMessage;
                socket.OnReceiveAction += outputHandler.HandleAction;

                GameProcess = Process.Start(startInfo);

                Status = ServerStatusType.Running;
                
                EnableFeatures();

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

                    if (inputHandler is not null)
                    {
                        inputHandlerCancellation.Cancel();
                        try
                        {
                            inputHandler.Wait();
                        }
                        catch (Exception)
                        {
                            //Задача была отменена или удалена. Это нормально, мы этого ждем.
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
                if (Config.ServerStartRetry)
                {
                    shouldRestart = true;

                    var waitDelayMs = Config.ServerStartRetryDelay;

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
        } while (shouldRestart);
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
            throw new ServerNotRunningException();
        }

        SetRestartStatus();

        if ((killGame || !SendSocketMessage("SOFTRESTART")) && IsGameProcessRunning)
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
            throw new ServerNotRunningException();
        }

        SetStopStatus(killGame);

        if ((killGame || !SendSocketMessage("QUIT")) && IsGameProcessRunning)
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

    public void Log(object message)
    {
        Program.Logger.Log("SERVER", message);
    }
    
    public void Error(object message)
    {
        Program.Logger.Error("SERVER", message);
    }

    public void Warn(object message)
    {
        Program.Logger.Warn("SERVER", message);
    }

    public void Debug(object message)
    {
        Program.Logger.Debug("SERVER", message);
    }

    public void Message(object message, ConsoleColor consoleColor)
    {
        Program.Logger.Message("SERVER", message, consoleColor);
    }
    
    public bool SendSocketMessage(string message)
    {
        if (Socket is null || !Socket.IsConnected)
        {
            Program.Logger.Error("SERVER", "Unable to send command to server, the console is disconnected");
            return false;
        }

        Socket.SendMessage(message);
        return true;
    }

    public void EnableFeatures()
    {
        foreach (var feature in _features)
        {
            feature.Enabled();
            feature.ConfigReloaded();
        }
    }

    public void RegisterFeature(ServerFeature serverFeature)
    {
        switch (serverFeature)
        {
            case ICommand command:
            {
                var commandKey = command.Command.ToLower().Trim();

                // If the command was already registered
                if (_commands.ContainsKey(commandKey))
                {
                    var message =
                        $"Warning, ServerLauncher tried to register duplicate command \"{commandKey}\"";

                    Program.Logger.Debug(nameof(Server), message);
                    Log(message);
                }
                else
                {
                    _commands.Add(commandKey, command);
                }

                break;
            }
            case IEventServerTick serverTick:
                _ticks.Add(serverTick);
                break;
        }
                    
        _features.Add(serverFeature);
    }

    public void ForEachHandler<T>(Action<T> action) where T : IEvent
    {
        foreach (var feature in _features)
        {
            if (!feature.IsEnabled)
            {
                continue;
            }
            
            if (feature is not T eventHandler)
            {
                continue;
            }

            action.Invoke(eventHandler);
        }
    }
    
    private void MainLoop()
    {
        // Creates and starts a timer
        var timer = new Stopwatch();
        timer.Restart();

        while (IsGameProcessRunning)
        {
            foreach (var tickEvent in _ticks)
            {
                try
                {
                    tickEvent.OnServerTick();
                }
                catch (Exception exception)
                {
                    Error(exception.ToString());
                    Error("Tick event removed for this feature.");

                    _ticks.Remove(tickEvent);
                }
            }

            timer.Stop();
            
            Thread.Sleep(Math.Max(Config.MultiAdminTickDelay - timer.Elapsed.Milliseconds, 0));

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

    private IEnumerable<string> GetArguments(int port)
    {
        var arguments = new List<string>
        {
            $"-multiadmin:{Program.Version}:{(int)ModFeatures.All}",
            "-batchmode",
            "-nographics",
            "-silent-crashes",
            "-nodedicateddelete",
            $"-id{Environment.ProcessId}",
            $"-console{port}",
            $"-port{Port}"
        };

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

        if (!string.IsNullOrEmpty(ConfigLocation))
        {
            arguments.Add("-configpath");
            arguments.Add(ConfigLocation);
        }

        // Add custom arguments
        arguments.AddRange(Arguments);

        return arguments;
    }
}