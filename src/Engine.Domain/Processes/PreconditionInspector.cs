namespace Engine.Processes;

using Engine.Kinds;
using Engine.Resources;
using Engine.Wiring;

/// <summary>
/// Static process program-facing inspection of preconditions.
/// Used for validation, not runtime execution.
/// </summary>
public static class PreconditionInspector
{
    public static ProcessPreconditionAnalysis Analyze(BehaviorDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var blockers = new HashSet<AttributeRef>();
        var hasOpaqueProcessBranches = CollectProcessBlockers(
            definition.Precondition,
            definition.TargetKind,
            InferTargetKinds(definition),
            blockers);

        return new ProcessPreconditionAnalysis(blockers, hasOpaqueProcessBranches);
    }

    private static bool CollectProcessBlockers(
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
                        opaque |= CollectProcessBlockers(child, selfKind, targetKinds, blockers);
                    }

                    return opaque;
                }
            case OrPrecondition:
                return true;

            case SelfAttributeThreshold selfThreshold:
                if (selfThreshold.ProcessRelevant)
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
                if (locationResourceThreshold.ProcessRelevant)
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
        if (!threshold.ProcessRelevant)
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
        if (!precondition.ProcessRelevant)
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

public readonly record struct ProcessPreconditionAnalysis(
    IReadOnlySet<AttributeRef> ProcessBlockers,
    bool HasOpaqueProcessBranches);
