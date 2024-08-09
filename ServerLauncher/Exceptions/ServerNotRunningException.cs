namespace ServerLauncher.Exceptions;

public class ServerNotRunningException : ServerException
{
    public ServerNotRunningException() : base("The server is not running")
    {
    }
}