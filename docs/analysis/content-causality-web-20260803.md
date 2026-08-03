# Content causality web (2026-08-03)

Generated from the generator configs plus pooled fires across the nine
30k worlds. One diagram per track. Node lines: name / 9-world fires /
what world-state it needs / what it makes. Gold ovals are relationship
currencies: their labels say who mints them and which other tracks
also consume them. Red = deletion-survey candidate. Green = the gambit
engines that mint a currency (successes).

## war

```mermaid
flowchart LR
  classDef cand fill:#8b1a1a,stroke:#5c0f0f,color:#fff
  classDef cur fill:#6b5b1f,stroke:#463c12,color:#fff
  classDef gam fill:#2d5b2d,stroke:#1d3d1d,color:#fff
  n_wars_get_names["wars_get_names<br/>fires 1242<br/>needs: AtWarWith<br/>makes: Occurrence+Penguin+Artifact"]
  cur_AtWarWith --> n_wars_get_names
  n_wars_get_names --> cur_MemberOf
  n_wars_get_names --> cur_Memorializes
  n_war_takes_its_toll["war_takes_its_toll<br/>fires 807<br/>needs: AtWarWith<br/>makes: Artifact kills 1"]
  cur_AtWarWith --> n_war_takes_its_toll
  n_war_takes_its_toll --> cur_ClaimedBy
  n_war_takes_its_toll --> cur_Memorializes
  n_ice_settles_accounts["ice_settles_accounts<br/>fires 650<br/>needs: AtWarWith<br/>makes: Artifact+Penguinx2+Canon kills 1"]
  cur_AtWarWith --> n_ice_settles_accounts
  n_ice_settles_accounts --> cur_ClaimedBy
  n_ice_settles_accounts --> cur_MemberOf
  n_ice_settles_accounts --> cur_Memorializes
  n_war_breaks_the_land["war_breaks_the_land<br/>fires 577<br/>needs: AtWarWith<br/>makes: Artifact+Faction"]
  cur_AtWarWith --> n_war_breaks_the_land
  n_war_breaks_the_land --> cur_ClaimedBy
  n_war_breaks_the_land --> cur_CorruptedBy
  n_war_breaks_the_land --> cur_RivalOf
  n_the_column_holds["the_column_holds<br/>fires 264<br/>needs: WagesCampaignAgainst<br/>makes: effect only"]
  cur_WagesCampaignAgainst --> n_the_column_holds
  n_allies_answer_the_call["allies_answer_the_call<br/>fires 215<br/>needs: AtWarWith<br/>makes: effect only"]
  cur_AtWarWith --> n_allies_answer_the_call
  n_allies_answer_the_call --> cur_AtWarWith
  n_orca_route_wars_get_names["orca_route_wars_get_names<br/>fires 83<br/>needs: AtWarWith+Memorializes<br/>makes: Occurrence+Penguin+Transportx2"]
  cur_AtWarWith --> n_orca_route_wars_get_names
  cur_Memorializes --> n_orca_route_wars_get_names
  n_orca_route_wars_get_names --> cur_AtWarWith
  n_orca_route_wars_get_names --> cur_Holds
  n_orca_route_wars_get_names --> cur_MemberOf
  n_orca_route_wars_get_names --> cur_Memorializes
  n_orca_route_wars_get_names --> cur_WardAgainst
  n_war_occurrences_launch_scouts["war_occurrences_launch_scouts<br/>fires 23<br/>needs: Holds<br/>makes: Transport"]
  cur_Holds --> n_war_occurrences_launch_scouts
  n_war_occurrences_launch_scouts --> cur_Holds
  n_war_occurrences_launch_scouts --> cur_Memorializes
  n_counter_offensive["counter_offensive<br/>fires 5<br/>needs: tag:terrified+bereaved<br/>makes: Occurrence"]:::cand
  n_counter_offensive --> cur_AtWarWith
  cur_AtWarWith(["AtWarWith<br/>gambit: territorial_war 1303 ok<br/>also consumed by: diplomacy, memory, trade, wilds"]):::cur
  cur_ClaimedBy(["ClaimedBy<br/>also consumed by: frontier, memory, wilds"]):::cur
  cur_CorruptedBy(["CorruptedBy<br/>gambit: haunts_transport 2851 ok<br/>also consumed by: memory, trade, wilds"]):::cur
  cur_Holds(["Holds<br/>gambits: wields/wrests 15068 ok<br/>also consumed by: devotion, knowledge, trade, wilds"]):::cur
  cur_MemberOf(["MemberOf<br/>also consumed by: lifecycle, memory"]):::cur
  cur_Memorializes(["Memorializes<br/>also consumed by: devotion, frontier, knowledge, memory, trade, wilds"]):::cur
  cur_RivalOf(["RivalOf<br/>gambit: declares_rivalry 2729 ok<br/>also consumed by: wilds"]):::cur
  cur_WagesCampaignAgainst(["WagesCampaignAgainst"]):::cur
  cur_WardAgainst(["WardAgainst<br/>also consumed by: wilds"]):::cur
```

## devotion

