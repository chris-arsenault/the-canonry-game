namespace IceRemembers;

using Engine.Domain.Config;

using K = IceRemembers.IceRemembersKinds;

// The Far Wanderer. A walker who has offered a far walk to the ice - the
// waymark deed is the casting hook - and the hearth whose light they left.
// The tale is the Great Thaw's living shape at penguin scale: the going-out,
// the ground no name has reached, the stone if the ice answers, and one of
// the endings the world actually produces. The cold is not a character. The
// cold does not hunt; it waits.
public static partial class IceRemembersChronicleArchetypes
{
    private static ChronicleArchetypeConfig TheFarWanderer() => new()
    {
        Id = "the_far_wanderer",
        TitleTemplate = "The Far Walk of {$wanderer}",
        Name = "The Far Wanderer",
        Description = "A walker past the hearth-light, and whichever ending the ice keeps.",
        PrimaryRoleRef = "$wanderer",
        PrimaryRole = new SemanticGeneratorTarget
        {
            Kind = K.Penguin,
            RequiredOutgoingRelationshipRequirements =
            [
                new SemanticGeneratorRelationshipRequirement { RelationshipKind = "memorializes", MinimumStrength = 0.45f },
            ],
        },
        SupportingRoles =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$hearth",
                Kind = K.Settlement,
                Query = new SemanticEntityQuery
                {
                    RelatedToTargetRelationship = "home_base",
                    RelatedToTargetIncoming = true,
                    RelatedToTargetMinStrength = 0.2f,
                },
            },
        ],
        OpenRoles = [new ChronicleOpenRoleConfig { Ref = "$companion", Kind = K.Penguin }],
        Beats = [.. WandererWalkBeats(), .. WandererTrailBeats(), .. WandererEndBeats()],
        FadeOutcomeId = "the_snow_closes_over",
        FadeNarrative = "New snow closed over the walk of {$wanderer}. The trail is still down there, for whoever learns to read it.",
        FadeGraceTicks = 3000,
        FadeQuietTicks = 2500,
        MaxActive = 1,
        CastingCadenceTicks = 600,
    };

    private static List<ChronicleBeatConfig> WandererWalkBeats() =>
    [
        new ChronicleBeatConfig
        {
            Id = "the_long_out",
            Open = Beat.Always(),
            Charges =
            [
                Beat.Charge("$wanderer", "$hearth", null, 1f, success: 0.1f),
                Beat.Charge("$hearth", "$wanderer", null, 1f, success: 0.1f),
            ],
        },
        new ChronicleBeatConfig
        {
            Id = "past_the_hearth_light",
            Narrative = "The lights of {$hearth} sink behind the ridgeline. {$wanderer} walks where the only warmth is carried warmth.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.BeyondDistance("$wanderer", "$hearth", 14f)),
            HeatGain = 2f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_white_between",
            Narrative = "Far past the last packed trail, {$wanderer} crosses ground no name has reached. The cold does not hunt. It waits.",
            Open = Beat.Heat(2f),
            Strike = Beat.OnFact(Beat.BeyondDistance("$wanderer", "$hearth", 26f)),
            HeatGain = 2.5f,
        },
        new ChronicleBeatConfig
        {
            Id = "the_flesh_pays",
            Narrative = "The walk takes its toll of flesh - it always does. {$wanderer} goes on lighter and slower.",
            Open = Beat.AttrBelow("$wanderer", "vitality", 40f),
            Strike = Beat.OnFact(Beat.AttrBelow("$wanderer", "vitality", 40f)),
            HeatGain = 2.5f,
            Charges = [Beat.Charge("$wanderer", "$hearth", null, 1.5f, success: 0.2f)],
        },
    ];

    private static List<ChronicleBeatConfig> WandererTrailBeats() =>
    [
        new ChronicleBeatConfig
        {
            Id = "the_ice_answers_the_walk",
            Narrative = "The ice answers: a remembrance-stone rises for the walk of {$wanderer}, and the far trails stay warm a little longer around it.",
            Open = Beat.Always(),
            Strike = Beat.GeneratorOn("$wanderer", "the_stone_remembers_the_walk"),
            HeatGain = 3f,
        },
        new ChronicleBeatConfig
        {
            Id = "another_takes_the_trail",
            Narrative = "{$companion} finds the causeway {$wanderer} packed toward home and follows it out. No trail stays lonely long.",
            Open = Beat.Always(),
            Strike = Beat.OutsiderJoins("$wanderer", "$companion"),
            HeatGain = 2.5f,
            Charges = [Beat.Charge("$companion", "$hearth", "penguin_raises_the_waymark", 1.5f, heatScaled: true, success: 0.15f)],
        },
    ];

    private static List<ChronicleBeatConfig> WandererEndBeats() =>
    [
        new ChronicleBeatConfig
        {
            Id = "the_walk_comes_home",
            Narrative = "{$wanderer} comes back under the hearth-light of {$hearth} with the walk complete. The Counting will not have their name this year; the stone already holds the deed.",
            Open = Beat.Heat(4f),
            Strike = Beat.OnFact(Beat.WithinDistance("$wanderer", "$hearth", 3f)),
            HeatGain = 2f,
            TerminalOutcomeId = "the_walk_comes_home",
        },
        new ChronicleBeatConfig
        {
            Id = "the_white_keeps_them",
            Narrative = "The white keeps {$wanderer}. The walk ended past the last waymark, where only the ice was there to take it in. What the stone holds, the frost keeps.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.Historical("$wanderer")),
            HeatGain = 3f,
            TerminalOutcomeId = "the_white_keeps_them",
        },
        new ChronicleBeatConfig
        {
            Id = "the_far_country_claims_them",
            Narrative = "{$wanderer} let the hearth-line go. Whatever ground holds them now, it is not {$hearth}; the far country has a new name in it.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.PairRelAbsent("$wanderer", "$hearth", "home_base")),
            HeatGain = 2f,
            TerminalOutcomeId = "the_far_country_claims_them",
        },
        new ChronicleBeatConfig
        {
            Id = "the_hearth_goes_cold",
            Narrative = "{$hearth} is gone. The walk of {$wanderer} has no home end now - only the going-out, and the stone to say it happened.",
            Open = Beat.Always(),
            Strike = Beat.OnFact(Beat.Historical("$hearth")),
            HeatGain = 2f,
            TerminalOutcomeId = "the_hearth_goes_cold",
        },
    ];
}
