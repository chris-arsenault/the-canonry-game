namespace TheCanonryGame.Game;

#pragma warning disable CA2000 // GameStartupResult transfers runtime ownership to the application shell.

using Engine.Host;
using Engine.Kernel.Driver;

using IceRemembers.Seed;

using TheCanonryGame.Game.Runtime;
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

internal enum GameStartupStage
{
    CheckingCachedWorld,
    LoadingCachedWorld,
    LoadingPregeneratedWorld,
    GeneratingWorld,
    SavingCachedWorld,
    StartingSimulation,
    PrewarmingSimulation,
}

internal enum GameStartupSourceKind
{
    Generated,
    GeneratedAndCached,
    Cached,
    PregeneratedWorldFile,
}

internal sealed record GameWorldCacheOptions(string Directory, bool Regenerate = false)
{
    public string Directory { get; } = string.IsNullOrWhiteSpace(Directory)
        ? throw new ArgumentException("World cache directory cannot be blank.", nameof(Directory))
        : Directory;
}

internal sealed record GamePregeneratedWorld(
    int BaseSeed,
    string Path,
    long SizeBytes)
{
    public string DisplayName => $"seed {BaseSeed}  ·  {SizeBytes / (1024d * 1024d):0} MB";
}

internal sealed record GameStartupResult(
    CanonryGameRuntime Runtime,
    GameStartupSourceKind SourceKind,
    string? SourcePath,
    string? Warning);

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
        Tableau = new GameRuntimeTableau(ReadModels.Entities, ReadModels.Symbols, Intents, ReadModels.Knowledge);
        Seed = new GameSeedIdentity(runtime.SeedIdentity.BaseSeed, runtime.SeedIdentity.Attempt);
    }

    public GameRuntimeReadModels ReadModels { get; }

    public GameRuntimeControls Controls { get; }

    public GameIntentPort Intents { get; }

    public GameRuntimeTableau Tableau { get; }

    public GameSeedIdentity Seed { get; }

    public string? RunLogPath => _runtime.RunLogPath;

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

    public static GameStartupResult StartInteractive(
        GameSeedPolicy seedPolicy,
        GameWorldCacheOptions? cacheOptions,
        Action<GameStartupStage>? reportStage = null,
        string? runLogPath = null)
    {
        DomainStartupSeedResult? startupSeed = null;
        IceRemembersStartupSeedCacheResult? cacheResult = null;
        if (cacheOptions is not null)
        {
            if (seedPolicy.Kind != GameSeedPolicyKind.Requested)
            {
                throw new InvalidOperationException("Only requested world seeds can use the startup cache.");
            }

            cacheResult = IceRemembersStartupSeedCache.LoadOrCreate(
                seedPolicy.RequestedSeed,
                cacheOptions.Directory,
                cacheOptions.Regenerate,
                stage => reportStage?.Invoke(MapCacheStage(stage)));
            startupSeed = cacheResult.StartupSeed;
        }

        reportStage?.Invoke(GameStartupStage.StartingSimulation);
        var runtime = StartUiRuntime(seedPolicy, startupSeed, runLogPath);
        var sourceKind = cacheResult is null
            ? GameStartupSourceKind.Generated
            : CacheSourceKind(cacheResult);
        return new GameStartupResult(
            runtime,
            sourceKind,
            cacheResult?.CachePath,
            cacheResult?.Warning);
    }

    private static GameStartupSourceKind CacheSourceKind(IceRemembersStartupSeedCacheResult cacheResult) =>
        cacheResult.CacheHit
            ? GameStartupSourceKind.Cached
            : GameStartupSourceKind.GeneratedAndCached;

    public static IReadOnlyList<GamePregeneratedWorld> DiscoverPregeneratedWorlds(string directory) =>
        IceRemembersPregeneratedWorldStore.Discover(directory)
            .Select(static world => new GamePregeneratedWorld(
                world.BaseSeed,
                world.Path,
                world.SizeBytes))
            .ToArray();

    public static GameStartupResult StartPregenerated(
        GamePregeneratedWorld world,
        Action<GameStartupStage>? reportStage = null,
        string? runLogPath = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        reportStage?.Invoke(GameStartupStage.LoadingPregeneratedWorld);
        var startupSeed = IceRemembersPregeneratedWorldStore.Read(
            new IceRemembersPregeneratedWorldInfo(world.BaseSeed, world.Path, world.SizeBytes));
        reportStage?.Invoke(GameStartupStage.StartingSimulation);
        var runtime = StartUiRuntime(
            GameSeedPolicy.Requested(world.BaseSeed),
            startupSeed,
            runLogPath);
        return new GameStartupResult(
            runtime,
            GameStartupSourceKind.PregeneratedWorldFile,
            world.Path,
            null);
    }

    private static CanonryGameRuntime StartUiRuntime(
        GameSeedPolicy seedPolicy,
        DomainStartupSeedResult? startupSeed,
        string? runLogPath)
    {
        var runtime = RuntimeFactory.StartUiRuntime(new KernelStartRequest(
            IceRemembersRuntime.DomainName,
            seedPolicy.ToStartupSeedPolicy(),
            KernelFunctionProfile.InteractiveRealtime,
            startupSeed: startupSeed),
            runLogPath);
        return new CanonryGameRuntime(runtime);
    }

    private static GameStartupStage MapCacheStage(IceRemembersStartupSeedCacheStage stage) => stage switch
    {
        IceRemembersStartupSeedCacheStage.CheckingCache => GameStartupStage.CheckingCachedWorld,
        IceRemembersStartupSeedCacheStage.ReadingCache => GameStartupStage.LoadingCachedWorld,
        IceRemembersStartupSeedCacheStage.GeneratingWorld => GameStartupStage.GeneratingWorld,
        IceRemembersStartupSeedCacheStage.WritingCache => GameStartupStage.SavingCachedWorld,
        _ => throw new InvalidOperationException($"Unknown startup cache stage '{stage}'."),
    };
}
