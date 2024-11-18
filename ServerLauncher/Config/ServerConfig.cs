using YamlDotNet.Serialization;

namespace ServerLauncher.Config
{
    public sealed class ServerConfig : IConfig
    {
        [YamlMember(Description = "Server port")]
        public uint Port { get; set; } = 7777;
    }
}