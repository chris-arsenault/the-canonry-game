namespace IceRemembers;

using Engine.Domain.Config;

/// <summary>
/// The world beats: recurring moods the world falls into, not a timeline it
/// passes through once. Two tracks. The WORLD track is the Berg's own
/// condition - its seasons cycle as its fortunes rise and fall (a default
/// mood always holds it), and rare physical or cosmic breaks preempt them.
/// The PEOPLES track is what the three cultures are doing - everyday activity
/// that a default always holds, and the epochal eras that break the routine.
///
/// Openings are loose and closings hysteretic, so beats come and go and
/// recur; each amplifies the very thing it is named for, so an active world
/// feels more like itself. Grounded in the lore bible: the Endless Day and
/// the Long Dark, the Thaw and the Freeze; the Hunt, the Trade, the Craft;
/// the Faction Wars, the Orca Incursion, the Frozen Peace, the Clever Ice Age.
/// Thresholds are calibrated to observed run scale and tuned by evidence.
/// </summary>
public static class IceRemembersAges
{
    public static IReadOnlyList<WorldBeatAgeConfig> Create() =>
    [
        // --- WORLD track: fortune-cycle fallbacks (one always holds) ---
        TheEndlessDay(),
        TheThaw(),
        TheFreeze(),
        TheLongDark(),
        // --- WORLD track: rare physical / cosmic breaks ---
        TheHeartbeatQuickens(),
        TheBeforeTimesSurface(),
        TheGreatThaw(),
        TheGreatFreeze(),
        // --- PEOPLES track: everyday activity fallbacks (one always holds) ---
        TheWitnessedLife(),
        TheHunt(),
        TheFarWandering(),
        TheGreatCrossing(),
        TheLongTrade(),
        TheDeepeningCraft(),
        // --- PEOPLES track: epochal eras ---
        TheFactionWars(),
        TheOrcaIncursion(),
        TheFrozenPeace(),
        TheCleverIceAge(),
    ];

    // ------------------------------------------------------------------
    // WORLD track fallbacks - the seasons as fortune-moods, on growth bands.
    // ------------------------------------------------------------------

