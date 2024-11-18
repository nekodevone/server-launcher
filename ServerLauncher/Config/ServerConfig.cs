using YamlDotNet.Serialization;

namespace ServerLauncher.Config
{
    public sealed class ServerConfig : IConfig
    {
        [YamlMember(Description = "Server port")]
        public uint Port { get; set; } = 7777;

        [YamlMember(Description = "The maximum amount of memory specified in megabytes")]
        public decimal MaxMemory { get; set; } = 2048;

        [YamlMember(Description = "Restart if the remaining game memory falls below this value in megabytes")]
        public decimal RestartLowMemory { get; set; } = 400;

        [YamlMember(Description = "The number of ticks memory can exceed the limit before restarting")]
        public uint RestartLowMemoryTicks { get; set; } = 10;

        [YamlMember(Description =
            "Restart at the end of the round if the remaining game memory falls below this value in megabytes")]
        public decimal RestartLowMemoryRoundEnd { get; set; } = 450;

        [YamlMember(Description =
            "The number of ticks memory can exceed the limit before restarting at the end of the round")]
        public uint RestartLowMemoryRoundEndTicks { get; set; } = 10;
    }
}