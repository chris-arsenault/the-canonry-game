namespace IceRemembers;

using Engine.Domain.Config;

using K = IceRemembers.IceRemembersKinds;

/// <summary>
/// Ice Remembers chronicle archetypes for the beat-driven Chronicle Director.
/// Every cast is grounded in existing history; from there the tale is
/// whatever beats the cast actually strikes. Adversarial archetypes map
/// stances onto the shared cross-story library; endings are recognized world
/// facts (deaths, edges culled by real gambits and drift), never director
/// writes; a cold, quiet tale fades from the telling.
/// </summary>
public static class IceRemembersChronicleArchetypes
{
    private const string Prot = IceRemembersChronicleBeats.Protagonist;
    private const string Adv = IceRemembersChronicleBeats.Adversary;

    public static IReadOnlyList<ChronicleBeatConfig> CreateSharedBeats() =>
        IceRemembersChronicleBeats.CreateShared();

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

    /// <summary>
    /// A hoarding monster already at war with a named penguin becomes the
    /// quarry of a hunt. The richest pool: wounds, oaths, a granted closing
    /// blow - and four endings the world itself must produce.
    /// </summary>
    private static ChronicleArchetypeConfig TheHunt() => new()
    {
        Id = "the_hunt",
        TitleTemplate = "The Hunt of {$quarry}",
        Name = "The Hunt",
        Description = "A much-provoked beast, a named hunter, and whatever ending the ice allows.",
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
        Stances = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Prot] = "$hunter",
            [Adv] = "$quarry",
        },
        Beats = [.. HuntBeats(), .. HuntTerminals()],
        FadeOutcomeId = "outlasted_the_telling",
        FadeNarrative = "The ice grew quiet. Whatever hunted between {$hunter} and {$quarry} outlasted the telling.",
        MaxActive = 1,
        CastingCadenceTicks = 400,
    };

    private static List<ChronicleBeatConfig> HuntBeats() =>
    [
        new ChronicleBeatConfig
        {
            Id = "the_hunters_oath",
            Open = Beat.Always(),
            Charges =
            [
                Beat.Charge("$hunter", "$quarry", "penguin_slays_monster", 2f),
                Beat.Charge("$hunter", "$quarry", "penguin_binds_what_was_loosed", 1.5f),
            ],
        },
        new ChronicleBeatConfig
        {
            Id = "the_beast_bleeds",
            Narrative = "The bindings on {$quarry} weaken. It is now or never.",
            Open = Beat.AttrBelow("$quarry", "containment", 55f),
            Strike = Beat.OnFact(Beat.AttrBelow("$quarry", "containment", 55f)),
            HeatGain = 2.5f,
            Charges = [Beat.Charge("$hunter", "$quarry", "penguin_slays_monster", 2f, heatScaled: true)],
        },
        new ChronicleBeatConfig
        {
            Id = "the_hunter_bleeds",
            Narrative = "{$hunter} bleeds on the ice, and the beast can smell it.",
            Open = Beat.AttrBelow("$hunter", "vitality", 40f),
            Strike = Beat.OnFact(Beat.AttrBelow("$hunter", "vitality", 40f)),
            HeatGain = 2.5f,
            Charges = [Beat.Charge("$quarry", "$hunter", null, 1.5f)],
        },
        new ChronicleBeatConfig
        {
            Id = "the_binding_hour",
            Narrative = "An old instrument is brought to bear against {$quarry}.",
            Open = Beat.Always(),
            Strike = Beat.OutsiderOn("$quarry", "artifact_binds_monster"),
            HeatGain = 2.5f,
        },
        new ChronicleBeatConfig
        {
            Id = "second_wind",
            Narrative = "{$hunter} rises again. The hunt is not finished.",
            Open = Beat.AttrBelow("$hunter", "vitality", 35f),
            Strike = Beat.OnFact(Beat.AttrAtLeast("$hunter", "vitality", 70f)),
            HeatGain = 1.5f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_beast_at_bay",
            Narrative = "At last {$hunter} brings {$quarry} to bay.",
            Open = Beat.Heat(5f),
            Strike = Beat.PairGambit("$hunter", "$quarry", "penguin_corners_the_beast"),
            HeatGain = 3f,
            Charges = [Beat.Charge("$hunter", "$quarry", "penguin_slays_monster", 3f, heatScaled: true)],
            Grants = [Beat.Grant("$hunter", "penguin_corners_the_beast")],
        },
    ];

    private static List<ChronicleBeatConfig> HuntTerminals() =>
    [
        new ChronicleBeatConfig
        {
            Id = "the_hunt_fulfilled",
            Narrative = "{$quarry} is fallen. The hunt of {$hunter} is fulfilled, and the ice will remember the blow.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.Historical("$quarry")),
            HeatGain = 3f,
            TerminalOutcomeId = "hunt_fulfilled",
        },
        new ChronicleBeatConfig
        {
            Id = "the_hunter_falls",
            Narrative = "{$hunter} will not return from the ice. The hunt dies with its hunter, and {$quarry} endures.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.Historical("$hunter")),
            HeatGain = 3f,
            TerminalOutcomeId = "the_hunt_dies_with_the_hunter",
        },
        new ChronicleBeatConfig
        {
            Id = "the_hunt_forsaken",
            Narrative = "The war between {$hunter} and {$quarry} is done. The beast endures; the hunt is left to the wind.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.PairRelAbsent("$hunter", "$quarry", "at_war_with")),
            HeatGain = 1f,
            TerminalOutcomeId = "the_beast_endures",
        },
    ];

    /// <summary>
    /// Two factions with a pressed rivalry. Declaration, grinding war, and
    /// peace are all real gambits; the chronicle recognizes which happened.
    /// </summary>
    private static ChronicleArchetypeConfig TheBloodRivalry() => new()
    {
        Id = "blood_rivalry",
        TitleTemplate = "The {$challenger}-{$rival} Rivalry",
        Name = "The Blood Rivalry",
        Description = "A rivalry pressed until the world settles it, one way or another.",
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
        Stances = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Prot] = "$challenger",
            [Adv] = "$rival",
        },
        Beats = [.. RivalryBeats(), .. RivalryTerminals()],
        FadeOutcomeId = "the_grudge_cools",
        FadeNarrative = "Neither {$challenger} nor {$rival} pressed the grievance further. The grudge cools in the dark.",
        MaxActive = 1,
        CastingCadenceTicks = 500,
    };

    private static List<ChronicleBeatConfig> RivalryBeats() =>
        [.. RivalryCoreBeats(), .. RivalryWarSagaBeats()];

    private static List<ChronicleBeatConfig> RivalryCoreBeats() =>
    [
        new ChronicleBeatConfig
        {
            Id = "pressed_grievances",
            Open = Beat.Always(),
            Charges =
            [
                Beat.Charge("$challenger", "$rival", "faction_declares_territorial_war", 2.5f),
                Beat.Charge("$rival", "$challenger", "faction_declares_territorial_war", 2.5f),
            ],
        },
        new ChronicleBeatConfig
        {
            Id = "the_war_to_settle_it",
            Narrative = "Grievance becomes war. {$challenger} and {$rival} will settle this in the open.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.PairRel("$challenger", "$rival", "at_war_with", 0.5f)),
            HeatGain = 3f,
        },
    ];

    private static List<ChronicleBeatConfig> RivalryWarSagaBeats() =>
    [
        new ChronicleBeatConfig
        {
            Id = "the_war_is_named",
            Narrative = "The war between {$challenger} and {$rival} has earned a name of its own.",
            Open = Beat.Always(),
            Strike = Beat.GeneratorOn("$challenger", "wars_get_names"),
            HeatGain = 3f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_call_to_banners",
            Narrative = "Banners answer {$challenger}. What was a grievance is becoming a war of nations.",
            Open = Beat.Always(),
            Strike = Beat.GeneratorOn("$challenger", "allies_answer_the_call"),
            HeatGain = 2.5f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_toll_is_counted",
            Narrative = "The dead of {$rival} are counted and named. Wars are paid for in penguins.",
            Open = Beat.Always(),
            Strike = Beat.GeneratorOn("$rival", "war_takes_its_toll"),
            HeatGain = 2.5f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_ruin_of_the_challenger",
            Narrative = "War hollows {$challenger}. Little remains of what the grievance was meant to defend.",
            Open = Beat.AttrBelow("$challenger", "cohesion", 18f),
            Strike = Beat.OnFact(Beat.AttrBelow("$challenger", "cohesion", 18f)),
            HeatGain = 3f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_ruin_of_the_rival",
            Narrative = "War hollows {$rival}. The ice will take back what neither side can hold.",
            Open = Beat.AttrBelow("$rival", "cohesion", 18f),
            Strike = Beat.OnFact(Beat.AttrBelow("$rival", "cohesion", 18f)),
            HeatGain = 3f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_long_grinding",
            Open = Beat.PairRel("$challenger", "$rival", "at_war_with", 0.05f),
            Charges =
            [
                Beat.Charge("$challenger", "$rival", "faction_sues_for_peace", 1.5f, heatScaled: true),
                Beat.Charge("$rival", "$challenger", "faction_sues_for_peace", 1.5f, heatScaled: true),
            ],
        },
    ];

    private static List<ChronicleBeatConfig> RivalryTerminals() =>
    [
        new ChronicleBeatConfig
        {
            Id = "rivalry_settled",
            Narrative = "The grievance between {$challenger} and {$rival} is settled ground.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.PairRelAbsent("$challenger", "$rival", "rival_of")),
            HeatGain = 1f,
            TerminalOutcomeId = "rivalry_settled",
        },
        new ChronicleBeatConfig
        {
            Id = "the_rival_broken",
            Narrative = "The rivalry ended the only way it could: {$rival} is no more.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.Historical("$rival")),
            HeatGain = 3f,
            TerminalOutcomeId = "the_rival_broken",
        },
        new ChronicleBeatConfig
        {
            Id = "the_challenger_broken",
            Narrative = "{$challenger} pressed the grievance and paid for it with everything.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.Historical("$challenger")),
            HeatGain = 3f,
            TerminalOutcomeId = "the_challenger_broken",
        },
    ];

    /// <summary>A student bound to a living master, and what survives of one inside the other.</summary>
    private static ChronicleArchetypeConfig TheTeaching() => new()
    {
        Id = "the_teaching",
        TitleTemplate = "The Teaching of {$student}",
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
        Beats = [.. TeachingBeats()],
        FadeOutcomeId = "the_lesson_rests",
        FadeNarrative = "The teaching of {$student} settled into silence, the way most teachings do.",
        MaxActive = 1,
        CastingCadenceTicks = 700,
    };

    private static List<ChronicleBeatConfig> TeachingBeats() =>
        [.. TeachingLifeBeats(), .. TeachingEndBeats()];

    private static List<ChronicleBeatConfig> TeachingLifeBeats() =>
    [
        new ChronicleBeatConfig
        {
            Id = "the_apprenticeship",
            Open = Beat.Always(),
            Charges =
            [
                Beat.Charge("$master", "$student", "penguin_mends_another", 1.5f),
                Beat.Charge("$student", "$master", "penguin_mends_another", 1.5f),
            ],
        },
        new ChronicleBeatConfig
        {
            Id = "the_mending",
            Narrative = "{$master} mends {$student}, as it was always done.",
            Open = Beat.Always(),
            Strike = Beat.PairGambit("$master", "$student"),
            HeatGain = 2f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_student_surpasses",
            Narrative = "The student tends the master. The seasons have turned.",
            Open = Beat.Always(),
            Strike = Beat.PairGambit("$student", "$master"),
            HeatGain = 2.5f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_teaching_passes_on",
            Narrative = "What was given to {$student} is given again. The lineage grows by one.",
            Open = Beat.Always(),
            Strike = Beat.RoleActs("$student", "penguin_passes_the_teaching"),
            HeatGain = 3f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_students_deed",
            Narrative = "{$student} makes a name in blood and ice. The teaching holds.",
            Open = Beat.Always(),
            Strike = Beat.RoleActs("$student", "penguin_slays_monster"),
            HeatGain = 2.5f,
        },
    ];

    private static List<ChronicleBeatConfig> TeachingEndBeats() =>
    [
        new ChronicleBeatConfig
        {
            Id = "the_teaching_lives",
            Narrative = "{$master} is gone. What {$master} knew lives on in {$student}.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.Historical("$master")),
            HeatGain = 2f,
            TerminalOutcomeId = "the_teaching_lives",
        },
        new ChronicleBeatConfig
        {
            Id = "the_teaching_broken",
            Narrative = "{$student} died before the teaching was done. What {$master} carries now has nowhere to go.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.Historical("$student")),
            HeatGain = 2f,
            TerminalOutcomeId = "the_teaching_broken",
        },
        new ChronicleBeatConfig
        {
            Id = "paths_diverge",
            Narrative = "{$student} and {$master} drifted apart. The teaching fades, unfinished.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.PairRelAbsent("$student", "$master", "taught_by")),
            HeatGain = 1f,
            TerminalOutcomeId = "paths_diverge",
        },
    ];

    /// <summary>Two settlements joined by a strong trade route, and whether the road outlives its makers.</summary>
    private static ChronicleArchetypeConfig TheFlourishingRoad() => new()
    {
        Id = "flourishing_road",
        TitleTemplate = "The {$port} Road",
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
        Beats = [.. RoadBeats()],
        FadeOutcomeId = "the_road_endures_quietly",
        FadeNarrative = "The road between {$port} and {$partner} needs no more telling. It simply endures.",
        MaxActive = 1,
        CastingCadenceTicks = 900,
    };

    private static List<ChronicleBeatConfig> RoadBeats() =>
        [.. RoadTradeBeats(), .. RoadWorldBeats()];

    private static List<ChronicleBeatConfig> RoadTradeBeats() =>
    [
        new ChronicleBeatConfig
        {
            Id = "laden_sledges",
            Open = Beat.Always(),
            Charges =
            [
                Beat.Charge("$port", "$partner", null, 1f),
                Beat.Charge("$partner", "$port", null, 1f),
                Beat.Charge("$port", "$partner", "settlement_raises_causeway", 3f, heatScaled: true),
                Beat.Charge("$partner", "$port", "settlement_raises_causeway", 3f, heatScaled: true),
            ],
        },
        new ChronicleBeatConfig
        {
            Id = "the_causeway_rises",
            Narrative = "Ice is raised over open water. The road between {$port} and {$partner} becomes a thing that can be walked.",
            Open = Beat.Always(),
            Strike = Beat.PairGambit("$port", "$partner", "settlement_raises_causeway"),
            HeatGain = 3f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_road_proves_true",
            Narrative = "The road between {$port} and {$partner} hums with traffic. It has proven true.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.PairRel("$port", "$partner", "trade_route", 0.95f)),
            HeatGain = 2f,
        },
    ];

    private static List<ChronicleBeatConfig> RoadWorldBeats() =>
    [
        new ChronicleBeatConfig
        {
            Id = "raiders_on_the_road",
            Narrative = "Raiders test the road. What flows between {$port} and {$partner} is worth stealing now.",
            Open = Beat.Always(),
            Strike = Beat.OutsiderOn("$port", "orca_raids_trade_route"),
            HeatGain = 2.5f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_port_builds_high",
            Narrative = "{$port} raises a new tier above the road. Prosperity is built in the open.",
            Open = Beat.Always(),
            Strike = Beat.RoleActs("$port", "settlement_raises_the_spire"),
            HeatGain = 2f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_gallery_beneath",
            Narrative = "{$port} digs deeper beneath the road. Some prosperity prefers the dark.",
            Open = Beat.Always(),
            Strike = Beat.RoleActs("$port", "settlement_digs_the_gallery"),
            HeatGain = 2f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_road_falls_silent",
            Narrative = "The road between {$port} and {$partner} fell silent, and the ice took it back.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.PairRelAbsent("$port", "$partner", "trade_route")),
            HeatGain = 1f,
            TerminalOutcomeId = "the_road_falls_silent",
        },
        new ChronicleBeatConfig
        {
            Id = "the_port_falls",
            Narrative = "A road is only as alive as its ends, and one of them is gone.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.Historical("$port")),
            HeatGain = 2f,
            TerminalOutcomeId = "the_port_falls",
        },
    ];

    /// <summary>A corrupted transport and the monster haunting it.</summary>
    private static ChronicleArchetypeConfig TheHauntedRun() => new()
    {
        Id = "haunted_run",
        TitleTemplate = "The Haunting of {$vessel}",
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
        Stances = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Prot] = "$vessel",
            [Adv] = "$haunt",
        },
        Beats = [.. HauntedRunBeats()],
        FadeOutcomeId = "the_haunt_settles_in",
        FadeNarrative = "The haunting of {$vessel} became just another sound of the run.",
        MaxActive = 1,
        CastingCadenceTicks = 500,
    };

    private static List<ChronicleBeatConfig> HauntedRunBeats() =>
    [
        new ChronicleBeatConfig
        {
            Id = "wards_and_salt",
            Open = Beat.Always(),
            Charges =
            [
                Beat.Charge("$vessel", "$haunt", "transport_wards_monster", 3f, heatScaled: true),
                Beat.Charge("$haunt", "$vessel", "monster_haunts_transport", 1.5f),
            ],
        },
        new ChronicleBeatConfig
        {
            Id = "the_wards_hold",
            Narrative = "The wards on {$vessel} hold against {$haunt}. The crew sleeps again.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.PairRel("$vessel", "$haunt", "ward_against", 0.5f)),
            HeatGain = 2.5f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_conduit_holds",
            Narrative = "Even haunted, {$vessel} keeps the routes alive.",
            Open = Beat.Always(),
            Strike = Beat.RoleActs("$vessel", "transport_opens_conduit"),
            HeatGain = 2f,
        },
        new ChronicleBeatConfig
        {
            Id = "salt_and_iron",
            Narrative = "Salt and iron are set against {$haunt}. An instrument remembers its purpose.",
            Open = Beat.Always(),
            Strike = Beat.OutsiderOn("$haunt", "artifact_binds_monster"),
            HeatGain = 2.5f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_run_cleansed",
            Narrative = "{$vessel} drove out {$haunt}. The run is clean, and crews sail it again.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.PairRelAbsent("$vessel", "$haunt", "corrupted_by")),
            HeatGain = 2f,
            TerminalOutcomeId = "the_run_cleansed",
        },
        new ChronicleBeatConfig
        {
            Id = "the_vessel_is_lost",
            Narrative = "{$vessel} was lost with all hands to {$haunt}.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.Historical("$vessel")),
            HeatGain = 3f,
            TerminalOutcomeId = "the_vessel_is_lost",
        },
    ];

    /// <summary>An ability and its devoted keeper.</summary>
    private static ChronicleArchetypeConfig TheLivingPractice() => new()
    {
        Id = "living_practice",
        TitleTemplate = "The Keeping of {$practice}",
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
        Beats = [.. PracticeBeats()],
        FadeOutcomeId = "the_practice_endures",
        FadeNarrative = "{$practice} was kept without drama, season after season. That, too, is a story.",
        MaxActive = 1,
        CastingCadenceTicks = 1000,
    };

    private static List<ChronicleBeatConfig> PracticeBeats() =>
    [
        new ChronicleBeatConfig
        {
            Id = "devotion",
            Open = Beat.Always(),
            Charges = [Beat.Charge("$practice", "$keeper", null, 1f), Beat.Charge("$keeper", "$practice", null, 1f)],
        },
        new ChronicleBeatConfig
        {
            Id = "taking_root",
            Narrative = "The devotion of {$keeper} deepens. {$practice} is taking root.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.PairRel("$practice", "$keeper", "practiced_by", 0.92f)),
            HeatGain = 2f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_practice_spreads",
            Narrative = "{$practice} finds another pair of hands. A discipline kept is a discipline shared.",
            Open = Beat.Always(),
            Strike = Beat.RoleActs("$practice", "ability_shares_practice"),
            HeatGain = 3f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_keepers_grace",
            Narrative = "The waters still around {$keeper}. This is what the keeping is for.",
            Open = Beat.Always(),
            Strike = Beat.RoleActs("$keeper", "penguin_stills_the_waters"),
            HeatGain = 2f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_keeper_passes",
            Narrative = "The keeping of {$practice} passes with {$keeper}. Whether it took root, the seasons will tell.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.Historical("$keeper")),
            HeatGain = 2f,
            TerminalOutcomeId = "the_keeper_passes",
        },
        new ChronicleBeatConfig
        {
            Id = "the_practice_fades",
            Narrative = "{$keeper} faltered, and {$practice} fades from living memory.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.PairRelAbsent("$practice", "$keeper", "practiced_by")),
            HeatGain = 1f,
            TerminalOutcomeId = "the_practice_fades",
        },
    ];

    /// <summary>An orca at war with a named penguin becomes the terror of the coasts.</summary>
    private static ChronicleArchetypeConfig TheTerrorOfTheCoasts() => new()
    {
        Id = "terror_of_the_coasts",
        TitleTemplate = "{$terror}, Terror of the Coasts",
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
        Stances = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Prot] = "$foe",
            [Adv] = "$terror",
        },
        Beats = [.. TerrorBeats()],
        FadeOutcomeId = "the_tide_forgets",
        FadeNarrative = "The raids thinned, the watches ended. The tide forgot what {$terror} and {$foe} owed each other.",
        MaxActive = 1,
        CastingCadenceTicks = 350,
    };

    private static List<ChronicleBeatConfig> TerrorBeats() =>
    [
        new ChronicleBeatConfig
        {
            Id = "raids_in_the_dark",
            Open = Beat.Always(),
            Charges =
            [
                Beat.Charge("$terror", "$foe", "orca_raids_trade_route", 2.5f),
                Beat.Charge("$foe", "$terror", "penguin_slays_orca", 2.5f, heatScaled: true),
            ],
        },
        new ChronicleBeatConfig
        {
            Id = "the_deep_current",
            Narrative = "{$terror} shapes the sea itself to its hunting.",
            Open = Beat.Always(),
            Strike = Beat.RoleActs("$terror", "orca_lays_deep_current"),
            HeatGain = 2.5f,
        },
        new ChronicleBeatConfig
        {
            Id = "blood_in_the_water",
            Narrative = "Another raid. The coast counts its dead and bars its harbors.",
            Open = Beat.Always(),
            Strike = Beat.RoleActs("$terror", "orca_raids_trade_route"),
            HeatGain = 2f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_terror_driven_off",
            Narrative = "{$foe} drove {$terror} from the coasts for good.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.Historical("$terror")),
            HeatGain = 3f,
            TerminalOutcomeId = "the_terror_driven_off",
        },
        new ChronicleBeatConfig
        {
            Id = "the_coast_mourns",
            Narrative = "{$foe} stood against {$terror} and fell. The coast learns to live afraid.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.Historical("$foe")),
            HeatGain = 3f,
            TerminalOutcomeId = "the_coast_mourns",
        },
        new ChronicleBeatConfig
        {
            Id = "the_coast_yields",
            Narrative = "The war between {$terror} and {$foe} is spent. The coast yields, and the terror hunts unchallenged.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.PairRelAbsent("$terror", "$foe", "at_war_with")),
            HeatGain = 1f,
            TerminalOutcomeId = "the_coast_yields",
        },
    ];
}