```mermaid
flowchart LR
  classDef cand fill:#8b1a1a,stroke:#5c0f0f,color:#fff
  classDef cur fill:#6b5b1f,stroke:#463c12,color:#fff
  classDef gam fill:#2d5b2d,stroke:#1d3d1d,color:#fff
  n_the_sky_renders_judgment["the_sky_renders_judgment<br/>fires 1186<br/>needs: OfferedTo+CleansedBy<br/>makes: Penguin+Artifact"]
  cur_OfferedTo --> n_the_sky_renders_judgment
  cur_CleansedBy --> n_the_sky_renders_judgment
  n_the_sky_renders_judgment --> cur_Holds
  n_the_sky_renders_judgment --> cur_HomeBase
  n_the_dead_keep_their_secrets["the_dead_keep_their_secrets<br/>fires 1139<br/>needs: OfferedTo+CleansedBy<br/>makes: Penguin+Artifact"]
  cur_OfferedTo --> n_the_dead_keep_their_secrets
  cur_CleansedBy --> n_the_dead_keep_their_secrets
  n_the_dead_keep_their_secrets --> cur_ClaimedBy
  n_the_dead_keep_their_secrets --> cur_Holds
  n_the_dead_keep_their_secrets --> cur_HomeBase
  n_relics_are_given_to_the_deep["relics_are_given_to_the_deep<br/>fires 759<br/>needs: Holds+ConsignedTo<br/>makes: effect only"]
  cur_Holds --> n_relics_are_given_to_the_deep
  cur_ConsignedTo --> n_relics_are_given_to_the_deep
  n_vigils_at_the_current["vigils_at_the_current<br/>fires 624<br/>needs: OfferedTo+CleansedBy<br/>makes: effect only"]
  cur_OfferedTo --> n_vigils_at_the_current
  cur_CleansedBy --> n_vigils_at_the_current
  n_vigils_at_the_current --> cur_OfferedTo
  n_the_current_provides["the_current_provides<br/>fires 277<br/>needs: OfferedTo+CleansedBy<br/>makes: Orcax2"]
  cur_OfferedTo --> n_the_current_provides
  cur_CleansedBy --> n_the_current_provides
  n_the_current_provides --> cur_HomeBase
  n_martyrs_are_witnessed["martyrs_are_witnessed<br/>fires 143<br/>needs: Memorializes tag:bereaved<br/>makes: Artifact"]
  cur_Memorializes --> n_martyrs_are_witnessed
  n_martyrs_are_witnessed --> cur_Memorializes
  n_pilgrims_found_new_currents["pilgrims_found_new_currents<br/>fires 135<br/>needs: OfferedTo+FoundedFrom<br/>makes: PointOfInterest+Canon"]
  cur_OfferedTo --> n_pilgrims_found_new_currents
  cur_FoundedFrom --> n_pilgrims_found_new_currents
  n_pilgrims_found_new_currents --> cur_ClaimedBy
  n_pilgrims_found_new_currents --> cur_FoundedFrom
  n_pilgrims_found_new_currents --> cur_OfferedTo
  n_a_hero_falls_to_the_deep["a_hero_falls_to_the_deep<br/>fires 79<br/>needs: PreysOn+CleansedBy<br/>makes: kills 1"]
  cur_PreysOn --> n_a_hero_falls_to_the_deep
  cur_CleansedBy --> n_a_hero_falls_to_the_deep
  n_the_offering_is_spent["the_offering_is_spent<br/>fires 61<br/>needs: OfferedTo<br/>makes: kills 1"]
  cur_OfferedTo --> n_the_offering_is_spent
  n_what_the_spring_remembers["what_the_spring_remembers<br/>fires 61<br/>needs: CleansedBy+Memorializes<br/>makes: Ability+Penguin+Settlement+Faction"]
  cur_CleansedBy --> n_what_the_spring_remembers
  cur_Memorializes --> n_what_the_spring_remembers
  n_what_the_spring_remembers --> cur_AlliedWith
  n_what_the_spring_remembers --> cur_ControlledBy
  n_what_the_spring_remembers --> cur_FoundedFrom
  n_what_the_spring_remembers --> cur_MemberOf
  n_what_the_spring_remembers --> cur_Memorializes
  n_what_the_spring_remembers --> cur_PracticedBy
  n_elders_descend_to_the_god["elders_descend_to_the_god<br/>fires 57<br/>needs: OfferedTo+CleansedBy<br/>makes: Artifact kills 1"]
  cur_OfferedTo --> n_elders_descend_to_the_god
  cur_CleansedBy --> n_elders_descend_to_the_god
  n_elders_descend_to_the_god --> cur_Memorializes
  n_vigils_leave_relics["vigils_leave_relics<br/>fires 44<br/>needs: HomeBase+Memorializes<br/>makes: Artifact+Penguin"]
  cur_HomeBase --> n_vigils_leave_relics
  cur_Memorializes --> n_vigils_leave_relics
  n_vigils_leave_relics --> cur_Holds
  n_vigils_leave_relics --> cur_MemberOf
  n_vigils_leave_relics --> cur_Memorializes
  n_kills_sung_to_the_deep["kills_sung_to_the_deep<br/>fires 34<br/>needs: PreysOn<br/>makes: Artifact"]
  cur_PreysOn --> n_kills_sung_to_the_deep
  n_kills_sung_to_the_deep --> cur_OfferedTo
  n_glory_to_the_dead_god["glory_to_the_dead_god<br/>fires 1<br/>needs: PreysOn tag:fervent<br/>makes: Occurrence"]:::cand
  cur_PreysOn --> n_glory_to_the_dead_god
  n_glory_to_the_dead_god --> cur_AtWarWith
  cur_AlliedWith(["AlliedWith<br/>also consumed by: trade"]):::cur
  cur_AtWarWith(["AtWarWith<br/>gambit: territorial_war 1303 ok<br/>also consumed by: diplomacy, memory, trade, war, wilds"]):::cur
  cur_ClaimedBy(["ClaimedBy<br/>also consumed by: frontier, memory, wilds"]):::cur
  cur_CleansedBy(["CleansedBy<br/>gambit: slays_monster 315 ok<br/>also consumed by: frontier, wilds"]):::cur
  cur_ConsignedTo(["ConsignedTo"]):::cur
  cur_ControlledBy(["ControlledBy<br/>also consumed by: frontier, wilds"]):::cur
  cur_FoundedFrom(["FoundedFrom<br/>also consumed by: frontier, knowledge, trade"]):::cur
  cur_Holds(["Holds<br/>gambits: wields/wrests 15068 ok<br/>also consumed by: knowledge, trade, war, wilds"]):::cur
  cur_HomeBase(["HomeBase<br/>also consumed by: frontier, knowledge, lifecycle, memory, trade"]):::cur
  cur_MemberOf(["MemberOf<br/>also consumed by: lifecycle, memory"]):::cur
  cur_Memorializes(["Memorializes<br/>also consumed by: frontier, knowledge, memory, trade, war, wilds"]):::cur
  cur_OfferedTo(["OfferedTo<br/>gambits: makes_offering 46552 ok<br/>also consumed by: frontier, wilds"]):::cur
  cur_PracticedBy(["PracticedBy<br/>also consumed by: knowledge, wilds"]):::cur
  cur_PreysOn(["PreysOn<br/>gambit-driven hunts<br/>also consumed by: wilds"]):::cur
```

