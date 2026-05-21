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
        float value,
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
        if (delta > 0f)
        {
            simulation.ModifyCellCultureInfluenceRawFromEntity(entityId, planeCode, q, r, delta, tick);
        }
        else if (delta < 0f)
        {
            simulation.ModifyCellCultureInfluenceRaw(cultureId, planeCode, q, r, delta, tick);
        }

        if (value > 0f)
        {
            ledger.Values[key] = value;
        }
        else
        {
            ledger.Values.Remove(key);
        }
    }

    internal static float GetCellInfluence(
        this WorldState world,
        int entityId,
        int planeCode,
        int q,
        int r)
    {
        var cultureId = world.EntityCultureId(entityId);
        return cultureId <= 0 ? 0f : world.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r);
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
        out float value)
    {
        return world.MoveNextCultureInfluenceCell(ref cursor, out q, out r, out value);
    }

    internal static void ModifyCellCultureLaborInfluenceRaw(
        this Simulation simulation,
        int cultureId,
        int planeCode,
        int q,
        int r,
        float delta,
        int tick)
    {
        simulation.ModifyCellCultureInfluenceRaw(cultureId, planeCode, q, r, delta, tick);
    }

    internal static float GetCellCultureLaborInfluenceRaw(
        this WorldState world,
        int cultureId,
        int planeCode,
        int q,
        int r)
    {
        return world.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r);
    }

    private readonly record struct InfluenceKey(int EntityId, int PlaneCode, int Q, int R);

    private sealed class InfluenceLedger
    {
        public Dictionary<InfluenceKey, float> Values { get; } = [];
    }
}
