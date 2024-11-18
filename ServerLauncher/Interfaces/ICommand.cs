namespace ServerLauncher.Interfaces
{
    public interface ICommand
    {
        string Command { get; }

        string Description { get; }

        string Usage { get; }

        bool IsPassToGame { get; }

        void Execute(ArraySegment<string> arguments);
    }
}