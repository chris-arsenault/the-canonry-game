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

    private const string CultureBoundaryComponentsSql = """
        SELECT
            run_id,
            tick,
            plane_code,
            culture_id,
            influenced_cell_count,
            component_count,
            largest_component_cell_count,
            largest_component_share,
            interior_cell_count,
            border_cell_count,
            isolated_cell_count,
            contested_cell_count,
            hole_cell_count
        FROM raw_culture_field_component_snapshots
        """;

    private const string CultureBirthsByBoundaryKindSql = """
        WITH birth_planes AS (
            SELECT
                run_id,
                tick,
                entity_id,
                culture_id,
                1 AS plane_code,
                physical_field_band_code AS field_band_code,
                physical_cell_kind_code AS cell_kind_code
            FROM raw_culture_entity_birth_field_events
            UNION ALL
            SELECT
                run_id,
                tick,
                entity_id,
                culture_id,
                2 AS plane_code,
                semantic_field_band_code AS field_band_code,
                semantic_cell_kind_code AS cell_kind_code
            FROM raw_culture_entity_birth_field_events
        )
        SELECT
            run_id,
            culture_id,
            plane_code,
            field_band_code,
            cell_kind_code,
            COUNT(*) AS birth_count,
            MIN(tick) AS first_birth_tick,
            MAX(tick) AS last_birth_tick
        FROM birth_planes
        GROUP BY run_id, culture_id, plane_code, field_band_code, cell_kind_code
        """;

    private const string MinorActorActionsByBoundaryKindSql = """
        SELECT
            run_id,
            culture_id,
            plane_code,
            cell_kind_code,
            action_code,
            targeting_code,
            status_code,
            COUNT(*) AS action_count,
            COALESCE(SUM(delta), 0.0) AS net_delta,
            COALESCE(AVG(score), 0.0) AS average_score,
            COALESCE(AVG(local_raw_mass), 0.0) AS average_local_raw_mass,
            COALESCE(AVG(connected_raw_mass), 0.0) AS average_connected_raw_mass
        FROM raw_minor_actor_verb_events
        GROUP BY run_id, culture_id, plane_code, cell_kind_code, action_code, targeting_code, status_code
        """;
}
