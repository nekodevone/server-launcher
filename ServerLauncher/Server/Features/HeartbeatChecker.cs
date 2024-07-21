using ServerLauncher.Interfaces.Events;
using ServerLauncher.Server.Features.Attributes;

namespace ServerLauncher.Server.Features;

[ServerFeature]
public class HeartbeatChecker : ServerFeature, IEventServerTick
{
    public HeartbeatChecker(Server server) : base(server)
    {
    }

    public override string Name => "Heartbeat Checker";

    public override string Description => "Check hearbeat";
    
    public override void ConfigReloaded() { }
    
    public void OnServerTick()
    {
        Console.WriteLine(1);
    }
}