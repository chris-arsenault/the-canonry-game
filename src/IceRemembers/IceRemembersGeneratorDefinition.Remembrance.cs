namespace IceRemembers;

using Engine.Domain.Config;

using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;

/// <summary>
/// Remembrance: the world's memory machinery answering the chronicle system.
/// These generators fire only on entities the Chronicle Director vouches for
/// (chronicle-heat and legendary-resolution gates), so remembrance flows
/// through ordinary world authorship - the director itself writes nothing.
/// Remembrance-stones rise for the cast of blazing stories; tales that resolve
/// at legendary heat enter a culture's canon as sagas.
/// </summary>
public static partial class IceRemembersGeneratorDefinition
{
    private const float IceTakesNoticeHeatMin = 6f;

    private static IReadOnlyList<SemanticGeneratorConfig> RemembranceGenerators() =>
    [
        TheIceTakesNotice(),
        LegendEntersTheCanon(),
    ];

    private const int LegendEntersTheCanonWindowTicks = 300;
    private const int SagaJurisdictionRadius = 2;

    // Remembrance falls on every actor kind alike (authorship parity,
    // actor-parity audit 2026-08-01): each builder authored once, emitted
    // per kind. Penguin emissions keep the original ids.
    private static SemanticGeneratorConfig TheIceTakesNotice() =>
        IceTakesNoticeFor("the_ice_takes_notice", K.Penguin);

    private static SemanticGeneratorConfig TheIceTakesNoticeOrca() =>
        IceTakesNoticeFor("the_ice_takes_notice_orca", K.Orca);

    private static SemanticGeneratorConfig IceTakesNoticeFor(string id, EntityKind actorKind) => new()
    {
        Id = id,
        Name = "The Ice Takes Notice",
        Description = "A story burning hot enough does not go unmarked. A remembrance-stone is raised for the one at its center - memory for the living, not the dead.",
        CausalLimit = "Depends on the target being cast in a running chronicle at high heat; creates one memorial artifact bound to the target.",
        Target = new SemanticGeneratorTarget
        {
            Kind = actorKind,
            ChronicleHeatMin = IceTakesNoticeHeatMin,
            WithoutIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(Rel.Memorializes) },
        },
        Produces =
        [
            Required("$stone", K.Artifact, "seal", "$target", "$target"),
        ],
        Relationships =
        [
            R("$stone", "$target", Rel.Memorializes, 0.75f),
        ],
        Triggers = TriggerOn(actorKind),
        // Remembrance-stones are rare by gate (chronicle heat), not by lottery:
        // the eligibility windows are brief blazing-story moments, so when
        // one arrives the ice must not lose the fire slot to routine growth.
        FireWeight = 50,
    };

    private static SemanticGeneratorConfig LegendEntersTheCanon() =>
        LegendEntersTheCanonFor("legend_enters_the_canon", K.Penguin);

    private static SemanticGeneratorConfig LegendEntersTheCanonOrca() =>
        LegendEntersTheCanonFor("legend_enters_the_canon_orca", K.Orca);

    private static SemanticGeneratorConfig LegendEntersTheCanonFor(string id, EntityKind actorKind) => new()
    {
        Id = id,
        Name = "The Legend Enters the Canon",
        Description = "A tale that resolved at legendary heat is not allowed to fade. The tellers set it down; it becomes a saga - a canon of the culture that lived it - and the one who carried the tale is remembered inside it.",
        CausalLimit = "Depends on the target having recently resolved out of a legendary chronicle; creates one saga canon claiming ground near the legend.",
        Target = new SemanticGeneratorTarget
        {
            Kind = actorKind,
            ChronicleLegendaryWithinTicks = LegendEntersTheCanonWindowTicks,
            WithoutIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(Rel.ClaimedBy) },
        },
        Produces =
        [
            ProducedCanon("$saga", "saga", "$target", culture: null, inheritCultureFrom: "$target"),
        ],
        Effects =
        [
            new ClaimSemanticJurisdictionEffect("$saga", SagaJurisdictionRadius),
        ],
        Relationships =
        [
            R("$saga", "$target", Rel.Memorializes, 0.90f),
            R("$saga", "$target", Rel.ClaimedBy, 0.70f),
        ],
        Triggers = TriggerOn(actorKind),
        FireWeight = 6,
    };
}
