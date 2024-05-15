using ServerLauncher.Config.ConfigHandler;

namespace ServerLauncher.Config;

public class ConfigStorage
{
    public ConfigEntry<string> ConfigLocation { get; } = new(
        "config_location", string.Empty, false, "Config Location",
        "Местоположение по умолчанию игры для хранения файлов конфигурации (директория)");

    public ConfigEntry<string> LogLocation { get; } =
        new("launcher_log_location", "logs",
            "MultiAdmin Log Location", "Папка, в которой Launcher будет сохранять журналы (директория)");

    public ConfigEntry<bool> DebugLog { get; } =
        new("launcher_debug_log", true,
            "Launcher Debug Logging",
            "Включает отладочное ведение журнала Launcher, это ведение журнала ведется в отдельный файл, чем все остальные журналы");

    public ConfigEntry<uint> Port { get; } =
        new("port", 7777,
            "Game Port", "Порт, который будет использоваться сервером");

    public ConfigEntry<bool> ManualStart { get; } =
        new("manual_start", true,
            "Manual Start", "Запускать ли сервер автоматически при запуске Launcher");

    public ConfigEntry<decimal> MaxMemory { get; } =
        new("max_memory", 2048,
            "Max Memory", "Максимальное количество памяти, указанное в мегабайтах");

    public ConfigEntry<decimal> RestartLowMemory { get; } =
        new("restart_low_memory", 400,
            "Restart Low Memory", "Перезапуск, если оставшаяся память игры опустится ниже этого значения в мегабайтах");

    public ConfigEntry<uint> RestartLowMemoryTicks { get; } =
        new("restart_low_memory_ticks", 10,
            "Restart Low Memory Ticks", "Количество тактов, которое память может превышать лимит перед перезапуском");

    public ConfigEntry<decimal> RestartLowMemoryRoundEnd { get; } =
        new("restart_low_memory_roundend", 450,
            "Restart Low Memory Round-End",
            "Перезапуск в конце раунда, если оставшаяся память игры опустится ниже этого значения в мегабайтах");

    public ConfigEntry<uint> RestartLowMemoryRoundEndTicks { get; } =
        new("restart_low_memory_roundend_ticks", 10,
            "Restart Low Memory Round-End Ticks",
            "Количество тактов, которое память может превышать лимит перед перезапуском в конце раунд");

    public ConfigEntry<int> SafeShutdownCheckDelay { get; } =
        new("safe_shutdown_check_delay", 100,
            "Safe Shutdown Check Delay",
            "The time in milliseconds between checking if a server is still running when safely shutting down");

    public ConfigEntry<double> ServerRestartTimeout { get; } =
        new("server_restart_timeout", 10,
            "Server Restart Timeout",
            "Время в секундах до принудительного перезапуска сервера Launcher, если он не отвечает на обычную команду перезапуска");

    public ConfigEntry<double> ServerStopTimeout { get; } =
        new("server_stop_timeout", 10,
            "Server Stop Timeout",
            "Время в секундах до принудительного выключения сервера Launcher, если он не отвечает на обычную команду выключения");

    public ConfigEntry<bool> ServerStartRetry { get; } =
        new("server_start_retry", true,
            "Server Start Retry", "Стоит ли пытаться снова запустить сервер после сбоя");

    public ConfigEntry<int> ServerStartRetryDelay { get; } =
        new("server_start_retry_delay", 10000,
            "Server Start Retry Delay",
            "Время в миллисекундах ожидания перед повторной попыткой запустить сервер после сбоя");

    public ConfigEntry<int> MultiAdminTickDelay { get; } =
        new("launch_tick_delay", 1000,
            "Launch Tick Delay",
            "Время в миллисекундах между тактами Launcher (любые функции, которые обновляются со временем)");

    public ConfigEntry<string> ServersFolder { get; } =
        new("servers_folder", "servers",
            "Servers Folder",
            "Местоположение папки servers, из которой Launcher загружает несколько конфигураций серверов");
}