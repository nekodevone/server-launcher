﻿using ServerLauncher.Config;
using ServerLauncher.Extensions;
using ServerLauncher.Logger;
using ServerLauncher.NativeExitSignal;
using ServerLauncher.Utility;

namespace ServerLauncher
{
    public static class Program
    {
        public static Log Logger => Log.Instance;

        public static Version Version { get; } = new(1, 0, 0);

        public static ConfigLoader ConfigLoader { get; set; }

        public static LauncherConfig LauncherConfig { get; set; }

        private static readonly List<Server.Server> InstantiatedServers = [];

        public static bool Headless { get; private set; }

        private static readonly string[] Args = Environment.GetCommandLineArgs();

        private static IExitSignal _exitSignalListener;

        private static bool _exited = false;

        private static readonly object ExitLock = new();

        public static void Main()
        {
            ConfigLoader = new ConfigLoader();
            LauncherConfig =
                ConfigLoader.Load<LauncherConfig>(Path.Combine(Directory.GetCurrentDirectory(), "launcher.yml"));

            Log.Instance = new Log(LauncherConfig.LogsDir);

            AppDomain.CurrentDomain.ProcessExit += OnExit;

            if (OperatingSystem.IsLinux())
            {
#if LINUX_SIGNALS
					_exitSignalListener = new UnixExitSignal();
#endif
            }
            else if (OperatingSystem.IsWindows())
            {
                _exitSignalListener = new WinExitSignal();
            }

            if (_exitSignalListener != null)
            {
                _exitSignalListener.Exit += OnExit;
            }

            // Удаляем путь к исполняемому файлу
            if (Args.Length > 0)
            {
                Args[0] = null;
            }

            Headless = GetFlagFromArgs(Args, "headless", "h");

            var serverIdArg = GetParamFromArgs(Args, "server-id", "id");
            var configArg = GetParamFromArgs(Args, "config", "c");

            Server.Server server = null;

            if (!string.IsNullOrEmpty(serverIdArg) || !string.IsNullOrEmpty(configArg))
            {
                server = new Server.Server(serverIdArg, configArg, Args);

                InstantiatedServers.Add(server);
            }

            if (server == null)
            {
                return;
            }

            switch (string.IsNullOrEmpty(server.Id))
            {
                case false when !string.IsNullOrEmpty(server.ConfigDir):
                    Log.Info(
                        $"Starting this instance with Server ID: \"{server.Id}\" and config directory: \"{server.ConfigDir}\"...");
                    break;

                case false:
                    Log.Info($"Starting this instance with Server ID: \"{server.Id}\"...");
                    break;

                default:
                {
                    Log.Info(
                        !string.IsNullOrEmpty(server.ConfigDir)
                            ? $"Starting this instance with config directory: \"{server.ConfigDir}\"..."
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
                Log.Error(exception.ToString());

                Logger.Dispose();
            }
        }

        private static void OnExit(object sender, EventArgs e)
        {
            lock (ExitLock)
            {
                if (_exited)
                    return;

                Log.Info("Stopping servers and exiting Laucnher...", color: ConsoleColor.DarkMagenta);

                foreach (var server in InstantiatedServers.Where(server => server.IsServerProcessRunning))
                {
                    try
                    {
                        Log.Info(
                            string.IsNullOrEmpty(server.Id)
                                ? "Stopping the default server..."
                                : $"Stopping server with ID \"{server.Id}\"...", color: ConsoleColor.DarkMagenta);

                        server.Stop();

                        // Wait for server to exit
                        var timeToWait = Math.Max(LauncherConfig.SafeShutdownCheckDelay, 0);
                        var timeWaited = 0;

                        while (server.IsServerProcessRunning)
                        {
                            Thread.Sleep(timeToWait);
                            timeWaited += timeToWait;

                            if (timeWaited < LauncherConfig.SafeShutdownTimeout)
                            {
                                continue;
                            }

                            Log.Error(
                                string.IsNullOrEmpty(server.Id)
                                    ? $"Failed to stop the default server within {timeWaited} ms, giving up..."
                                    : $"Failed to stop server with ID \"{server.Id}\" within {timeWaited} ms, giving up...");
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        // LogDebugException(nameof(OnExit), ex);
                    }
                }

                _exited = true;
            }
        }

        private static bool GetFlagFromArgs(string[] args, string key = null, string alias = null)
        {
            return GetFlagFromArgs(args, [key], [alias]);
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
            return GetParamFromArgs(args, [key], [alias]);
        }

        private static string GetParamFromArgs(string[] args, string[] keys = null, string[] aliases = null)
        {
            var hasKeys = !keys.IsNullOrEmpty();
            var hasAliases = !aliases.IsNullOrEmpty();

            if (!hasKeys && !hasAliases)
            {
                return null;
            }

            for (var i = 0; i < args.Length - 1; i++)
            {
                var lowArg = args[i]?.ToLower();

                if (string.IsNullOrEmpty(lowArg))
                {
                    continue;
                }

                if (hasKeys)
                {
                    if (keys!.Any(key => lowArg == $"--{key?.ToLower()}"))
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

                if (string.IsNullOrEmpty(lowArg))
                {
                    continue;
                }

                if (hasKeys)
                {
                    if (keys!.Any(key => lowArg == $"--{key?.ToLower()}"))
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
}