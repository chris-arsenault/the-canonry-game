namespace MonteCarloTool;

using DuckDB.NET.Data;

using Engine.Constants;
using Engine.Observability.Export;

internal sealed partial class MonteCarloBatchAnalyzer
{
    private void WriteAnalysisParquetTables(string batchDirectory, string analysisDirectory)
    {
        using var connection = OpenBatch(batchDirectory);
        WriteOutcomeAnalysis(connection, analysisDirectory);
        WriteLowAltitudeAnalysis(connection, analysisDirectory);
        WriteProcessAnalysis(connection, analysisDirectory);
        WriteWorldAnalysis(connection, analysisDirectory);
    }

    private static void WriteOutcomeAnalysis(DuckDBConnection connection, string analysisDirectory)
    {
        ExportAnalysisTable(connection, analysisDirectory, "analysis_overview", AnalysisOverviewSql);
        ExportAnalysisTable(connection, analysisDirectory, "analysis_readiness", AnalysisReadinessSql);
        ExportAnalysisTable(connection, analysisDirectory, "gambit_success_rate", GambitSuccessRateSql);
        ExportAnalysisTable(connection, analysisDirectory, "generator_balance", GeneratorBalanceSql);
        ExportAnalysisTable(connection, analysisDirectory, "generated_subtype_coverage", GeneratedSubtypeCoverageSql);
        ExportAnalysisTable(connection, analysisDirectory, "lifecycle", LifecycleSql);
        ExportAnalysisTable(connection, analysisDirectory, "experiment_comparison", ExperimentComparisonSql);
    }

    private static void WriteProcessAnalysis(DuckDBConnection connection, string analysisDirectory)
    {
        ExportAnalysisTable(connection, analysisDirectory, "action_utilization", ActionUtilizationSql);
        ExportAnalysisTable(connection, analysisDirectory, "stock_dynamics", StockDynamicsSql);
    }

    private static void WriteLowAltitudeAnalysis(DuckDBConnection connection, string analysisDirectory)
    {
        ExportAnalysisTable(connection, analysisDirectory, "low_altitude_verb_utilization", LowAltitudeVerbUtilizationSql);
        ExportAnalysisTable(connection, analysisDirectory, "low_altitude_affordance_utilization", LowAltitudeAffordanceUtilizationSql);
        ExportAnalysisTable(connection, analysisDirectory, "entity_roaming", EntityRoamingSql);
        ExportAnalysisTable(connection, analysisDirectory, "roaming_summary", RoamingSummarySql);
        ExportAnalysisTable(connection, analysisDirectory, "influence_dynamics", InfluenceDynamicsSql);
        ExportAnalysisTable(connection, analysisDirectory, "road_creation", RoadCreationSql);
        ExportAnalysisTable(connection, analysisDirectory, "road_usage", RoadUsageSql);
        ExportAnalysisTable(connection, analysisDirectory, "road_reinforcement", RoadReinforcementSql);
    }

    private static void WriteWorldAnalysis(DuckDBConnection connection, string analysisDirectory)
    {
        ExportAnalysisTable(connection, analysisDirectory, "map_spread", MapSpreadSql);
        ExportAnalysisTable(connection, analysisDirectory, "coverage_development", CoverageDevelopmentSql);
        ExportAnalysisTable(connection, analysisDirectory, "terrain_occupancy", TerrainOccupancySql);
    }

    private static void ExportAnalysisTable(
        DuckDBConnection connection,
        string analysisDirectory,
        string tableName,
        string selectSql)
    {
        var parquetPath = Path.Combine(analysisDirectory, $"{tableName}.parquet");
        SqlFileHelpers.Execute(
            connection,
            $"COPY ({selectSql}) TO {SqlFileHelpers.QuoteString(parquetPath)} (FORMAT PARQUET);");
    }

    private const string AnalysisOverviewSql = """
        WITH final_ticks AS (
            SELECT run_id, MAX(tick) AS final_tick
            FROM raw_tick_snapshots
            GROUP BY run_id
        ),
        final_snapshots AS (
            SELECT tick.*
            FROM raw_tick_snapshots tick
            JOIN final_ticks final
              ON final.run_id = tick.run_id
             AND final.final_tick = tick.tick
        )
        SELECT
            COUNT(*) AS run_count,
            COALESCE(MAX(tick), 0) AS max_tick,
            COALESCE(SUM(total_generator_fires), 0) AS total_generator_fires,
            COALESCE(AVG(CAST(total_generator_fires AS DOUBLE)), 0.0) AS average_fires_per_run,
            COALESCE(AVG(CAST(active_entities AS DOUBLE)), 0.0) AS average_final_active_entities,
            COALESCE(AVG(CAST(historical_entities AS DOUBLE)), 0.0) AS average_final_historical_entities,
            COALESCE(AVG(CAST(physical_spread AS DOUBLE)), 0.0) AS average_final_physical_spread,
            COALESCE(AVG(CAST(semantic_spread AS DOUBLE)), 0.0) AS average_final_semantic_spread,
            COALESCE(AVG(CAST(uncovered_cells AS DOUBLE)), 0.0) AS average_final_uncovered_cells
        FROM final_snapshots
        """;

