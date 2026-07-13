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
public static class IceRemembersChronicleArchetypes
{
    public static IReadOnlyList<ChronicleArchetypeConfig> Create() =>
    [
        TheHunt(),
        TheBloodRivalry(),
        TheTeaching(),
        TheFlourishingRoad(),
        TheHauntedRun(),
        TheLivingPractice(),
        TheTerrorOfTheCoasts(),
    ];

    private static ChronicleBiasConfig Bias(string actor, string target, string? gambit, float bonus) =>
        new() { ActorRole = actor, TargetRole = target, Gambit = gambit, ScoreBonus = bonus };

    private static ChronicleSustainConfig Sustain(string role, float delta, string attribute = "vitality") =>
        new() { Role = role, Attribute = attribute, DeltaPerPulse = delta };

    private static ChronicleResolutionRelationshipConfig Cull(string source, string target, string kind) =>
        new() { SourceRole = source, TargetRole = target, RelationshipKind = kind, Strength = 0f, Polarity = RelationshipPolarity.Negative, CullInstead = true };

    private static ChronicleResolutionRelationshipConfig Create(string source, string target, string kind, float strength, RelationshipPolarity polarity) =>
        new() { SourceRole = source, TargetRole = target, RelationshipKind = kind, Strength = strength, Polarity = polarity };

    /// <summary>
    /// A hoarding monster already at war with a named penguin becomes the
    /// quarry of a hunt. The hunter is sustained while the story runs, the
    /// slaying is favored, and the tale ends either in the beast's fall or
    /// with the beast enduring and the hunt abandoned.
    /// </summary>
    private static ChronicleArchetypeConfig TheHunt() => new()
    {
        Acts = HuntActs(),
        Resolutions = HuntResolutions(),
        Id = "the_hunt",
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
        MaxActive = 1,
        CastingCadenceTicks = 400,
    };

    private static List<ChronicleActConfig> HuntActs() =>
    [
        new ChronicleActConfig
        {
            DeadlineTicks = 300,
            Bias = [Bias("$hunter", "$quarry", "penguin_binds_what_was_loosed", 2f), Bias("$hunter", "$quarry", "penguin_slays_monster", 3f)],
            Sustain = [Sustain("$hunter", 0.02f)],
        },
        new ChronicleActConfig
        {
            DeadlineTicks = 500,
            AdvanceWhen = new ChronicleConditionConfig { Kind = ChronicleConditionKind.RoleHistorical, RoleA = "$quarry" },
            Bias = [Bias("$hunter", "$quarry", "penguin_slays_monster", 5f)],
            Sustain = [Sustain("$hunter", 0.02f)],
        },
    ];

