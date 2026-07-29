namespace Engine.Kernel;

internal static class KernelEffectExecutor
{
    public static void ModifyAttribute(Simulation simulation, int entityId, int attributeId, FixedScalar delta) =>
        simulation.ModifyAttribute(entityId, attributeId, delta, simulation.CurrentTick);

    public static void ModifyAttributeUnits(Simulation simulation, int entityId, int attributeId, int deltaUnits) =>
        simulation.ModifyAttributeUnits(entityId, attributeId, deltaUnits, simulation.CurrentTick);

    public static void SetAttribute(Simulation simulation, int entityId, int attributeId, FixedScalar value) =>
        simulation.SetAttribute(entityId, attributeId, value, simulation.CurrentTick);

    public static void TransferAttribute(
        Simulation simulation,
        int sourceId,
        int sourceAttributeId,
        int destinationId,
        int destinationAttributeId,
        FixedScalar amount)
    {
        if (simulation.World.GetAttribute(sourceId, sourceAttributeId) < amount)
        {
            return;
        }

        ModifyAttribute(simulation, sourceId, sourceAttributeId, -amount);
        ModifyAttribute(simulation, destinationId, destinationAttributeId, amount);
    }

    public static void SetEntityStatus(Simulation simulation, int entityId, int statusCode) =>
        simulation.SetEntityStatus(entityId, statusCode, simulation.CurrentTick);

    public static void SetEntityCulture(Simulation simulation, int entityId, int cultureId) =>
        simulation.SetEntityCulture(entityId, cultureId, simulation.CurrentTick);

    public static void MovePhysical(
        Simulation simulation,
        int entityId,
        int q,
        int r,
        int z,
        int causeCode = MovementCauseCodes.Placement)
    {
        if (causeCode == MovementCauseCodes.Travel)
        {
            simulation.Movement.Navigate(simulation, entityId, PlaneCodes.Physical, q, r, simulation.CurrentTick);
            return;
        }

        simulation.Movement.Place(
            simulation,
            entityId,
            PlaneCodes.Physical,
            q,
            r,
            z,
            simulation.CurrentTick,
            causeCode);
    }

    public static void MoveSemantic(
        Simulation simulation,
        int entityId,
        int q,
        int r,
        int s,
        int causeCode = MovementCauseCodes.Placement)
    {
        _ = s;
        if (causeCode == MovementCauseCodes.Travel)
        {
            simulation.Movement.Navigate(simulation, entityId, PlaneCodes.Semantic, q, r, simulation.CurrentTick);
            return;
        }

        simulation.Movement.Place(
            simulation,
            entityId,
            PlaneCodes.Semantic,
            q,
            r,
            z: 0,
            simulation.CurrentTick,
            causeCode);
    }

    public static void SetCellState(Simulation simulation, int q, int r, int terrainId, int ownerEntityId) =>
        simulation.SetCellState(q, r, terrainId, ownerEntityId, simulation.CurrentTick);

    public static void SetCellState(
        Simulation simulation,
        int planeCode,
        int q,
        int r,
        int terrainId,
        int ownerEntityId) =>
        simulation.SetCellState(planeCode, q, r, terrainId, ownerEntityId, simulation.CurrentTick);

    public static void SetCellTerrain(Simulation simulation, int q, int r, int terrainId) =>
        SetCellTerrain(simulation, PlaneCodes.Physical, q, r, terrainId);

    public static void SetCellTerrain(Simulation simulation, int planeCode, int q, int r, int terrainId)
    {
        // Authored repaints never cross the land/water divide: a scar or a
        // cleansing changes what ground is, not whether it is ground. Cells
        // on the wrong side of the divide are skipped, never converted.
        if (planeCode == PlaneCodes.Physical &&
            simulation.World.CellIsKnown(planeCode, q, r) &&
            simulation.KernelData.Terrain.IsWater(simulation.World.CellBaseTerrainId(planeCode, q, r)) !=
            simulation.KernelData.Terrain.IsWater(terrainId))
        {
            return;
        }

        simulation.SetCellTerrain(planeCode, q, r, terrainId, simulation.CurrentTick);
    }

    public static void SetCoverageOwner(Simulation simulation, int q, int r, int ownerEntityId) =>
        simulation.SetCoverageOwner(q, r, ownerEntityId, simulation.CurrentTick);

    public static void SetCoverageOwner(Simulation simulation, int planeCode, int q, int r, int ownerEntityId) =>
        simulation.SetCoverageOwner(planeCode, q, r, ownerEntityId, simulation.CurrentTick);

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
        FixedScalar strength) =>
        simulation.CreateRelationship(
            relationshipKindId,
            polarityCode,
            sourceEntityId,
            targetEntityId,
            strength,
            simulation.CurrentTick);

    public static void SetRelationshipStrength(Simulation simulation, int relationshipId, FixedScalar strength) =>
        simulation.SetRelationshipStrength(relationshipId, strength, simulation.CurrentTick);

    public static void ModifyRelationshipStrengthUnits(
        Simulation simulation,
        int relationshipId,
        int deltaUnits) =>
        simulation.ModifyRelationshipStrengthUnits(
            relationshipId,
            deltaUnits,
            simulation.CurrentTick);

    public static void CullRelationship(Simulation simulation, int relationshipId) =>
        simulation.CullRelationship(relationshipId, simulation.CurrentTick);

    public static void CullIncomingRelationshipsOfKind(Simulation simulation, int targetEntityId, int relationshipKindId) =>
        simulation.CullIncomingRelationshipsOfKind(targetEntityId, relationshipKindId, simulation.CurrentTick);

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
