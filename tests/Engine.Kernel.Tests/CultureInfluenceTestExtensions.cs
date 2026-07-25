namespace Engine.Kernel.Tests;

using System.Runtime.CompilerServices;

using Engine.Kernel;

internal static class CultureInfluenceTestExtensions
{
    private static readonly ConditionalWeakTable<Simulation, InfluenceLedger> InfluenceLedgers = new();

    internal static void SetCellInfluence(
        this Simulation simulation,
        int entityId,
        int planeCode,
        int q,
        int r,
        FixedScalar value,
        int tick)
    {
        var cultureId = simulation.World.EntityCultureId(entityId);
        if (cultureId <= 0)
        {
            return;
        }

        var key = new InfluenceKey(entityId, planeCode, q, r);
        var ledger = InfluenceLedgers.GetValue(simulation, static _ => new InfluenceLedger());
        ledger.Values.TryGetValue(key, out var previousValue);
        var delta = value - previousValue;
        if (delta > FixedScalar.Zero)
        {
            simulation.ModifyCellCultureInfluenceRawFromEntity(entityId, planeCode, q, r, delta, tick);
        }
        else if (delta < FixedScalar.Zero)
        {
            simulation.ModifyCellCultureInfluenceRaw(cultureId, planeCode, q, r, delta, tick);
        }

        if (value > FixedScalar.Zero)
        {
            ledger.Values[key] = value;
        }
        else
        {
            ledger.Values.Remove(key);
        }
    }

    internal static FixedScalar GetCellInfluence(
        this WorldState world,
        int entityId,
        int planeCode,
        int q,
        int r)
    {
        var cultureId = world.EntityCultureId(entityId);
        return cultureId <= 0
            ? FixedScalar.Zero
            : world.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r);
    }

    internal static int InfluenceCellCount(this WorldState world, int entityId, int planeCode)
    {
        var cultureId = world.EntityCultureId(entityId);
        return cultureId <= 0 ? 0 : world.CultureInfluenceCellCount(cultureId, planeCode);
    }

    internal static SpatialLayerCellCursor InfluenceCells(this WorldState world, int entityId, int planeCode)
    {
        var cultureId = world.EntityCultureId(entityId);
        return cultureId <= 0 ? default : world.CultureInfluenceCells(cultureId, planeCode);
    }

    internal static bool MoveNextInfluenceCell(
        this WorldState world,
        ref SpatialLayerCellCursor cursor,
        out int q,
        out int r,
        out FixedScalar value)
    {
        return world.MoveNextCultureInfluenceCell(ref cursor, out q, out r, out value);
    }

    private readonly record struct InfluenceKey(int EntityId, int PlaneCode, int Q, int R);

    private sealed class InfluenceLedger
    {
        public Dictionary<InfluenceKey, FixedScalar> Values { get; } = [];
    }
}
