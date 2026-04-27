namespace Engine.Kernel;

internal static class PressureProgramSnapshot
{
    public static void Write(global::System.IO.BinaryWriter writer, PressureProgramRuntime program)
    {
        KernelProgramSnapshot.WriteData(writer, program.Data);
        KernelProgramSnapshot.WriteState(writer, program.State);
    }

    public static void Read(global::System.IO.BinaryReader reader, PressureProgramRuntime program)
    {
        KernelProgramSnapshot.ReadData(reader, program.Data);
        KernelProgramSnapshot.ReadState(reader, program.State);
    }
}