    private const string AnalysisReadinessSql = """
        WITH source_counts(source_table_code, source_row_count) AS (
            SELECT 1, COUNT(*) FROM raw_runs UNION ALL
            SELECT 2, COUNT(*) FROM raw_experiment_variants UNION ALL
            SELECT 3, COUNT(*) FROM raw_tick_snapshots UNION ALL
            SELECT 4, COUNT(*) FROM raw_kind_snapshots UNION ALL
            SELECT 5, COUNT(*) FROM raw_entity_events UNION ALL
            SELECT 6, COUNT(*) FROM raw_entity_snapshots UNION ALL
            SELECT 7, COUNT(*) FROM raw_entity_attribute_snapshots UNION ALL
            SELECT 8, COUNT(*) FROM raw_attribute_events UNION ALL
            SELECT 9, COUNT(*) FROM raw_movement_events UNION ALL
            SELECT 10, COUNT(*) FROM raw_trace_events UNION ALL
            SELECT 11, COUNT(*) FROM raw_map_cell_events UNION ALL
            SELECT 12, COUNT(*) FROM raw_map_snapshots UNION ALL
            SELECT 13, COUNT(*) FROM raw_coverage_snapshots UNION ALL
            SELECT 14, COUNT(*) FROM raw_coverage_terrain_snapshots UNION ALL
            SELECT 15, COUNT(*) FROM raw_placement_events UNION ALL
            SELECT 16, COUNT(*) FROM raw_generator_events UNION ALL
            SELECT 17, COUNT(*) FROM raw_generator_outputs UNION ALL
            SELECT 18, COUNT(*) FROM raw_generator_state_snapshots UNION ALL
            SELECT 19, COUNT(*) FROM raw_relationship_events UNION ALL
            SELECT 20, COUNT(*) FROM raw_relationship_snapshots UNION ALL
            SELECT 21, COUNT(*) FROM raw_lifecycle_events UNION ALL
            SELECT 22, COUNT(*) FROM raw_lifecycle_reason_events UNION ALL
            SELECT 23, COUNT(*) FROM raw_cell_influence_events UNION ALL
            SELECT 24, COUNT(*) FROM raw_gambit_events UNION ALL
            SELECT 25, COUNT(*) FROM raw_cell_road_events
        ),
        features(feature_code, source_table_code) AS (
            VALUES
                (1, 1), (2, 2), (3, 3), (4, 4), (5, 5),
                (6, 6), (7, 7), (8, 8), (9, 9), (10, 10),
                (11, 11), (12, 12), (13, 13), (14, 14), (15, 15),
                (16, 16), (17, 17), (18, 18), (19, 19), (20, 20),
                (21, 21), (22, 22), (23, 23), (24, 24), (25, 25)
        )
        SELECT
            feature_code,
            source_table_code,
            source_row_count,
            source_row_count > 0 AS ready,
            source_row_count > 0 AS trustworthy,
            CASE WHEN source_row_count > 0 THEN 1 ELSE 2 END AS status_code
        FROM features
        JOIN source_counts USING (source_table_code)
        """;

    private const string GambitSuccessRateSql = """
        SELECT
            gambit_id,
            COUNT(*) AS attempt_count,
            SUM(CASE WHEN succeeded THEN 1 ELSE 0 END) AS success_count,
            SUM(CASE WHEN succeeded THEN 0 ELSE 1 END) AS failure_count,
            CASE
                WHEN COUNT(*) = 0 THEN 0.0
                ELSE SUM(CASE WHEN succeeded THEN 1 ELSE 0 END)::DOUBLE / COUNT(*)
            END AS success_rate,
            MIN(tick) AS first_attempt_tick,
            MAX(tick) AS last_attempt_tick,
            COUNT(DISTINCT actor_id) AS distinct_actors,
            COUNT(DISTINCT target_entity_id) AS distinct_targets
        FROM raw_gambit_events
        GROUP BY gambit_id
        """;

    private static readonly string GeneratorBalanceSql = $$"""
        SELECT
            generator_id,
            COUNT(*) AS pulses,
            SUM(CASE WHEN status_code IN ({{GeneratorStatusCodes.Eligible}}, {{GeneratorStatusCodes.Fired}}) THEN 1 ELSE 0 END) AS eligible_pulses,
            SUM(CASE WHEN status_code = {{GeneratorStatusCodes.Blocked}} THEN 1 ELSE 0 END) AS blocked_pulses,
            SUM(CASE WHEN status_code = {{GeneratorStatusCodes.EligibleNotSelected}} THEN 1 ELSE 0 END) AS eligible_not_selected_pulses,
            SUM(CASE WHEN event_type_code = {{(int)Engine.Eventing.KernelEventType.GeneratorFired}} THEN 1 ELSE 0 END) AS fired_count,
            SUM(CASE WHEN status_code = {{GeneratorStatusCodes.FireFailed}} THEN 1 ELSE 0 END) AS fire_failed_pulses,
            CASE
                WHEN SUM(CASE WHEN status_code IN ({{GeneratorStatusCodes.Eligible}}, {{GeneratorStatusCodes.Fired}}) THEN 1 ELSE 0 END) = 0 THEN 0.0
                ELSE SUM(CASE WHEN event_type_code = {{(int)Engine.Eventing.KernelEventType.GeneratorFired}} THEN 1 ELSE 0 END)::DOUBLE /
                     SUM(CASE WHEN status_code IN ({{GeneratorStatusCodes.Eligible}}, {{GeneratorStatusCodes.Fired}}) THEN 1 ELSE 0 END)
            END AS selection_rate_when_eligible,
            COALESCE(AVG(CAST(target_pool_count AS DOUBLE)), 0.0) AS average_target_pool,
            COALESCE(AVG(CAST(candidate_count AS DOUBLE)), 0.0) AS average_candidate_count,
            COALESCE(SUM(created_entity_count), 0) AS created_entity_count,
            COALESCE(SUM(created_relationship_count), 0) AS created_relationship_count,
            COALESCE(SUM(created_map_cell_count), 0) AS created_map_cell_count,
            COALESCE(MAX(CASE WHEN status_code <> {{GeneratorStatusCodes.Fired}} THEN status_code ELSE 0 END), 0) AS primary_blocker_code
        FROM raw_generator_events
        GROUP BY generator_id
        """;

    private const string GeneratedSubtypeCoverageSql = """
        WITH entity_domain AS (
            SELECT run_id, entity_id, any_value(kind_id) AS kind_id, any_value(subtype_id) AS subtype_id
            FROM raw_entity_snapshots
            GROUP BY run_id, entity_id
        )
        SELECT
            output.generator_id,
            COALESCE(entity_domain.kind_id, 0) AS kind_id,
            COALESCE(entity_domain.subtype_id, 0) AS subtype_id,
            COUNT(*) AS produced_count
        FROM raw_generator_outputs output
        LEFT JOIN entity_domain
          ON entity_domain.run_id = output.run_id
         AND entity_domain.entity_id = output.output_entity_id
        WHERE output.output_entity_id <> 0
        GROUP BY output.generator_id, kind_id, subtype_id
        """;

