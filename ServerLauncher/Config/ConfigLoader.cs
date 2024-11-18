using System.Text;
using ServerLauncher.Logger;
using YamlDotNet.Serialization;

namespace ServerLauncher.Config
{
    public sealed class ConfigLoader
    {
        private readonly ISerializer _serializer = new SerializerBuilder()
            .Build();

        private readonly IDeserializer _deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();

        public T Load<T>(string path) where T : IConfig, new()
        {
            Log.Info($"Loading configuration from \"{path}\"");

            if (!File.Exists(path))
            {
                Log.Warn("Configuration file isn't found. Creating a new one.");

                var config = new T();

                Save(path, config);

                return config;
            }

            var data = File.ReadAllText(path, Encoding.UTF8);

            return _deserializer.Deserialize<T>(data);
        }

        public void Save<T>(string path, T config) where T : IConfig
        {
            var data = _serializer.Serialize(config);

            File.WriteAllText(path, data, Encoding.UTF8);
        }
    }
}