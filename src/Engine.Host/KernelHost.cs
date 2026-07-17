namespace Engine.Host;

using Engine.Domain;
using Engine.Domain.Compilation;
using Engine.Domain.Startup;
using Engine.Eventing;
using Engine.Kernel;
using Engine.Kernel.Driver;
using Engine.Projection;

public sealed partial class KernelHost : IDisposable
{
    private readonly Simulation _kernel;
    private readonly KernelCommandCompletionPump _commandCompletions;
    private readonly CultureBoundaryDirtyCell[] _cultureBoundaryRefreshCells;
    private readonly IReadOnlyList<DomainKnowledgeSite> _knowledgeSites;
    private bool _disposed;

    internal KernelHost(
        Simulation kernel,
        DomainIdRegistry domainIds,
        ProjectionSymbolCatalog symbols,
        KernelConsumerRegistry consumers,
        IKernelDriverTimeControl time,
        ICommandSubmitter commands,
        KernelCommandCompletionPump commandCompletions,
        int seedEventCount,
        CompiledDomain? compiledDomain = null,
        IReadOnlyList<DomainKnowledgeSite>? knowledgeSites = null)
    {
        _kernel = kernel;
        _cultureBoundaryRefreshCells = new CultureBoundaryDirtyCell[kernel.Options.MapCellCapacity];
        _knowledgeSites = knowledgeSites ?? [];
        Events = kernel.Events;
        DomainIds = domainIds;
        Symbols = symbols;
        Consumers = consumers;
        Time = time;
        Commands = commands;
        _commandCompletions = commandCompletions;
        SeedEventCount = seedEventCount;
        CompiledDomain = compiledDomain;
    }

    public IKernelEventReader Events { get; }

    public DomainIdRegistry DomainIds { get; }

    public CompiledDomain? CompiledDomain { get; }

    public ProjectionSymbolCatalog Symbols { get; }

    internal KernelConsumerRegistry Consumers { get; }

    public IKernelDriverTimeControl Time { get; }

    public ICommandSubmitter Commands { get; }

    public int SeedEventCount { get; }

    public bool BootstrapCompleted { get; } = true;

    public int CurrentTick => _kernel.CurrentTick;

    public long CurrentVersion => _kernel.CurrentVersion;

    public bool ProgramIsEnabled(int programId) =>
        _kernel.ProgramIsEnabled(programId);

    public void SaveKernelState(Stream destination)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        KernelSnapshot.SaveKernelState(_kernel, destination);
    }

    public void DrainConsumers()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _commandCompletions.DrainAvailable();
        Consumers.DrainAll();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Time.Dispose();
        _commandCompletions.Dispose();
        Consumers.Dispose();
    }
}
