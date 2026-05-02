namespace Engine.Kernel;

internal static class KernelQuery
{
    public static void ForEachEntity(WorldState world, global::System.Func<int, bool> visit)
    {
        for (var entityId = 1; entityId <= world.EntityHighWatermark; entityId++)
        {
            if (!visit(entityId))
            {
                return;
            }
        }
    }

    public static void ForEachActiveEntity(WorldState world, global::System.Func<int, bool> visit)
    {
        ForEachEntity(
            world,
            entityId => !world.EntityIsActive(entityId) || visit(entityId));
    }

    public static void ForEachEntityOfKind(WorldState world, int kindId, global::System.Func<int, bool> visit)
    {
        for (var entityId = world.FirstEntityOfKind(kindId);
             entityId > 0;
             entityId = world.NextEntityOfKind(entityId))
        {
            if (!visit(entityId))
            {
                return;
            }
        }
    }

    public static void ForEachPhysicalEntityAt(WorldState world, int q, int r, global::System.Func<int, bool> visit)
    {
        for (var entityId = world.FirstPhysicalEntityAt(q, r);
             entityId > 0;
             entityId = world.NextPhysicalEntityAt(entityId))
        {
            if (!visit(entityId))
            {
                return;
            }
        }
    }

    public static void ForEachSemanticEntityAt(WorldState world, int q, int r, global::System.Func<int, bool> visit)
    {
        for (var entityId = world.FirstSemanticEntityAt(q, r);
             entityId > 0;
             entityId = world.NextSemanticEntityAt(entityId))
        {
            if (!visit(entityId))
            {
                return;
            }
        }
    }

    public static void ForEachCoverageCell(WorldState world, int ownerEntityId, global::System.Func<int, int, bool> visit)
    {
        var cursor = world.CoverageCells(ownerEntityId);
        while (world.MoveNextCoverageCell(ref cursor, out var q, out var r))
        {
            if (!visit(q, r))
            {
                return;
            }
        }
    }

