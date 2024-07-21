namespace ServerLauncher;

public static class Program
{
    public static Logger Logger { get; private set; } = new(Directory.GetCurrentDirectory());

    public static Version Version { get; } = new(1, 0, 0);

    public static Config.Config GlobalConfig = new(Path.Combine(Directory.GetCurrentDirectory(), "config.yml"));

    public static bool Headless { get; private set; }

    public static void Main()
    {
        GlobalConfig = GlobalConfig.Load();

        try
        {
            new Server.Server("test2", 7777, Directory.GetCurrentDirectory(), new string[0]).Start();
        }
        catch (Exception exception)
        {
            Logger.Error("SERVER", exception.ToString());
            
            Logger.Dispose();
        }
    }
}