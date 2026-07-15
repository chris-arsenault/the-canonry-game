namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Graph;

using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;

/// <summary>
/// War-saga generators: the background events that make wars cost lives and
/// alliances mean something. Both use the related-to-target binding query so
/// the bundle binds entities already storied with the warring faction.
/// </summary>
public static partial class IceRemembersGeneratorDefinition
{
    private const float AlliesAnswerMinimumWarStrength = 0.55f;
    private const float AlliesAnswerMinimumAllianceStrength = 0.45f;
    private const float WarTollMinimumWarStrength = 0.5f;
    private const float WarTollWarStrengthSpend = -0.18f;

    private static SemanticGeneratorConfig AlliesAnswerTheCall() => new()
    {
        Id = "allies_answer_the_call",
        Name = "Allies Answer the Call",
        Description = "A faction fighting a hot war calls on a sworn ally, and the ally enters the war. Blocs form from accords that were actually kept.",
        CausalLimit = "Requires a strong outgoing at-war relationship on the target and a bound ally and enemy already related to the target; creates war edges only between those existing factions.",
        Target = TargetWithStrongOutgoing(K.Faction, Rel.AtWarWith, AlliesAnswerMinimumWarStrength),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$ally",
                Kind = K.Faction,
                Query = new SemanticEntityQuery
                {
                    RelatedToTargetRelationship = L(Rel.AlliedWith),
                    RelatedToTargetMinStrength = AlliesAnswerMinimumAllianceStrength,
                },
            },
            new SemanticGeneratorBindingTarget
            {
                Ref = "$enemy",
                Kind = K.Faction,
                Query = new SemanticEntityQuery
                {
                    RelatedToTargetRelationship = L(Rel.AtWarWith),
                    RelatedToTargetMinStrength = AlliesAnswerMinimumWarStrength,
                    WithoutOutgoingRelationships = new HashSet<string>(
                        [L(Rel.AlliedWith)], StringComparer.Ordinal),
                },
            },
        ],
        Produces = [],
        Relationships =
        [
            R("$ally", "$enemy", Rel.AtWarWith, 0.7f),
            R("$enemy", "$ally", Rel.AtWarWith, 0.7f),
        ],
        Triggers = TriggerOn(K.Faction),
        PulseWeight = 10,
    };

    private static SemanticGeneratorConfig WarTakesItsToll() => new()
    {
        Id = "war_takes_its_toll",
        Name = "War Takes Its Toll",
        Description = "A hot war claims a named member of the warring faction. The fallen are memorialized by a relic, and the war burns lower for the loss — exhausted wars can finally be peaced.",
        CausalLimit = "Requires a strong outgoing at-war relationship and a named member bound to the target; retires only that member and spends the trigger war strength.",
        Target = TargetWithStrongOutgoing(K.Faction, Rel.AtWarWith, WarTollMinimumWarStrength),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$casualty",
                Kind = K.Penguin,
                Query = new SemanticEntityQuery
                {
                    RelatedToTargetRelationship = L(Rel.MemberOf),
                    RelatedToTargetMinStrength = 0.3f,
                },
            },
        ],
        Produces =
        [
            Required("$memorial", K.Artifact, "relic", "$casualty", "$target"),
        ],
        Relationships =
        [
            R("$memorial", "$casualty", Rel.Memorializes, 0.85f),
            R("$memorial", "$target", Rel.ClaimedBy, 0.6f),
        ],
        Effects =
        [
            new ErodeCultureAtTargetEffect(radius: 4, amountPerCell: 20f),
            new SetBindingHistoricalEffect("$casualty"),
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.AtWarWith),
                WarTollWarStrengthSpend,
                GeneratorRelationshipInstanceSelection.TriggerRelationship,
                includeReciprocal: true),
        ],
        Triggers = TriggerOn(K.Faction),
        PulseWeight = 14,
    };
}
