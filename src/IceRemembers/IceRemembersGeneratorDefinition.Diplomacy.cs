namespace IceRemembers;

using Engine.Domain.Config;

using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;

// The diplomacy track (track fidelity pass: 3.4% of authorship, 0.5% of
// execution, 0.3% of entities - a stub, not an economy). Mediation gives
// wars a third act beyond exhaustion and death: an accord cools the war
// hard, and the existing sue-for-peace gambit can then conclude it.
// Envoys give diplomacy a standing peacetime loop off cross-culture trade.
public static partial class IceRemembersGeneratorDefinition
{
    private const float AccordWarStrengthSpend = -0.50f;
    private const float AccordRivalrySpend = -0.30f;
    private const float EnvoysMinimumTradeStrength = 0.60f;
    private const float EnvoysTradeRouteSpend = -0.30f;

    private static SemanticGeneratorConfig TheAccordIsSealed() => new()
    {
        Id = "the_accord_is_sealed",
        Name = "The Accord Is Sealed",
        Description = "A mediator stands between two warring banners and the ice witnesses the terms. The accord does not always end the war - but it always takes the heat out of it, and cooled wars can finally be concluded.",
        CausalLimit = "Depends on an outgoing at-war edge with the foe bound through it; spends half the war's strength in both directions, so a war survives at most one sealing before it can be sued to peace.",
        Target = TargetWithOutgoing(K.Faction, Rel.AtWarWith),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$foe",
                Kind = K.Faction,
                Query = new SemanticEntityQuery
                {
                    RelatedToTargetRelationship = L(Rel.AtWarWith),
                    RelatedToTargetIncoming = true,
                    RelatedToTargetMinStrength = 0.2f,
                },
            },
        ],
        Produces =
        [
            Required("$accord", K.Canon, "accord", "$target", "$target", cultureSource: ProducedCultureSource.World),
            Required("$mediator", K.Penguin, "witness", "$target", "$target", cultureSource: ProducedCultureSource.AnyParticipant),
        ],
        Relationships =
        [
            R("$mediator", "$accord", Rel.Holds, 1f),
            R("$accord", "$target", Rel.Memorializes, 0.80f),
            R("$accord", "$foe", Rel.Memorializes, 0.80f),
        ],
        Effects =
        [
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.AtWarWith),
                AccordWarStrengthSpend,
                GeneratorRelationshipInstanceSelection.TriggerRelationship,
                includeReciprocal: true),
            // Terms bury grievance too: without this, sealed pairs simply
            // re-declared and mediation raised the war count it was built
            // to lower.
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.RivalOf),
                AccordRivalrySpend,
                GeneratorRelationshipInstanceSelection.AllTargetRelationships,
                includeReciprocal: true),
        ],
        Triggers = TriggerOn(K.Faction),
        FireWeight = 6,
    };

    private static SemanticGeneratorConfig EnvoysCrossTheIce() => new()
    {
        Id = "envoys_cross_the_ice",
        Name = "Envoys Cross the Ice",
        Description = "A trade route that has carried enough between two peoples starts carrying words. An envoy walks it openly and an embassy stone stands where the routes meet, belonging to neither side and known to both.",
        CausalLimit = "Depends on a strong trade route to a settlement of another culture; spends the route's strength, so the road must be rebuilt before it carries another embassy.",
        Target = TargetWithStrongOutgoing(K.Settlement, Rel.TradeRoute, EnvoysMinimumTradeStrength),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$partner",
                Kind = K.Settlement,
                Query = new SemanticEntityQuery
                {
                    RelatedToTargetRelationship = L(Rel.TradeRoute),
                    RelatedToTargetMinStrength = 0.4f,
                    TargetCultureMatch = QueryTargetCultureMatch.DifferentFromTarget,
                },
            },
        ],
        Produces =
        [
            Required("$envoy", K.Penguin, "witness", "$target", "$target", cultureSource: ProducedCultureSource.AnyParticipant),
            Required("$embassy", K.PointOfInterest, "shoal", "$target", "$target", cultureSource: ProducedCultureSource.World),
        ],
        Relationships =
        [
            R("$envoy", "$embassy", Rel.HomeBase, 0.85f),
            R("$embassy", "$target", Rel.Memorializes, 0.80f),
            R("$embassy", "$partner", Rel.Memorializes, 0.75f),
        ],
        Effects =
        [
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.TradeRoute),
                EnvoysTradeRouteSpend,
                GeneratorRelationshipInstanceSelection.TriggerRelationship,
                includeReciprocal: true),
        ],
        Triggers = TriggerOn(K.Settlement),
        FireWeight = 6,
    };
}
