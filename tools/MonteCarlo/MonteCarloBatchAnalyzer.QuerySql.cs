namespace MonteCarloTool;

internal sealed partial class MonteCarloBatchAnalyzer
{
    private const string GeneratorRowsSql = """
        WITH pulse AS (
            SELECT
                generator_id,
                COUNT(*) AS pulses,
                SUM(CASE WHEN status IN ('eligible', 'eligible_not_selected', 'fired', 'fire_failed') THEN 1 ELSE 0 END) AS eligible_pulses,
                SUM(CASE WHEN status = 'blocked' THEN 1 ELSE 0 END) AS blocked_pulses,
                SUM(CASE WHEN status = 'eligible_not_selected' THEN 1 ELSE 0 END) AS eligible_not_selected_pulses,
                SUM(CASE WHEN status = 'fire_failed' THEN 1 ELSE 0 END) AS fire_failed_pulses,
                COALESCE(AVG(CAST(target_pool_count AS DOUBLE)), 0.0) AS average_target_pool,
                COALESCE(AVG(CAST(candidate_count AS DOUBLE)), 0.0) AS average_candidate_count,
                COALESCE(SUM(inactive_rejected_count), 0) AS inactive_rejected,
                COALESCE(SUM(subtype_rejected_count), 0) AS subtype_rejected,
                COALESCE(SUM(saturation_rejected_count), 0) AS saturation_rejected,
                COALESCE(SUM(secondary_target_rejected_count), 0) AS secondary_target_rejected,
                COALESCE(SUM(wilderness_rejected_count), 0) AS wilderness_rejected,
                COALESCE(SUM(trigger_rejected_count), 0) AS trigger_rejected
            FROM generator_events
            WHERE event_type = 'pulse'
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
            COALESCE(p.generator_id, f.generator_id) AS generator_id,
            COALESCE(p.pulses, 0) AS pulses,
            COALESCE(p.eligible_pulses, 0) AS eligible_pulses,
            COALESCE(p.blocked_pulses, 0) AS blocked_pulses,
            COALESCE(p.eligible_not_selected_pulses, 0) AS eligible_not_selected_pulses,
            COALESCE(f.fired_count, 0) AS fired_count,
            COALESCE(p.fire_failed_pulses, 0) AS fire_failed_pulses,
            COALESCE(p.average_target_pool, 0.0) AS average_target_pool,
            COALESCE(p.average_candidate_count, 0.0) AS average_candidate_count,
            COALESCE(p.inactive_rejected, 0) AS inactive_rejected,
            COALESCE(p.subtype_rejected, 0) AS subtype_rejected,
            COALESCE(p.saturation_rejected, 0) AS saturation_rejected,
            COALESCE(p.secondary_target_rejected, 0) AS secondary_target_rejected,
            COALESCE(p.wilderness_rejected, 0) AS wilderness_rejected,
            COALESCE(p.trigger_rejected, 0) AS trigger_rejected,
            COALESCE(f.created_entity_count, 0) AS created_entity_count,
            COALESCE(f.created_relationship_count, 0) AS created_relationship_count
        FROM pulse p
        FULL OUTER JOIN fired f
          ON p.generator_id = f.generator_id;
        """;

}
