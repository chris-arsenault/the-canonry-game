namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class LowAltitudeVerbProgramRuntime
{
    private bool HasTargetRelationship(WorldState world, int entityId, int targetEntityId, int rule)
    {
        var relationshipKindId = Data.RelationshipKindId(rule);
        if (relationshipKindId == 0)
        {
            return false;
        }

        if (Data.RelationshipDirectionCode(rule) is LowAltitudeVerbRelationshipDirectionCodes.Outgoing
            or LowAltitudeVerbRelationshipDirectionCodes.Either)
        {
            if (HasRelationshipInDirection(world, entityId, targetEntityId, relationshipKindId, outgoing: true))
            {
                return true;
            }
        }

        if (Data.RelationshipDirectionCode(rule) is LowAltitudeVerbRelationshipDirectionCodes.Incoming
            or LowAltitudeVerbRelationshipDirectionCodes.Either)
        {
            return HasRelationshipInDirection(world, entityId, targetEntityId, relationshipKindId, outgoing: false);
        }

        return false;
    }

    private static bool HasRelationshipInDirection(
        WorldState world,
        int entityId,
        int targetEntityId,
        int relationshipKindId,
        bool outgoing)
    {
        var cursor = outgoing
            ? world.OutgoingRelationshipsOfKind(entityId, relationshipKindId)
            : world.IncomingRelationshipsOfKind(entityId, relationshipKindId);
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            var relatedEntityId = outgoing
                ? world.RelationshipTargetId(relationshipId)
                : world.RelationshipSourceId(relationshipId);
            if (relatedEntityId == targetEntityId)
            {
                return true;
            }
        }

        return false;
    }

    private void AdjustTargetRelationshipStrength(
        Simulation simulation,
        int entityId,
        int rule,
        int tick)
    {
        var targetOccurrenceHandle =
            State.TargetEntityOccurrenceHandle(entityId);
        var relationshipKindId = Data.RelationshipKindId(rule);
        var delta = Data.RelationshipStrengthDelta(rule);
        if (targetOccurrenceHandle == 0 ||
            relationshipKindId == 0 ||
            delta == 0)
        {
            return;
        }

        var world = simulation.World;
        if (!world.TryResolveEntityOccurrence(
                targetOccurrenceHandle,
                out var targetEntityId))
        {
            return;
        }

        if (Data.RelationshipDirectionCode(rule) is LowAltitudeVerbRelationshipDirectionCodes.Outgoing
            or LowAltitudeVerbRelationshipDirectionCodes.Either)
        {
            AdjustMatchingRelationships(
                simulation,
                world,
                entityId,
                targetEntityId,
                relationshipKindId,
                outgoing: true,
                delta: delta,
                tick: tick);
        }

        if (Data.RelationshipDirectionCode(rule) is LowAltitudeVerbRelationshipDirectionCodes.Incoming
            or LowAltitudeVerbRelationshipDirectionCodes.Either)
        {
            AdjustMatchingRelationships(
                simulation,
                world,
                entityId,
                targetEntityId,
                relationshipKindId,
                outgoing: false,
                delta: delta,
                tick: tick);
        }
    }

    private static void AdjustMatchingRelationships(
        Simulation simulation,
        WorldState world,
        int entityId,
        int targetEntityId,
        int relationshipKindId,
        bool outgoing,
        FixedScalar delta,
        int tick)
    {
        var cursor = outgoing
            ? world.OutgoingRelationshipsOfKind(entityId, relationshipKindId)
            : world.IncomingRelationshipsOfKind(entityId, relationshipKindId);
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            var relatedEntityId = outgoing
                ? world.RelationshipTargetId(relationshipId)
                : world.RelationshipSourceId(relationshipId);
            if (relatedEntityId != targetEntityId)
            {
                continue;
            }

            simulation.ModifyRelationshipStrengthUnits(
                relationshipId,
                KernelFixedPoint.RelationshipStrengthDeltaUnits(delta),
                tick);
        }
    }
}
