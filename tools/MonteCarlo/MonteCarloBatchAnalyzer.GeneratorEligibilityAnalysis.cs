namespace MonteCarloTool;

using DuckDB.NET.Data;

using Engine.Constants;
using Engine.Eventing;
using Engine.Observability.Export;

internal sealed partial class MonteCarloBatchAnalyzer
{
    private static void WriteGeneratorEligibilityAnalysis(DuckDBConnection connection, string analysisDirectory)
    {
        ExportAnalysisTable(connection, analysisDirectory, "generator_post_fire_eligibility_decay", GeneratorPostFireEligibilityDecaySql);
        ExportAnalysisTable(connection, analysisDirectory, "generator_component_fire_windows", GeneratorComponentFireWindowsSql);
    }

    private static readonly string GeneratorPostFireEligibilityDecaySql = $$"""
        WITH target_at_fire AS (
            SELECT *
            FROM (
                SELECT
                    target.*,
                    ROW_NUMBER() OVER (
                        PARTITION BY run_id, generator_id, tick, target_entity_id
                        ORDER BY CASE WHEN status_code = {{GeneratorStatusCodes.Eligible}} THEN 0 ELSE 1 END
                    ) AS target_rank
                FROM raw_generator_target_evaluations target
            ) ranked
            WHERE target_rank = 1
        ),
        fire_planes AS (
            SELECT
                fire.run_id,
                fire.tick AS fire_tick,
                fire.generator_id,
                fire.selected_target_entity_id AS target_entity_id,
                fire.cause_id,
                fire.relationship_spend_count,
                fire.output_lock_relationship_created_count,
                COALESCE(target.target_culture_id, 0) AS target_culture_id,
                {{PlaneCodes.Physical}} AS plane_code,
                COALESCE(target.physical_component_id, 0) AS component_id,
                COALESCE(target.physical_component_cell_count, 0) AS component_cell_count,
                COALESCE(target.physical_component_raw_mass, 0.0) AS component_raw_mass,
                COALESCE(target.physical_field_band_code, 0) AS fire_field_band_code,
                COALESCE(target.physical_cell_kind_code, 0) AS fire_cell_kind_code,
                COALESCE(target.physical_connected_raw_mass, 0.0) AS fire_connected_raw_mass,
                COALESCE(target.status_code = {{GeneratorStatusCodes.Eligible}}, FALSE) AS same_target_eligible_before_fire
            FROM raw_generator_events fire
            LEFT JOIN target_at_fire target
              ON target.run_id = fire.run_id
             AND target.generator_id = fire.generator_id
             AND target.tick = fire.tick
             AND target.target_entity_id = fire.selected_target_entity_id
            WHERE fire.event_type_code = {{(int)KernelEventType.GeneratorFired}}
              AND fire.selected_target_entity_id IS NOT NULL
            UNION ALL
            SELECT
                fire.run_id,
                fire.tick AS fire_tick,
                fire.generator_id,
                fire.selected_target_entity_id AS target_entity_id,
                fire.cause_id,
                fire.relationship_spend_count,
                fire.output_lock_relationship_created_count,
                COALESCE(target.target_culture_id, 0) AS target_culture_id,
                {{PlaneCodes.Semantic}} AS plane_code,
                COALESCE(target.semantic_component_id, 0) AS component_id,
                COALESCE(target.semantic_component_cell_count, 0) AS component_cell_count,
                COALESCE(target.semantic_component_raw_mass, 0.0) AS component_raw_mass,
                COALESCE(target.semantic_field_band_code, 0) AS fire_field_band_code,
                COALESCE(target.semantic_cell_kind_code, 0) AS fire_cell_kind_code,
                COALESCE(target.semantic_connected_raw_mass, 0.0) AS fire_connected_raw_mass,
                COALESCE(target.status_code = {{GeneratorStatusCodes.Eligible}}, FALSE) AS same_target_eligible_before_fire
            FROM raw_generator_events fire
            LEFT JOIN target_at_fire target
              ON target.run_id = fire.run_id
             AND target.generator_id = fire.generator_id
             AND target.tick = fire.tick
             AND target.target_entity_id = fire.selected_target_entity_id
            WHERE fire.event_type_code = {{(int)KernelEventType.GeneratorFired}}
              AND fire.selected_target_entity_id IS NOT NULL
        ),
        eval_planes AS (
            SELECT
                run_id,
                tick,
                generator_id,
                target_entity_id,
                target_culture_id,
                {{PlaneCodes.Physical}} AS plane_code,
                physical_component_id AS component_id,
                physical_field_band_code AS field_band_code,
                physical_connected_raw_mass AS connected_raw_mass,
                status_code,
                rejection_reason_code
            FROM raw_generator_target_evaluations
            UNION ALL
            SELECT
                run_id,
                tick,
                generator_id,
                target_entity_id,
                target_culture_id,
                {{PlaneCodes.Semantic}} AS plane_code,
                semantic_component_id AS component_id,
                semantic_field_band_code AS field_band_code,
                semantic_connected_raw_mass AS connected_raw_mass,
                status_code,
                rejection_reason_code
            FROM raw_generator_target_evaluations
        ),
        next_target_tick AS (
            SELECT
                fire.run_id,
                fire.fire_tick,
                fire.generator_id,
                fire.target_entity_id,
                fire.plane_code,
                MIN(eval.tick) AS next_tick
            FROM fire_planes fire
            JOIN eval_planes eval
              ON eval.run_id = fire.run_id
             AND eval.generator_id = fire.generator_id
             AND eval.target_entity_id = fire.target_entity_id
             AND eval.plane_code = fire.plane_code
             AND eval.tick > fire.fire_tick
             AND eval.tick <= fire.fire_tick + 500
            GROUP BY fire.run_id, fire.fire_tick, fire.generator_id, fire.target_entity_id, fire.plane_code
        ),
        next_component_tick AS (
            SELECT
                fire.run_id,
                fire.fire_tick,
                fire.generator_id,
                fire.target_entity_id,
                fire.plane_code,
                MIN(eval.tick) AS next_tick
            FROM fire_planes fire
            JOIN eval_planes eval
              ON eval.run_id = fire.run_id
             AND eval.generator_id = fire.generator_id
             AND eval.target_culture_id = fire.target_culture_id
             AND eval.plane_code = fire.plane_code
             AND eval.component_id = fire.component_id
             AND eval.tick > fire.fire_tick
             AND eval.tick <= fire.fire_tick + 500
            WHERE fire.component_id <> 0
            GROUP BY fire.run_id, fire.fire_tick, fire.generator_id, fire.target_entity_id, fire.plane_code
        ),
        target_reeligible AS (
            SELECT
                fire.run_id,
                fire.fire_tick,
                fire.generator_id,
                fire.target_entity_id,
                fire.plane_code,
                MIN(eval.tick - fire.fire_tick) AS post_fire_same_target_reeligible_ticks
            FROM fire_planes fire
            JOIN eval_planes eval
              ON eval.run_id = fire.run_id
             AND eval.generator_id = fire.generator_id
             AND eval.target_entity_id = fire.target_entity_id
             AND eval.plane_code = fire.plane_code
             AND eval.tick > fire.fire_tick
             AND eval.tick <= fire.fire_tick + 500
             AND eval.status_code = {{GeneratorStatusCodes.Eligible}}
            GROUP BY fire.run_id, fire.fire_tick, fire.generator_id, fire.target_entity_id, fire.plane_code
        ),
        next_target_status AS (
            SELECT
                tick.run_id,
                tick.fire_tick,
                tick.generator_id,
                tick.target_entity_id,
                tick.plane_code,
                MAX(CASE WHEN eval.status_code = {{GeneratorStatusCodes.Eligible}} THEN 1 ELSE 0 END) AS same_target_eligible_after_fire
            FROM next_target_tick tick
            JOIN eval_planes eval
              ON eval.run_id = tick.run_id
             AND eval.generator_id = tick.generator_id
             AND eval.target_entity_id = tick.target_entity_id
             AND eval.plane_code = tick.plane_code
             AND eval.tick = tick.next_tick
            GROUP BY tick.run_id, tick.fire_tick, tick.generator_id, tick.target_entity_id, tick.plane_code
        ),
        same_component_after AS (
            SELECT
                tick.run_id,
                tick.fire_tick,
                tick.generator_id,
                tick.target_entity_id,
                tick.plane_code,
                COUNT(DISTINCT eval.target_entity_id) AS same_component_candidate_count_after_fire
            FROM next_component_tick tick
            JOIN fire_planes fire
              ON fire.run_id = tick.run_id
             AND fire.fire_tick = tick.fire_tick
             AND fire.generator_id = tick.generator_id
             AND fire.target_entity_id = tick.target_entity_id
             AND fire.plane_code = tick.plane_code
            JOIN eval_planes eval
              ON eval.run_id = tick.run_id
             AND eval.generator_id = tick.generator_id
             AND eval.target_culture_id = fire.target_culture_id
             AND eval.plane_code = tick.plane_code
             AND eval.component_id = fire.component_id
             AND eval.tick = tick.next_tick
             AND eval.status_code = {{GeneratorStatusCodes.Eligible}}
            GROUP BY tick.run_id, tick.fire_tick, tick.generator_id, tick.target_entity_id, tick.plane_code
        ),
        loss AS (
            SELECT
                fire.run_id,
                fire.fire_tick,
                fire.generator_id,
                fire.target_entity_id,
                fire.plane_code,
                COUNT(DISTINCT CASE
                    WHEN eval.field_band_code = {{CultureFieldStrengthBandCodes.Strong}}
                     AND eval.rejection_reason_code = {{GeneratorTargetRejectionReasonCodes.RequiredRelationship}}
                    THEN eval.target_entity_id ELSE NULL END) AS strong_field_candidate_loss_due_to_causal_spend,
                COUNT(DISTINCT CASE
                    WHEN eval.field_band_code = {{CultureFieldStrengthBandCodes.Strong}}
                     AND eval.rejection_reason_code = {{GeneratorTargetRejectionReasonCodes.RelationshipAbsence}}
                    THEN eval.target_entity_id ELSE NULL END) AS strong_field_candidate_loss_due_to_absence_lock
            FROM fire_planes fire
            LEFT JOIN eval_planes eval
              ON eval.run_id = fire.run_id
             AND eval.generator_id = fire.generator_id
             AND eval.target_culture_id = fire.target_culture_id
             AND eval.plane_code = fire.plane_code
             AND eval.component_id = fire.component_id
             AND eval.tick > fire.fire_tick
             AND eval.tick <= fire.fire_tick + 500
             AND eval.status_code <> {{GeneratorStatusCodes.Eligible}}
            WHERE fire.component_id <> 0
            GROUP BY fire.run_id, fire.fire_tick, fire.generator_id, fire.target_entity_id, fire.plane_code
        ),
        mass_250 AS (
            SELECT
                fire.run_id,
                fire.fire_tick,
                fire.generator_id,
                fire.target_entity_id,
                fire.plane_code,
                ARG_MAX(eval.connected_raw_mass, eval.tick) AS connected_raw_mass_250
            FROM fire_planes fire
            JOIN eval_planes eval
              ON eval.run_id = fire.run_id
             AND eval.generator_id = fire.generator_id
             AND eval.target_entity_id = fire.target_entity_id
             AND eval.plane_code = fire.plane_code
             AND eval.tick > fire.fire_tick
             AND eval.tick <= fire.fire_tick + 250
            GROUP BY fire.run_id, fire.fire_tick, fire.generator_id, fire.target_entity_id, fire.plane_code
        ),
        component_fire_500 AS (
            SELECT
                base.run_id,
                base.fire_tick,
                base.generator_id,
                base.target_entity_id,
                base.plane_code,
                COUNT(*) AS generator_fires_per_culture_component_per_500_ticks
            FROM fire_planes base
            JOIN fire_planes peer
              ON peer.run_id = base.run_id
             AND peer.generator_id = base.generator_id
             AND peer.target_culture_id = base.target_culture_id
             AND peer.plane_code = base.plane_code
             AND peer.component_id = base.component_id
             AND FLOOR(peer.fire_tick / 500.0) = FLOOR(base.fire_tick / 500.0)
            WHERE base.component_id <> 0
            GROUP BY base.run_id, base.fire_tick, base.generator_id, base.target_entity_id, base.plane_code
        )
        SELECT
            fire.run_id,
            fire.fire_tick,
            fire.generator_id,
            fire.target_entity_id,
            fire.target_culture_id,
            fire.plane_code,
            fire.component_id,
            fire.component_cell_count,
            fire.component_raw_mass,
            fire.fire_field_band_code,
            fire.fire_cell_kind_code,
            fire.fire_connected_raw_mass,
            fire.cause_id,
            fire.relationship_spend_count,
            fire.output_lock_relationship_created_count,
            fire.same_target_eligible_before_fire,
            COALESCE(next_target.same_target_eligible_after_fire, 0) > 0 AS same_target_eligible_after_fire,
            COALESCE(target_reeligible.post_fire_same_target_reeligible_ticks, -1) AS post_fire_same_target_reeligible_ticks,
            COALESCE(same_component.same_component_candidate_count_after_fire, 0) AS same_component_candidate_count_after_fire,
            COALESCE(loss.strong_field_candidate_loss_due_to_causal_spend, 0) AS strong_field_candidate_loss_due_to_causal_spend,
            COALESCE(loss.strong_field_candidate_loss_due_to_absence_lock, 0) AS strong_field_candidate_loss_due_to_absence_lock,
            COALESCE(component_fire.generator_fires_per_culture_component_per_500_ticks, 0) AS generator_fires_per_culture_component_per_500_ticks,
            COALESCE(mass_250.connected_raw_mass_250 - fire.fire_connected_raw_mass, 0.0) AS connected_mass_delta_250_ticks_after_fire
        FROM fire_planes fire
        LEFT JOIN next_target_status next_target
          ON next_target.run_id = fire.run_id
         AND next_target.fire_tick = fire.fire_tick
         AND next_target.generator_id = fire.generator_id
         AND next_target.target_entity_id = fire.target_entity_id
         AND next_target.plane_code = fire.plane_code
        LEFT JOIN target_reeligible
          ON target_reeligible.run_id = fire.run_id
         AND target_reeligible.fire_tick = fire.fire_tick
         AND target_reeligible.generator_id = fire.generator_id
         AND target_reeligible.target_entity_id = fire.target_entity_id
         AND target_reeligible.plane_code = fire.plane_code
        LEFT JOIN same_component_after same_component
          ON same_component.run_id = fire.run_id
         AND same_component.fire_tick = fire.fire_tick
         AND same_component.generator_id = fire.generator_id
         AND same_component.target_entity_id = fire.target_entity_id
         AND same_component.plane_code = fire.plane_code
        LEFT JOIN loss
          ON loss.run_id = fire.run_id
         AND loss.fire_tick = fire.fire_tick
         AND loss.generator_id = fire.generator_id
         AND loss.target_entity_id = fire.target_entity_id
         AND loss.plane_code = fire.plane_code
        LEFT JOIN mass_250
          ON mass_250.run_id = fire.run_id
         AND mass_250.fire_tick = fire.fire_tick
         AND mass_250.generator_id = fire.generator_id
         AND mass_250.target_entity_id = fire.target_entity_id
         AND mass_250.plane_code = fire.plane_code
        LEFT JOIN component_fire_500 component_fire
          ON component_fire.run_id = fire.run_id
         AND component_fire.fire_tick = fire.fire_tick
         AND component_fire.generator_id = fire.generator_id
         AND component_fire.target_entity_id = fire.target_entity_id
         AND component_fire.plane_code = fire.plane_code
        """;

