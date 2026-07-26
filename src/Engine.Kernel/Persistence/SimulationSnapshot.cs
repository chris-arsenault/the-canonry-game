namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class Simulation
{
    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        writer.Write(CurrentTick);
        writer.Write(_phaseRevision);
        KernelData.WriteSnapshot(writer);
        World.WriteSnapshot(writer);
        _programs.WriteSnapshot(writer);
        MovementProgramSnapshot.Write(writer, Movement);
        LowAltitudeVerbProgramSnapshot.Write(writer, LowAltitudeVerbs);
        EffectsProgramSnapshot.Write(writer, Effects);
        CultureSlotProgramSnapshot.Write(writer, CultureSlots);
        MinorActorProgramSnapshot.Write(writer, MinorActors);
        RelationshipProgramSnapshot.Write(writer, Relationships);
        GambitProgramSnapshot.Write(writer, Gambits);
        GeneratorProgramSnapshot.Write(writer, Generators);
        KernelProgramSnapshot.WriteData(writer, Chronicles.Data);
        KernelProgramSnapshot.WriteState(writer, Chronicles.State);
        KernelProgramSnapshot.WriteData(writer, WorldBeats.Data);
        KernelProgramSnapshot.WriteState(writer, WorldBeats.State);
        KernelProgramSnapshot.WriteData(writer, MapFrontier.Data);
        KernelProgramSnapshot.WriteState(writer, MapFrontier.State);
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        CurrentTick = reader.ReadInt32();
        _phaseRevision = reader.ReadInt64();
        KernelData.ReadSnapshot(reader);
        World.ReadSnapshot(reader);
        _programs.ReadSnapshot(this, reader);
        MovementProgramSnapshot.Read(reader, Movement);
        LowAltitudeVerbProgramSnapshot.Read(reader, LowAltitudeVerbs);
        CultureLocality.Invalidate();
        EffectsProgramSnapshot.Read(reader, Effects);
        CultureSlotProgramSnapshot.Read(reader, CultureSlots);
        MinorActorProgramSnapshot.Read(reader, MinorActors);
        RelationshipProgramSnapshot.Read(reader, Relationships);
        GambitProgramSnapshot.Read(reader, Gambits);
        GeneratorProgramSnapshot.Read(reader, Generators);
        KernelProgramSnapshot.ReadData(reader, Chronicles.Data);
        KernelProgramSnapshot.ReadState(reader, Chronicles.State);
        KernelProgramSnapshot.ReadData(reader, WorldBeats.Data);
        KernelProgramSnapshot.ReadState(reader, WorldBeats.State);
        KernelProgramSnapshot.ReadData(reader, MapFrontier.Data);
        KernelProgramSnapshot.ReadState(reader, MapFrontier.State);
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
