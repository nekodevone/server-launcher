namespace ServerLauncher;

public static class Program
{
    public static Logger Logger { get; private set; }

    public static void Main()
    {
        Logger = new Logger(Directory.GetCurrentDirectory() + "/Test");
        
        Logger.Log("System", "Hello, world");

        Console.ReadLine();
    }
}