    private static WorldBeatAgeConfig TheEndlessDay() => new()
    {
        Id = "age_the_endless_day",
        Name = "The Endless Day",
        Description = "The sun does not set. Seas teem with krill, the ice retreats, and the colonies gorge and forget. An ordinary good season - the mood the world keeps when nothing presses harder.",
        Track = WorldBeatTrack.World,
        Priority = 1,
        MinOpenTicks = 2000,
        CooldownTicks = 0,
        Charges =
        [
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.3f, "charters_find_harbors"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.3f, "sanctuaries_anchor_colonies"),
            new(WorldBeatChargeDomain.VerbWeight, 1.2f, "penguin_wandering_season"),
        ],
    };

    private static WorldBeatAgeConfig TheThaw() => new()
    {
        Id = "age_the_thaw",
        Name = "The Thaw",
        Description = "The ice-break screams across the Berg. Channels open, new waters bare themselves, and the bold go out into ground no banner has stood on. The world is opening.",
        Track = WorldBeatTrack.World,
        Priority = 3,
        MinOpenTicks = 1500,
        CooldownTicks = 1500,
        OpenWhen = [Growth(WorldBeatComparator.AtLeast, 30f)],
        CloseWhen = [Growth(WorldBeatComparator.Below, 20f)],
        Charges =
        [
            new(WorldBeatChargeDomain.VerbWeight, 1.5f, "penguin_wandering_season"),
            new(WorldBeatChargeDomain.VerbWeight, 1.5f, "penguin_pilgrimage"),
            new(WorldBeatChargeDomain.VerbWeight, 1.5f, "orca_far_currents"),
            new(WorldBeatChargeDomain.GambitScore, 1.6f, "faction_charters_settlement"),
            new(WorldBeatChargeDomain.MovementStepRate, 0.85f),
        ],
    };

    private static WorldBeatAgeConfig TheFreeze() => new()
    {
        Id = "age_the_freeze",
        Name = "The Freeze",
        Description = "The quartermasters post the food arithmetic - calories per penguin against days remaining. Channels narrow, the Last Catch runs like a war, and everyone draws in.",
        Track = WorldBeatTrack.World,
        Priority = 3,
        MinOpenTicks = 1500,
        CooldownTicks = 1500,
        OpenWhen = [Growth(WorldBeatComparator.Below, 12f)],
        CloseWhen = [Growth(WorldBeatComparator.AtLeast, 18f)],
        Charges =
        [
            new(WorldBeatChargeDomain.VerbWeight, 0.7f, "penguin_wandering_season"),
            new(WorldBeatChargeDomain.VerbWeight, 0.7f, "penguin_pilgrimage"),
            new(WorldBeatChargeDomain.MovementStepRate, 1.15f),
            new(WorldBeatChargeDomain.ContestDecayTide, 1.3f),
        ],
    };

    private static WorldBeatAgeConfig TheLongDark() => new()
    {
        Id = "age_the_long_dark",
        Name = "The Long Dark",
        Description = "Four months without sunrise. The sea seals for miles, the aurora burns brighter than ever, and the boundary with the ice-memory grows thin - the dead speak, and the untended edges recede into the ice.",
        Track = WorldBeatTrack.World,
        Priority = 4,
        MinOpenTicks = 2000,
        CooldownTicks = 2500,
        OpenWhen = [Growth(WorldBeatComparator.Below, 3f)],
        CloseWhen = [Growth(WorldBeatComparator.AtLeast, 8f)],
        Charges =
        [
            new(WorldBeatChargeDomain.GlobalDecayTide, 4f),
            new(WorldBeatChargeDomain.ContestDecayTide, 1.5f),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.4f, "the_ice_takes_notice"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.4f, "records_cast_shadows"),
            new(WorldBeatChargeDomain.MovementStepRate, 1.2f),
            // Nobody sets out into the white while the sun is gone: the
            // scouting pushes and founding chains ebb until the Thaw.
            new(WorldBeatChargeDomain.VerbWeight, 0.5f, "penguin_scout_the_white"),
            new(WorldBeatChargeDomain.VerbWeight, 0.6f, "transport_voyage_beyond_the_charts"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 0.5f, "the_thaw_calls_the_bold"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 0.5f, "sanctuaries_anchor_colonies"),
        ],
    };

    // ------------------------------------------------------------------
    // WORLD track hard beats - rare physical and cosmic breaks.
    // ------------------------------------------------------------------

    private static WorldBeatAgeConfig TheHeartbeatQuickens() => new()
    {
        Id = "age_the_heartbeat_quickens",
        Name = "The Heartbeat Quickens",
        Description = "The Corpse Current runs warmer and reaches farther. The Dead God stirs in its dreaming; the deep ice wakes what should sleep, and the Wake-Singers grow bold with the smell of copper in the water.",
        Track = WorldBeatTrack.World,
        Priority = 8,
        MinOpenTicks = 2500,
        CooldownTicks = 3000,
        OpenWhen = [Kind(WorldBeatComparator.AtLeast, 10f, "monster")],
        CloseWhen = [Kind(WorldBeatComparator.Below, 6f, "monster")],
        Charges =
        [
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.6f, "corruption_wakes_monsters"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.6f, "monsters_stalk_the_marches"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.5f, "kills_sung_to_the_deep"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.5f, "glory_to_the_dead_god"),
            new(WorldBeatChargeDomain.GambitScore, 1.5f, "artifact_binds_monster"),
        ],
    };

    private static WorldBeatAgeConfig TheBeforeTimesSurface() => new()
    {
        Id = "age_the_before_times_surface",
        Name = "The Before Times Surface",
        Description = "The melt bares what should stay buried. Relics with serial numbers in no known tongue rise from the deep ice, radiating power that makes the ice-memory itself recoil - and every hunter and shadow-broker wants them.",
        Track = WorldBeatTrack.World,
        Priority = 7,
        MinOpenTicks = 2500,
        CooldownTicks = 3000,
        OpenWhen = [Kind(WorldBeatComparator.AtLeast, 65f, "artifact")],
        CloseWhen = [Kind(WorldBeatComparator.Below, 50f, "artifact")],
        Charges =
        [
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.6f, "what_the_artifact_knows"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.6f, "stolen_artifacts_draw_hunters"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.4f, "vigils_leave_relics"),
            new(WorldBeatChargeDomain.GambitScore, 1.4f, "artifact_binds_monster"),
        ],
    };

    private static WorldBeatAgeConfig TheGreatThaw() => new()
    {
        Id = "age_the_great_thaw",
        Name = "The Great Thaw",
        Description = "The great sheets retreat all at once. An age of expansion and optimism - waters rich with krill and ancient secrets, bold explorers claiming ground, guild halls rising on charters of pressed ice. The world opens wide.",
        Track = WorldBeatTrack.World,
        Priority = 9,
        MinOpenTicks = 3000,
        CooldownTicks = 6000,
        OpenWhen =
        [
            new(WorldBeatFact.TicksSinceStart, WorldBeatComparator.Below, 9000f),
            Growth(WorldBeatComparator.AtLeast, 55f),
        ],
        CloseWhen = [Growth(WorldBeatComparator.Below, 35f)],
        Charges =
        [
            new(WorldBeatChargeDomain.VerbWeight, 1.8f, "penguin_wandering_season"),
            new(WorldBeatChargeDomain.VerbWeight, 1.8f, "penguin_pilgrimage"),
            new(WorldBeatChargeDomain.GambitScore, 2f, "faction_charters_settlement"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.6f, "charters_find_harbors"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.6f, "pilgrims_found_new_currents"),
            new(WorldBeatChargeDomain.MovementStepRate, 0.8f),
        ],
    };

    private static WorldBeatAgeConfig TheGreatFreeze() => new()
    {
        Id = "age_the_great_freeze",
        Name = "The Great Freeze",
        Description = "The Final Seal writ across the whole Berg. Growth does not slow - it reverses. The cold does not hunt; it waits, and takes back everything the living no longer press to hold.",
        Track = WorldBeatTrack.World,
        Priority = 9,
        MinOpenTicks = 3000,
        CooldownTicks = 6000,
        OpenWhen =
        [
            new(WorldBeatFact.TicksSinceStart, WorldBeatComparator.AtLeast, 12000f),
            Growth(WorldBeatComparator.Below, 0f),
        ],
        CloseWhen = [Growth(WorldBeatComparator.AtLeast, 6f)],
        Charges =
        [
            new(WorldBeatChargeDomain.GlobalDecayTide, 8f),
            new(WorldBeatChargeDomain.ContestDecayTide, 2f),
            new(WorldBeatChargeDomain.MovementStepRate, 1.3f),
        ],
    };

    // ------------------------------------------------------------------
    // PEOPLES track fallbacks - everyday activity (one always holds).
    // ------------------------------------------------------------------

    private static WorldBeatAgeConfig TheWitnessedLife() => new()
    {
        Id = "age_the_witnessed_life",
        Name = "The Witnessed Life",
        Description = "The ordinary work of being seen. Names accumulate their echoes, deeds are recited in the Trade Terraces, and the ice records who the living are becoming. The mood the peoples keep between the greater eras.",
        Track = WorldBeatTrack.Peoples,
        Priority = 1,
        MinOpenTicks = 2000,
        CooldownTicks = 0,
        Charges =
        [
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.3f, "the_ice_takes_notice"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.3f, "legend_enters_the_canon"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.2f, "monster_slayings_become_festivals"),
        ],
    };

    private static WorldBeatAgeConfig TheFarWandering() => new()
    {
        Id = "age_the_far_wandering",
        Name = "The Far Wandering",
        Description = "The mood of the Great Thaw returns: bold crews walk past the last remembrance-stone, ships stand out beyond the charts, and every marginal rookery dreams of becoming a great berg. The ice from such years has a brightness, as if even the frost believed things were going well.",
        Track = WorldBeatTrack.Peoples,
        Priority = 4,
        MinOpenTicks = 1800,
        CooldownTicks = 1500,
        OpenWhen = [Kind(WorldBeatComparator.AtLeast, 7f, "settlement")],
        CloseWhen = [Kind(WorldBeatComparator.Below, 5f, "settlement")],
        Charges =
        [
            new(WorldBeatChargeDomain.VerbWeight, 1.8f, "penguin_scout_the_white"),
            new(WorldBeatChargeDomain.VerbWeight, 1.8f, "orca_sound_the_far_water"),
            new(WorldBeatChargeDomain.VerbWeight, 2f, "transport_voyage_beyond_the_charts"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.6f, "the_thaw_calls_the_bold"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.6f, "sanctuaries_anchor_colonies"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.5f, "charters_find_harbors"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.5f, "the_river_earns_its_name"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.5f, "the_still_water_is_sounded"),
            new(WorldBeatChargeDomain.MovementStepRate, 0.9f),
        ],
    };

    private static WorldBeatAgeConfig TheGreatCrossing() => new()
    {
        Id = "age_the_great_crossing",
        Name = "The Great Crossing",
        Description = "Once in a generation the peoples attempt what Piien attempted: a crossing meant to outlast the food math. Whole flotillas commit to the far water, and either the charts grow a new coast or the sea keeps its answer. The ice records these years carefully, the way it does before events it considers significant.",
        Track = WorldBeatTrack.Peoples,
        Priority = 6,
        MinOpenTicks = 1200,
        CooldownTicks = 6000,
        OpenWhen =
        [
            Kind(WorldBeatComparator.AtLeast, 10f, "settlement"),
            Kind(WorldBeatComparator.AtLeast, 4f, "transport"),
        ],
        CloseWhen = [Kind(WorldBeatComparator.Below, 3f, "transport")],
        Charges =
        [
            new(WorldBeatChargeDomain.VerbWeight, 2.5f, "transport_voyage_beyond_the_charts"),
            new(WorldBeatChargeDomain.VerbWeight, 2.2f, "orca_sound_the_far_water"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 2f, "bulls_range_the_far_currents"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.8f, "conduit_points_found_reefholds"),
            new(WorldBeatChargeDomain.MovementStepRate, 0.85f),
        ],
    };

    private static WorldBeatAgeConfig TheHunt() => new()
    {
        Id = "age_the_hunt",
        Name = "The Hunt",
        Description = "Quarry is on the ice and in the water. The orca sing their tactics, the penguins take up the old forms, and the beast is cornered and slain - for the hunt is the fundamental activity of being alive here.",
        Track = WorldBeatTrack.Peoples,
        Priority = 4,
        MinOpenTicks = 1500,
        CooldownTicks = 1000,
        OpenWhen = [Kind(WorldBeatComparator.AtLeast, 3f, "monster")],
        CloseWhen = [Kind(WorldBeatComparator.Below, 1f, "monster")],
        Charges =
        [
            new(WorldBeatChargeDomain.GambitScore, 1.6f, "penguin_slays_monster"),
            new(WorldBeatChargeDomain.GambitScore, 1.6f, "penguin_corners_the_beast"),
            new(WorldBeatChargeDomain.GambitSuccess, 1.4f, "penguin_slays_monster"),
            new(WorldBeatChargeDomain.GambitSuccess, 1.4f, "penguin_corners_the_beast"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.5f, "wardens_rise_where_the_hunt_bites"),
        ],
    };

    private static WorldBeatAgeConfig TheLongTrade() => new()
    {
        Id = "age_the_long_trade",
        Name = "The Long Trade",
        Description = "The couriers walk the Accord route and the shipments move. Fire-cores rise to the surface, aurora-crystals sink to the dark, and neither colony can afford to let the other die. Commerce, not friendship - but commerce.",
        Track = WorldBeatTrack.Peoples,
        Priority = 4,
        MinOpenTicks = 1800,
        CooldownTicks = 1200,
        OpenWhen =
        [
            Kind(WorldBeatComparator.AtLeast, 8f, "settlement"),
            War(WorldBeatComparator.Below, 12f),
        ],
        CloseWhen = [War(WorldBeatComparator.AtLeast, 18f)],
        Charges =
        [
            new(WorldBeatChargeDomain.GambitScore, 1.6f, "settlement_opens_trade_route"),
            new(WorldBeatChargeDomain.GambitScore, 1.5f, "transport_opens_conduit"),
            new(WorldBeatChargeDomain.GambitScore, 1.5f, "transport_takes_up_the_passenger"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.4f, "trade_routes_convene_negotiation"),
        ],
    };

    private static WorldBeatAgeConfig TheDeepeningCraft() => new()
    {
        Id = "age_the_deepening_craft",
        Name = "The Deepening Craft",
        Description = "The forms pass from hand to hand. Ice-readers deepen their sight, forge-masters teach the core-crack, and technique spreads faster than any one keeper can hold it. The craft is alive in the world.",
        Track = WorldBeatTrack.Peoples,
        Priority = 4,
        MinOpenTicks = 1800,
        CooldownTicks = 1200,
        OpenWhen = [Kind(WorldBeatComparator.AtLeast, 5f, "ability")],
        CloseWhen = [Kind(WorldBeatComparator.Below, 3f, "ability")],
        Charges =
        [
            new(WorldBeatChargeDomain.GambitScore, 1.6f, "ability_shares_practice"),
            new(WorldBeatChargeDomain.GambitSuccess, 1.4f, "ability_shares_practice"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.5f, "mapped_peaks_birth_techniques"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.5f, "practiced_forms_find_tomes"),
        ],
    };

    // ------------------------------------------------------------------
    // PEOPLES track hard beats - the epochal eras.
    // ------------------------------------------------------------------

    private static WorldBeatAgeConfig TheFactionWars() => new()
    {
        Id = "age_the_faction_wars",
        Name = "The Faction Wars",
        Description = "Trade routes become battle lines. Old alliances fracture along cultural fault lines and each faction claims the true path to survival - dressed in ideology to make it seem about something other than krill.",
        Track = WorldBeatTrack.Peoples,
        Priority = 9,
        MinOpenTicks = 3000,
        CooldownTicks = 3000,
        OpenWhen = [War(WorldBeatComparator.AtLeast, 20f)],
        CloseWhen = [War(WorldBeatComparator.Below, 12f)],
        Charges =
        [
            new(WorldBeatChargeDomain.GambitScore, 4f, "faction_marches_the_banners"),
            new(WorldBeatChargeDomain.GambitScore, 1.6f, "faction_declares_territorial_war"),
            new(WorldBeatChargeDomain.GambitSuccess, 1.5f, "faction_marches_the_banners"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 2.4f, "war_takes_its_toll"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.6f, "wars_get_names"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.4f, "the_column_holds"),
            new(WorldBeatChargeDomain.ContestDecayTide, 2f),
        ],
    };

    private static WorldBeatAgeConfig TheOrcaIncursion() => new()
    {
        Id = "age_the_orca_incursion",
        Name = "The Orca Incursion",
        Description = "The orca come not as raiders but as conquerors, their war-pods striking with coordination that speaks of dark intelligence. Old feuds between Stack and Shelf fade as the colonies band together against extinction.",
        Track = WorldBeatTrack.Peoples,
        Priority = 8,
        MinOpenTicks = 3000,
        CooldownTicks = 3000,
        OpenWhen = [Kind(WorldBeatComparator.AtLeast, 9f, "orca")],
        CloseWhen = [Kind(WorldBeatComparator.Below, 6f, "orca")],
        Charges =
        [
            new(WorldBeatChargeDomain.GambitScore, 2f, "orca_raids_trade_route"),
            new(WorldBeatChargeDomain.GambitSuccess, 1.5f, "orca_raids_trade_route"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.6f, "pods_hunt_the_convoys"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.6f, "claimed_waters_draw_bulls"),
            new(WorldBeatChargeDomain.GambitScore, 1.5f, "faction_swears_cold_accord"),
        ],
    };

    private static WorldBeatAgeConfig TheFrozenPeace() => new()
    {
        Id = "age_the_frozen_peace",
        Name = "The Frozen Peace",
        Description = "Not peace - exhaustion dressed as stability. The threat recedes, leaving generational scars; survivors rebuild the bonds between cultures, and great festivals return, tinged with remembrance for the fallen.",
        Track = WorldBeatTrack.Peoples,
        Priority = 6,
        MinOpenTicks = 3000,
        CooldownTicks = 3000,
        OpenWhen =
        [
            new(WorldBeatFact.CulledRelationshipsOfKind, WorldBeatComparator.AtLeast, 30f, RelationshipKind: "at_war_with"),
            War(WorldBeatComparator.Below, 22f),
        ],
        CloseWhen = [War(WorldBeatComparator.AtLeast, 30f)],
        Charges =
        [
            new(WorldBeatChargeDomain.GambitScore, 2f, "faction_sues_for_peace"),
            new(WorldBeatChargeDomain.GambitScore, 2f, "faction_swears_cold_accord"),
            new(WorldBeatChargeDomain.GambitScore, 0.5f, "faction_declares_territorial_war"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.5f, "the_accord_convenes"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.4f, "conferences_become_festivals"),
        ],
    };

    private static WorldBeatAgeConfig TheCleverIceAge() => new()
    {
        Id = "age_the_clever_ice_age",
        Name = "The Clever Ice Age",
        Description = "War-weary, the civilizations turn to cleverness over conflict. Guilds found schools of magic and workshops of invention, unlocking fire-cores and spectralum lenses. An age of wonders - and of things the ice records but the histories do not name.",
        Track = WorldBeatTrack.Peoples,
        Priority = 7,
        MinOpenTicks = 3000,
        CooldownTicks = 4000,
        OpenWhen = [Kind(WorldBeatComparator.AtLeast, 10f, "ability")],
        CloseWhen = [Kind(WorldBeatComparator.Below, 7f, "ability")],
        Charges =
        [
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.8f, "mapped_peaks_birth_techniques"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.8f, "practiced_forms_find_tomes"),
            new(WorldBeatChargeDomain.GeneratorFireWeight, 1.5f, "wardens_take_up_the_old_forms"),
            new(WorldBeatChargeDomain.GambitScore, 1.6f, "ability_shares_practice"),
            new(WorldBeatChargeDomain.GambitScore, 0.6f, "faction_declares_territorial_war"),
        ],
    };

    private static WorldBeatConditionConfig Growth(WorldBeatComparator comparator, float permille) =>
        new(WorldBeatFact.CultureMassGrowthPermille, comparator, permille);

    private static WorldBeatConditionConfig Kind(WorldBeatComparator comparator, float count, string kind) =>
        new(WorldBeatFact.ActiveEntitiesOfKind, comparator, count, Kind: kind);

    private static WorldBeatConditionConfig War(WorldBeatComparator comparator, float count) =>
        new(WorldBeatFact.LivingRelationshipsOfKind, comparator, count, RelationshipKind: "at_war_with");
}
