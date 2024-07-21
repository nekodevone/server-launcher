namespace ServerLauncher.Server.Features;

public abstract class ServerFeature
{
    public ServerFeature(Server server)
    {
        Server = server;
    }
    
    public abstract string Name { get; }
    
    public abstract string Description { get; }
    
    public bool IsEnabled { get; protected set; }
    
    public Server Server { get; }

    public virtual void Enabled()
    {
        Server.Log($"Feature {Name} has been enabled. {Description}");

        IsEnabled = true;
    }
    
    public virtual void Disabled()
    {
        Server.Log($"Feature {Name} has been disabled.");

        IsEnabled = false;
    }

    public abstract void ConfigReloaded();
}