namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class GambitProgramRuntime
{
    private FixedScalar EvaluateRelationshipTargets(
        Simulation simulation,
        int gambitSlot,
        int entityId,
        int tick,
        ref int candidateCount)
    {
        var world = simulation.World;
        var directionCode = _gambitRelationshipTargetDirectionCodes[gambitSlot];
        var threshold = _gambitRelationshipTargetHasMinimumStrength[gambitSlot] == 0
            ? FixedScalar.Zero
            : _gambitRelationshipTargetMinimumStrengths[gambitSlot];
        var cursor = RelationshipTargetCursor(
            world,
            entityId,
            directionCode,
            _gambitRelationshipTargetKindIds[gambitSlot],
            threshold);
        var state = new TargetSelectionState(candidateCount);
        var total = FixedScalar.Zero;
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            var targetId = directionCode == GambitRelationshipTargetDirectionCodes.Outgoing
                ? world.RelationshipTargetId(relationshipId)
                : world.RelationshipSourceId(relationshipId);
            if (!RelationshipTargetEligible(world, gambitSlot, entityId, targetId))
            {
                continue;
            }

            total += EvaluateTargetCandidate(
                simulation,
                gambitSlot,
                entityId,
                targetId,
                tick,
                state,
                relationshipTargetSourceSatisfied: true);
        }

        candidateCount = state.CandidateCount;
        return total;
    }

    private static RelationshipGraphCursor RelationshipTargetCursor(
        WorldState world,
        int entityId,
        int directionCode,
        int relationshipKindId,
        FixedScalar minimumStrength)
    {
        return directionCode == GambitRelationshipTargetDirectionCodes.Outgoing
            ? world.OutgoingRelationshipsOfKindAbove(
                entityId,
                relationshipKindId,
                minimumStrength)
            : world.IncomingRelationshipsOfKindAbove(
                entityId,
                relationshipKindId,
                minimumStrength);
    }

    private bool RelationshipTargetEligible(
        WorldState world,
        int gambitSlot,
        int entityId,
        int targetId)
    {
        if (!world.EntityIsActive(targetId) || targetId == entityId)
        {
            return false;
        }

        return _gambitTargetModes[gambitSlot] != BehaviorTargetModeCodes.KindOther ||
            world.EntityKindId(targetId) == _gambitTargetKindIds[gambitSlot];
    }
}
