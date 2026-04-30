namespace MonteCarloTool;

using DuckDB.NET.Data;

using Engine.Constants;
using Engine.Projection;

internal sealed partial class MonteCarloBatchAnalyzer
{
    private static List<LowAltitudeVerbSummaryRow> LoadLowAltitudeVerbRows(DuckDBConnection connection) =>
        Query(connection, LowAltitudeVerbRowsSql, reader => new LowAltitudeVerbSummaryRow(
            Kind: ReadString(reader, "kind"),
            Verb: ReadString(reader, "verb"),
            Operation: LowAltitudeOperationName(ReadInt(reader, "operation_code")),
            TargetMode: LowAltitudeTargetModeName(ReadInt(reader, "target_mode_code")),
            Plane: PlaneName(ReadInt(reader, "plane_code")),
            OfferedCount: ReadInt(reader, "offered_count"),
            SelectedCount: ReadInt(reader, "selected_count"),
            NoTargetCount: ReadInt(reader, "no_target_count"),
            SelectionRateWhenOffered: ReadDouble(reader, "selection_rate_when_offered"),
            AverageSelectedWeight: ReadDouble(reader, "average_selected_weight")));

    private static List<RoamingSummaryRow> LoadRoamingSummaryRows(DuckDBConnection connection) =>
        Query(connection, RoamingSummaryRowsSql, reader => new RoamingSummaryRow(
            Kind: ReadString(reader, "kind"),
            Plane: PlaneName(ReadInt(reader, "plane_code")),
            EntityCount: ReadInt(reader, "entity_count"),
            MoverCount: ReadInt(reader, "mover_count"),
            ZeroMoveShare: ReadDouble(reader, "zero_move_share"),
            AverageMoveCount: ReadDouble(reader, "average_move_count"),
            AverageUniquePositions: ReadDouble(reader, "average_unique_positions"),
            AverageRoamRadius: ReadDouble(reader, "average_roam_radius")));

    private static List<InfluenceSummaryRow> LoadInfluenceSummaryRows(DuckDBConnection connection) =>
        Query(connection, InfluenceSummaryRowsSql, reader => new InfluenceSummaryRow(
            Kind: ReadString(reader, "kind"),
            Plane: PlaneName(ReadInt(reader, "plane_code")),
            EntityCount: ReadInt(reader, "entity_count"),
            AverageInfluencedCells: ReadDouble(reader, "average_influenced_cells"),
            AverageNetInfluenceDelta: ReadDouble(reader, "average_net_influence_delta"),
            AverageMeanAnchorDistance: ReadDouble(reader, "average_mean_anchor_distance"),
            AverageMaxAnchorDistance: ReadDouble(reader, "average_max_anchor_distance")));

