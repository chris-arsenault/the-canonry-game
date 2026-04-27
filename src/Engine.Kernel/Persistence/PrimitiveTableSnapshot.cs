namespace Engine.Kernel;

public sealed partial class PrimitiveIntTable
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
        _count = reader.ReadInt32();
        KernelSnapshotBinary.Read(reader, _firstKeys);
        KernelSnapshotBinary.Read(reader, _secondKeys);
        KernelSnapshotBinary.Read(reader, _values);
    }

    internal void ForEach(global::System.Action<int, int, int> visitor)
    {
        global::System.ArgumentNullException.ThrowIfNull(visitor);
        for (var i = 0; i < _firstKeys.Length; i++)
        {
            if (_firstKeys[i] != 0)
            {
                visitor(_firstKeys[i], _secondKeys[i], _values[i]);
            }
        }
    }
}

public sealed partial class PrimitiveFloatTable
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
        _count = reader.ReadInt32();
        KernelSnapshotBinary.Read(reader, _firstKeys);
        KernelSnapshotBinary.Read(reader, _secondKeys);
        KernelSnapshotBinary.Read(reader, _values);
    }

    internal void ForEach(global::System.Action<int, int, float> visitor)
    {
        global::System.ArgumentNullException.ThrowIfNull(visitor);
        for (var i = 0; i < _firstKeys.Length; i++)
        {
            if (_firstKeys[i] != 0)
            {
                visitor(_firstKeys[i], _secondKeys[i], _values[i]);
            }
        }
    }
}
