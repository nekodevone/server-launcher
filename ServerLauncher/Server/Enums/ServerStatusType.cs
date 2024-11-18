namespace ServerLauncher.Server.Enums
{
    public enum ServerStatusType
    {
        NotStarted,
        Starting,
        Running,
        Stopping,
        ExitActionStop,
        ForceStopping,
        Restarting,
        ExitActionRestart,
        Stopped,
        StoppedUnexpectedly
    }
}