    private static readonly string LowAltitudeVerbUtilizationSql = $$"""
        WITH low_altitude AS (
            SELECT
                trace.run_id,
                trace.entity_id,
                COALESCE(entity.kind_id, 0) AS kind_id,
                trace.action_id AS verb_id,
                trace.parent_candidate_index AS operation_code,
                trace.effect_scope_code AS plane_code,
                trace.effect_value_id AS target_mode_code,
                trace.effect_kind_id AS target_kind_id,
                trace.status_code,
                trace.adjusted_weight,
                trace.affordance_score,
                trace.affordance_bonus
            FROM raw_trace_events trace
            LEFT JOIN (
                SELECT run_id, entity_id, any_value(kind_id) AS kind_id
                FROM raw_entity_snapshots
                GROUP BY run_id, entity_id
            ) entity
              ON entity.run_id = trace.run_id
             AND entity.entity_id = trace.entity_id
            WHERE trace.action_family_code = {{ActionFamilyCodes.LowAltitudeVerb}}
              AND trace.trace_subject_code IN ({{TraceSubjectCodes.MovementIntent}}, {{TraceSubjectCodes.InfluenceIntent}}, {{TraceSubjectCodes.RelationshipIntent}})
        ),
        grouped AS (
            SELECT
                kind_id,
                verb_id,
                operation_code,
                plane_code,
                target_mode_code,
                target_kind_id,
                COUNT(*) AS trace_event_count,
                SUM(CASE WHEN status_code = {{TraceStatusCodes.Offered}} THEN 1 ELSE 0 END) AS offered_count,
                SUM(CASE WHEN status_code = {{TraceStatusCodes.Selected}} THEN 1 ELSE 0 END) AS selected_count,
                SUM(CASE WHEN status_code = {{TraceStatusCodes.NoTarget}} THEN 1 ELSE 0 END) AS no_target_count,
                COALESCE(AVG(CASE WHEN status_code = {{TraceStatusCodes.Selected}} THEN adjusted_weight ELSE NULL END), 0.0) AS average_selected_weight,
                COALESCE(AVG(CASE WHEN status_code = {{TraceStatusCodes.Selected}} THEN affordance_score ELSE NULL END), 0.0) AS average_selected_affordance_score,
                COALESCE(AVG(CASE WHEN status_code = {{TraceStatusCodes.Selected}} THEN affordance_bonus ELSE NULL END), 0.0) AS average_selected_affordance_bonus,
                SUM(CASE WHEN status_code = {{TraceStatusCodes.Selected}} AND affordance_score > 0 THEN 1 ELSE 0 END) AS selected_with_affordance_count,
                COUNT(DISTINCT entity_id) AS distinct_entities
            FROM low_altitude
            GROUP BY kind_id, verb_id, operation_code, plane_code, target_mode_code, target_kind_id
        )
        SELECT
            kind_id,
            verb_id,
            operation_code,
            plane_code,
            target_mode_code,
            target_kind_id,
            trace_event_count,
            offered_count,
            selected_count,
            no_target_count,
            CASE WHEN offered_count = 0 THEN 0.0 ELSE selected_count::DOUBLE / offered_count END AS selection_rate_when_offered,
            average_selected_weight,
            average_selected_affordance_score,
            average_selected_affordance_bonus,
            selected_with_affordance_count,
            distinct_entities
        FROM grouped
        """;

    private static readonly string EntityRoamingSql = $$"""
        WITH entities AS (
            SELECT
                run_id,
                entity_id,
                any_value(kind_id) AS kind_id,
                any_value(subtype_id) AS subtype_id,
                any_value(culture_id) AS culture_id,
                arg_max(status_code, tick) AS status_code
            FROM raw_entity_snapshots
            GROUP BY run_id, entity_id
        ),
        planes(plane_code) AS (
            VALUES ({{PlaneCodes.Physical}}), ({{PlaneCodes.Semantic}})
        ),
        first_snapshots AS (
            SELECT *
            FROM (
                SELECT
                    snapshot.*,
                    ROW_NUMBER() OVER (PARTITION BY run_id, entity_id ORDER BY tick) AS rank
                FROM raw_entity_snapshots snapshot
            ) ranked
            WHERE rank = 1
        ),
        start_positions AS (
            SELECT
                run_id,
                entity_id,
                {{PlaneCodes.Physical}} AS plane_code,
                physical_q AS q,
                physical_r AS r,
                -(physical_q + physical_r) AS s
            FROM first_snapshots
            UNION ALL
            SELECT
                run_id,
                entity_id,
                {{PlaneCodes.Semantic}} AS plane_code,
                semantic_q AS q,
                semantic_r AS r,
                semantic_s AS s
            FROM first_snapshots
        ),
        observed_positions AS (
            SELECT
                run_id,
                entity_id,
                {{PlaneCodes.Physical}} AS plane_code,
                physical_q AS q,
                physical_r AS r,
                -(physical_q + physical_r) AS s
            FROM raw_entity_snapshots
            UNION ALL
            SELECT
                run_id,
                entity_id,
                {{PlaneCodes.Semantic}} AS plane_code,
                semantic_q AS q,
                semantic_r AS r,
                semantic_s AS s
            FROM raw_entity_snapshots
            UNION ALL
            SELECT run_id, entity_id, plane_code, from_q AS q, from_r AS r, from_s AS s
            FROM raw_movement_events
            UNION ALL
            SELECT run_id, entity_id, plane_code, to_q AS q, to_r AS r, to_s AS s
            FROM raw_movement_events
        ),
        movement AS (
            SELECT
                run_id,
                entity_id,
                plane_code,
                COUNT(*) AS move_count,
                COALESCE(SUM(distance), 0) AS total_distance
            FROM raw_movement_events
            GROUP BY run_id, entity_id, plane_code
        ),
        position_stats AS (
            SELECT
                position.run_id,
                position.entity_id,
                position.plane_code,
                COUNT(DISTINCT CAST(position.q AS VARCHAR) || ',' || CAST(position.r AS VARCHAR) || ',' || CAST(position.s AS VARCHAR)) AS unique_positions,
                COALESCE(MAX((ABS(position.q - start.q) + ABS(position.r - start.r) + ABS(position.s - start.s)) / 2), 0) AS roam_radius_max,
                COALESCE(QUANTILE_CONT((ABS(position.q - start.q) + ABS(position.r - start.r) + ABS(position.s - start.s)) / 2, 0.95), 0.0) AS roam_radius_p95
            FROM observed_positions position
            JOIN start_positions start
              ON start.run_id = position.run_id
             AND start.entity_id = position.entity_id
             AND start.plane_code = position.plane_code
            GROUP BY position.run_id, position.entity_id, position.plane_code
        )
        SELECT
            entity.run_id,
            entity.entity_id,
            entity.kind_id,
            entity.subtype_id,
            entity.culture_id,
            entity.status_code,
            plane.plane_code,
            COALESCE(movement.move_count, 0) AS move_count,
            COALESCE(movement.total_distance, 0) AS total_distance,
            COALESCE(position_stats.unique_positions, 1) AS unique_positions,
            COALESCE(position_stats.roam_radius_max, 0) AS roam_radius_max,
            COALESCE(position_stats.roam_radius_p95, 0.0) AS roam_radius_p95,
            CASE
                WHEN COALESCE(movement.total_distance, 0) = 0 THEN 0.0
                ELSE COALESCE(position_stats.roam_radius_max, 0)::DOUBLE / movement.total_distance
            END AS path_efficiency
        FROM entities entity
        CROSS JOIN planes plane
        LEFT JOIN movement
          ON movement.run_id = entity.run_id
         AND movement.entity_id = entity.entity_id
         AND movement.plane_code = plane.plane_code
        LEFT JOIN position_stats
          ON position_stats.run_id = entity.run_id
         AND position_stats.entity_id = entity.entity_id
         AND position_stats.plane_code = plane.plane_code
        """;

