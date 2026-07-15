namespace IceRemembers;

using Engine.Domain.Config;

/// <summary>
/// The ages of the world: the beat treatment applied to the world itself.
/// Three regime tracks - the Ice (the land's physical mood), the Peoples
/// (the social regime), the Memory (the semantic regime) - each holding at
/// most one open age. Ages open and close on recognized macro facts, never
/// timers, and while open they charge the rates of ordinary authored
/// content. The Still Ice is the Ice track's fallback, so at least one age
/// is always open. Thresholds are calibrated against the 25k-tick bootstrap
/// (run kin-tide-3x25k-20260715d); they are authored content, tuned by
/// evidence like any other balance value.
/// </summary>
public static class IceRemembersAges
{
    public static IReadOnlyList<WorldBeatAgeConfig> Create() =>
    [
        TheThaw(),
        TheStillIce(),
        TheLongDark(),
        AgeOfStriving(),
        AgeOfAccords(),
        AgeOfSigns(),
        AgeOfCanons(),
    ];

    /// <summary>
    /// The founding regime: the world is young and unsaturated, and the ice
    /// rewards those who go out into it. Journeys and settlement charters
    /// run charged until the settled world fills in.
    /// </summary>
    private static WorldBeatAgeConfig TheThaw() => new()
    {
        Id = "age_the_thaw",
        Name = "The Thaw",
        Description = "The ice has opened and the world is young. Wandering seasons run long, pilgrims cross the white, and new holdings take root where no banner has stood.",
        Track = WorldBeatTrack.Ice,
        Priority = 10,
        MinOpenTicks = 5000,
        CooldownTicks = 10000,
        OpenWhen =
        [
            new(WorldBeatFact.TotalCultureRawMass, WorldBeatComparator.Below, 400_000f),
        ],
        CloseWhen =
        [
            new(WorldBeatFact.ActiveEntitiesOfKind, WorldBeatComparator.AtLeast, 14f, Kind: "settlement"),
        ],
        Charges =
        [
            new(WorldBeatChargeDomain.VerbWeight, 1.6f, "penguin_wandering_season"),
            new(WorldBeatChargeDomain.VerbWeight, 1.6f, "penguin_pilgrimage"),
            new(WorldBeatChargeDomain.VerbWeight, 1.6f, "orca_far_currents"),
            new(WorldBeatChargeDomain.VerbWeight, 1.5f, "penguin_follow_the_gathering"),
            new(WorldBeatChargeDomain.VerbWeight, 1.5f, "orca_follow_the_gathering"),
            new(WorldBeatChargeDomain.GambitScore, 2f, "faction_charters_settlement"),
            new(WorldBeatChargeDomain.GeneratorPulseWeight, 1.5f, "charters_find_harbors"),
            new(WorldBeatChargeDomain.GeneratorPulseWeight, 1.5f, "pilgrims_found_new_currents"),
        ],
    };

    /// <summary>
    /// Ordinary time on the Ice track: no charges, always eligible, lowest
    /// priority. Its existence guarantees the invariant that at least one
    /// age is open somewhere.
    /// </summary>
    private static WorldBeatAgeConfig TheStillIce() => new()
    {
        Id = "age_the_still_ice",
        Name = "The Still Ice",
        Description = "The ice lies quiet. Seasons turn, holdings keep their fires, and nothing presses harder than it must.",
        Track = WorldBeatTrack.Ice,
        Priority = 0,
        MinOpenTicks = 2500,
        CooldownTicks = 0,
    };

    /// <summary>
    /// Stagnation recognized: the settled world has stopped growing. The
    /// tide runs harder against every contested holding - the ice reclaims
    /// what the living no longer press to keep.
    /// </summary>
    private static WorldBeatAgeConfig TheLongDark() => new()
    {
        Id = "age_the_long_dark",
        Name = "The Long Dark",
        Description = "Growth has gone out of the world. The dark presses in on every frontier, and only what is defended is kept.",
        Track = WorldBeatTrack.Ice,
        Priority = 5,
        MinOpenTicks = 4000,
        CooldownTicks = 8000,
        OpenWhen =
        [
            new(WorldBeatFact.TicksSinceStart, WorldBeatComparator.AtLeast, 15_000f),
            new(WorldBeatFact.CultureMassGrowthPermille, WorldBeatComparator.Below, 5f),
        ],
        CloseWhen =
        [
            new(WorldBeatFact.CultureMassGrowthPermille, WorldBeatComparator.AtLeast, 12f),
        ],
        Charges =
        [
            new(WorldBeatChargeDomain.ContestDecayTide, 1.5f),
        ],
    };

