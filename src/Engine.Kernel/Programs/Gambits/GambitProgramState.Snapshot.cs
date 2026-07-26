namespace Engine.Kernel;

internal sealed partial class GambitProgramState : IActiveKernelProgramStateSnapshot
{
    public void WriteActiveSnapshot(global::System.IO.BinaryWriter writer)
    {
        writer.Write(GambitEnabled.Length);
        for (var gambitId = 0; gambitId < GambitEnabled.Length; gambitId++)
        {
            writer.Write(GambitEnabled[gambitId]);
        }

        WriteSparsePositiveValues(writer, EntityCooldownExpiresAtTicks);
        WriteSparsePositiveValues(writer, CooldownExpiresAtTicks);
    }

    public void ReadActiveSnapshot(global::System.IO.BinaryReader reader)
    {
        if (reader.ReadInt32() != GambitEnabled.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var gambitId = 0; gambitId < GambitEnabled.Length; gambitId++)
        {
            GambitEnabled[gambitId] = reader.ReadBoolean();
        }

        ReadSparsePositiveValues(reader, EntityCooldownExpiresAtTicks);
        ReadSparsePositiveValues(reader, CooldownExpiresAtTicks);
    }

    private static void WriteSparsePositiveValues(
        global::System.IO.BinaryWriter writer,
        int[] values)
    {
        var count = 0;
        for (var index = 1; index < values.Length; index++)
        {
            if (values[index] > 0)
            {
                count++;
            }
        }

        writer.Write(count);
        for (var index = 1; index < values.Length; index++)
        {
            if (values[index] <= 0)
            {
                continue;
            }

            writer.Write(index);
            writer.Write(values[index]);
        }
    }

    private static void ReadSparsePositiveValues(
        global::System.IO.BinaryReader reader,
        int[] values)
    {
        var count = reader.ReadInt32();
        if ((uint)count >= (uint)values.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var item = 0; item < count; item++)
        {
            var index = reader.ReadInt32();
            var value = reader.ReadInt32();
            if ((uint)index == 0u ||
                index >= values.Length ||
                value <= 0 ||
                values[index] != 0)
            {
                throw new global::System.InvalidOperationException();
            }

            values[index] = value;
        }
    }
}
