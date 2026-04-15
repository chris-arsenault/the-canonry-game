namespace Engine.Processes;

using Engine.Kinds;
using Engine.Wiring;
using Engine.Resources;

/// <summary>
/// Static planner-facing inspection of preconditions.
/// Used for validation, not runtime execution.
/// </summary>
public static class PreconditionInspector
{
    public static PlannerPreconditionAnalysis Analyze(ProcessDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var blockers = new HashSet<AttributeRef>();
        var hasOpaquePlannerBranches = CollectPlannerBlockers(
            definition.Precondition,
            definition.TargetKind,
            InferTargetKinds(definition),
            blockers);

        return new PlannerPreconditionAnalysis(blockers, hasOpaquePlannerBranches);
    }

    private static bool CollectPlannerBlockers(
        IPrecondition precondition,
        EntityKind selfKind,
        IReadOnlySet<EntityKind> targetKinds,
        HashSet<AttributeRef> blockers)
    {
        switch (precondition)
        {
            case AndPrecondition andPrecondition:
            {
                var opaque = false;
                foreach (var child in andPrecondition.Children)
                {
                    opaque |= CollectPlannerBlockers(child, selfKind, targetKinds, blockers);
                }

                return opaque;
            }
            case OrPrecondition:
                return true;
            
            case SelfAttributeThreshold selfThreshold:
                if (selfThreshold.PlannerRelevant)
                {
                    blockers.Add(AttributeRef.Self(selfThreshold.AttributeName, selfKind));
                }

                return false;
            case TargetAttributeThreshold targetThreshold:
                if (!targetThreshold.PlannerRelevant)
                {
                    return false;
                }

                foreach (var targetKind in targetKinds)
                {
                    blockers.Add(AttributeRef.Target(targetThreshold.AttributeName, targetKind));
                }

                return false;
            case SemanticDistanceThreshold semanticDistance:
                if (!semanticDistance.PlannerRelevant)
                {
                    return false;
                }

                foreach (var targetKind in targetKinds)
                {
                    blockers.Add(AttributeRef.Target(semanticDistance.AttributeName, targetKind));
                }

                return false;
            case RelatedEntityMatchesTarget relatedEntityMatchesTarget:
                if (!relatedEntityMatchesTarget.PlannerRelevant)
                {
                    return false;
                }

                foreach (var targetKind in targetKinds)
                {
                    blockers.Add(AttributeRef.TargetRelation(
                        relatedEntityMatchesTarget.RelationshipKind,
                        targetKind));
                }

                return false;
            case LocationResourceThreshold locationResourceThreshold:
                if (locationResourceThreshold.PlannerRelevant)
                {
                    blockers.Add(AttributeRef.Resource(locationResourceThreshold.ResourceType.ToDomainName()));
                }

                return false;
            default:
                return false;
        }
    }

    private static HashSet<EntityKind> InferTargetKinds(ProcessDefinition definition)
    {
        var targetKinds = definition.ProviderSelectors
            .Where(selector => selector.Scope == RefScope.Target && selector.Kind.HasValue)
            .Select(selector => selector.Kind!.Value)
            .ToHashSet();

        if (targetKinds.Count == 0)
        {
            targetKinds.Add(definition.TargetKind);
        }

        return targetKinds;
    }
}

public readonly record struct PlannerPreconditionAnalysis(
    IReadOnlySet<AttributeRef> PlannerBlockers,
    bool HasOpaquePlannerBranches);
