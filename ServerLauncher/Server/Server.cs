using System.Diagnostics;
using System.Reflection;
using ServerLauncher.Config;
using ServerLauncher.Exceptions;
using ServerLauncher.Extensions;
using ServerLauncher.Logger;
using ServerLauncher.Server.Enums;
using ServerLauncher.Server.Handlers;

namespace ServerLauncher.Server
{
    public class Server
    {
        public const int RxBufferSize = 25000;
        public const int TxBufferSize = 200000;

        /// <summary>
        /// ID
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Port
        /// </summary>
        public uint Port => Config.Port;

        /// <summary>
        /// Console socket
        /// </summary>
        private ServerSocket Socket { get; set; }

        /// <summary>
        /// Process
        /// </summary>
        private Process ServerProcess { get; set; }

        /// <summary>
        /// Configuration
        /// </summary>
        private ServerConfig Config { get; }

        /// <summary>
        /// Is server process running
        /// </summary>
        public bool IsServerProcessRunning
        {
            get
            {
                if (ServerProcess is null)
                {
                    return false;
                }

                ServerProcess.Refresh();
                return !ServerProcess.HasExited;
            }
        }

        /// <summary>
        /// Configuration directory path
        /// </summary>
        public string ConfigDir { get; }

        /// <summary>
        /// Logs directory path
        /// </summary>
        public string LogsDir { get; }

        /// <summary>
        /// Additional arguments
        /// </summary>
        public string[] Arguments { get; }

        /// <summary>
        /// Supported mod features
        /// </summary>
        public ModFeatures SupportModFeatures { get; set; }

        /// <summary>
        /// Current server status
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
        /// Last known server status
        /// </summary>
        public ServerStatusType LastStatus { get; private set; }

        /// <summary>
        /// Is server running
        /// </summary>
        public bool IsRunning => !IsStopped;

        /// <summary>
        /// Is server stopped
        /// </summary>
        public bool IsStopped => Status is ServerStatusType.NotStarted or ServerStatusType.Stopped
            or ServerStatusType.StoppedUnexpectedly;

        /// <summary>
        /// Is server started
        /// </summary>
        public bool IsStarted => !IsStopped && !IsStarting;

        /// <summary>
        /// Is server starting
        /// </summary>
        public bool IsStarting => Status is ServerStatusType.Starting;

        /// <summary>
        /// Is server stopping
        /// </summary>
        public bool IsStopping =>
            Status is ServerStatusType.Stopping or ServerStatusType.ForceStopping or ServerStatusType.Restarting;

        /// <summary>
        /// Is server loading
        /// </summary>
        public bool IsLoading { get; set; }

        /// <summary>
        /// Start time
        /// </summary>
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// Game logs directory path
        /// </summary>
        public string GameLogDirectoryFile { get; private set; }

        public bool CheckStopTimeout =>
            (DateTime.Now - _initStopTimeoutTime).Seconds > Program.LauncherConfig.ServerStopTimeout;

        public bool CheckRestartTimeout =>
            (DateTime.Now - _initRestartTimeoutTime).Seconds > Program.LauncherConfig.ServerRestartTimeout;

        private DateTime _initRestartTimeoutTime;

        private DateTime _initStopTimeoutTime;

        private ServerStatusType _serverStatus = ServerStatusType.NotStarted;

        private string _startDateTime;

        public Server(string id = null, string configLocation = null, string[] args = null)
        {
            Id = id;

            ConfigDir = string.IsNullOrEmpty(Id)
                ? null
                : Utilities.GetFullPathSafe(Path.Combine(Program.LauncherConfig.ConfigDir, Id));

            ConfigDir = Utilities.GetFullPathSafe(configLocation) ??
                        Utilities.GetFullPathSafe(ConfigDir);

            Config = Program.ConfigLoader.Load<ServerConfig>(Path.Combine(ConfigDir, "launcher.yml"));

            Arguments = args;

            LogsDir = Utilities.GetFullPathSafe(Program.LauncherConfig.LogsDir);
        }

        public void Start(bool restartOnCrash = true)
        {
            if (!IsStopped)
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
                    Program.Logger.InitializeServerLogger(Id, LogsDir);

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

                    ServerProcess = new Process
                    {
                        StartInfo = startInfo,
                        EnableRaisingEvents = true
                    };

                    ServerProcess.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Log.Stdout(Id, e.Data);
                        }
                    };

                    ServerProcess.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Log.Stdout(Id, e.Data, true);
                        }
                    };

                    ServerProcess.Start();

                    ServerProcess.BeginOutputReadLine();
                    ServerProcess.BeginErrorReadLine();

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

                                ServerEvents.OnCrashed();

                                SendError("Game engine exited unexpectedly");

                                shouldRestart = restartOnCrash;
                                break;
                        }

                        ServerProcess?.Dispose();
                        ServerProcess = null;

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
                    if (Program.LauncherConfig.ServerStartRetry)
                    {
                        shouldRestart = true;

                        var waitDelayMs = Program.LauncherConfig.ServerStartRetryDelay;

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

            if ((killGame || !SendSocketMessage("SOFTRESTART")) && IsServerProcessRunning)
                ServerProcess.Kill();
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

            if ((killGame || !SendSocketMessage("QUIT")) && IsServerProcessRunning) ServerProcess.Kill();
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
            Log.Warn(message, Id);
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

        private void MainLoop()
        {
            // Creates and starts a timer
            var timer = new Stopwatch();
            timer.Restart();

            while (IsServerProcessRunning)
            {
                ServerEvents.OnTick();

                timer.Stop();

                Thread.Sleep(Math.Max(Program.LauncherConfig.TickDelay - timer.Elapsed.Milliseconds, 0));

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

            if (!string.IsNullOrEmpty(ConfigDir))
            {
                arguments.Add("-configpath");
                arguments.Add(ConfigDir);
            }

            arguments.AddRange(Arguments.Where(arg => arg != null));

            return arguments;
        }
    }
}