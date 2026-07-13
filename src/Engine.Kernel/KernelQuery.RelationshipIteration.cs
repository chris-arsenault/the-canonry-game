namespace Engine.Kernel;

internal static partial class KernelQuery
{
    public static bool MoveNextRelatedEndpoint(
        WorldState world,
        ref RelationshipGraphCursor cursor,
        bool outgoing,
        out int relationshipId,
        out int endpointId)
    {
        if (!world.MoveNextRelationship(ref cursor, out relationshipId))
        {
            endpointId = 0;
            return false;
        }

        endpointId = outgoing
            ? world.RelationshipTargetId(relationshipId)
            : world.RelationshipSourceId(relationshipId);
        return true;
    }
}
