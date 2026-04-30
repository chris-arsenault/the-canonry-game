namespace Engine.Kernel;

internal static class MapActivityProgramSnapshot
{
    public static void Write(global::System.IO.BinaryWriter writer, MapActivityProgramRuntime program)
    {
        KernelProgramSnapshot.WriteData(writer, program.Data);
        KernelProgramSnapshot.WriteState(writer, program.State);
    }

    public static void Read(global::System.IO.BinaryReader reader, MapActivityProgramRuntime program)
    {
        KernelProgramSnapshot.ReadData(reader, program.Data);
        KernelProgramSnapshot.ReadState(reader, program.State);
    }
}

