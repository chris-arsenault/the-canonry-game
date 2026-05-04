namespace MonteCarloTool;

using Engine.Constants;

internal sealed partial class MonteCarloBatchAnalyzer
{
    private static readonly string ActionUtilizationSql = $$"""
        WITH entity_kind AS (
            SELECT run_id, entity_id, any_value(kind_id) AS kind_id
            FROM raw_entity_snapshots
            GROUP BY run_id, entity_id
        )
        SELECT
            COALESCE(entity_kind.kind_id, 0) AS kind_id,
            trace.action_id,
            SUM(CASE WHEN trace.status_code = {{TraceStatusCodes.Offered}} THEN 1 ELSE 0 END) AS candidate_evaluations,
            SUM(CASE WHEN trace.status_code = {{TraceStatusCodes.Selected}} THEN 1 ELSE 0 END) AS selected_count,
            CASE
                WHEN SUM(CASE WHEN trace.status_code = {{TraceStatusCodes.Offered}} THEN 1 ELSE 0 END) = 0 THEN 0.0
                ELSE SUM(CASE WHEN trace.status_code = {{TraceStatusCodes.Selected}} THEN 1 ELSE 0 END)::DOUBLE /
                     SUM(CASE WHEN trace.status_code = {{TraceStatusCodes.Offered}} THEN 1 ELSE 0 END)
            END AS selection_rate_when_offered,
            COALESCE(AVG(CASE WHEN trace.status_code = {{TraceStatusCodes.Selected}} THEN CAST(trace.adjusted_weight AS DOUBLE) ELSE NULL END), 0.0) AS average_score,
            0.0 AS precondition_pass_rate,
            0.0 AS cooldown_rate,
            0 AS top_failure_diagnostic_code,
            2 AS evidence_source_code
        FROM raw_trace_events trace
        LEFT JOIN entity_kind
          ON entity_kind.run_id = trace.run_id
         AND entity_kind.entity_id = trace.entity_id
        WHERE trace.action_family_code = {{ActionFamilyCodes.LowAltitudeVerb}}
          AND trace.trace_subject_code IN ({{TraceSubjectCodes.MovementIntent}}, {{TraceSubjectCodes.InfluenceIntent}}, {{TraceSubjectCodes.RelationshipIntent}})
        GROUP BY kind_id, trace.action_id
        """;

    private const string StockDynamicsSql = """
        WITH ordered AS (
            SELECT *,
                   LAG(value) OVER (PARTITION BY run_id, entity_id, attribute_id ORDER BY tick) AS previous_value
            FROM raw_entity_attribute_snapshots
        ),
        deltas AS (
            SELECT *,
                   value - COALESCE(previous_value, value) AS delta,
                   LAG(value - COALESCE(previous_value, value)) OVER (
                       PARTITION BY run_id, entity_id, attribute_id ORDER BY tick) AS previous_delta
            FROM ordered
        ),
        entity_kind AS (
            SELECT run_id, entity_id, any_value(kind_id) AS kind_id
            FROM raw_entity_snapshots
            GROUP BY run_id, entity_id
        )
        SELECT
            samples.run_id,
            samples.entity_id,
            COALESCE(MAX(entity_kind.kind_id), 0) AS kind_id,
            samples.attribute_id,
            COUNT(*) AS sample_count,
            MIN(value) AS minimum_observed,
            MAX(value) AS maximum_observed,
            COALESCE(VAR_POP(value), 0.0) AS variance,
            SUM(CASE WHEN previous_delta * delta < 0 THEN 1 ELSE 0 END) AS reversal_count,
            SUM(CASE WHEN value < 33.333 THEN 1 ELSE 0 END) AS dwell_low_ticks,
            SUM(CASE WHEN value >= 33.333 AND value < 66.666 THEN 1 ELSE 0 END) AS dwell_mid_ticks,
            SUM(CASE WHEN value >= 66.666 THEN 1 ELSE 0 END) AS dwell_high_ticks,
            SUM(CASE WHEN value <= 1.0 THEN 1 ELSE 0 END) AS pinned_low_ticks,
            SUM(CASE WHEN value >= 99.0 THEN 1 ELSE 0 END) AS pinned_high_ticks,
            LEAST(1.0, GREATEST(0.0, (MAX(value) - MIN(value)) / 100.0)) AS normalized_range
        FROM deltas samples
        LEFT JOIN entity_kind
          ON entity_kind.run_id = samples.run_id
         AND entity_kind.entity_id = samples.entity_id
        GROUP BY samples.run_id, samples.entity_id, samples.attribute_id
        HAVING COUNT(*) >= 2
        """;
}
