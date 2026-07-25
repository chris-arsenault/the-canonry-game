namespace MonteCarlo.Tests.Observability;

using DuckDB.NET.Data;

using Engine.Constants;

using MonteCarloTool;

#pragma warning disable CA1515, CA2100 // xUnit requires public test types; SQL strings use constants and fixture-owned paths.

// Executable output-contract ledger for Monte Carlo observability.
//
// These tests intentionally stay at the Monte Carlo output boundary. They do not inspect kernel
// internals. Their purpose is to pin the gap analysis to a concrete parquet contract so a later
// implementation cannot drift by renaming tables, reintroducing CSVs, zero-filling analysis, keeping
// parallel legacy outputs, or emitting unjoinable facts. If this class is green without shortcuts,
// the documented Monte Carlo observability gaps are closed at the output-contract level.
[Collection("Monte Carlo output gap acceptance")]
public sealed class MonteCarloOutputGapTests(MonteCarloOutputGapFixture fixture)
    : IClassFixture<MonteCarloOutputGapFixture>
{
    [MonteCarloAcceptanceTheory]
    [MemberData(nameof(RawDomainContracts))]
    public void RawOutput_DocumentedDomainTableMustSatisfyAcceptanceContract(string tableName, string columns, string reason)
    {
        ValidateContract(tableName, columns, reason);

        // One row in RawDomainContracts is one documented output gap. The table must exist, carry
        // the declared facts, be production-populated, and obey the primitive-id/no-string rule.
        Assert.True(fixture.RawParquetExists(tableName), $"{tableName}: missing canonical raw parquet table. {reason}");
        var actual = fixture.RawColumns(tableName);
        foreach (var column in SplitColumns(columns))
        {
            Assert.Contains(column, actual, StringComparer.OrdinalIgnoreCase);
        }
        Assert.True(fixture.RawRowCount(tableName) > 0, $"{tableName}: table exists but has no production rows. {reason}");
        Assert.DoesNotContain(
            fixture.RawColumnTypes(tableName),
            column => IsStringType(column.Type));
    }

    [MonteCarloAcceptanceTheory]
    [MemberData(nameof(AnalysisContracts))]
    public void AnalysisOutput_DocumentedAnalysisTableMustSatisfyAcceptanceContract(string tableName, string columns, string reason)
    {
        ValidateContract(tableName, columns, reason);

        // One row in AnalysisContracts is one documented analysis output. It must be parquet-backed,
        // populated, carry the declared tuning fields, and keep structured rows primitive-id based.
        Assert.True(fixture.AnalysisParquetExists(tableName), $"{tableName}: missing analysis parquet table. {reason}");
        var actual = fixture.AnalysisColumns(tableName);
        foreach (var column in SplitColumns(columns))
        {
            Assert.Contains(column, actual, StringComparer.OrdinalIgnoreCase);
        }
        Assert.True(fixture.AnalysisRowCount(tableName) > 0, $"{tableName}: analysis table has no rows. {reason}");
        Assert.DoesNotContain(
            fixture.AnalysisColumnTypes(tableName),
            column => IsStringType(column.Type));
    }

    [MonteCarloAcceptanceTheory]
    [MemberData(nameof(LegacyParallelOutputs))]
    public void RawOutput_LegacyParallelTablesMustNotBeExportedAsParquet(string tableName, string replacement)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(replacement);

        // Compatibility can be a view, but the output directory must not contain parallel parquet
        // systems for the same domain. This is the architecture gap that caused repeated confusion.
        Assert.False(
            fixture.RawParquetExists(tableName),
            $"{tableName}: legacy parallel parquet should be removed; use canonical output '{replacement}'.");
    }

    [MonteCarloAcceptanceTheory]
    [MemberData(nameof(LegacyCsvArtifacts))]
    public void AnalysisOutput_LegacyCsvArtifactsMustNotBeWritten(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        // CSVs were explicitly excluded from the target analysis contract. Leaving them around makes
        // it too easy to keep patching stale reports instead of the parquet analysis layer.
        Assert.False(fixture.AnalysisFileExists(fileName), $"{fileName}: structured analysis output must be parquet-only.");
    }

    [MonteCarloAcceptanceFact]
    public void GeneratorOutput_FiredCreatedCountsMustBeExplainedByGeneratorOutputs()
    {
        // This replaces same-tick inference. Every created entity/relationship/map mutation caused
        // by a generator fire must have an explicit generator_outputs attribution row.
        fixture.AssertRawTablesExist("generator_events", "generator_outputs");

        Assert.Equal(0, fixture.RawQueryLong(
            ["generator_events", "generator_outputs"],
            """
            WITH fired AS (
                SELECT
                    run_id,
                    cause_id,
                    created_entity_count + created_relationship_count + created_map_cell_count AS expected_outputs
                FROM generator_events
                WHERE created_entity_count + created_relationship_count + created_map_cell_count > 0
            ),
            actual AS (
                SELECT run_id, cause_id, COUNT(*) AS actual_outputs
                FROM generator_outputs
                GROUP BY run_id, cause_id
            )
            SELECT COUNT(*)
            FROM fired f
            LEFT JOIN actual a
              ON a.run_id = f.run_id
             AND a.cause_id = f.cause_id
            WHERE COALESCE(a.actual_outputs, 0) <> f.expected_outputs;
            """));
    }

    [MonteCarloAcceptanceFact]
    public void GeneratorOutput_EntityOutputsMustJoinEntityDomainRows()
    {
        // Generator observability should reference entity-domain facts, not duplicate entity state.
        fixture.AssertRawTablesExist("generator_outputs", "entity_events", "entity_snapshots");

        Assert.Equal(0, fixture.RawQueryLong(
            ["generator_outputs", "entity_events", "entity_snapshots"],
            """
            SELECT COUNT(*)
            FROM generator_outputs output
            LEFT JOIN entity_events evt
              ON evt.run_id = output.run_id
             AND evt.entity_id = output.output_entity_id
            LEFT JOIN entity_snapshots snap
              ON snap.run_id = output.run_id
             AND snap.entity_id = output.output_entity_id
            WHERE output.output_entity_id <> 0
              AND evt.entity_id IS NULL
              AND snap.entity_id IS NULL;
            """));
    }

    [MonteCarloAcceptanceFact]
    public void MapCoverageOutput_CoverageTerrainSnapshotsMustSumToCoverageSnapshots()
    {
        // Owner-level coverage and owner-by-terrain coverage are the same map/coverage domain.
        // If they disagree, coverage development analysis cannot be trusted.
        fixture.AssertRawTablesExist("coverage_snapshots", "coverage_terrain_snapshots");

        Assert.Equal(0, fixture.RawQueryLong(
            ["coverage_snapshots", "coverage_terrain_snapshots"],
            """
            WITH terrain_sum AS (
                SELECT run_id, tick, owner_entity_id, SUM(cell_count) AS cell_count
                FROM coverage_terrain_snapshots
                GROUP BY run_id, tick, owner_entity_id
            )
            SELECT COUNT(*)
            FROM coverage_snapshots coverage
            LEFT JOIN terrain_sum terrain
              ON terrain.run_id = coverage.run_id
             AND terrain.tick = coverage.tick
             AND terrain.owner_entity_id = coverage.owner_entity_id
            WHERE COALESCE(terrain.cell_count, -1) <> coverage.cell_count;
            """));
    }

    [MonteCarloAcceptanceFact]
    public void MapCoverageOutput_MapSnapshotsMustAgreeWithTickSnapshotCellTotals()
    {
        // Terrain aggregate snapshots and tick coverage counts must agree on total map size.
        fixture.AssertRawTablesExist("map_snapshots", "tick_snapshots");

        Assert.Equal(0, fixture.RawQueryLong(
            ["map_snapshots", "tick_snapshots"],
            """
            WITH map_total AS (
                SELECT run_id, tick, SUM(cell_count) AS total_cells
                FROM map_snapshots
                GROUP BY run_id, tick
            )
            SELECT COUNT(*)
            FROM tick_snapshots tick
            JOIN map_total map
              ON map.run_id = tick.run_id
             AND map.tick = tick.tick
            WHERE map.total_cells <> tick.covered_cells + tick.uncovered_cells;
            """));
    }

    [MonteCarloAcceptanceFact]
    public void PlacementOutput_SelectedPlacementsMustHaveGeneratorOrMapCause()
    {
        // Placement events are useful only if they explain a spatial decision made by a generator or
        // map system. Selected placements with no cause are just another unjoinable table.
        fixture.AssertRawTablesExist("placement_events");

        Assert.Equal(0, fixture.RawQueryLong(
            ["placement_events"],
            """
            SELECT COUNT(*)
            FROM placement_events
            WHERE selected
              AND cause_id = 0
              AND anchor_entity_id = 0;
            """));
    }

    [MonteCarloAcceptanceFact]
    public void EntityOutput_AttributeSnapshotsMustSupportPerEntityStockDynamics()
    {
        // Aggregate attribute bands are not enough. Individual entity/attribute samples over time
        // are the raw material for variance, reversals, dwell, and pinning.
        fixture.AssertRawTablesExist("entity_attribute_snapshots");

        Assert.True(fixture.RawQueryLong(
            ["entity_attribute_snapshots"],
            """
            SELECT COUNT(*)
            FROM (
                SELECT run_id, entity_id, attribute_id
                FROM entity_attribute_snapshots
                GROUP BY run_id, entity_id, attribute_id
                HAVING COUNT(DISTINCT tick) >= 2
            ) samples;
            """) > 0);
    }

    [MonteCarloAcceptanceFact]
    public void EntityOutput_KindHealthSummariesMustNotBeZeroFilledPlaceholders()
    {
        // The completion plan calls out the current zero health summary problem directly. Health
        // summaries must be computed from configured health attributes.
        fixture.AssertRawTablesExist("kind_snapshots");

        Assert.True(fixture.RawAny("kind_snapshots", "average_health <> 0 OR minimum_health <> 0 OR maximum_health <> 0"));
        Assert.Equal(0, fixture.RawQueryLong(
            ["kind_snapshots"],
            """
            SELECT COUNT(*)
            FROM kind_snapshots
            WHERE minimum_health > average_health
               OR average_health > maximum_health;
            """));
    }

    [MonteCarloAcceptanceFact]
    public void LifecycleOutput_HistoricalTransitionsMustHaveLifecycleReasons()
    {
        // Lifecycle events tell us what happened. Lifecycle reasons close the tuning gap by making
        // deaths and historical transitions attributable at the output layer.
        fixture.AssertRawTablesExist("lifecycle_events", "lifecycle_reason_events");

        Assert.Equal(0, fixture.RawQueryLong(
            ["lifecycle_events", "lifecycle_reason_events"],
            """
            SELECT COUNT(*)
            FROM lifecycle_events lifecycle
            LEFT JOIN lifecycle_reason_events reason
              ON reason.run_id = lifecycle.run_id
             AND reason.tick = lifecycle.tick
             AND reason.entity_id = lifecycle.entity_id
            WHERE lifecycle.status_code <> 0
              AND reason.reason_code_id IS NULL;
            """));
    }

    [MonteCarloAcceptanceFact]
    public void SnapshotOutput_RunMetadataMustRecordSnapshotCadencesAndVariantIdentity()
    {
        // Cadence metadata is part of interpreting sparse snapshot rows. Without it, missing rows
        // cannot be distinguished from disabled providers or lower-frequency sampling.
        fixture.AssertRawTablesExist("experiment_variants");

        var columns = fixture.RawColumns("experiment_variants");
        foreach (var column in new[]
                 {
                     "run_id",
                     "experiment_id",
                     "variant_id",
                     "config_hash_id",
                     "trace_enabled",
                     "entity_snapshot_cadence",
                     "map_snapshot_cadence",
                     "generator_snapshot_cadence",
                 })
        {
            Assert.Contains(column, columns, StringComparer.OrdinalIgnoreCase);
        }
    }

    [MonteCarloAcceptanceFact]
    public void AnalysisOutput_ReadinessMustCoverEverySemanticFeatureAndReportNoHiddenGaps()
    {
        // analysis_readiness is the guard against claiming completion when a source table is empty,
        // synthetic-only, or zero-filled. Green means every feature in the ledger is explicitly ready.
        fixture.AssertAnalysisTablesExist("analysis_readiness");

        Assert.True(fixture.AnalysisQueryLong(
            ["analysis_readiness"],
            "SELECT COUNT(DISTINCT feature_code) FROM analysis_readiness;") >= 25);
        Assert.Equal(0, fixture.AnalysisQueryLong(
            ["analysis_readiness"],
            """
            SELECT COUNT(*)
            FROM analysis_readiness
            WHERE ready = FALSE
               OR trustworthy = FALSE
               OR source_row_count = 0;
            """));
    }

    [MonteCarloAcceptanceFact]
    public void AnalysisOutput_ActionUtilizationMustBeDerivedFromTraceEvents()
    {
        // Action utilization should follow the current primitive low-altitude trace stream, not the
        // deleted planner candidate tables or execution-count-only fallbacks.
        fixture.AssertRawTablesExist("trace_events");
        fixture.AssertAnalysisTablesExist("action_utilization");

        Assert.Equal(
            fixture.RawQueryLong(
                ["trace_events"],
                $"""
                SELECT COUNT(*)
                FROM trace_events
                WHERE action_family_code = {ActionFamilyCodes.LowAltitudeVerb}
                  AND trace_subject_code IN (
                      {TraceSubjectCodes.MovementIntent},
                      {TraceSubjectCodes.InfluenceIntent},
                      {TraceSubjectCodes.RelationshipIntent})
                  AND status_code = {TraceStatusCodes.Offered};
                """),
            fixture.AnalysisQueryLong(["action_utilization"], "SELECT COALESCE(SUM(candidate_evaluations), 0) FROM action_utilization;"));
        Assert.True(fixture.AnalysisQueryLong(["action_utilization"], "SELECT COUNT(*) FROM action_utilization WHERE evidence_source_code <> 0;") > 0);
    }

    [MonteCarloAcceptanceFact]
    public void AnalysisOutput_StockDynamicsMustBeDerivedFromEntityAttributeSnapshots()
    {
        // Stock dynamics should answer the actual tuning question: are individual stocks fluctuating
        // or pinned? That requires sample counts and per-entity range/variance, not aggregate bands.
        fixture.AssertRawTablesExist("entity_attribute_snapshots");
        fixture.AssertAnalysisTablesExist("stock_dynamics");

        Assert.Equal(0, fixture.AnalysisQueryLong(
            ["stock_dynamics"],
            """
            SELECT COUNT(*)
            FROM stock_dynamics
            WHERE sample_count < 2
               OR normalized_range < 0
               OR normalized_range > 1
               OR variance < 0;
            """));
        Assert.True(fixture.AnalysisQueryLong(
            ["stock_dynamics"],
            "SELECT COUNT(*) FROM stock_dynamics WHERE reversal_count > 0 OR normalized_range > 0;") > 0);
    }

    [MonteCarloAcceptanceFact]
    public void AnalysisOutput_MapAndCoverageReportsMustBeDerivedFromMapCoverageSources()
    {
        // Map spread and coverage development are separate analysis products over the same
        // map/coverage family. Both must be backed by the canonical raw sources.
        fixture.AssertRawTablesExist("map_snapshots", "coverage_snapshots", "coverage_terrain_snapshots");
        fixture.AssertAnalysisTablesExist("map_spread", "coverage_development", "terrain_occupancy");

        Assert.True(fixture.AnalysisQueryLong(["map_spread"], "SELECT COUNT(*) FROM map_spread WHERE plane_code IN (1, 2) AND total_cell_count > 0;") > 0);
        Assert.True(fixture.AnalysisQueryLong(["coverage_development"], "SELECT COUNT(*) FROM coverage_development WHERE plane_code IN (1, 2) AND final_cell_count >= 0;") > 0);
        Assert.True(fixture.AnalysisQueryLong(["terrain_occupancy"], "SELECT COUNT(*) FROM terrain_occupancy WHERE plane_code IN (1, 2) AND terrain_id <> 0;") > 0);
    }

    [MonteCarloAcceptanceFact]
    public void SpatialOutput_MapAndPlacementTelemetryMustBePlaneAware()
    {
        fixture.AssertRawTablesExist("map_cell_events", "map_snapshots", "placement_events");

        Assert.True(fixture.DistinctRawValueCount("map_cell_events", "plane_code") >= 2);
        Assert.True(fixture.DistinctRawValueCount("map_snapshots", "plane_code") >= 2);
        Assert.True(fixture.RawQueryLong(
            ["placement_events"],
            """
            SELECT COUNT(*)
            FROM placement_events
            WHERE selected
              AND plane_code = 2;
            """) > 0);
    }

    [MonteCarloAcceptanceFact]
    public void AnalysisOutput_ExperimentComparisonMustJoinExperimentVariants()
    {
        // Cross-variant tuning cannot be done from filenames or ad hoc run directories. The parquet
        // comparison table must be keyed by structured experiment metadata.
        fixture.AssertRawTablesExist("experiment_variants");
        fixture.AssertAnalysisTablesExist("experiment_comparison");

        Assert.Equal(0, fixture.AnalysisQueryLong(
            ["experiment_comparison"],
            """
            SELECT COUNT(*)
            FROM experiment_comparison
            WHERE experiment_id = 0
               OR variant_id = 0
               OR run_count = 0;
            """));
    }

    public static TheoryData<string, string, string> RawDomainContracts()
    {
        var data = new TheoryData<string, string, string>();
        AddRawDomainContracts(data);
        return data;
    }

    public static TheoryData<string, string, string> AnalysisContracts()
    {
        var data = new TheoryData<string, string, string>();
        AddAnalysisContracts(data);
        return data;
    }

    public static TheoryData<string, string> LegacyParallelOutputs()
    {
        var data = new TheoryData<string, string>();
        data.Add("planner_traces", "trace_events");
        data.Add("planner_goals", "trace_events");
        data.Add("planner_candidates", "trace_events");
        data.Add("planner_scores", "trace_events");
        data.Add("planner_steps", "trace_events");
        data.Add("planner_localities", "trace_events");
        data.Add("planner_trace_goals", "trace_events");
        data.Add("planner_trace_scores", "trace_events");
        data.Add("planner_trace_steps", "trace_events");
        data.Add("process_candidates", "trace_events");
        data.Add("action_executions", "trace_events");
        data.Add("generated_entities", "generator_outputs");
        return data;
    }

    public static TheoryData<string> LegacyCsvArtifacts()
    {
        var data = new TheoryData<string>();
        data.Add("generator_balance.csv");
        data.Add("generated_subtype_coverage.csv");
        data.Add("action_utilization.csv");
        data.Add("attribute_bands.csv");
        data.Add("map_spread.csv");
        data.Add("map_generation.csv");
        data.Add("terrain_occupancy.csv");
        data.Add("lifecycle.csv");
        data.Add("flags.csv");
        return data;
    }

    private static void AddRawDomainContracts(TheoryData<string, string, string> data)
    {
        ArgumentNullException.ThrowIfNull(data);

        data.Add("runs", "run_id,seed,domain_id,started_unix_milliseconds", "Run metadata is the root key for every output join.");
        data.Add("experiment_variants", "run_id,experiment_id,variant_id,config_hash_id,git_sha_id,trace_enabled,entity_snapshot_cadence,map_snapshot_cadence,generator_snapshot_cadence", "Variant metadata and snapshot cadences are required for cross-run comparisons.");
        data.Add("tick_snapshots", "run_id,tick,active_entities,historical_entities,total_generator_fires,active_relationships,coverage_count,covered_cells,uncovered_cells,physical_spread,semantic_spread", "Tick snapshots provide coarse run state and coverage denominators.");
        data.Add("kind_snapshots", "run_id,tick,kind_id,subtype_id,active_count,historical_count,average_health,minimum_health,maximum_health", "Kind health summaries must be computed, not zero-filled.");
        data.Add("entity_events", "run_id,tick,event_type_code,source_code_id,entity_id,kind_id,subtype_id,culture_id,previous_status_code,current_status_code,physical_q,physical_r,semantic_q,semantic_r,semantic_s", "Entity events own identity and status changes.");
        data.Add("entity_snapshots", "run_id,tick,entity_id,kind_id,subtype_id,culture_id,status_code,physical_q,physical_r,semantic_q,semantic_r,semantic_s", "Entity snapshots own current entity state.");
        data.Add("entity_attribute_snapshots", "run_id,tick,entity_id,attribute_id,value", "Per-entity attribute samples drive stock dynamics.");
        data.Add("attribute_events", "run_id,tick,entity_id,attribute_id,old_value,new_value,delta,source_code_id", "Attribute events explain stock changes.");
        data.Add("movement_events", "run_id,tick,entity_id,plane_code,source_code_id,from_q,from_r,from_s,to_q,to_r,to_s,distance", "Movement events explain physical and semantic motion.");
        data.Add("trace_events", "run_id,tick,intent_id,entity_id,trace_subject_code,candidate_index,parent_candidate_index,action_family_code,action_id,effect_scope_code,effect_value_id,effect_kind_id,target_entity_id,status_code,affordance_source_kind_code,affordance_source_id,affordance_shape_code,affordance_source_kind_mask,affordance_shape_mask,affordance_contribution_count,generator_affordance_count,gambit_affordance_count,hidden_affordance_count,selection_bucket_code,urgency,reward_or_base_score,raw_weight,adjusted_weight,affordance_score,affordance_bonus", "Raw trace events remain primitive payload, not the semantic analysis contract.");
        data.Add("map_cell_events", "run_id,tick,event_type_code,source_code_id,plane_code,cause_id,q,r,previous_terrain_id,current_terrain_id,previous_owner_entity_id,current_owner_entity_id", "Cell mutation history owns plane-aware terrain and coverage changes.");
        data.Add("cell_influence_events", "run_id,tick,source_code_id,action_id,entity_id,plane_code,q,r,previous_influence,current_influence,delta", "Cell influence events own plane-aware influence deltas for the low-altitude map layer.");
        data.Add("culture_field_cell_snapshots", "run_id,tick,culture_id,plane_code,q,r,share,raw_influence,total_raw_influence,z", "Sampled culture field cells provide plane-aware culture-map analysis without a hot-path scalar event stream.");
        data.Add("culture_slot_occupancy_events", "run_id,tick,source_code_id,culture_id,slot_id,polarity_code,previous_entity_id,current_entity_id,previous_score,current_score", "Culture slot occupancy events expose the slot hierarchy feedback loop for health and influence tuning.");
        data.Add("minor_actor_verb_events", "run_id,tick,actor_id,source_entity_id,source_rule_id,source_work_tier,effective_radius,culture_id,verb_id,purpose_code,action_code,targeting_code,plane_code,q,r,status_code,cell_kind_code,local_raw_mass,connected_raw_mass,score,delta", "Minor actor verb events explain low-salience cohort labor choices without promoting cohorts to graph entities.");
        data.Add("entity_tag_events", "run_id,tick,entity_id,tag_id,assigned,source_code_id", "Entity tag events record discrete status changes: authored state machines such as terrified settlements and fervent factions.");
        data.Add("culture_power_snapshots", "run_id,tick,culture_id,witness_share,shadow_share,wake_share,witness_mass,hidden_mass,semantic_mass,custody_strength,claim_strength,war_strength,offering_strength", "Culture power snapshots track each culture's Witness/Shadow/Wake ledger shares of world totals.");
        data.Add("culture_field_component_snapshots", "run_id,tick,plane_code,culture_id,influenced_cell_count,component_count,largest_component_cell_count,largest_component_share,interior_cell_count,border_cell_count,isolated_cell_count,contested_cell_count,hole_cell_count", "Culture field component snapshots measure whether cultural influence coalesces into durable connected masses.");
        data.Add("culture_entity_field_snapshots", "run_id,tick,entity_id,culture_id,plane_code,cell_kind_code,slot_occupied,local_raw_mass,connected_raw_mass,influence_production,strength_band_code,raw_mass,total_raw_mass,foreign_culture_id,foreign_share,foreign_raw_mass", "Culture entity field snapshots compare slot occupants and ordinary entities inside the derived local culture field.");
        data.Add("culture_entity_birth_field_events", "run_id,tick,entity_id,culture_id,physical_field_band_code,physical_cell_kind_code,physical_local_raw_mass,physical_connected_raw_mass,semantic_field_band_code,semantic_cell_kind_code,semantic_local_raw_mass,semantic_connected_raw_mass", "Culture entity birth field events show whether generated entities appear inside strong, trace, or empty cultural fields.");
        data.Add("culture_influence_action_field_events", "run_id,tick,entity_id,culture_id,action_id,plane_code,cell_kind_code,slot_occupied,local_raw_mass,connected_raw_mass,previous_influence,current_influence,delta", "Culture influence action field events explain whether influence-producing actions reinforce interiors, borders, isolated traces, or contested cells.");
        data.Add("cell_road_events", "run_id,tick,source_code_id,plane_code,q,r,travel_count,previous_quality,current_quality,delta", "Cell road events own plane-aware road creation and reinforcement facts.");
        data.Add("map_snapshots", "run_id,tick,plane_code,terrain_id,cell_count,covered_cell_count,active_entity_count", "Map snapshots own plane-aware terrain aggregates.");
        data.Add("coverage_snapshots", "run_id,tick,plane_code,owner_entity_id,coverage_kind_code,cell_count,min_q,max_q,min_r,max_r,active_entity_count", "Coverage snapshots own named territory size and spread by plane.");
        data.Add("coverage_terrain_snapshots", "run_id,tick,plane_code,owner_entity_id,terrain_id,cell_count", "Coverage terrain snapshots explain named coverage terrain mix by plane.");
        data.Add("placement_events", "run_id,tick,cause_id,anchor_entity_id,placement_mode_code,plane_code,q,r,s,terrain_id,coverage_owner_entity_id,rejection_reason_code,selected", "Placement events explain spatial creation decisions.");
        data.Add("generator_events", "run_id,tick,event_type_code,generator_id,status_code,cause_id,target_pool_count,inactive_rejected_count,subtype_rejected_count,secondary_target_rejected_count,trigger_rejected_count,candidate_count,failed_fire_count,selected_target_entity_id,created_entity_count,created_relationship_count,created_map_cell_count,relationship_spend_count,output_lock_relationship_created_count", "Generator events own eligibility, fire counts, and fired relationship spend/lock effects.");
        data.Add("generator_target_evaluations", "run_id,tick,generator_id,target_entity_id,target_kind_id,target_subtype_id,target_culture_id,physical_q,physical_r,semantic_q,semantic_r,semantic_s,status_code,rejection_reason_code,physical_field_band_code,physical_cell_kind_code,physical_local_raw_mass,physical_connected_raw_mass,semantic_field_band_code,semantic_cell_kind_code,semantic_local_raw_mass,semantic_connected_raw_mass", "Generator target evaluations explain whether strong local cultural fields are passing or failing generator gates.");
        data.Add("generator_outputs", "run_id,tick,cause_id,generator_id,output_domain_code,output_entity_id,output_relationship_kind_id,output_source_entity_id,output_target_entity_id,output_q,output_r,output_s,placement_event_id", "Generator outputs explicitly attribute created domain rows.");
        data.Add("generator_state_snapshots", "run_id,tick,generator_id,enabled,cadence,cooldown_remaining,run_count,last_run_tick", "Generator state snapshots explain availability.");
        data.Add("gambit_events", "run_id,tick,event_type_code,gambit_id,actor_id,actor_kind_id,target_entity_id,state_code,succeeded,affected_entity_count", "Gambit events own attempt outcomes so success rate is measured directly instead of inferred from relationship side effects.");
        data.Add("relationship_events", "run_id,tick,change_code_id,source_code_id,cause_id,relationship_kind_id,polarity_code,source_id,target_id,previous_strength,new_strength,action_id", "Relationship events own topology, strength changes, and direct action attribution.");
        data.Add("relationship_snapshots", "run_id,tick,relationship_kind_id,active_count,average_strength,minimum_strength,maximum_strength", "Relationship snapshots own active relationship strength distribution.");
        data.Add("lifecycle_events", "run_id,tick,event_type_code,entity_id,kind_id,status_code", "Lifecycle events own deaths and historical transitions.");
        data.Add("lifecycle_reason_events", "run_id,tick,entity_id,reason_code_id,cause_id,source_code_id", "Lifecycle reasons explain deaths and transitions when tuning lifecycle behavior.");
    }

    private static void AddAnalysisContracts(TheoryData<string, string, string> data)
    {
        ArgumentNullException.ThrowIfNull(data);

        data.Add("analysis_overview", "run_count,max_tick,total_generator_fires,average_fires_per_run,average_final_active_entities,average_final_historical_entities,average_final_physical_spread,average_final_semantic_spread,average_final_uncovered_cells", "Overview gives run-level outcomes.");
        data.Add("analysis_readiness", "feature_code,source_table_code,source_row_count,ready,trustworthy,status_code", "Readiness prevents hidden missing or zero-filled sources.");
        data.Add("gambit_success_rate", "gambit_id,attempt_count,success_count,failure_count,success_rate,first_attempt_tick,last_attempt_tick,distinct_actors,distinct_targets", "Gambit success rate must be directly queryable from attempted gambits, not inferred from permanent relationship creation.");
        data.Add("generator_balance", "generator_id,pulses,eligible_pulses,blocked_pulses,eligible_not_selected_pulses,fired_count,fire_failed_pulses,selection_rate_when_eligible,average_target_pool,average_candidate_count,created_entity_count,created_relationship_count,created_map_cell_count,primary_blocker_code", "Generator balance explains pulse, rejection, and output counts.");
        data.Add("generated_subtype_coverage", "generator_id,kind_id,subtype_id,produced_count", "Generated subtype coverage tracks generator output coverage.");
        data.Add("low_altitude_verb_utilization", "kind_id,verb_id,operation_code,plane_code,target_mode_code,target_kind_id,trace_event_count,offered_count,selected_count,no_target_count,selection_rate_when_offered,average_selected_weight,average_selected_affordance_score,average_selected_affordance_bonus,selected_with_affordance_count,distinct_entities", "Low-altitude verb utilization is the active verb-deck report for movement and influence physics.");
        data.Add("low_altitude_affordance_utilization", "source_kind_code,source_id,shape_code,verb_id,operation_code,plane_code,target_mode_code,target_kind_id,offered_count,selected_count,contribution_count,hidden_contribution_count,generator_contribution_count,gambit_contribution_count,rows_with_gambit_contribution,rows_with_presence_contribution,rows_with_relationship_strength_contribution,rows_with_influence_contribution,selection_rate_when_offered,average_offered_score,average_selected_score,average_offered_bonus,average_selected_bonus,distinct_entities", "Low-altitude affordance utilization shows which generator and gambit affordances are affecting verb candidates.");
        data.Add("entity_roaming", "run_id,entity_id,kind_id,subtype_id,culture_id,status_code,plane_code,move_count,total_distance,unique_positions,roam_radius_max,roam_radius_p95,path_efficiency", "Entity roaming measures physical and semantic variance without treating movement volume as failure.");
        data.Add("roaming_summary", "kind_id,plane_code,entity_count,mover_count,zero_move_share,average_move_count,average_unique_positions,average_roam_radius,p50_roam_radius,p75_roam_radius,p95_roam_radius,overclustered_roam_share", "Roaming summary tracks whether entities actually traverse meaningful space by kind and plane.");
        data.Add("influence_dynamics", "run_id,entity_id,plane_code,kind_id,subtype_id,event_count,positive_delta_events,negative_delta_events,net_influence_delta,influenced_cell_count,average_final_influence,max_final_influence,isolated_cell_count,neighbor_connection_count,mean_anchor_distance,p95_anchor_distance,max_anchor_distance", "Influence dynamics tracks low-altitude settlement/faction influence shape and discontinuity by plane.");
        data.Add("road_creation", "run_id,plane_code,road_cell_count,light_road_cell_count,heavy_road_cell_count,total_travel_count,total_quality_delta,average_final_quality,max_quality,first_road_tick,last_road_tick", "Road creation tracks whether travel is creating persistent road surface by plane.");
        data.Add("road_usage", "run_id,plane_code,move_count,road_move_count,heavy_road_move_count,road_move_share,heavy_road_move_share,average_destination_road_quality,total_distance", "Road usage tracks observed movement onto already-roaded cells without claiming candidate prioritization.");
        data.Add("road_reinforcement", "run_id,plane_code,road_cell_count,reused_road_cell_count,reused_road_cell_share,positive_reinforcement_event_count,average_moves_before_road,average_moves_after_road,average_moves_after_light_road,average_moves_after_heavy_road,average_travel_count,average_final_quality,max_quality", "Road reinforcement tracks whether created roads receive later movement and quality growth.");
        data.Add("action_utilization", "kind_id,action_id,candidate_evaluations,selected_count,selection_rate_when_offered,average_score,precondition_pass_rate,cooldown_rate,top_failure_diagnostic_code,evidence_source_code", "Action utilization must be derived from current trace telemetry.");
        data.Add("stock_dynamics", "run_id,entity_id,kind_id,attribute_id,sample_count,minimum_observed,maximum_observed,variance,reversal_count,dwell_low_ticks,dwell_mid_ticks,dwell_high_ticks,pinned_low_ticks,pinned_high_ticks,normalized_range", "Stock dynamics answers whether entity stocks fluctuate or pin.");
        data.Add("map_spread", "run_id,snapshot_tick,plane_code,total_cell_count,active_entities,activity_event_count,activity_cells,activity_cell_share,activity_entropy,activity_top_cell_share,activity_bounding_box_area,activity_coverage_within_bounding_box,activity_axis_anisotropy,largest_unoccupied_strict_hex_radius", "Map spread measures activity footprint, concentration, corridor bias, and strict empty radius by plane.");
        data.Add("coverage_development", "run_id,plane_code,owner_entity_id,coverage_kind_code,initial_cell_count,final_cell_count,cell_growth,terrain_mix_count,active_entities_inside", "Coverage development measures named coverage growth by plane.");
        data.Add("terrain_occupancy", "plane_code,terrain_id,average_final_active_entities,average_named_coverage_share", "Terrain occupancy explains terrain utilization by plane.");
        data.Add("culture_boundary_components", "run_id,tick,plane_code,culture_id,influenced_cell_count,component_count,largest_component_cell_count,largest_component_share,interior_cell_count,border_cell_count,isolated_cell_count,contested_cell_count,hole_cell_count", "Culture boundary components expose host-sampled topology for coalescence analysis without making component identity a kernel gameplay fact.");
        data.Add("culture_births_by_boundary_kind", "run_id,culture_id,plane_code,field_band_code,cell_kind_code,birth_count,first_birth_tick,last_birth_tick", "Culture births by boundary kind show whether new entities appear inside kernel-classified cultural fields.");
        data.Add("minor_actor_actions_by_boundary_kind", "run_id,culture_id,plane_code,cell_kind_code,action_code,targeting_code,status_code,action_count,net_delta,average_score,average_local_raw_mass,average_connected_raw_mass", "Minor actor actions by boundary kind show how labor interacts with kernel-classified interiors, borders, isolated cells, and contested fronts.");
        data.Add("lifecycle", "kind_id,deaths,deaths_per_run,average_death_tick,average_final_active,average_final_historical,average_final_health", "Lifecycle summarizes deaths and kind health outcomes.");
        data.Add("experiment_comparison", "experiment_id,variant_id,run_count,average_fires_per_run,average_final_active_entities,average_final_physical_spread,average_final_uncovered_cells", "Experiment comparison is keyed by structured variant metadata.");
    }

    private static string[] SplitColumns(string columns) =>
        columns.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool IsStringType(string type) =>
        type.Contains("VARCHAR", StringComparison.OrdinalIgnoreCase) ||
        type.Contains("TEXT", StringComparison.OrdinalIgnoreCase);

    private static void ValidateContract(string tableName, string columns, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(columns);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
    }
}

