namespace TheCanonryGame.Game;

using Engine.Host;
using Engine.Kernel.Driver;
using Engine.Projection;

using IceRemembers.Seed;

using TheCanonryGame.Game.Tableau;

internal enum GameSeedPolicyKind
{
    Random = 0,
    Requested = 1,
}

internal readonly record struct GameSeedPolicy
{
    private GameSeedPolicy(GameSeedPolicyKind kind, int requestedSeed)
    {
        Kind = kind;
        RequestedSeed = requestedSeed;
    }

    public GameSeedPolicyKind Kind { get; }

    public int RequestedSeed { get; }

    public static GameSeedPolicy Random() => new(GameSeedPolicyKind.Random, 0);

    public static GameSeedPolicy Requested(int seed)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(seed);
        return new GameSeedPolicy(GameSeedPolicyKind.Requested, seed);
    }

    internal StartupSeedPolicy ToStartupSeedPolicy() => Kind switch
    {
        GameSeedPolicyKind.Random => StartupSeedPolicy.Random(),
        GameSeedPolicyKind.Requested => StartupSeedPolicy.Requested(RequestedSeed),
        _ => throw new InvalidOperationException($"Unknown game seed policy '{Kind}'."),
    };
}

internal sealed record GameSeedIdentity(int BaseSeed, int Attempt);

internal readonly record struct GameRuntimePulse(int PreviousTick, int CurrentTick)
{
    public int TicksProcessed => Math.Max(0, CurrentTick - PreviousTick);
}

internal sealed class GameRuntimeReadModels
{
    internal GameRuntimeReadModels(KernelUiRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        Map = runtime.Map;
        Entities = runtime.Ui;
        Chronicle = runtime.Chronicle;
        Symbols = runtime.Symbols;
    }

    public HexMapProjection Map { get; }

    public UiProjectionState Entities { get; }

    public UiChronicleProjection Chronicle { get; }

    public IProjectionSymbolCatalog Symbols { get; }
}

internal sealed class GameRuntimeControls
{
    private readonly IKernelDriverTimeControl _time;

    internal GameRuntimeControls(IKernelDriverTimeControl time)
    {
        ArgumentNullException.ThrowIfNull(time);
        _time = time;
    }

    public bool IsPaused => _time.IsPaused;

    public double TicksPerSecond => _time.TicksPerSecond;

    public int CurrentTick => _time.CurrentTick;

    public void Pause() => _time.RequestPause();

    public void Resume() => _time.RequestResume();

    public void Step(int ticks) => _time.RequestSingleStep(ticks);

    public void SetTickRate(double ticksPerSecond) => _time.SetTickRate(ticksPerSecond);
}

internal sealed class GameIntentPort
{
    private readonly ICommandSubmitter _commands;
    private readonly Func<long> _worldVersion;
    private int _submitterSequence;

    internal GameIntentPort(
        ICommandSubmitter commands,
        Func<long> worldVersion)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(worldVersion);
        _commands = commands;
        _worldVersion = worldVersion;
    }

    public bool RequestGeneratorEnabled(int generatorId, bool enabled)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(generatorId);

        var version = _worldVersion();
        var issueVersion = version > int.MaxValue ? int.MaxValue : (int)version;
        var command = new SetGeneratorEnabledCommand(
            issueVersion,
            generatorId,
            enabled,
            SubmitterId: 1,
            SubmitterSequence: ++_submitterSequence);
        return _commands.TrySetGeneratorEnabled(command, out _);
    }
}

internal sealed class CanonryGameRuntime : IDisposable
{
    private readonly KernelUiRuntime _runtime;
    private bool _disposed;

    internal CanonryGameRuntime(KernelUiRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;
        ReadModels = new GameRuntimeReadModels(runtime);
        Controls = new GameRuntimeControls(runtime.Time);
        Intents = new GameIntentPort(runtime.Commands, () => runtime.Ui.WorldVersion);
        Tableau = new GameRuntimeTableau(ReadModels.Entities, ReadModels.Symbols, Intents);
        Seed = new GameSeedIdentity(runtime.SeedIdentity.BaseSeed, runtime.SeedIdentity.Attempt);
    }

    public GameRuntimeReadModels ReadModels { get; }

    public GameRuntimeControls Controls { get; }

    public GameIntentPort Intents { get; }

    public GameRuntimeTableau Tableau { get; }

    public GameSeedIdentity Seed { get; }

    public string RunLogPath => _runtime.RunLogPath;

    public GameRuntimePulse Update()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var pulse = _runtime.Pump();
        return new GameRuntimePulse(pulse.PreviousTick, pulse.CurrentTick);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _runtime.Dispose();
    }
}

internal static class CanonryGameRuntimeFactory
{
    private static readonly KernelRuntimeFactory RuntimeFactory = IceRemembersRuntime.CreateRuntimeFactory();

    public static CanonryGameRuntime Start(
        GameSeedPolicy seedPolicy,
        string? runLogPath = null)
    {
        var runtime = RuntimeFactory.StartUiRuntime(new KernelStartRequest(
            IceRemembersRuntime.DomainName,
            seedPolicy.ToStartupSeedPolicy(),
            KernelFunctionProfile.InteractiveRealtime),
            runLogPath);
        return new CanonryGameRuntime(runtime);
    }
}
