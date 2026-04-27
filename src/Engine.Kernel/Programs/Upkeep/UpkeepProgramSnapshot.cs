namespace Engine.Kernel;

internal static class UpkeepProgramSnapshot
{
    public static void Write(global::System.IO.BinaryWriter writer, UpkeepProgramRuntime program)
    {
        KernelProgramSnapshot.WriteData(writer, program.Data);
        KernelProgramSnapshot.WriteState(writer, program.State);
    }

    public static void Read(global::System.IO.BinaryReader reader, UpkeepProgramRuntime program)
    {
        KernelProgramSnapshot.ReadData(reader, program.Data);
        KernelProgramSnapshot.ReadState(reader, program.State);
    }
}