    private static readonly string RoamingSummarySql = $$"""
        WITH roaming AS (
            {{EntityRoamingSql}}
        )
        SELECT
            kind_id,
            plane_code,
            COUNT(*) AS entity_count,
            SUM(CASE WHEN move_count > 0 THEN 1 ELSE 0 END) AS mover_count,
            AVG(CASE WHEN move_count = 0 THEN 1.0 ELSE 0.0 END) AS zero_move_share,
            COALESCE(AVG(CAST(move_count AS DOUBLE)), 0.0) AS average_move_count,
            COALESCE(AVG(CAST(unique_positions AS DOUBLE)), 0.0) AS average_unique_positions,
            COALESCE(AVG(CAST(roam_radius_max AS DOUBLE)), 0.0) AS average_roam_radius,
            COALESCE(QUANTILE_CONT(roam_radius_max, 0.50), 0.0) AS p50_roam_radius,
            COALESCE(QUANTILE_CONT(roam_radius_max, 0.75), 0.0) AS p75_roam_radius,
            COALESCE(QUANTILE_CONT(roam_radius_max, 0.95), 0.0) AS p95_roam_radius,
            AVG(CASE WHEN move_count >= 10 AND unique_positions <= 3 THEN 1.0 ELSE 0.0 END) AS overclustered_roam_share
        FROM roaming
        GROUP BY kind_id, plane_code
        """;

