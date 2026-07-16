namespace Engine.Kernel;

using Engine.Constants;

/// <summary>
/// The record of ground the world has opened: a ring of thawed cell
/// coordinates the re-freeze pass revisits, plus running counts for
/// telemetry. Fixed capacity; when full, the oldest thawed cells (the
/// innermost, least likely to be reclaimed) roll off the re-freeze watch.
/// </summary>
internal sealed class MapFrontierProgramState : IKernelProgramState
{
    internal readonly int[] ThawedQ;
    internal readonly int[] ThawedR;
    internal int ThawedNext;
    internal int ThawedCount;
    internal int TotalThawed;
    internal int TotalReclaimed;
    internal int TotalSemanticOpened;

    public MapFrontierProgramState(int capacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ThawedQ = new int[capacity];
        ThawedR = new int[capacity];
    }

    public int ProgramId => KernelProgramCodes.MapFrontier;

    public int SchemaVersion => 1;

    internal int Capacity => ThawedQ.Length;

    internal void RecordThawed(int q, int r)
    {
        ThawedQ[ThawedNext] = q;
        ThawedR[ThawedNext] = r;
        ThawedNext = (ThawedNext + 1) % ThawedQ.Length;
        if (ThawedCount < ThawedQ.Length)
        {
            ThawedCount++;
        }

        TotalThawed++;
    }
}
