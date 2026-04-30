namespace Engine.Behaviors;

using Engine.Kinds;
using Engine.Resources;
using Engine.Wiring;

/// <summary>
/// Static behavior program-facing inspection of preconditions.
/// Used for validation, not runtime execution.
/// </summary>
public static class PreconditionInspector
{
    public static BehaviorPreconditionAnalysis Analyze(BehaviorDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var blockers = new HashSet<AttributeRef>();
        var hasOpaqueBehaviorBranches = CollectBehaviorBlockers(
            definition.Precondition,
            definition.TargetKind,
            InferTargetKinds(definition),
            blockers);

        return new BehaviorPreconditionAnalysis(blockers, hasOpaqueBehaviorBranches);
    }

    private static bool CollectBehaviorBlockers(
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
                        opaque |= CollectBehaviorBlockers(child, selfKind, targetKinds, blockers);
                    }

                    return opaque;
                }
            case OrPrecondition:
                return true;

            case SelfAttributeThreshold selfThreshold:
                if (selfThreshold.BehaviorRelevant)
                {
                    blockers.Add(AttributeRef.Self(selfThreshold.AttributeName, selfKind));
                }

                return false;
            case TargetAttributeThreshold targetThreshold:
                AddTargetAttributeBlockers(targetThreshold, targetKinds, blockers);
                return false;
            case RelatedEntityMatchesTarget relatedEntityMatchesTarget:
                AddTargetRelationBlockers(relatedEntityMatchesTarget, targetKinds, blockers);
                return false;
            case LocationResourceThreshold locationResourceThreshold:
                if (locationResourceThreshold.BehaviorRelevant)
                {
                    blockers.Add(AttributeRef.Resource(locationResourceThreshold.ResourceType.ToDomainName()));
                }

                return false;
            default:
                return false;
        }
    }

    private static void AddTargetAttributeBlockers(
        TargetAttributeThreshold threshold,
        IReadOnlySet<EntityKind> targetKinds,
        HashSet<AttributeRef> blockers)
    {
        if (!threshold.BehaviorRelevant)
        {
            return;
        }

        foreach (var targetKind in targetKinds)
        {
            blockers.Add(AttributeRef.Target(threshold.AttributeName, targetKind));
        }
    }

    private static void AddTargetRelationBlockers(
        RelatedEntityMatchesTarget precondition,
        IReadOnlySet<EntityKind> targetKinds,
        HashSet<AttributeRef> blockers)
    {
        if (!precondition.BehaviorRelevant)
        {
            return;
        }

        foreach (var targetKind in targetKinds)
        {
            blockers.Add(AttributeRef.TargetRelation(precondition.RelationshipKind, targetKind));
        }
    }

    private static HashSet<EntityKind> InferTargetKinds(BehaviorDefinition definition)
    {
        var targetKinds = definition.ProviderSelectors
            .Where(selector => selector.Kind.HasValue)
            .Select(selector => selector.Kind!.Value)
            .ToHashSet();

        if (targetKinds.Count == 0)
        {
            targetKinds.Add(definition.TargetKind);
        }

        return targetKinds;
    }
}

public readonly record struct BehaviorPreconditionAnalysis(
    IReadOnlySet<AttributeRef> BehaviorBlockers,
    bool HasOpaqueBehaviorBranches);
