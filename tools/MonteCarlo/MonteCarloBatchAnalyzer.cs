namespace MonteCarloTool;

using System.Data.Common;
using System.Globalization;

using DuckDB.NET.Data;

using Engine.Constants;
using Engine.Domain.Config;
using Engine.Observability;
using Engine.Observability.Export;

internal sealed partial class MonteCarloBatchAnalyzer(DomainConfig domainConfig)
{
    private static readonly string[] RequiredTables =
    [
        "symbols",
        "entity_name_symbols",
        "runs",
        "experiment_variants",
        "tick_snapshots",
        "kind_snapshots",
        "entity_snapshots",
        "entity_attribute_snapshots",
        "entity_events",
        "planner_goals",
        "planner_traces",
        "planner_candidates",
        "planner_scores",
        "planner_steps",
        "planner_localities",
        "trace_events",
        "process_events",
        "process_executions",
        "attribute_events",
        "movement_events",
        "resource_events",
        "resource_snapshots",
        "resource_terrain_snapshots",
        "pressure_events",
        "pressure_snapshots",
        "pressure_source_snapshots",
        "relationship_events",
        "relationship_snapshots",
        "generator_events",
        "generator_outputs",
        "generator_state_snapshots",
        "lifecycle_events",
        "lifecycle_reason_events",
        "map_generation_events",
        "map_cell_events",
        "map_snapshots",
        "coverage_snapshots",
        "coverage_terrain_snapshots",
        "placement_events",
    ];

    private static readonly OptionalRawTableDefinition[] OptionalRawTables =
    [
        new(
            "cell_influence_events",
            """
            SELECT
                CAST(NULL AS BIGINT) AS run_id,
                CAST(NULL AS BIGINT) AS tick,
                CAST(NULL AS BIGINT) AS source_code_id,
                CAST(NULL AS BIGINT) AS entity_id,
                CAST(NULL AS BIGINT) AS plane_code,
                CAST(NULL AS BIGINT) AS q,
                CAST(NULL AS BIGINT) AS r,
                CAST(NULL AS DOUBLE) AS previous_influence,
                CAST(NULL AS DOUBLE) AS current_influence,
                CAST(NULL AS DOUBLE) AS delta
            WHERE FALSE
            """),
    ];

    private readonly DomainConfig _domainConfig = domainConfig;

    public MonteCarloAnalysisReport AnalyzeToFiles(string batchDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchDirectory);

