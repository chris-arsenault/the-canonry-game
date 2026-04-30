# Planner Tuning Metrics Focus

This document is the working scope for the current planner/process tuning pass.
Generator fire counts are not a success metric for this pass. They may be
reported as background context only when a change obviously breaks the world,
but they are not the thing being optimized.

The three metrics that matter are:

1. map utilization
2. individual entity physical/semantic roaming variance
3. verb utilization

All metrics below should be computed from Monte Carlo telemetry parquet output.
Do not infer behavior from visual impressions alone.

## 1. Map Utilization

Question: how much of each plane is left unused by simulation activity?

Primary metric: largest strict empty radius by plane. For physical hexes this
means the largest exact hex distance from any map cell to the nearest activity
cell. A high value means the map has a large dead zone even when entity count
and movement volume look healthy.

Physical plane telemetry source:

- `entity_snapshots`: `run_id`, `tick`, `entity_id`, `kind_id`, `status_code`, `physical_q`, `physical_r`
- `coverage_snapshots`: `run_id`, `tick`, `owner_entity_id`, `coverage_kind_code`, `cell_count`, `min_q`, `max_q`, `min_r`, `max_r`
- `map_cell_events` or `map_snapshots`: map cell population and terrain context when component reconstruction needs the full grid
- `symbols`: resolve `kind_id`, terrain ids, and coverage kind codes to stable names

Semantic plane telemetry source:

- `entity_snapshots`: `run_id`, `tick`, `entity_id`, `kind_id`, `status_code`, `semantic_q`, `semantic_r`, `semantic_s`
- `symbols`: resolve `kind_id` to stable names

Activity definition:

- Activity cells/points include active entity positions, movement endpoints,
  selected placements, and map/coverage mutation cells when present in telemetry.
- Activity should be cumulative through the snapshot tick being analyzed, not
  only final-position occupancy.
- Do not use entity count, connected empty components, or maximum distance
  between entities as primary utilization metrics; those are structurally weak
  for this simulation.

Physical utilization metrics:

- `physical_activity_cells`: count distinct physical cells touched by cumulative activity.
- `physical_activity_cell_share`: `physical_activity_cells / total_physical_cells`.
- `physical_activity_entropy`: normalized entropy of activity event counts by cell.
- `physical_activity_top_cell_share`: share of all activity events that occurred on the single busiest cell.
- `physical_activity_axis_anisotropy`: largest axial span divided by smallest axial span across q/r/s axes; this catches single-corridor use better than q/r bounding-box ratio.
- `physical_largest_unoccupied_strict_hex_radius`: max exact hex distance from every map cell to the nearest activity cell.
- `physical_activity_bbox_fill`: activity cells divided by the activity bounding-box area.

Influence-shape watch metrics:

- `influence_component_count_by_entity`: connected components above an influence threshold such as `influence >= 10`.
- `influence_largest_component_share_by_entity`: largest influenced component cell count divided by total influenced cells for that entity.
- `influence_isolated_cell_count_by_entity`: influenced cells above threshold with no same-entity influenced neighbor above threshold.
- `influence_mean_distance_from_anchor_by_entity`: mean hex distance from the entity's physical anchor or owned coverage to influenced cells.
- `influence_p95_distance_from_anchor_by_entity`: p95 anchor distance for influenced cells.

Expected influence shape:

- Spider-leg tendrils are acceptable if there is still one dominant component.
- Heavily discontinuous influence is a tuning problem: many islands or low largest-component share means projection is sampling too far from the frontier or consolidation is too weak.
- These metrics are a watch item for the new settlement/faction influence system, not a reason to revive old entity-process verb experiments.

Semantic utilization metrics:

- `semantic_activity_points`: count distinct semantic points touched by cumulative activity.
- `semantic_activity_entropy`: normalized entropy of activity event counts by semantic point.
- `semantic_activity_top_point_share`: share of semantic activity on the busiest point.
- `semantic_activity_axis_anisotropy`: largest semantic q/r/s span divided by smallest span.
- `semantic_largest_unoccupied_strict_hex_radius`: max exact semantic hex distance from sampled semantic points to nearest semantic activity point.
- `semantic_activity_bbox_fill`: activity points divided by sampled semantic bounding volume.

Expected shape:

- Lower largest strict empty radius is better.
- Higher activity entropy is usually better; a low value means a nexus is swallowing the sim.
- Higher bbox fill is not always better; a compact metropolis can be good, but a single corridor with most of the plane unused is bad.
- Analyze physical and semantic planes separately. A good physical distribution can still hide a collapsed semantic distribution, and vice versa.

## 2. Individual Entity Roaming Variance

Question: how far do individual entities actually roam on each plane?

Telemetry source:

- `movement_events`: `run_id`, `tick`, `entity_id`, `plane_code`, `from_q`, `from_r`, `from_s`, `to_q`, `to_r`, `to_s`, `distance`
- `entity_snapshots`: final active/historical status and kind/subtype/culture joins
- `symbols`: resolve `plane_code`, `kind_id`, `subtype_id`, and `culture_id`

