namespace ServerLauncher.Interfaces.Events;

public interface IEventServerCrashed : IEvent
{
    void OnServerCrashed();
}