## trade

```mermaid
flowchart LR
  classDef cand fill:#8b1a1a,stroke:#5c0f0f,color:#fff
  classDef cur fill:#6b5b1f,stroke:#463c12,color:#fff
  classDef gam fill:#2d5b2d,stroke:#1d3d1d,color:#fff
  n_the_deep_conclave["the_deep_conclave<br/>fires 713<br/>needs: Holds<br/>makes: Penguin+Artifact+Canon"]
  cur_Holds --> n_the_deep_conclave
  n_the_deep_conclave --> cur_ClaimedBy
  n_the_deep_conclave --> cur_Holds
  n_records_cast_shadows["records_cast_shadows<br/>fires 674<br/>needs: Holds+Memorializes<br/>makes: Artifact+Penguin"]
  cur_Holds --> n_records_cast_shadows
  cur_Memorializes --> n_records_cast_shadows
  n_records_cast_shadows --> cur_Holds
  n_records_cast_shadows --> cur_Memorializes
  n_debtors_flee_to_the_open_court["debtors_flee_to_the_open_court<br/>fires 478<br/>needs: CorruptedBy+Memorializes<br/>makes: Penguin+Artifact+Settlement"]
  cur_CorruptedBy --> n_debtors_flee_to_the_open_court
  cur_Memorializes --> n_debtors_flee_to_the_open_court
  n_debtors_flee_to_the_open_court --> cur_Holds
  n_debtors_flee_to_the_open_court --> cur_HomeBase
  n_debtors_flee_to_the_open_court --> cur_Memorializes
  n_debtors_flee_to_the_open_court --> cur_TradeRoute
  n_wars_feed_shadow_brokers["wars_feed_shadow_brokers<br/>fires 332<br/>needs: AtWarWith<br/>makes: Penguin+Artifact"]
  cur_AtWarWith --> n_wars_feed_shadow_brokers
  n_wars_feed_shadow_brokers --> cur_Holds
  n_wars_feed_shadow_brokers --> cur_Memorializes
  n_rich_hubs_grow_undermarkets["rich_hubs_grow_undermarkets<br/>fires 284<br/>needs: TradeRoute+FoundedFrom<br/>makes: Settlement+Penguin+Artifact"]
  cur_TradeRoute --> n_rich_hubs_grow_undermarkets
  cur_FoundedFrom --> n_rich_hubs_grow_undermarkets
  n_rich_hubs_grow_undermarkets --> cur_FoundedFrom
  n_rich_hubs_grow_undermarkets --> cur_Holds
  n_rich_hubs_grow_undermarkets --> cur_HomeBase
  n_rich_hubs_grow_undermarkets --> cur_TradeRoute
  n_routes_draw_readers["routes_draw_readers<br/>fires 164<br/>needs: TradeRoute<br/>makes: Penguin+Faction+Artifact"]
  cur_TradeRoute --> n_routes_draw_readers
  n_routes_draw_readers --> cur_ControlledBy
  n_routes_draw_readers --> cur_FoundedFrom
  n_routes_draw_readers --> cur_HomeBase
  n_routes_draw_readers --> cur_MemberOf
  n_routes_draw_readers --> cur_NativeTo
  n_routes_draw_readers --> cur_TradeRoute
  n_charters_find_harbors["charters_find_harbors<br/>fires 124<br/>needs: Holds+NativeTo<br/>makes: Settlement+Penguin+Artifact"]
  cur_Holds --> n_charters_find_harbors
  cur_NativeTo --> n_charters_find_harbors
  n_charters_find_harbors --> cur_ControlledBy
  n_charters_find_harbors --> cur_HomeBase
  n_charters_find_harbors --> cur_NativeTo
  n_trade_routes_convene_negotiation["trade_routes_convene_negotiation<br/>fires 114<br/>needs: TradeRoute+Memorializes<br/>makes: Occurrence+Penguin+Artifact+Transport"]
  cur_TradeRoute --> n_trade_routes_convene_negotiation
  cur_Memorializes --> n_trade_routes_convene_negotiation
  n_trade_routes_convene_negotiation --> cur_Holds
  n_trade_routes_convene_negotiation --> cur_HomeBase
  n_trade_routes_convene_negotiation --> cur_MemberOf
  n_trade_routes_convene_negotiation --> cur_Memorializes
  n_trade_routes_convene_negotiation --> cur_NativeTo
  n_conferences_become_festivals["conferences_become_festivals<br/>fires 25<br/>needs: Memorializes+AlliedWith<br/>makes: Occurrence"]
  cur_Memorializes --> n_conferences_become_festivals
  cur_AlliedWith --> n_conferences_become_festivals
  n_conferences_become_festivals --> cur_HomeBase
  n_trade_conferences_raise_citadels["trade_conferences_raise_citadels<br/>fires 23<br/>needs: HomeBase+Holds<br/>makes: Transport"]
  cur_HomeBase --> n_trade_conferences_raise_citadels
  cur_Holds --> n_trade_conferences_raise_citadels
  n_trade_conferences_raise_citadels --> cur_Holds
  n_trade_conferences_raise_citadels --> cur_Memorializes
  cur_AlliedWith(["AlliedWith"]):::cur
  cur_AtWarWith(["AtWarWith<br/>gambit: territorial_war 1303 ok<br/>also consumed by: diplomacy, memory, war, wilds"]):::cur
  cur_ClaimedBy(["ClaimedBy<br/>also consumed by: frontier, memory, wilds"]):::cur
  cur_ControlledBy(["ControlledBy<br/>also consumed by: frontier, wilds"]):::cur
  cur_CorruptedBy(["CorruptedBy<br/>gambit: haunts_transport 2851 ok<br/>also consumed by: memory, wilds"]):::cur
  cur_FoundedFrom(["FoundedFrom<br/>also consumed by: devotion, frontier, knowledge"]):::cur
  cur_Holds(["Holds<br/>gambits: wields/wrests 15068 ok<br/>also consumed by: devotion, knowledge, war, wilds"]):::cur
  cur_HomeBase(["HomeBase<br/>also consumed by: devotion, frontier, knowledge, lifecycle, memory"]):::cur
  cur_MemberOf(["MemberOf<br/>also consumed by: lifecycle, memory"]):::cur
  cur_Memorializes(["Memorializes<br/>also consumed by: devotion, frontier, knowledge, memory, war, wilds"]):::cur
  cur_NativeTo(["NativeTo<br/>also consumed by: knowledge"]):::cur
  cur_TradeRoute(["TradeRoute<br/>gambit: opens_trade_route 1466 ok<br/>also consumed by: diplomacy"]):::cur
```

