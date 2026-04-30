namespace Engine.Kernel;

internal static class LowAltitudeVerbProgramSnapshot
{
    public static void Write(global::System.IO.BinaryWriter writer, LowAltitudeVerbProgramRuntime program)
    {
        KernelProgramSnapshot.WriteData(writer, program.Data);
        KernelProgramSnapshot.WriteState(writer, program.State);
    }

    public static void Read(global::System.IO.BinaryReader reader, LowAltitudeVerbProgramRuntime program)
    {
        KernelProgramSnapshot.ReadData(reader, program.Data);
        KernelProgramSnapshot.ReadState(reader, program.State);
    }
}
