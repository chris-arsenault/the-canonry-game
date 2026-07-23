namespace IceRemembers;

using Engine.Domain.Config;

using K = IceRemembers.IceRemembersKinds;

// The land's wound and the object's weight. The scar tale is corruption as
// consequence without simple guilt - an inheritance finding available
// space, and the slow question of whether the mending or the wound is more
// patient. The burden tale is the Before-Times fear made personal: a
// serial-numbered thing whose history can be spent, and the one who
// carries it while the count continues.
public static partial class IceRemembersChronicleArchetypes
{
    private static ChronicleArchetypeConfig TheScarThatWouldNotClose() => new()
    {
        Id = "the_scar_that_would_not_close",
        TitleTemplate = "The Scar of {$scar}",
        Name = "The Scar That Would Not Close",
        Description = "A wounded stretch of the Berg, the mending set against it, and which of them is more patient.",
        PrimaryRoleRef = "$scar",
        PrimaryRole = new SemanticGeneratorTarget
        {
            Kind = K.Region,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "scar" },
            RequiredOutgoingRelationshipRequirements =
            [
                new SemanticGeneratorRelationshipRequirement { RelationshipKind = "corrupted_by", MinimumStrength = 0.45f },
            ],
        },
        SupportingRoles =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$mender",
                Kind = K.Artifact,
                Query = new SemanticEntityQuery
                {
                    RelatedToTargetRelationship = "cleansed_by",
                    RelatedToTargetMinStrength = 0.3f,
                },
            },
        ],
        OpenRoles = [new ChronicleOpenRoleConfig { Ref = "$keeper", Kind = K.Penguin }],
        Beats = [.. ScarWoundBeats(), .. ScarMendingBeats(), .. ScarEndBeats()],
        FadeOutcomeId = "the_wound_scabs_over",
        FadeNarrative = "Neither the wound in {$scar} nor the mending pressed its claim. The land learned to live half-healed, the way most land does.",
        // The land's story runs on the land's clock: corruption eats
        // stability over thousands of ticks, so the telling waits longer
        // than a hunt would before calling the ground quiet.
        FadeGraceTicks = 6000,
        FadeQuietTicks = 4500,
        MaxActive = 1,
        CastingCadenceTicks = 700,
    };

    private static List<ChronicleBeatConfig> ScarWoundBeats() =>
    [
        new ChronicleBeatConfig
        {
            Id = "the_long_tending",
            Open = Beat.Always(),
            Charges =
            [
                Beat.Charge("$mender", "$scar", "artifact_cleanses_scar", 2.5f, heatScaled: true, success: 0.2f),
                Beat.Charge("$scar", "$scar", "region_breathes_the_thaw", 1.5f, success: 0.15f),
            ],
        },
        new ChronicleBeatConfig
        {
            Id = "the_wound_deepens",
            Narrative = "The ground of {$scar} gives further. What is not watched is not stopped.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.AttrBelow("$scar", "stability", 30f)),
            HeatGain = 2.5f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_wound_breeds",
            Narrative = "Something comes up out of the broken ground of {$scar}. The corruption does not behave like an enemy; it behaves like an inheritance finding available space.",
            Open = Beat.Always(),
            Strike = Beat.GeneratorOn("$scar", "corruption_wakes_monsters"),
            HeatGain = 3f,
        },
    ];

    private static List<ChronicleBeatConfig> ScarMendingBeats() =>
    [
        new ChronicleBeatConfig
        {
            Id = "the_mending_holds",
            Narrative = "{$mender} is set against the wound, and for a season the wound listens.",
            Open = Beat.Always(),
            Strike = Beat.PairGambit("$mender", "$scar", "artifact_cleanses_scar"),
            HeatGain = 3f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_springs_answer",
            Narrative = "Beneath {$scar} the old warmth stirs. Some wounds mend from below, without ceremony, the way the Berg has always healed.",
            Open = Beat.Always(),
            Strike = Beat.RoleActs("$scar", "region_breathes_the_thaw"),
            HeatGain = 2f,
        },
        new ChronicleBeatConfig
        {
            Id = "a_keeper_comes",
            Narrative = "{$keeper} takes up the watch at {$scar}. Watching a thing is not the same as stopping it, but it is how stopping starts.",
            Open = Beat.Always(),
            Strike = Beat.OutsiderJoins("$scar", "$keeper"),
            HeatGain = 2.5f,
        },
    ];

    private static List<ChronicleBeatConfig> ScarEndBeats() =>
    [
        new ChronicleBeatConfig
        {
            Id = "the_scar_closes",
            Narrative = "The ground of {$scar} holds firm again. The land does not forget it was wounded - the ice keeps that - but it no longer needs anyone to say so.",
            // The closing only counts after the wound has truly deepened;
            // a freshly cast scar still standing firm is not a recovery.
            Open = Beat.AttrBelow("$scar", "stability", 45f),
            Strike = Beat.OnFact(Beat.AttrAtLeast("$scar", "stability", 85f)),
            HeatGain = 2f,
            TerminalOutcomeId = "the_scar_closes",
        },
        new ChronicleBeatConfig
        {
            Id = "the_land_is_lost",
            Narrative = "{$scar} is gone past mending. The maps will carry the name a while longer; the ground has already given it up.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.Historical("$scar")),
            HeatGain = 3f,
            TerminalOutcomeId = "the_land_is_lost",
        },
        new ChronicleBeatConfig
        {
            Id = "the_mender_is_spent",
            Narrative = "{$mender} gave what it had to the wound and is done. Some objects finish their purpose; the scar keeps its own counsel about whether it was enough.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.Historical("$mender")),
            HeatGain = 2.5f,
            TerminalOutcomeId = "the_mender_is_spent",
        },
    ];

    private static ChronicleArchetypeConfig TheBurden() => new()
    {
        Id = "the_burden",
        TitleTemplate = "The Burden of {$bearer}",
        Name = "The Burden",
        Description = "A serial-numbered thing, the one who carries it, and which of them is spent first.",
        PrimaryRoleRef = "$burden",
        PrimaryRole = new SemanticGeneratorTarget
        {
            Kind = K.Artifact,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "weapon" },
            RequiredIncomingRelationshipRequirements =
            [
                new SemanticGeneratorRelationshipRequirement { RelationshipKind = "holds", MinimumStrength = 0.5f },
            ],
        },
        SupportingRoles =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$bearer",
                Kind = K.Penguin,
                Query = new SemanticEntityQuery
                {
                    RelatedToTargetRelationship = "holds",
                    RelatedToTargetMinStrength = 0.5f,
                },
            },
        ],
        OpenRoles = [new ChronicleOpenRoleConfig { Ref = "$covetous", Kind = K.Monster }],
        Stances = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Prot] = "$bearer",
            [Adv] = "$burden",
        },
        Beats = [.. BurdenCarryBeats(), .. BurdenEndBeats()],
        FadeOutcomeId = "the_count_continues",
        FadeNarrative = "The burden went quiet in the keeping of {$bearer}. Quiet is not finished. Somewhere, the count continues.",
        MaxActive = 1,
        CastingCadenceTicks = 800,
    };

    private static List<ChronicleBeatConfig> BurdenCarryBeats() =>
    [
        new ChronicleBeatConfig
        {
            Id = "the_keeping",
            Open = Beat.Always(),
            Charges =
            [
                Beat.Charge("$bearer", "$burden", "penguin_wields_artifact", 1.5f, success: 0.1f),
            ],
        },
        new ChronicleBeatConfig
        {
            Id = "the_grip_tightens",
            Narrative = "{$bearer} carries {$burden} closer than before. Objects become involved with their keepers by being handled near enough to cost.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.PairRel("$bearer", "$burden", "holds", 0.8f)),
            HeatGain = 2.5f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_ice_recoils",
            Narrative = "The frost thins around {$bearer}, and something in {$burden} keeps speaking its numbers. The carrying is spending them.",
            Open = Beat.AttrBelow("$bearer", "vitality", 45f),
            Strike = Beat.OnFact(Beat.AttrBelow("$bearer", "vitality", 45f)),
            HeatGain = 2.5f,
        },
        new ChronicleBeatConfig
        {
            Id = "hunters_in_the_dark",
            Narrative = "{$covetous} has scented what {$bearer} carries. Things made for killing call to things that hunger.",
            Open = Beat.Always(),
            Strike = Beat.OutsiderJoins("$burden", "$covetous", "monster_steals_artifact"),
            HeatGain = 2.5f,
            Charges = [Beat.Charge("$covetous", "$burden", "monster_steals_artifact", 2f, heatScaled: true, success: 0.15f)],
        },
    ];

    private static List<ChronicleBeatConfig> BurdenEndBeats() =>
    [
        new ChronicleBeatConfig
        {
            Id = "the_burden_is_laid_down",
            Narrative = "{$bearer} no longer carries {$burden}. Given up, taken, or consigned to the deep - the hands are empty either way, and lighter than they remember being.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.PairRelAbsent("$bearer", "$burden", "holds")),
            HeatGain = 2f,
            TerminalOutcomeId = "the_burden_is_laid_down",
        },
        new ChronicleBeatConfig
        {
            Id = "the_bearer_is_spent",
            Narrative = "{$bearer} is done, and {$burden} is not. It waits for the next pair of hands with the patience of a thing that has already outlived its makers.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.Historical("$bearer")),
            HeatGain = 3f,
            TerminalOutcomeId = "the_bearer_is_spent",
        },
        new ChronicleBeatConfig
        {
            Id = "the_burden_is_unmade",
            Narrative = "{$burden} is ended. One number falls out of the series, and whatever keeps the count has one fewer thing to wait for.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.Historical("$burden")),
            HeatGain = 3f,
            TerminalOutcomeId = "the_burden_is_unmade",
        },
    ];
}