Per-entity physical roaming metrics:

- `physical_move_count`: count `movement_events` where `plane_code = physical`.
- `physical_total_distance`: sum `distance` for physical moves.
- `physical_unique_cells`: count distinct physical destination cells visited.
- `physical_roam_radius_max`: max hex distance from the entity's first observed physical position.
- `physical_roam_radius_p95`: p95 distance from first observed physical position across all observed positions.
- `physical_path_efficiency`: max displacement from start divided by total physical distance, with zero-distance handling.

Per-entity semantic roaming metrics:

- `semantic_move_count`: count `movement_events` where `plane_code = semantic`.
- `semantic_total_distance`: sum `distance` for semantic moves.
- `semantic_unique_points`: count distinct semantic destination coordinates visited.
- `semantic_roam_radius_max`: max hex distance from the entity's first observed semantic position.
- `semantic_roam_radius_p95`: p95 distance from first observed semantic position across all observed positions.
- `semantic_path_efficiency`: max displacement from start divided by total semantic distance, with zero-distance handling.

Aggregate roaming metrics:

- Report p50/p75/p95 by `kind`, `subtype`, and `culture`.
- Report active and historical entities separately.
- Report zero-roam share by plane: active entities with no movement on that plane.
- Report over-clustered roam share: entities with many moves but `unique_cells` or `unique_points` below a small threshold.

Expected shape:

- Movement volume by itself is not bad.
- Bad movement means high movement with low roaming radius, low unique destinations, or repeated rotation around one local cluster.
- Good movement means entities traverse meaningful space, not just spend ticks paying a movement tax.

## 3. Verb Utilization

Question: are verbs being used across the process deck, or does the planner collapse into a few dominant verbs?

Telemetry source:

- `process_executions`: `run_id`, `tick`, `entity_id`, `kind_id`, `plan_id`, `goal_id`, `step_index`, `process_id`, `target_entity_id`, `state_code`, `affected_entity_id`, `locality_id`
- `process_events`: `run_id`, `tick`, `entity_id`, `kind_id`, `executed_process_id`, `state_code`, `candidate_count`, locality fields
- `planner_candidates`: `run_id`, `tick`, `entity_id`, `kind_id`, `process_id`, `target_entity_id`, `raw_score`, `adjusted_score`, `reward_score`, `precondition_status_code`, `diagnostic_code`, `selected`
- `planner_goals`: desired effect and urgency context for candidates
- `symbols`: resolve `process_id`, `kind_id`, `diagnostic_code`, and trace/status codes

Primary utilization metrics:

- `verb_execution_count`: count successful `process_executions` by `process_id`.
- `verb_execution_share_by_kind`: `process_id` executions divided by total executions for that `kind_id`.
- `top_verb_share_by_kind`: max `verb_execution_share_by_kind` per kind.
- `verb_entropy_by_kind`: entropy over process execution shares per kind.
- `active_verb_count_by_kind`: number of verbs with execution share above 1%.
- `dead_verb_count_by_kind`: configured verbs with zero successful executions.

Planner candidate metrics:

- `candidate_count_by_verb`: count `planner_candidates` by `process_id`.
- `selected_count_by_verb`: count `planner_candidates.selected = true` by `process_id`.
- `selection_rate_by_verb`: `selected_count_by_verb / candidate_count_by_verb`.
- `blocked_count_by_verb_and_diagnostic`: count candidates by `process_id` and `diagnostic_code`.
- `capacity_reached_by_verb`: count candidates where `diagnostic_code = capacity_reached`.
- `precondition_failed_by_verb`: count candidates where preconditions fail.

Verb target contention metrics:

- `same_tick_action_target_collision_count`: count `(run_id, tick, process_id, target_entity_id)` groups with more than one distinct acting `entity_id` in successful `process_executions`.
- `same_tick_action_target_excess_claims`: sum `actor_count - 1` over those groups.
- `target_concentration_by_verb`: share of each verb's executions aimed at its top target entity.

Expected shape:

- Dominant verbs are acceptable only if they do not starve the rest of the deck.
- A healthy deck should have multiple active verbs per kind and a lower top-verb share than the current collapse patterns.
- Reservation systems should reduce same-action/same-target pile-on without producing large no-action rates.
- Candidate diagnostics matter: a verb with many candidates and no executions is different from a verb that is never offered.

## Reporting Rules

- Always report all three focus areas together: map utilization, roaming variance, and verb utilization.
- Do not lead with generator fires.
- Do not treat movement share as inherently bad. Interpret movement through roaming variance and utilization.
- Do not treat a green test suite or nonzero process count as evidence that the simulation is behaving well.
- Prefer SQL/Python/DuckDB analysis over C# analysis code for exploratory Monte Carlo interpretation.
