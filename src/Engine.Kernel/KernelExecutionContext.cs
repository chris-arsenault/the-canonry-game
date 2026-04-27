namespace Engine.Kernel;

public readonly record struct KernelExecutionContext(
    int ProgramId,
    int SourceCodeId,
    int SubCode1 = 0,
    int SubCode2 = 0,
    int SubCode3 = 0,
    int SubCode4 = 0);

public readonly record struct KernelExecutionScope : global::System.IDisposable
{
    private readonly Simulation? _simulation;
    private readonly KernelExecutionContext _previous;

    internal KernelExecutionScope(Simulation simulation, in KernelExecutionContext previous)
    {
        _simulation = simulation;
        _previous = previous;
    }

    public void Dispose()
    {
        if (_simulation is not null)
        {
            _simulation.RestoreExecutionContext(_previous);
        }
    }
}
