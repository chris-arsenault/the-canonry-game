namespace Engine.Kernel;

internal static class ProcessProgramSnapshot
{
    public static void Write(global::System.IO.BinaryWriter writer, ProcessProgramRuntime program)
    {
        KernelProgramSnapshot.WriteData(writer, program.Data);
        KernelProgramSnapshot.WriteState(writer, program.State);
    }

    public static void Read(global::System.IO.BinaryReader reader, ProcessProgramRuntime program)
    {
        KernelProgramSnapshot.ReadData(reader, program.Data);
        KernelProgramSnapshot.ReadState(reader, program.State);
    }
}
