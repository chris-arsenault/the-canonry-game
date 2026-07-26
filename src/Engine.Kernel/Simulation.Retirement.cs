namespace Engine.Kernel;

internal sealed partial class Simulation
{
    internal void RetireEntity(int entityId, int tick)
    {
        if (!World.EntityIsActive(entityId))
        {
            return;
        }

        LowAltitudeVerbs.NoteEntityRetired(World, entityId);
        CultureSlots.NoteEntityRetired(this, entityId, tick);
        MinorActors.NoteEntityRetired(this, entityId, tick);
        Chronicles.NoteEntityRetired(entityId);
        World.ReleaseCoverageOwner(entityId, tick, _executionContext);
        for (var relationshipId = World.FirstIncidentRelationship(entityId); relationshipId > 0;)
        {
            var nextRelationshipId = World.NextIncidentRelationship(entityId, relationshipId);
            CullRelationship(relationshipId, tick);
            relationshipId = nextRelationshipId;
        }

        World.RemoveDistrictsOfHost(entityId);
        Gambits.NoteEntityRetired(entityId);
        World.SetEntityStatus(entityId, EntityStatusCodes.Historical, tick, _executionContext);
    }
}