## diplomacy

```mermaid
flowchart LR
  classDef cand fill:#8b1a1a,stroke:#5c0f0f,color:#fff
  classDef cur fill:#6b5b1f,stroke:#463c12,color:#fff
  classDef gam fill:#2d5b2d,stroke:#1d3d1d,color:#fff
  n_the_accord_is_sealed["the_accord_is_sealed<br/>fires 379<br/>needs: AtWarWith<br/>makes: Canon+Penguin"]
  cur_AtWarWith --> n_the_accord_is_sealed
  n_the_accord_is_sealed --> cur_Holds
  n_the_accord_is_sealed --> cur_Memorializes
  n_the_accord_convenes["the_accord_convenes<br/>fires 47<br/>needs: tag:terrified<br/>makes: Canon"]
  n_the_accord_convenes --> cur_Memorializes
  n_the_accord_holds["the_accord_holds<br/>fires 36<br/>needs: tag:oathbreaker<br/>makes: Artifact"]
  n_the_accord_holds --> cur_Memorializes
  n_envoys_cross_the_ice["envoys_cross_the_ice<br/>fires 2<br/>needs: TradeRoute<br/>makes: Penguin+PointOfInterest"]:::cand
  cur_TradeRoute --> n_envoys_cross_the_ice
  n_envoys_cross_the_ice --> cur_HomeBase
  n_envoys_cross_the_ice --> cur_Memorializes
  cur_AtWarWith(["AtWarWith<br/>gambit: territorial_war 1303 ok<br/>also consumed by: memory, trade, war, wilds"]):::cur
  cur_Holds(["Holds<br/>gambits: wields/wrests 15068 ok<br/>also consumed by: devotion, knowledge, trade, war, wilds"]):::cur
  cur_HomeBase(["HomeBase<br/>also consumed by: devotion, frontier, knowledge, lifecycle, memory, trade"]):::cur
  cur_Memorializes(["Memorializes<br/>also consumed by: devotion, frontier, knowledge, memory, trade, war, wilds"]):::cur
  cur_TradeRoute(["TradeRoute<br/>gambit: opens_trade_route 1466 ok<br/>also consumed by: trade"]):::cur
```

## wilds