    private static readonly string InfluenceDynamicsSql = $"""
        WITH influence_events AS (
            SELECT
                run_id,
                entity_id,
                plane_code,
                q,
                r,
                SUM(delta) AS final_influence,
                COUNT(*) AS event_count,
                SUM(CASE WHEN delta > 0 THEN 1 ELSE 0 END) AS positive_delta_events,
                SUM(CASE WHEN delta < 0 THEN 1 ELSE 0 END) AS negative_delta_events
            FROM raw_cell_influence_events
            GROUP BY run_id, entity_id, plane_code, q, r
        ),
        influenced_cells AS (
            SELECT *,
                   -(q + r) AS s
            FROM influence_events
            WHERE final_influence >= 10.0
        ),
        final_entities AS (
            SELECT *
            FROM (
                SELECT
                    snapshot.run_id,
                    snapshot.entity_id,
                    snapshot.kind_id,
                    snapshot.subtype_id,
                    snapshot.status_code,
                    snapshot.physical_q,
                    snapshot.physical_r,
                    -(snapshot.physical_q + snapshot.physical_r) AS physical_s,
                    snapshot.semantic_q,
                    snapshot.semantic_r,
                    snapshot.semantic_s,
                    ROW_NUMBER() OVER (PARTITION BY snapshot.run_id, snapshot.entity_id ORDER BY snapshot.tick DESC) AS rank
                FROM raw_entity_snapshots snapshot
            ) ranked
            WHERE rank = 1
        ),
        neighbor_counts AS (
            SELECT
                cell.run_id,
                cell.entity_id,
                cell.plane_code,
                cell.q,
                cell.r,
                COUNT(neighbor.q) AS same_entity_neighbor_count
            FROM influenced_cells cell
            LEFT JOIN influenced_cells neighbor
              ON neighbor.run_id = cell.run_id
             AND neighbor.entity_id = cell.entity_id
             AND neighbor.plane_code = cell.plane_code
             AND (
                    (neighbor.q = cell.q + 1 AND neighbor.r = cell.r) OR
                    (neighbor.q = cell.q - 1 AND neighbor.r = cell.r) OR
                    (neighbor.q = cell.q AND neighbor.r = cell.r + 1) OR
                    (neighbor.q = cell.q AND neighbor.r = cell.r - 1) OR
                    (neighbor.q = cell.q + 1 AND neighbor.r = cell.r - 1) OR
                    (neighbor.q = cell.q - 1 AND neighbor.r = cell.r + 1)
                 )
            GROUP BY cell.run_id, cell.entity_id, cell.plane_code, cell.q, cell.r
        )
        SELECT
            events.run_id,
            events.entity_id,
            events.plane_code,
            COALESCE(entity.kind_id, 0) AS kind_id,
            COALESCE(entity.subtype_id, 0) AS subtype_id,
            SUM(events.event_count) AS event_count,
            SUM(events.positive_delta_events) AS positive_delta_events,
            SUM(events.negative_delta_events) AS negative_delta_events,
            COALESCE(SUM(events.final_influence), 0.0) AS net_influence_delta,
            COUNT(CASE WHEN events.final_influence >= 10.0 THEN 1 ELSE NULL END) AS influenced_cell_count,
            COALESCE(AVG(CASE WHEN events.final_influence >= 10.0 THEN events.final_influence ELSE NULL END), 0.0) AS average_final_influence,
            COALESCE(MAX(CASE WHEN events.final_influence >= 10.0 THEN events.final_influence ELSE NULL END), 0.0) AS max_final_influence,
            COALESCE(SUM(CASE WHEN neighbors.same_entity_neighbor_count = 0 THEN 1 ELSE 0 END), 0) AS isolated_cell_count,
            COALESCE(SUM(neighbors.same_entity_neighbor_count), 0) AS neighbor_connection_count,
            COALESCE(AVG(CASE
                WHEN events.final_influence >= 10.0 THEN
                    CASE WHEN events.plane_code = {PlaneCodes.Semantic} THEN
                        (ABS(events.q - entity.semantic_q) + ABS(events.r - entity.semantic_r) + ABS((-(events.q + events.r)) - entity.semantic_s)) / 2.0
                    ELSE
                        (ABS(events.q - entity.physical_q) + ABS(events.r - entity.physical_r) + ABS((-(events.q + events.r)) - entity.physical_s)) / 2.0
                    END
                ELSE NULL
            END), 0.0) AS mean_anchor_distance,
            COALESCE(QUANTILE_CONT(CASE
                WHEN events.final_influence >= 10.0 THEN
                    CASE WHEN events.plane_code = {PlaneCodes.Semantic} THEN
                        (ABS(events.q - entity.semantic_q) + ABS(events.r - entity.semantic_r) + ABS((-(events.q + events.r)) - entity.semantic_s)) / 2.0
                    ELSE
                        (ABS(events.q - entity.physical_q) + ABS(events.r - entity.physical_r) + ABS((-(events.q + events.r)) - entity.physical_s)) / 2.0
                    END
                ELSE NULL
            END, 0.95), 0.0) AS p95_anchor_distance,
            COALESCE(MAX(CASE
                WHEN events.final_influence >= 10.0 THEN
                    CASE WHEN events.plane_code = {PlaneCodes.Semantic} THEN
                        (ABS(events.q - entity.semantic_q) + ABS(events.r - entity.semantic_r) + ABS((-(events.q + events.r)) - entity.semantic_s)) / 2
                    ELSE
                        (ABS(events.q - entity.physical_q) + ABS(events.r - entity.physical_r) + ABS((-(events.q + events.r)) - entity.physical_s)) / 2
                    END
                ELSE NULL
            END), 0) AS max_anchor_distance
        FROM influence_events events
        LEFT JOIN final_entities entity
          ON entity.run_id = events.run_id
         AND entity.entity_id = events.entity_id
        LEFT JOIN neighbor_counts neighbors
         ON neighbors.run_id = events.run_id
         AND neighbors.entity_id = events.entity_id
         AND neighbors.plane_code = events.plane_code
         AND neighbors.q = events.q
         AND neighbors.r = events.r
        GROUP BY events.run_id, events.entity_id, events.plane_code, entity.kind_id, entity.subtype_id
        """;

    private const string RoadCreationSql = """
        WITH road_cells AS (
            SELECT
                run_id,
                plane_code,
                q,
                r,
                MIN(CASE WHEN current_quality > 0.0 THEN tick ELSE NULL END) AS first_road_tick,
                MAX(tick) AS last_road_tick,
                ARG_MAX(current_quality, tick) AS final_quality,
                MAX(current_quality) AS max_quality,
                MAX(travel_count) AS travel_count,
                SUM(delta) AS total_quality_delta
            FROM raw_cell_road_events
            GROUP BY run_id, plane_code, q, r
        ),
        qualified AS (
            SELECT *
            FROM road_cells
            WHERE COALESCE(final_quality, 0.0) > 0.0
               OR COALESCE(max_quality, 0.0) > 0.0
               OR COALESCE(travel_count, 0) > 0
        )
        SELECT
            run_id,
            plane_code,
            COUNT(*) AS road_cell_count,
            SUM(CASE WHEN final_quality > 25.0 THEN 1 ELSE 0 END) AS light_road_cell_count,
            SUM(CASE WHEN final_quality > 75.0 THEN 1 ELSE 0 END) AS heavy_road_cell_count,
            COALESCE(SUM(travel_count), 0) AS total_travel_count,
            COALESCE(SUM(total_quality_delta), 0.0) AS total_quality_delta,
            COALESCE(AVG(CAST(final_quality AS DOUBLE)), 0.0) AS average_final_quality,
            COALESCE(MAX(max_quality), 0.0) AS max_quality,
            COALESCE(MIN(first_road_tick), 0) AS first_road_tick,
            COALESCE(MAX(last_road_tick), 0) AS last_road_tick
        FROM qualified
        GROUP BY run_id, plane_code
        """;

    private const string RoadUsageSql = """
        WITH movement AS (
            SELECT
                run_id,
                tick,
                entity_id,
                plane_code,
                to_q,
                to_r,
                distance
            FROM raw_movement_events
        ),
        move_road AS (
            SELECT
                movement.run_id,
                movement.tick,
                movement.entity_id,
                movement.plane_code,
                movement.to_q,
                movement.to_r,
                movement.distance,
                COALESCE(road.current_quality, 0.0) AS destination_road_quality
            FROM movement
            LEFT JOIN LATERAL (
                SELECT current_quality
                FROM raw_cell_road_events road
                WHERE road.run_id = movement.run_id
                  AND road.plane_code = movement.plane_code
                  AND road.q = movement.to_q
                  AND road.r = movement.to_r
                  AND road.tick < movement.tick
                ORDER BY road.tick DESC
                LIMIT 1
            ) road ON TRUE
        )
        SELECT
            run_id,
            plane_code,
            COUNT(*) AS move_count,
            SUM(CASE WHEN destination_road_quality > 25.0 THEN 1 ELSE 0 END) AS road_move_count,
            SUM(CASE WHEN destination_road_quality > 75.0 THEN 1 ELSE 0 END) AS heavy_road_move_count,
            CASE
                WHEN COUNT(*) = 0 THEN 0.0
                ELSE SUM(CASE WHEN destination_road_quality > 25.0 THEN 1 ELSE 0 END)::DOUBLE / COUNT(*)
            END AS road_move_share,
            CASE
                WHEN COUNT(*) = 0 THEN 0.0
                ELSE SUM(CASE WHEN destination_road_quality > 75.0 THEN 1 ELSE 0 END)::DOUBLE / COUNT(*)
            END AS heavy_road_move_share,
            COALESCE(AVG(CAST(destination_road_quality AS DOUBLE)), 0.0) AS average_destination_road_quality,
            COALESCE(SUM(distance), 0) AS total_distance
        FROM move_road
        GROUP BY run_id, plane_code
        """;

