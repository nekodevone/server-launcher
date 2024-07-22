namespace ServerLauncher.Server.EventArgs;

public class MessageEventArgs
{
    public MessageEventArgs(string message, byte color)
    {
        Message = message;
        Color = color;
    }

    public string Message { get; }

    public byte Color { get; }
}