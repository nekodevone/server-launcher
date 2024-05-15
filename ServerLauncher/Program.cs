namespace ServerLauncher;

public static class Program
{
    public static Logger Logger { get; private set; }

    public static Version Version { get; } = new Version(1, 0, 0);

    public static bool Headless { get; private set; }
    
    public static void Main()
    {
    }
}