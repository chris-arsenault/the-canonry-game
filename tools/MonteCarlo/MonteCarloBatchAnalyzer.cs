namespace MonteCarloTool;

using System.Data.Common;
using System.Globalization;
using System.Numerics;
using System.Text;

using DuckDB.NET.Data;

using Engine.Domain.Config;
using Engine.Map;
using Engine.Processes;

internal sealed class MonteCarloBatchAnalyzer(DomainConfig domainConfig)
{
    private static readonly string[] RequiredTables =
    [
        "runs",
        "tick_snapshots",
        "kind_snapshots",
        "entity_snapshots",
        "entity_events",
        "planner_goals",
        "process_events",
        "process_candidates",
        "attribute_events",
        "movement_events",
        "resource_events",
        "resource_snapshots",
        "relationship_events",
        "relationship_snapshots",
        "generator_events",
        "generated_entities",
        "lifecycle_events",
        "map_generation_events",
        "map_snapshots",
    ];

    private readonly DomainConfig _domainConfig = domainConfig;

    public MonteCarloAnalysisReport AnalyzeToFiles(string batchDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchDirectory);

        var report = Analyze(batchDirectory);
        var analysisDirectory = Path.Combine(batchDirectory, "analysis");
        WriteArtifacts(report, analysisDirectory);
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

        var overview = LoadBatchOverview(connection);
        var generatorRows = LoadGeneratorRows(connection, overview.RunCount);
        var generatedCoverage = LoadGeneratedCoverageRows(connection);
        var processRows = LoadProcessPressureRows(connection);
        var plannerRows = LoadPlannerPressureRows(connection, processRows);
        var resourceRows = LoadResourcePressureRows(connection);
        var mapGenerationRows = LoadMapGenerationRows(connection);
        var terrainRows = LoadTerrainOccupancyRows(connection);
        var lifecycleRows = LoadLifecycleRows(connection, overview.RunCount);
        var entitySnapshots = LoadEntitySnapshots(connection);
        var attributeRows = BuildAttributeBandRows(entitySnapshots, attributeConfigs);
        var mapRows = BuildMapSpreadRows(entitySnapshots, LoadTotalCellCount(connection));
        var flags = BuildFlags(generatorRows, plannerRows, mapRows, lifecycleRows, resourceRows, mapGenerationRows);

