namespace ServerLauncher.Exceptions
{
    public class ServerAlreadyRunningException : ServerException
    {
        public ServerAlreadyRunningException() : base("The server is already running")
        {
        }
    }
}