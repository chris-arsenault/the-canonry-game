namespace Engine.Kernel;

internal static partial class KernelQuery
{
    // flags: bit 0 = has minimum strength, bit 1 = scan incoming edges.
    public static bool SelfRelationshipToKindPasses(
        WorldState world,
        int entityId,
        int relationshipKindId,
        int flags,
        int counterpartKindId,
        int counterpartSubtypeId,
        float minimumStrength)
    {
        var hasMinimumStrength = flags & 1;
        var incoming = (flags & 2) != 0;
        var cursor = incoming
            ? world.IncomingRelationshipsOfKind(entityId, relationshipKindId)
            : world.OutgoingRelationshipsOfKind(entityId, relationshipKindId);
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            var counterpartId = incoming
                ? world.RelationshipSourceId(relationshipId)
                : world.RelationshipTargetId(relationshipId);
            if (!world.EntityIsActive(counterpartId) ||
                world.EntityKindId(counterpartId) != counterpartKindId)
            {
                continue;
            }

            if (counterpartSubtypeId != 0 &&
                world.EntitySubtypeId(counterpartId) != counterpartSubtypeId)
            {
                continue;
            }

            if (RelationshipMeetsMinimum(world, relationshipId, hasMinimumStrength, minimumStrength))
            {
                return true;
            }
        }

        return false;
    }
}
