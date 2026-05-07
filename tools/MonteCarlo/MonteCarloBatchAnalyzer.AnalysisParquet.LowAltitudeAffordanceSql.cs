namespace MonteCarloTool;

using Engine.Constants;

internal sealed partial class MonteCarloBatchAnalyzer
{
    private static readonly string LowAltitudeAffordanceUtilizationSql = $$"""
        WITH low_altitude AS (
            SELECT
                trace.run_id,
                trace.entity_id,
                trace.affordance_source_kind_code AS source_kind_code,
                trace.affordance_source_id AS source_id,
                trace.affordance_shape_code AS shape_code,
                trace.affordance_source_kind_mask,
                trace.affordance_shape_mask,
                trace.affordance_contribution_count,
                trace.generator_affordance_count,
                trace.gambit_affordance_count,
                trace.hidden_affordance_count,
                trace.action_id AS verb_id,
                trace.parent_candidate_index AS operation_code,
                trace.effect_scope_code AS plane_code,
                trace.effect_value_id AS target_mode_code,
                trace.effect_kind_id AS target_kind_id,
                trace.status_code,
                trace.affordance_score,
                trace.affordance_bonus
            FROM raw_trace_events trace
            WHERE trace.action_family_code = {{ActionFamilyCodes.LowAltitudeVerb}}
              AND trace.trace_subject_code IN ({{TraceSubjectCodes.MovementIntent}}, {{TraceSubjectCodes.InfluenceIntent}}, {{TraceSubjectCodes.RelationshipIntent}})
              AND trace.affordance_score > 0
        ),
        grouped AS (
            SELECT
                source_kind_code,
                source_id,
                shape_code,
                verb_id,
                operation_code,
                plane_code,
                target_mode_code,
                target_kind_id,
                SUM(CASE WHEN status_code = {{TraceStatusCodes.Offered}} THEN 1 ELSE 0 END) AS offered_count,
                SUM(CASE WHEN status_code = {{TraceStatusCodes.Selected}} THEN 1 ELSE 0 END) AS selected_count,
                SUM(affordance_contribution_count) AS contribution_count,
                SUM(hidden_affordance_count) AS hidden_contribution_count,
                SUM(generator_affordance_count) AS generator_contribution_count,
                SUM(gambit_affordance_count) AS gambit_contribution_count,
                SUM(CASE WHEN (affordance_source_kind_mask & 2) != 0 THEN 1 ELSE 0 END) AS rows_with_gambit_contribution,
                SUM(CASE WHEN (affordance_shape_mask & 1) != 0 THEN 1 ELSE 0 END) AS rows_with_presence_contribution,
                SUM(CASE WHEN (affordance_shape_mask & 2) != 0 THEN 1 ELSE 0 END) AS rows_with_relationship_strength_contribution,
                SUM(CASE WHEN (affordance_shape_mask & 4) != 0 THEN 1 ELSE 0 END) AS rows_with_influence_contribution,
                COALESCE(AVG(CASE WHEN status_code = {{TraceStatusCodes.Offered}} THEN affordance_score ELSE NULL END), 0.0) AS average_offered_score,
                COALESCE(AVG(CASE WHEN status_code = {{TraceStatusCodes.Selected}} THEN affordance_score ELSE NULL END), 0.0) AS average_selected_score,
                COALESCE(AVG(CASE WHEN status_code = {{TraceStatusCodes.Offered}} THEN affordance_bonus ELSE NULL END), 0.0) AS average_offered_bonus,
                COALESCE(AVG(CASE WHEN status_code = {{TraceStatusCodes.Selected}} THEN affordance_bonus ELSE NULL END), 0.0) AS average_selected_bonus,
                COUNT(DISTINCT entity_id) AS distinct_entities
            FROM low_altitude
            GROUP BY source_kind_code, source_id, shape_code, verb_id, operation_code, plane_code, target_mode_code, target_kind_id
        )
        SELECT
            source_kind_code,
            source_id,
            shape_code,
            verb_id,
            operation_code,
            plane_code,
            target_mode_code,
            target_kind_id,
            offered_count,
            selected_count,
            contribution_count,
            hidden_contribution_count,
            generator_contribution_count,
            gambit_contribution_count,
            rows_with_gambit_contribution,
            rows_with_presence_contribution,
            rows_with_relationship_strength_contribution,
            rows_with_influence_contribution,
            CASE WHEN offered_count = 0 THEN 0.0 ELSE selected_count::DOUBLE / offered_count END AS selection_rate_when_offered,
            average_offered_score,
            average_selected_score,
            average_offered_bonus,
            average_selected_bonus,
            distinct_entities
        FROM grouped
        """;
}
