namespace Engine.Kernel;

internal static partial class KernelQuery
{
    internal static bool QueryRelatedToTargetMatches(
        WorldState world,
        in SharedQuery query,
        in QueryBinding binding,
        int entityId)
    {
        if (query.RelatedToTargetKindId == 0)
        {
            return true;
        }

        var relationshipId = query.RelatedToTargetIncoming != 0
            ? FindRelationship(world, binding.TargetId, entityId, query.RelatedToTargetKindId)
            : FindRelationship(world, entityId, binding.TargetId, query.RelatedToTargetKindId);
        return relationshipId != 0 &&
            RelationshipStrength(world, relationshipId) >= query.RelatedToTargetMinStrength;
    }

    // flags: bit 0 = has minimum strength, bit 1 = scan incoming edges.
    public static bool SelfRelationshipToKindPasses(
        WorldState world,
        int entityId,
        int relationshipKindId,
        int flags,
        int counterpartKindId,
        int counterpartSubtypeId,
        FixedScalar minimumStrength)
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
