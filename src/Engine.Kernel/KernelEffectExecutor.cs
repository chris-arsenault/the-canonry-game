namespace Engine.Kernel;

internal static class KernelEffectExecutor
{
    public static void ModifyAttribute(Simulation simulation, int entityId, int attributeId, float delta) =>
        simulation.ModifyAttribute(entityId, attributeId, delta, simulation.CurrentTick);

    public static void SetAttribute(Simulation simulation, int entityId, int attributeId, float value) =>
        simulation.SetAttribute(entityId, attributeId, value, simulation.CurrentTick);

    public static void TransferAttribute(
        Simulation simulation,
        int sourceId,
        int sourceAttributeId,
        int destinationId,
        int destinationAttributeId,
        float amount)
    {
        if (simulation.World.GetAttribute(sourceId, sourceAttributeId) < amount)
        {
            return;
        }

        ModifyAttribute(simulation, sourceId, sourceAttributeId, -amount);
        ModifyAttribute(simulation, destinationId, destinationAttributeId, amount);
    }

    public static void ModifyResource(Simulation simulation, int resourceId, int q, int r, float delta) =>
        simulation.ModifyResource(resourceId, q, r, delta, simulation.CurrentTick);

    public static void SetEntityStatus(Simulation simulation, int entityId, int statusCode) =>
        simulation.SetEntityStatus(entityId, statusCode, simulation.CurrentTick);

    public static void MovePhysical(Simulation simulation, int entityId, int q, int r) =>
        simulation.MoveEntityPhysical(entityId, q, r, simulation.CurrentTick);

    public static void MoveSemantic(Simulation simulation, int entityId, int q, int r, int s) =>
        simulation.MoveEntitySemantic(entityId, q, r, s, simulation.CurrentTick);

    public static void SetCellState(Simulation simulation, int q, int r, int terrainId, int ownerEntityId) =>
        simulation.SetCellState(q, r, terrainId, ownerEntityId, simulation.CurrentTick);

    public static void SetCellTerrain(Simulation simulation, int q, int r, int terrainId) =>
        simulation.SetCellTerrain(q, r, terrainId, simulation.CurrentTick);

    public static void SetCoverageOwner(Simulation simulation, int q, int r, int ownerEntityId) =>
        simulation.SetCoverageOwner(q, r, ownerEntityId, simulation.CurrentTick);

    public static int CreateEntity(
        Simulation simulation,
        int kindId,
        int subtypeId,
        int cultureId,
        int physicalQ,
        int physicalR,
        int semanticQ,
        int semanticR,
        int semanticS) =>
        simulation.CreateEntity(
            kindId,
            subtypeId,
            cultureId,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            semanticS,
            simulation.CurrentTick);

    public static int CreateRelationship(
        Simulation simulation,
        int relationshipKindId,
        int polarityCode,
        int sourceEntityId,
        int targetEntityId,
        float strength,
        int factId0 = 0,
        int factId1 = 0,
        int factId2 = 0,
        int factId3 = 0,
        int factCount = 0) =>
        simulation.CreateRelationship(
            relationshipKindId,
            polarityCode,
            sourceEntityId,
            targetEntityId,
            strength,
            simulation.CurrentTick,
            factId0,
            factId1,
            factId2,
            factId3,
            factCount);

    public static void SetRelationshipStrength(Simulation simulation, int relationshipId, float strength) =>
        simulation.SetRelationshipStrength(relationshipId, strength, simulation.CurrentTick);

    public static void CullRelationship(Simulation simulation, int relationshipId) =>
        simulation.CullRelationship(relationshipId, simulation.CurrentTick);

    public static void CullOutgoingRelationshipsOfKind(Simulation simulation, int sourceEntityId, int relationshipKindId) =>
        simulation.CullOutgoingRelationshipsOfKind(sourceEntityId, relationshipKindId, simulation.CurrentTick);

    public static void CullAllRelationshipsOfKind(
        Simulation simulation,
        WorldState world,
        int entityId,
        int relationshipKindId)
    {
        CullOutgoingRelationshipsOfKind(simulation, entityId, relationshipKindId);
        while (true)
        {
            var cursor = world.IncomingRelationshipsOfKind(entityId, relationshipKindId);
            if (!world.MoveNextRelationship(ref cursor, out var relationshipId))
            {
                return;
            }

            CullRelationship(simulation, relationshipId);
        }
    }
}
