namespace ServerLauncher;

public static class Program
{
    public static Logger Logger { get; private set; } = new(Directory.GetCurrentDirectory());

    public static Version Version { get; } = new Version(1, 0, 0);

    public static bool Headless { get; private set; }
    
    public static void Main()
    {
        try
        {
            new Server.Server("test2", 7777, Directory.GetCurrentDirectory(), new string[0]).Start();
        }
        catch
        {
            Logger.Dispose();
        }
    }
}