```mermaid
flowchart LR
  classDef cand fill:#8b1a1a,stroke:#5c0f0f,color:#fff
  classDef cur fill:#6b5b1f,stroke:#463c12,color:#fff
  classDef gam fill:#2d5b2d,stroke:#1d3d1d,color:#fff
  n_stolen_artifacts_draw_hunters["stolen_artifacts_draw_hunters<br/>fires 611<br/>needs: Holds+AtWarWith<br/>makes: Penguin"]
  cur_Holds --> n_stolen_artifacts_draw_hunters
  cur_AtWarWith --> n_stolen_artifacts_draw_hunters
  n_stolen_artifacts_draw_hunters --> cur_AtWarWith
  n_monsters_stalk_the_marches["monsters_stalk_the_marches<br/>fires 585<br/>needs: PreysOn<br/>makes: effect only"]
  cur_PreysOn --> n_monsters_stalk_the_marches
  n_monsters_stalk_the_marches --> cur_PreysOn
  n_wardens_take_up_the_old_forms["wardens_take_up_the_old_forms<br/>fires 471<br/>needs: PracticedBy<br/>makes: effect only"]
  cur_PracticedBy --> n_wardens_take_up_the_old_forms
  n_wardens_take_up_the_old_forms --> cur_PracticedBy
  n_the_dread_song_breaks_the_watch["the_dread_song_breaks_the_watch<br/>fires 441<br/>needs: any Penguin<br/>makes: kills 1"]
  n_the_dread_song_breaks_the_watch --> cur_OfferedTo
  n_overclaimed_blooms_draw_beasts["overclaimed_blooms_draw_beasts<br/>fires 344<br/>needs: ClaimedBy+CorruptedBy<br/>makes: Monster"]
  cur_ClaimedBy --> n_overclaimed_blooms_draw_beasts
  cur_CorruptedBy --> n_overclaimed_blooms_draw_beasts
  n_overclaimed_blooms_draw_beasts --> cur_CorruptedBy
  n_overclaimed_blooms_draw_beasts --> cur_HomeBase
  n_haunted_transports_spread_plague["haunted_transports_spread_plague<br/>fires 307<br/>needs: CorruptedBy+Memorializes<br/>makes: Monster+Occurrence"]
  cur_CorruptedBy --> n_haunted_transports_spread_plague
  cur_Memorializes --> n_haunted_transports_spread_plague
  n_haunted_transports_spread_plague --> cur_CorruptedBy
  n_haunted_transports_spread_plague --> cur_Memorializes
  n_the_fear_fades["the_fear_fades<br/>fires 267<br/>needs: PreysOn tag:hunted<br/>makes: effect only"]
  cur_PreysOn --> n_the_fear_fades
  n_corruption_wakes_monsters["corruption_wakes_monsters<br/>fires 216<br/>needs: CorruptedBy+CleansedBy<br/>makes: Monster+Transport+Penguin"]
  cur_CorruptedBy --> n_corruption_wakes_monsters
  cur_CleansedBy --> n_corruption_wakes_monsters
  n_corruption_wakes_monsters --> cur_CorruptedBy
  n_corruption_wakes_monsters --> cur_Holds
  n_corruption_wakes_monsters --> cur_HomeBase
  n_corruption_wakes_monsters --> cur_Memorializes
  n_corruption_wakes_monsters --> cur_NativeTo
  n_corrupted_monsters_raise_revenants["corrupted_monsters_raise_revenants<br/>fires 176<br/>needs: CorruptedBy+RivalOf<br/>makes: Monster"]
  cur_CorruptedBy --> n_corrupted_monsters_raise_revenants
  cur_RivalOf --> n_corrupted_monsters_raise_revenants
  n_corrupted_monsters_raise_revenants --> cur_CorruptedBy
  n_the_hunt_settles_in["the_hunt_settles_in<br/>fires 160<br/>needs: PreysOn tag:hunted+terrified<br/>makes: effect only"]
  cur_PreysOn --> n_the_hunt_settles_in
  n_monster_slayings_become_festivals["monster_slayings_become_festivals<br/>fires 135<br/>needs: CleansedBy+Memorializes<br/>makes: Occurrence+Ability+Transport"]
  cur_CleansedBy --> n_monster_slayings_become_festivals
  cur_Memorializes --> n_monster_slayings_become_festivals
  n_monster_slayings_become_festivals --> cur_ConduitOf
  n_monster_slayings_become_festivals --> cur_Holds
  n_monster_slayings_become_festivals --> cur_MemberOf
  n_monster_slayings_become_festivals --> cur_Memorializes
  n_pods_mark_their_prey["pods_mark_their_prey<br/>fires 120<br/>needs: OfferedTo<br/>makes: effect only"]
  cur_OfferedTo --> n_pods_mark_their_prey
  n_pods_mark_their_prey --> cur_PreysOn
  n_wardens_rise_where_the_hunt_bites["wardens_rise_where_the_hunt_bites<br/>fires 106<br/>needs: tag:terrified<br/>makes: Penguin"]
  n_wardens_rise_where_the_hunt_bites --> cur_HomeBase
  n_pods_hunt_the_convoys["pods_hunt_the_convoys<br/>fires 66<br/>needs: OfferedTo<br/>makes: effect only"]
  cur_OfferedTo --> n_pods_hunt_the_convoys
  n_pods_hunt_the_convoys --> cur_PreysOn
  n_the_white_has_teeth["the_white_has_teeth<br/>fires 45<br/>needs: PreysOn<br/>makes: effect only"]
  cur_PreysOn --> n_the_white_has_teeth
  n_the_white_has_teeth --> cur_PreysOn
  n_warded_monsters_found_orders["warded_monsters_found_orders<br/>fires 36<br/>needs: WardAgainst+Memorializes<br/>makes: Faction+Transport"]
  cur_WardAgainst --> n_warded_monsters_found_orders
  cur_Memorializes --> n_warded_monsters_found_orders
  n_warded_monsters_found_orders --> cur_Holds
  n_warded_monsters_found_orders --> cur_Memorializes
  n_the_watch_answers_the_song["the_watch_answers_the_song<br/>fires 19<br/>needs: Memorializes tag:terrified<br/>makes: Penguin+Transport"]
  cur_Memorializes --> n_the_watch_answers_the_song
  n_the_watch_answers_the_song --> cur_Holds
  n_the_watch_answers_the_song --> cur_HomeBase
  n_the_watch_answers_the_song --> cur_Memorializes
  n_controlled_depths_wake_leviathans["controlled_depths_wake_leviathans<br/>fires 5<br/>needs: ControlledBy+CorruptedBy<br/>makes: Monster+Occurrence"]:::cand
  cur_ControlledBy --> n_controlled_depths_wake_leviathans
  cur_CorruptedBy --> n_controlled_depths_wake_leviathans
  n_controlled_depths_wake_leviathans --> cur_CorruptedBy
  n_controlled_depths_wake_leviathans --> cur_HomeBase
  n_the_silence_answers["the_silence_answers<br/>fires 2<br/>needs: tag:profaner<br/>makes: effect only"]:::cand
  cur_AtWarWith(["AtWarWith<br/>gambit: territorial_war 1303 ok<br/>also consumed by: diplomacy, memory, trade, war"]):::cur
  cur_ClaimedBy(["ClaimedBy<br/>also consumed by: frontier, memory"]):::cur
  cur_CleansedBy(["CleansedBy<br/>gambit: slays_monster 315 ok<br/>also consumed by: devotion, frontier"]):::cur
  cur_ConduitOf(["ConduitOf<br/>also consumed by: frontier, knowledge, memory"]):::cur
  cur_ControlledBy(["ControlledBy<br/>also consumed by: frontier"]):::cur
  cur_CorruptedBy(["CorruptedBy<br/>gambit: haunts_transport 2851 ok<br/>also consumed by: memory, trade"]):::cur
  cur_Holds(["Holds<br/>gambits: wields/wrests 15068 ok<br/>also consumed by: devotion, knowledge, trade, war"]):::cur
  cur_HomeBase(["HomeBase<br/>also consumed by: devotion, frontier, knowledge, lifecycle, memory, trade"]):::cur
  cur_MemberOf(["MemberOf<br/>also consumed by: lifecycle, memory"]):::cur
  cur_Memorializes(["Memorializes<br/>also consumed by: devotion, frontier, knowledge, memory, trade, war"]):::cur
  cur_NativeTo(["NativeTo<br/>also consumed by: knowledge, trade"]):::cur
  cur_OfferedTo(["OfferedTo<br/>gambits: makes_offering 46552 ok<br/>also consumed by: devotion, frontier"]):::cur
  cur_PracticedBy(["PracticedBy<br/>also consumed by: knowledge"]):::cur
  cur_PreysOn(["PreysOn<br/>gambit-driven hunts<br/>also consumed by: devotion"]):::cur
  cur_RivalOf(["RivalOf<br/>gambit: declares_rivalry 2729 ok"]):::cur
  cur_WardAgainst(["WardAgainst"]):::cur
```

## frontier

