namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Graph;

using K = IceRemembers.IceRemembersKinds;

/// <summary>
/// Ice Remembers story archetypes for the Chronicle Director. Every cast is
/// grounded in existing history: hunts start from real monster wars,
/// rivalries from pressed rival edges, teachings from real lineage bonds.
/// The director then biases the cast toward the plot and guarantees an
/// ending the chronicle can retell.
/// </summary>
public static class IceRemembersStoryArchetypes
{
    public static IReadOnlyList<StoryArchetypeConfig> Create() =>
    [
        TheHunt(),
        TheBloodRivalry(),
        TheTeaching(),
    ];

    private static StoryBiasConfig Bias(string actor, string target, string gambit, float bonus) =>
        new() { ActorRole = actor, TargetRole = target, Gambit = gambit, ScoreBonus = bonus };

    private static StorySustainConfig Sustain(string role, float delta) =>
        new() { Role = role, Attribute = "vitality", DeltaPerPulse = delta };

    private static StoryResolutionRelationshipConfig Cull(string source, string target, string kind) =>
        new() { SourceRole = source, TargetRole = target, RelationshipKind = kind, Strength = 0f, Polarity = RelationshipPolarity.Negative, CullInstead = true };

    private static StoryResolutionRelationshipConfig Create(string source, string target, string kind, float strength, RelationshipPolarity polarity) =>
        new() { SourceRole = source, TargetRole = target, RelationshipKind = kind, Strength = strength, Polarity = polarity };

