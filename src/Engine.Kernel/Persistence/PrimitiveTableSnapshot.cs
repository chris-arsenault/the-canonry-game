namespace Engine.Kernel;

internal sealed partial class PrimitiveIntTable
{
    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        writer.Write(_count);
        KernelSnapshotBinary.Write(writer, _firstKeys);
        KernelSnapshotBinary.Write(writer, _secondKeys);
        KernelSnapshotBinary.Write(writer, _values);
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        _ = reader.ReadInt32();
        KernelSnapshotBinary.Read(reader, _firstKeys);
        KernelSnapshotBinary.Read(reader, _secondKeys);
        KernelSnapshotBinary.Read(reader, _values);
        RebuildOccupiedSlots();
    }

    internal void ForEach(global::System.Action<int, int, int> visitor)
    {
        global::System.ArgumentNullException.ThrowIfNull(visitor);
        for (var i = 0; i < _count; i++)
        {
            var slot = _occupiedSlots[i];
            visitor(_firstKeys[slot], _secondKeys[slot], _values[slot]);
        }
    }
}

internal sealed partial class PrimitiveFloatTable
{
    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        writer.Write(_count);
        KernelSnapshotBinary.Write(writer, _firstKeys);
        KernelSnapshotBinary.Write(writer, _secondKeys);
        KernelSnapshotBinary.Write(writer, _values);
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        _ = reader.ReadInt32();
        KernelSnapshotBinary.Read(reader, _firstKeys);
        KernelSnapshotBinary.Read(reader, _secondKeys);
        KernelSnapshotBinary.Read(reader, _values);
        RebuildOccupiedSlots();
    }

    internal void ForEach(global::System.Action<int, int, float> visitor)
    {
        global::System.ArgumentNullException.ThrowIfNull(visitor);
        for (var i = 0; i < _count; i++)
        {
            var slot = _occupiedSlots[i];
            visitor(_firstKeys[slot], _secondKeys[slot], _values[slot]);
        }
    }
}
