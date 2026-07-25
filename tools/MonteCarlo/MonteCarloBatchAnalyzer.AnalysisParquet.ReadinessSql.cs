namespace MonteCarloTool;

internal sealed partial class MonteCarloBatchAnalyzer
{
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
            SELECT 17, COUNT(*) FROM raw_generator_target_evaluations UNION ALL
            SELECT 18, COUNT(*) FROM raw_generator_outputs UNION ALL
            SELECT 19, COUNT(*) FROM raw_generator_state_snapshots UNION ALL
            SELECT 20, COUNT(*) FROM raw_relationship_events UNION ALL
            SELECT 21, COUNT(*) FROM raw_relationship_snapshots UNION ALL
            SELECT 22, COUNT(*) FROM raw_lifecycle_events UNION ALL
            SELECT 23, COUNT(*) FROM raw_lifecycle_reason_events UNION ALL
            SELECT 24, COUNT(*) FROM raw_cell_influence_events UNION ALL
            SELECT 25, COUNT(*) FROM raw_gambit_events UNION ALL
            SELECT 26, COUNT(*) FROM raw_cell_road_events UNION ALL
            SELECT 27, COUNT(*) FROM raw_culture_field_cell_snapshots UNION ALL
            SELECT 28, COUNT(*) FROM raw_culture_slot_occupancy_events
        ),
        features(feature_code, source_table_code) AS (
            VALUES
                (1, 1), (2, 2), (3, 3), (4, 4), (5, 5),
                (6, 6), (7, 7), (8, 8), (9, 9), (10, 10),
                (11, 11), (12, 12), (13, 13), (14, 14), (15, 15),
                (16, 16), (17, 17), (18, 18), (19, 19), (20, 20),
                (21, 21), (22, 22), (23, 23), (24, 24), (25, 25),
                (26, 26), (27, 27), (28, 28)
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
}
