namespace ServerLauncher.Interfaces.Events;

public interface IEventServerWaitingForPlayers : IEvent
{
    void OnServerWaitingForPlayers();
}