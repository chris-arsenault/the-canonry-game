namespace Engine.Kernel;

internal static class MapSignalsProgramSnapshot
{
    public static void Write(global::System.IO.BinaryWriter writer, MapSignalsProgramRuntime program)
    {
        KernelProgramSnapshot.WriteData(writer, program.Data);
        KernelProgramSnapshot.WriteState(writer, program.State);
    }

    public static void Read(global::System.IO.BinaryReader reader, MapSignalsProgramRuntime program)
    {
        KernelProgramSnapshot.ReadData(reader, program.Data);
        KernelProgramSnapshot.ReadState(reader, program.State);
    }
}
