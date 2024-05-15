namespace ServerLauncher.Interfaces.Events;

public interface IEventIdleExited : IEvent
{
    void OnServerIdleExited();
}