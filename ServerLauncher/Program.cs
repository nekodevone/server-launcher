using ServerLauncher.NativeExitSignal;
using ServerLauncher.Utility;

namespace ServerLauncher;

public static class Program
{
    public static Logger Logger { get; private set; } = new(Directory.GetCurrentDirectory());

    public static Version Version { get; } = new(1, 0, 0);

    public static Config.Config GlobalConfig = new(Path.Combine(Directory.GetCurrentDirectory(), "config.yml"));

    private static readonly List<Server.Server> InstantiatedServers = [];

    public static bool Headless { get; private set; }

    private static uint? portArg;
    public static readonly string[] Args = Environment.GetCommandLineArgs();

    private static IExitSignal exitSignalListener;

    private static bool exited = false;
    private static readonly object ExitLock = new object();

    public static void Main()
    {
        GlobalConfig = GlobalConfig.Load();

        AppDomain.CurrentDomain.ProcessExit += OnExit;

        if (OperatingSystem.IsLinux())
        {
#if LINUX
					exitSignalListener = new UnixExitSignal();
#endif
        }
        else if (OperatingSystem.IsWindows())
        {
            exitSignalListener = new WinExitSignal();
        }

        if (exitSignalListener != null)
            exitSignalListener.Exit += OnExit;

        // Remove executable path
        if (Args.Length > 0)
            Args[0] = null;
        Headless = GetFlagFromArgs(Args, "headless", "h");

        var serverIdArg = GetParamFromArgs(Args, "server-id", "id");
        var configArg = GetParamFromArgs(Args, "config", "c");
        portArg = uint.TryParse(GetParamFromArgs(Args, "port", "p"), out uint port) ? (uint?)port : null;

        Server.Server server = null;

        if (!string.IsNullOrEmpty(serverIdArg) || !string.IsNullOrEmpty(configArg))
        {
            server = new Server.Server(serverIdArg, portArg, configArg, Args);

            InstantiatedServers.Add(server);
        }

        if (server == null)
        {
            return;
        }

        switch (string.IsNullOrEmpty(server.Id))
        {
            case false when !string.IsNullOrEmpty(server.ConfigLocation):
                Logger.Log("SERVER",
                    $"Starting this instance with Server ID: \"{server.Id}\" and config directory: \"{server.ConfigLocation}\"...");
                break;
            case false:
                Logger.Log("SERVER", $"Starting this instance with Server ID: \"{server.Id}\"...");
                break;
            default:
            {
                Logger.Log("SERVER",
                    !string.IsNullOrEmpty(server.ConfigLocation)
                        ? $"Starting this instance with config directory: \"{server.ConfigLocation}\"..."
                        : "Starting this instance in single server mode...");
                break;
            }
        }

        try
        {
            server.Start();
        }
        catch (Exception exception)
        {
            Logger.Error("SERVER", exception.ToString());
            
            Logger.Dispose();
        }
    }


    private static void OnExit(object sender, EventArgs e)
    {
        lock (ExitLock)
        {
            if (exited)
                return;

            Logger.Message("SERVER", "Stopping servers and exiting Laucnher...", ConsoleColor.DarkMagenta);

            foreach (var server in InstantiatedServers.Where(server => server.IsGameProcessRunning))
            {
                try
                {
                    Logger.Message("SERVER",
                        string.IsNullOrEmpty(server.Id)
                            ? "Stopping the default server..."
                            : $"Stopping server with ID \"{server.Id}\"...", ConsoleColor.DarkMagenta);

                    server.Stop();

                    // Wait for server to exit
                    var timeToWait = Math.Max(server.Config.SafeShutdownCheckDelay, 0);
                    var timeWaited = 0;

                    while (server.IsGameProcessRunning)
                    {
                        Thread.Sleep(timeToWait);
                        timeWaited += timeToWait;

                        if (timeWaited < server.Config.SafeShutdownTimeout)
                        {
                            continue;
                        }

                        Logger.Message("SERVER",
                            string.IsNullOrEmpty(server.Id)
                                ? $"Failed to stop the default server within {timeWaited} ms, giving up..."
                                : $"Failed to stop server with ID \"{server.Id}\" within {timeWaited} ms, giving up...",
                            ConsoleColor.Red);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    // LogDebugException(nameof(OnExit), ex);
                }
            }

            exited = true;
        }
    }

    private static bool GetFlagFromArgs(string[] args, string key = null, string alias = null)
        {
            return GetFlagFromArgs(args, new[] { key }, new[] { alias });
        }

        private static bool GetFlagFromArgs(string[] args, string[] keys = null, string[] aliases = null)
        {
            if (keys.IsNullOrEmpty() && aliases.IsNullOrEmpty()) return false;

            return bool.TryParse(GetParamFromArgs(args, keys, aliases), out var result)
                ? result
                : ArgsContainsParam(args, keys, aliases);
        }

        private static string GetParamFromArgs(string[] args, string key = null, string alias = null)
        {
            return GetParamFromArgs(args, new string[] { key }, new string[] { alias });
        }

        private static string GetParamFromArgs(string[] args, string[] keys = null, string[] aliases = null)
        {
            var hasKeys = !keys.IsNullOrEmpty();
            var hasAliases = !aliases.IsNullOrEmpty();

            if (!hasKeys && !hasAliases) return null;

            for (var i = 0; i < args.Length - 1; i++)
            {
                var lowArg = args[i]?.ToLower();

                if (string.IsNullOrEmpty(lowArg)) continue;

                if (hasKeys)
                {
                    if (keys.Any(key => lowArg == $"--{key?.ToLower()}"))
                    {
                        var value = args[i + 1];

                        args[i] = null;
                        args[i + 1] = null;

                        return value;
                    }
                }

                if (!hasAliases)
                {
                    continue;
                }

                {
                    if (!aliases.Any(alias => lowArg == $"-{alias?.ToLower()}"))
                    {
                        continue;
                    }

                    var value = args[i + 1];

                    args[i] = null;
                    args[i + 1] = null;

                    return value;
                }
            }

            return null;
        }

        private static bool ArgsContainsParam(IList<string> args, string[] keys = null, string[] aliases = null)
        {
            var hasKeys = !keys.IsNullOrEmpty();
            var hasAliases = !aliases.IsNullOrEmpty();

            if (!hasKeys && !hasAliases) return false;

            for (var i = 0; i < args.Count; i++)
            {
                var lowArg = args[i]?.ToLower();

                if (string.IsNullOrEmpty(lowArg)) continue;

                if (hasKeys)
                {
                    if (keys.Any(key => lowArg == $"--{key?.ToLower()}"))
                    {
                        args[i] = null;
                        return true;
                    }
                }

                if (!hasAliases)
                {
                    continue;
                }

                if (!aliases.Any(alias => lowArg == $"-{alias?.ToLower()}"))
                {
                    continue;
                }

                args[i] = null;
                return true;
            }

            return false;
        }
    }