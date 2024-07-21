using YamlDotNet.Serialization;

namespace ServerLauncher.Config
{
    public sealed class Config : ConfigBase<Config>
    {
        [YamlMember(Description = "Местоположение по умолчанию игры для хранения файлов конфигурации (директория)")]
        public string ConfigLocation { get; set; } = string.Empty;

        [YamlMember(Description = "Папка, в которой Launcher будет сохранять журналы (директория)")]
        public string LogLocation { get; set; } = "logs";

        [YamlMember(Description = "Включает отладочное ведение журнала Launcher, это ведение журнала ведется в отдельный файл, чем все остальные журналы")]
        public bool DebugLog { get; set; } = true;

        [YamlMember(Description = "Порт, который будет использоваться сервером")]
        public uint Port { get; set; } = 7777;

        [YamlMember(Description = "Запускать ли сервер автоматически при запуске Launcher")]
        public bool ManualStart { get; set; } = true;

        [YamlMember(Description = "Максимальное количество памяти, указанное в мегабайтах")]
        public decimal MaxMemory { get; set; } = 2048;

        [YamlMember(Description = "Перезапуск, если оставшаяся память игры опустится ниже этого значения в мегабайтах")]
        public decimal RestartLowMemory { get; set; } = 400;

        [YamlMember(Description = "Количество тактов, которое память может превышать лимит перед перезапуском")]
        public uint RestartLowMemoryTicks { get; set; } = 10;

        [YamlMember(Description = "Перезапуск в конце раунда, если оставшаяся память игры опустится ниже этого значения в мегабайтах")]
        public decimal RestartLowMemoryRoundEnd { get; set; } = 450;

        [YamlMember(Description = "Количество тактов, которое память может превышать лимит перед перезапуском в конце раунд")]
        public uint RestartLowMemoryRoundEndTicks { get; set; } = 10;

        [YamlMember(Description = "The time in milliseconds between checking if a server is still running when safely shutting down")]
        public int SafeShutdownCheckDelay { get; set; } = 100;

        [YamlMember(Description = "Время в секундах до принудительного перезапуска сервера Launcher, если он не отвечает на обычную команду перезапуска")]
        public double ServerRestartTimeout { get; set; } = 10;

        [YamlMember(Description = "Время в секундах до принудительного выключения сервера Launcher, если он не отвечает на обычную команду выключения")]
        public double ServerStopTimeout { get; set; } = 10;

        [YamlMember(Description = "Стоит ли пытаться снова запустить сервер после сбоя")]
        public bool ServerStartRetry { get; set; } = true;

        [YamlMember(Description = "Время в миллисекундах ожидания перед повторной попыткой запустить сервер после сбоя")]
        public int ServerStartRetryDelay { get; set; } = 10000;

        [YamlMember(Description = "Время в миллисекундах между тактами Launcher (любые функции, которые обновляются со временем)")]
        public int MultiAdminTickDelay { get; set; } = 1000;

        [YamlMember(Description = "Servers Folder")]
        public string ServersFolder { get; set; } = "servers";

        public Config(string filePath) : base(filePath)
        {
        }

        public Config()
        {
        }
    }
}
