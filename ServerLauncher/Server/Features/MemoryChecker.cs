using ServerLauncher.Interfaces.Events;
using ServerLauncher.Logger;
using ServerLauncher.Server.Enums;
using ServerLauncher.Server.Features.Attributes;

namespace ServerLauncher.Server.Features;

[ServerFeature]
public class MemoryChecker : ServerFeature, IEventServerTick, IEventServerRoundEnded
{
    public MemoryChecker(Server server) : base(server)
    {
    }

    public override string Name => "MemoryChecker";

    public override string Description => "Restarts the server if the working memory becomes too low";

    public long LowBytes { get; set; }
    public long LowBytesSoft { get; set; }

    public long MaxBytes { get; set; }

    public long MemoryUsedBytes
    {
        get
        {
            if (Server.GameProcess is null)
                return 0;

            Server.GameProcess.Refresh();

            return Server.GameProcess.WorkingSet64;
        }
    }

    public long MemoryLeftBytes => MaxBytes - MemoryUsedBytes;

    public decimal LowMb
    {
        get => decimal.Divide(LowBytes, BytesInMegabyte);
        set => LowBytes = (long)decimal.Multiply(value, BytesInMegabyte);
    }

    public decimal LowMbSoft
    {
        get => decimal.Divide(LowBytesSoft, BytesInMegabyte);
        set => LowBytesSoft = (long)decimal.Multiply(value, BytesInMegabyte);
    }

    public decimal MaxMb
    {
        get => decimal.Divide(MaxBytes, BytesInMegabyte);
        set => MaxBytes = (long)decimal.Multiply(value, BytesInMegabyte);
    }

    public decimal MemoryUsedMb => decimal.Divide(MemoryUsedBytes, BytesInMegabyte);

    public decimal MemoryLeftMb => decimal.Divide(MemoryLeftBytes, BytesInMegabyte);

    private const decimal BytesInMegabyte = 1048576;

    private const int OutputPrecision = 2;

    private uint _tickCount;
    private uint _tickCountSoft;

    private uint _maxTicks = 10;
    private uint _maxTicksSoft = 10;

    private bool _restart;

    public override void Enabled()
    {
        _tickCount = 0;
        _tickCountSoft = 0;

        _restart = false;

        base.Enabled();
    }

    public override void ConfigReloaded()
    {
        _maxTicks = Server.Config.RestartLowMemoryTicks;
        _maxTicksSoft = Server.Config.RestartLowMemoryRoundEndTicks;

        LowMb = Server.Config.RestartLowMemory;
        LowMbSoft = Server.Config.RestartLowMemoryRoundEnd;
        MaxMb = Server.Config.MaxMemory;
    }

    public void OnServerTick()
    {
        if (LowBytes < 0 && LowBytesSoft < 0 || MaxBytes < 0) return;

        if (_tickCount < _maxTicks && LowBytes >= 0 && MemoryLeftBytes <= LowBytes)
        {
            Log.Warning(
                $"Program is running low on memory ({decimal.Round(MemoryLeftMb, OutputPrecision)} MB left), the server will restart if it continues",
                Server.Id);
            _tickCount++;
        }
        else
        {
            _tickCount = 0;
        }

        if (!_restart && _tickCountSoft < _maxTicksSoft && LowBytesSoft >= 0 && MemoryLeftBytes <= LowBytesSoft)
        {
            Log.Warning(
                $"Program is running low on memory ({decimal.Round(MemoryLeftMb, OutputPrecision)} MB left), the server will restart at the end of the round if it continues",
                Server.Id);
            _tickCountSoft++;
        }
        else
        {
            _tickCountSoft = 0;
        }

        if (Server.Status is ServerStatusType.Restarting) return;

        if (_tickCount >= _maxTicks)
        {
            Log.Error("Restarting due to low memory...", Server.Id);
            Server.Restart();

            _restart = false;
        }
        else if (!_restart && _tickCountSoft >= _maxTicksSoft)
        {
            Log.Warning("Server will restart at the end of the round due to low memory", Server.Id);

            _restart = true;
        }
    }

    public void OnServerRoundEnded()
    {
        if (!_restart || Server.IsStopping) return;

        Log.Error("Restarting due to low memory (Round End)...");

        Server.Restart();

        Enabled();
    }
}