namespace Engine.Kernel;

public sealed partial class RandomSource
{
    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        writer.Write(_state);
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        _state = reader.ReadUInt32();
    }
}
