namespace ServerLauncher.Server.Enums;

[Flags]
public enum ModFeatures
{
    None = 0,

    // Replaces detecting game output with MultiAdmin events for game events
    CustomEvents = 1 << 0,

    // Supporting all current features
    All = ~(~0 << 1)
}