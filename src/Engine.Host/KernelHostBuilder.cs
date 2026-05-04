namespace Engine.Host;

using Engine.Domain;
using Engine.Domain.Compilation;
using Engine.Domain.Config;
using Engine.Domain.Startup;
using Engine.Eventing.Schema;
using Engine.Kernel;
using Engine.Kernel.Driver;
using Engine.Projection;

internal sealed class KernelHostBuilder
{
    private readonly List<Action<DomainIdRegistry>> _domainRegistrations = [];
    private readonly List<Action<KernelSymbolContext>> _symbolConfigurators = [];
    private readonly List<Action<KernelProgramConfigurationContext>> _programConfigurators = [];
    private readonly List<Action<KernelConfigurationContext>> _kernelConfigurators = [];
    private readonly List<Action<KernelHostConsumerContext>> _consumerRegistrations = [];
    private DomainIdRegistry? _domainIds;
    private CompiledDomain? _compiledDomain;
    private DomainStartupSeed? _domainStartupSeed;
    private Simulation? _kernel;
    private byte[]? _kernelState;
    private IKernelStartupSeeder? _startupSeeder;
    private SimulationOptions _simulationOptions = new()
    {
        EventSchemaValidator = KernelEventSchemaRegistry.Validator,
    };
    private RealtimeDriverConfig _driverConfig = new(
        TicksPerSecond: 0d,
        MaxTicksPerSlice: 1,
        SliceInterval: TimeSpan.FromMilliseconds(16));
    private int _seedHeadroom = 128;

    public KernelHostBuilder WithKernel(Simulation kernel)
    {
        ArgumentNullException.ThrowIfNull(kernel);
        _kernel = kernel;
        _kernelState = null;
        return this;
    }

