namespace Engine.Kernel;

public interface IKernel
{
    int CurrentTick { get; }

    long CurrentVersion { get; }

    int AdvanceTicks(int count, in AdvanceOptions options = default);
}