    private const string RoadReinforcementSql = """
        WITH road_cells AS (
            SELECT
                run_id,
                plane_code,
                q,
                r,
                MIN(CASE WHEN current_quality > 0.0 THEN tick ELSE NULL END) AS first_road_tick,
                MIN(CASE WHEN current_quality > 25.0 THEN tick ELSE NULL END) AS first_light_road_tick,
                MIN(CASE WHEN current_quality > 75.0 THEN tick ELSE NULL END) AS first_heavy_road_tick,
                ARG_MAX(current_quality, tick) AS final_quality,
                MAX(current_quality) AS max_quality,
                MAX(travel_count) AS travel_count,
                SUM(CASE WHEN delta > 0.0 THEN 1 ELSE 0 END) AS positive_reinforcement_events
            FROM raw_cell_road_events
            GROUP BY run_id, plane_code, q, r
        ),
        qualified AS (
            SELECT *
            FROM road_cells
            WHERE first_road_tick IS NOT NULL
        ),
        cell_reinforcement AS (
            SELECT
                road.run_id,
                road.plane_code,
                road.q,
                road.r,
                road.final_quality,
                road.max_quality,
                road.travel_count,
                road.positive_reinforcement_events,
                SUM(CASE WHEN movement.tick < road.first_road_tick THEN 1 ELSE 0 END) AS moves_before_road,
                SUM(CASE WHEN movement.tick > road.first_road_tick THEN 1 ELSE 0 END) AS moves_after_road,
                SUM(CASE
                    WHEN road.first_light_road_tick IS NOT NULL
                     AND movement.tick >= road.first_light_road_tick THEN 1
                    ELSE 0
                END) AS moves_after_light_road,
                SUM(CASE
                    WHEN road.first_heavy_road_tick IS NOT NULL
                     AND movement.tick >= road.first_heavy_road_tick THEN 1
                    ELSE 0
                END) AS moves_after_heavy_road
            FROM qualified road
            LEFT JOIN raw_movement_events movement
              ON movement.run_id = road.run_id
             AND movement.plane_code = road.plane_code
             AND movement.to_q = road.q
             AND movement.to_r = road.r
            GROUP BY
                road.run_id,
                road.plane_code,
                road.q,
                road.r,
                road.final_quality,
                road.max_quality,
                road.travel_count,
                road.positive_reinforcement_events
        )
        SELECT
            run_id,
            plane_code,
            COUNT(*) AS road_cell_count,
            SUM(CASE WHEN moves_after_road > 0 AND positive_reinforcement_events > 1 THEN 1 ELSE 0 END) AS reused_road_cell_count,
            CASE
                WHEN COUNT(*) = 0 THEN 0.0
                ELSE SUM(CASE WHEN moves_after_road > 0 AND positive_reinforcement_events > 1 THEN 1 ELSE 0 END)::DOUBLE / COUNT(*)
            END AS reused_road_cell_share,
            SUM(positive_reinforcement_events) AS positive_reinforcement_event_count,
            COALESCE(AVG(CAST(moves_before_road AS DOUBLE)), 0.0) AS average_moves_before_road,
            COALESCE(AVG(CAST(moves_after_road AS DOUBLE)), 0.0) AS average_moves_after_road,
            COALESCE(AVG(CAST(moves_after_light_road AS DOUBLE)), 0.0) AS average_moves_after_light_road,
            COALESCE(AVG(CAST(moves_after_heavy_road AS DOUBLE)), 0.0) AS average_moves_after_heavy_road,
            COALESCE(AVG(CAST(travel_count AS DOUBLE)), 0.0) AS average_travel_count,
            COALESCE(AVG(CAST(final_quality AS DOUBLE)), 0.0) AS average_final_quality,
            COALESCE(MAX(max_quality), 0.0) AS max_quality
        FROM cell_reinforcement
        GROUP BY run_id, plane_code
        """;

