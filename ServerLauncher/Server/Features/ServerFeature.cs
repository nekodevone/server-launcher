using ServerLauncher.Logger;

namespace ServerLauncher.Server.Features
{
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
            Log.Info($"Feature {Name} has been enabled. {Description}", Server.Id);

            IsEnabled = true;
        }

        public virtual void Disabled()
        {
            Log.Info($"Feature {Name} has been disabled.", Server.Id);

            IsEnabled = false;
        }

        public abstract void ConfigReloaded();
    }
}