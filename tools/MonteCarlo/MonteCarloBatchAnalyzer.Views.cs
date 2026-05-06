namespace MonteCarloTool;

using DuckDB.NET.Data;

using Engine.Constants;
using Engine.Observability;

internal sealed partial class MonteCarloBatchAnalyzer
{
    private sealed record AnalysisViewDefinition(string TableName, string SelectSql);

    private static readonly AnalysisViewDefinition[] AnalysisViewDefinitions =
    [
        new(
            "symbol_lookup",
            """
            SELECT
                symbol_kind,
                id,
                any_value(stable_name) AS stable_name,
                any_value(display_name) AS display_name,
                any_value(description) AS description,
                any_value(icon_key) AS icon_key,
                any_value(color_key) AS color_key
            FROM raw_symbols
            GROUP BY symbol_kind, id
            """),
        new("symbols", "SELECT * FROM symbol_lookup"),
        new("entity_name_symbols", "SELECT * FROM raw_entity_name_symbols"),
        new(
            "runs",
            $"""
            SELECT
                r.run_id,
                r.seed,
                COALESCE(domain_symbol.stable_name, CAST(r.domain_id AS VARCHAR)) AS domain,
                CAST(r.started_unix_milliseconds AS VARCHAR) AS started_utc
            FROM raw_runs r
            LEFT JOIN symbol_lookup run_symbol
              ON run_symbol.symbol_kind = {TelemetrySymbolKinds.Run}
             AND run_symbol.id = r.run_id
            LEFT JOIN symbol_lookup domain_symbol
              ON domain_symbol.symbol_kind = {TelemetrySymbolKinds.Domain}
             AND domain_symbol.id = r.domain_id
            """),
        new(
            "tick_snapshots",
            """
            SELECT runs.run_id, ts.tick, ts.active_entities, ts.historical_entities, ts.total_generator_fires,
                   ts.active_relationships, ts.coverage_count, ts.covered_cells, ts.uncovered_cells,
                   ts.physical_spread, ts.semantic_spread
            FROM raw_tick_snapshots ts
            JOIN runs ON runs.seed = (SELECT seed FROM raw_runs WHERE raw_runs.run_id = ts.run_id LIMIT 1)
            """),
        new(
            "kind_snapshots",
            $"""
            SELECT runs.run_id, ks.tick,
                   COALESCE(kind_symbol.stable_name, CAST(ks.kind_id AS VARCHAR)) AS kind,
                   COALESCE(subtype_symbol.stable_name, CAST(ks.subtype_id AS VARCHAR)) AS subtype,
                   ks.active_count, ks.historical_count, ks.average_health, ks.minimum_health, ks.maximum_health
            FROM raw_kind_snapshots ks
            JOIN raw_runs rr ON rr.run_id = ks.run_id
            JOIN runs ON runs.seed = rr.seed
            LEFT JOIN symbol_lookup kind_symbol
              ON kind_symbol.symbol_kind = {(int)Engine.Projection.ProjectionSymbolKind.Kind}
             AND kind_symbol.id = ks.kind_id
            LEFT JOIN symbol_lookup subtype_symbol
              ON subtype_symbol.symbol_kind = {(int)Engine.Projection.ProjectionSymbolKind.Subtype}
             AND subtype_symbol.id = ks.subtype_id
            """),
        new(
            "entity_snapshot_attributes",
            $"""
            SELECT
                eas.run_id,
                eas.tick,
                eas.entity_id,
                string_agg(
                    COALESCE(attribute_symbol.stable_name, CAST(eas.attribute_id AS VARCHAR)) || '=' || CAST(eas.value AS VARCHAR),
                    '|'
                    ORDER BY eas.attribute_id) AS attributes
            FROM raw_entity_attribute_snapshots eas
            LEFT JOIN symbol_lookup attribute_symbol
              ON attribute_symbol.symbol_kind = {(int)Engine.Projection.ProjectionSymbolKind.Attribute}
             AND attribute_symbol.id = eas.attribute_id
            GROUP BY eas.run_id, eas.tick, eas.entity_id
            """),
        new(
            "entity_snapshots",
            $"""
            SELECT runs.run_id, es.tick, es.entity_id,
                   COALESCE(kind_symbol.stable_name, CAST(es.kind_id AS VARCHAR)) AS kind,
                   COALESCE(subtype_symbol.stable_name, CAST(es.subtype_id AS VARCHAR)) AS subtype,
                   COALESCE(culture_symbol.stable_name, CAST(es.culture_id AS VARCHAR)) AS culture,
                   COALESCE(status_symbol.display_name, CAST(es.status_code AS VARCHAR)) AS status,
                   es.physical_q, es.physical_r, es.semantic_q, es.semantic_r, es.semantic_s,
                   COALESCE(attrs.attributes, '') AS attributes
            FROM raw_entity_snapshots es
            JOIN raw_runs rr ON rr.run_id = es.run_id
            JOIN runs ON runs.seed = rr.seed
            LEFT JOIN symbol_lookup kind_symbol
              ON kind_symbol.symbol_kind = {(int)Engine.Projection.ProjectionSymbolKind.Kind}
             AND kind_symbol.id = es.kind_id
            LEFT JOIN symbol_lookup subtype_symbol
              ON subtype_symbol.symbol_kind = {(int)Engine.Projection.ProjectionSymbolKind.Subtype}
             AND subtype_symbol.id = es.subtype_id
            LEFT JOIN symbol_lookup culture_symbol
              ON culture_symbol.symbol_kind = {(int)Engine.Projection.ProjectionSymbolKind.Culture}
             AND culture_symbol.id = es.culture_id
            LEFT JOIN symbol_lookup status_symbol
              ON status_symbol.symbol_kind = {TelemetrySymbolKinds.EntityStatus}
             AND status_symbol.id = es.status_code
            LEFT JOIN entity_snapshot_attributes attrs
              ON attrs.run_id = es.run_id
             AND attrs.tick = es.tick
             AND attrs.entity_id = es.entity_id
            """),
        new(
            "generator_events",
            $"""
            SELECT runs.run_id, ge.tick,
                   CASE
                     WHEN ge.event_type_code = {(int)Engine.Eventing.KernelEventType.GeneratorEvaluated} THEN 'pulse'
                     WHEN ge.event_type_code = {(int)Engine.Eventing.KernelEventType.GeneratorFired} THEN 'fired'
                     ELSE COALESCE(event_symbol.stable_name, CAST(ge.event_type_code AS VARCHAR))
                   END AS event_type,
                   COALESCE(generator_symbol.stable_name, CAST(ge.generator_id AS VARCHAR)) AS generator_id,
                   COALESCE(status_symbol.stable_name, CAST(ge.status_code AS VARCHAR)) AS status,
                   ge.target_pool_count, ge.inactive_rejected_count, ge.subtype_rejected_count,
                   ge.secondary_target_rejected_count, ge.trigger_rejected_count,
                   ge.candidate_count, ge.failed_fire_count,
                   ge.selected_target_entity_id, ge.created_entity_count, ge.created_relationship_count
            FROM raw_generator_events ge
            JOIN raw_runs rr ON rr.run_id = ge.run_id
            JOIN runs ON runs.seed = rr.seed
            LEFT JOIN symbol_lookup generator_symbol
              ON generator_symbol.symbol_kind = {(int)Engine.Projection.ProjectionSymbolKind.Generator}
             AND generator_symbol.id = ge.generator_id
            LEFT JOIN symbol_lookup status_symbol
              ON status_symbol.symbol_kind = {TelemetrySymbolKinds.GeneratorStatus}
             AND status_symbol.id = ge.status_code
            LEFT JOIN symbol_lookup event_symbol
              ON event_symbol.symbol_kind = {TelemetrySymbolKinds.KernelEventType}
             AND event_symbol.id = ge.event_type_code
            """),
        new(
            "generated_entities",
            $"""
            SELECT runs.run_id, generated.tick,
                   COALESCE(generator_symbol.stable_name, CAST(generated.generator_id AS VARCHAR)) AS generator_id,
                   generated.output_entity_id AS entity_id,
                   COALESCE(kind_symbol.stable_name, CAST(snap.kind_id AS VARCHAR)) AS kind,
                   COALESCE(subtype_symbol.stable_name, CAST(snap.subtype_id AS VARCHAR)) AS subtype,
                   COALESCE(culture_symbol.stable_name, CAST(snap.culture_id AS VARCHAR)) AS culture,
                   snap.physical_q, snap.physical_r, snap.semantic_q, snap.semantic_r, snap.semantic_s
            FROM raw_generator_outputs generated
            JOIN raw_runs rr ON rr.run_id = generated.run_id
            JOIN runs ON runs.seed = rr.seed
            LEFT JOIN raw_entity_snapshots snap
              ON snap.run_id = generated.run_id
             AND snap.entity_id = generated.output_entity_id
            LEFT JOIN symbol_lookup generator_symbol
              ON generator_symbol.symbol_kind = {(int)Engine.Projection.ProjectionSymbolKind.Generator}
             AND generator_symbol.id = generated.generator_id
            LEFT JOIN symbol_lookup kind_symbol
              ON kind_symbol.symbol_kind = {(int)Engine.Projection.ProjectionSymbolKind.Kind}
             AND kind_symbol.id = snap.kind_id
            LEFT JOIN symbol_lookup subtype_symbol
              ON subtype_symbol.symbol_kind = {(int)Engine.Projection.ProjectionSymbolKind.Subtype}
             AND subtype_symbol.id = snap.subtype_id
            LEFT JOIN symbol_lookup culture_symbol
              ON culture_symbol.symbol_kind = {(int)Engine.Projection.ProjectionSymbolKind.Culture}
             AND culture_symbol.id = snap.culture_id
            WHERE generated.output_entity_id <> 0
            """),
        new(
            "lifecycle_events",
            $"""
            SELECT runs.run_id, le.tick,
                   CASE WHEN le.status_code = {EntityStatusCodes.Historical} THEN 'death' ELSE COALESCE(event_symbol.stable_name, CAST(le.event_type_code AS VARCHAR)) END AS event_type,
                   le.entity_id,
                   COALESCE(kind_symbol.stable_name, CAST(le.kind_id AS VARCHAR)) AS kind,
                   COALESCE(entity_name.display_name, 'E' || CAST(le.entity_id AS VARCHAR)) AS name
            FROM raw_lifecycle_events le
            JOIN raw_runs rr ON rr.run_id = le.run_id
            JOIN runs ON runs.seed = rr.seed
            LEFT JOIN symbol_lookup event_symbol
              ON event_symbol.symbol_kind = {TelemetrySymbolKinds.KernelEventType}
             AND event_symbol.id = le.event_type_code
            LEFT JOIN symbol_lookup kind_symbol
              ON kind_symbol.symbol_kind = {(int)Engine.Projection.ProjectionSymbolKind.Kind}
             AND kind_symbol.id = le.kind_id
            LEFT JOIN raw_entity_name_symbols entity_name
              ON entity_name.run_id = le.run_id
             AND entity_name.entity_id = le.entity_id
            """),
        new(
            "map_generation_events",
            $"""
            SELECT runs.run_id, mge.tick, mge.base_seed, mge.attempt, mge.terrain_seed, mge.entity_seed,
                   COALESCE(phase_symbol.stable_name, CAST(mge.phase_id AS VARCHAR)) AS phase,
                   COALESCE(description_symbol.stable_name, CAST(mge.description_id AS VARCHAR)) AS description,
                   COALESCE(reason_symbol.stable_name, '') AS reasons,
                   mge.will_retry
            FROM raw_map_generation_events mge
            JOIN raw_runs rr ON rr.run_id = mge.run_id
            JOIN runs ON runs.seed = rr.seed
            LEFT JOIN symbol_lookup phase_symbol
              ON phase_symbol.symbol_kind = {TelemetrySymbolKinds.MapGenerationPhase}
             AND phase_symbol.id = mge.phase_id
            LEFT JOIN symbol_lookup description_symbol
              ON description_symbol.symbol_kind = {TelemetrySymbolKinds.MapGenerationDescription}
             AND description_symbol.id = mge.description_id
            LEFT JOIN symbol_lookup reason_symbol
              ON reason_symbol.symbol_kind = {TelemetrySymbolKinds.MapGenerationReason}
             AND reason_symbol.id = mge.reason_id
            """),
        new(
            "map_snapshots",
            $"""
            SELECT runs.run_id, ms.tick,
                   COALESCE(terrain_symbol.stable_name, CAST(ms.terrain_id AS VARCHAR)) AS terrain,
                   ms.cell_count, ms.covered_cell_count, ms.active_entity_count
            FROM raw_map_snapshots ms
            JOIN raw_runs rr ON rr.run_id = ms.run_id
            JOIN runs ON runs.seed = rr.seed
            LEFT JOIN symbol_lookup terrain_symbol
              ON terrain_symbol.symbol_kind = {(int)Engine.Projection.ProjectionSymbolKind.Terrain}
             AND terrain_symbol.id = ms.terrain_id
            """),
        new(
            "map_cell_events",
            $"""
            SELECT runs.run_id, mce.tick,
                   COALESCE(event_symbol.stable_name, CAST(mce.event_type_code AS VARCHAR)) AS event_type,
                   COALESCE(source_symbol.stable_name, CAST(mce.source_code_id AS VARCHAR)) AS source_code,
                   mce.q, mce.r,
                   COALESCE(previous_terrain_symbol.stable_name, CAST(mce.previous_terrain_id AS VARCHAR)) AS previous_terrain,
                   COALESCE(current_terrain_symbol.stable_name, CAST(mce.current_terrain_id AS VARCHAR)) AS current_terrain,
                   mce.previous_owner_entity_id,
                   previous_owner_name.display_name AS previous_owner_name,
                   mce.current_owner_entity_id,
                   current_owner_name.display_name AS current_owner_name
            FROM raw_map_cell_events mce
            JOIN raw_runs rr ON rr.run_id = mce.run_id
            JOIN runs ON runs.seed = rr.seed
            LEFT JOIN symbol_lookup event_symbol
              ON event_symbol.symbol_kind = {TelemetrySymbolKinds.KernelEventType}
             AND event_symbol.id = mce.event_type_code
            LEFT JOIN symbol_lookup source_symbol
              ON source_symbol.symbol_kind = {(int)Engine.Projection.ProjectionSymbolKind.SourceCode}
             AND source_symbol.id = mce.source_code_id
            LEFT JOIN symbol_lookup previous_terrain_symbol
              ON previous_terrain_symbol.symbol_kind = {(int)Engine.Projection.ProjectionSymbolKind.Terrain}
             AND previous_terrain_symbol.id = mce.previous_terrain_id
            LEFT JOIN symbol_lookup current_terrain_symbol
              ON current_terrain_symbol.symbol_kind = {(int)Engine.Projection.ProjectionSymbolKind.Terrain}
             AND current_terrain_symbol.id = mce.current_terrain_id
            LEFT JOIN raw_entity_name_symbols previous_owner_name
              ON previous_owner_name.run_id = mce.run_id
             AND previous_owner_name.entity_id = mce.previous_owner_entity_id
            LEFT JOIN raw_entity_name_symbols current_owner_name
              ON current_owner_name.run_id = mce.run_id
             AND current_owner_name.entity_id = mce.current_owner_entity_id
            """),
    ];

    private static void CreateAnalysisViews(DuckDBConnection connection)
    {
        foreach (var definition in AnalysisViewDefinitions)
        {
            CreateMaterializedAnalysisTable(connection, definition.TableName, definition.SelectSql);
        }
    }
}
