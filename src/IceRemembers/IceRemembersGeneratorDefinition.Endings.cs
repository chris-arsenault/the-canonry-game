namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Graph;

using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;

/// <summary>
/// Ending generators: the authored acts that carry durable things out of
/// the zeitgeist. Seals shatter when the peace they bound breaks, a
/// faction's claimed relics are sealed into its memorial wall, a held tome
/// is read into practiced knowledge, and an instrument left at a devoted
/// site is spent into the rite. Every ending transforms — the graph keeps
/// the name.
/// </summary>
public static partial class IceRemembersGeneratorDefinition
{
    private const float SealShattersMinimumWarStrength = 0.5f;
    private const float MemorialWallMinimumClaimStrength = 0.4f;
    // 0.6 left the rite nearly silent (3 fires across 3x100k); devotion
    // rarely climbs that high before the instrument wanders on.
    private const float OfferingSpentMinimumOfferingStrength = 0.4f;

    private static SemanticGeneratorTarget TargetWithStrongIncoming(
        EntityKind kind,
        Rel required,
        float minimumStrength) =>
        new()
        {
            Kind = kind,
            RequiredIncomingRelationshipRequirements =
            [
                new SemanticGeneratorRelationshipRequirement
                {
                    RelationshipKind = L(required),
                    MinimumStrength = minimumStrength,
                },
            ],
        };

    private static IReadOnlyList<SemanticGeneratorConfig> EndingsGenerators() =>
    [
        TheSealShatters(),
        OneWordYields(),
        MemorialsJoinTheWall(),
        TheTomeIsRead(),
        TheOfferingIsSpent(),
    ];

