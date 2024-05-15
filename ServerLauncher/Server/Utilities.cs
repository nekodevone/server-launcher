namespace ServerLauncher.Server;

public static class Utilities
{
    public static string GetExecutablePath()
    {
        var gameExe = string.Empty;

        if (OperatingSystem.IsLinux())
        {
            gameExe = "SCPSL.x86_64";
        }
        else if (OperatingSystem.IsWindows())
        {
            gameExe = "SCPSL.exe";
        }
        else
        {
            throw new FileNotFoundException("Invalid OS, can't run executable");
        }
        
        if (!File.Exists(gameExe))
        {
            throw new FileNotFoundException(
                $"Can't find game executable \"{gameExe}\", the working directory must be the game directory");
        }
        
        return gameExe;
    }
}