    private const string MapSpreadSql = """
        WITH snapshot_ticks AS (
            SELECT run_id, MAX(tick) AS tick
            FROM raw_tick_snapshots
            GROUP BY run_id
        ),
        map_totals AS (
            SELECT run_id, tick, SUM(cell_count) AS total_cell_count
            FROM raw_map_snapshots
            GROUP BY run_id, tick
        ),
        active_entities AS (
            SELECT run_id, tick, COUNT(DISTINCT entity_id) AS active_entities
            FROM raw_entity_snapshots
            WHERE status_code = 1
            GROUP BY run_id, tick
        ),
        known_cells AS (
            SELECT DISTINCT run_id, q, r
            FROM raw_map_cell_events
            UNION
            SELECT DISTINCT run_id, q, r
            FROM raw_cell_influence_events
            WHERE plane_code = 1
            UNION
            SELECT DISTINCT run_id, physical_q AS q, physical_r AS r
            FROM raw_entity_snapshots
            UNION
            SELECT DISTINCT run_id, from_q AS q, from_r AS r
            FROM raw_movement_events
            WHERE plane_code = 1
            UNION
            SELECT DISTINCT run_id, to_q AS q, to_r AS r
            FROM raw_movement_events
            WHERE plane_code = 1
        ),
        raw_activity AS (
            SELECT run_id, tick, physical_q AS q, physical_r AS r
            FROM raw_entity_snapshots
            WHERE status_code = 1
            UNION ALL
            SELECT run_id, tick, from_q AS q, from_r AS r
            FROM raw_movement_events
            WHERE plane_code = 1
            UNION ALL
            SELECT run_id, tick, to_q AS q, to_r AS r
            FROM raw_movement_events
            WHERE plane_code = 1
            UNION ALL
            SELECT run_id, tick, q, r
            FROM raw_placement_events
            WHERE plane_code = 1 AND selected AND tick > 0
            UNION ALL
            SELECT run_id, tick, q, r
            FROM raw_map_cell_events
            WHERE tick > 0
            UNION ALL
            SELECT run_id, tick, q, r
            FROM raw_cell_influence_events
            WHERE plane_code = 1 AND tick > 0
        ),
        activity_by_snapshot AS (
            SELECT ticks.run_id, ticks.tick AS snapshot_tick, activity.q, activity.r, COUNT(*) AS activity_count
            FROM snapshot_ticks ticks
            JOIN raw_activity activity
              ON activity.run_id = ticks.run_id
             AND activity.tick <= ticks.tick
            GROUP BY ticks.run_id, ticks.tick, activity.q, activity.r
        ),
        activity_totals AS (
            SELECT
                run_id,
                snapshot_tick,
                COUNT(*) AS activity_cells,
                SUM(activity_count) AS activity_event_count,
                MAX(activity_count) AS top_cell_activity_count,
                MIN(q) AS min_q,
                MAX(q) AS max_q,
                MIN(r) AS min_r,
                MAX(r) AS max_r,
                MIN(q + r) AS min_s,
                MAX(q + r) AS max_s
            FROM activity_by_snapshot
            GROUP BY run_id, snapshot_tick
        ),
        activity_probabilities AS (
            SELECT
                activity.run_id,
                activity.snapshot_tick,
                activity.activity_count::DOUBLE / NULLIF(totals.activity_event_count, 0) AS probability
            FROM activity_by_snapshot activity
            JOIN activity_totals totals
              ON totals.run_id = activity.run_id
             AND totals.snapshot_tick = activity.snapshot_tick
        ),
        activity_entropy AS (
            SELECT
                probabilities.run_id,
                probabilities.snapshot_tick,
                CASE WHEN MAX(totals.activity_cells) <= 1 THEN 0.0
                     ELSE -SUM(probabilities.probability * LN(probabilities.probability)) /
                          LN(MAX(totals.activity_cells)::DOUBLE)
                END AS activity_entropy
            FROM activity_probabilities probabilities
            JOIN activity_totals totals
              ON totals.run_id = probabilities.run_id
             AND totals.snapshot_tick = probabilities.snapshot_tick
            GROUP BY probabilities.run_id, probabilities.snapshot_tick
        ),
        nearest_activity AS (
            SELECT
                ticks.run_id,
                ticks.tick AS snapshot_tick,
                cells.q,
                cells.r,
                MIN((ABS(cells.q - activity.q) +
                     ABS(cells.r - activity.r) +
                     ABS((cells.q + cells.r) - (activity.q + activity.r))) / 2) AS nearest_activity_radius
            FROM snapshot_ticks ticks
            JOIN known_cells cells
              ON cells.run_id = ticks.run_id
            LEFT JOIN activity_by_snapshot activity
              ON activity.run_id = ticks.run_id
             AND activity.snapshot_tick = ticks.tick
            GROUP BY ticks.run_id, ticks.tick, cells.q, cells.r
        ),
        empty_radius AS (
            SELECT
                run_id,
                snapshot_tick,
                COALESCE(MAX(nearest_activity_radius), 0) AS largest_unoccupied_strict_hex_radius
            FROM nearest_activity
            GROUP BY run_id, snapshot_tick
        )
        SELECT
            ticks.run_id,
            ticks.tick AS snapshot_tick,
            COALESCE(map_totals.total_cell_count, 0) AS total_cell_count,
            COALESCE(active_entities.active_entities, 0) AS active_entities,
            COALESCE(totals.activity_event_count, 0) AS activity_event_count,
            COALESCE(totals.activity_cells, 0) AS activity_cells,
            CASE WHEN COALESCE(map_totals.total_cell_count, 0) = 0 THEN 0.0
                 ELSE COALESCE(totals.activity_cells, 0)::DOUBLE / map_totals.total_cell_count
            END AS activity_cell_share,
            COALESCE(entropy.activity_entropy, 0.0) AS activity_entropy,
            CASE WHEN COALESCE(totals.activity_event_count, 0) = 0 THEN 0.0
                 ELSE COALESCE(totals.top_cell_activity_count, 0)::DOUBLE / totals.activity_event_count
            END AS activity_top_cell_share,
            COALESCE((totals.max_q - totals.min_q + 1) * (totals.max_r - totals.min_r + 1), 0) AS activity_bounding_box_area,
            CASE WHEN COALESCE((totals.max_q - totals.min_q + 1) * (totals.max_r - totals.min_r + 1), 0) = 0 THEN 0.0
                 ELSE COALESCE(totals.activity_cells, 0)::DOUBLE /
                      ((totals.max_q - totals.min_q + 1) * (totals.max_r - totals.min_r + 1))
            END AS activity_coverage_within_bounding_box,
            CASE WHEN COALESCE(totals.activity_cells, 0) = 0 THEN 0.0
                 ELSE GREATEST(
                        totals.max_q - totals.min_q + 1,
                        totals.max_r - totals.min_r + 1,
                        totals.max_s - totals.min_s + 1)::DOUBLE /
                      GREATEST(
                        1,
                        LEAST(
                            totals.max_q - totals.min_q + 1,
                            totals.max_r - totals.min_r + 1,
                            totals.max_s - totals.min_s + 1))
            END AS activity_axis_anisotropy,
            COALESCE(empty_radius.largest_unoccupied_strict_hex_radius, 0) AS largest_unoccupied_strict_hex_radius
        FROM snapshot_ticks ticks
        LEFT JOIN map_totals
          ON map_totals.run_id = ticks.run_id
         AND map_totals.tick = ticks.tick
        LEFT JOIN active_entities
          ON active_entities.run_id = ticks.run_id
         AND active_entities.tick = ticks.tick
        LEFT JOIN activity_totals totals
          ON totals.run_id = ticks.run_id
         AND totals.snapshot_tick = ticks.tick
        LEFT JOIN activity_entropy entropy
          ON entropy.run_id = ticks.run_id
         AND entropy.snapshot_tick = ticks.tick
        LEFT JOIN empty_radius
          ON empty_radius.run_id = ticks.run_id
         AND empty_radius.snapshot_tick = ticks.tick
        """;

