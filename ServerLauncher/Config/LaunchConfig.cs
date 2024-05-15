using ServerLauncher.Config.ConfigHandler;
using ServerLauncher.Utility;

namespace ServerLauncher.Config;

public class LaunchConfig : InheritableConfigRegister
{
    public static readonly string GlobalConfigFilePath = Utils.GetFullPathSafe(ConfigFileName);

    public static readonly LaunchConfig GlobalConfig = new(GlobalConfigFilePath, null);

    public const string ConfigFileName = "launcher.json";

    public ConfigStorage Storage { get; } = new();

    public LaunchConfig ParentConfig
    {
        get => ParentConfigRegister as LaunchConfig;
        protected set => ParentConfigRegister = value;
    }

    public Config Config { get; }

    public LaunchConfig(Config config, LaunchConfig parentConfig, bool createConfig = true)
    {
        Config = config;
        ParentConfig = parentConfig;

        if (createConfig && !File.Exists(Config?.ConfigPath))
        {
            try
            {
                if (Config?.ConfigPath is not null)
                {
                    File.Create(Config.ConfigPath).Close();
                }
            }
            catch (Exception e)
            {
            }
        }

        #region Config Register

        foreach (var property in GetType().GetProperties())
        {
            if (property.GetValue(this) is ConfigEntry entry)
            {
                RegisterConfig(entry);
            }
        }

        #endregion

        ReloadConfig();
    }

    public LaunchConfig(Config config, bool createConfig = true) : this(config, GlobalConfig, createConfig)
    {
    }

    public LaunchConfig(string path, LaunchConfig parentConfig, bool createConfig = true) : this(
        new Config(path), parentConfig, createConfig)
    {
    }

    public LaunchConfig(string path, bool createConfig = true) : this(path, GlobalConfig, createConfig)
    {
    }

    public override bool ShouldInheritConfigEntry(ConfigEntry configEntry)
    {
        return ConfigContains(configEntry.Key);
    }

    public override void UpdateConfigValueInheritable(ConfigEntry configEntry)
    {
        Console.WriteLine(configEntry.Description);
    }

    private bool ConfigContains(string key)
    {
        return Config != null && Config.Contains(key);
    }

    private void ReloadConfig()
    {
        ParentConfig?.ReloadConfig();
        Config?.ReadConfigFile();

        UpdateRegisteredConfigValues();
    }
}