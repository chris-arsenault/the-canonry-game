namespace Engine.Kernel;

internal static partial class KernelQuery
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
        for (var entityId = 1; entityId <= world.EntityHighWatermark; entityId++)
        {
            if (!world.EntityIsActive(entityId))
            {
                continue;
            }

            if (!visit(entityId))
            {
                return;
            }
        }
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

    public static void ForEachActiveEntityOfKind(WorldState world, int kindId, global::System.Func<int, bool> visit)
    {
        for (var entityId = world.FirstEntityOfKind(kindId);
             entityId > 0;
             entityId = world.NextEntityOfKind(entityId))
        {
            if (!world.EntityIsActive(entityId))
            {
                continue;
            }

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
        ForEachCoverageCell(world, ownerEntityId, PlaneCodes.Physical, visit);
    }

    public static void ForEachCoverageCell(WorldState world, int ownerEntityId, int planeCode, global::System.Func<int, int, bool> visit)
    {
        var cursor = world.CoverageCells(ownerEntityId, planeCode);
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
        bool outgoing,
        global::System.Func<int, int, bool> visit)
    {
        var cursor = outgoing
            ? world.OutgoingRelationshipsOfKindAbove(entityId, relationshipKindId, minimumStrength)
            : world.IncomingRelationshipsOfKindAbove(entityId, relationshipKindId, minimumStrength);
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
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

    public static bool EntityKindMatches(WorldState world, int entityId, int kindId) =>
        world.EntityKindId(entityId) == kindId;

    public static bool TraitFilterPasses(WorldState world, int entityId, int traitId) =>
        traitId == 0 || world.HasTrait(entityId, traitId);

    public static bool SubtypeAllowed(int[]? subtypeIds, int start, int count, int subtypeId)
    {
        if (count == 0)
        {
            return true;
        }

        if (subtypeIds is null)
        {
            return false;
        }

        var end = start + count;
        for (var i = start; i < end; i++)
        {
            if (subtypeIds[i] == subtypeId)
            {
                return true;
            }
        }

        return false;
    }

    public static QueryEvaluation EvaluateSharedQuery(
        WorldState world,
        in SharedQuery query,
        in QueryBinding binding,
        int selectedOrdinal)
    {
        var queryValue = query;
        var bindingValue = binding;
        var count = 0;
        var selectedEntityId = 0;
        ForEachActiveEntity(
            world,
            entityId =>
            {
                if (!SharedQueryMatchesCandidate(world, queryValue, bindingValue, entityId))
                {
                    return true;
                }

                if (count == selectedOrdinal)
                {
                    selectedEntityId = entityId;
                }

                count++;
                return selectedEntityId == 0;
            });
        return new QueryEvaluation(count, selectedEntityId);
    }

    public static bool EvaluatePredicate(
        WorldState world,
        in QueryPredicate predicate,
        in QueryBinding binding)
    {
        return TryEvaluateSimplePredicate(world, predicate, binding, out var result) ||
            TryEvaluateRangePredicate(world, predicate, binding, out result) ||
            TryEvaluateCulturePredicate(world, predicate, binding, out result) ||
            TryEvaluateRelationshipPredicate(world, predicate, binding, out result)
            ? result
            : throw new global::System.InvalidOperationException();
    }

    public static bool QueryPredicateIsComposition(int code) =>
        code is BehaviorPreconditionCodes.And or
            BehaviorPreconditionCodes.Or or
            BehaviorPreconditionCodes.Not;

    public static bool TryApplyCompositionPredicate(
        int code,
        global::System.Span<bool> stack,
        ref int stackCount)
    {
        if (code == BehaviorPreconditionCodes.And)
        {
            var right = PopBool(stack, ref stackCount);
            var left = PopBool(stack, ref stackCount);
            PushBool(stack, ref stackCount, left && right);
            return true;
        }

        if (code == BehaviorPreconditionCodes.Or)
        {
            var right = PopBool(stack, ref stackCount);
            var left = PopBool(stack, ref stackCount);
            PushBool(stack, ref stackCount, left || right);
            return true;
        }

        if (code != BehaviorPreconditionCodes.Not)
        {
            return false;
        }

        PushBool(stack, ref stackCount, !PopBool(stack, ref stackCount));
        return true;
    }

    private static bool TryEvaluateSimplePredicate(
        WorldState world,
        in QueryPredicate predicate,
        in QueryBinding binding,
        out bool result)
    {
        result = predicate.Code switch
        {
            BehaviorPreconditionCodes.Always => true,
            BehaviorPreconditionCodes.TargetIsSelf => binding.TargetId == binding.ActorId,
            BehaviorPreconditionCodes.TargetIsNotSelf => binding.TargetId != binding.ActorId,
            BehaviorPreconditionCodes.HasColocatedEntity => HasColocatedEntity(world, binding.ActorId),
            _ => false,
        };
        return predicate.Code is BehaviorPreconditionCodes.Always or
            BehaviorPreconditionCodes.TargetIsSelf or
            BehaviorPreconditionCodes.TargetIsNotSelf or
            BehaviorPreconditionCodes.HasColocatedEntity;
    }

    private static bool TryEvaluateRangePredicate(
        WorldState world,
        in QueryPredicate predicate,
        in QueryBinding binding,
        out bool result)
    {
        if (TryEvaluateAttributePredicate(world, predicate, binding, out result))
        {
            return true;
        }

        if (TryEvaluateSpatialPredicate(world, predicate, binding, out result))
        {
            return true;
        }

        result = predicate.Code switch
        {
            BehaviorPreconditionCodes.CooldownElapsed => binding.RecentActionCount(binding.Tick - predicate.A + 1) == 0,
            BehaviorPreconditionCodes.EntityCountAtLeast => ActiveEntityCountOfKind(world, predicate.A) >= predicate.B,
            BehaviorPreconditionCodes.SelfWorkTierBelow => binding.EntityWorkTier(binding.ActorId) < predicate.A,
            _ => false,
        };
        return predicate.Code is BehaviorPreconditionCodes.CooldownElapsed or
            BehaviorPreconditionCodes.EntityCountAtLeast or
            BehaviorPreconditionCodes.SelfWorkTierBelow;
    }

    private static bool TryEvaluateAttributePredicate(
        WorldState world,
        in QueryPredicate predicate,
        in QueryBinding binding,
        out bool result)
    {
        result = predicate.Code switch
        {
            BehaviorPreconditionCodes.SelfAttributeRange => AttributeInRange(world, binding.ActorId, predicate.A, predicate.B, predicate.C, predicate.X, predicate.Y),
            BehaviorPreconditionCodes.TargetAttributeRange => AttributeInRange(world, binding.TargetId, predicate.A, predicate.B, predicate.C, predicate.X, predicate.Y),
            _ => false,
        };
        return predicate.Code is BehaviorPreconditionCodes.SelfAttributeRange or
            BehaviorPreconditionCodes.TargetAttributeRange;
    }

    private static bool TryEvaluateSpatialPredicate(
        WorldState world,
        in QueryPredicate predicate,
        in QueryBinding binding,
        out bool result)
    {
        if (TryEvaluateGeographyPredicate(world, predicate, binding, out result))
        {
            return true;
        }

        result = predicate.Code switch
        {
            BehaviorPreconditionCodes.PlaneDistance => PlaneDistance(world, binding.ActorId, binding.TargetId, predicate.B) <= predicate.A,
            BehaviorPreconditionCodes.WithinTravelReach => binding.TravelCost(binding.ActorId, binding.TargetId, predicate.B, binding.Tick) <= predicate.X,
            BehaviorPreconditionCodes.ContestedInfluence => InfluenceOverlapPasses(world, binding.ActorId, binding.TargetId, predicate.A, predicate.X, predicate.Y),
            BehaviorPreconditionCodes.SharedInfluence => InfluenceOverlapPasses(world, binding.ActorId, binding.TargetId, predicate.A, predicate.X, predicate.Y),
            _ => false,
        };
        return predicate.Code is BehaviorPreconditionCodes.PlaneDistance or
            BehaviorPreconditionCodes.WithinTravelReach or
            BehaviorPreconditionCodes.ContestedInfluence or
            BehaviorPreconditionCodes.SharedInfluence;
    }

    private static bool TryEvaluateRelationshipPredicate(
        WorldState world,
        in QueryPredicate predicate,
        in QueryBinding binding,
        out bool result)
    {
        if (TryEvaluateDirectedRelationshipPredicate(world, predicate, binding, out result))
        {
            return true;
        }

        if (TryEvaluateEndpointRelationshipPredicate(world, predicate, binding, out result))
        {
            return true;
        }

        result = false;
        return false;
    }

    private static bool TryEvaluateCulturePredicate(
        WorldState world,
        in QueryPredicate predicate,
        in QueryBinding binding,
        out bool result)
    {
        result = predicate.Code switch
        {
            BehaviorPreconditionCodes.EndpointCultureIs => EndpointCulturePasses(world, binding, predicate.A, predicate.B),
            BehaviorPreconditionCodes.EndpointSubtypeIs => EndpointSubtypePasses(world, binding, predicate.A, predicate.B),
            BehaviorPreconditionCodes.SelfCultureMass => SelfCultureMassPasses(world, binding.ActorId, predicate.A, predicate.X, predicate.Y),
            _ => false,
        };
        return predicate.Code is BehaviorPreconditionCodes.EndpointCultureIs or
            BehaviorPreconditionCodes.EndpointSubtypeIs or
            BehaviorPreconditionCodes.SelfCultureMass;
    }

    private static bool EndpointCulturePasses(
        WorldState world,
        in QueryBinding binding,
        int endpointCode,
        int cultureId)
    {
        var entityId = endpointCode == BehaviorEndpointCodes.Target
            ? binding.TargetId
            : binding.ActorId;
        return world.EntityCultureId(entityId) == cultureId;
    }

    private static bool EndpointSubtypePasses(
        WorldState world,
        in QueryBinding binding,
        int endpointCode,
        int subtypeId)
    {
        var entityId = endpointCode == BehaviorEndpointCodes.Target
            ? binding.TargetId
            : binding.ActorId;
        return world.EntitySubtypeId(entityId) == subtypeId;
    }

    private static bool SelfCultureMassPasses(
        WorldState world,
        int entityId,
        int planeCode,
        float minimumRawMass,
        float minimumConnectedRawMass)
    {
        if (!world.EntityIsActive(entityId))
        {
            return false;
        }

        var cultureId = world.EntityCultureId(entityId);
        if (cultureId <= 0)
        {
            return false;
        }

        var q = CurrentQ(world, entityId, planeCode);
        var r = CurrentR(world, entityId, planeCode);
        if (!world.CellIsKnown(planeCode, q, r))
        {
            return false;
        }

        var field = world.CultureFieldAt(cultureId, planeCode, q, r);
        return field.RawMass >= minimumRawMass &&
            field.ConnectedRawMass >= minimumConnectedRawMass;
    }

    private static bool TryEvaluateDirectedRelationshipPredicate(
        WorldState world,
        in QueryPredicate predicate,
        in QueryBinding binding,
        out bool result)
    {
        result = predicate.Code switch
        {
            BehaviorPreconditionCodes.SelfHasRelationshipToTarget => RelationshipPasses(world, binding.ActorId, binding.TargetId, predicate.A, predicate.B, predicate.X),
            BehaviorPreconditionCodes.RelatedEntityMatchesTarget => RelationshipPasses(world, binding.ActorId, binding.TargetId, predicate.A, predicate.B, predicate.X),
            BehaviorPreconditionCodes.TargetHasRelationshipToSelf => RelationshipPasses(world, binding.TargetId, binding.ActorId, predicate.A, predicate.B, predicate.X),
            BehaviorPreconditionCodes.RelationshipExists => RelationshipPasses(world, predicate.C, predicate.D, predicate.A, predicate.B, predicate.X),
            _ => false,
        };
        return predicate.Code is BehaviorPreconditionCodes.SelfHasRelationshipToTarget or
            BehaviorPreconditionCodes.RelatedEntityMatchesTarget or
            BehaviorPreconditionCodes.TargetHasRelationshipToSelf or
            BehaviorPreconditionCodes.RelationshipExists;
    }

    private static bool TryEvaluateEndpointRelationshipPredicate(
        WorldState world,
        in QueryPredicate predicate,
        in QueryBinding binding,
        out bool result)
    {
        result = predicate.Code switch
        {
            BehaviorPreconditionCodes.SelfHasOutgoingRelationship => SelfOutgoingRelationshipPasses(world, binding.ActorId, predicate.A, predicate.B, predicate.X),
            BehaviorPreconditionCodes.SelfHasOutgoingRelationshipToKind => SelfRelationshipToKindPasses(world, binding.ActorId, predicate.A, predicate.B, predicate.C, predicate.D, predicate.X),
            BehaviorPreconditionCodes.EndpointRelationshipCountBelow => EndpointRelationshipCountIsBelow(world, predicate, binding),
            _ => false,
        };
        return predicate.Code is BehaviorPreconditionCodes.SelfHasOutgoingRelationship or
            BehaviorPreconditionCodes.SelfHasOutgoingRelationshipToKind or
            BehaviorPreconditionCodes.EndpointRelationshipCountBelow;
    }

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

    public static bool TerrainCellCountInRadiusBelow(
        WorldState world,
        int originQ,
        int originR,
        int terrainId,
        int radius,
        int maximumExclusive) =>
        TerrainCellCountInRadiusBelow(
            world,
            PlaneCodes.Physical,
            originQ,
            originR,
            terrainId,
            radius,
            maximumExclusive);

    public static bool TerrainCellCountInRadiusBelow(
        WorldState world,
        int planeCode,
        int originQ,
        int originR,
        int terrainId,
        int radius,
        int maximumExclusive)
    {
        if (radius < 0 || maximumExclusive <= 0)
        {
            return false;
        }

        var count = 0;
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minR = Max(-radius, -dq - radius);
            var maxR = Min(radius, -dq + radius);
            for (var dr = minR; dr <= maxR; dr++)
            {
                var q = originQ + dq;
                var r = originR + dr;
                if (!world.CellIsKnown(planeCode, q, r) || world.CellTerrainId(planeCode, q, r) != terrainId)
                {
                    continue;
                }

                count++;
                if (count >= maximumExclusive)
                {
                    return false;
                }
            }
        }

        return true;
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
        float minimumStrength)
    {
        var relationshipId = world.FindRelationship(sourceId, targetId, relationshipKindId);
        return relationshipId > 0 &&
               RelationshipMeetsMinimum(world, relationshipId, hasMinimumStrength, minimumStrength);
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

    public static bool HasRelationshipOfKind(
        WorldState world,
        int entityId,
        int relationshipKindId,
        bool outgoing,
        float minimumStrength = 0f)
    {
        var found = false;
        ForEachRelatedEndpoint(
            world,
            entityId,
            relationshipKindId,
            minimumStrength,
            outgoing,
            (_, _) =>
            {
                found = true;
                return false;
            });
        return found;
    }

    public static bool AllRelationshipKindRequirementsPass(
        WorldState world,
        int entityId,
        int[] relationshipKindIds,
        float[] relationshipMinimumStrengths,
        int start,
        int count,
        bool outgoing)
    {
        var end = start + count;
        for (var filter = start; filter < end; filter++)
        {
            if (!HasRelationshipOfKind(
                    world,
                    entityId,
                    relationshipKindIds[filter],
                    outgoing,
                    relationshipMinimumStrengths[filter]))
            {
                return false;
            }
        }

        return true;
    }

    public static bool NoRelationshipKindRequirementsPresent(
        WorldState world,
        int entityId,
        int[] relationshipKindIds,
        int start,
        int count,
        bool outgoing)
    {
        var end = start + count;
        for (var filter = start; filter < end; filter++)
        {
            if (HasRelationshipOfKind(world, entityId, relationshipKindIds[filter], outgoing))
            {
                return false;
            }
        }

        return true;
    }

    public static bool AnyDirectionRelationshipKindRequirementsPass(
        WorldState world,
        int entityId,
        int[] relationshipKindIds,
        float[] relationshipMinimumStrengths,
        int start,
        int count)
    {
        var end = start + count;
        for (var filter = start; filter < end; filter++)
        {
            var relationshipKindId = relationshipKindIds[filter];
            var minimumStrength = relationshipMinimumStrengths[filter];
            if (!HasRelationshipOfKind(world, entityId, relationshipKindId, outgoing: true, minimumStrength: minimumStrength) &&
                !HasRelationshipOfKind(world, entityId, relationshipKindId, outgoing: false, minimumStrength: minimumStrength))
            {
                return false;
            }
        }

        return true;
    }

    public static int RelationshipEndpointCount(
        WorldState world,
        int entityId,
        int relationshipKindId,
        bool outgoing)
    {
        var count = 0;
        ForEachRelatedEndpoint(
            world,
            entityId,
            relationshipKindId,
            minimumStrength: 0f,
            outgoing,
            (_, _) =>
            {
                count++;
                return true;
            });
        return count;
    }

    public static float BestRelationshipStrengthBetween(
        WorldState world,
        int entityId,
        int targetEntityId,
        int relationshipKindId,
        int directionCode)
    {
        var best = 0f;
        if (directionCode is LowAltitudeVerbRelationshipDirectionCodes.Outgoing
            or LowAltitudeVerbRelationshipDirectionCodes.Either)
        {
            best = Max(best, RelationshipStrengthBetween(world, entityId, targetEntityId, relationshipKindId));
        }

        if (directionCode is LowAltitudeVerbRelationshipDirectionCodes.Incoming
            or LowAltitudeVerbRelationshipDirectionCodes.Either)
        {
            best = Max(best, RelationshipStrengthBetween(world, targetEntityId, entityId, relationshipKindId));
        }

        return best;
    }

    public static int FindRelationship(WorldState world, int sourceId, int targetId, int relationshipKindId) =>
        world.FindRelationship(sourceId, targetId, relationshipKindId);

    public static bool TryFindRelationship(
        WorldState world,
        int sourceId,
        int targetId,
        int relationshipKindId,
        int hasMinimumStrength,
        float minimumStrength,
        out int relationshipId)
    {
        relationshipId = 0;
        if (relationshipKindId <= 0)
        {
            return false;
        }

        var candidateId = world.FindRelationship(sourceId, targetId, relationshipKindId);
        if (candidateId == 0 ||
            !RelationshipMeetsMinimum(world, candidateId, hasMinimumStrength, minimumStrength))
        {
            return false;
        }

        relationshipId = candidateId;
        return true;
    }

    public static float RelationshipStrength(WorldState world, int relationshipId) =>
        world.RelationshipStrength(relationshipId);

    public static bool TryFirstRelationshipOfKind(
        WorldState world,
        int entityId,
        int relationshipKindId,
        bool outgoing,
        float minimumStrength,
        out int relationshipId)
    {
        relationshipId = 0;
        if (relationshipKindId <= 0)
        {
            return false;
        }

        var cursor = outgoing
            ? world.OutgoingRelationshipsOfKind(entityId, relationshipKindId)
            : world.IncomingRelationshipsOfKind(entityId, relationshipKindId);
        while (world.MoveNextRelationship(ref cursor, out relationshipId))
        {
            if (world.RelationshipStrength(relationshipId) >= minimumStrength)
            {
                return true;
            }
        }

        relationshipId = 0;
        return false;
    }

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
        var selfCultureId = world.EntityCultureId(entityId);
        var targetCultureId = world.EntityCultureId(targetId);
        if (selfCultureId <= 0 || targetCultureId <= 0)
        {
            return false;
        }

        var selfAtTarget = world.GetCellCultureInfluenceRaw(
            selfCultureId,
            planeCode,
            CurrentQ(world, targetId, planeCode),
            CurrentR(world, targetId, planeCode));
        var targetAtSelf = world.GetCellCultureInfluenceRaw(
            targetCultureId,
            planeCode,
            CurrentQ(world, entityId, planeCode),
            CurrentR(world, entityId, planeCode));
        return selfAtTarget >= selfMinimum && targetAtSelf >= targetMinimum;
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

    private static float RelationshipStrengthBetween(WorldState world, int sourceId, int targetId, int relationshipKindId)
    {
        var relationshipId = FindRelationship(world, sourceId, targetId, relationshipKindId);
        return relationshipId == 0 ? 0f : RelationshipStrength(world, relationshipId);
    }

    internal static bool SharedQueryMatchesCandidate(
        WorldState world,
        in SharedQuery query,
        in QueryBinding binding,
        int entityId)
    {
        return EntityIsActiveKind(world, entityId, query.KindId) &&
            entityId != binding.TargetId &&
            !binding.ReferenceAlreadyBound(entityId) &&
            QueryIdentityFiltersPass(world, query, entityId) &&
            QueryCultureMatches(world, query.CultureMatchCode, entityId, binding.TargetId) &&
            QueryRelationshipFiltersPass(world, query, entityId, query.WithoutOutgoingStart, query.WithoutOutgoingCount, outgoing: true) &&
            QueryRelationshipFiltersPass(world, query, entityId, query.WithoutIncomingStart, query.WithoutIncomingCount, outgoing: false) &&
            QueryRelatedToTargetMatches(world, query, binding, entityId) &&
            QueryAdjacencyMatches(world, query, binding, entityId);
    }

    private static bool QueryIdentityFiltersPass(WorldState world, in SharedQuery query, int entityId) =>
        SubtypeAllowed(query.SubtypeIds, query.SubtypeStart, query.SubtypeCount, world.EntitySubtypeId(entityId)) &&
        TagBitsPass(world.EntityTagBits(entityId), query.RequiredTagBits, query.WithoutTagBits) &&
        (query.CultureId == 0 || world.EntityCultureId(entityId) == query.CultureId);

    internal static bool TagBitsPass(int entityTagBits, int requiredTagBits, int withoutTagBits) =>
        (entityTagBits & requiredTagBits) == requiredTagBits &&
        (entityTagBits & withoutTagBits) == 0;

    private static bool QueryCultureMatches(WorldState world, int cultureMatchCode, int entityId, int targetId)
    {
        return cultureMatchCode switch
        {
            GeneratorCultureMatchCodes.SameAsTarget => world.EntityCultureId(entityId) == world.EntityCultureId(targetId),
            GeneratorCultureMatchCodes.DifferentFromTarget => world.EntityCultureId(entityId) != world.EntityCultureId(targetId),
            _ => true,
        };
    }

    private static bool QueryRelationshipFiltersPass(
        WorldState world,
        in SharedQuery query,
        int entityId,
        int start,
        int count,
        bool outgoing)
    {
        var filterIds = query.RelationshipKindFilterIds;
        if (filterIds is null)
        {
            return count == 0;
        }

        var end = start + count;
        for (var filter = start; filter < end; filter++)
        {
            if (HasRelationshipOfKind(world, entityId, filterIds[filter], outgoing))
            {
                return false;
            }
        }

        return true;
    }

    private static bool QueryAdjacencyMatches(
        WorldState world,
        in SharedQuery query,
        in QueryBinding binding,
        int entityId)
    {
        return query.AdjacentToTarget == 0 ||
            HexDistance(
                world.EntityPhysicalQ(entityId),
                world.EntityPhysicalR(entityId),
                binding.TargetOriginQ,
                binding.TargetOriginR) == 1;
    }

    private static bool EndpointRelationshipCountIsBelow(
        WorldState world,
        in QueryPredicate predicate,
        in QueryBinding binding)
    {
        var endpointId = predicate.B == BehaviorEndpointCodes.Target ? binding.TargetId : binding.ActorId;
        var count = DistinctRelationshipEndpointCount(world, endpointId, predicate.A, predicate.D, predicate.X);
        return count < predicate.C;
    }

    private static void PushBool(global::System.Span<bool> stack, ref int stackCount, bool value)
    {
        if (stackCount >= stack.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        stack[stackCount] = value;
        stackCount++;
    }

    private static bool PopBool(global::System.Span<bool> stack, ref int stackCount)
    {
        if (stackCount <= 0)
        {
            throw new global::System.InvalidOperationException();
        }

        stackCount--;
        return stack[stackCount];
    }

    private static int Abs(int value) => value < 0 ? -value : value;

    private static int Max(int left, int right) => left >= right ? left : right;

    private static int Min(int left, int right) => left <= right ? left : right;

    private static float Max(float left, float right) => left >= right ? left : right;
}