    public KernelHostBuilder WithKernelState(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);
        using var memory = new MemoryStream();
        source.CopyTo(memory);
        _kernelState = memory.ToArray();
        _kernel = null;
        return this;
    }

    public KernelHostBuilder WithDomainIds(DomainIdRegistry domainIds)
    {
        ArgumentNullException.ThrowIfNull(domainIds);
        _domainIds = domainIds;
        return this;
    }

    public KernelHostBuilder WithDomainConfig(DomainConfig config) =>
        WithCompiledDomain(DomainCompiler.Compile(config));

    public KernelHostBuilder WithCompiledDomain(CompiledDomain compiledDomain)
    {
        ArgumentNullException.ThrowIfNull(compiledDomain);
        _compiledDomain = compiledDomain;
        return this;
    }

    public KernelHostBuilder WithSimulationOptions(SimulationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _simulationOptions = options;
        return this;
    }

    public KernelHostBuilder WithStartupSeeder(IKernelStartupSeeder startupSeeder)
    {
        ArgumentNullException.ThrowIfNull(startupSeeder);
        if (_domainStartupSeed is not null)
        {
            throw new InvalidOperationException("A domain startup seed is already configured.");
        }

        _startupSeeder = startupSeeder;
        return this;
    }

    public KernelHostBuilder WithStartupSeeder(
        int estimatedEventCount,
        Action<KernelStartupContext> seed)
    {
        if (_domainStartupSeed is not null)
        {
            throw new InvalidOperationException("A domain startup seed is already configured.");
        }

        _startupSeeder = new DelegateKernelStartupSeeder(estimatedEventCount, seed);
        return this;
    }

    public KernelHostBuilder WithDomainStartupSeed(DomainStartupSeed seed)
    {
        ArgumentNullException.ThrowIfNull(seed);
        if (_startupSeeder is not null)
        {
            throw new InvalidOperationException("A custom startup seeder is already configured.");
        }

        _domainStartupSeed = seed;
        return this;
    }

    public KernelHostBuilder WithSeedHeadroom(int seedHeadroom)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(seedHeadroom);
        _seedHeadroom = seedHeadroom;
        return this;
    }

    public KernelHostBuilder WithDriverConfig(RealtimeDriverConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _driverConfig = config;
        return this;
    }

    public KernelHostBuilder RegisterDomain(Action<DomainIdRegistry> register)
    {
        ArgumentNullException.ThrowIfNull(register);
        _domainRegistrations.Add(register);
        return this;
    }

    public KernelHostBuilder ConfigureSymbols(Action<KernelSymbolContext> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _symbolConfigurators.Add(configure);
        return this;
    }

    public KernelHostBuilder WithEntityNameSynthesizer(IProjectionEntityNameSynthesizer synthesizer)
    {
        ArgumentNullException.ThrowIfNull(synthesizer);
        _symbolConfigurators.Add(symbols => symbols.UseEntityNameSynthesizer(synthesizer));
        return this;
    }

    public KernelHostBuilder ConfigurePrograms(Action<KernelProgramConfigurationContext> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _programConfigurators.Add(configure);
        return this;
    }

    public KernelHostBuilder ConfigureKernel(Action<KernelConfigurationContext> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _kernelConfigurators.Add(configure);
        return this;
    }

    public KernelHostBuilder RegisterConsumers(Action<KernelHostConsumerContext> register)
    {
        ArgumentNullException.ThrowIfNull(register);
        _consumerRegistrations.Add(register);
        return this;
    }

    public KernelHostBuilder RegisterProjection(
        string name,
        Func<KernelHostConsumerContext, IKernelEventProjection> projectionFactory,
        int batchSize = 256)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(projectionFactory);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
        return RegisterConsumers(context =>
            context.RegisterProjection(name, projectionFactory(context), batchSize));
    }

    public KernelHost Build()
    {
        ValidateBuildState();
        EnsureSeedBurstFits();
        var domainIds = CreateDomainIds();
        var symbols = new ProjectionSymbolCatalog();
        var symbolContext = CreateSymbolContext(symbols);
        var simulationOptions = CreateSimulationOptions();
        var kernel = CreateKernel(simulationOptions, out var kernelWasLoaded);
        ConfigureKernel(kernel, domainIds, symbolContext, kernelWasLoaded);
        return CreateHost(kernel, domainIds, symbols, symbolContext, simulationOptions, kernelWasLoaded);
    }

    private void ValidateBuildState()
    {
        ValidateCompiledDomainState();
        ValidateStartupState();
    }

    private void ValidateCompiledDomainState()
    {
        if (_compiledDomain is not null && _domainIds is not null)
        {
            throw new InvalidOperationException("A compiled domain owns the domain ID catalog; do not also pass DomainIdRegistry.");
        }

        if (_compiledDomain is not null && _domainRegistrations.Count > 0)
        {
            throw new InvalidOperationException("A compiled domain owns domain registration; do not also register domain IDs manually.");
        }

        if (_compiledDomain is not null && _kernel is not null)
        {
            throw new InvalidOperationException("A compiled domain must configure runtime codes before the kernel is constructed.");
        }

        if (_domainStartupSeed is not null && _compiledDomain is null)
        {
            throw new InvalidOperationException("A domain startup seed requires a compiled domain.");
        }
    }

    private void ValidateStartupState()
    {
        if (_kernelState is not null && (_startupSeeder is not null || _domainStartupSeed is not null))
        {
            throw new InvalidOperationException("A loaded kernel state replaces startup seeding.");
        }
    }

    private DomainIdRegistry CreateDomainIds()
    {
        var domainIds = _compiledDomain?.Ids ?? _domainIds ?? new DomainIdRegistry();
        foreach (var register in _domainRegistrations)
        {
            register(domainIds);
        }

        return domainIds;
    }

    private KernelSymbolContext CreateSymbolContext(ProjectionSymbolCatalog symbols)
    {
        var symbolContext = new KernelSymbolContext(symbols);
        if (_compiledDomain is not null)
        {
            ApplyDomainSymbols(symbolContext, _compiledDomain.Symbols);
        }

        foreach (var configure in _symbolConfigurators)
        {
            configure(symbolContext);
        }

        return symbolContext;
    }

    private SimulationOptions CreateSimulationOptions()
    {
        return _compiledDomain is null
            ? _simulationOptions
            : _simulationOptions with
            {
                RuntimeCodes = ToKernelRuntimeCodes(_compiledDomain.RuntimeCodes),
            };
    }

    private Simulation CreateKernel(SimulationOptions simulationOptions, out bool kernelWasLoaded)
    {
        kernelWasLoaded = _kernelState is not null;
        return _kernel ??
            (kernelWasLoaded
                ? KernelSnapshot.LoadKernelState(new MemoryStream(_kernelState!), simulationOptions)
                : new Simulation(simulationOptions));
    }

    private void ConfigureKernel(
        Simulation kernel,
        DomainIdRegistry domainIds,
        KernelSymbolContext symbolContext,
        bool kernelWasLoaded)
    {
        var nextSequenceBeforeKernelConfiguration = kernel.Events.NextSequence;
        var kernelConfigurationContext = new KernelConfigurationContext(kernel, domainIds, symbolContext);
        var programConfig = new KernelProgramConfigurationContext();
        foreach (var configure in _programConfigurators)
        {
            configure(programConfig);
        }

        if (_compiledDomain is not null && !kernelWasLoaded)
        {
            KernelDataInstaller.Install(kernelConfigurationContext, _compiledDomain.KernelData);
            KernelProgramInstaller.Install(kernelConfigurationContext, _compiledDomain.Programs, programConfig);
        }

        foreach (var programOverride in programConfig.Overrides)
        {
            kernelConfigurationContext.SetProgramEnabled(programOverride.Key, programOverride.Value);
        }

        foreach (var configure in _kernelConfigurators)
        {
            configure(kernelConfigurationContext);
        }

        if (_domainStartupSeed is not null && !kernelWasLoaded)
        {
            DomainStartupSeedInstaller.ConfigureKernel(kernelConfigurationContext, _domainStartupSeed);
        }

        if (kernel.Events.NextSequence != nextSequenceBeforeKernelConfiguration)
        {
            throw new InvalidOperationException("Kernel configuration must not emit events.");
        }
    }

    private KernelHost CreateHost(
        Simulation kernel,
        DomainIdRegistry domainIds,
        ProjectionSymbolCatalog symbols,
        KernelSymbolContext symbolContext,
        SimulationOptions simulationOptions,
        bool kernelWasLoaded)
    {
        KernelConsumerRegistry? consumers = null;
        KernelCommandCompletionPump? commandCompletions = null;
        KernelDriverTimeController? time = null;

        try
        {
            consumers = new KernelConsumerRegistry(kernel.Events);
            var completionTable = new KernelCommandCompletionTable(simulationOptions.CommandCapacity);
            var commands = new KernelCommandSubmitter(kernel.Commands, completionTable);
            commandCompletions = new KernelCommandCompletionPump(kernel.Events, completionTable);

            RegisterConsumers(domainIds, symbols, kernel, consumers);
            var seedEventCount = SeedKernelStartup(kernel, domainIds, symbolContext, kernelWasLoaded);
            SealStartupConsumers(consumers, commandCompletions);

            time = new KernelDriverTimeController(kernel, _driverConfig);
            var host = new KernelHost(
                kernel,
                domainIds,
                symbols,
                consumers,
                time,
                commands,
                commandCompletions,
                seedEventCount,
                _compiledDomain);
            consumers = null;
            commandCompletions = null;
            time = null;
            return host;
        }
        finally
        {
            time?.Dispose();
            commandCompletions?.Dispose();
            consumers?.Dispose();
        }
    }

    private void RegisterConsumers(
        DomainIdRegistry domainIds,
        ProjectionSymbolCatalog symbols,
        Simulation kernel,
        KernelConsumerRegistry consumers)
    {
        var consumerContext = new KernelHostConsumerContext(domainIds, kernel.Events, symbols, consumers);
        foreach (var register in _consumerRegistrations)
        {
            register(consumerContext);
        }
    }

    private int SeedKernelStartup(
        Simulation kernel,
        DomainIdRegistry domainIds,
        KernelSymbolContext symbolContext,
        bool kernelWasLoaded)
    {
        var hasStartupSeed = !kernelWasLoaded && (_startupSeeder is not null || _domainStartupSeed is not null);
        if (hasStartupSeed && kernel.Events.NextSequence != 0)
        {
            throw new InvalidOperationException("Kernel startup consumers must be registered before seed events are emitted.");
        }

        if (!hasStartupSeed)
        {
            return kernelWasLoaded ? checked((int)kernel.Events.NextSequence) : 0;
        }

        return SeedFreshKernel(kernel, domainIds, symbolContext);
    }

    private int SeedFreshKernel(
        Simulation kernel,
        DomainIdRegistry domainIds,
        KernelSymbolContext symbolContext)
    {
        var nextSequenceBeforeSeed = kernel.Events.NextSequence;
        var startupContext = new KernelStartupContext(kernel, domainIds, symbolContext);
        if (_domainStartupSeed is not null)
        {
            DomainStartupSeedInstaller.Install(startupContext, _domainStartupSeed);
        }
        else
        {
            _startupSeeder!.Seed(startupContext);
        }

        return checked((int)(kernel.Events.NextSequence - nextSequenceBeforeSeed));
    }

    private static void SealStartupConsumers(
        KernelConsumerRegistry consumers,
        KernelCommandCompletionPump commandCompletions)
    {
        commandCompletions.DrainAvailable();
        consumers.DrainAll();
        consumers.ThrowIfAnyDropped();
        consumers.Seal();
    }

    private static KernelRuntimeCodes ToKernelRuntimeCodes(DomainRuntimeCodes codes) =>
        new(
            codes.RelationshipCreatedCodeId.Value,
            codes.RelationshipCulledCodeId.Value,
            codes.RelationshipStrengthChangedCodeId.Value,
            codes.GambitSourceCodeId.Value,
            codes.UpkeepSourceCodeId.Value,
            codes.StartupSourceCodeId.Value,
            codes.RelationshipSourceCodeId.Value,
            codes.GeneratorSourceCodeId.Value,
            codes.MapActivitySourceCodeId.Value,
            codes.MapSignalsSourceCodeId.Value,
            codes.LowAltitudeVerbSourceCodeId.Value,
            codes.EffectsSourceCodeId.Value);

    private static void ApplyDomainSymbols(
        KernelSymbolContext context,
        IReadOnlyList<DomainSymbolSeed> symbols)
    {
        foreach (var symbol in symbols)
        {
            context.RegisterSymbol(
                ToProjectionSymbolKind(symbol.Kind),
                symbol.Id,
                symbol.StableName,
                symbol.DisplayName,
                symbol.Description,
                symbol.IconKey,
                symbol.ColorKey);
        }
    }

    private static ProjectionSymbolKind ToProjectionSymbolKind(DomainSymbolKind kind)
    {
        if (TryCoreProjectionSymbolKind(kind, out var symbolKind) ||
            TryRuntimeProjectionSymbolKind(kind, out symbolKind) ||
            TryNarrativeProjectionSymbolKind(kind, out symbolKind))
        {
            return symbolKind;
        }

        throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
    }

    private static bool TryCoreProjectionSymbolKind(DomainSymbolKind kind, out ProjectionSymbolKind symbolKind)
    {
        switch (kind)
        {
            case DomainSymbolKind.Kind:
                symbolKind = ProjectionSymbolKind.Kind;
                return true;
            case DomainSymbolKind.Subtype:
                symbolKind = ProjectionSymbolKind.Subtype;
                return true;
            case DomainSymbolKind.Culture:
                symbolKind = ProjectionSymbolKind.Culture;
                return true;
            case DomainSymbolKind.Attribute:
                symbolKind = ProjectionSymbolKind.Attribute;
                return true;
            case DomainSymbolKind.Trait:
                symbolKind = ProjectionSymbolKind.Trait;
                return true;
            case DomainSymbolKind.StructuralFact:
                symbolKind = ProjectionSymbolKind.StructuralFact;
                return true;
            case DomainSymbolKind.Terrain:
                symbolKind = ProjectionSymbolKind.Terrain;
                return true;
            default:
                symbolKind = default;
                return false;
        }
    }

    private static bool TryRuntimeProjectionSymbolKind(DomainSymbolKind kind, out ProjectionSymbolKind symbolKind)
    {
        switch (kind)
        {
            case DomainSymbolKind.Verb:
                symbolKind = ProjectionSymbolKind.Verb;
                return true;
            case DomainSymbolKind.Generator:
                symbolKind = ProjectionSymbolKind.Generator;
                return true;
            case DomainSymbolKind.Gambit:
                symbolKind = ProjectionSymbolKind.Gambit;
                return true;
            case DomainSymbolKind.Goal:
                symbolKind = ProjectionSymbolKind.Goal;
                return true;
            default:
                symbolKind = default;
                return false;
        }
    }

    private static bool TryNarrativeProjectionSymbolKind(DomainSymbolKind kind, out ProjectionSymbolKind symbolKind)
    {
        switch (kind)
        {
            case DomainSymbolKind.RelationshipKind:
                symbolKind = ProjectionSymbolKind.RelationshipKind;
                return true;
            case DomainSymbolKind.EventCode:
                symbolKind = ProjectionSymbolKind.EventCode;
                return true;
            case DomainSymbolKind.ReasonCode:
                symbolKind = ProjectionSymbolKind.ReasonCode;
                return true;
            case DomainSymbolKind.SourceCode:
                symbolKind = ProjectionSymbolKind.SourceCode;
                return true;
            default:
                symbolKind = default;
                return false;
        }
    }

    private void EnsureSeedBurstFits()
    {
        var seedEventCount = _domainStartupSeed is not null
            ? DomainStartupSeedInstaller.EstimateEventCount(_domainStartupSeed, _compiledDomain!)
            : _startupSeeder?.EstimatedEventCount ?? 0;
        var requiredCapacity = checked(seedEventCount + _seedHeadroom);
        if (_simulationOptions.EventCapacity < requiredCapacity)
        {
            throw new InvalidOperationException(
                $"Kernel event capacity {_simulationOptions.EventCapacity} cannot hold startup seed burst " +
                $"{seedEventCount} plus headroom {_seedHeadroom}.");
        }
    }
}
