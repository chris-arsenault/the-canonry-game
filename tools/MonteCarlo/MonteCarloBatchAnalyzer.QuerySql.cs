namespace MonteCarloTool;

internal sealed partial class MonteCarloBatchAnalyzer
{
    private const string GeneratorRowsSql = """
        WITH evaluations AS (
            SELECT
                generator_id,
                COUNT(*) AS evaluations,
                SUM(CASE WHEN status IN ('eligible', 'eligible_not_selected', 'fired', 'fire_failed') THEN 1 ELSE 0 END) AS eligible_evaluations,
                SUM(CASE WHEN status = 'blocked' THEN 1 ELSE 0 END) AS blocked_evaluations,
                SUM(CASE WHEN status = 'eligible_not_selected' THEN 1 ELSE 0 END) AS eligible_not_selected_evaluations,
                SUM(CASE WHEN status = 'fire_failed' THEN 1 ELSE 0 END) AS fire_failed_evaluations,
                COALESCE(AVG(CAST(target_pool_count AS DOUBLE)), 0.0) AS average_target_pool,
                COALESCE(AVG(CAST(candidate_count AS DOUBLE)), 0.0) AS average_candidate_count,
                COALESCE(SUM(inactive_rejected_count), 0) AS inactive_rejected,
                COALESCE(SUM(subtype_rejected_count), 0) AS subtype_rejected,
                COALESCE(SUM(secondary_target_rejected_count), 0) AS secondary_target_rejected,
                COALESCE(SUM(trigger_rejected_count), 0) AS trigger_rejected
            FROM generator_events
            WHERE event_type = 'evaluated'
            GROUP BY generator_id
        ),
        fired AS (
            SELECT
                generator_id,
                COUNT(*) AS fired_count,
                COALESCE(SUM(created_entity_count), 0) AS created_entity_count,
                COALESCE(SUM(created_relationship_count), 0) AS created_relationship_count
            FROM generator_events
            WHERE event_type = 'fired'
            GROUP BY generator_id
        )
        SELECT
            COALESCE(e.generator_id, f.generator_id) AS generator_id,
            COALESCE(e.evaluations, 0) AS evaluations,
            COALESCE(e.eligible_evaluations, 0) AS eligible_evaluations,
            COALESCE(e.blocked_evaluations, 0) AS blocked_evaluations,
            COALESCE(e.eligible_not_selected_evaluations, 0) AS eligible_not_selected_evaluations,
            COALESCE(f.fired_count, 0) AS fired_count,
            COALESCE(e.fire_failed_evaluations, 0) AS fire_failed_evaluations,
            COALESCE(e.average_target_pool, 0.0) AS average_target_pool,
            COALESCE(e.average_candidate_count, 0.0) AS average_candidate_count,
            COALESCE(e.inactive_rejected, 0) AS inactive_rejected,
            COALESCE(e.subtype_rejected, 0) AS subtype_rejected,
            COALESCE(e.secondary_target_rejected, 0) AS secondary_target_rejected,
            COALESCE(e.trigger_rejected, 0) AS trigger_rejected,
            COALESCE(f.created_entity_count, 0) AS created_entity_count,
            COALESCE(f.created_relationship_count, 0) AS created_relationship_count
        FROM evaluations e
        FULL OUTER JOIN fired f
          ON e.generator_id = f.generator_id;
        """;

}