    private static readonly string LowAltitudeVerbRowsSql = $"""
        WITH low_altitude AS (
            SELECT
                trace.run_id,
                trace.entity_id,
                COALESCE(entity.kind_id, 0) AS kind_id,
                trace.action_id AS verb_id,
                trace.parent_candidate_index AS operation_code,
                trace.effect_scope_code AS plane_code,
                trace.effect_value_id AS target_mode_code,
                trace.status_code,
                trace.adjusted_weight
            FROM raw_trace_events trace
            LEFT JOIN (
                SELECT run_id, entity_id, any_value(kind_id) AS kind_id
                FROM raw_entity_snapshots
                GROUP BY run_id, entity_id
            ) entity
              ON entity.run_id = trace.run_id
             AND entity.entity_id = trace.entity_id
            WHERE trace.action_family_code = {ActionFamilyCodes.LowAltitudeVerb}
              AND trace.trace_subject_code IN ({TraceSubjectCodes.MovementIntent}, {TraceSubjectCodes.InfluenceIntent})
        ),
        grouped AS (
            SELECT
                kind_id,
                verb_id,
                operation_code,
                plane_code,
                target_mode_code,
                SUM(CASE WHEN status_code = {TraceStatusCodes.Offered} THEN 1 ELSE 0 END) AS offered_count,
                SUM(CASE WHEN status_code = {TraceStatusCodes.Selected} THEN 1 ELSE 0 END) AS selected_count,
                SUM(CASE WHEN status_code = {TraceStatusCodes.NoTarget} THEN 1 ELSE 0 END) AS no_target_count,
                COALESCE(AVG(CASE WHEN status_code = {TraceStatusCodes.Selected} THEN adjusted_weight ELSE NULL END), 0.0) AS average_selected_weight
            FROM low_altitude
            GROUP BY kind_id, verb_id, operation_code, plane_code, target_mode_code
        )
        SELECT
            COALESCE(kind_symbol.stable_name, CAST(grouped.kind_id AS VARCHAR)) AS kind,
            COALESCE(verb_symbol.stable_name, CAST(grouped.verb_id AS VARCHAR)) AS verb,
            grouped.operation_code,
            grouped.target_mode_code,
            grouped.plane_code,
            grouped.offered_count,
            grouped.selected_count,
            grouped.no_target_count,
            CASE
                WHEN grouped.offered_count = 0 THEN 0.0
                ELSE grouped.selected_count::DOUBLE / grouped.offered_count
            END AS selection_rate_when_offered,
            grouped.average_selected_weight
        FROM grouped
        LEFT JOIN symbol_lookup kind_symbol
          ON kind_symbol.symbol_kind = {(int)ProjectionSymbolKind.Kind}
         AND kind_symbol.id = grouped.kind_id
        LEFT JOIN symbol_lookup verb_symbol
          ON verb_symbol.symbol_kind = {(int)ProjectionSymbolKind.Verb}
         AND verb_symbol.id = grouped.verb_id
        ORDER BY selected_count DESC, offered_count DESC, kind, verb;
        """;

    private static readonly string RoamingSummaryRowsSql = $"""
        WITH entities AS (
            SELECT
                run_id,
                entity_id,
                any_value(kind_id) AS kind_id
            FROM raw_entity_snapshots
            GROUP BY run_id, entity_id
        ),
        planes(plane_code) AS (
            VALUES ({PlaneCodes.Physical}), ({PlaneCodes.Semantic})
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
                {PlaneCodes.Physical} AS plane_code,
                physical_q AS q,
                physical_r AS r,
                -(physical_q + physical_r) AS s
            FROM first_snapshots
            UNION ALL
            SELECT
                run_id,
                entity_id,
                {PlaneCodes.Semantic} AS plane_code,
                semantic_q AS q,
                semantic_r AS r,
                semantic_s AS s
            FROM first_snapshots
        ),
        observed_positions AS (
            SELECT
                run_id,
                entity_id,
                {PlaneCodes.Physical} AS plane_code,
                physical_q AS q,
                physical_r AS r,
                -(physical_q + physical_r) AS s
            FROM raw_entity_snapshots
            UNION ALL
            SELECT
                run_id,
                entity_id,
                {PlaneCodes.Semantic} AS plane_code,
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
                COUNT(*) AS move_count
            FROM raw_movement_events
            GROUP BY run_id, entity_id, plane_code
        ),
        position_stats AS (
            SELECT
                position.run_id,
                position.entity_id,
                position.plane_code,
                COUNT(DISTINCT CAST(position.q AS VARCHAR) || ',' || CAST(position.r AS VARCHAR) || ',' || CAST(position.s AS VARCHAR)) AS unique_positions,
                COALESCE(MAX((ABS(position.q - start.q) + ABS(position.r - start.r) + ABS(position.s - start.s)) / 2), 0) AS roam_radius_max
            FROM observed_positions position
            JOIN start_positions start
              ON start.run_id = position.run_id
             AND start.entity_id = position.entity_id
             AND start.plane_code = position.plane_code
            GROUP BY position.run_id, position.entity_id, position.plane_code
        ),
        roaming AS (
            SELECT
                entity.run_id,
                entity.entity_id,
                entity.kind_id,
                plane.plane_code,
                COALESCE(movement.move_count, 0) AS move_count,
                COALESCE(position_stats.unique_positions, 1) AS unique_positions,
                COALESCE(position_stats.roam_radius_max, 0) AS roam_radius_max
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
        )
        SELECT
            COALESCE(kind_symbol.stable_name, CAST(roaming.kind_id AS VARCHAR)) AS kind,
            roaming.plane_code,
            COUNT(*) AS entity_count,
            SUM(CASE WHEN move_count > 0 THEN 1 ELSE 0 END) AS mover_count,
            AVG(CASE WHEN move_count = 0 THEN 1.0 ELSE 0.0 END) AS zero_move_share,
            COALESCE(AVG(CAST(move_count AS DOUBLE)), 0.0) AS average_move_count,
            COALESCE(AVG(CAST(unique_positions AS DOUBLE)), 0.0) AS average_unique_positions,
            COALESCE(AVG(CAST(roam_radius_max AS DOUBLE)), 0.0) AS average_roam_radius
        FROM roaming
        LEFT JOIN symbol_lookup kind_symbol
          ON kind_symbol.symbol_kind = {(int)ProjectionSymbolKind.Kind}
         AND kind_symbol.id = roaming.kind_id
        GROUP BY kind, roaming.plane_code
        ORDER BY kind, roaming.plane_code;
        """;