    public static void ForEachRelatedEndpoint(
        WorldState world,
        int entityId,
        int relationshipKindId,
        float minimumStrength,
        int requiredFactId,
        bool outgoing,
        global::System.Func<int, int, bool> visit)
    {
        var cursor = outgoing
            ? world.OutgoingRelationshipsOfKindAbove(entityId, relationshipKindId, minimumStrength)
            : world.IncomingRelationshipsOfKindAbove(entityId, relationshipKindId, minimumStrength);
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            if (!world.RelationshipHasFact(relationshipId, requiredFactId))
            {
                continue;
            }

            var endpointId = outgoing
                ? world.RelationshipTargetId(relationshipId)
                : world.RelationshipSourceId(relationshipId);
            if (!visit(relationshipId, endpointId))
            {
                return;
            }
        }
    }

    public static bool PhysicalCellIsOccupied(WorldState world, int q, int r)
    {
        var occupied = false;
        ForEachPhysicalEntityAt(
            world,
            q,
            r,
            entityId =>
            {
                occupied = world.EntityIsActive(entityId) &&
                    world.EntityPhysicalQ(entityId) == q &&
                    world.EntityPhysicalR(entityId) == r;
                return !occupied;
            });
        return occupied;
    }

    public static bool SemanticCellIsOccupied(WorldState world, int q, int r)
    {
        var occupied = false;
        ForEachSemanticEntityAt(
            world,
            q,
            r,
            entityId =>
            {
                occupied = world.EntityIsActive(entityId) &&
                    world.EntitySemanticQ(entityId) == q &&
                    world.EntitySemanticR(entityId) == r;
                return !occupied;
            });
        return occupied;
    }

    public static bool EntityIsActiveKind(WorldState world, int entityId, int kindId) =>
        world.EntityIsActive(entityId) && world.EntityKindId(entityId) == kindId;

    public static bool TraitFilterPasses(WorldState world, int entityId, int traitId) =>
        traitId == 0 || world.HasTrait(entityId, traitId);

    public static bool ValueInRange(float value, int hasMin, int hasMax, float min, float max)
    {
        if (hasMin != 0 && value < min)
        {
            return false;
        }

        return hasMax == 0 || value <= max;
    }

    public static bool AttributeInRange(
        WorldState world,
        int entityId,
        int attributeId,
        int hasMin,
        int hasMax,
        float min,
        float max) =>
        ValueInRange(world.GetAttribute(entityId, attributeId), hasMin, hasMax, min, max);

    public static int ActiveEntityCountOfKind(WorldState world, int kindId)
    {
        var count = 0;
        for (var entityId = world.FirstEntityOfKind(kindId);
             entityId > 0;
             entityId = world.NextEntityOfKind(entityId))
        {
            if (world.EntityIsActive(entityId))
            {
                count++;
            }
        }

        return count;
    }

    public static bool HasColocatedEntity(WorldState world, int entityId)
    {
        var q = world.EntityPhysicalQ(entityId);
        var r = world.EntityPhysicalR(entityId);
        for (var colocatedId = world.FirstPhysicalEntityAt(q, r);
             colocatedId > 0;
             colocatedId = world.NextPhysicalEntityAt(colocatedId))
        {
            if (colocatedId != entityId && world.EntityIsActive(colocatedId))
            {
                return true;
            }
        }

        return false;
    }

    public static int PlaneDistance(WorldState world, int entityId, int targetId, int planeCode) =>
        planeCode == PlaneCodes.Semantic
            ? SemanticDistance(world, entityId, targetId)
            : HexDistance(
                world.EntityPhysicalQ(entityId),
                world.EntityPhysicalR(entityId),
                world.EntityPhysicalQ(targetId),
                world.EntityPhysicalR(targetId));

    public static int HexDistance(int aq, int ar, int bq, int br)
    {
        var aS = -aq - ar;
        var bS = -bq - br;
        return (Abs(aq - bq) + Abs(ar - br) + Abs(aS - bS)) / 2;
    }

    public static int CurrentQ(WorldState world, int entityId, int planeCode) =>
        planeCode == PlaneCodes.Semantic ? world.EntitySemanticQ(entityId) : world.EntityPhysicalQ(entityId);

    public static int CurrentR(WorldState world, int entityId, int planeCode) =>
        planeCode == PlaneCodes.Semantic ? world.EntitySemanticR(entityId) : world.EntityPhysicalR(entityId);

    public static bool RelationshipPasses(
        WorldState world,
        int sourceId,
        int targetId,
        int relationshipKindId,
        int hasMinimumStrength,
        float minimumStrength,
        int requiredFactId = 0)
    {
        var relationshipId = world.FindRelationship(sourceId, targetId, relationshipKindId);
        return relationshipId > 0 &&
               RelationshipMeetsMinimum(world, relationshipId, hasMinimumStrength, minimumStrength) &&
               world.RelationshipHasFact(relationshipId, requiredFactId);
    }

    public static bool SelfOutgoingRelationshipPasses(
        WorldState world,
        int entityId,
        int relationshipKindId,
        int hasMinimumStrength,
        float minimumStrength)
    {
        var cursor = world.OutgoingRelationshipsOfKind(entityId, relationshipKindId);
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            if (RelationshipMeetsMinimum(world, relationshipId, hasMinimumStrength, minimumStrength))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasRelationshipOfKind(WorldState world, int entityId, int relationshipKindId, bool outgoing)
    {
        var found = false;
        ForEachRelatedEndpoint(
            world,
            entityId,
            relationshipKindId,
            minimumStrength: 0f,
            requiredFactId: 0,
            outgoing,
            (_, _) =>
            {
                found = true;
                return false;
            });
        return found;
    }

    public static int FindRelationship(WorldState world, int sourceId, int targetId, int relationshipKindId) =>
        world.FindRelationship(sourceId, targetId, relationshipKindId);

    public static bool RelationshipHasFact(WorldState world, int relationshipId, int requiredFactId) =>
        world.RelationshipHasFact(relationshipId, requiredFactId);

    public static float RelationshipStrength(WorldState world, int relationshipId) =>
        world.RelationshipStrength(relationshipId);

    public static int DistinctRelationshipEndpointCount(
        WorldState world,
        int endpointId,
        int relationshipKindId,
        int hasMinimumStrength,
        float minimumStrength)
    {
        var count = 0;
        var outgoing = world.OutgoingRelationshipsOfKind(endpointId, relationshipKindId);
        while (world.MoveNextRelationship(ref outgoing, out var outgoingRelationshipId))
        {
            if (RelationshipMeetsMinimum(world, outgoingRelationshipId, hasMinimumStrength, minimumStrength))
            {
                count++;
            }
        }

        var incoming = world.IncomingRelationshipsOfKind(endpointId, relationshipKindId);
        while (world.MoveNextRelationship(ref incoming, out var incomingRelationshipId))
        {
            if (!RelationshipMeetsMinimum(world, incomingRelationshipId, hasMinimumStrength, minimumStrength))
            {
                continue;
            }

            var counterpartId = world.RelationshipSourceId(incomingRelationshipId);
            if (!OutgoingRelationshipToCounterpartExists(
                    world,
                    endpointId,
                    counterpartId,
                    relationshipKindId,
                    hasMinimumStrength,
                    minimumStrength))
            {
                count++;
            }
        }

        return count;
    }

    public static bool InfluenceOverlapPasses(
        WorldState world,
        int entityId,
        int targetId,
        int planeCode,
        float selfMinimum,
        float targetMinimum)
    {
        var selfCellCount = world.InfluenceCellCount(entityId, planeCode);
        var targetCellCount = world.InfluenceCellCount(targetId, planeCode);
        if (selfCellCount == 0 || targetCellCount == 0)
        {
            return false;
        }

        return selfCellCount <= targetCellCount
            ? InfluenceOverlapPassesFromCursor(world, entityId, targetId, planeCode, selfMinimum, targetMinimum)
            : InfluenceOverlapPassesFromCursor(world, targetId, entityId, planeCode, targetMinimum, selfMinimum);
    }

    public static float ThresholdEvidence(float value, float threshold)
    {
        if (threshold <= 0f)
        {
            return value > 0f ? 1f : 0f;
        }

        return Clamp01(value / threshold);
    }

    public static float DistanceEvidence(int distance, int maxDistance)
    {
        if (maxDistance <= 0)
        {
            return distance == 0 ? 1f : 0f;
        }

        if (distance > maxDistance)
        {
            return 0f;
        }

        return 1f - (distance / (float)(maxDistance + 1));
    }

    public static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }

    private static int SemanticDistance(WorldState world, int sourceId, int targetId)
    {
        var q = Abs(world.EntitySemanticQ(sourceId) - world.EntitySemanticQ(targetId));
        var r = Abs(world.EntitySemanticR(sourceId) - world.EntitySemanticR(targetId));
        var s = Abs(world.EntitySemanticS(sourceId) - world.EntitySemanticS(targetId));
        return Max(q, Max(r, s));
    }

    private static bool RelationshipMeetsMinimum(
        WorldState world,
        int relationshipId,
        int hasMinimumStrength,
        float minimumStrength) =>
        hasMinimumStrength == 0 || world.RelationshipStrength(relationshipId) >= minimumStrength;

    private static bool OutgoingRelationshipToCounterpartExists(
        WorldState world,
        int endpointId,
        int counterpartId,
        int relationshipKindId,
        int hasMinimumStrength,
        float minimumStrength)
    {
        var outgoing = world.OutgoingRelationshipsOfKind(endpointId, relationshipKindId);
        while (world.MoveNextRelationship(ref outgoing, out var relationshipId))
        {
            if (world.RelationshipTargetId(relationshipId) == counterpartId &&
                RelationshipMeetsMinimum(world, relationshipId, hasMinimumStrength, minimumStrength))
            {
                return true;
            }
        }

        return false;
    }

    private static bool InfluenceOverlapPassesFromCursor(
        WorldState world,
        int cursorEntityId,
        int otherEntityId,
        int planeCode,
        float cursorMinimum,
        float otherMinimum)
    {
        var cursor = world.InfluenceCells(cursorEntityId, planeCode);
        while (world.MoveNextInfluenceCell(ref cursor, out var q, out var r, out var influence))
        {
            if (influence >= cursorMinimum &&
                (planeCode != PlaneCodes.Physical || world.CellIsKnown(q, r)) &&
                world.GetCellInfluence(otherEntityId, planeCode, q, r) >= otherMinimum)
            {
                return true;
            }
        }

        return false;
    }

    private static int Abs(int value) => value < 0 ? -value : value;

    private static int Max(int left, int right) => left >= right ? left : right;
}