    private const string CoverageDevelopmentSql = """
        WITH ranked AS (
            SELECT *,
                   ROW_NUMBER() OVER (PARTITION BY run_id, owner_entity_id ORDER BY tick) AS first_rank,
                   ROW_NUMBER() OVER (PARTITION BY run_id, owner_entity_id ORDER BY tick DESC) AS last_rank
            FROM raw_coverage_snapshots
        ),
        terrain_mix AS (
            SELECT run_id, owner_entity_id, COUNT(DISTINCT terrain_id) AS terrain_mix_count
            FROM raw_coverage_terrain_snapshots
            GROUP BY run_id, owner_entity_id
        )
        SELECT
            final.run_id,
            final.owner_entity_id,
            final.coverage_kind_code,
            COALESCE(initial.cell_count, 0) AS initial_cell_count,
            final.cell_count AS final_cell_count,
            final.cell_count - COALESCE(initial.cell_count, 0) AS cell_growth,
            COALESCE(MAX(terrain_mix.terrain_mix_count), 0) AS terrain_mix_count,
            final.active_entity_count AS active_entities_inside
        FROM ranked final
        LEFT JOIN ranked initial
          ON initial.run_id = final.run_id
         AND initial.owner_entity_id = final.owner_entity_id
         AND initial.first_rank = 1
        LEFT JOIN terrain_mix
          ON terrain_mix.run_id = final.run_id
         AND terrain_mix.owner_entity_id = final.owner_entity_id
        WHERE final.last_rank = 1
        GROUP BY final.run_id, final.owner_entity_id, final.coverage_kind_code,
                 initial.cell_count, final.cell_count, final.active_entity_count
        """;

    private const string TerrainOccupancySql = """
        WITH final_ticks AS (
            SELECT run_id, MAX(tick) AS final_tick
            FROM raw_map_snapshots
            GROUP BY run_id
        ),
        final_map AS (
            SELECT map.*
            FROM raw_map_snapshots map
            JOIN final_ticks final
              ON final.run_id = map.run_id
             AND final.final_tick = map.tick
        )
        SELECT
            terrain_id,
            COALESCE(AVG(CAST(active_entity_count AS DOUBLE)), 0.0) AS average_final_active_entities,
            COALESCE(AVG(CASE WHEN cell_count = 0 THEN 0.0 ELSE covered_cell_count::DOUBLE / cell_count END), 0.0) AS average_named_coverage_share
        FROM final_map
        GROUP BY terrain_id
        """;

    private const string LifecycleSql = """
        WITH final_ticks AS (
            SELECT run_id, MAX(tick) AS final_tick
            FROM raw_kind_snapshots
            GROUP BY run_id
        ),
        final_kind AS (
            SELECT kind.*
            FROM raw_kind_snapshots kind
            JOIN final_ticks final
              ON final.run_id = kind.run_id
             AND final.final_tick = kind.tick
        ),
        deaths AS (
            SELECT kind_id, COUNT(*) AS deaths, AVG(CAST(tick AS DOUBLE)) AS average_death_tick
            FROM raw_lifecycle_events
            GROUP BY kind_id
        ),
        run_count AS (
            SELECT COUNT(DISTINCT run_id) AS value FROM raw_runs
        )
        SELECT
            final_kind.kind_id,
            COALESCE(MAX(deaths.deaths), 0) AS deaths,
            CASE WHEN MAX(run_count.value) = 0 THEN 0.0 ELSE COALESCE(MAX(deaths.deaths), 0)::DOUBLE / MAX(run_count.value) END AS deaths_per_run,
            COALESCE(MAX(deaths.average_death_tick), 0.0) AS average_death_tick,
            COALESCE(AVG(CAST(final_kind.active_count AS DOUBLE)), 0.0) AS average_final_active,
            COALESCE(AVG(CAST(final_kind.historical_count AS DOUBLE)), 0.0) AS average_final_historical,
            COALESCE(AVG(CAST(final_kind.average_health AS DOUBLE)), 0.0) AS average_final_health
        FROM final_kind
        CROSS JOIN run_count
        LEFT JOIN deaths
          ON deaths.kind_id = final_kind.kind_id
        GROUP BY final_kind.kind_id
        """;

    private const string ExperimentComparisonSql = """
        WITH overview AS (
            WITH final_ticks AS (
                SELECT run_id, MAX(tick) AS final_tick
                FROM raw_tick_snapshots
                GROUP BY run_id
            )
            SELECT snapshot.*
            FROM raw_tick_snapshots snapshot
            JOIN final_ticks final
              ON final.run_id = snapshot.run_id
             AND final.final_tick = snapshot.tick
        )
        SELECT
            variant.experiment_id,
            variant.variant_id,
            COUNT(DISTINCT variant.run_id) AS run_count,
            COALESCE(AVG(CAST(overview.total_generator_fires AS DOUBLE)), 0.0) AS average_fires_per_run,
            COALESCE(AVG(CAST(overview.active_entities AS DOUBLE)), 0.0) AS average_final_active_entities,
            COALESCE(AVG(CAST(overview.physical_spread AS DOUBLE)), 0.0) AS average_final_physical_spread,
            COALESCE(AVG(CAST(overview.uncovered_cells AS DOUBLE)), 0.0) AS average_final_uncovered_cells
        FROM raw_experiment_variants variant
        JOIN overview
          ON overview.run_id = variant.run_id
        GROUP BY variant.experiment_id, variant.variant_id
        """;
}
