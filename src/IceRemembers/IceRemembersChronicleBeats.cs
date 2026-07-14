namespace IceRemembers;

using Engine.Domain.Config;

/// <summary>
/// The cross-story beat library. Beats here are written against stances -
/// "protagonist" and "adversary" - not role refs, and apply to every
/// archetype whose stance map covers them. They carry no relationship kinds
/// and no attributes, so any adversarial pair can live them; the wounds,
/// oaths, and endings that need domain specifics stay archetype-authored.
/// </summary>
public static class IceRemembersChronicleBeats
{
    internal const string Protagonist = "protagonist";
    internal const string Adversary = "adversary";

    public static IReadOnlyList<ChronicleBeatConfig> CreateShared() =>
    [
        TheNaming(),
        TheAnswer(),
        BlowUponBlow(),
        TheTideTurns(),
        Flight(),
        TheClosing(),
        AlliesAnswer(),
        TheAdversaryGathers(),
        TheLull(),
        NoQuarter(),
        TheGatheringStorm(),
    ];

    private static ChronicleBeatConfig TheNaming() => new()
    {
        Id = "the_naming",
        Narrative = "{protagonist} moves first against {adversary}. The ice takes notice.",
        Open = Beat.Always(),
        Strike = Beat.PairAttempt(Protagonist, Adversary),
        HeatGain = 2f,
    };

    private static ChronicleBeatConfig TheAnswer() => new()
    {
        Id = "the_answer",
        Narrative = "{adversary} answers in kind. This is no one-sided tale.",
        Open = Beat.Always(),
        Strike = Beat.PairAttempt(Adversary, Protagonist),
        HeatGain = 2f,
    };

    private static ChronicleBeatConfig BlowUponBlow() => new()
    {
        Id = "blow_upon_blow",
        Narrative = "{protagonist} presses on against {adversary}. Blow answers blow.",
        Open = Beat.Heat(3.5f),
        Strike = Beat.PairAttempt(Protagonist, Adversary),
        HeatGain = 2f,
        Charges = [Beat.Charge(Protagonist, Adversary, null, 1f), Beat.Charge(Adversary, Protagonist, null, 1f)],
    };

    private static ChronicleBeatConfig TheTideTurns() => new()
    {
        Id = "the_tide_turns",
        Narrative = "{adversary} turns the tide against {protagonist}.",
        Open = Beat.Heat(3.5f),
        Strike = Beat.PairAttempt(Adversary, Protagonist),
        HeatGain = 2f,
    };

    private static ChronicleBeatConfig Flight() => new()
    {
        Id = "flight",
        Narrative = "{protagonist} and {adversary} lose each other in the white distance.",
        Open = Beat.Heat(2f),
        Strike = Beat.OnFact(Beat.BeyondDistance(Protagonist, Adversary, 40f)),
        HeatGain = 1.5f,
    };

    private static ChronicleBeatConfig TheClosing() => new()
    {
        Id = "the_closing",
        Narrative = "They circle closer. The ice holds its breath.",
        Open = Beat.Heat(4f),
        Strike = Beat.OnFact(Beat.WithinDistance(Protagonist, Adversary, 6f)),
        HeatGain = 2.5f,
        Charges = [Beat.Charge(Protagonist, Adversary, null, 2f, heatScaled: true)],
    };

    private static ChronicleBeatConfig AlliesAnswer() => new()
    {
        Id = "allies_answer",
        Narrative = "Another blade turns toward {adversary}. The tale gathers company.",
        Open = Beat.Always(),
        Strike = Beat.OutsiderOn(Adversary),
        HeatGain = 2f,
    };

    private static ChronicleBeatConfig TheAdversaryGathers() => new()
    {
        Id = "the_adversary_gathers",
        Narrative = "The world turns on {protagonist}. The tale darkens.",
        Open = Beat.Always(),
        Strike = Beat.OutsiderOn(Protagonist),
        HeatGain = 2f,
    };

    private static ChronicleBeatConfig TheLull() => new()
    {
        Id = "the_lull",
        Narrative = "They draw apart to breathe. The ice waits.",
        Open = Beat.Heat(5.5f),
        Strike = Beat.OnFact(Beat.HeatBelow(2.5f)),
        HeatGain = 1f,
    };

    private static ChronicleBeatConfig NoQuarter() => new()
    {
        Id = "no_quarter",
        Narrative = "Past this point there is no stepping back for {protagonist} or {adversary}.",
        Open = Beat.Heat(8f),
        Strike = Beat.PairAttempt(Protagonist, Adversary),
        HeatGain = 2.5f,
        Charges =
        [
            Beat.Charge(Protagonist, Adversary, null, 2.5f, heatScaled: true),
            Beat.Charge(Adversary, Protagonist, null, 2.5f, heatScaled: true),
        ],
    };