    private static readonly string InfluenceSummaryRowsSql = $"""
        WITH influence_events AS (
            SELECT
                run_id,
                entity_id,
                plane_code,
                q,
                r,
                SUM(delta) AS final_influence,
                COUNT(*) AS event_count
            FROM raw_cell_influence_events
            GROUP BY run_id, entity_id, plane_code, q, r
        ),
        final_entities AS (
            SELECT *
            FROM (
                SELECT
                    snapshot.run_id,
                    snapshot.entity_id,
                    snapshot.kind_id,
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
        by_entity AS (
            SELECT
                events.run_id,
                events.entity_id,
                events.plane_code,
                COALESCE(entity.kind_id, 0) AS kind_id,
                COUNT(CASE WHEN events.final_influence >= 10.0 THEN 1 ELSE NULL END) AS influenced_cells,
                COALESCE(SUM(events.final_influence), 0.0) AS net_influence_delta,
                COALESCE(AVG(CASE
                    WHEN events.final_influence >= 10.0 THEN
                        CASE WHEN events.plane_code = {PlaneCodes.Semantic} THEN
                            (ABS(events.q - entity.semantic_q) + ABS(events.r - entity.semantic_r) + ABS((-(events.q + events.r)) - entity.semantic_s)) / 2.0
                        ELSE
                            (ABS(events.q - entity.physical_q) + ABS(events.r - entity.physical_r) + ABS((-(events.q + events.r)) - entity.physical_s)) / 2.0
                        END
                    ELSE NULL
                END), 0.0) AS mean_anchor_distance,
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
            GROUP BY events.run_id, events.entity_id, events.plane_code, entity.kind_id
        )
        SELECT
            COALESCE(kind_symbol.stable_name, CAST(by_entity.kind_id AS VARCHAR)) AS kind,
            by_entity.plane_code,
            COUNT(*) AS entity_count,
            COALESCE(AVG(CAST(influenced_cells AS DOUBLE)), 0.0) AS average_influenced_cells,
            COALESCE(AVG(CAST(net_influence_delta AS DOUBLE)), 0.0) AS average_net_influence_delta,
            COALESCE(AVG(CAST(mean_anchor_distance AS DOUBLE)), 0.0) AS average_mean_anchor_distance,
            COALESCE(AVG(CAST(max_anchor_distance AS DOUBLE)), 0.0) AS average_max_anchor_distance
        FROM by_entity
        LEFT JOIN symbol_lookup kind_symbol
         ON kind_symbol.symbol_kind = {(int)ProjectionSymbolKind.Kind}
         AND kind_symbol.id = by_entity.kind_id
        GROUP BY kind, by_entity.plane_code
        ORDER BY kind, by_entity.plane_code;
        """;
}