[CollectionDefinition("Monte Carlo output gap acceptance", DisableParallelization = true)]
public sealed class MonteCarloOutputGapGroup
{
}

public sealed class MonteCarloOutputGapFixture : IDisposable
{
    private readonly string _outputDirectory;
    private readonly string _artifactRoot;

    public MonteCarloOutputGapFixture()
    {
        _artifactRoot = Path.Combine(Path.GetTempPath(), "canonry-monte-carlo-output-gap-tests", Guid.NewGuid().ToString("N"));
        const string runId = "output-gap-acceptance";
        _outputDirectory = Path.Combine(_artifactRoot, runId);
        Directory.CreateDirectory(_artifactRoot);
        if (!MonteCarloAcceptanceTestSwitch.IsEnabled)
        {
            return;
        }

        var configPath = Path.Combine(_artifactRoot, "kernel-tuning.json");
        File.WriteAllText(
            configPath,
            """
            {
              "kernelTuning": {}
            }
            """);

        var exitCode = MonteCarloCli.Run([
            "run",
            "--run-id", runId,
            "--artifact-root", _artifactRoot,
            "--runs", "3",
            // Lifespan bands intentionally push first lifecycle deaths beyond short smoke runs.
            // The output-gap fixture must run long enough to populate lifecycle tables from real events.
            "--ticks", "650",
            "--seed", "9401",
            "--trace",
            "--entity-snapshot-cadence", "10",
            "--config-file", configPath,
        ]);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Monte Carlo output fixture failed with exit code {exitCode}.");
        }
    }

    public bool RawParquetExists(string tableName) =>
        File.Exists(RawParquetPath(tableName));

    public bool AnalysisParquetExists(string tableName) =>
        File.Exists(AnalysisParquetPath(tableName));

    public bool AnalysisFileExists(string fileName) =>
        File.Exists(Path.Combine(_outputDirectory, "analysis", RequireNonEmpty(fileName)));

    public long RawRowCount(string tableName) =>
        RowCount(RawParquetPath(tableName));

    public long AnalysisRowCount(string tableName) =>
        RowCount(AnalysisParquetPath(tableName));

    public HashSet<string> RawColumns(string tableName) =>
        Columns(RawParquetPath(tableName));

    public HashSet<string> AnalysisColumns(string tableName) =>
        Columns(AnalysisParquetPath(tableName));

    public IReadOnlyList<OutputColumnType> RawColumnTypes(string tableName) =>
        ColumnTypes(RawParquetPath(tableName));

    public IReadOnlyList<OutputColumnType> AnalysisColumnTypes(string tableName) =>
        ColumnTypes(AnalysisParquetPath(tableName));

    public long DistinctRawValueCount(string tableName, string columnName) =>
        ScalarLong(RawParquetPath(tableName), $"COUNT(DISTINCT {QuoteIdentifier(RequireNonEmpty(columnName))})");

    public bool RawAny(string tableName, string predicate) =>
        ScalarLong(RawParquetPath(tableName), $"SUM(CASE WHEN {RequireNonEmpty(predicate)} THEN 1 ELSE 0 END)") > 0;

    public void AssertRawTablesExist(params string[] tableNames)
    {
        ArgumentNullException.ThrowIfNull(tableNames);
        foreach (var tableName in tableNames)
        {
            Assert.True(RawParquetExists(tableName), $"{tableName}: required raw parquet table is missing.");
        }
    }

    public void AssertAnalysisTablesExist(params string[] tableNames)
    {
        ArgumentNullException.ThrowIfNull(tableNames);
        foreach (var tableName in tableNames)
        {
            Assert.True(AnalysisParquetExists(tableName), $"{tableName}: required analysis parquet table is missing.");
        }
    }

    public long RawQueryLong(IReadOnlyList<string> tableNames, string sql) =>
        QueryLong(tableNames, sql, RawParquetPath);

    public long AnalysisQueryLong(IReadOnlyList<string> tableNames, string sql) =>
        QueryLong(tableNames, sql, AnalysisParquetPath);

    public void Dispose()
    {
        if (Directory.Exists(_artifactRoot))
        {
            Directory.Delete(_artifactRoot, recursive: true);
        }
    }

    private string RawParquetPath(string tableName) =>
        Path.Combine(_outputDirectory, $"{RequireNonEmpty(tableName)}.parquet");

    private string AnalysisParquetPath(string tableName) =>
        Path.Combine(_outputDirectory, "analysis", $"{RequireNonEmpty(tableName)}.parquet");

    private static long RowCount(string parquetPath) =>
        ScalarLong(parquetPath, "COUNT(*)");

    private static HashSet<string> Columns(string parquetPath) =>
        ColumnTypes(parquetPath)
            .Select(column => column.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static List<OutputColumnType> ColumnTypes(string parquetPath)
    {
        if (!File.Exists(parquetPath))
        {
            return [];
        }

        using var connection = OpenDuckDb();
        using var command = connection.CreateCommand();
        command.CommandText = $"DESCRIBE SELECT * FROM read_parquet({QuoteString(parquetPath)});";
        using var reader = command.ExecuteReader();
        var columns = new List<OutputColumnType>();
        while (reader.Read())
        {
            columns.Add(new OutputColumnType(reader.GetString(0), reader.GetString(1)));
        }

        return columns;
    }

    private static long QueryLong(IReadOnlyList<string> tableNames, string sql, Func<string, string> parquetPath)
    {
        ArgumentNullException.ThrowIfNull(tableNames);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        ArgumentNullException.ThrowIfNull(parquetPath);

        using var connection = OpenDuckDb();
        foreach (var tableName in tableNames)
        {
            var path = parquetPath(tableName);
            Assert.True(File.Exists(path), $"{tableName}: required parquet table is missing.");
            using var viewCommand = connection.CreateCommand();
            viewCommand.CommandText =
                $"CREATE OR REPLACE TEMP VIEW {QuoteIdentifier(tableName)} AS SELECT * FROM read_parquet({QuoteString(path)});";
            viewCommand.ExecuteNonQuery();
        }

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return ToLong(command.ExecuteScalar());
    }

    private static long ScalarLong(string parquetPath, string expression)
    {
        if (!File.Exists(parquetPath))
        {
            return 0;
        }

        using var connection = OpenDuckDb();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {RequireNonEmpty(expression)} AS value FROM read_parquet({QuoteString(parquetPath)});";
        return ToLong(command.ExecuteScalar());
    }

    private static long ToLong(object? value) =>
        value switch
        {
            null => 0,
            System.Numerics.BigInteger bigInteger => (long)bigInteger,
            _ => Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture),
        };

    private static DuckDBConnection OpenDuckDb()
    {
        var connection = new DuckDBConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static string QuoteIdentifier(string value) =>
        $"\"{RequireNonEmpty(value).Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    private static string QuoteString(string value) =>
        $"'{RequireNonEmpty(value).Replace("'", "''", StringComparison.Ordinal)}'";

    private static string RequireNonEmpty(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }
}

public sealed record OutputColumnType(string Name, string Type);

#pragma warning restore CA1515, CA2100
