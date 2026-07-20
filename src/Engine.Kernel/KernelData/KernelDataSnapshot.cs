namespace Engine.Kernel;

internal sealed partial class KernelDataSet
{
    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        Entities.WriteSnapshot(writer);
        Terrain.WriteSnapshot(writer);
        Cultures.WriteSnapshot(writer);
        PhysicalGeography.WriteSnapshot(writer);
        PlacementTaste.WriteSnapshot(writer);
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        Entities.ReadSnapshot(reader);
        Terrain.ReadSnapshot(reader);
        Cultures.ReadSnapshot(reader);
        PhysicalGeography.ReadSnapshot(reader);
        PlacementTaste.ReadSnapshot(reader);
    }
}

internal sealed partial class KernelEntityData
{
    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        KernelSnapshotBinary.WriteObject(writer, this);
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        KernelSnapshotBinary.ReadObject(reader, this);
    }
}

internal sealed partial class KernelTerrainData
{
    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        KernelSnapshotBinary.WriteObject(writer, this);
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        KernelSnapshotBinary.ReadObject(reader, this);
    }
}

internal sealed partial class KernelCultureData
{
    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        KernelSnapshotBinary.WriteObject(writer, this);
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        KernelSnapshotBinary.ReadObject(reader, this);
    }
}