```mermaid
flowchart LR
  classDef cand fill:#8b1a1a,stroke:#5c0f0f,color:#fff
  classDef cur fill:#6b5b1f,stroke:#463c12,color:#fff
  classDef gam fill:#2d5b2d,stroke:#1d3d1d,color:#fff
  n_cleansed_springs_found_sanctuaries["cleansed_springs_found_sanctuaries<br/>fires 251<br/>needs: CleansedBy+FoundedFrom<br/>makes: Settlement+Faction+Ability"]
  cur_CleansedBy --> n_cleansed_springs_found_sanctuaries
  cur_FoundedFrom --> n_cleansed_springs_found_sanctuaries
  n_cleansed_springs_found_sanctuaries --> cur_AlliedWith
  n_cleansed_springs_found_sanctuaries --> cur_ControlledBy
  n_cleansed_springs_found_sanctuaries --> cur_FoundedFrom
  n_cleansed_springs_found_sanctuaries --> cur_HomeBase
  n_cleansed_springs_found_sanctuaries --> cur_Memorializes
  n_cleansed_springs_found_sanctuaries --> cur_PracticedBy
  n_unclaimed_places_draw_wardens["unclaimed_places_draw_wardens<br/>fires 181<br/>needs: ClaimedBy<br/>makes: Penguin+Transport"]
  cur_ClaimedBy --> n_unclaimed_places_draw_wardens
  n_unclaimed_places_draw_wardens --> cur_ClaimedBy
  n_unclaimed_places_draw_wardens --> cur_ConduitOf
  n_unclaimed_places_draw_wardens --> cur_Holds
  n_unclaimed_places_draw_wardens --> cur_HomeBase
  n_unclaimed_places_draw_wardens --> cur_MemberOf
  n_unclaimed_places_draw_wardens --> cur_NativeTo
  n_conduit_points_found_reefholds["conduit_points_found_reefholds<br/>fires 124<br/>needs: ConduitOf+FoundedFrom<br/>makes: Settlement+Artifact+Penguin"]
  cur_ConduitOf --> n_conduit_points_found_reefholds
  cur_FoundedFrom --> n_conduit_points_found_reefholds
  n_conduit_points_found_reefholds --> cur_ConduitOf
  n_conduit_points_found_reefholds --> cur_FoundedFrom
  n_conduit_points_found_reefholds --> cur_Holds
  n_conduit_points_found_reefholds --> cur_HomeBase
  n_conduit_points_found_reefholds --> cur_TradeRoute
  n_bulls_range_the_far_currents["bulls_range_the_far_currents<br/>fires 47<br/>needs: OfferedTo<br/>makes: effect only"]
  cur_OfferedTo --> n_bulls_range_the_far_currents
  n_bulls_range_the_far_currents --> cur_HomeBase
  n_bulls_range_the_far_currents --> cur_MemberOf
  n_sanctuaries_anchor_colonies["sanctuaries_anchor_colonies<br/>fires 40<br/>needs: ControlledBy+FoundedFrom<br/>makes: Settlement+Faction+Transport"]
  cur_ControlledBy --> n_sanctuaries_anchor_colonies
  cur_FoundedFrom --> n_sanctuaries_anchor_colonies
  n_sanctuaries_anchor_colonies --> cur_AlliedWith
  n_sanctuaries_anchor_colonies --> cur_ConduitOf
  n_sanctuaries_anchor_colonies --> cur_ControlledBy
  n_sanctuaries_anchor_colonies --> cur_FoundedFrom
  n_sanctuaries_anchor_colonies --> cur_Holds
  n_sanctuaries_anchor_colonies --> cur_HomeBase
  n_sanctuaries_anchor_colonies --> cur_NativeTo
  n_sanctuaries_anchor_colonies --> cur_TradeRoute
  n_the_thaw_calls_the_bold["the_thaw_calls_the_bold<br/>fires 33<br/>needs: FoundedFrom+Memorializes<br/>makes: Penguin+Artifact+Ability+Canon"]
  cur_FoundedFrom --> n_the_thaw_calls_the_bold
  cur_Memorializes --> n_the_thaw_calls_the_bold
  n_the_thaw_calls_the_bold --> cur_HomeBase
  n_the_thaw_calls_the_bold --> cur_MemberOf
  n_the_thaw_calls_the_bold --> cur_Memorializes
  n_the_thaw_calls_the_bold --> cur_PracticedBy
  n_claimed_waters_draw_bulls["claimed_waters_draw_bulls<br/>fires 11<br/>needs: ClaimedBy+HomeBase<br/>makes: Orca"]:::cand
  cur_ClaimedBy --> n_claimed_waters_draw_bulls
  cur_HomeBase --> n_claimed_waters_draw_bulls
  n_claimed_waters_draw_bulls --> cur_AlliedWith
  n_claimed_waters_draw_bulls --> cur_ClaimedBy
  n_claimed_waters_draw_bulls --> cur_HomeBase
  cur_AlliedWith(["AlliedWith<br/>also consumed by: trade"]):::cur
  cur_ClaimedBy(["ClaimedBy<br/>also consumed by: memory, wilds"]):::cur
  cur_CleansedBy(["CleansedBy<br/>gambit: slays_monster 315 ok<br/>also consumed by: devotion, wilds"]):::cur
  cur_ConduitOf(["ConduitOf<br/>also consumed by: knowledge, memory"]):::cur
  cur_ControlledBy(["ControlledBy<br/>also consumed by: wilds"]):::cur
  cur_FoundedFrom(["FoundedFrom<br/>also consumed by: devotion, knowledge, trade"]):::cur
  cur_Holds(["Holds<br/>gambits: wields/wrests 15068 ok<br/>also consumed by: devotion, knowledge, trade, war, wilds"]):::cur
  cur_HomeBase(["HomeBase<br/>also consumed by: devotion, knowledge, lifecycle, memory, trade"]):::cur
  cur_MemberOf(["MemberOf<br/>also consumed by: lifecycle, memory"]):::cur
  cur_Memorializes(["Memorializes<br/>also consumed by: devotion, knowledge, memory, trade, war, wilds"]):::cur
  cur_NativeTo(["NativeTo<br/>also consumed by: knowledge, trade"]):::cur
  cur_OfferedTo(["OfferedTo<br/>gambits: makes_offering 46552 ok<br/>also consumed by: devotion, wilds"]):::cur
  cur_PracticedBy(["PracticedBy<br/>also consumed by: knowledge, wilds"]):::cur
  cur_TradeRoute(["TradeRoute<br/>gambit: opens_trade_route 1466 ok<br/>also consumed by: diplomacy, trade"]):::cur
```

