namespace ServerLauncher.Server.Handlers.Enums;

public enum OutputCodes : byte
{
    //0x00 - 0x0F - reserved for colors

    RoundRestart = 0x10,
    IdleEnter = 0x11,
    IdleExit = 0x12,
    ExitActionReset = 0x13,
    ExitActionShutdown = 0x14,
    ExitActionSilentShutdown = 0x15,
    ExitActionRestart = 0x16,
    Heartbeat = 0x17
}