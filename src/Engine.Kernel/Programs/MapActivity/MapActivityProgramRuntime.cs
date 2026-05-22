namespace Engine.Kernel;

internal sealed class MapActivityProgramRuntime :
    IKernelProgramRuntime<MapActivityProgramData, MapActivityProgramState>
{
    public MapActivityProgramRuntime(MapActivityProgramData data, MapActivityProgramState state)
        : this(data, state, new KernelProgramTuningOptions())
    {
    }

    public MapActivityProgramRuntime(
        MapActivityProgramData data,
        MapActivityProgramState state,
        KernelProgramTuningOptions tuning)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        global::System.ArgumentNullException.ThrowIfNull(tuning);
        Data = data;
        State = state;
        Tuning = tuning;
    }

    public MapActivityProgramData Data { get; }

    public MapActivityProgramState State { get; }

    public KernelProgramTuningOptions Tuning { get; }

    public float Heat(int planeCode, int q, int r, int tick) =>
        State.Heat(planeCode, q, r, tick, Data.HeatDecayPerTick, Tuning.MinorActorMainTickRatio);

    public int VisitCount(int planeCode, int q, int r) => State.VisitCount(planeCode, q, r);

    public int LastVisitedTick(int planeCode, int q, int r) => State.LastVisitedTick(planeCode, q, r);

    public int CurrentOccupancy(int planeCode, int q, int r) => State.CurrentOccupancy(planeCode, q, r);

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        State.BeginTick();
        var world = simulation.World;
        for (var entityId = 1; entityId <= world.EntityHighWatermark; entityId++)
        {
            if (!world.EntityIsActive(entityId))
            {
                continue;
            }

            State.RecordPresence(
                PlaneCodes.Physical,
                world.EntityPhysicalQ(entityId),
                world.EntityPhysicalR(entityId),
                tick,
                Data.PresenceHeat,
                Data.HeatDecayPerTick,
                Data.HeatEmaAlpha,
                Tuning.MinorActorMainTickRatio);
            State.RecordPresence(
                PlaneCodes.Semantic,
                world.EntitySemanticQ(entityId),
                world.EntitySemanticR(entityId),
                tick,
                Data.PresenceHeat,
                Data.HeatDecayPerTick,
                Data.HeatEmaAlpha,
                Tuning.MinorActorMainTickRatio);
        }
    }
}
