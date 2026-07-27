namespace Engine.Kernel;

internal sealed partial class Simulation
{
    internal void RetireEntity(int entityId, int tick)
    {
        if (!World.EntityIsActive(entityId))
        {
            return;
        }

        if (IsCollectingDeltas)
        {
            throw new global::System.InvalidOperationException();
        }

        LowAltitudeVerbs.NoteEntityRetired(entityId);
        Gambits.NoteEntityRetired(entityId);
        Relationships.NoteEntityRetired(entityId);
        CultureSlots.NoteEntityRetired(this, entityId, tick);
        MinorActors.NoteEntityRetired(this, entityId, tick);
        Chronicles.NoteEntityRetired(entityId);
        ReleaseCoverageOwner(entityId, tick);
        for (var relationshipId = World.FirstIncidentRelationship(entityId); relationshipId > 0;)
        {
            var nextRelationshipId = World.NextIncidentRelationship(entityId, relationshipId);
            CullRelationshipOwned(relationshipId, tick);
            relationshipId = nextRelationshipId;
        }

        World.RemoveDistrictsOfHost(entityId);
        World.SetEntityStatus(entityId, EntityStatusCodes.Historical, tick, ExecutionContext);
    }

    internal void ReleaseCoverageOwner(int entityId, int tick)
    {
        if (!IsCollectingDeltas)
        {
            World.ReleaseCoverageOwner(entityId, tick, ExecutionContext);
            return;
        }

        QueueCoverageRelease(entityId, PlaneCodes.Physical, tick);
        QueueCoverageRelease(entityId, PlaneCodes.Semantic, tick);
    }

    private void QueueCoverageRelease(
        int entityId,
        int planeCode,
        int tick)
    {
        var cursor = World.CoverageCells(entityId, planeCode);
        while (World.MoveNextCoverageCell(
                   ref cursor,
                   out var q,
                   out var r))
        {
            _continuousOwners.Enqueue(
                new CellStateDelta(
                    planeCode,
                    q,
                    r,
                    World.CellBaseTerrainId(planeCode, q, r),
                    OwnerEntityId: 0,
                    ExecutionContext,
                    ExpectedOwnerEntityId: entityId));
        }
    }
}
