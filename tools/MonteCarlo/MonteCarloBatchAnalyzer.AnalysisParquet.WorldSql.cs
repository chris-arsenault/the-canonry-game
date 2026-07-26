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
        WITH RECURSIVE
        cells AS (
            SELECT
                run_id,
                tick,
                plane_code,
                culture_id,
                q,
                r,
                raw_influence,
                share
            FROM raw_culture_field_cell_snapshots
            WHERE z = 0 AND (raw_influence > 0 OR share > 0)
        ),
        supported AS (
            SELECT *
            FROM cells
            WHERE raw_influence >= 30
        ),
        reach AS (
            SELECT
                run_id,
                tick,
                plane_code,
                culture_id,
                q AS root_q,
                r AS root_r,
                q,
                r
            FROM supported
            UNION
            SELECT
                reach.run_id,
                reach.tick,
                reach.plane_code,
                reach.culture_id,
                reach.root_q,
                reach.root_r,
                neighbor.q,
                neighbor.r
            FROM reach
            JOIN supported neighbor
              ON neighbor.run_id = reach.run_id
             AND neighbor.tick = reach.tick
             AND neighbor.plane_code = reach.plane_code
             AND neighbor.culture_id = reach.culture_id
             AND (
                    (neighbor.q = reach.q + 1 AND neighbor.r = reach.r)
                 OR (neighbor.q = reach.q + 1 AND neighbor.r = reach.r - 1)
                 OR (neighbor.q = reach.q AND neighbor.r = reach.r - 1)
                 OR (neighbor.q = reach.q - 1 AND neighbor.r = reach.r)
                 OR (neighbor.q = reach.q - 1 AND neighbor.r = reach.r + 1)
                 OR (neighbor.q = reach.q AND neighbor.r = reach.r + 1)
             )
        ),
        labels AS (
            SELECT
                run_id,
                tick,
                plane_code,
                culture_id,
                q,
                r,
                MIN((root_q::BIGINT + 1000000) * 2000001 + root_r + 1000000) AS component_id
            FROM reach
            GROUP BY run_id, tick, plane_code, culture_id, q, r
        ),
        component_sizes AS (
            SELECT
                run_id,
                tick,
                plane_code,
                culture_id,
                component_id,
                COUNT(*) AS cell_count
            FROM labels
            GROUP BY run_id, tick, plane_code, culture_id, component_id
        ),
        component_summary AS (
            SELECT
                run_id,
                tick,
                plane_code,
                culture_id,
                COUNT(*) AS component_count,
                MAX(cell_count) AS largest_component_cell_count
            FROM component_sizes
            GROUP BY run_id, tick, plane_code, culture_id
        ),
        cell_shape AS (
            SELECT
                cell.run_id,
                cell.tick,
                cell.plane_code,
                cell.culture_id,
                cell.q,
                cell.r,
                cell.raw_influence,
                cell.share,
                COUNT(neighbor.q) AS supported_adjacent_count
            FROM cells cell
            LEFT JOIN supported neighbor
              ON neighbor.run_id = cell.run_id
             AND neighbor.tick = cell.tick
             AND neighbor.plane_code = cell.plane_code
             AND neighbor.culture_id = cell.culture_id
             AND (
                    (neighbor.q = cell.q + 1 AND neighbor.r = cell.r)
                 OR (neighbor.q = cell.q + 1 AND neighbor.r = cell.r - 1)
                 OR (neighbor.q = cell.q AND neighbor.r = cell.r - 1)
                 OR (neighbor.q = cell.q - 1 AND neighbor.r = cell.r)
                 OR (neighbor.q = cell.q - 1 AND neighbor.r = cell.r + 1)
                 OR (neighbor.q = cell.q AND neighbor.r = cell.r + 1)
             )
            GROUP BY
                cell.run_id, cell.tick, cell.plane_code, cell.culture_id,
                cell.q, cell.r, cell.raw_influence, cell.share
        ),
        shape_summary AS (
            SELECT
                run_id,
                tick,
                plane_code,
                culture_id,
                COUNT(*) AS influenced_cell_count,
                SUM(CASE WHEN raw_influence >= 100 AND supported_adjacent_count >= 5 THEN 1 ELSE 0 END) AS interior_cell_count,
                SUM(CASE WHEN raw_influence >= 5 AND supported_adjacent_count = 0 THEN 1 ELSE 0 END) AS isolated_cell_count
            FROM cell_shape
            GROUP BY run_id, tick, plane_code, culture_id
        )
        SELECT
            shape.run_id,
            shape.tick,
            shape.plane_code,
            shape.culture_id,
            shape.influenced_cell_count,
            COALESCE(component.component_count, 0) AS component_count,
            COALESCE(component.largest_component_cell_count, 0) AS largest_component_cell_count,
            CASE
                WHEN shape.influenced_cell_count = 0 THEN 0.0
                ELSE COALESCE(component.largest_component_cell_count, 0)::DOUBLE
                    * 100.0 / shape.influenced_cell_count
            END AS largest_component_share,
            shape.interior_cell_count,
            shape.influenced_cell_count - shape.interior_cell_count - shape.isolated_cell_count AS border_cell_count,
            shape.isolated_cell_count,
            0 AS contested_cell_count,
            0 AS hole_cell_count
        FROM shape_summary shape
        LEFT JOIN component_summary component
          ON component.run_id = shape.run_id
         AND component.tick = shape.tick
         AND component.plane_code = shape.plane_code
         AND component.culture_id = shape.culture_id
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
            role_code,
            action_code,
            targeting_code,
            COUNT(*) AS action_count,
            COALESCE(SUM(delta), 0.0) AS net_delta,
            COALESCE(AVG(strength), 0.0) AS average_strength,
            COALESCE(AVG(local_raw_mass), 0.0) AS average_local_raw_mass,
            COALESCE(AVG(connected_raw_mass), 0.0) AS average_connected_raw_mass
        FROM raw_minor_actor_work_events
        GROUP BY run_id, culture_id, plane_code, cell_kind_code, role_code, action_code, targeting_code
        """;
}
