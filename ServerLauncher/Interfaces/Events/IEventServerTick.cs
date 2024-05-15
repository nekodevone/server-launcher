namespace ServerLauncher.Interfaces.Events;

public interface IEventServerTick : IEvent
{
    void OnServerTick();
}