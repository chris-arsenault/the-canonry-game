namespace Engine.Host;

/// <summary>
/// Startup hook invoked by the host after consumers have subscribed and before
/// tick advancement begins. Implementations receive the host-owned startup
/// surface rather than direct kernel access.
/// </summary>
public interface IKernelStartupSeeder
{
    int EstimatedEventCount { get; }

    void Seed(KernelStartupContext context);
}

public sealed class DelegateKernelStartupSeeder : IKernelStartupSeeder
{
    private readonly Action<KernelStartupContext> _seed;

    public DelegateKernelStartupSeeder(int estimatedEventCount, Action<KernelStartupContext> seed)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(estimatedEventCount);
        ArgumentNullException.ThrowIfNull(seed);

        EstimatedEventCount = estimatedEventCount;
        _seed = seed;
    }

    public int EstimatedEventCount { get; }

    public void Seed(KernelStartupContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _seed(context);
    }
}