    /// <summary>
    /// A hoarding monster already at war with a named penguin becomes the
    /// quarry of a hunt. The hunter is sustained while the story runs, the
    /// slaying is favored, and the tale ends either in the beast's fall or
    /// with the beast enduring and the hunt abandoned.
    /// </summary>
    private static StoryArchetypeConfig TheHunt() => new()
    {
        Acts = HuntActs(),
        Resolutions = HuntResolutions(),
        Id = "story_the_hunt",
        Name = "The Hunt",
        Description = "A much-provoked beast, a named hunter, and an ending one way or the other.",
        PrimaryRoleRef = "$quarry",
        PrimaryRole = new SemanticGeneratorTarget
        {
            Kind = K.Monster,
            RequiredOutgoingRelationshipRequirements =
            [
                new SemanticGeneratorRelationshipRequirement { RelationshipKind = "holds", MinimumStrength = 0.5f },
            ],
        },
        SupportingRoles =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$hunter",
                Kind = K.Penguin,
                Query = new SemanticEntityQuery
                {
                    RelatedToTargetRelationship = "at_war_with",
                    RelatedToTargetMinStrength = 0.3f,
                },
            },
        ],
        MaxActive = 3,
        CastingCadenceTicks = 60,
    };

    private static List<StoryActConfig> HuntActs() =>
    [
        new StoryActConfig
        {
            DeadlineTicks = 300,
            Bias = [Bias("$hunter", "$quarry", "penguin_binds_what_was_loosed", 2f), Bias("$hunter", "$quarry", "penguin_slays_monster", 3f)],
            Sustain = [Sustain("$hunter", 0.02f)],
        },
        new StoryActConfig
        {
            DeadlineTicks = 500,
            AdvanceWhen = new StoryConditionConfig { Kind = StoryConditionKind.RoleHistorical, RoleA = "$quarry" },
            Bias = [Bias("$hunter", "$quarry", "penguin_slays_monster", 5f)],
            Sustain = [Sustain("$hunter", 0.02f)],
        },
    ];

    private static List<StoryResolutionConfig> HuntResolutions() =>
    [
        new StoryResolutionConfig
        {
            OutcomeId = "hunt_fulfilled",
            When = new StoryConditionConfig { Kind = StoryConditionKind.RoleHistorical, RoleA = "$quarry" },
            Relationships = [Cull("$hunter", "$quarry", "at_war_with")],
        },
        new StoryResolutionConfig
        {
            OutcomeId = "the_beast_endures",
            Weight = 1,
            Relationships =
            [
                Cull("$hunter", "$quarry", "at_war_with"),
                Create("$quarry", "$hunter", "preys_on", 0.6f, RelationshipPolarity.Negative),
            ],
        },
    ];

    /// <summary>
    /// Two factions with a pressed rivalry are cast as blood rivals. The
    /// director favors declaration and settlement between exactly this pair,
    /// and the story ends settled or curdled into lasting betrayal.
    /// </summary>
    private static StoryArchetypeConfig TheBloodRivalry() => new()
    {
        Acts = RivalryActs(),
        Resolutions = RivalryResolutions(),
        Id = "story_blood_rivalry",
        Name = "The Blood Rivalry",
        Description = "A rivalry pressed until it must resolve: settled ground or lasting hatred.",
        PrimaryRoleRef = "$challenger",
        PrimaryRole = new SemanticGeneratorTarget
        {
            Kind = K.Faction,
            RequiredOutgoingRelationshipRequirements =
            [
                new SemanticGeneratorRelationshipRequirement { RelationshipKind = "rival_of", MinimumStrength = 0.45f },
            ],
        },
        SupportingRoles =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$rival",
                Kind = K.Faction,
                Query = new SemanticEntityQuery
                {
                    RelatedToTargetRelationship = "rival_of",
                    RelatedToTargetMinStrength = 0.45f,
                },
            },
        ],
        MaxActive = 3,
        CastingCadenceTicks = 80,
    };

    private static List<StoryActConfig> RivalryActs() =>
    [
        new StoryActConfig
        {
            DeadlineTicks = 400,
            AdvanceWhen = new StoryConditionConfig
            {
                Kind = StoryConditionKind.RolePairRelationship,
                RoleA = "$challenger",
                RoleB = "$rival",
                RelationshipKind = "at_war_with",
                MinStrength = 0.5f,
            },
            Bias = [Bias("$challenger", "$rival", "faction_declares_territorial_war", 2.5f), Bias("$rival", "$challenger", "faction_declares_territorial_war", 2.5f)],
        },
        new StoryActConfig
        {
            DeadlineTicks = 750,
            Bias = [Bias("$challenger", "$rival", "faction_sues_for_peace", 1.5f), Bias("$rival", "$challenger", "faction_sues_for_peace", 1.5f)],
        },
    ];

    private static List<StoryResolutionConfig> RivalryResolutions() =>
    [
        new StoryResolutionConfig
        {
            OutcomeId = "rivalry_settled",
            Weight = 2,
            Relationships =
            [
                Cull("$challenger", "$rival", "rival_of"),
                Cull("$rival", "$challenger", "rival_of"),
                Cull("$challenger", "$rival", "at_war_with"),
                Cull("$rival", "$challenger", "at_war_with"),
            ],
        },
        new StoryResolutionConfig
        {
            OutcomeId = "rivalry_becomes_hatred",
            Weight = 1,
            Relationships =
            [
                Create("$challenger", "$rival", "betrayed_by", 0.6f, RelationshipPolarity.Negative),
                Create("$rival", "$challenger", "betrayed_by", 0.6f, RelationshipPolarity.Negative),
                Cull("$challenger", "$rival", "at_war_with"),
                Cull("$rival", "$challenger", "at_war_with"),
            ],
        },
    ];

    /// <summary>
    /// A student bound to a living master. The pair sustain each other while
    /// the teaching runs; when the master dies the teaching lives on in the
    /// student, or the paths quietly diverge.
    /// </summary>
    private static StoryArchetypeConfig TheTeaching() => new()
    {
        Id = "story_the_teaching",
        Name = "The Teaching",
        Description = "Master and student, and what survives of one inside the other.",
        PrimaryRoleRef = "$student",
        PrimaryRole = new SemanticGeneratorTarget
        {
            Kind = K.Penguin,
            RequiredOutgoingRelationshipRequirements =
            [
                new SemanticGeneratorRelationshipRequirement { RelationshipKind = "taught_by", MinimumStrength = 0.5f },
            ],
        },
        SupportingRoles =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$master",
                Kind = K.Penguin,
                Query = new SemanticEntityQuery
                {
                    RelatedToTargetRelationship = "taught_by",
                    RelatedToTargetIncoming = true,
                    RelatedToTargetMinStrength = 0.5f,
                },
            },
        ],
        Acts =
        [
            new StoryActConfig
            {
                DeadlineTicks = 500,
                Bias = [Bias("$master", "$student", "penguin_mends_another", 1.5f), Bias("$student", "$master", "penguin_mends_another", 1.5f)],
                Sustain = [Sustain("$student", 0.015f), Sustain("$master", 0.015f)],
            },
            new StoryActConfig
            {
                DeadlineTicks = 500,
                AdvanceWhen = new StoryConditionConfig { Kind = StoryConditionKind.RoleHistorical, RoleA = "$master" },
                Sustain = [Sustain("$student", 0.015f)],
            },
        ],
        Resolutions =
        [
            new StoryResolutionConfig
            {
                OutcomeId = "the_teaching_lives",
                When = new StoryConditionConfig { Kind = StoryConditionKind.RoleHistorical, RoleA = "$master" },
            },
            new StoryResolutionConfig
            {
                OutcomeId = "paths_diverge",
                Weight = 1,
                Relationships = [Cull("$student", "$master", "taught_by")],
            },
        ],
        MaxActive = 4,
        CastingCadenceTicks = 100,
    };
}
