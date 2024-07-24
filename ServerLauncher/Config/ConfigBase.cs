using ServerLauncher.Logger;
using System.Text;
using YamlDotNet.Serialization;

namespace ServerLauncher.Config
{
    /// <summary>
    /// Базовый класс для конфигов, предполагается, что рут конфига с различными подконфигами, который
    /// будет сохраняться в какой либо файл наследует этот класс
    /// для дальнейшего его использования при загрузки/сохранении.
    /// </summary>
    public abstract class ConfigBase<T> where T : ConfigBase<T>
    {
        [YamlIgnore]
        public string FilePath { get; set; }

        public ConfigBase(string filePath)
        {
            FilePath = filePath;
        }

        public ConfigBase()
        {
        }

        public T Update()
        {
            Log.Info("Обновляю конфиги...");

            Save();

            Log.Info("Конфиги обновлены");

            return this as T;
        }

        public T Load()
        {
            Log.Info("Загружаю конфиги...");

            if (File.Exists(FilePath))
            {
                var deserializer = new DeserializerBuilder()
                                       .IgnoreUnmatchedProperties()
                                       .Build();

                var readed = File.ReadAllText(FilePath, Encoding.UTF8);

                if (readed.Length > 0)
                {
                    try
                    {
                        var result = deserializer.Deserialize<T>(readed);
                        result.FilePath = FilePath;

                        Log.Info("Конфиги загружены!");

                        return result;
                    }
                    catch
                    {
                    }
                }

                Log.Info("Файл конфигов либо пустой либо имеет неверный формат");
            }

            Log.Info("Файл конфигов не был обнаружен, создаю стандартный");
            Save();

            return this as T;
        }

        private void Save()
        {
            var serializer = new SerializerBuilder().Build();
            var yaml = serializer.Serialize(this);

            if (yaml != null)
            {
                File.WriteAllText(FilePath, yaml, Encoding.UTF8);
            }
        }
    }
}
