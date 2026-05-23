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
        LowAltitudeVerbProgramSnapshot.Write(writer, LowAltitudeVerbs);
        MapActivityProgramSnapshot.Write(writer, MapActivity);
        MapSignalsProgramSnapshot.Write(writer, MapSignals);
        CultureBoundaryProgramSnapshot.Write(writer, CultureBoundaries);
        EffectsProgramSnapshot.Write(writer, Effects);
        CultureSlotProgramSnapshot.Write(writer, CultureSlots);
        MinorActorProgramSnapshot.Write(writer, MinorActors);
        UpkeepProgramSnapshot.Write(writer, Upkeep);
        RelationshipProgramSnapshot.Write(writer, Relationships);
        GambitProgramSnapshot.Write(writer, Gambits);
        GeneratorProgramSnapshot.Write(writer, Generators);
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        CurrentTick = reader.ReadInt32();
        _random.ReadSnapshot(reader);
        KernelData.ReadSnapshot(reader);
        World.ReadSnapshot(reader);
        _programs.ReadSnapshot(this, reader);
        MovementProgramSnapshot.Read(reader, Movement);
        LowAltitudeVerbProgramSnapshot.Read(reader, LowAltitudeVerbs);
        MapActivityProgramSnapshot.Read(reader, MapActivity);
        MapSignalsProgramSnapshot.Read(reader, MapSignals);
        CultureBoundaryProgramSnapshot.Read(reader, CultureBoundaries);
        EffectsProgramSnapshot.Read(reader, Effects);
        CultureSlotProgramSnapshot.Read(reader, CultureSlots);
        MinorActorProgramSnapshot.Read(reader, MinorActors);
        UpkeepProgramSnapshot.Read(reader, Upkeep);
        RelationshipProgramSnapshot.Read(reader, Relationships);
        GambitProgramSnapshot.Read(reader, Gambits);
        GeneratorProgramSnapshot.Read(reader, Generators);
    }

    internal void EmitSnapshotEvents()
    {
        World.EmitSnapshotEvents(_executionContext);
        Movement.EmitSnapshotEvents(this, CurrentTick);
        CultureSlots.EmitSnapshotEvents(this, CurrentTick);
    }

    internal int SnapshotEventCount() =>
        World.SnapshotEventCount() + Movement.SnapshotEventCount() + CultureSlots.SnapshotEventCount();
}
