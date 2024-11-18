using YamlDotNet.Serialization;

namespace ServerLauncher.Config
{
    public sealed class LauncherConfig : IConfig
    {
        [YamlMember(Description = "The folder where the Launcher will save logs (directory)")]
        public string LogsDir { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "logs");

        [YamlMember(Description = "Path to server configurations folder (i.e. ~/.config/SCP Secret Laboratory/config)")]
        public string ConfigDir { get; set; } =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SCP Secret Laboratory", "config"
            );

        [YamlMember(Description =
            "The time in milliseconds between checking if a server is still running when safely shutting down")]
        public int SafeShutdownCheckDelay { get; set; } = 100;

        [YamlMember(Description = "The time in milliseconds before Launcher gives up on safely shutting down a server")]
        public int SafeShutdownTimeout { get; set; } = 10000;

        [YamlMember(Description =
            "The time in seconds before forcibly restarting the Launcher server if it does not respond to the normal restart command")]
        public double ServerRestartTimeout { get; set; } = 10;

        [YamlMember(Description =
            "The time in seconds before forcibly shutting down the Launcher server if it does not respond to the normal shutdown command")]
        public double ServerStopTimeout { get; set; } = 10;

        [YamlMember(Description = "Should the server be retried to start after a failure")]
        public bool ServerStartRetry { get; set; } = true;

        [YamlMember(Description =
            "The time in milliseconds to wait before retrying to start the server after a failure")]
        public int ServerStartRetryDelay { get; set; } = 10000;

        [YamlMember(Description =
            "The time in milliseconds between Launcher ticks (any functions that update over time)")]
        public int TickDelay { get; set; } = 1000;
    }
}