namespace Engine.Kernel;

public readonly record struct KernelExecutionContext(
    int ProgramId,
    int SourceCodeId);

public readonly record struct KernelExecutionScope : global::System.IDisposable
{
    private readonly Simulation? _simulation;
    private readonly KernelExecutionContext _previous;
    private readonly int _previousDepth;

    internal KernelExecutionScope(Simulation simulation, in KernelExecutionContext previous, int previousDepth)
    {
        _simulation = simulation;
        _previous = previous;
        _previousDepth = previousDepth;
    }

    public void Dispose()
    {
        if (_simulation is not null)
        {
            _simulation.RestoreExecutionContext(_previous, _previousDepth);
        }
    }
}
