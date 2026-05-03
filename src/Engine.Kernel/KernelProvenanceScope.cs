namespace Engine.Kernel;

internal readonly record struct KernelProvenanceScope : global::System.IDisposable
{
    private readonly Simulation? _simulation;
    private readonly KernelExecutionContext _previous;
    private readonly int _previousDepth;
    private readonly int _invocationId;

    internal KernelProvenanceScope(
        Simulation simulation,
        in KernelExecutionContext previous,
        int previousDepth,
        int invocationId)
    {
        _simulation = simulation;
        _previous = previous;
        _previousDepth = previousDepth;
        _invocationId = invocationId;
    }

    public void Dispose()
    {
        if (_simulation is not null)
        {
            _simulation.CompleteProvenanceInvocation(_invocationId);
            _simulation.RestoreExecutionContext(_previous, _previousDepth);
        }
    }
}
