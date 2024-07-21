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
            Program.Logger.Log("CONFIG", "Обновляю конфиги...");

            Save();

            Program.Logger.Log("CONFIG", "Конфиги обновлены");

            return this as T;
        }

        public T Load()
        {
            Program.Logger.Log("CONFIG", "Загружаю конфиги...");

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

                        Program.Logger.Log("CONFIG", "Конфиги загружены!");

                        return result;
                    }
                    catch
                    {
                    }
                }

                Program.Logger.Log("CONFIG", "Файл конфигов либо пустой либо имеет неверный формат");
            }

            Program.Logger.Log("CONFIG", "Файл конфигов не был обнаружен, создаю стандартный");
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
