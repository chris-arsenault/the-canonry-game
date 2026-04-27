namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class Simulation
{
    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        writer.Write(CurrentTick);
        _random.WriteSnapshot(writer);
        KernelData.WriteSnapshot(writer);
        World.WriteSnapshot(writer);
        _programs.WriteSnapshot(writer);
        UpkeepProgramSnapshot.Write(writer, Upkeep);
        RelationshipProgramSnapshot.Write(writer, Relationships);
        ProcessProgramSnapshot.Write(writer, Processes);
        GambitProgramSnapshot.Write(writer, Gambits);
        PressureProgramSnapshot.Write(writer, Pressures);
        GeneratorProgramSnapshot.Write(writer, Generators);
        DegeneracyProgramSnapshot.Write(writer, Degeneracy);
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        CurrentTick = reader.ReadInt32();
        _random.ReadSnapshot(reader);
        KernelData.ReadSnapshot(reader);
        World.ReadSnapshot(reader);
        _programs.ReadSnapshot(this, reader);
        UpkeepProgramSnapshot.Read(reader, Upkeep);
        RelationshipProgramSnapshot.Read(reader, Relationships);
        ProcessProgramSnapshot.Read(reader, Processes);
        GambitProgramSnapshot.Read(reader, Gambits);
        PressureProgramSnapshot.Read(reader, Pressures);
        GeneratorProgramSnapshot.Read(reader, Generators);
        DegeneracyProgramSnapshot.Read(reader, Degeneracy);
    }

    internal void EmitSnapshotEvents()
    {
        World.EmitSnapshotEvents(_executionContext);
    }
}
