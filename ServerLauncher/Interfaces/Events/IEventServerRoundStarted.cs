namespace ServerLauncher.Interfaces.Events;

public interface IEventServerRoundStarted : IEvent
{
    void OnServerRoundStarted();
}