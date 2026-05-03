namespace MonteCarloTool;

using System.Data.Common;
using System.Globalization;
using System.Numerics;

using DuckDB.NET.Data;

using Engine.Constants;

internal sealed partial class MonteCarloBatchAnalyzer
{
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

    private static string PlaneName(int planeCode) =>
        planeCode switch
        {
            PlaneCodes.Physical => "physical",
            PlaneCodes.Semantic => "semantic",
            _ => planeCode.ToString(CultureInfo.InvariantCulture),
        };

    private static string LowAltitudeOperationName(int operationCode) =>
        operationCode switch
        {
            ProgramOpcodeCodes.MoveActor => "move_actor",
            ProgramOpcodeCodes.AddOwnedCellScalar => "add_owned_cell_scalar",
            ProgramOpcodeCodes.SubtractOtherCellScalar => "subtract_other_cell_scalar",
            ProgramOpcodeCodes.TryClaimCell => "try_claim_cell",
            ProgramOpcodeCodes.TryRelocateEntity => "try_relocate_entity",
            _ => operationCode.ToString(CultureInfo.InvariantCulture),
        };

    private static string LowAltitudeTargetModeName(int targetModeCode) =>
        targetModeCode switch
        {
            TargetSelectorCodes.Explore => "explore",
            TargetSelectorCodes.EntityKind => "entity_kind",
            TargetSelectorCodes.RelationshipTarget => "relationship_target",
            TargetSelectorCodes.Terrain => "terrain",
            TargetSelectorCodes.SelfMapValue => "self_map_value",
            TargetSelectorCodes.OtherMapValue => "other_map_value",
            _ => targetModeCode.ToString(CultureInfo.InvariantCulture),
        };

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

        return ConvertToInt(reader.GetValue(ordinal));
    }

    private static int ConvertToInt(object value)
    {
        if (value is BigInteger bigInteger)
        {
            return (int)bigInteger;
        }

        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static double ReadDouble(DbDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        if (reader.IsDBNull(ordinal))
        {
            return 0d;
        }

        return ConvertToDouble(reader.GetValue(ordinal));
    }

    private static double ConvertToDouble(object value)
    {
        if (value is BigInteger bigInteger)
        {
            return (double)bigInteger;
        }

        return Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }

    private static string ReadString(DbDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal)
            ? string.Empty
            : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture) ?? string.Empty;
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

    private sealed record ObservedActionRow(
        string Kind,
        string ActionName,
        int CandidateEvaluations,
        int SelectedCount,
        double AverageScore,
        double PreconditionPassRate,
        double CooldownRate)
    {
        public static ObservedActionRow Empty(string kind, string actionName) => new(
            kind,
            actionName,
            0,
            0,
            0d,
            0d,
            0d);
    }

    private sealed record TelemetryCoverage(
        int PlannerCandidateRows,
        int ActionExecutionRows,
        int MapSnapshotRows,
        int LocalityOpportunityRows,
        int PlannerTraceRows,
        int LowAltitudeTraceRows,
        int CellInfluenceRows);

    private sealed record OptionalRawTableDefinition(string TableName, string EmptySelectSql);

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
