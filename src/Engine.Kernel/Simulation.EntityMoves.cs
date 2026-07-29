namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class Simulation
{
    internal void MoveEntityPhysicalUnchecked(int entityId, int q, int r, int tick)
    {
        World.MoveEntityPhysical(entityId, q, r, tick, ExecutionContext);
    }

    internal void MoveEntityPhysicalUnchecked(int entityId, int q, int r, int z, int tick)
    {
        World.MoveEntityPhysical(entityId, q, r, z, tick, ExecutionContext);
    }

    internal void MoveEntitySemantic(int entityId, int q, int r, int s, int tick)
    {
        _ = Movement.Place(
            this,
            entityId,
            PlaneCodes.Semantic,
            q,
            r,
            z: 0,
            tick,
            MovementCauseCodes.Forced);
        _ = s;
    }

    internal MovementMoveResult MoveEntitySemantic(int entityId, int q, int r, int s, int tick, int causeCode)
    {
        _ = s;
        if (causeCode == MovementCauseCodes.Travel)
        {
            return Movement.Navigate(this, entityId, PlaneCodes.Semantic, q, r, tick);
        }

        return Movement.Place(
            this,
            entityId,
            PlaneCodes.Semantic,
            q,
            r,
            z: 0,
            tick,
            causeCode);
    }
}
