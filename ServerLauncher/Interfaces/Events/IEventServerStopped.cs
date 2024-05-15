namespace ServerLauncher.Interfaces.Events;

public interface IEventServerStopped : IEvent
{
    void OnServerStopped();
}