        return new MonteCarloAnalysisReport
        {
            Overview = overview,
            Flags = flags,
            GeneratorBalance = generatorRows,
            GeneratedSubtypeCoverage = generatedCoverage,
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

    private void WriteArtifacts(MonteCarloAnalysisReport report, string analysisDirectory)
    {
        Directory.CreateDirectory(analysisDirectory);
        CsvFileWriter.WriteCsv(Path.Combine(analysisDirectory, "generator_balance.csv"), report.GeneratorBalance);
        CsvFileWriter.WriteCsv(Path.Combine(analysisDirectory, "generated_subtype_coverage.csv"), report.GeneratedSubtypeCoverage);
        CsvFileWriter.WriteCsv(Path.Combine(analysisDirectory, "planner_pressure.csv"), report.PlannerPressure);
        CsvFileWriter.WriteCsv(Path.Combine(analysisDirectory, "process_pressure.csv"), report.ProcessPressure);
        CsvFileWriter.WriteCsv(Path.Combine(analysisDirectory, "attribute_bands.csv"), report.AttributeBands);
        CsvFileWriter.WriteCsv(Path.Combine(analysisDirectory, "map_spread.csv"), report.MapSpread);
        CsvFileWriter.WriteCsv(Path.Combine(analysisDirectory, "map_generation.csv"), report.MapGeneration);
        CsvFileWriter.WriteCsv(Path.Combine(analysisDirectory, "terrain_occupancy.csv"), report.TerrainOccupancy);
        CsvFileWriter.WriteCsv(Path.Combine(analysisDirectory, "lifecycle.csv"), report.Lifecycle);
        CsvFileWriter.WriteCsv(Path.Combine(analysisDirectory, "resource_pressure.csv"), report.ResourcePressure);
        CsvFileWriter.WriteCsv(Path.Combine(analysisDirectory, "flags.csv"), report.Flags);
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
            return dbConnection;
        }

        var connection = new DuckDBConnection("Data Source=:memory:");
        connection.Open();

        foreach (var table in RequiredTables)
        {
            var path = Path.Combine(batchDirectory, $"{table}.parquet");
            SqlFileHelpers.Execute(
                connection,
                $"CREATE VIEW {SqlFileHelpers.QuoteIdentifier(table)} AS SELECT * FROM read_parquet({SqlFileHelpers.QuoteString(path)});");
        }

        return connection;
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
        const string sql = """
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

        var observed = Query(connection, sql, reader => new ObservedGeneratorRow(
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

    private List<ProcessPressureRow> LoadProcessPressureRows(DuckDBConnection connection)
    {
        const string summarySql = """
            SELECT
                kind,
                process_name,
                COUNT(*) AS candidate_evaluations,
                SUM(CASE WHEN was_selected THEN 1 ELSE 0 END) AS selected_count,
                COALESCE(AVG(CAST(score AS DOUBLE)), 0.0) AS average_score,
                COALESCE(AVG(CASE WHEN precondition_passed THEN 1.0 ELSE 0.0 END), 0.0) AS precondition_pass_rate,
                COALESCE(AVG(CASE WHEN on_cooldown THEN 1.0 ELSE 0.0 END), 0.0) AS cooldown_rate
            FROM process_candidates
            GROUP BY kind, process_name;
            """;

        const string failureSql = """
            SELECT
                kind,
                process_name,
                precondition_diagnostic,
                COUNT(*) AS failure_count
            FROM process_candidates
            WHERE precondition_passed = false
            GROUP BY kind, process_name, precondition_diagnostic;
            """;

        var failures = Query(connection, failureSql, reader => new FailureDiagnosticRow(
            Kind: ReadString(reader, "kind"),
            ProcessName: ReadString(reader, "process_name"),
            Diagnostic: ReadString(reader, "precondition_diagnostic"),
            Count: ReadInt(reader, "failure_count")))
            .GroupBy(row => (row.Kind, row.ProcessName))
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(row => row.Count)
                    .ThenBy(row => row.Diagnostic, StringComparer.Ordinal)
                    .First().Diagnostic);

        var observed = Query(connection, summarySql, reader => new ObservedProcessRow(
            Kind: ReadString(reader, "kind"),
            ProcessName: ReadString(reader, "process_name"),
            CandidateEvaluations: ReadInt(reader, "candidate_evaluations"),
            SelectedCount: ReadInt(reader, "selected_count"),
            AverageScore: ReadDouble(reader, "average_score"),
            PreconditionPassRate: ReadDouble(reader, "precondition_pass_rate"),
            CooldownRate: ReadDouble(reader, "cooldown_rate")))
            .ToDictionary(row => (row.Kind, row.ProcessName), row => row);

        var rows = _domainConfig.Processes
            .Where(process => process.Role == DefinitionRole.Process)
            .Select(process =>
            {
                var observedRow = observed.GetValueOrDefault((process.TargetKind.Value, process.Name)) ??
                                  ObservedProcessRow.Empty(process.TargetKind.Value, process.Name);
                var topFailure = failures.GetValueOrDefault((process.TargetKind.Value, process.Name)) ?? string.Empty;

                return new ProcessPressureRow(
                    Kind: process.TargetKind.Value,
                    ProcessName: process.Name,
                    CandidateEvaluations: observedRow.CandidateEvaluations,
                    SelectedCount: observedRow.SelectedCount,
                    SelectionRateWhenOffered: observedRow.CandidateEvaluations == 0
                        ? 0d
                        : observedRow.SelectedCount / (double)observedRow.CandidateEvaluations,
                    AverageScore: observedRow.AverageScore,
                    PreconditionPassRate: observedRow.PreconditionPassRate,
                    CooldownRate: observedRow.CooldownRate,
                    TopFailureDiagnostic: topFailure);
            });

        return rows
            .OrderBy(row => row.Kind, StringComparer.Ordinal)
            .ThenByDescending(row => row.SelectedCount)
            .ThenByDescending(row => row.CandidateEvaluations)
            .ThenBy(row => row.ProcessName, StringComparer.Ordinal)
            .ToList();
    }

    private List<PlannerPressureRow> LoadPlannerPressureRows(
        DuckDBConnection connection,
        List<ProcessPressureRow> processRows)
    {
        const string sql = """
            SELECT
                kind,
                COUNT(*) AS evaluations,
                SUM(CASE WHEN is_stale THEN 1 ELSE 0 END) AS stale_evaluations,
                SUM(CASE WHEN executed_process_name IS NULL THEN 1 ELSE 0 END) AS no_execution_evaluations,
                SUM(CASE WHEN executed_process_name IS NOT NULL THEN 1 ELSE 0 END) AS executed_evaluations,
                COALESCE(AVG(CAST(candidate_count AS DOUBLE)), 0.0) AS average_candidate_count
            FROM process_events
            GROUP BY kind;
            """;

        var observed = Query(connection, sql, reader => new ObservedPlannerRow(
            Kind: ReadString(reader, "kind"),
            Evaluations: ReadInt(reader, "evaluations"),
            StaleEvaluations: ReadInt(reader, "stale_evaluations"),
            NoExecutionEvaluations: ReadInt(reader, "no_execution_evaluations"),
            ExecutedEvaluations: ReadInt(reader, "executed_evaluations"),
            AverageCandidateCount: ReadDouble(reader, "average_candidate_count")))
            .ToDictionary(row => row.Kind, row => row, StringComparer.Ordinal);

        var processByKind = processRows
            .GroupBy(row => row.Kind)
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

        var endpoints = Query(connection, endpointSql, reader => new KeyValuePair<string, (double Initial, double Final)>(
                ReadString(reader, "resource_type"),
                (
                    ReadDouble(reader, "average_initial_total"),
                    ReadDouble(reader, "average_final_total"))))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

        return Query(connection, overviewSql, reader => new ResourcePressureRow(
                ResourceType: ReadString(reader, "resource_type"),
                AverageTotalOverTime: ReadDouble(reader, "average_total_over_time"),
                AverageInitialTotal: endpoints.GetValueOrDefault(ReadString(reader, "resource_type")).Initial,
                AverageFinalTotal: endpoints.GetValueOrDefault(ReadString(reader, "resource_type")).Final,
                FinalToInitialRatio: ComputeRatio(
                    endpoints.GetValueOrDefault(ReadString(reader, "resource_type")).Final,
                    endpoints.GetValueOrDefault(ReadString(reader, "resource_type")).Initial),
                LowestCellStock: ReadDouble(reader, "lowest_cell_stock")))
            .OrderBy(row => row.ResourceType, StringComparer.Ordinal)
            .ToList();
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

    private List<LifecycleSummaryRow> LoadLifecycleRows(DuckDBConnection connection, int runCount)
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

        const string deathSql = """
            SELECT
                kind,
                COUNT(*) AS deaths,
                COALESCE(AVG(CAST(tick AS DOUBLE)), 0.0) AS average_death_tick
            FROM lifecycle_events
            WHERE event_type = 'death'
            GROUP BY kind;
            """;

        var finals = Query(connection, finalSql, reader => new ObservedLifecycleRow(
                Kind: ReadString(reader, "kind"),
                Deaths: 0,
                AverageDeathTick: 0d,
                AverageFinalActive: ReadDouble(reader, "average_final_active"),
                AverageFinalHistorical: ReadDouble(reader, "average_final_historical"),
                AverageFinalHealth: ReadDouble(reader, "average_final_health")))
            .ToDictionary(row => row.Kind, row => row, StringComparer.Ordinal);

        var deaths = Query(connection, deathSql, reader => new KeyValuePair<string, (int Deaths, double AvgTick)>(
                ReadString(reader, "kind"),
                (ReadInt(reader, "deaths"), ReadDouble(reader, "average_death_tick"))))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

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

    private int LoadTotalCellCount(DuckDBConnection connection)
    {
        const string sql = """
            SELECT COALESCE(SUM(cell_count), 0) AS total_cells
            FROM map_snapshots
            WHERE run_id = (SELECT run_id FROM runs ORDER BY run_id LIMIT 1)
              AND tick = (
                    SELECT MIN(tick)
                    FROM map_snapshots
                    WHERE run_id = (SELECT run_id FROM runs ORDER BY run_id LIMIT 1)
              );
            """;

        return QuerySingle(connection, sql, reader => ReadInt(reader, "total_cells"));
    }

    private static List<AttributeBandSummaryRow> BuildAttributeBandRows(
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

        return aggregates
            .OrderBy(pair => pair.Key.Kind, StringComparer.Ordinal)
            .ThenBy(pair => pair.Key.Subtype, StringComparer.Ordinal)
            .ThenBy(pair => pair.Key.Attribute, StringComparer.Ordinal)
            .Select(pair =>
            {
                var acc = pair.Value;
                var denom = Math.Max(1, acc.SampleCount);
                return new AttributeBandSummaryRow(
                    Kind: pair.Key.Kind,
                    Subtype: pair.Key.Subtype,
                    AttributeName: pair.Key.Attribute,
                    SampleCount: acc.SampleCount,
                    AverageValue: acc.ValueSum / denom,
                    AverageNormalized: acc.NormalizedSum / denom,
                    MinimumObserved: acc.SampleCount == 0 ? 0d : acc.MinObserved,
                    MaximumObserved: acc.SampleCount == 0 ? 0d : acc.MaxObserved,
                    CriticalShare: acc.CriticalCount / (double)denom,
                    LowShare: acc.LowCount / (double)denom,
                    MidShare: acc.MidCount / (double)denom,
                    HighShare: acc.HighCount / (double)denom);
            })
            .ToList();
    }

    private static List<MapSpreadSummaryRow> BuildMapSpreadRows(
        List<EntitySnapshotRecord> snapshots,
        int totalCellCount)
    {
        var rows = new List<MapSpreadSummaryRow>();

        foreach (var runGroup in snapshots.GroupBy(snapshot => snapshot.RunId))
        {
            var orderedTicks = runGroup.Select(snapshot => snapshot.Tick).Distinct().OrderBy(tick => tick).ToList();
            if (orderedTicks.Count == 0)
            {
                continue;
            }

            var seedTick = orderedTicks[0];
            var finalTick = orderedTicks[^1];
            var seedCoords = runGroup
                .Where(snapshot => snapshot.Tick == seedTick && string.Equals(snapshot.Status, "Active", StringComparison.Ordinal))
                .Select(snapshot => new HexCoord(snapshot.PhysicalQ, snapshot.PhysicalR))
                .Distinct()
                .ToList();

            var finalEntities = runGroup
                .Where(snapshot => snapshot.Tick == finalTick && string.Equals(snapshot.Status, "Active", StringComparison.Ordinal))
                .ToList();

            var occupiedCells = finalEntities
                .Select(snapshot => (snapshot.PhysicalQ, snapshot.PhysicalR))
                .Distinct()
                .ToList();

            var bboxArea = 0;
            if (occupiedCells.Count > 0)
            {
                var minQ = occupiedCells.Min(cell => cell.PhysicalQ);
                var maxQ = occupiedCells.Max(cell => cell.PhysicalQ);
                var minR = occupiedCells.Min(cell => cell.PhysicalR);
                var maxR = occupiedCells.Max(cell => cell.PhysicalR);
                bboxArea = (maxQ - minQ + 1) * (maxR - minR + 1);
            }

            var distances = finalEntities
                .Select(snapshot =>
                {
                    var coord = new HexCoord(snapshot.PhysicalQ, snapshot.PhysicalR);
                    if (seedCoords.Count == 0)
                    {
                        return 0;
                    }

                    return seedCoords.Min(seed => HexCoord.Distance(coord, seed));
                })
                .ToList();

            rows.Add(new MapSpreadSummaryRow(
                RunId: runGroup.Key,
                SnapshotTick: finalTick,
                ActiveEntities: finalEntities.Count,
                OccupiedCells: occupiedCells.Count,
                OccupiedCellShare: totalCellCount == 0 ? 0d : occupiedCells.Count / (double)totalCellCount,
                BoundingBoxArea: bboxArea,
                OccupancyWithinBoundingBox: bboxArea == 0 ? 0d : occupiedCells.Count / (double)bboxArea,
                MeanDistanceFromSeed: distances.Count == 0 ? 0d : distances.Average(),
                MaxDistanceFromSeed: distances.Count == 0 ? 0d : distances.Max(),
                Anisotropy: ComputeAnisotropy(finalEntities)));
        }

        return rows
            .OrderBy(row => row.RunId, StringComparer.Ordinal)
            .ToList();
    }

    private static List<AnalysisFlagRow> BuildFlags(
        List<GeneratorAnalysisRow> generatorRows,
        List<PlannerPressureRow> plannerRows,
        List<MapSpreadSummaryRow> mapRows,
        List<LifecycleSummaryRow> lifecycleRows,
        List<ResourcePressureRow> resourceRows,
        List<MapGenerationSummaryRow> mapGenerationRows)
    {
        var flags = new List<AnalysisFlagRow>();

        var degenerateCount = mapGenerationRows.Sum(row => row.EventCount);
        if (degenerateCount > 0)
        {
            var top = mapGenerationRows
                .OrderByDescending(row => row.EventCount)
                .ThenBy(row => row.Phase, StringComparer.Ordinal)
                .First();
            flags.Add(new AnalysisFlagRow(
                Category: "map_generation",
                Severity: "warning",
                Message: FormattableString.Invariant(
                    $"Procedural map degeneracy occurred {degenerateCount} times. Top cause: {top.Phase} -> {top.Description}.")));
        }

        var zeroFire = generatorRows
            .Where(row => row.Pulses > 0 && row.FiredCount == 0)
            .OrderByDescending(row => row.Pulses)
            .ThenBy(row => row.GeneratorId, StringComparer.Ordinal)
            .Take(5)
            .ToList();
        if (zeroFire.Count > 0)
        {
            flags.Add(new AnalysisFlagRow(
                Category: "generator_balance",
                Severity: "warning",
                Message: $"Configured generators with pulse activity but zero fires: {string.Join(", ", zeroFire.Select(row => row.GeneratorId))}."));
        }

        var totalFires = generatorRows.Sum(row => row.FiredCount);
        var topGenerator = generatorRows.OrderByDescending(row => row.FiredCount).FirstOrDefault();
        if (totalFires > 0 && topGenerator is not null)
        {
            var topShare = topGenerator.FiredCount / (double)totalFires;
            if (topShare >= 0.35d)
            {
                flags.Add(new AnalysisFlagRow(
                    Category: "generator_balance",
                    Severity: "warning",
                    Message: FormattableString.Invariant(
                        $"{topGenerator.GeneratorId} accounts for {topShare:P1} of generator fires across the batch.")));
            }
        }

        foreach (var planner in plannerRows.Where(row => row.StaleRate >= 0.25d || row.NoExecutionRate >= 0.4d))
        {
            flags.Add(new AnalysisFlagRow(
                Category: "planner",
                Severity: "warning",
                Message: FormattableString.Invariant(
                    $"{planner.Kind} planner is sticky: stale={planner.StaleRate:P1}, no_execution={planner.NoExecutionRate:P1}, top_process='{planner.TopExecutedProcess}'.")));
        }

        var averageOccupiedShare = mapRows.Count == 0 ? 0d : mapRows.Average(row => row.OccupiedCellShare);
        var averageAnisotropy = mapRows.Count == 0 ? 0d : mapRows.Average(row => row.Anisotropy);
        if (averageOccupiedShare > 0d && averageOccupiedShare <= 0.15d)
        {
            flags.Add(new AnalysisFlagRow(
                Category: "map",
                Severity: "warning",
                Message: FormattableString.Invariant(
                    $"Final occupied physical map share averages {averageOccupiedShare:P1}; expansion is still sparse.")));
        }

        if (averageAnisotropy >= 3.5d)
        {
            flags.Add(new AnalysisFlagRow(
                Category: "map",
                Severity: "warning",
                Message: FormattableString.Invariant(
                    $"Final physical placement anisotropy averages {averageAnisotropy:F2}; the map is still corridor-biased.")));
        }

        var totalDeaths = lifecycleRows.Sum(row => row.Deaths);
        if (totalDeaths == 0)
        {
            flags.Add(new AnalysisFlagRow(
                Category: "lifecycle",
                Severity: "warning",
                Message: "No entities died across the analyzed batch."));
        }
        else
        {
            var noDeathKinds = lifecycleRows
                .Where(row => row.Deaths == 0)
                .Select(row => row.Kind)
                .OrderBy(kind => kind, StringComparer.Ordinal)
                .ToList();
            if (noDeathKinds.Count > 0)
            {
                flags.Add(new AnalysisFlagRow(
                    Category: "lifecycle",
                    Severity: "info",
                    Message: $"Kinds with zero recorded deaths: {string.Join(", ", noDeathKinds)}."));
            }
        }

        if (resourceRows.Count > 0 && resourceRows.All(row => row.FinalToInitialRatio >= 0.9d))
        {
            flags.Add(new AnalysisFlagRow(
                Category: "resources",
                Severity: "info",
                Message: "Global resource stocks barely deplete across the batch; carrying-capacity pressure may still be too soft."));
        }

        return flags;
    }

    private string RenderMarkdown(MonteCarloAnalysisReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Monte Carlo Tuning Summary");
        builder.AppendLine();
        builder.AppendLine("## Batch");
        builder.AppendLine(FormattableString.Invariant($"- Runs: {report.Overview.RunCount}"));
        builder.AppendLine(FormattableString.Invariant($"- Max tick: {report.Overview.MaxTick}"));
        builder.AppendLine(FormattableString.Invariant($"- Total generator fires: {report.Overview.TotalGeneratorFires}"));
        builder.AppendLine(FormattableString.Invariant($"- Avg fires/run: {report.Overview.AverageFiresPerRun:F2}"));
        builder.AppendLine(FormattableString.Invariant($"- Avg final active entities: {report.Overview.AverageFinalActiveEntities:F2}"));
        builder.AppendLine(FormattableString.Invariant($"- Avg final historical entities: {report.Overview.AverageFinalHistoricalEntities:F2}"));
        builder.AppendLine(FormattableString.Invariant($"- Avg final physical spread: {report.Overview.AverageFinalPhysicalSpread:F2}"));
        builder.AppendLine(FormattableString.Invariant($"- Avg final semantic spread: {report.Overview.AverageFinalSemanticSpread:F2}"));
        builder.AppendLine();

        builder.AppendLine("## Flags");
        if (report.Flags.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var flag in report.Flags)
            {
                builder.AppendLine(FormattableString.Invariant($"- [{flag.Severity}] {flag.Category}: {flag.Message}"));
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Map Generation");
        AppendTable(
            builder,
            report.MapGeneration.Take(10),
            ("phase", row => row.Phase),
            ("events", row => row.EventCount.ToString(CultureInfo.InvariantCulture)),
            ("runs", row => row.AffectedRuns.ToString(CultureInfo.InvariantCulture)),
            ("max_attempt", row => row.MaxAttempt.ToString(CultureInfo.InvariantCulture)),
            ("description", row => row.Description),
            ("reasons", row => string.IsNullOrWhiteSpace(row.Reasons) ? "-" : row.Reasons));

        builder.AppendLine();
        builder.AppendLine("## Generator Balance");
        AppendTable(
            builder,
            report.GeneratorBalance.Take(8),
            ("generator", row => row.GeneratorId),
            ("fires", row => row.FiredCount.ToString(CultureInfo.InvariantCulture)),
            ("eligible", row => row.EligiblePulses.ToString(CultureInfo.InvariantCulture)),
            ("select_rate", row => FormatPercent(row.SelectionRateWhenEligible)),
            ("blocker", row => row.PrimaryBlocker));

        builder.AppendLine();
        builder.AppendLine("## Planner Pressure");
        AppendTable(
            builder,
            report.PlannerPressure,
            ("kind", row => row.Kind),
            ("stale", row => FormatPercent(row.StaleRate)),
            ("no_exec", row => FormatPercent(row.NoExecutionRate)),
            ("avg_candidates", row => row.AverageCandidateCount.ToString("F2", CultureInfo.InvariantCulture)),
            ("top_process", row => row.TopExecutedProcess),
            ("top_share", row => FormatPercent(row.TopProcessShareOfExecutions)));

        builder.AppendLine();
        builder.AppendLine("## Process Pressure");
        AppendTable(
            builder,
            report.ProcessPressure.Take(12),
            ("kind", row => row.Kind),
            ("process", row => row.ProcessName),
            ("selected", row => row.SelectedCount.ToString(CultureInfo.InvariantCulture)),
            ("offered", row => row.CandidateEvaluations.ToString(CultureInfo.InvariantCulture)),
            ("select_rate", row => FormatPercent(row.SelectionRateWhenOffered)),
            ("precond", row => FormatPercent(row.PreconditionPassRate)),
            ("top_failure", row => string.IsNullOrWhiteSpace(row.TopFailureDiagnostic) ? "-" : row.TopFailureDiagnostic));

        builder.AppendLine();
        builder.AppendLine("## Attribute Bands");
        AppendTable(
            builder,
            report.AttributeBands
                .OrderByDescending(row => row.CriticalShare)
                .ThenByDescending(row => row.LowShare)
                .Take(12),
            ("kind", row => row.Kind),
            ("subtype", row => row.Subtype),
            ("attribute", row => row.AttributeName),
            ("critical", row => FormatPercent(row.CriticalShare)),
            ("low", row => FormatPercent(row.LowShare)),
            ("mid", row => FormatPercent(row.MidShare)),
            ("high", row => FormatPercent(row.HighShare)));

        builder.AppendLine();
        builder.AppendLine("## Map Spread");
        AppendTable(
            builder,
            report.MapSpread.Take(10),
            ("run", row => row.RunId),
            ("occupied", row => row.OccupiedCells.ToString(CultureInfo.InvariantCulture)),
            ("share", row => FormatPercent(row.OccupiedCellShare)),
            ("bbox_fill", row => FormatPercent(row.OccupancyWithinBoundingBox)),
            ("mean_seed_dist", row => row.MeanDistanceFromSeed.ToString("F2", CultureInfo.InvariantCulture)),
            ("anisotropy", row => row.Anisotropy.ToString("F2", CultureInfo.InvariantCulture)));

        builder.AppendLine();
        builder.AppendLine("## Lifecycle");
        AppendTable(
            builder,
            report.Lifecycle,
            ("kind", row => row.Kind),
            ("deaths", row => row.Deaths.ToString(CultureInfo.InvariantCulture)),
            ("deaths/run", row => row.DeathsPerRun.ToString("F2", CultureInfo.InvariantCulture)),
            ("avg_death_tick", row => row.AverageDeathTick.ToString("F2", CultureInfo.InvariantCulture)),
            ("final_active", row => row.AverageFinalActive.ToString("F2", CultureInfo.InvariantCulture)),
            ("final_health", row => row.AverageFinalHealth.ToString("F2", CultureInfo.InvariantCulture)));

        builder.AppendLine();
        builder.AppendLine("## Resource Pressure");
        AppendTable(
            builder,
            report.ResourcePressure,
            ("resource", row => row.ResourceType),
            ("initial_total", row => row.AverageInitialTotal.ToString("F2", CultureInfo.InvariantCulture)),
            ("avg_total", row => row.AverageTotalOverTime.ToString("F2", CultureInfo.InvariantCulture)),
            ("final_total", row => row.AverageFinalTotal.ToString("F2", CultureInfo.InvariantCulture)),
            ("final/initial", row => FormatPercent(row.FinalToInitialRatio)),
            ("lowest_cell", row => row.LowestCellStock.ToString("F2", CultureInfo.InvariantCulture)));

        return builder.ToString();
    }

    private static void AppendTable<T>(
        StringBuilder builder,
        IEnumerable<T> rows,
        params (string Header, Func<T, string> Cell)[] columns)
    {
        builder.Append('|');
        builder.Append(string.Join('|', columns.Select(column => column.Header)));
        builder.AppendLine("|");
        builder.Append('|');
        builder.Append(string.Join('|', columns.Select(_ => "---")));
        builder.AppendLine("|");

        foreach (var row in rows)
        {
            builder.Append('|');
            builder.Append(string.Join('|', columns.Select(column => EscapeMarkdownCell(column.Cell(row)))));
            builder.AppendLine("|");
        }
    }

    private static string EscapeMarkdownCell(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal);

    private static string FormatPercent(double value) =>
        value.ToString("P1", CultureInfo.InvariantCulture);

    private static IEnumerable<(string AttributeName, double Value)> ParseAttributes(string attributes)
    {
        if (string.IsNullOrWhiteSpace(attributes))
        {
            yield break;
        }

        foreach (var segment in attributes.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = segment.Split('=', 2);
            if (parts.Length != 2 ||
                !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                continue;
            }

            yield return (parts[0], value);
        }
    }

    private static double Normalize(double value, float min, float max)
    {
        if (Math.Abs(max - min) < double.Epsilon)
        {
            return 0d;
        }

        return Math.Clamp((value - min) / (max - min), 0d, 1d);
    }

    private static double ComputeAnisotropy(IReadOnlyList<EntitySnapshotRecord> snapshots)
    {
        if (snapshots.Count < 2)
        {
            return 1d;
        }

        var meanQ = snapshots.Average(snapshot => snapshot.PhysicalQ);
        var meanR = snapshots.Average(snapshot => snapshot.PhysicalR);
        var sxx = snapshots.Average(snapshot => Math.Pow(snapshot.PhysicalQ - meanQ, 2));
        var syy = snapshots.Average(snapshot => Math.Pow(snapshot.PhysicalR - meanR, 2));
        var sxy = snapshots.Average(snapshot => (snapshot.PhysicalQ - meanQ) * (snapshot.PhysicalR - meanR));

        var trace = sxx + syy;
        var determinant = (sxx * syy) - (sxy * sxy);
        var discriminant = Math.Sqrt(Math.Max(0d, (trace * trace * 0.25d) - determinant));
        var lambda1 = (trace * 0.5d) + discriminant;
        var lambda2 = (trace * 0.5d) - discriminant;

        if (lambda1 <= 1e-9d)
        {
            return 1d;
        }

        if (lambda2 <= 1e-9d)
        {
            return lambda1;
        }

        return lambda1 / lambda2;
    }

    private static double ComputeRatio(double numerator, double denominator)
    {
        if (denominator <= 0d)
        {
            return numerator <= 0d ? 1d : 0d;
        }

        return numerator / denominator;
    }

    private static string PrimaryBlocker(ObservedGeneratorRow row)
    {
        var candidates = new (string Label, int Count)[]
        {
            ("inactive", row.InactiveRejected),
            ("subtype", row.SubtypeRejected),
            ("saturation", row.SaturationRejected),
            ("secondary_target", row.SecondaryTargetRejected),
            ("wilderness", row.WildernessRejected),
            ("trigger", row.TriggerRejected),
        };

        var top = candidates
            .OrderByDescending(candidate => candidate.Count)
            .ThenBy(candidate => candidate.Label, StringComparer.Ordinal)
            .First();

        return top.Count == 0 ? "none" : top.Label;
    }

    private static T QuerySingle<T>(DuckDBConnection connection, string sql, Func<DbDataReader, T> map)
    {
        var rows = Query(connection, sql, map);
        return rows.Count == 0 ? throw new InvalidOperationException("Expected one row.") : rows[0];
    }

    private static List<T> Query<T>(DuckDBConnection connection, string sql, Func<DbDataReader, T> map)
    {
        using var command = connection.CreateCommand();
#pragma warning disable CA2100
        command.CommandText = sql;
#pragma warning restore CA2100
        using var reader = command.ExecuteReader();
        var rows = new List<T>();
        while (reader.Read())
        {
            rows.Add(map(reader));
        }

        return rows;
    }

    private static int ReadInt(DbDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        if (reader.IsDBNull(ordinal))
        {
            return 0;
        }

        return reader.GetValue(ordinal) switch
        {
            int value => value,
            long value => checked((int)value),
            short value => value,
            byte value => value,
            BigInteger value => (int)value,
            decimal value => decimal.ToInt32(value),
            float value => checked((int)value),
            double value => checked((int)value),
            string value => int.Parse(value, CultureInfo.InvariantCulture),
            IConvertible convertible => convertible.ToInt32(CultureInfo.InvariantCulture),
            var value => int.Parse(value.ToString() ?? "0", CultureInfo.InvariantCulture),
        };
    }

    private static double ReadDouble(DbDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        if (reader.IsDBNull(ordinal))
        {
            return 0d;
        }

        return reader.GetValue(ordinal) switch
        {
            double value => value,
            float value => value,
            decimal value => (double)value,
            int value => value,
            long value => value,
            short value => value,
            byte value => value,
            BigInteger value => (double)value,
            string value => double.Parse(value, CultureInfo.InvariantCulture),
            IConvertible convertible => convertible.ToDouble(CultureInfo.InvariantCulture),
            var value => double.Parse(value.ToString() ?? "0", CultureInfo.InvariantCulture),
        };
    }

    private static string ReadString(DbDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
    }

    private sealed class AttributeBandAccumulator
    {
        public int SampleCount { get; set; }
        public double ValueSum { get; set; }
        public double NormalizedSum { get; set; }
        public double MinObserved { get; set; } = double.PositiveInfinity;
        public double MaxObserved { get; set; } = double.NegativeInfinity;
        public int CriticalCount { get; set; }
        public int LowCount { get; set; }
        public int MidCount { get; set; }
        public int HighCount { get; set; }
    }

    private sealed record ObservedGeneratorRow(
        string GeneratorId,
        int Pulses,
        int EligiblePulses,
        int BlockedPulses,
        int EligibleNotSelectedPulses,
        int FiredCount,
        int FireFailedPulses,
        double AverageTargetPool,
        double AverageCandidateCount,
        int InactiveRejected,
        int SubtypeRejected,
        int SaturationRejected,
        int SecondaryTargetRejected,
        int WildernessRejected,
        int TriggerRejected,
        int CreatedEntityCount,
        int CreatedRelationshipCount)
    {
        public static ObservedGeneratorRow Empty(string generatorId) => new(
            generatorId,
            0,
            0,
            0,
            0,
            0,
            0,
            0d,
            0d,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0);
    }

    private sealed record ObservedProcessRow(
        string Kind,
        string ProcessName,
        int CandidateEvaluations,
        int SelectedCount,
        double AverageScore,
        double PreconditionPassRate,
        double CooldownRate)
    {
        public static ObservedProcessRow Empty(string kind, string processName) => new(
            kind,
            processName,
            0,
            0,
            0d,
            0d,
            0d);
    }

    private sealed record FailureDiagnosticRow(string Kind, string ProcessName, string Diagnostic, int Count);

    private sealed record ObservedPlannerRow(
        string Kind,
        int Evaluations,
        int StaleEvaluations,
        int NoExecutionEvaluations,
        int ExecutedEvaluations,
        double AverageCandidateCount)
    {
        public static ObservedPlannerRow Empty(string kind) => new(kind, 0, 0, 0, 0, 0d);
    }

    private sealed record ObservedLifecycleRow(
        string Kind,
        int Deaths,
        double AverageDeathTick,
        double AverageFinalActive,
        double AverageFinalHistorical,
        double AverageFinalHealth)
    {
        public static ObservedLifecycleRow Empty(string kind) => new(kind, 0, 0d, 0d, 0d, 0d);
    }

    private sealed record EntitySnapshotRecord(
        string RunId,
        int Tick,
        int EntityId,
        string Kind,
        string Subtype,
        string Status,
        int PhysicalQ,
        int PhysicalR,
        string Attributes);
}

internal sealed class MonteCarloAnalysisReport
{
    public required BatchOverviewRow Overview { get; init; }
    public required IReadOnlyList<AnalysisFlagRow> Flags { get; init; }
    public required IReadOnlyList<GeneratorAnalysisRow> GeneratorBalance { get; init; }
    public required IReadOnlyList<GeneratedSubtypeCoverageRow> GeneratedSubtypeCoverage { get; init; }
    public required IReadOnlyList<PlannerPressureRow> PlannerPressure { get; init; }
    public required IReadOnlyList<ProcessPressureRow> ProcessPressure { get; init; }
    public required IReadOnlyList<AttributeBandSummaryRow> AttributeBands { get; init; }
    public required IReadOnlyList<MapSpreadSummaryRow> MapSpread { get; init; }
    public required IReadOnlyList<MapGenerationSummaryRow> MapGeneration { get; init; }
    public required IReadOnlyList<TerrainOccupancyRow> TerrainOccupancy { get; init; }
    public required IReadOnlyList<LifecycleSummaryRow> Lifecycle { get; init; }
    public required IReadOnlyList<ResourcePressureRow> ResourcePressure { get; init; }
}

internal sealed record BatchOverviewRow(
    int RunCount,
    int MaxTick,
    int TotalGeneratorFires,
    double AverageFiresPerRun,
    double AverageFinalActiveEntities,
    double AverageFinalHistoricalEntities,
    double AverageFinalPhysicalSpread,
    double AverageFinalSemanticSpread,
    double AverageFinalUncoveredCells);

internal sealed record AnalysisFlagRow(string Category, string Severity, string Message);

internal sealed record GeneratorAnalysisRow(
    string GeneratorId,
    string GeneratorName,
    string TargetKind,
    int Pulses,
    int EligiblePulses,
    int BlockedPulses,
    int EligibleNotSelectedPulses,
    int FiredCount,
    int FireFailedPulses,
    double FireRatePerRun,
    double SelectionRateWhenEligible,
    double AverageTargetPool,
    double AverageCandidateCount,
    int InactiveRejected,
    int SubtypeRejected,
    int SaturationRejected,
    int SecondaryTargetRejected,
    int WildernessRejected,
    int TriggerRejected,
    string PrimaryBlocker,
    int CreatedEntityCount,
    int CreatedRelationshipCount);

internal sealed record GeneratedSubtypeCoverageRow(
    string GeneratorId,
    string Kind,
    string Subtype,
    int ProducedCount);

internal sealed record PlannerPressureRow(
    string Kind,
    int Evaluations,
    double StaleRate,
    double NoExecutionRate,
    double AverageCandidateCount,
    string TopExecutedProcess,
    double TopProcessShareOfExecutions);

internal sealed record ProcessPressureRow(
    string Kind,
    string ProcessName,
    int CandidateEvaluations,
    int SelectedCount,
    double SelectionRateWhenOffered,
    double AverageScore,
    double PreconditionPassRate,
    double CooldownRate,
    string TopFailureDiagnostic);

internal sealed record AttributeBandSummaryRow(
    string Kind,
    string Subtype,
    string AttributeName,
    int SampleCount,
    double AverageValue,
    double AverageNormalized,
    double MinimumObserved,
    double MaximumObserved,
    double CriticalShare,
    double LowShare,
    double MidShare,
    double HighShare);

internal sealed record MapSpreadSummaryRow(
    string RunId,
    int SnapshotTick,
    int ActiveEntities,
    int OccupiedCells,
    double OccupiedCellShare,
    int BoundingBoxArea,
    double OccupancyWithinBoundingBox,
    double MeanDistanceFromSeed,
    double MaxDistanceFromSeed,
    double Anisotropy);

internal sealed record MapGenerationSummaryRow(
    string Phase,
    string Description,
    string Reasons,
    int EventCount,
    int AffectedRuns,
    int RetryingEvents,
    int MaxAttempt);

internal sealed record TerrainOccupancyRow(
    string Terrain,
    double AverageFinalActiveEntities,
    double AverageNamedCoverageShare);

internal sealed record LifecycleSummaryRow(
    string Kind,
    int Deaths,
    double DeathsPerRun,
    double AverageDeathTick,
    double AverageFinalActive,
    double AverageFinalHistorical,
    double AverageFinalHealth);

internal sealed record ResourcePressureRow(
    string ResourceType,
    double AverageTotalOverTime,
    double AverageInitialTotal,
    double AverageFinalTotal,
    double FinalToInitialRatio,
    double LowestCellStock);