## knowledge

```mermaid
flowchart LR
  classDef cand fill:#8b1a1a,stroke:#5c0f0f,color:#fff
  classDef cur fill:#6b5b1f,stroke:#463c12,color:#fff
  classDef gam fill:#2d5b2d,stroke:#1d3d1d,color:#fff
  n_the_tome_is_read["the_tome_is_read<br/>fires 705<br/>needs: Holds<br/>makes: Ability kills 1"]
  cur_Holds --> n_the_tome_is_read
  n_the_tome_is_read --> cur_PracticedBy
  n_practiced_forms_find_tomes["practiced_forms_find_tomes<br/>fires 570<br/>needs: PracticedBy+ConduitOf<br/>makes: Penguin+Artifact"]
  cur_PracticedBy --> n_practiced_forms_find_tomes
  cur_ConduitOf --> n_practiced_forms_find_tomes
  n_practiced_forms_find_tomes --> cur_ConduitOf
  n_practiced_forms_find_tomes --> cur_Holds
  n_practiced_forms_find_tomes --> cur_PracticedBy
  n_what_the_artifact_knows["what_the_artifact_knows<br/>fires 171<br/>needs: Holds+HomeBase<br/>makes: Settlement+Artifact+Faction"]
  cur_Holds --> n_what_the_artifact_knows
  cur_HomeBase --> n_what_the_artifact_knows
  n_what_the_artifact_knows --> cur_ControlledBy
  n_what_the_artifact_knows --> cur_HomeBase
  n_the_still_water_is_sounded["the_still_water_is_sounded<br/>fires 99<br/>needs: any Transport<br/>makes: PointOfInterest"]
  n_the_still_water_is_sounded --> cur_ConduitOf
  n_conduits_map_hidden_peaks["conduits_map_hidden_peaks<br/>fires 71<br/>needs: NativeTo+Memorializes<br/>makes: PointOfInterest"]
  cur_NativeTo --> n_conduits_map_hidden_peaks
  cur_Memorializes --> n_conduits_map_hidden_peaks
  n_conduits_map_hidden_peaks --> cur_ConduitOf
  n_conduits_map_hidden_peaks --> cur_Memorializes
  n_deep_conduits_name_depths["deep_conduits_name_depths<br/>fires 36<br/>needs: ConduitOf+FoundedFrom<br/>makes: Artifact"]
  cur_ConduitOf --> n_deep_conduits_name_depths
  cur_FoundedFrom --> n_deep_conduits_name_depths
  n_deep_conduits_name_depths --> cur_FoundedFrom
  n_deep_conduits_name_depths --> cur_Memorializes
  n_mapped_peaks_birth_techniques["mapped_peaks_birth_techniques<br/>fires 16<br/>needs: ConduitOf+Memorializes<br/>makes: Ability+Artifact"]
  cur_ConduitOf --> n_mapped_peaks_birth_techniques
  cur_Memorializes --> n_mapped_peaks_birth_techniques
  n_mapped_peaks_birth_techniques --> cur_ConduitOf
  n_mapped_peaks_birth_techniques --> cur_Memorializes
  cur_ConduitOf(["ConduitOf<br/>also consumed by: frontier, memory"]):::cur
  cur_ControlledBy(["ControlledBy<br/>also consumed by: frontier, wilds"]):::cur
  cur_FoundedFrom(["FoundedFrom<br/>also consumed by: devotion, frontier, trade"]):::cur
  cur_Holds(["Holds<br/>gambits: wields/wrests 15068 ok<br/>also consumed by: devotion, trade, war, wilds"]):::cur
  cur_HomeBase(["HomeBase<br/>also consumed by: devotion, frontier, lifecycle, memory, trade"]):::cur
  cur_Memorializes(["Memorializes<br/>also consumed by: devotion, frontier, memory, trade, war, wilds"]):::cur
  cur_NativeTo(["NativeTo<br/>also consumed by: trade"]):::cur
  cur_PracticedBy(["PracticedBy<br/>also consumed by: wilds"]):::cur
```

## memory