    /// <summary>
    /// The war regime: enough wars burn at once that the world reorganizes
    /// around them. Campaigns march, the contest tide runs high, and war's
    /// own machinery is charged.
    /// </summary>
    private static WorldBeatAgeConfig AgeOfStriving() => new()
    {
        Id = "age_of_striving",
        Name = "The Age of Striving",
        Description = "The banners are out. Wars burn across the ice and the peoples measure themselves against each other in taken ground.",
        Track = WorldBeatTrack.Peoples,
        Priority = 10,
        MinOpenTicks = 3000,
        CooldownTicks = 4000,
        OpenWhen =
        [
            new(WorldBeatFact.LivingRelationshipsOfKind, WorldBeatComparator.AtLeast, 20f, RelationshipKind: "at_war_with"),
        ],
        CloseWhen =
        [
            new(WorldBeatFact.LivingRelationshipsOfKind, WorldBeatComparator.Below, 12f, RelationshipKind: "at_war_with"),
        ],
        Charges =
        [
            new(WorldBeatChargeDomain.GambitScore, 4f, "faction_marches_the_banners"),
            new(WorldBeatChargeDomain.GambitScore, 1.5f, "faction_declares_territorial_war"),
            new(WorldBeatChargeDomain.GeneratorPulseWeight, 1.5f, "war_takes_its_toll"),
            new(WorldBeatChargeDomain.GeneratorPulseWeight, 2f, "the_column_holds"),
            new(WorldBeatChargeDomain.GeneratorPulseWeight, 1.5f, "wars_get_names"),
            new(WorldBeatChargeDomain.ContestDecayTide, 2f),
        ],
    };

    /// <summary>
    /// The diplomacy regime: enough wars have ended that the peoples turn to
    /// accords. Opens on war exhaustion - the campaigns themselves feed this
    /// fact - and breaks when war returns in force.
    /// </summary>
    private static WorldBeatAgeConfig AgeOfAccords() => new()
    {
        Id = "age_of_accords",
        Name = "The Age of Accords",
        Description = "The peoples are tired of burning. Envoys cross where columns marched, and what was taken is written down and traded back.",
        Track = WorldBeatTrack.Peoples,
        Priority = 5,
        MinOpenTicks = 3000,
        CooldownTicks = 4000,
        OpenWhen =
        [
            new(WorldBeatFact.CulledRelationshipsOfKind, WorldBeatComparator.AtLeast, 40f, RelationshipKind: "at_war_with"),
            new(WorldBeatFact.LivingRelationshipsOfKind, WorldBeatComparator.Below, 20f, RelationshipKind: "at_war_with"),
        ],
        CloseWhen =
        [
            new(WorldBeatFact.LivingRelationshipsOfKind, WorldBeatComparator.AtLeast, 24f, RelationshipKind: "at_war_with"),
        ],
        Charges =
        [
            new(WorldBeatChargeDomain.GambitScore, 2f, "faction_sues_for_peace"),
            new(WorldBeatChargeDomain.GambitScore, 2f, "faction_swears_cold_accord"),
            new(WorldBeatChargeDomain.GeneratorPulseWeight, 1.5f, "the_accord_convenes"),
            new(WorldBeatChargeDomain.GeneratorPulseWeight, 1.5f, "trade_routes_convene_negotiation"),
        ],
    };

    /// <summary>
    /// The semantic regime of spreading practice: techniques move from hand
    /// to hand faster than institutions can pin them down.
    /// </summary>
    private static WorldBeatAgeConfig AgeOfSigns() => new()
    {
        Id = "age_of_signs",
        Name = "The Age of Signs",
        Description = "The forms are loose in the world. Practices pass from teacher to student faster than any canon can claim them.",
        Track = WorldBeatTrack.Memory,
        Priority = 5,
        MinOpenTicks = 3000,
        CooldownTicks = 4000,
        OpenWhen =
        [
            new(WorldBeatFact.ActiveEntitiesOfKind, WorldBeatComparator.AtLeast, 6f, Kind: "ability"),
        ],
        CloseWhen =
        [
            new(WorldBeatFact.ActiveEntitiesOfKind, WorldBeatComparator.AtLeast, 8f, Kind: "canon"),
        ],
        Charges =
        [
            new(WorldBeatChargeDomain.GambitScore, 1.75f, "ability_shares_practice"),
            new(WorldBeatChargeDomain.GeneratorPulseWeight, 1.5f, "mapped_peaks_birth_techniques"),
            new(WorldBeatChargeDomain.GeneratorPulseWeight, 1.5f, "practiced_forms_find_tomes"),
        ],
    };

    /// <summary>
    /// The semantic regime of hardened memory: the canons hold the world's
    /// story and remembrance flows through institutions.
    /// </summary>
    private static WorldBeatAgeConfig AgeOfCanons() => new()
    {
        Id = "age_of_canons",
        Name = "The Age of Canons",
        Description = "What the ice remembers is written now. The canons keep the tally, and a deed matters as much for who records it as for who did it.",
        Track = WorldBeatTrack.Memory,
        Priority = 10,
        MinOpenTicks = 4000,
        CooldownTicks = 6000,
        OpenWhen =
        [
            new(WorldBeatFact.ActiveEntitiesOfKind, WorldBeatComparator.AtLeast, 8f, Kind: "canon"),
            new(WorldBeatFact.LegendaryResolutions, WorldBeatComparator.AtLeast, 1f),
        ],
        CloseWhen =
        [
            new(WorldBeatFact.ActiveEntitiesOfKind, WorldBeatComparator.Below, 6f, Kind: "canon"),
        ],
        Charges =
        [
            new(WorldBeatChargeDomain.GeneratorPulseWeight, 1.5f, "the_ice_takes_notice"),
            new(WorldBeatChargeDomain.GeneratorPulseWeight, 1.5f, "legend_enters_the_canon"),
            new(WorldBeatChargeDomain.GeneratorPulseWeight, 1.5f, "records_cast_shadows"),
        ],
    };
}
