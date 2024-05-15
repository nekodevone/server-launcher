namespace ServerLauncher.Interfaces.Events;

public interface IEventServerStarted : IEvent
{
    void OnServerStarted();
}