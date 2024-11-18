namespace ServerLauncher.Exceptions
{
    public abstract class ServerException : Exception
    {
        protected ServerException(string message) : base(message)
        {
        }
    }
}