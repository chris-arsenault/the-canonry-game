namespace Engine.Host;

using Engine.Eventing.Schema;
using Engine.Domain;
using Engine.Kernel;
using Engine.Kernel.Driver;
using Engine.Observability;
using Engine.Projection;

public sealed class KernelRuntimeFactory
{
    private readonly Dictionary<string, IKernelDomainModule> _domains;

    public KernelRuntimeFactory(IEnumerable<IKernelDomainModule> domains)
    {
        ArgumentNullException.ThrowIfNull(domains);

        _domains = new Dictionary<string, IKernelDomainModule>(StringComparer.OrdinalIgnoreCase);
        foreach (var domain in domains)
        {
            ArgumentNullException.ThrowIfNull(domain);
            ArgumentException.ThrowIfNullOrWhiteSpace(domain.Name);
            if (!_domains.TryAdd(domain.Name, domain))
            {
                throw new InvalidOperationException($"Domain '{domain.Name}' is already registered.");
            }
        }
    }

    public static KernelRuntimeFactory ForDomains(params IKernelDomainModule[] domains) =>
        new(domains);

    public KernelRuntime Start(KernelStartRequest request) =>
        StartCore(request, configureConsumers: null);

    public KernelUiRuntime StartUiRuntime(KernelStartRequest request, string? runLogPath = null)
    {
        HexMapProjection? map = null;
        UiProjectionState? ui = null;
        UiChronicleProjection? chronicle = null;

        var runtime = StartCore(
            request,
            builder =>
            {
                map = new HexMapProjection();
                ui = new UiProjectionState();
                builder
                    .RegisterProjection("map", _ => map)
                    .RegisterProjection("ui", _ => ui)
                    .RegisterProjection("chronicle", context =>
                    {
                        chronicle = new UiChronicleProjection(context.Symbols);
                        return chronicle;
                    });
            });

        var logger = new RunLogger(runLogPath ?? RunLogger.DefaultLogPath(), runtime.Host.Events, runtime.Host.Symbols);
        return new KernelUiRuntime(
            runtime,
            map ?? throw new InvalidOperationException("UI map projection was not registered."),
            ui ?? throw new InvalidOperationException("UI projection was not registered."),
            chronicle ?? throw new InvalidOperationException("UI chronicle projection was not registered."),
            logger);
    }

    private KernelRuntime StartCore(
        KernelStartRequest request,
        Action<KernelHostBuilder>? configureConsumers)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_domains.TryGetValue(request.DomainName, out var domain))
        {
            throw new InvalidOperationException($"Domain '{request.DomainName}' is not registered.");
        }

        var baseSeed = request.SeedPolicy.ResolveBaseSeed();
        var seedResult = domain.BuildStartupSeed(new DomainStartupSeedRequest(baseSeed));
        var builder = new KernelHostBuilder()
            .WithDomainConfig(domain.CreateDomainConfig())
            .WithSimulationOptions(new SimulationOptions
            {
                Seed = baseSeed,
                EventSchemaValidator = KernelEventSchemaRegistry.Validator,
            })
            .WithDriverConfig(DriverConfigFor(request.FunctionProfile))
            .ConfigureSymbols(domain.ConfigureProjectionSymbols)
            .WithDomainStartupSeed(seedResult.Seed);

        configureConsumers?.Invoke(builder);

        return new KernelRuntime(
            builder.Build(),
            request,
            seedResult.Identity);
    }

    private static RealtimeDriverConfig DriverConfigFor(KernelFunctionProfile profile) => profile switch
    {
        KernelFunctionProfile.InteractiveRealtime => new RealtimeDriverConfig(
            TicksPerSecond: 1d,
            MaxTicksPerSlice: 8,
            SliceInterval: TimeSpan.FromMilliseconds(16)),
        KernelFunctionProfile.ManualStep => new RealtimeDriverConfig(
            TicksPerSecond: 0d,
            MaxTicksPerSlice: 1,
            SliceInterval: TimeSpan.FromMilliseconds(16)),
        KernelFunctionProfile.Batch => new RealtimeDriverConfig(
            TicksPerSecond: 0d,
            MaxTicksPerSlice: 1,
            SliceInterval: TimeSpan.FromMilliseconds(16)),
        _ => throw new InvalidOperationException($"Unknown kernel function profile '{profile}'."),
    };
}

public sealed class KernelRuntime : IDisposable
{
    private bool _disposed;

    internal KernelRuntime(
        KernelHost host,
        KernelStartRequest request,
        StartupSeedIdentity seedIdentity)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(seedIdentity);

        Host = host;
        Request = request;
        SeedIdentity = seedIdentity;
    }

    public KernelHost Host { get; }

    public KernelStartRequest Request { get; }

    public StartupSeedIdentity SeedIdentity { get; }

    public ProjectionSymbolCatalog Symbols => Host.Symbols;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Host.Dispose();
    }
}

public readonly record struct KernelRuntimePulse(
    int PreviousTick,
    int CurrentTick)
{
    public int TicksProcessed => Math.Max(0, CurrentTick - PreviousTick);
}

public sealed class KernelUiRuntime : IDisposable
{
    private readonly KernelRuntime _runtime;
    private readonly RunLogger _runLogger;
    private bool _disposed;

    internal KernelUiRuntime(
        KernelRuntime runtime,
        HexMapProjection map,
        UiProjectionState ui,
        UiChronicleProjection chronicle,
        RunLogger runLogger)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(ui);
        ArgumentNullException.ThrowIfNull(chronicle);
        ArgumentNullException.ThrowIfNull(runLogger);

        _runtime = runtime;
        Map = map;
        Ui = ui;
        Chronicle = chronicle;
        _runLogger = runLogger;
    }

    public HexMapProjection Map { get; }

    public UiProjectionState Ui { get; }

    public UiChronicleProjection Chronicle { get; }

    public ProjectionSymbolCatalog Symbols => _runtime.Symbols;

    public DomainIdRegistry DomainIds => _runtime.Host.DomainIds;

    public StartupSeedIdentity SeedIdentity => _runtime.SeedIdentity;

    public IKernelDriverTimeControl Time => _runtime.Host.Time;

    public ICommandSubmitter Commands => _runtime.Host.Commands;

    public string RunLogPath => _runLogger.LogPath;

    public KernelRuntimePulse Pump()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var previousTick = Ui.CurrentTick;
        _runtime.Host.DrainConsumers();
        _runLogger.DrainAvailable();
        var currentTick = Ui.CurrentTick;

        for (var tick = previousTick + 1; tick <= currentTick; tick++)
        {
            _runLogger.OnTickEnded(tick);
        }

        return new KernelRuntimePulse(previousTick, currentTick);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _runLogger.Dispose();
        _runtime.Dispose();
    }
}