    private static ChronicleBeatConfig TheGatheringStorm() => new()
    {
        Id = "the_gathering_storm",
        Narrative = "What began between {protagonist} and {adversary} now pulls at the world around them.",
        Open = Beat.Heat(6f),
        Strike = Beat.OnFact(Beat.Heat(7.5f)),
        HeatGain = 1.5f,
        Charges =
        [
            Beat.Charge(Protagonist, Adversary, null, 2f, heatScaled: true),
            Beat.Charge(Adversary, Protagonist, null, 2f, heatScaled: true),
        ],
    };
}

/// <summary>Shared beat-authoring vocabulary for chronicle definitions.</summary>
internal static class Beat
{
    internal static ChronicleConditionConfig Always() =>
        new() { Kind = ChronicleConditionKind.Always };

    internal static ChronicleConditionConfig Heat(float value) =>
        new() { Kind = ChronicleConditionKind.HeatAtLeast, Value = value };

    internal static ChronicleConditionConfig HeatBelow(float value) =>
        new() { Kind = ChronicleConditionKind.HeatBelow, Value = value };

    internal static ChronicleConditionConfig AttrAtLeast(string role, string attribute, float value) =>
        new() { Kind = ChronicleConditionKind.RoleAttributeAtLeast, RoleA = role, Attribute = attribute, Value = value };

    internal static ChronicleConditionConfig PairRel(string roleA, string roleB, string kind, float value) =>
        new() { Kind = ChronicleConditionKind.RolePairRelationship, RoleA = roleA, RoleB = roleB, RelationshipKind = kind, Value = value };

    internal static ChronicleConditionConfig PairRelAbsent(string roleA, string roleB, string kind) =>
        new() { Kind = ChronicleConditionKind.RolePairRelationshipAbsent, RoleA = roleA, RoleB = roleB, RelationshipKind = kind, Value = 0.05f };

    internal static ChronicleConditionConfig Historical(string role) =>
        new() { Kind = ChronicleConditionKind.RoleHistorical, RoleA = role };

    internal static ChronicleConditionConfig AttrBelow(string role, string attribute, float value) =>
        new() { Kind = ChronicleConditionKind.RoleAttributeBelow, RoleA = role, Attribute = attribute, Value = value };

    internal static ChronicleConditionConfig WithinDistance(string roleA, string roleB, float distance) =>
        new() { Kind = ChronicleConditionKind.RolePairWithinDistance, RoleA = roleA, RoleB = roleB, Value = distance };

    internal static ChronicleConditionConfig BeyondDistance(string roleA, string roleB, float distance) =>
        new() { Kind = ChronicleConditionKind.RolePairBeyondDistance, RoleA = roleA, RoleB = roleB, Value = distance };

    internal static ChronicleStrikeConfig PairGambit(string actor, string target, string? gambit = null) =>
        new() { Kind = ChronicleStrikeKind.RolePairGambit, ActorRole = actor, TargetRole = target, Gambit = gambit };

    /// <summary>For outcome-neutral narratives only: the attempt itself is the beat.</summary>
    internal static ChronicleStrikeConfig PairAttempt(string actor, string target, string? gambit = null) =>
        new() { Kind = ChronicleStrikeKind.RolePairGambit, ActorRole = actor, TargetRole = target, Gambit = gambit, AcceptFailure = true };

    internal static ChronicleStrikeConfig OutsiderOn(string target, string? gambit = null) =>
        new() { Kind = ChronicleStrikeKind.OutsiderGambitOnRole, TargetRole = target, Gambit = gambit };

    /// <summary>Entrance: the outsider acting on the role joins the open role (attempt suffices).</summary>
    internal static ChronicleStrikeConfig OutsiderJoins(string target, string joinsRole, string? gambit = null) =>
        new() { Kind = ChronicleStrikeKind.OutsiderGambitOnRole, TargetRole = target, Gambit = gambit, JoinsRole = joinsRole, AcceptFailure = true };

    /// <summary>Entrance: the outside target of the role's deed joins the open role (on success).</summary>
    internal static ChronicleStrikeConfig RoleDrawsIn(string actor, string gambit, string joinsRole) =>
        new() { Kind = ChronicleStrikeKind.RoleGambit, ActorRole = actor, Gambit = gambit, JoinsRole = joinsRole };

    internal static ChronicleStrikeConfig RoleActs(string actor, string gambit) =>
        new() { Kind = ChronicleStrikeKind.RoleGambit, ActorRole = actor, Gambit = gambit };

    internal static ChronicleStrikeConfig GeneratorOn(string target, string generator) =>
        new() { Kind = ChronicleStrikeKind.GeneratorOnRole, TargetRole = target, Generator = generator };

    internal static ChronicleStrikeConfig OnFact(ChronicleConditionConfig fact) =>
        new() { Kind = ChronicleStrikeKind.StateFact, Fact = fact };

    internal static ChronicleChargeConfig Charge(string actor, string target, string? gambit, float bonus, bool heatScaled = false) =>
        new() { ActorRole = actor, TargetRole = target, Gambit = gambit, ScoreBonus = bonus, HeatScaled = heatScaled };

    internal static ChronicleGrantConfig Grant(string role, string gambit) =>
        new() { Role = role, Gambit = gambit };
}
