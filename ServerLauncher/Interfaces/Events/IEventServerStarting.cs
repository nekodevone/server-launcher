namespace ServerLauncher.Interfaces.Events;

public interface IEventServerStarting : IEvent
{
    void OnServerStarting();
}