namespace MonteCarloTool;

internal sealed partial class MonteCarloBatchAnalyzer
{
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
            plane_code,
            terrain_id,
            COALESCE(AVG(CAST(active_entity_count AS DOUBLE)), 0.0) AS average_final_active_entities,
            COALESCE(AVG(CASE WHEN cell_count = 0 THEN 0.0 ELSE covered_cell_count::DOUBLE / cell_count END), 0.0) AS average_named_coverage_share
        FROM final_map
        GROUP BY plane_code, terrain_id
        """;
}
