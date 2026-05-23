namespace Engine.Kernel;

internal static class CultureBoundaryProgramSnapshot
{
    public static void Write(global::System.IO.BinaryWriter writer, CultureBoundaryProgramRuntime program)
    {
        KernelProgramSnapshot.WriteData(writer, program.Data);
        KernelProgramSnapshot.WriteState(writer, program.State);
    }

    public static void Read(global::System.IO.BinaryReader reader, CultureBoundaryProgramRuntime program)
    {
        KernelProgramSnapshot.ReadData(reader, program.Data);
        KernelProgramSnapshot.ReadState(reader, program.State);
        program.Invalidate();
    }
}
