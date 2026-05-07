namespace Engine.Kernel;

internal sealed class MovementProgramScratch
{
    internal readonly int[] PathOpenSet;
    internal EpochStampSet PathOpen;
    internal EpochStampSet PathClosed;
    internal EpochStampedArray<int> PathCameFrom;
    internal EpochStampedArray<float> PathGScore;
    internal EpochStampedArray<float> PathFScore;

    public MovementProgramScratch(int pathCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pathCapacity);
        PathOpenSet = new int[pathCapacity];
        PathOpen = new EpochStampSet(pathCapacity);
        PathClosed = new EpochStampSet(pathCapacity);
        PathCameFrom = new EpochStampedArray<int>(pathCapacity);
        PathGScore = new EpochStampedArray<float>(pathCapacity);
        PathFScore = new EpochStampedArray<float>(pathCapacity);
    }

    internal int PathCapacity => PathOpenSet.Length;
}
