namespace Engine.Kernel.Programs;

public sealed partial class KernelProgramRegistry
{
    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        writer.Write(_count);
        for (var i = 0; i < _count; i++)
        {
            writer.Write(_programIds[i]);
            writer.Write(_enabled[i]);
        }
    }

    internal void ReadSnapshot(Engine.Kernel.Simulation simulation, global::System.IO.BinaryReader reader)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        var count = reader.ReadInt32();
        for (var i = 0; i < count; i++)
        {
            simulation.SetProgramEnabled(reader.ReadInt32(), reader.ReadBoolean());
        }
    }
}
