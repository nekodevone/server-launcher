namespace ServerLauncher.Interfaces.Events;

public interface IEventServerRoundEnded : IEvent
{
    void OnServerRoundEnded();
}