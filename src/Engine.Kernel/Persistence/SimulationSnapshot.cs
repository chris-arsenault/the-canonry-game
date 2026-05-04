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
        MovementProgramSnapshot.Write(writer, Movement);
        PlanningProgramSnapshot.Write(writer, Planners);
        LowAltitudeVerbProgramSnapshot.Write(writer, LowAltitudeVerbs);
        MapActivityProgramSnapshot.Write(writer, MapActivity);
        MapSignalsProgramSnapshot.Write(writer, MapSignals);
        EffectsProgramSnapshot.Write(writer, Effects);
        UpkeepProgramSnapshot.Write(writer, Upkeep);
        RelationshipProgramSnapshot.Write(writer, Relationships);
        GambitProgramSnapshot.Write(writer, Gambits);
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
        MovementProgramSnapshot.Read(reader, Movement);
        PlanningProgramSnapshot.Read(reader, Planners);
        LowAltitudeVerbProgramSnapshot.Read(reader, LowAltitudeVerbs);
        MapActivityProgramSnapshot.Read(reader, MapActivity);
        MapSignalsProgramSnapshot.Read(reader, MapSignals);
        EffectsProgramSnapshot.Read(reader, Effects);
        UpkeepProgramSnapshot.Read(reader, Upkeep);
        RelationshipProgramSnapshot.Read(reader, Relationships);
        GambitProgramSnapshot.Read(reader, Gambits);
        GeneratorProgramSnapshot.Read(reader, Generators);
        DegeneracyProgramSnapshot.Read(reader, Degeneracy);
    }

    internal void EmitSnapshotEvents()
    {
        World.EmitSnapshotEvents(_executionContext);
        Movement.EmitSnapshotEvents(this, CurrentTick);
    }

    internal int SnapshotEventCount() => World.SnapshotEventCount() + Movement.SnapshotEventCount();
}
