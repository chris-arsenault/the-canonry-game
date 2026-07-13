namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Kinds;

using Rel = IceRemembers.IceRemembersRelationship;

public static partial class IceRemembersGeneratorDefinition
{
    private static SemanticGeneratorTarget TargetWithStrongOutgoingWithoutIncomingOrOutgoing(
        EntityKind kind,
        Rel required,
        float minimumStrength,
        Rel withoutIncoming,
        Rel withoutOutgoing) =>
        new()
        {
            Kind = kind,
            RequiredOutgoingRelationshipRequirements =
            [
                new SemanticGeneratorRelationshipRequirement
                {
                    RelationshipKind = L(required),
                    MinimumStrength = minimumStrength,
                },
            ],
            WithoutIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(withoutIncoming) },
            WithoutOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(withoutOutgoing) },
        };

    private static SemanticGeneratorTarget TargetWithStrongOutgoingAndWithoutIncoming(
        EntityKind kind,
        Rel required,
        float minimumStrength,
        Rel without) =>
        new()
        {
            Kind = kind,
            RequiredOutgoingRelationshipRequirements =
            [
                new SemanticGeneratorRelationshipRequirement
                {
                    RelationshipKind = L(required),
                    MinimumStrength = minimumStrength,
                },
            ],
            WithoutIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(without) },
        };

    private static SemanticGeneratorTarget TargetWithStrongOutgoing(
        EntityKind kind,
        Rel required,
        float minimumStrength) =>
        new()
        {
            Kind = kind,
            RequiredOutgoingRelationshipRequirements =
            [
                new SemanticGeneratorRelationshipRequirement
                {
                    RelationshipKind = L(required),
                    MinimumStrength = minimumStrength,
                },
            ],
        };
}
