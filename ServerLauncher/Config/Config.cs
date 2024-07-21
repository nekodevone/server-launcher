using System.Text;
using ServerLauncher.Utility;

namespace ServerLauncher.Config;

public class Config
{
    private string[] RawData;
    
    private string internalConfigPath;
    
    public string ConfigPath
    {
        get => internalConfigPath;
        private set
        {
            try
            {
                internalConfigPath = Utilities.GetFullPathSafe(value);
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

        try
        {
           RawData = File.Exists(ConfigPath) ? File.ReadAllLines(ConfigPath, Encoding.UTF8) : Array.Empty<string>();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    public bool Contains(string key)
    {
        return RawData != null &&
               RawData.Any(entry => entry.StartsWith($"{key}:", StringComparison.CurrentCultureIgnoreCase));
    }
}