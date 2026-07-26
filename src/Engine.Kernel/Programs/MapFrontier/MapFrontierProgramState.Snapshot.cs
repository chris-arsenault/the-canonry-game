namespace Engine.Kernel;

internal sealed partial class MapFrontierProgramState : IActiveKernelProgramStateSnapshot
{
    public void WriteActiveSnapshot(global::System.IO.BinaryWriter writer)
    {
        writer.Write(ThawedCount);
        for (var index = 0; index < ThawedCount; index++)
        {
            writer.Write(ThawedQ[index]);
            writer.Write(ThawedR[index]);
        }

        writer.Write(ReclaimCursor);
        writer.Write(IceRetreatCursor);
        writer.Write(SeaIceCursor);
        writer.Write(TotalThawed);
        writer.Write(TotalReclaimed);
        writer.Write(TotalSemanticOpened);
        writer.Write(NextPhysicalCultureId);
        writer.Write(NextSemanticCultureId);
    }

    public void ReadActiveSnapshot(global::System.IO.BinaryReader reader)
    {
        ThawedCount = reader.ReadInt32();
        if ((uint)ThawedCount > (uint)ThawedQ.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var index = 0; index < ThawedCount; index++)
        {
            ThawedQ[index] = reader.ReadInt32();
            ThawedR[index] = reader.ReadInt32();
        }

        ReclaimCursor = reader.ReadInt32();
        IceRetreatCursor = reader.ReadInt32();
        SeaIceCursor = reader.ReadInt32();
        TotalThawed = reader.ReadInt32();
        TotalReclaimed = reader.ReadInt32();
        TotalSemanticOpened = reader.ReadInt32();
        NextPhysicalCultureId = reader.ReadInt32();
        NextSemanticCultureId = reader.ReadInt32();
        if (SnapshotScalarsAreInvalid())
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private bool SnapshotScalarsAreInvalid()
    {
        return ReclaimCursor < 0 ||
            IceRetreatCursor < 0 ||
            SeaIceCursor < 0 ||
            TotalThawed < 0 ||
            TotalReclaimed < 0 ||
            TotalSemanticOpened < 0 ||
            NextPhysicalCultureId <= 0 ||
            NextSemanticCultureId <= 0;
    }
}
