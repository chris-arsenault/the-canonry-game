namespace Engine.Kernel;

using Engine.Constants;

internal static partial class KernelQuery
{
    private static bool TryEvaluateGeographyPredicate(
        WorldState world,
        in QueryPredicate predicate,
        in QueryBinding binding,
        out bool result)
    {
        result = predicate.Code switch
        {
            BehaviorPreconditionCodes.SelfHexFactRange => HexFactInRange(world, binding.ActorId, predicate.A, predicate.B, predicate.C),
            BehaviorPreconditionCodes.TargetHexFactRange => HexFactInRange(world, binding.TargetId, predicate.A, predicate.B, predicate.C),
            BehaviorPreconditionCodes.SelfOnTerrainGroup => EntityOnTerrainGroup(world, binding.ActorId, predicate.A),
            _ => false,
        };
        return predicate.Code is BehaviorPreconditionCodes.SelfHexFactRange or
            BehaviorPreconditionCodes.TargetHexFactRange or
            BehaviorPreconditionCodes.SelfOnTerrainGroup;
    }

    // Latent geography at the entity's feet: one quantized fact column,
    // banded. Facts read 0 outside the installed field, so a min bound
    // fails harmlessly off-map while a pure max bound still passes.
    private static bool HexFactInRange(WorldState world, int entityId, int factCode, int min, int max)
    {
        var fact = world.PhysicalHexFact(
            world.EntityPhysicalQ(entityId),
            world.EntityPhysicalR(entityId),
            factCode);
        return fact >= min && fact <= max;
    }

    private static bool EntityOnTerrainGroup(WorldState world, int entityId, int boundaryGroupId)
    {
        var terrainId = world.CellTerrainId(
            PlaneCodes.Physical,
            world.EntityPhysicalQ(entityId),
            world.EntityPhysicalR(entityId));
        return terrainId > 0 && world.TerrainBoundaryGroupId(terrainId) == boundaryGroupId;
    }
}
