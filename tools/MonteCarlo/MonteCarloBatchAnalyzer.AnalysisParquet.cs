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
        WritePlannerAnalysis(connection, analysisDirectory);
        WriteWorldAnalysis(connection, analysisDirectory);
    }

    private static void WriteOutcomeAnalysis(DuckDBConnection connection, string analysisDirectory)
    {
        ExportAnalysisTable(connection, analysisDirectory, "analysis_overview", AnalysisOverviewSql);
        ExportAnalysisTable(connection, analysisDirectory, "analysis_readiness", AnalysisReadinessSql);
        ExportAnalysisTable(connection, analysisDirectory, "generator_balance", GeneratorBalanceSql);
        ExportAnalysisTable(connection, analysisDirectory, "generated_subtype_coverage", GeneratedSubtypeCoverageSql);
        ExportAnalysisTable(connection, analysisDirectory, "lifecycle", LifecycleSql);
        ExportAnalysisTable(connection, analysisDirectory, "experiment_comparison", ExperimentComparisonSql);
    }

    private static void WritePlannerAnalysis(DuckDBConnection connection, string analysisDirectory)
    {
        ExportAnalysisTable(connection, analysisDirectory, "process_pressure", ProcessPressureSql);
        ExportAnalysisTable(connection, analysisDirectory, "planner_pressure", PlannerPressureSql);
        ExportAnalysisTable(connection, analysisDirectory, "stock_dynamics", StockDynamicsSql);
    }

    private static void WriteWorldAnalysis(DuckDBConnection connection, string analysisDirectory)
    {
        ExportAnalysisTable(connection, analysisDirectory, "map_spread", MapSpreadSql);
        ExportAnalysisTable(connection, analysisDirectory, "coverage_development", CoverageDevelopmentSql);
        ExportAnalysisTable(connection, analysisDirectory, "terrain_occupancy", TerrainOccupancySql);
        ExportAnalysisTable(connection, analysisDirectory, "resource_pressure", ResourcePressureSql);
        ExportAnalysisTable(connection, analysisDirectory, "pressure_dynamics", PressureDynamicsSql);
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
            SELECT 11, COUNT(*) FROM raw_planner_traces UNION ALL
            SELECT 12, COUNT(*) FROM raw_planner_goals UNION ALL
            SELECT 13, COUNT(*) FROM raw_planner_candidates UNION ALL
            SELECT 14, COUNT(*) FROM raw_planner_scores UNION ALL
            SELECT 15, COUNT(*) FROM raw_planner_steps UNION ALL
            SELECT 16, COUNT(*) FROM raw_planner_localities UNION ALL
            SELECT 17, COUNT(*) FROM raw_process_executions UNION ALL
            SELECT 18, COUNT(*) FROM raw_map_cell_events UNION ALL
            SELECT 19, COUNT(*) FROM raw_map_snapshots UNION ALL
            SELECT 20, COUNT(*) FROM raw_coverage_snapshots UNION ALL
            SELECT 21, COUNT(*) FROM raw_coverage_terrain_snapshots UNION ALL
            SELECT 22, COUNT(*) FROM raw_placement_events UNION ALL
            SELECT 23, COUNT(*) FROM raw_resource_events UNION ALL
            SELECT 24, COUNT(*) FROM raw_resource_snapshots UNION ALL
            SELECT 25, COUNT(*) FROM raw_resource_terrain_snapshots UNION ALL
            SELECT 26, COUNT(*) FROM raw_pressure_events UNION ALL
            SELECT 27, COUNT(*) FROM raw_pressure_snapshots UNION ALL
            SELECT 28, COUNT(*) FROM raw_pressure_source_snapshots UNION ALL
            SELECT 29, COUNT(*) FROM raw_generator_events UNION ALL
            SELECT 30, COUNT(*) FROM raw_generator_outputs UNION ALL
            SELECT 31, COUNT(*) FROM raw_generator_state_snapshots UNION ALL
            SELECT 32, COUNT(*) FROM raw_relationship_events UNION ALL
            SELECT 33, COUNT(*) FROM raw_relationship_snapshots UNION ALL
            SELECT 34, COUNT(*) FROM raw_lifecycle_events UNION ALL
            SELECT 35, COUNT(*) FROM raw_lifecycle_reason_events
        ),
        features(feature_code, source_table_code) AS (
            VALUES
                (1, 1), (2, 2), (3, 3), (4, 4), (5, 5),
                (6, 6), (7, 7), (8, 8), (9, 9), (10, 10),
                (11, 11), (12, 12), (13, 13), (14, 14), (15, 15),
                (16, 16), (17, 17), (18, 18), (19, 19), (20, 20),
                (21, 21), (22, 22), (23, 23), (24, 24), (25, 25),
                (26, 26), (27, 27), (28, 28), (29, 29), (30, 30),
                (31, 31), (32, 32), (33, 33), (34, 34), (35, 35)
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

    private const string ProcessPressureSql = """
        SELECT
            kind_id,
            process_id,
            COUNT(*) AS candidate_evaluations,
            SUM(CASE WHEN selected THEN 1 ELSE 0 END) AS selected_count,
            CASE WHEN COUNT(*) = 0 THEN 0.0 ELSE SUM(CASE WHEN selected THEN 1 ELSE 0 END)::DOUBLE / COUNT(*) END AS selection_rate_when_offered,
            COALESCE(AVG(CAST(adjusted_score AS DOUBLE)), 0.0) AS average_score,
            COALESCE(AVG(CASE WHEN precondition_status_code = 0 THEN 1.0 ELSE 0.0 END), 0.0) AS precondition_pass_rate,
            COALESCE(AVG(CASE WHEN cooldown_state_code <> 0 THEN 1.0 ELSE 0.0 END), 0.0) AS cooldown_rate,
            COALESCE(MAX(diagnostic_code), 0) AS top_failure_diagnostic_code,
            1 AS evidence_source_code
        FROM raw_planner_candidates
        GROUP BY kind_id, process_id
        """;

    private const string PlannerPressureSql = """
        WITH execution_counts AS (
            SELECT kind_id, process_id, COUNT(*) AS selected_count
            FROM raw_process_executions
            GROUP BY kind_id, process_id
        ),
        ranked_execution AS (
            SELECT *, ROW_NUMBER() OVER (PARTITION BY kind_id ORDER BY selected_count DESC, process_id) AS rank
            FROM execution_counts
        )
        SELECT
            candidates.kind_id,
            COUNT(DISTINCT candidates.plan_id) AS evaluations,
            COUNT(DISTINCT candidates.goal_id) AS goal_count,
            COUNT(*) AS candidate_count,
            0.0 AS stale_rate,
            COALESCE(AVG(CASE WHEN executions.entity_id IS NULL THEN 1.0 ELSE 0.0 END), 0.0) AS no_execution_rate,
            COALESCE(AVG(CAST(candidates.candidate_index AS DOUBLE)), 0.0) AS average_candidate_count,
            COALESCE(MAX(CASE WHEN ranked.rank = 1 THEN ranked.process_id ELSE 0 END), 0) AS top_executed_process_id,
            CASE
                WHEN COALESCE(SUM(ranked.selected_count), 0) = 0 THEN 0.0
                ELSE COALESCE(MAX(CASE WHEN ranked.rank = 1 THEN ranked.selected_count ELSE 0 END), 0)::DOUBLE /
                     COALESCE(SUM(ranked.selected_count), 1)
            END AS top_process_share_of_executions
        FROM raw_planner_candidates candidates
        LEFT JOIN raw_process_executions executions
          ON executions.run_id = candidates.run_id
         AND executions.entity_id = candidates.entity_id
         AND executions.plan_id = candidates.plan_id
         AND executions.goal_id = candidates.goal_id
        LEFT JOIN ranked_execution ranked
          ON ranked.kind_id = candidates.kind_id
        GROUP BY candidates.kind_id
        """;

    private const string StockDynamicsSql = """
        WITH ordered AS (
            SELECT *,
                   LAG(value) OVER (PARTITION BY run_id, entity_id, attribute_id ORDER BY tick) AS previous_value
            FROM raw_entity_attribute_snapshots
        ),
        deltas AS (
            SELECT *,
                   value - COALESCE(previous_value, value) AS delta,
                   LAG(value - COALESCE(previous_value, value)) OVER (
                       PARTITION BY run_id, entity_id, attribute_id ORDER BY tick) AS previous_delta
            FROM ordered
        ),
        entity_kind AS (
            SELECT run_id, entity_id, any_value(kind_id) AS kind_id
            FROM raw_entity_snapshots
            GROUP BY run_id, entity_id
        )
        SELECT
            samples.run_id,
            samples.entity_id,
            COALESCE(MAX(entity_kind.kind_id), 0) AS kind_id,
            samples.attribute_id,
            COUNT(*) AS sample_count,
            MIN(value) AS minimum_observed,
            MAX(value) AS maximum_observed,
            COALESCE(VAR_POP(value), 0.0) AS variance,
            SUM(CASE WHEN previous_delta * delta < 0 THEN 1 ELSE 0 END) AS reversal_count,
            SUM(CASE WHEN value < 33.333 THEN 1 ELSE 0 END) AS dwell_low_ticks,
            SUM(CASE WHEN value >= 33.333 AND value < 66.666 THEN 1 ELSE 0 END) AS dwell_mid_ticks,
            SUM(CASE WHEN value >= 66.666 THEN 1 ELSE 0 END) AS dwell_high_ticks,
            SUM(CASE WHEN value <= 1.0 THEN 1 ELSE 0 END) AS pinned_low_ticks,
            SUM(CASE WHEN value >= 99.0 THEN 1 ELSE 0 END) AS pinned_high_ticks,
            LEAST(1.0, GREATEST(0.0, (MAX(value) - MIN(value)) / 100.0)) AS normalized_range
        FROM deltas samples
        LEFT JOIN entity_kind
          ON entity_kind.run_id = samples.run_id
         AND entity_kind.entity_id = samples.entity_id
        GROUP BY samples.run_id, samples.entity_id, samples.attribute_id
        HAVING COUNT(*) >= 2
        """;

    private const string MapSpreadSql = """
        WITH entity_cells AS (
            SELECT run_id, tick, entity_id, physical_q, physical_r
            FROM raw_entity_snapshots
            WHERE status_code = 1
        ),
        map_totals AS (
            SELECT run_id, tick, SUM(cell_count) AS total_cell_count
            FROM raw_map_snapshots
            GROUP BY run_id, tick
        )
        SELECT
            cells.run_id,
            cells.tick AS snapshot_tick,
            COALESCE(MAX(map_totals.total_cell_count), 0) AS total_cell_count,
            COUNT(DISTINCT cells.entity_id) AS active_entities,
            COUNT(DISTINCT CAST(cells.physical_q AS VARCHAR) || ':' || CAST(cells.physical_r AS VARCHAR)) AS occupied_cells,
            CASE WHEN COALESCE(MAX(map_totals.total_cell_count), 0) = 0 THEN 0.0
                 ELSE COUNT(DISTINCT CAST(cells.physical_q AS VARCHAR) || ':' || CAST(cells.physical_r AS VARCHAR))::DOUBLE /
                      MAX(map_totals.total_cell_count)
            END AS occupied_cell_share,
            ((MAX(physical_q) - MIN(physical_q) + 1) * (MAX(physical_r) - MIN(physical_r) + 1)) AS bounding_box_area,
            CASE WHEN ((MAX(physical_q) - MIN(physical_q) + 1) * (MAX(physical_r) - MIN(physical_r) + 1)) = 0 THEN 0.0
                 ELSE COUNT(DISTINCT CAST(cells.physical_q AS VARCHAR) || ':' || CAST(cells.physical_r AS VARCHAR))::DOUBLE /
                      ((MAX(physical_q) - MIN(physical_q) + 1) * (MAX(physical_r) - MIN(physical_r) + 1))
            END AS occupancy_within_bounding_box,
            COALESCE(AVG(ABS(physical_q) + ABS(physical_r)), 0.0) AS mean_distance_from_seed,
            COALESCE(MAX(ABS(physical_q) + ABS(physical_r)), 0) AS max_distance_from_seed,
            CASE WHEN (MAX(physical_r) - MIN(physical_r) + 1) = 0 THEN 1.0
                 ELSE (MAX(physical_q) - MIN(physical_q) + 1)::DOUBLE / (MAX(physical_r) - MIN(physical_r) + 1)
            END AS anisotropy
        FROM entity_cells cells
        LEFT JOIN map_totals
          ON map_totals.run_id = cells.run_id
         AND map_totals.tick = cells.tick
        GROUP BY cells.run_id, cells.tick
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

    private const string ResourcePressureSql = """
        WITH first_ticks AS (
            SELECT run_id, resource_id, MIN(tick) AS first_tick
            FROM raw_resource_snapshots
            GROUP BY run_id, resource_id
        ),
        last_ticks AS (
            SELECT run_id, resource_id, MAX(tick) AS last_tick
            FROM raw_resource_snapshots
            GROUP BY run_id, resource_id
        ),
        endpoints AS (
            SELECT
                resource.resource_id,
                AVG(CASE WHEN resource.tick = first_ticks.first_tick THEN resource.total END) AS average_initial_total,
                AVG(CASE WHEN resource.tick = last_ticks.last_tick THEN resource.total END) AS average_final_total
            FROM raw_resource_snapshots resource
            JOIN first_ticks USING (run_id, resource_id)
            JOIN last_ticks USING (run_id, resource_id)
            GROUP BY resource.resource_id
        )
        SELECT
            resource.resource_id,
            COALESCE(AVG(CAST(resource.total AS DOUBLE)), 0.0) AS average_total_over_time,
            COALESCE(MAX(endpoints.average_initial_total), 0.0) AS average_initial_total,
            COALESCE(MAX(endpoints.average_final_total), 0.0) AS average_final_total,
            CASE WHEN COALESCE(MAX(endpoints.average_initial_total), 0.0) = 0.0 THEN 0.0
                 ELSE COALESCE(MAX(endpoints.average_final_total), 0.0) / MAX(endpoints.average_initial_total)
            END AS final_to_initial_ratio,
            COALESCE(MIN(CAST(resource.minimum AS DOUBLE)), 0.0) AS lowest_cell_stock
        FROM raw_resource_snapshots resource
        LEFT JOIN endpoints
          ON endpoints.resource_id = resource.resource_id
        GROUP BY resource.resource_id
        """;

    private const string PressureDynamicsSql = """
        WITH sources AS (
            SELECT pressure_id, scope_code, scope_entity_id, COUNT(DISTINCT source_code_id) AS source_count
            FROM raw_pressure_source_snapshots
            GROUP BY pressure_id, scope_code, scope_entity_id
        )
        SELECT
            pressure.pressure_id,
            pressure.scope_code,
            pressure.scope_entity_id,
            COALESCE(AVG(CAST(pressure.value AS DOUBLE)), 0.0) AS average_value,
            COALESCE(MAX(CAST(pressure.value AS DOUBLE)), 0.0) AS maximum_value,
            COALESCE(MAX(sources.source_count), 0) AS source_count
        FROM raw_pressure_snapshots pressure
        LEFT JOIN sources
          ON sources.pressure_id = pressure.pressure_id
         AND sources.scope_code = pressure.scope_code
         AND sources.scope_entity_id = pressure.scope_entity_id
        GROUP BY pressure.pressure_id, pressure.scope_code, pressure.scope_entity_id
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
