namespace Engine.Host;

using Engine.Domain;
using Engine.Domain.Compilation;
using Engine.Eventing.Schema;
using Engine.Kernel;
using Engine.Kernel.Driver;
using Engine.Observability;
using Engine.Projection;

public sealed class KernelRuntimeFactory
{
    private readonly Dictionary<string, IKernelDomainModule> _domains;
    private readonly Dictionary<string, CompiledDomain> _compiledDomains;
    private readonly object _compiledDomainGate = new();

    public KernelRuntimeFactory(IEnumerable<IKernelDomainModule> domains)
    {
        ArgumentNullException.ThrowIfNull(domains);

        _domains = new Dictionary<string, IKernelDomainModule>(StringComparer.OrdinalIgnoreCase);
        _compiledDomains = new Dictionary<string, CompiledDomain>(StringComparer.OrdinalIgnoreCase);
        foreach (var domain in domains)
        {
            if (domain is null)
            {
                throw new ArgumentException("Domain collection cannot contain null entries.", nameof(domains));
            }

            if (string.IsNullOrWhiteSpace(domain.Name))
            {
                throw new ArgumentException("Domain name cannot be null or whitespace.", nameof(domains));
            }

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
        ProvenanceProjection? provenance = null;
        UiChronicleProjection? chronicle = null;

        var runtime = StartCore(
            request,
            builder =>
            {
                map = new HexMapProjection();
                ui = new UiProjectionState();
                provenance = new ProvenanceProjection();
                builder
                    .RegisterProjection("map", _ => map)
                    .RegisterProjection(
                        "ui",
                        context =>
                        {
                            var projection = ui ??
                                throw new InvalidOperationException("UI projection was not initialized.");
                            context.Symbols.UseEntityProjectionState(projection);
                            return projection;
                        })
                    .RegisterProjection(
                        "provenance",
                        context =>
                        {
                            var projection = provenance ??
                                throw new InvalidOperationException("UI provenance projection was not initialized.");
                            context.Symbols.UseProvenanceProjection(projection);
                            return projection;
                        })
                    .RegisterProjection(
                        "chronicle",
                        context =>
                        {
                            chronicle = new UiChronicleProjection(context.Symbols);
                            return chronicle;
                        });
            });
        var provenanceProjection = provenance ??
            throw new InvalidOperationException("UI provenance projection was not registered.");

        var logger = runLogPath is null
            ? null
            : new RunLogger(runLogPath, runtime.Host.Events, runtime.Host.Symbols);
        return new KernelUiRuntime(
            runtime,
            map ?? throw new InvalidOperationException("UI map projection was not registered."),
            ui ?? throw new InvalidOperationException("UI projection was not registered."),
            provenanceProjection,
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
        var compiledDomain = CreateRuntimeCompiledDomain(GetOrCompileDomain(domain));
        var kernelTuning = KernelStartupConfigLoader.LoadKernelTuning(request.ConfigFilePath);
        var builder = new KernelHostBuilder()
            .WithCompiledDomain(compiledDomain)
            .WithSimulationOptions(new SimulationOptions
            {
                Seed = baseSeed,
                EventSchemaValidator = KernelEventSchemaRegistry.Validator,
                TraceEnabled = request.TraceEnabled,
                KernelTuning = kernelTuning,
            })
            .WithDriverConfig(DriverConfigFor(request.FunctionProfile))
            .ConfigureSymbols(domain.ConfigureProjectionSymbols)
            .ConfigurePrograms(programs => ApplyProgramEnabledOverrides(programs, kernelTuning.Programs.Enabled))
            .WithDomainStartupSeed(seedResult.Seed);

        configureConsumers?.Invoke(builder);

        return new KernelRuntime(
            builder.Build(),
            request,
            seedResult.Identity);
    }

    private CompiledDomain GetOrCompileDomain(IKernelDomainModule domain)
    {
        lock (_compiledDomainGate)
        {
            if (_compiledDomains.TryGetValue(domain.Name, out var compiledDomain))
            {
                return compiledDomain;
            }

            compiledDomain = DomainCompiler.Compile(domain.CreateDomainConfig());
            _compiledDomains.Add(domain.Name, compiledDomain);
            return compiledDomain;
        }
    }

    private static CompiledDomain CreateRuntimeCompiledDomain(CompiledDomain cachedDomain) =>
        new()
        {
            Config = cachedDomain.Config,
            Ids = cachedDomain.Ids.Clone(),
            DefinitionGraph = cachedDomain.DefinitionGraph,
            RuntimeCodes = cachedDomain.RuntimeCodes,
            KernelData = cachedDomain.KernelData,
            Programs = cachedDomain.Programs,
            Symbols = cachedDomain.Symbols,
            ChronicleQueries = cachedDomain.ChronicleQueries,
            Diagnostics = cachedDomain.Diagnostics,
        };

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

    private static void ApplyProgramEnabledOverrides(
        KernelProgramConfigurationContext programs,
        ProgramEnabledTuningOptions enabled)
    {
        ApplyProgramEnabledOverride(programs, KernelProgramCodes.Movement, enabled.Movement);
        ApplyProgramEnabledOverride(programs, KernelProgramCodes.LowAltitudeVerbs, enabled.LowAltitudeVerbs);
        ApplyProgramEnabledOverride(programs, KernelProgramCodes.MapActivity, enabled.MapActivity);
        ApplyProgramEnabledOverride(programs, KernelProgramCodes.MapSignals, enabled.MapSignals);
        ApplyProgramEnabledOverride(programs, KernelProgramCodes.Effects, enabled.Effects);
        ApplyProgramEnabledOverride(programs, KernelProgramCodes.CultureSlots, enabled.CultureSlots);
        ApplyProgramEnabledOverride(programs, KernelProgramCodes.Upkeep, enabled.Upkeep);
        ApplyProgramEnabledOverride(programs, KernelProgramCodes.Gambits, enabled.Gambits);
        ApplyProgramEnabledOverride(programs, KernelProgramCodes.Relationships, enabled.Relationships);
        ApplyProgramEnabledOverride(programs, KernelProgramCodes.Generators, enabled.Generators);
        ApplyProgramEnabledOverride(programs, KernelProgramCodes.MinorActors, enabled.MinorActors);
    }

    private static void ApplyProgramEnabledOverride(
        KernelProgramConfigurationContext programs,
        int programId,
        int overrideCode)
    {
        if (overrideCode == ProgramEnabledOverrideCodes.Enabled)
        {
            programs.Enable(programId);
            return;
        }

        if (overrideCode == ProgramEnabledOverrideCodes.Disabled)
        {
            programs.Disable(programId);
        }
    }
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
    private readonly RunLogger? _runLogger;
    private bool _disposed;

    internal KernelUiRuntime(
        KernelRuntime runtime,
        HexMapProjection map,
        UiProjectionState ui,
        ProvenanceProjection provenance,
        UiChronicleProjection chronicle,
        RunLogger? runLogger)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(ui);
        ArgumentNullException.ThrowIfNull(provenance);
        ArgumentNullException.ThrowIfNull(chronicle);

        _runtime = runtime;
        Map = map;
        Ui = ui;
        Provenance = provenance;
        Chronicle = chronicle;
        _runLogger = runLogger;
    }

    public HexMapProjection Map { get; }

    public UiProjectionState Ui { get; }

    public ProvenanceProjection Provenance { get; }

    public UiChronicleProjection Chronicle { get; }

    public ProjectionSymbolCatalog Symbols => _runtime.Symbols;

    public CompiledDomain? CompiledDomain => _runtime.Host.CompiledDomain;

    public DomainIdRegistry DomainIds => _runtime.Host.DomainIds;

    public StartupSeedIdentity SeedIdentity => _runtime.SeedIdentity;

    public IKernelDriverTimeControl Time => _runtime.Host.Time;

    public ICommandSubmitter Commands => _runtime.Host.Commands;

    public string? RunLogPath => _runLogger?.LogPath;

    public KernelRuntimePulse Pump()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var previousTick = Ui.CurrentTick;
        _runtime.Host.DrainConsumers();
        var currentTick = Ui.CurrentTick;
        if (_runLogger is not null)
        {
            _runLogger.DrainAvailable();
            for (var tick = previousTick + 1; tick <= currentTick; tick++)
            {
                _runLogger.OnTickEnded(tick);
            }
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
        _runLogger?.Dispose();
        _runtime.Dispose();
    }
}
