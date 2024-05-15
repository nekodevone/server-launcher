using ServerLauncher.Utility;

namespace ServerLauncher.Config;

public class Config
{
    private ConfigStorage RawData;
    
    private string internalConfigPath;
    
    public string ConfigPath
    {
        get => internalConfigPath;
        private set
        {
            try
            {
                internalConfigPath = Utils.GetFullPathSafe(value);
            }
            catch (Exception)
            {
                internalConfigPath = value;
            }
        }
    }

    public Config(string path)
    {
        ReadConfigFile(path);
    }

    public void ReadConfigFile()
    {
        ReadConfigFile(ConfigPath);
    }
    
    private void ReadConfigFile(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }
        
        ConfigPath = path;
        
        Console.WriteLine(ConfigPath);

        try
        {
           //RawData = File.Exists(ConfigPath) ? JsonSerializer.Deserialize<ConfigStorage>(ConfigPath) : null;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}