```mermaid
flowchart LR
  classDef cand fill:#8b1a1a,stroke:#5c0f0f,color:#fff
  classDef cur fill:#6b5b1f,stroke:#463c12,color:#fff
  classDef gam fill:#2d5b2d,stroke:#1d3d1d,color:#fff
  n_one_word_yields["one_word_yields<br/>fires 485<br/>needs: any Canon<br/>makes: kills 1"]
  n_the_canon_is_carried["the_canon_is_carried<br/>fires 348<br/>needs: ClaimedBy+ConduitOf<br/>makes: Penguin"]
  cur_ClaimedBy --> n_the_canon_is_carried
  cur_ConduitOf --> n_the_canon_is_carried
  n_the_canon_is_carried --> cur_ConduitOf
  n_the_canon_is_carried --> cur_Holds
  n_the_seal_shatters["the_seal_shatters<br/>fires 305<br/>needs: AtWarWith<br/>makes: kills 1"]
  cur_AtWarWith --> n_the_seal_shatters
  n_the_stone_remembers_the_walk["the_stone_remembers_the_walk<br/>fires 279<br/>needs: Memorializes<br/>makes: Artifact"]
  cur_Memorializes --> n_the_stone_remembers_the_walk
  n_the_stone_remembers_the_walk --> cur_Memorializes
  n_the_polynya_never_closes["the_polynya_never_closes<br/>fires 151<br/>needs: HomeBase+ConduitOf<br/>makes: PointOfInterest"]
  cur_HomeBase --> n_the_polynya_never_closes
  cur_ConduitOf --> n_the_polynya_never_closes
  n_the_polynya_never_closes --> cur_ClaimedBy
  n_the_polynya_never_closes --> cur_ConduitOf
  n_the_river_earns_its_name["the_river_earns_its_name<br/>fires 113<br/>needs: any Settlement<br/>makes: Region"]
  n_the_river_earns_its_name --> cur_ClaimedBy
  n_the_ice_takes_notice["the_ice_takes_notice<br/>fires 31<br/>needs: Memorializes chronicle-heat<br/>makes: Artifact"]
  cur_Memorializes --> n_the_ice_takes_notice
  n_the_ice_takes_notice --> cur_Memorializes
  n_legend_enters_the_canon["legend_enters_the_canon<br/>fires 14<br/>needs: ClaimedBy chronicle-heat<br/>makes: Canon"]
  cur_ClaimedBy --> n_legend_enters_the_canon
  n_legend_enters_the_canon --> cur_ClaimedBy
  n_legend_enters_the_canon --> cur_Memorializes
  n_the_crater_keeps_its_count["the_crater_keeps_its_count<br/>fires 10<br/>needs: any Penguin/Orca<br/>makes: PointOfInterest+Artifact"]
  n_the_crater_keeps_its_count --> cur_ClaimedBy
  n_the_crater_keeps_its_count --> cur_Holds
  n_memorials_join_the_wall["memorials_join_the_wall<br/>fires 8<br/>needs: MemberOf<br/>makes: kills 2"]:::cand
  cur_MemberOf --> n_memorials_join_the_wall
  n_the_war_leaves_its_mark["the_war_leaves_its_mark<br/>fires 0<br/>needs: AtWarWith+CorruptedBy<br/>makes: effect only"]:::cand
  cur_AtWarWith --> n_the_war_leaves_its_mark
  cur_CorruptedBy --> n_the_war_leaves_its_mark
  n_the_war_leaves_its_mark --> cur_CorruptedBy
  cur_AtWarWith(["AtWarWith<br/>gambit: territorial_war 1303 ok<br/>also consumed by: diplomacy, trade, war, wilds"]):::cur
  cur_ClaimedBy(["ClaimedBy<br/>also consumed by: frontier, wilds"]):::cur
  cur_ConduitOf(["ConduitOf<br/>also consumed by: frontier, knowledge"]):::cur
  cur_CorruptedBy(["CorruptedBy<br/>gambit: haunts_transport 2851 ok<br/>also consumed by: trade, wilds"]):::cur
  cur_Holds(["Holds<br/>gambits: wields/wrests 15068 ok<br/>also consumed by: devotion, knowledge, trade, war, wilds"]):::cur
  cur_HomeBase(["HomeBase<br/>also consumed by: devotion, frontier, knowledge, lifecycle, trade"]):::cur
  cur_MemberOf(["MemberOf<br/>also consumed by: lifecycle"]):::cur
  cur_Memorializes(["Memorializes<br/>also consumed by: devotion, frontier, knowledge, trade, war, wilds"]):::cur
```

## lifecycle

```mermaid
flowchart LR
  classDef cand fill:#8b1a1a,stroke:#5c0f0f,color:#fff
  classDef cur fill:#6b5b1f,stroke:#463c12,color:#fff
  classDef gam fill:#2d5b2d,stroke:#1d3d1d,color:#fff
  n_the_line_continues["the_line_continues<br/>fires 1014<br/>needs: HomeBase+MemberOf<br/>makes: Orcax2+PointOfInterestx2+Penguinx2+Transport"]
  cur_HomeBase --> n_the_line_continues
  cur_MemberOf --> n_the_line_continues
  n_the_line_continues --> cur_AlliedWith
  n_the_line_continues --> cur_ClaimedBy
  n_the_line_continues --> cur_ConduitOf
  n_the_line_continues --> cur_Holds
  n_the_line_continues --> cur_HomeBase
  n_the_line_continues --> cur_MemberOf
  n_the_line_continues --> cur_NativeTo
  cur_AlliedWith(["AlliedWith<br/>also consumed by: trade"]):::cur
  cur_ClaimedBy(["ClaimedBy<br/>also consumed by: frontier, memory, wilds"]):::cur
  cur_ConduitOf(["ConduitOf<br/>also consumed by: frontier, knowledge, memory"]):::cur
  cur_Holds(["Holds<br/>gambits: wields/wrests 15068 ok<br/>also consumed by: devotion, knowledge, trade, war, wilds"]):::cur
  cur_HomeBase(["HomeBase<br/>also consumed by: devotion, frontier, knowledge, memory, trade"]):::cur
  cur_MemberOf(["MemberOf<br/>also consumed by: memory"]):::cur
  cur_NativeTo(["NativeTo<br/>also consumed by: knowledge, trade"]):::cur
```

## desperation

```mermaid
flowchart LR
  classDef cand fill:#8b1a1a,stroke:#5c0f0f,color:#fff
  classDef cur fill:#6b5b1f,stroke:#463c12,color:#fff
  classDef gam fill:#2d5b2d,stroke:#1d3d1d,color:#fff
  n_the_deep_sings_the_drowned_song["the_deep_sings_the_drowned_song<br/>fires 142<br/>needs:  +desperation-gate<br/>makes: PointOfInterest+Orcax2 kills 2"]
  n_the_deep_sings_the_drowned_song --> cur_HomeBase
  n_the_deep_sings_the_drowned_song --> cur_MemberOf
  n_every_debt_comes_due["every_debt_comes_due<br/>fires 71<br/>needs:  +desperation-gate<br/>makes: Settlement+Penguinx2+Artifact kills 2"]
  n_every_debt_comes_due --> cur_Holds
  n_every_debt_comes_due --> cur_HomeBase
  n_the_beacon_hosts_the_dispossessed["the_beacon_hosts_the_dispossessed<br/>fires 30<br/>needs:  +desperation-gate<br/>makes: Settlement+Penguinx2+Transport kills 2"]
  n_the_beacon_hosts_the_dispossessed --> cur_Holds
  n_the_beacon_hosts_the_dispossessed --> cur_HomeBase
  cur_Holds(["Holds<br/>gambits: wields/wrests 15068 ok<br/>also consumed by: devotion, knowledge, trade, war, wilds"]):::cur
  cur_HomeBase(["HomeBase<br/>also consumed by: devotion, frontier, knowledge, lifecycle, memory, trade"]):::cur
  cur_MemberOf(["MemberOf<br/>also consumed by: lifecycle, memory"]):::cur
```