        var report = Analyze(batchDirectory);
        var analysisDirectory = Path.Combine(batchDirectory, "analysis");
        WriteArtifacts(report, batchDirectory, analysisDirectory);
        return report;
    }

    public MonteCarloAnalysisReport Analyze(string batchDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchDirectory);

        using var connection = OpenBatch(batchDirectory);

        var attributeConfigs = _domainConfig.Kinds
            .SelectMany(kind => kind.Value.Attributes.Select(attribute =>
                new KeyValuePair<(string Kind, string Attribute), AttributeConfig>(
                    (kind.Key.Value, attribute.Name),
                    attribute)))
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        var telemetryCoverage = LoadTelemetryCoverage(connection);
        var overview = LoadBatchOverview(connection);
        var generatorRows = LoadGeneratorRows(connection, overview.RunCount);
        var generatedCoverage = LoadGeneratedCoverageRows(connection);
        var lowAltitudeRows = LoadLowAltitudeVerbRows(connection);
        var roamingRows = LoadRoamingSummaryRows(connection);
        var influenceRows = LoadInfluenceSummaryRows(connection);
        var processRows = LoadProcessPressureRows(connection);
        var plannerRows = LoadPlannerPressureRows(connection, processRows);
        var resourceRows = LoadResourcePressureRows(connection);
        var mapGenerationRows = LoadMapGenerationRows(connection);
        var terrainRows = LoadTerrainOccupancyRows(connection);
        var lifecycleRows = LoadLifecycleRows(connection, overview.RunCount);
        var entitySnapshots = LoadEntitySnapshots(connection);
        var attributeRows = BuildAttributeBandRows(entitySnapshots, attributeConfigs);
        var mapRows = LoadMapSpreadRows(connection);
        var flags = BuildFlags(
            mapRows,
            lifecycleRows,
            mapGenerationRows,
            telemetryCoverage);

        return new MonteCarloAnalysisReport
        {
            Overview = overview,
            Flags = flags,
            GeneratorBalance = generatorRows,
            GeneratedSubtypeCoverage = generatedCoverage,
            LowAltitudeVerbUtilization = lowAltitudeRows,
            RoamingSummary = roamingRows,
            InfluenceSummary = influenceRows,
            PlannerPressure = plannerRows,
            ProcessPressure = processRows,
            AttributeBands = attributeRows,
            MapSpread = mapRows,
            MapGeneration = mapGenerationRows,
            TerrainOccupancy = terrainRows,
            Lifecycle = lifecycleRows,
            ResourcePressure = resourceRows,
        };
    }

    private void WriteArtifacts(MonteCarloAnalysisReport report, string batchDirectory, string analysisDirectory)
    {
        Directory.CreateDirectory(analysisDirectory);
        WriteAnalysisParquetTables(batchDirectory, analysisDirectory);
        File.WriteAllText(Path.Combine(analysisDirectory, "summary.md"), RenderMarkdown(report));
    }

    private DuckDBConnection OpenBatch(string batchDirectory)
    {
        var databasePath = Path.Combine(batchDirectory, "telemetry.duckdb");
        var hasAllParquet = RequiredTables
            .Select(table => Path.Combine(batchDirectory, $"{table}.parquet"))
            .All(File.Exists);

        if (!hasAllParquet)
        {
            if (!File.Exists(databasePath))
            {
                var missing = RequiredTables
                    .Select(table => Path.Combine(batchDirectory, $"{table}.parquet"))
                    .First(path => !File.Exists(path));
                throw new FileNotFoundException(
                    $"Missing Monte Carlo telemetry parquet '{missing}' and no DuckDB store '{databasePath}' was found.");
            }

            var dbConnection = new DuckDBConnection($"Data Source={databasePath}");
            dbConnection.Open();
            foreach (var table in RequiredTables)
            {
                SqlFileHelpers.Execute(
                    dbConnection,
                    $"CREATE OR REPLACE TEMP VIEW {SqlFileHelpers.QuoteIdentifier($"raw_{table}")} AS SELECT * FROM {SqlFileHelpers.QuoteIdentifier(table)};");
            }

            CreateOptionalRawViews(dbConnection, batchDirectory, useParquet: false);
            CreateAnalysisViews(dbConnection);
            return dbConnection;
        }

        var connection = new DuckDBConnection("Data Source=:memory:");
        connection.Open();

        foreach (var table in RequiredTables)
        {
            var path = Path.Combine(batchDirectory, $"{table}.parquet");
            SqlFileHelpers.Execute(
                connection,
                $"CREATE OR REPLACE TEMP VIEW {SqlFileHelpers.QuoteIdentifier($"raw_{table}")} AS SELECT * FROM read_parquet({SqlFileHelpers.QuoteString(path)});");
        }

        CreateOptionalRawViews(connection, batchDirectory, useParquet: true);
        CreateAnalysisViews(connection);
        return connection;
    }

    private static void CreateOptionalRawViews(DuckDBConnection connection, string batchDirectory, bool useParquet)
    {
        foreach (var table in OptionalRawTables)
        {
            var rawName = SqlFileHelpers.QuoteIdentifier($"raw_{table.TableName}");
            if (useParquet)
            {
                var path = Path.Combine(batchDirectory, $"{table.TableName}.parquet");
                var selectSql = File.Exists(path)
                    ? $"SELECT * FROM read_parquet({SqlFileHelpers.QuoteString(path)})"
                    : table.EmptySelectSql;
                SqlFileHelpers.Execute(connection, $"CREATE OR REPLACE TEMP VIEW {rawName} AS {selectSql};");
                continue;
            }

            var select = DuckDbTableExists(connection, table.TableName)
                ? $"SELECT * FROM {SqlFileHelpers.QuoteIdentifier(table.TableName)}"
                : table.EmptySelectSql;
            SqlFileHelpers.Execute(connection, $"CREATE OR REPLACE TEMP VIEW {rawName} AS {select};");
        }
    }

    private static bool DuckDbTableExists(DuckDBConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = 'main'
              AND table_name = ?
            """;
        var parameter = command.CreateParameter();
        parameter.Value = tableName;
        command.Parameters.Add(parameter);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
    }

    private static void CreateMaterializedAnalysisTable(DuckDBConnection connection, string tableName, string selectSql)
    {
        SqlFileHelpers.Execute(
            connection,
            $"CREATE OR REPLACE TEMP TABLE {SqlFileHelpers.QuoteIdentifier(tableName)} AS {selectSql};");
    }

    private BatchOverviewRow LoadBatchOverview(DuckDBConnection connection)
    {
        const string sql = """
            WITH final_ticks AS (
                SELECT run_id, MAX(tick) AS final_tick
                FROM tick_snapshots
                GROUP BY run_id
            ),
            final_snapshots AS (
                SELECT ts.*
                FROM tick_snapshots ts
                JOIN final_ticks ft
                  ON ts.run_id = ft.run_id
                 AND ts.tick = ft.final_tick
            )
            SELECT
                COUNT(*) AS run_count,
                COALESCE(MAX(tick), 0) AS max_tick,
                COALESCE(SUM(total_generator_fires), 0) AS total_generator_fires,
                COALESCE(AVG(CAST(total_generator_fires AS DOUBLE)), 0.0) AS average_fires_per_run,
                COALESCE(AVG(CAST(active_entities AS DOUBLE)), 0.0) AS average_final_active_entities,
                COALESCE(AVG(CAST(historical_entities AS DOUBLE)), 0.0) AS average_final_historical_entities,
                COALESCE(AVG(CAST(physical_spread AS DOUBLE)), 0.0) AS average_final_physical_spread,
                COALESCE(AVG(CAST(semantic_spread AS DOUBLE)), 0.0) AS average_final_semantic_spread,
                COALESCE(AVG(CAST(uncovered_cells AS DOUBLE)), 0.0) AS average_final_uncovered_cells
            FROM final_snapshots;
            """;

        return QuerySingle(connection, sql, reader => new BatchOverviewRow(
            RunCount: ReadInt(reader, "run_count"),
            MaxTick: ReadInt(reader, "max_tick"),
            TotalGeneratorFires: ReadInt(reader, "total_generator_fires"),
            AverageFiresPerRun: ReadDouble(reader, "average_fires_per_run"),
            AverageFinalActiveEntities: ReadDouble(reader, "average_final_active_entities"),
            AverageFinalHistoricalEntities: ReadDouble(reader, "average_final_historical_entities"),
            AverageFinalPhysicalSpread: ReadDouble(reader, "average_final_physical_spread"),
            AverageFinalSemanticSpread: ReadDouble(reader, "average_final_semantic_spread"),
            AverageFinalUncoveredCells: ReadDouble(reader, "average_final_uncovered_cells")));
    }

    private List<GeneratorAnalysisRow> LoadGeneratorRows(DuckDBConnection connection, int runCount)
    {
        var observed = LoadObservedGeneratorRows(connection);
        return BuildGeneratorRows(observed, runCount);
    }

    private static Dictionary<string, ObservedGeneratorRow> LoadObservedGeneratorRows(DuckDBConnection connection)
    {
        return Query(connection, GeneratorRowsSql, reader => new ObservedGeneratorRow(
            GeneratorId: ReadString(reader, "generator_id"),
            Pulses: ReadInt(reader, "pulses"),
            EligiblePulses: ReadInt(reader, "eligible_pulses"),
            BlockedPulses: ReadInt(reader, "blocked_pulses"),
            EligibleNotSelectedPulses: ReadInt(reader, "eligible_not_selected_pulses"),
            FiredCount: ReadInt(reader, "fired_count"),
            FireFailedPulses: ReadInt(reader, "fire_failed_pulses"),
            AverageTargetPool: ReadDouble(reader, "average_target_pool"),
            AverageCandidateCount: ReadDouble(reader, "average_candidate_count"),
            InactiveRejected: ReadInt(reader, "inactive_rejected"),
            SubtypeRejected: ReadInt(reader, "subtype_rejected"),
            SaturationRejected: ReadInt(reader, "saturation_rejected"),
            SecondaryTargetRejected: ReadInt(reader, "secondary_target_rejected"),
            WildernessRejected: ReadInt(reader, "wilderness_rejected"),
            TriggerRejected: ReadInt(reader, "trigger_rejected"),
            CreatedEntityCount: ReadInt(reader, "created_entity_count"),
            CreatedRelationshipCount: ReadInt(reader, "created_relationship_count")))
            .ToDictionary(row => row.GeneratorId, row => row, StringComparer.Ordinal);
    }

    private List<GeneratorAnalysisRow> BuildGeneratorRows(
        Dictionary<string, ObservedGeneratorRow> observed,
        int runCount)
    {
        var configured = _domainConfig.Semantics?.Generators ?? [];
        var rows = configured.Select(generator =>
        {
            var observedRow = observed.GetValueOrDefault(generator.Id) ?? ObservedGeneratorRow.Empty(generator.Id);
            var selectionRate = observedRow.EligiblePulses == 0
                ? 0d
                : observedRow.FiredCount / (double)observedRow.EligiblePulses;
            var primaryBlocker = PrimaryBlocker(observedRow);

            return new GeneratorAnalysisRow(
                GeneratorId: generator.Id,
                GeneratorName: generator.Name,
                TargetKind: generator.Target.Kind.Value,
                Pulses: observedRow.Pulses,
                EligiblePulses: observedRow.EligiblePulses,
                BlockedPulses: observedRow.BlockedPulses,
                EligibleNotSelectedPulses: observedRow.EligibleNotSelectedPulses,
                FiredCount: observedRow.FiredCount,
                FireFailedPulses: observedRow.FireFailedPulses,
                FireRatePerRun: runCount == 0 ? 0d : observedRow.FiredCount / (double)runCount,
                SelectionRateWhenEligible: selectionRate,
                AverageTargetPool: observedRow.AverageTargetPool,
                AverageCandidateCount: observedRow.AverageCandidateCount,
                InactiveRejected: observedRow.InactiveRejected,
                SubtypeRejected: observedRow.SubtypeRejected,
                SaturationRejected: observedRow.SaturationRejected,
                SecondaryTargetRejected: observedRow.SecondaryTargetRejected,
                WildernessRejected: observedRow.WildernessRejected,
                TriggerRejected: observedRow.TriggerRejected,
                PrimaryBlocker: primaryBlocker,
                CreatedEntityCount: observedRow.CreatedEntityCount,
                CreatedRelationshipCount: observedRow.CreatedRelationshipCount);
        });

        return rows
            .OrderByDescending(row => row.FiredCount)
            .ThenByDescending(row => row.EligiblePulses)
            .ThenBy(row => row.GeneratorId, StringComparer.Ordinal)
            .ToList();
    }

    private List<GeneratedSubtypeCoverageRow> LoadGeneratedCoverageRows(DuckDBConnection connection)
    {
        const string sql = """
            SELECT
                generator_id,
                kind,
                subtype,
                COUNT(*) AS produced_count
            FROM generated_entities
            GROUP BY generator_id, kind, subtype
            ORDER BY generator_id, produced_count DESC, kind, subtype;
            """;

        return Query(connection, sql, reader => new GeneratedSubtypeCoverageRow(
            GeneratorId: ReadString(reader, "generator_id"),
            Kind: ReadString(reader, "kind"),
            Subtype: ReadString(reader, "subtype"),
            ProducedCount: ReadInt(reader, "produced_count")));
    }

    private static TelemetryCoverage LoadTelemetryCoverage(DuckDBConnection connection)
    {
        var sql = $"""
            SELECT
                (SELECT COUNT(*) FROM process_planner_events) AS process_planner_rows,
                (SELECT COUNT(*) FROM process_execution_events) AS process_execution_rows,
                (SELECT COUNT(*) FROM raw_map_snapshots) AS map_snapshot_rows,
                (SELECT COUNT(*) FROM raw_planner_localities) AS locality_opportunity_rows,
                (SELECT COUNT(*) FROM raw_planner_traces) AS planner_trace_rows,
                (SELECT COUNT(*) FROM raw_trace_events WHERE action_family_code = {ActionFamilyCodes.LowAltitudeVerb}) AS low_altitude_trace_rows,
                (SELECT COUNT(*) FROM raw_cell_influence_events) AS cell_influence_rows;
            """;

        return QuerySingle(connection, sql, reader => new TelemetryCoverage(
            ProcessPlannerRows: ReadInt(reader, "process_planner_rows"),
            ProcessExecutionRows: ReadInt(reader, "process_execution_rows"),
            MapSnapshotRows: ReadInt(reader, "map_snapshot_rows"),
            LocalityOpportunityRows: ReadInt(reader, "locality_opportunity_rows"),
            PlannerTraceRows: ReadInt(reader, "planner_trace_rows"),
            LowAltitudeTraceRows: ReadInt(reader, "low_altitude_trace_rows"),
            CellInfluenceRows: ReadInt(reader, "cell_influence_rows")));
    }

    private List<ProcessPressureRow> LoadProcessPressureRows(DuckDBConnection connection)
    {
        var observed = LoadObservedActionRows(connection);
        return BuildProcessPressureRows(observed, "planner_candidates");
    }

    private static Dictionary<(string Kind, string ProcessName), ObservedProcessRow> LoadObservedActionRows(
        DuckDBConnection connection)
    {
        var summarySql = $"""
            SELECT
                COALESCE(kind_symbol.stable_name, CAST(candidate.kind_id AS VARCHAR)) AS kind,
                COALESCE(action_symbol.stable_name, CAST(candidate.action_id AS VARCHAR)) AS process_name,
                COUNT(*) AS candidate_evaluations,
                SUM(CASE WHEN candidate.selected THEN 1 ELSE 0 END) AS selected_count,
                COALESCE(AVG(CAST(candidate.adjusted_score AS DOUBLE)), 0.0) AS average_score,
                COALESCE(AVG(CASE WHEN candidate.precondition_status_code = 0 THEN 1.0 ELSE 0.0 END), 0.0) AS precondition_pass_rate,
                COALESCE(AVG(CASE WHEN candidate.cooldown_state_code <> 0 THEN 1.0 ELSE 0.0 END), 0.0) AS cooldown_rate
            FROM raw_planner_candidates candidate
            LEFT JOIN symbol_lookup kind_symbol
              ON kind_symbol.symbol_kind = {(int)Engine.Projection.ProjectionSymbolKind.Kind}
             AND kind_symbol.id = candidate.kind_id
            LEFT JOIN symbol_lookup action_symbol
              ON action_symbol.symbol_kind = {(int)Engine.Projection.ProjectionSymbolKind.Verb}
             AND action_symbol.id = candidate.action_id
            GROUP BY kind, process_name;
            """;

        return Query(connection, summarySql, reader =>
            {
                return new ObservedProcessRow(
                    Kind: ReadString(reader, "kind"),
                    ProcessName: ReadString(reader, "process_name"),
                    CandidateEvaluations: ReadInt(reader, "candidate_evaluations"),
                    SelectedCount: ReadInt(reader, "selected_count"),
                    AverageScore: ReadDouble(reader, "average_score"),
                    PreconditionPassRate: ReadDouble(reader, "precondition_pass_rate"),
                    CooldownRate: ReadDouble(reader, "cooldown_rate"));
            })
            .ToDictionary(row => (row.Kind, row.ProcessName), row => row);
    }

    private List<ProcessPressureRow> BuildProcessPressureRows(
        Dictionary<(string Kind, string ProcessName), ObservedProcessRow> observed,
        string evidenceSource)
    {
        var configured = _domainConfig.Processes
            .Select(process => (Kind: process.TargetKind.Value, Name: process.Id))
            .Concat((_domainConfig.Gameplay?.LowAltitudeVerbs ?? [])
                .Select(verb => (Kind: verb.Kind, Name: verb.Id)))
            .ToList();

        var rows = configured
            .Select(action =>
            {
                var observedRow = observed.GetValueOrDefault((action.Kind, action.Name)) ??
                                  ObservedProcessRow.Empty(action.Kind, action.Name);

                return new ProcessPressureRow(
                    Kind: action.Kind,
                    ProcessName: action.Name,
                    CandidateEvaluations: observedRow.CandidateEvaluations,
                    SelectedCount: observedRow.SelectedCount,
                    SelectionRateWhenOffered: observedRow.CandidateEvaluations == 0
                        ? 0d
                        : observedRow.SelectedCount / (double)observedRow.CandidateEvaluations,
                    AverageScore: observedRow.AverageScore,
                    PreconditionPassRate: observedRow.PreconditionPassRate,
                    CooldownRate: observedRow.CooldownRate,
                    TopFailureDiagnostic: string.Empty,
                    EvidenceSource: evidenceSource);
            });

        return rows
            .OrderBy(row => row.Kind, StringComparer.Ordinal)
            .ThenByDescending(row => row.SelectedCount)
            .ThenByDescending(row => row.CandidateEvaluations)
            .ThenBy(row => row.ProcessName, StringComparer.Ordinal)
            .ToList();
    }

    private static readonly string PlannerPressureRowsSql = $"""
        SELECT
            COALESCE(kind_symbol.stable_name, CAST(candidate.kind_id AS VARCHAR)) AS kind,
            COUNT(DISTINCT candidate.plan_id) AS evaluations,
            0 AS stale_evaluations,
            COUNT(DISTINCT CASE WHEN execution.entity_id IS NULL THEN candidate.plan_id ELSE NULL END) AS no_execution_evaluations,
            COUNT(DISTINCT CASE WHEN execution.entity_id IS NOT NULL THEN candidate.plan_id ELSE NULL END) AS executed_evaluations,
            COALESCE(AVG(CAST(candidate_count.candidate_count AS DOUBLE)), 0.0) AS average_candidate_count
        FROM raw_planner_candidates candidate
        LEFT JOIN raw_process_executions execution
          ON execution.run_id = candidate.run_id
         AND execution.entity_id = candidate.entity_id
         AND execution.plan_id = candidate.plan_id
         AND execution.goal_id = candidate.goal_id
        LEFT JOIN symbol_lookup kind_symbol
          ON kind_symbol.symbol_kind = {(int)Engine.Projection.ProjectionSymbolKind.Kind}
         AND kind_symbol.id = candidate.kind_id
        LEFT JOIN (
            SELECT run_id, entity_id, plan_id, COUNT(*) AS candidate_count
            FROM raw_planner_candidates
            GROUP BY run_id, entity_id, plan_id
        ) candidate_count
          ON candidate_count.run_id = candidate.run_id
         AND candidate_count.entity_id = candidate.entity_id
         AND candidate_count.plan_id = candidate.plan_id
        GROUP BY kind;
        """;

    private List<PlannerPressureRow> LoadPlannerPressureRows(
        DuckDBConnection connection,
        List<ProcessPressureRow> processRows)
    {
        var observed = Query(connection, PlannerPressureRowsSql, reader => new ObservedPlannerRow(
            Kind: ReadString(reader, "kind"),
            Evaluations: ReadInt(reader, "evaluations"),
            StaleEvaluations: ReadInt(reader, "stale_evaluations"),
            NoExecutionEvaluations: ReadInt(reader, "no_execution_evaluations"),
            ExecutedEvaluations: ReadInt(reader, "executed_evaluations"),
            AverageCandidateCount: ReadDouble(reader, "average_candidate_count")))
            .ToDictionary(row => row.Kind, row => row, StringComparer.Ordinal);

        var processByKind = processRows
            .GroupBy(row => row.Kind, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(row => row.SelectedCount)
                    .ThenByDescending(row => row.CandidateEvaluations)
                    .ThenBy(row => row.ProcessName, StringComparer.Ordinal)
                    .ToList(),
                StringComparer.Ordinal);

        var rows = _domainConfig.Kinds.Keys.Select(kind =>
        {
            var observedRow = observed.GetValueOrDefault(kind.Value) ?? ObservedPlannerRow.Empty(kind.Value);
            var ranked = processByKind.GetValueOrDefault(kind.Value) ?? [];
            var top = ranked.FirstOrDefault();
            var totalSelected = ranked.Sum(row => row.SelectedCount);

            return new PlannerPressureRow(
                Kind: kind.Value,
                Evaluations: observedRow.Evaluations,
                StaleRate: observedRow.Evaluations == 0 ? 0d : observedRow.StaleEvaluations / (double)observedRow.Evaluations,
                NoExecutionRate: observedRow.Evaluations == 0 ? 0d : observedRow.NoExecutionEvaluations / (double)observedRow.Evaluations,
                AverageCandidateCount: observedRow.AverageCandidateCount,
                TopExecutedProcess: top?.ProcessName ?? string.Empty,
                TopProcessShareOfExecutions: totalSelected == 0 || top is null ? 0d : top.SelectedCount / (double)totalSelected);
        });

        return rows
            .OrderBy(row => row.Kind, StringComparer.Ordinal)
            .ToList();
    }

    private List<ResourcePressureRow> LoadResourcePressureRows(DuckDBConnection connection)
    {
        const string overviewSql = """
            SELECT
                resource_type,
                COALESCE(AVG(CAST(total AS DOUBLE)), 0.0) AS average_total_over_time,
                COALESCE(MIN(CAST(minimum AS DOUBLE)), 0.0) AS lowest_cell_stock
            FROM resource_snapshots
            GROUP BY resource_type;
            """;

        var endpoints = LoadResourceEndpointTotals(connection);
        return Query(connection, overviewSql, reader => BuildResourcePressureRow(reader, endpoints))
            .OrderBy(row => row.ResourceType, StringComparer.Ordinal)
            .ToList();
    }

    private static Dictionary<string, (double Initial, double Final)> LoadResourceEndpointTotals(
        DuckDBConnection connection)
    {
        const string endpointSql = """
            WITH first_ticks AS (
                SELECT run_id, MIN(tick) AS first_tick
                FROM resource_snapshots
                GROUP BY run_id
            ),
            last_ticks AS (
                SELECT run_id, MAX(tick) AS last_tick
                FROM resource_snapshots
                GROUP BY run_id
            ),
            initial_totals AS (
                SELECT
                    rs.resource_type,
                    COALESCE(AVG(CAST(rs.total AS DOUBLE)), 0.0) AS average_initial_total
                FROM resource_snapshots rs
                JOIN first_ticks ft
                  ON rs.run_id = ft.run_id
                 AND rs.tick = ft.first_tick
                GROUP BY rs.resource_type
            ),
            final_totals AS (
                SELECT
                    rs.resource_type,
                    COALESCE(AVG(CAST(rs.total AS DOUBLE)), 0.0) AS average_final_total
                FROM resource_snapshots rs
                JOIN last_ticks lt
                  ON rs.run_id = lt.run_id
                 AND rs.tick = lt.last_tick
                GROUP BY rs.resource_type
            )
            SELECT
                COALESCE(i.resource_type, f.resource_type) AS resource_type,
                COALESCE(i.average_initial_total, 0.0) AS average_initial_total,
                COALESCE(f.average_final_total, 0.0) AS average_final_total
            FROM initial_totals i
            FULL OUTER JOIN final_totals f
              ON i.resource_type = f.resource_type;
            """;

        return Query(connection, endpointSql, reader => new KeyValuePair<string, (double Initial, double Final)>(
                ReadString(reader, "resource_type"),
                (
                    ReadDouble(reader, "average_initial_total"),
                    ReadDouble(reader, "average_final_total"))))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private static ResourcePressureRow BuildResourcePressureRow(
        DbDataReader reader,
        Dictionary<string, (double Initial, double Final)> endpoints)
    {
        var resourceType = ReadString(reader, "resource_type");
        var endpoint = endpoints.GetValueOrDefault(resourceType);
        return new ResourcePressureRow(
            ResourceType: resourceType,
            AverageTotalOverTime: ReadDouble(reader, "average_total_over_time"),
            AverageInitialTotal: endpoint.Initial,
            AverageFinalTotal: endpoint.Final,
            FinalToInitialRatio: ComputeRatio(endpoint.Final, endpoint.Initial),
            LowestCellStock: ReadDouble(reader, "lowest_cell_stock"));
    }

    private List<MapGenerationSummaryRow> LoadMapGenerationRows(DuckDBConnection connection)
    {
        const string sql = """
            SELECT
                phase,
                description,
                reasons,
                COUNT(*) AS event_count,
                COUNT(DISTINCT run_id) AS affected_runs,
                SUM(CASE WHEN will_retry THEN 1 ELSE 0 END) AS retrying_events,
                MAX(attempt) AS max_attempt
            FROM map_generation_events
            GROUP BY phase, description, reasons
            ORDER BY event_count DESC, phase, description, reasons;
            """;

        return Query(connection, sql, reader => new MapGenerationSummaryRow(
                Phase: ReadString(reader, "phase"),
                Description: ReadString(reader, "description"),
                Reasons: ReadString(reader, "reasons"),
                EventCount: ReadInt(reader, "event_count"),
                AffectedRuns: ReadInt(reader, "affected_runs"),
                RetryingEvents: ReadInt(reader, "retrying_events"),
                MaxAttempt: ReadInt(reader, "max_attempt")))
            .ToList();
    }

    private List<TerrainOccupancyRow> LoadTerrainOccupancyRows(DuckDBConnection connection)
    {
        const string sql = """
            WITH final_ticks AS (
                SELECT run_id, MAX(tick) AS final_tick
                FROM map_snapshots
                GROUP BY run_id
            ),
            final_map AS (
                SELECT ms.*
                FROM map_snapshots ms
                JOIN final_ticks ft
                  ON ms.run_id = ft.run_id
                 AND ms.tick = ft.final_tick
            )
            SELECT
                terrain,
                COALESCE(AVG(CAST(active_entity_count AS DOUBLE)), 0.0) AS average_final_active_entities,
                COALESCE(AVG(CASE WHEN cell_count = 0 THEN 0.0 ELSE CAST(covered_cell_count AS DOUBLE) / cell_count END), 0.0) AS average_named_coverage_share
            FROM final_map
            GROUP BY terrain;
            """;

        return Query(connection, sql, reader => new TerrainOccupancyRow(
                Terrain: ReadString(reader, "terrain"),
                AverageFinalActiveEntities: ReadDouble(reader, "average_final_active_entities"),
                AverageNamedCoverageShare: ReadDouble(reader, "average_named_coverage_share")))
            .OrderByDescending(row => row.AverageFinalActiveEntities)
            .ThenBy(row => row.Terrain, StringComparer.Ordinal)
            .ToList();
    }

    private static List<MapSpreadSummaryRow> LoadMapSpreadRows(DuckDBConnection connection)
    {
        var sql = $"{MapSpreadSql} ORDER BY run_id;";
        return Query(connection, sql, reader => new MapSpreadSummaryRow(
            RunId: ReadString(reader, "run_id"),
            SnapshotTick: ReadInt(reader, "snapshot_tick"),
            ActiveEntities: ReadInt(reader, "active_entities"),
            ActivityEventCount: ReadInt(reader, "activity_event_count"),
            ActivityCells: ReadInt(reader, "activity_cells"),
            ActivityCellShare: ReadDouble(reader, "activity_cell_share"),
            ActivityEntropy: ReadDouble(reader, "activity_entropy"),
            ActivityTopCellShare: ReadDouble(reader, "activity_top_cell_share"),
            ActivityBoundingBoxArea: ReadInt(reader, "activity_bounding_box_area"),
            ActivityCoverageWithinBoundingBox: ReadDouble(reader, "activity_coverage_within_bounding_box"),
            ActivityAxisAnisotropy: ReadDouble(reader, "activity_axis_anisotropy"),
            LargestUnoccupiedStrictHexRadius: ReadInt(reader, "largest_unoccupied_strict_hex_radius")));
    }

    private List<LifecycleSummaryRow> LoadLifecycleRows(DuckDBConnection connection, int runCount)
    {
        var finals = LoadFinalLifecycleRows(connection);
        var deaths = LoadLifecycleDeathRows(connection);
        return BuildLifecycleRows(finals, deaths, runCount);
    }

    private static Dictionary<string, ObservedLifecycleRow> LoadFinalLifecycleRows(DuckDBConnection connection)
    {
        const string finalSql = """
            WITH final_ticks AS (
                SELECT run_id, MAX(tick) AS final_tick
                FROM kind_snapshots
                GROUP BY run_id
            ),
            final_kind AS (
                SELECT ks.*
                FROM kind_snapshots ks
                JOIN final_ticks ft
                  ON ks.run_id = ft.run_id
                 AND ks.tick = ft.final_tick
            )
            SELECT
                kind,
                COALESCE(AVG(CAST(active_count AS DOUBLE)), 0.0) AS average_final_active,
                COALESCE(AVG(CAST(historical_count AS DOUBLE)), 0.0) AS average_final_historical,
                COALESCE(AVG(CAST(average_health AS DOUBLE)), 0.0) AS average_final_health
            FROM final_kind
            GROUP BY kind;
            """;

        return Query(connection, finalSql, reader => new ObservedLifecycleRow(
                Kind: ReadString(reader, "kind"),
                Deaths: 0,
                AverageDeathTick: 0d,
                AverageFinalActive: ReadDouble(reader, "average_final_active"),
                AverageFinalHistorical: ReadDouble(reader, "average_final_historical"),
                AverageFinalHealth: ReadDouble(reader, "average_final_health")))
            .ToDictionary(row => row.Kind, row => row, StringComparer.Ordinal);
    }

    private static Dictionary<string, (int Deaths, double AvgTick)> LoadLifecycleDeathRows(
        DuckDBConnection connection)
    {
        const string deathSql = """
            SELECT
                kind,
                COUNT(*) AS deaths,
                COALESCE(AVG(CAST(tick AS DOUBLE)), 0.0) AS average_death_tick
            FROM lifecycle_events
            WHERE event_type = 'death'
            GROUP BY kind;
            """;

        return Query(connection, deathSql, reader => new KeyValuePair<string, (int Deaths, double AvgTick)>(
                ReadString(reader, "kind"),
                (ReadInt(reader, "deaths"), ReadDouble(reader, "average_death_tick"))))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private List<LifecycleSummaryRow> BuildLifecycleRows(
        Dictionary<string, ObservedLifecycleRow> finals,
        Dictionary<string, (int Deaths, double AvgTick)> deaths,
        int runCount)
    {
        var rows = _domainConfig.Kinds.Keys.Select(kind =>
        {
            var observed = finals.GetValueOrDefault(kind.Value) ?? ObservedLifecycleRow.Empty(kind.Value);
            var death = deaths.GetValueOrDefault(kind.Value);
            return new LifecycleSummaryRow(
                Kind: kind.Value,
                Deaths: death.Deaths,
                DeathsPerRun: runCount == 0 ? 0d : death.Deaths / (double)runCount,
                AverageDeathTick: death.AvgTick,
                AverageFinalActive: observed.AverageFinalActive,
                AverageFinalHistorical: observed.AverageFinalHistorical,
                AverageFinalHealth: observed.AverageFinalHealth);
        });

        return rows
            .OrderBy(row => row.Kind, StringComparer.Ordinal)
            .ToList();
    }

    private List<EntitySnapshotRecord> LoadEntitySnapshots(DuckDBConnection connection)
    {
        const string sql = """
            SELECT
                run_id,
                tick,
                entity_id,
                kind,
                subtype,
                status,
                physical_q,
                physical_r,
                attributes
            FROM entity_snapshots;
            """;

        return Query(connection, sql, reader => new EntitySnapshotRecord(
                RunId: ReadString(reader, "run_id"),
                Tick: ReadInt(reader, "tick"),
                EntityId: ReadInt(reader, "entity_id"),
                Kind: ReadString(reader, "kind"),
                Subtype: ReadString(reader, "subtype"),
                Status: ReadString(reader, "status"),
                PhysicalQ: ReadInt(reader, "physical_q"),
                PhysicalR: ReadInt(reader, "physical_r"),
                Attributes: ReadString(reader, "attributes")))
            .ToList();
    }

    private static List<AttributeBandSummaryRow> BuildAttributeBandRows(
        List<EntitySnapshotRecord> snapshots,
        Dictionary<(string Kind, string Attribute), AttributeConfig> attributeConfigs)
    {
        var aggregates = BuildAttributeBandAggregates(snapshots, attributeConfigs);
        return aggregates
            .OrderBy(pair => pair.Key.Kind, StringComparer.Ordinal)
            .ThenBy(pair => pair.Key.Subtype, StringComparer.Ordinal)
            .ThenBy(pair => pair.Key.Attribute, StringComparer.Ordinal)
            .Select(pair => BuildAttributeBandRow(pair.Key, pair.Value))
            .ToList();
    }

    private static Dictionary<(string Kind, string Subtype, string Attribute), AttributeBandAccumulator>
        BuildAttributeBandAggregates(
            List<EntitySnapshotRecord> snapshots,
            Dictionary<(string Kind, string Attribute), AttributeConfig> attributeConfigs)
    {
        var aggregates = new Dictionary<(string Kind, string Subtype, string Attribute), AttributeBandAccumulator>();
        foreach (var snapshot in snapshots)
        {
            if (!string.Equals(snapshot.Status, "Active", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var (attributeName, value) in ParseAttributes(snapshot.Attributes))
            {
                if (!attributeConfigs.TryGetValue((snapshot.Kind, attributeName), out var config))
                {
                    continue;
                }

                var normalized = Normalize(value, config.Min, config.Max);
                var key = (snapshot.Kind, snapshot.Subtype, attributeName);
                if (!aggregates.TryGetValue(key, out var accumulator))
                {
                    accumulator = new AttributeBandAccumulator();
                    aggregates[key] = accumulator;
                }

                accumulator.SampleCount++;
                accumulator.ValueSum += value;
                accumulator.NormalizedSum += normalized;
                accumulator.MinObserved = Math.Min(accumulator.MinObserved, value);
                accumulator.MaxObserved = Math.Max(accumulator.MaxObserved, value);

                if (normalized <= 0.15d)
                {
                    accumulator.CriticalCount++;
                }
                else if (normalized < 0.35d)
                {
                    accumulator.LowCount++;
                }
                else if (normalized <= 0.65d)
                {
                    accumulator.MidCount++;
                }
                else
                {
                    accumulator.HighCount++;
                }
            }
        }

        return aggregates;
    }

    private static AttributeBandSummaryRow BuildAttributeBandRow(
        (string Kind, string Subtype, string Attribute) key,
        AttributeBandAccumulator acc)
    {
        var denom = Math.Max(1, acc.SampleCount);
        return new AttributeBandSummaryRow(
            Kind: key.Kind,
            Subtype: key.Subtype,
            AttributeName: key.Attribute,
            SampleCount: acc.SampleCount,
            AverageValue: acc.ValueSum / denom,
            AverageNormalized: acc.NormalizedSum / denom,
            MinimumObserved: acc.SampleCount == 0 ? 0d : acc.MinObserved,
            MaximumObserved: acc.SampleCount == 0 ? 0d : acc.MaxObserved,
            CriticalShare: acc.CriticalCount / (double)denom,
            LowShare: acc.LowCount / (double)denom,
            MidShare: acc.MidCount / (double)denom,
            HighShare: acc.HighCount / (double)denom);
    }

}