    private static List<ChronicleResolutionConfig> HuntResolutions() =>
    [
        new ChronicleResolutionConfig
        {
            OutcomeId = "hunt_fulfilled",
            When = new ChronicleConditionConfig { Kind = ChronicleConditionKind.RoleHistorical, RoleA = "$quarry" },
            Relationships = [Cull("$hunter", "$quarry", "at_war_with")],
        },
        new ChronicleResolutionConfig
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
    private static ChronicleArchetypeConfig TheBloodRivalry() => new()
    {
        Acts = RivalryActs(),
        Resolutions = RivalryResolutions(),
        Id = "blood_rivalry",
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
        MaxActive = 1,
        CastingCadenceTicks = 500,
    };

    private static List<ChronicleActConfig> RivalryActs() =>
    [
        new ChronicleActConfig
        {
            DeadlineTicks = 1250,
            AdvanceWhen = new ChronicleConditionConfig
            {
                Kind = ChronicleConditionKind.RolePairRelationship,
                RoleA = "$challenger",
                RoleB = "$rival",
                RelationshipKind = "at_war_with",
                MinStrength = 0.5f,
            },
            Bias = [Bias("$challenger", "$rival", "faction_declares_territorial_war", 2.5f), Bias("$rival", "$challenger", "faction_declares_territorial_war", 2.5f)],
        },
        new ChronicleActConfig
        {
            DeadlineTicks = 3750,
            Bias = [Bias("$challenger", "$rival", "faction_sues_for_peace", 1.5f), Bias("$rival", "$challenger", "faction_sues_for_peace", 1.5f)],
        },
    ];

    private static List<ChronicleResolutionConfig> RivalryResolutions() =>
    [
        new ChronicleResolutionConfig
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
        new ChronicleResolutionConfig
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
    private static ChronicleArchetypeConfig TheTeaching() => new()
    {
        Id = "the_teaching",
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
            new ChronicleActConfig
            {
                DeadlineTicks = 500,
                Bias = [Bias("$master", "$student", "penguin_mends_another", 1.5f), Bias("$student", "$master", "penguin_mends_another", 1.5f)],
                Sustain = [Sustain("$student", 0.015f), Sustain("$master", 0.015f)],
            },
            new ChronicleActConfig
            {
                DeadlineTicks = 500,
                AdvanceWhen = new ChronicleConditionConfig { Kind = ChronicleConditionKind.RoleHistorical, RoleA = "$master" },
                Sustain = [Sustain("$student", 0.015f)],
            },
        ],
        Resolutions =
        [
            new ChronicleResolutionConfig
            {
                OutcomeId = "the_teaching_lives",
                When = new ChronicleConditionConfig { Kind = ChronicleConditionKind.RoleHistorical, RoleA = "$master" },
            },
            new ChronicleResolutionConfig
            {
                OutcomeId = "paths_diverge",
                Weight = 1,
                Relationships = [Cull("$student", "$master", "taught_by")],
            },
        ],
        MaxActive = 1,
        CastingCadenceTicks = 700,
    };

    /// <summary>
    /// Two settlements joined by a strong trade route. The road either grows
    /// into legend or falls silent - and either way the chronicle knows which
    /// ports mattered.
    /// </summary>
    private static ChronicleArchetypeConfig TheFlourishingRoad() => new()
    {
        Id = "flourishing_road",
        Name = "The Flourishing Road",
        Description = "A road between two ports, and whether it outlives its makers.",
        PrimaryRoleRef = "$port",
        PrimaryRole = new SemanticGeneratorTarget
        {
            Kind = K.Settlement,
            RequiredOutgoingRelationshipRequirements =
            [
                new SemanticGeneratorRelationshipRequirement { RelationshipKind = "trade_route", MinimumStrength = 0.85f },
            ],
        },
        SupportingRoles =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$partner",
                Kind = K.Settlement,
                Query = new SemanticEntityQuery { RelatedToTargetRelationship = "trade_route", RelatedToTargetMinStrength = 0.85f },
            },
        ],
        Acts =
        [
            new ChronicleActConfig
            {
                DeadlineTicks = 400,
                Bias = [Bias("$port", "$partner", null, 1f), Bias("$partner", "$port", null, 1f)],
                Sustain = [Sustain("$port", 0.02f, "temp_hp"), Sustain("$partner", 0.02f, "temp_hp")],
            },
            new ChronicleActConfig
            {
                DeadlineTicks = 400,
                AdvanceWhen = new ChronicleConditionConfig { Kind = ChronicleConditionKind.RolePairRelationship, RoleA = "$port", RoleB = "$partner", RelationshipKind = "trade_route", MinStrength = 0.95f },
                Sustain = [Sustain("$port", 0.02f, "temp_hp"), Sustain("$partner", 0.02f, "temp_hp")],
            },
        ],
        Resolutions =
        [
            new ChronicleResolutionConfig
            {
                OutcomeId = "the_road_becomes_legend",
                When = new ChronicleConditionConfig { Kind = ChronicleConditionKind.RolePairRelationship, RoleA = "$port", RoleB = "$partner", RelationshipKind = "trade_route", MinStrength = 0.9f },
            },
            new ChronicleResolutionConfig
            {
                OutcomeId = "the_road_falls_silent",
                Weight = 1,
                Relationships = [Cull("$port", "$partner", "trade_route"), Cull("$partner", "$port", "trade_route")],
            },
        ],
        MaxActive = 1,
        CastingCadenceTicks = 900,
    };

    /// <summary>
    /// A corrupted transport and the monster haunting it. Wards are favored;
    /// the run is either cleansed or the vessel is lost with all hands.
    /// </summary>
    private static ChronicleArchetypeConfig TheHauntedRun() => new()
    {
        Id = "haunted_run",
        Name = "The Haunted Run",
        Description = "A vessel, its haunt, and which of them survives the route.",
        PrimaryRoleRef = "$vessel",
        PrimaryRole = new SemanticGeneratorTarget
        {
            Kind = K.Transport,
            RequiredOutgoingRelationshipRequirements =
            [
                new SemanticGeneratorRelationshipRequirement { RelationshipKind = "corrupted_by", MinimumStrength = 0.5f },
            ],
        },
        SupportingRoles =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$haunt",
                Kind = K.Monster,
                Query = new SemanticEntityQuery { RelatedToTargetRelationship = "corrupted_by", RelatedToTargetIncoming = true, RelatedToTargetMinStrength = 0.5f },
            },
        ],
        Acts =
        [
            new ChronicleActConfig
            {
                DeadlineTicks = 350,
                Bias = [Bias("$vessel", "$haunt", "transport_wards_monster", 3f)],
                Sustain = [Sustain("$vessel", 0.02f, "integrity")],
            },
            new ChronicleActConfig
            {
                DeadlineTicks = 350,
                AdvanceWhen = new ChronicleConditionConfig { Kind = ChronicleConditionKind.RolePairRelationship, RoleA = "$vessel", RoleB = "$haunt", RelationshipKind = "ward_against", MinStrength = 0.5f },
                Bias = [Bias("$vessel", "$haunt", "transport_wards_monster", 4f)],
            },
        ],
        Resolutions =
        [
            new ChronicleResolutionConfig
            {
                OutcomeId = "the_run_cleansed",
                When = new ChronicleConditionConfig { Kind = ChronicleConditionKind.RolePairRelationship, RoleA = "$vessel", RoleB = "$haunt", RelationshipKind = "ward_against", MinStrength = 0.5f },
                Relationships = [Cull("$vessel", "$haunt", "corrupted_by")],
            },
            new ChronicleResolutionConfig
            {
                OutcomeId = "the_vessel_is_lost",
                Weight = 1,
                MarkHistorical = ["$vessel"],
            },
        ],
        MaxActive = 1,
        CastingCadenceTicks = 500,
    };

    /// <summary>
    /// An ability and its devoted keeper. The pair sustain each other; the
    /// practice either takes root in the culture or quietly fades.
    /// </summary>
    private static ChronicleArchetypeConfig TheLivingPractice() => new()
    {
        Id = "living_practice",
        Name = "The Living Practice",
        Description = "A discipline and the one who keeps it alive.",
        PrimaryRoleRef = "$practice",
        PrimaryRole = new SemanticGeneratorTarget
        {
            Kind = K.Ability,
            RequiredOutgoingRelationshipRequirements =
            [
                new SemanticGeneratorRelationshipRequirement { RelationshipKind = "practiced_by", MinimumStrength = 0.8f },
            ],
        },
        SupportingRoles =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$keeper",
                Kind = K.Penguin,
                Query = new SemanticEntityQuery { RelatedToTargetRelationship = "practiced_by", RelatedToTargetIncoming = true, RelatedToTargetMinStrength = 0.8f },
            },
        ],
        Acts =
        [
            new ChronicleActConfig
            {
                DeadlineTicks = 450,
                Bias = [Bias("$practice", "$keeper", null, 1f)],
                Sustain = [Sustain("$keeper", 0.015f), Sustain("$practice", 0.02f, "clarity")],
            },
            new ChronicleActConfig
            {
                DeadlineTicks = 450,
                AdvanceWhen = new ChronicleConditionConfig { Kind = ChronicleConditionKind.RolePairRelationship, RoleA = "$practice", RoleB = "$keeper", RelationshipKind = "practiced_by", MinStrength = 0.9f },
                Sustain = [Sustain("$keeper", 0.015f), Sustain("$practice", 0.02f, "clarity")],
            },
        ],
        Resolutions =
        [
            new ChronicleResolutionConfig
            {
                OutcomeId = "the_practice_takes_root",
                When = new ChronicleConditionConfig { Kind = ChronicleConditionKind.RolePairRelationship, RoleA = "$practice", RoleB = "$keeper", RelationshipKind = "practiced_by", MinStrength = 0.85f },
            },
            new ChronicleResolutionConfig
            {
                OutcomeId = "the_practice_fades",
                Weight = 1,
                Relationships = [Cull("$practice", "$keeper", "practiced_by")],
            },
        ],
        MaxActive = 1,
        CastingCadenceTicks = 1000,
    };

    /// <summary>
    /// An orca at war with a named penguin becomes the terror of the coasts.
    /// Raid and reprisal are both favored - a proper duel - until the terror
    /// is driven off or the coast yields.
    /// </summary>
    private static ChronicleArchetypeConfig TheTerrorOfTheCoasts() => new()
    {
        Id = "terror_of_the_coasts",
        Name = "The Terror of the Coasts",
        Description = "A raider, a defender, and a coastline that remembers both.",
        PrimaryRoleRef = "$terror",
        PrimaryRole = new SemanticGeneratorTarget
        {
            Kind = K.Orca,
            RequiredOutgoingRelationshipRequirements =
            [
                new SemanticGeneratorRelationshipRequirement { RelationshipKind = "at_war_with", MinimumStrength = 0.4f },
            ],
        },
        SupportingRoles =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$foe",
                Kind = K.Penguin,
                Query = new SemanticEntityQuery { RelatedToTargetRelationship = "at_war_with", RelatedToTargetMinStrength = 0.4f },
            },
        ],
        Acts =
        [
            new ChronicleActConfig
            {
                DeadlineTicks = 350,
                Bias = [Bias("$terror", "$foe", "orca_raids_trade_route", 2.5f), Bias("$foe", "$terror", "penguin_slays_orca", 2.5f)],
            },
            new ChronicleActConfig
            {
                DeadlineTicks = 400,
                AdvanceWhen = new ChronicleConditionConfig { Kind = ChronicleConditionKind.RoleHistorical, RoleA = "$terror" },
                Bias = [Bias("$foe", "$terror", "penguin_slays_orca", 4f)],
            },
        ],
        Resolutions =
        [
            new ChronicleResolutionConfig
            {
                OutcomeId = "the_terror_driven_off",
                When = new ChronicleConditionConfig { Kind = ChronicleConditionKind.RoleHistorical, RoleA = "$terror" },
            },
            new ChronicleResolutionConfig
            {
                OutcomeId = "the_coast_yields",
                Weight = 1,
                Relationships = [Cull("$terror", "$foe", "at_war_with"), Cull("$foe", "$terror", "at_war_with")],
            },
        ],
        MaxActive = 1,
        CastingCadenceTicks = 350,
    };
}
