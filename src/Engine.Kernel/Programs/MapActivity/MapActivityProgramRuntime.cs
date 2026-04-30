namespace Engine.Kernel;

internal sealed class MapActivityProgramRuntime :
    IKernelProgramRuntime<MapActivityProgramData, MapActivityProgramState>
{
    public MapActivityProgramRuntime(MapActivityProgramData data, MapActivityProgramState state)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        Data = data;
        State = state;
    }

    public MapActivityProgramData Data { get; }

    public MapActivityProgramState State { get; }

    public float Heat(int planeCode, int q, int r) => State.Heat(planeCode, q, r);

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
                Data.PresenceHeat);
            State.RecordPresence(
                PlaneCodes.Semantic,
                world.EntitySemanticQ(entityId),
                world.EntitySemanticR(entityId),
                tick,
                Data.PresenceHeat);
        }
    }
}

