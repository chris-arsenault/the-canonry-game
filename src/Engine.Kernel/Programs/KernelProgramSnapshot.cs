namespace Engine.Kernel;

internal interface IActiveKernelProgramStateSnapshot
{
    void WriteActiveSnapshot(global::System.IO.BinaryWriter writer);

    void ReadActiveSnapshot(global::System.IO.BinaryReader reader);
}

internal static class KernelProgramSnapshot
{
    public static void WriteData(global::System.IO.BinaryWriter writer, IKernelProgramData data)
    {
        writer.Write(data.ProgramId);
        writer.Write(data.SchemaVersion);
        KernelSnapshotBinary.WriteObject(writer, data);
    }

    public static void ReadData(global::System.IO.BinaryReader reader, IKernelProgramData data)
    {
        if (reader.ReadInt32() != data.ProgramId || reader.ReadInt32() != data.SchemaVersion)
        {
            throw new global::System.InvalidOperationException();
        }

        KernelSnapshotBinary.ReadObject(reader, data);
    }

    public static void WriteState(global::System.IO.BinaryWriter writer, IKernelProgramState state)
    {
        writer.Write(state.ProgramId);
        writer.Write(state.SchemaVersion);
        if (state is IActiveKernelProgramStateSnapshot activeState)
        {
            activeState.WriteActiveSnapshot(writer);
            return;
        }

        KernelSnapshotBinary.WriteObject(writer, state);
    }

    public static void ReadState(global::System.IO.BinaryReader reader, IKernelProgramState state)
    {
        if (reader.ReadInt32() != state.ProgramId || reader.ReadInt32() != state.SchemaVersion)
        {
            throw new global::System.InvalidOperationException();
        }

        if (state is IActiveKernelProgramStateSnapshot activeState)
        {
            activeState.ReadActiveSnapshot(reader);
            return;
        }

        KernelSnapshotBinary.ReadObject(reader, state);
    }
}
