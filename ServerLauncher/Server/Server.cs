using System.Diagnostics;
using System.Reflection;
using ServerLauncher.Exceptions;
using ServerLauncher.Extensions;
using ServerLauncher.Interfaces;
using ServerLauncher.Logger;
using ServerLauncher.Server.Enums;
using ServerLauncher.Server.Features;
using ServerLauncher.Server.Features.Attributes;
using ServerLauncher.Server.Handlers;

namespace ServerLauncher.Server;

public class Server
{
    public const int RxBufferSize = 25000;
    public const int TxBufferSize = 200000;

    private static readonly Dictionary<string, ICommand> _commands = new();

    private readonly uint? port;

    private readonly List<ServerFeature> _features = new();
    private DateTime _initRestartTimeoutTime;

    private DateTime _initStopTimeoutTime;

    private ServerStatusType _serverStatus = ServerStatusType.NotStarted;
    private string _startDateTime;

    public Server(string id = null, uint? port = null, string configLocation = null, string[] args = null)
    {
        Id = id;
        ServerDir = string.IsNullOrEmpty(Id)
            ? null
            : Utilities.GetFullPathSafe(Path.Combine(Program.GlobalConfig.ServersFolder, Id));

        ConfigLocation = Utilities.GetFullPathSafe(configLocation) ??
                         Utilities.GetFullPathSafe(Program.GlobalConfig.ConfigLocation) ??
                         Utilities.GetFullPathSafe(ServerDir);

        this.port = port;

        Arguments = args;

        Config = new Config.Config(Path.Combine(ConfigLocation, "config.yml"));
        Config = Config.Load();

        LogDirectory = Utilities.GetFullPathSafe(Path.Combine(string.IsNullOrEmpty(ServerDir) ? "" : ServerDir,
            Config.LogLocation));

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var features = assembly.GetTypes().Where(type =>
                type.GetCustomAttribute(typeof(ServerFeatureAttribute), true) is not null);

            foreach (var feature in features)
                try
                {
                    var instance = Activator.CreateInstance(feature, this);

                    if (instance is not ServerFeature serverFeature) continue;

                    RegisterFeature(serverFeature);
                }
                catch (Exception exception)
                {
                    SendError(exception.Message);
                }
        }
    }

    /// <summary>
    ///     Айди
    /// </summary>
    public string Id { get; }

    /// <summary>
    ///     Сокет
    /// </summary>
    public ServerSocket Socket { get; private set; }

    /// <summary>
    ///     Процесс игры
    /// </summary>
    public Process GameProcess { get; private set; }

    /// <summary>
    ///     Конфиг
    /// </summary>
    public Config.Config Config { get; }

    /// <summary>
    ///     Фичи
    /// </summary>
    public IEnumerable<ServerFeature> Features => _features;

    /// <summary>
    ///     Команды
    /// </summary>
    public Dictionary<string, ICommand> Commands => _commands;

    /// <summary>
    ///     Запущен ли процесс игры
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
    ///     Папка сервера
    /// </summary>
    public string ServerDir { get; }

    /// <summary>
    ///     Путь к логам
    /// </summary>
    public string LogDirectory { get; private set; }

    /// <summary>
    ///     Локация конфига
    /// </summary>
    public string ConfigLocation { get; }

    /// <summary>
    ///     Порт
    /// </summary>
    public uint Port => port ?? Config.Port;

    /// <summary>
    ///     Аргументы
    /// </summary>
    public string[] Arguments { get; }

    /// <summary>
    ///     Поддерживаемые фичи
    /// </summary>
    public ModFeatures SupportModFeatures { get; set; }

    /// <summary>
    ///     Статус сервера
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
    ///     Статус сервера
    /// </summary>
    public ServerStatusType LastStatus { get; private set; }

    /// <summary>
    ///     Выключен ли
    /// </summary>
    public bool IsStopped => Status is ServerStatusType.NotStarted || Status is ServerStatusType.Stopped ||
                             Status is ServerStatusType.StoppedUnexpectedly;

    /// <summary>
    ///     Запущен ли
    /// </summary>
    public bool IsRunning => !IsStopped;

    /// <summary>
    ///     Включен ли
    /// </summary>
    public bool IsStarted => !IsStopped && !IsStarting;

    /// <summary>
    ///     Включается ли
    /// </summary>
    public bool IsStarting => Status is ServerStatusType.Starting;

    /// <summary>
    ///     Выключается ли
    /// </summary>
    public bool IsStopping => Status is ServerStatusType.Stopping || Status is ServerStatusType.ForceStopping ||
                              Status is ServerStatusType.Restarting;

    /// <summary>
    ///     Загружается ли
    /// </summary>
    public bool IsLoading { get; set; }

    /// <summary>
    ///     Время запуска
    /// </summary>
    public DateTime StartTime { get; private set; }

    /// <summary>
    ///     Путь к файлу логов игры
    /// </summary>
    public string GameLogDirectoryFile { get; private set; }

    public bool CheckStopTimeout =>
        (DateTime.Now - _initStopTimeoutTime).Seconds > Config.ServerStopTimeout;

    public bool CheckRestartTimeout =>
        (DateTime.Now - _initRestartTimeoutTime).Seconds > Config.ServerRestartTimeout;

    public void Start(bool restartOnCrash = true)
    {
        if (!IsStopped) throw new ServerAlreadyRunningException();

        var shouldRestart = false;

        do
        {
            StartTime = DateTime.Now;
            Status = ServerStatusType.Starting;

            try
            {
                Program.Logger.InitializeServerLogger(Id, LogDirectory);

                Log.Info($"{Id} is executing...", Id);

                var consolePort = (int)Port + 100;
                var socket = new ServerSocket(consolePort);
                socket.Connect();

                Socket = socket;

                var arguments = GetArguments(consolePort);

                var exe = Utilities.GetExecutablePath();

                var startInfo = new ProcessStartInfo(exe, arguments.JoinArguments())
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                Log.Info($"Starting server with the following parameters:\n{exe} {startInfo.Arguments}", Id);

                SupportModFeatures = ModFeatures.None;

                ServerEvents.OnStarting();

                var inputHandlerCancellation = new CancellationTokenSource();
                Task inputHandler = null;

                if (!Program.Headless)
                    inputHandler = Task.Run(() => InputHandler.Write(this, inputHandlerCancellation.Token),
                        inputHandlerCancellation.Token);

                var outputHandler = new OutputHandler(this);

                socket.OnReceiveMessage += outputHandler.HandleMessage;
                socket.OnReceiveAction += outputHandler.HandleAction;

                GameProcess = new Process
                {
                    StartInfo = startInfo,
                    EnableRaisingEvents = true
                };

                GameProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Log.Info($"[stdout]: {e.Data}", Id);
                    }
                };

                GameProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Log.Error($"[stderr]: {e.Data}", Id);
                    }
                };

                GameProcess.Start();

                GameProcess.BeginOutputReadLine();
                GameProcess.BeginErrorReadLine();

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

                            ServerEvents.OnCrashed();

                            SendError("Game engine exited unexpectedly");

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
                }
                catch (Exception exception)
                {
                    SendError("Shutdown failed...");
                    Log.Error(exception.Message, Id);
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message, Id);

                // If the server should try to start up again
                if (Config.ServerStartRetry)
                {
                    shouldRestart = true;

                    var waitDelayMs = Config.ServerStartRetryDelay;

                    if (waitDelayMs > 0)
                    {
                        SendError($"Startup failed! Waiting for {waitDelayMs} ms before retrying...");
                        Thread.Sleep(waitDelayMs);
                    }
                    else
                    {
                        SendError("Startup failed! Retrying...");
                    }
                }
                else
                {
                    SendError("Startup failed! Exiting...");
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
        if (!IsRunning) throw new ServerNotRunningException();

        SetRestartStatus();

        if ((killGame || !SendSocketMessage("SOFTRESTART")) && IsGameProcessRunning)
            GameProcess.Kill();
    }

    public void SetStopStatus(bool killGame = false)
    {
        _initStopTimeoutTime = DateTime.Now;
        Status = killGame ? ServerStatusType.ForceStopping : ServerStatusType.Stopping;

        ServerEvents.OnStopped();
    }

    public void Stop(bool killGame = false)
    {
        if (!IsRunning) throw new ServerNotRunningException();

        SetStopStatus(killGame);

        if ((killGame || !SendSocketMessage("QUIT")) && IsGameProcessRunning) GameProcess.Kill();
    }

    public bool SetServerRequestedStatus(ServerStatusType status)
    {
        // Don't override the console's own requests
        if (IsStopping) return false;

        Status = status;

        return true;
    }

    public void SendError(string message)
    {
        Log.Error(message, Id);
    }

    public void SendWarn(string message)
    {
        Log.Warning(message, Id);
    }

    public bool SendSocketMessage(string message)
    {
        if (Socket is null || !Socket.IsConnected)
        {
            Log.Error("Unable to send command to server, the console is disconnected", Id);
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
        if (serverFeature is ICommand command)
        {
            var commandKey = command.Command.ToLower().Trim();

            // If the command was already registered
            if (_commands.ContainsKey(commandKey))
            {
                var message =
                    $"Warning, ServerLauncher tried to register duplicate command \"{commandKey}\"";

                Log.Debug(message, Id);
            }
            else
            {
                _commands.Add(commandKey, command);
            }
        }

        _features.Add(serverFeature);
    }

    private void MainLoop()
    {
        // Creates and starts a timer
        var timer = new Stopwatch();
        timer.Restart();

        while (IsGameProcessRunning)
        {
            ServerEvents.OnTick();

            timer.Stop();

            Thread.Sleep(Math.Max(Config.TickDelay - timer.Elapsed.Milliseconds, 0));

            timer.Restart();

            if (Status is ServerStatusType.Restarting && CheckRestartTimeout)
            {
                SendError("Server restart timed out, killing the server process...");
                Restart(true);
            }

            if (Status is ServerStatusType.Stopping && CheckStopTimeout)
            {
                SendError("Server exit timed out, killing the server process...");
                Stop(true);
            }

            if (Status is not ServerStatusType.ForceStopping) continue;

            SendError("Force stopping the server process...");
            Stop(true);
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
            $"-port{Port}",
            $"-rxbuffer {RxBufferSize}",
            $"-txbuffer {TxBufferSize}"
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

        arguments.AddRange(Arguments.Where(arg => arg != null));

        return arguments;
    }
}