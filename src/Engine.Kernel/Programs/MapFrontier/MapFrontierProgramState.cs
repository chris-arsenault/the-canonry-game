namespace Engine.Kernel;

using Engine.Constants;

/// <summary>
/// The record of ground the frontier has opened. Startup-heart cells never
/// enter this fixed-capacity list, so ordinary re-freeze can revisit only
/// earned frontier ground. A persistent cursor rotates reclaim work across
/// the full history without rescanning the map or favoring one direction.
/// </summary>
internal sealed class MapFrontierProgramState : IKernelProgramState
{
    internal readonly int[] ThawedQ;
    internal readonly int[] ThawedR;
    internal int ThawedCount;
    internal int ReclaimCursor;
    internal int TotalThawed;
    internal int TotalReclaimed;
    internal int TotalSemanticOpened;
    internal int NextPhysicalCultureId = 1;
    internal int NextSemanticCultureId = 1;

    public MapFrontierProgramState(int capacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ThawedQ = new int[capacity];
        ThawedR = new int[capacity];
    }

    public int ProgramId => KernelProgramCodes.MapFrontier;

    public int SchemaVersion => 3;

    internal int Capacity => ThawedQ.Length;

    internal void RecordThawed(int q, int r)
    {
        if (ThawedCount >= ThawedQ.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        ThawedQ[ThawedCount] = q;
        ThawedR[ThawedCount] = r;
        ThawedCount++;
        TotalThawed++;
    }
}
