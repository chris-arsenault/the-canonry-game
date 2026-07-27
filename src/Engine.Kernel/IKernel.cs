namespace Engine.Kernel;

internal interface IKernel
{
    int CurrentTick { get; }

    int PublishedLowWatermark { get; }

    int PublishedHighWatermark { get; }

    long CurrentVersion { get; }

    int AdvanceTicks(int count, in AdvanceOptions options = default);

    int ConvergeAt(
        int tick,
        global::System.Threading.CancellationToken cancellationToken = default);
}
