namespace ServerLauncher.Interfaces.Events;

public interface IEventServerIdleEntered : IEvent
{
    void OnServerIdleEntered();
}