    private static readonly string GeneratorComponentFireWindowsSql = $$"""
        WITH target_at_fire AS (
            SELECT *
            FROM (
                SELECT
                    target.*,
                    ROW_NUMBER() OVER (
                        PARTITION BY run_id, generator_id, tick, target_entity_id
                        ORDER BY CASE WHEN status_code = {{GeneratorStatusCodes.Eligible}} THEN 0 ELSE 1 END
                    ) AS target_rank
                FROM raw_generator_target_evaluations target
            ) ranked
            WHERE target_rank = 1
        ),
        fire_planes AS (
            SELECT
                fire.run_id,
                fire.tick AS fire_tick,
                fire.generator_id,
                COALESCE(target.target_culture_id, 0) AS target_culture_id,
                {{PlaneCodes.Physical}} AS plane_code,
                COALESCE(target.physical_component_id, 0) AS component_id,
                COALESCE(target.physical_component_cell_count, 0) AS component_cell_count,
                COALESCE(target.physical_component_raw_mass, 0.0) AS component_raw_mass,
                COALESCE(target.physical_field_band_code, 0) AS field_band_code,
                COALESCE(target.physical_connected_raw_mass, 0.0) AS connected_raw_mass
            FROM raw_generator_events fire
            LEFT JOIN target_at_fire target
              ON target.run_id = fire.run_id
             AND target.generator_id = fire.generator_id
             AND target.tick = fire.tick
             AND target.target_entity_id = fire.selected_target_entity_id
            WHERE fire.event_type_code = {{(int)KernelEventType.GeneratorFired}}
              AND fire.selected_target_entity_id IS NOT NULL
            UNION ALL
            SELECT
                fire.run_id,
                fire.tick AS fire_tick,
                fire.generator_id,
                COALESCE(target.target_culture_id, 0) AS target_culture_id,
                {{PlaneCodes.Semantic}} AS plane_code,
                COALESCE(target.semantic_component_id, 0) AS component_id,
                COALESCE(target.semantic_component_cell_count, 0) AS component_cell_count,
                COALESCE(target.semantic_component_raw_mass, 0.0) AS component_raw_mass,
                COALESCE(target.semantic_field_band_code, 0) AS field_band_code,
                COALESCE(target.semantic_connected_raw_mass, 0.0) AS connected_raw_mass
            FROM raw_generator_events fire
            LEFT JOIN target_at_fire target
              ON target.run_id = fire.run_id
             AND target.generator_id = fire.generator_id
             AND target.tick = fire.tick
             AND target.target_entity_id = fire.selected_target_entity_id
            WHERE fire.event_type_code = {{(int)KernelEventType.GeneratorFired}}
              AND fire.selected_target_entity_id IS NOT NULL
        )
        SELECT
            run_id,
            generator_id,
            target_culture_id,
            plane_code,
            component_id,
            CAST(FLOOR(fire_tick / 500.0) * 500 AS INTEGER) AS window_start_tick,
            CAST(FLOOR(fire_tick / 500.0) * 500 + 499 AS INTEGER) AS window_end_tick,
            COUNT(*) AS generator_fires_per_culture_component_per_500_ticks,
            SUM(CASE WHEN field_band_code = {{CultureFieldStrengthBandCodes.Strong}} THEN 1 ELSE 0 END) AS strong_field_fire_count,
            COALESCE(AVG(CAST(component_cell_count AS DOUBLE)), 0.0) AS average_component_cell_count,
            COALESCE(AVG(CAST(component_raw_mass AS DOUBLE)), 0.0) AS average_component_raw_mass,
            COALESCE(AVG(CAST(connected_raw_mass AS DOUBLE)), 0.0) AS average_connected_raw_mass
        FROM fire_planes
        WHERE component_id <> 0
        GROUP BY
            run_id,
            generator_id,
            target_culture_id,
            plane_code,
            component_id,
            CAST(FLOOR(fire_tick / 500.0) * 500 AS INTEGER),
            CAST(FLOOR(fire_tick / 500.0) * 500 + 499 AS INTEGER)
        """;
}
