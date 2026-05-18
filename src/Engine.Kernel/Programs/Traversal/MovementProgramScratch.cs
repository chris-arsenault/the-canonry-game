namespace Engine.Kernel;

internal sealed class MovementProgramScratch
{
    internal readonly int[] PathOpenSet;
    internal EpochStampSet PathOpen;
    internal EpochStampSet PathClosed;
    internal EpochStampedArray<int> PathCameFrom;
    internal EpochStampedArray<float> PathGScore;
    internal EpochStampedArray<float> PathFScore;
    internal readonly int[] SegmentOpenSet;
    internal EpochStampSet SegmentOpen;
    internal EpochStampSet SegmentClosed;
    internal EpochStampedArray<int> SegmentCameFrom;
    internal EpochStampedArray<float> SegmentGScore;
    internal EpochStampedArray<float> SegmentFScore;
    internal readonly int[] SegmentEdgeCachePlaneCodes;
    internal readonly int[] SegmentEdgeCacheProfileIds;
    internal readonly int[] SegmentEdgeCacheLevelSizes;
    internal readonly int[] SegmentEdgeCacheFromSegments;
    internal readonly int[] SegmentEdgeCacheToSegments;
    internal readonly int[] SegmentEdgeCacheTopologyEpochs;
    internal readonly int[] SegmentEdgeCacheRoadEpochs;
    internal readonly float[] SegmentEdgeCacheCosts;
    internal readonly bool[] SegmentEdgeCacheFound;
    internal readonly bool[] SegmentEdgeCacheOccupied;

    public MovementProgramScratch(int pathCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pathCapacity);
        PathOpenSet = new int[pathCapacity];
        PathOpen = new EpochStampSet(pathCapacity);
        PathClosed = new EpochStampSet(pathCapacity);
        PathCameFrom = new EpochStampedArray<int>(pathCapacity);
        PathGScore = new EpochStampedArray<float>(pathCapacity);
        PathFScore = new EpochStampedArray<float>(pathCapacity);
        SegmentOpenSet = new int[pathCapacity];
        SegmentOpen = new EpochStampSet(pathCapacity);
        SegmentClosed = new EpochStampSet(pathCapacity);
        SegmentCameFrom = new EpochStampedArray<int>(pathCapacity);
        SegmentGScore = new EpochStampedArray<float>(pathCapacity);
        SegmentFScore = new EpochStampedArray<float>(pathCapacity);
        SegmentEdgeCachePlaneCodes = new int[pathCapacity];
        SegmentEdgeCacheProfileIds = new int[pathCapacity];
        SegmentEdgeCacheLevelSizes = new int[pathCapacity];
        SegmentEdgeCacheFromSegments = new int[pathCapacity];
        SegmentEdgeCacheToSegments = new int[pathCapacity];
        SegmentEdgeCacheTopologyEpochs = new int[pathCapacity];
        SegmentEdgeCacheRoadEpochs = new int[pathCapacity];
        SegmentEdgeCacheCosts = new float[pathCapacity];
        SegmentEdgeCacheFound = new bool[pathCapacity];
        SegmentEdgeCacheOccupied = new bool[pathCapacity];
    }

    internal int PathCapacity => PathOpenSet.Length;

    internal int SegmentCapacity => SegmentOpenSet.Length;
}