    private static SemanticGeneratorConfig TheSealShatters() => new()
    {
        Id = "the_seal_shatters",
        Name = "The Seal Shatters",
        Description = "A faction that marches back to war breaks the seal that closed its last one. The shards go into the record; the account is open again.",
        CausalLimit = "Depends on a strong renewed at-war relationship and a same-culture peace seal; marks the seal historical, which removes the binding from the pool.",
        Target = TargetWithStrongOutgoing(K.Faction, Rel.AtWarWith, SealShattersMinimumWarStrength),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$seal",
                Kind = K.Artifact,
                Query = new SemanticEntityQuery
                {
                    Subtypes = new HashSet<string>(StringComparer.Ordinal) { "seal" },
                    TargetCultureMatch = QueryTargetCultureMatch.SameAsTarget,
                },
            },
        ],
        Produces = [],
        Effects =
        [
            new SetBindingHistoricalEffect("$seal"),
            new DemoteBindingStageEffect("$target"),
        ],
        Triggers = TriggerOn(K.Faction),
        PulseWeight = 6,
        CadenceTicks = 2000,
        CadenceJitterTicks = 500,
    };

    private static SemanticGeneratorConfig OneWordYields() => new()
    {
        Id = "one_word_yields",
        Name = "One Word Yields",
        Description = "Two accords of one culture cannot hold the same ground. The elder word is read a last time, honored, and closed - the living accord keeps the jurisdiction.",
        CausalLimit = "Depends on two same-culture accords existing at once; marks one historical, which removes the duplicate from the pool.",
        Target = new SemanticGeneratorTarget
        {
            Kind = K.Canon,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "accord" },
        },
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$elder",
                Kind = K.Canon,
                Query = new SemanticEntityQuery
                {
                    Subtypes = new HashSet<string>(StringComparer.Ordinal) { "accord" },
                    TargetCultureMatch = QueryTargetCultureMatch.SameAsTarget,
                },
            },
        ],
        Produces = [],
        Effects =
        [
            new SetBindingHistoricalEffect("$elder"),
        ],
        Triggers = TriggerOn(K.Canon),
        PulseWeight = 4,
        CadenceTicks = 3000,
        CadenceJitterTicks = 800,
    };

    private static SemanticGeneratorConfig MemorialsJoinTheWall() => new()
    {
        Id = "memorials_join_the_wall",
        Name = "Memorials Join the Wall",
        Description = "A faction rich in claimed relics seals two of them into its memorial wall. The dead keep their names in stone; the relics leave the living world.",
        CausalLimit = "Depends on two distinct claimed relics bound to the target; marks both historical, which shrinks its own future pool.",
        Target = TargetWithOutgoing(K.Faction, Rel.MemberOf),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$first",
                Kind = K.Artifact,
                Query = new SemanticEntityQuery
                {
                    Subtypes = new HashSet<string>(StringComparer.Ordinal) { "relic" },
                    RelatedToTargetRelationship = L(Rel.ClaimedBy),
                    RelatedToTargetMinStrength = MemorialWallMinimumClaimStrength,
                },
            },
            new SemanticGeneratorBindingTarget
            {
                Ref = "$second",
                Kind = K.Artifact,
                Query = new SemanticEntityQuery
                {
                    Subtypes = new HashSet<string>(StringComparer.Ordinal) { "relic" },
                    RelatedToTargetRelationship = L(Rel.ClaimedBy),
                    RelatedToTargetMinStrength = MemorialWallMinimumClaimStrength,
                },
            },
        ],
        Produces = [],
        Effects =
        [
            new SetBindingHistoricalEffect("$first"),
            new SetBindingHistoricalEffect("$second"),
        ],
        Triggers = TriggerOn(K.Faction),
        PulseWeight = 5,
        CadenceTicks = 1200,
        CadenceJitterTicks = 300,
    };

    private static SemanticGeneratorConfig TheTomeIsRead() => new()
    {
        Id = "the_tome_is_read",
        Name = "The Tome Is Read",
        Description = "A held tome gives up what it knows. The reading becomes a living practice; the pages, having said everything, pass into the record.",
        CausalLimit = "Depends on a penguin holding a tome; marks the tome historical, which removes the binding from the pool.",
        Target = TargetWithOutgoing(K.Penguin, Rel.Holds),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$tome",
                Kind = K.Artifact,
                Query = new SemanticEntityQuery
                {
                    Subtypes = new HashSet<string>(StringComparer.Ordinal) { "tome" },
                    RelatedToTargetRelationship = L(Rel.Holds),
                    RelatedToTargetIncoming = true,
                },
            },
        ],
        Produces =
        [
            Required("$practice", K.Ability, "technique", "$target", "$target"),
        ],
        Relationships =
        [
            R("$practice", "$target", Rel.PracticedBy, 0.8f),
        ],
        Effects =
        [
            new SetBindingHistoricalEffect("$tome"),
        ],
        Triggers = TriggerOn(K.Penguin),
        PulseWeight = 6,
        CadenceTicks = 1600,
        CadenceJitterTicks = 400,
    };

    private static SemanticGeneratorConfig TheOfferingIsSpent() => new()
    {
        Id = "the_offering_is_spent",
        Name = "The Offering Is Spent",
        Description = "An instrument left at a devoted site is played once more and given to the water. The rite keeps its sound; the instrument keeps only its name.",
        CausalLimit = "Depends on strong incoming devotion and an instrument adjacent to the site; marks the instrument historical.",
        Target = TargetWithStrongIncoming(K.PointOfInterest, Rel.OfferedTo, OfferingSpentMinimumOfferingStrength),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$instrument",
                Kind = K.Artifact,
                Query = new SemanticEntityQuery
                {
                    Subtypes = new HashSet<string>(StringComparer.Ordinal) { "instrument" },
                    AdjacentToTarget = true,
                },
            },
        ],
        Produces = [],
        Effects =
        [
            new SetBindingHistoricalEffect("$instrument"),
        ],
        Triggers = TriggerOn(K.PointOfInterest),
        PulseWeight = 5,
        // Requires a wandering instrument to stand beside the chosen
        // devoted site on the attempt tick - a rare alignment. At cadence
        // 2000 that gave ~10 attempts and ~0.3 fires per 100k; the shorter
        // clock buys the rare geometry more chances while the rite stays
        // an uncommon ending.
        CadenceTicks = 800,
        CadenceJitterTicks = 200,
    };
}
