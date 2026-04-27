namespace Engine.Host;

using Engine.Eventing;
using Engine.Domain;
using Engine.Domain.Compilation;
using Engine.Kernel;
using Engine.Kernel.Driver;
using Engine.Projection;

public sealed class KernelHost : IDisposable
{
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
        CompiledDomain? compiledDomain = null)
    {
        Kernel = kernel;
        Events = kernel.Events;
        DomainIds = domainIds;
        Symbols = symbols;
        Consumers = consumers;
        Time = time;
        Commands = commands;
        CommandCompletions = commandCompletions;
        SeedEventCount = seedEventCount;
        CompiledDomain = compiledDomain;
    }

    public Simulation Kernel { get; }

    public KernelEventLog Events { get; }

    public DomainIdRegistry DomainIds { get; }

    public CompiledDomain? CompiledDomain { get; }

    public ProjectionSymbolCatalog Symbols { get; }

    public KernelConsumerRegistry Consumers { get; }

    public IKernelDriverTimeControl Time { get; }

    public ICommandSubmitter Commands { get; }

    public KernelCommandCompletionPump CommandCompletions { get; }

    public int SeedEventCount { get; }

    public bool BootstrapCompleted { get; } = true;

    public void SetProgramEnabled(int programId, bool enabled) =>
        Kernel.SetProgramEnabled(programId, enabled);

    public bool ProgramIsEnabled(int programId) =>
        Kernel.ProgramIsEnabled(programId);

    public void SaveKernelState(Stream destination)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        KernelSnapshot.SaveKernelState(Kernel, destination);
    }

    public void DrainConsumers()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        CommandCompletions.DrainAvailable();
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
        CommandCompletions.Dispose();
        Consumers.Dispose();
    }
}
