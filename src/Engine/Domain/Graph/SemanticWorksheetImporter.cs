namespace Engine.Domain.Graph;

using System.Text;
using System.Text.RegularExpressions;
using Engine.Kinds;

public static partial class SemanticWorksheetImporter
{
    private const string EntityKindColumn = "Entity Kind";
    private const string AttributeColumn = "Attribute";
    private const string BoundsColumn = "Bounds";
    private const string ConservationColumn = "Conservation";
    private const string InflowsColumn = "Named Inflows (signature; form)";
    private const string OutflowsColumn = "Named Outflows (signature; form)";
    private const string ModulatorsColumn = "Rate Modulators (flow -> direction; form)";
    private const string FeedbackClassColumn = "Feedback Class";
    private const string ReadsColumn = "Existing Processes (reads)";
    private const string DirectWritesColumn = "Existing Processes (direct writes)";
    private const string IndirectWritesColumn = "Existing Processes (indirect writes)";
    private const string NotesColumn = "Notes";

    public static IReadOnlyList<SemanticImportDiagnostic> ImportDirectory(DefinitionGraph graph, string directoryPath)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        Reset(graph.SemanticFlows);

        if (!Directory.Exists(directoryPath))
        {
            graph.SemanticFlows.Diagnostics.Add(new SemanticImportDiagnostic
            {
                Severity = SemanticDiagnosticSeverity.Error,
                Code = "missing_directory",
                Message = $"Semantic worksheet directory '{directoryPath}' does not exist.",
                SourceWorksheet = directoryPath,
            });
            return graph.SemanticFlows.Diagnostics.ToArray();
        }

        var context = new ImportContext(graph);
        foreach (var filePath in Directory.GetFiles(directoryPath, "*.csv").OrderBy(static path => path, StringComparer.Ordinal))
        {
            ImportFile(context, filePath);
        }

        ValidateVerbConsistency(context);
        return graph.SemanticFlows.Diagnostics.ToArray();
    }

    private static void ImportFile(ImportContext context, string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        if (lines.Length == 0)
        {
            return;
        }

        var header = ParseCsvLine(lines[0]);
        var indexByHeader = header
            .Select((name, index) => (name, index))
            .ToDictionary(pair => pair.name, pair => pair.index, StringComparer.Ordinal);

        for (var rowIndex = 1; rowIndex < lines.Length; rowIndex++)
        {
            if (string.IsNullOrWhiteSpace(lines[rowIndex]))
            {
                continue;
            }

            var values = ParseCsvLine(lines[rowIndex]);
            if (values.All(static value => string.IsNullOrWhiteSpace(value)))
            {
                continue;
            }

            ImportRow(context, Path.GetFileName(filePath), rowIndex + 1, indexByHeader, values);
        }
    }

    private static void ImportRow(
        ImportContext context,
        string worksheetName,
        int rowNumber,
        IReadOnlyDictionary<string, int> indexByHeader,
        IReadOnlyList<string> values)
    {
        var kindText = GetCell(EntityKindColumn, indexByHeader, values);
        var attributeText = GetCell(AttributeColumn, indexByHeader, values);

        if (!Enum.TryParse<EntityKind>(kindText, ignoreCase: true, out var entityKind))
        {
            context.AddDiagnostic(
                SemanticDiagnosticSeverity.Error,
                "unknown_entity_kind",
                $"Unknown entity kind '{kindText}'.",
                worksheetName,
                rowNumber);
            return;
        }

        var attributeId = new DefinitionAttributeId(attributeText);
        var attributeKey = new DefinitionAttributeKey(entityKind, attributeId);
        if (!context.Graph.Attributes.ContainsKey(attributeKey))
        {
            context.AddDiagnostic(
                SemanticDiagnosticSeverity.Error,
                "unknown_attribute",
                $"Worksheet row references undeclared attribute '{entityKind}.{attributeText}'.",
                worksheetName,
                rowNumber);
            return;
        }

        var stock = context.GetOrCreateStock(
            attributeKey,
            GetCell(BoundsColumn, indexByHeader, values),
            GetCell(ConservationColumn, indexByHeader, values),
            GetCell(FeedbackClassColumn, indexByHeader, values),
            GetCell(NotesColumn, indexByHeader, values),
            worksheetName,
            rowNumber);

        ImportFlowColumn(context, worksheetName, rowNumber, stock, GetCell(InflowsColumn, indexByHeader, values), SemanticFlowRole.Inflow);
        ImportFlowColumn(context, worksheetName, rowNumber, stock, GetCell(OutflowsColumn, indexByHeader, values), SemanticFlowRole.Outflow);
        ImportModifierColumn(context, worksheetName, rowNumber, stock, GetCell(ModulatorsColumn, indexByHeader, values));
        ImportProcessReferenceColumn(context, worksheetName, rowNumber, stock, GetCell(ReadsColumn, indexByHeader, values), SemanticProcessReferenceKind.Reads);
        ImportProcessReferenceColumn(context, worksheetName, rowNumber, stock, GetCell(DirectWritesColumn, indexByHeader, values), SemanticProcessReferenceKind.DirectWrites);
        ImportProcessReferenceColumn(context, worksheetName, rowNumber, stock, GetCell(IndirectWritesColumn, indexByHeader, values), SemanticProcessReferenceKind.IndirectWrites);
    }

    private static void ImportFlowColumn(
        ImportContext context,
        string worksheetName,
        int rowNumber,
        SemanticStockNode stock,
        string cell,
        SemanticFlowRole role)
    {
        foreach (var segment in SplitTopLevelSegments(cell))
        {
            if (!TryParseNamedEntry(segment, out var flowId, out var rawSignature, out var rawForm))
            {
                context.AddDiagnostic(
                    SemanticDiagnosticSeverity.Warning,
                    "unparsed_flow_segment",
                    $"Could not parse flow segment '{segment}'.",
                    worksheetName,
                    rowNumber);
                continue;
            }

            var verb = context.GetOrCreateVerb(flowId);
            context.Graph.SemanticFlows.Flows.Add(new SemanticFlowEdge
            {
                Id = $"{worksheetName}:{rowNumber}:{flowId}:{role}",
                Verb = verb,
                Role = role,
                TargetStock = stock,
                RawSignature = rawSignature,
                RawForm = rawForm,
                SourceWorksheet = worksheetName,
                SourceRow = rowNumber,
                Mechanism = ParseMechanism(rawForm),
            });
        }
    }

    private static void ImportModifierColumn(
        ImportContext context,
        string worksheetName,
        int rowNumber,
        SemanticStockNode stock,
        string cell)
    {
        foreach (var segment in SplitTopLevelSegments(cell))
        {
            var match = ModifierPattern().Match(segment);
            if (!match.Success)
            {
                context.AddDiagnostic(
                    SemanticDiagnosticSeverity.Warning,
                    "unparsed_modifier_segment",
                    $"Could not parse modifier segment '{segment}'.",
                    worksheetName,
                    rowNumber);
                continue;
            }

            var targetVerbId = match.Groups["id"].Value.Trim();
            var targetVerb = context.GetOrCreateVerb(targetVerbId);

            context.Graph.SemanticFlows.Modifiers.Add(new SemanticModifierEdge
            {
                Id = $"{worksheetName}:{rowNumber}:{targetVerbId}:modifier",
                SourceStock = stock,
                TargetVerbId = targetVerbId,
                TargetVerb = targetVerb,
                RawForm = match.Groups["form"].Value.Trim(),
                SourceWorksheet = worksheetName,
                SourceRow = rowNumber,
                Direction = ParseModifierDirection(match.Groups["dir"].Value),
            });
        }
    }

    private static void ImportProcessReferenceColumn(
        ImportContext context,
        string worksheetName,
        int rowNumber,
        SemanticStockNode stock,
        string cell,
        SemanticProcessReferenceKind referenceKind)
    {
        foreach (var segment in SplitTopLevelSegments(cell))
        {
            if (!TryParseNamedEntry(segment, out var processId, out _, out _))
            {
                context.AddDiagnostic(
                    SemanticDiagnosticSeverity.Warning,
                    "unparsed_process_reference",
                    $"Could not parse process reference segment '{segment}'.",
                    worksheetName,
                    rowNumber);
                continue;
            }

            context.Graph.Processes.TryGetValue(processId, out var resolvedProcess);
            var matchesDefinition = resolvedProcess is not null && ProcessTouchesStock(resolvedProcess, stock.AttributeKey, referenceKind);

            context.Graph.SemanticFlows.ProcessReferences.Add(new SemanticProcessReference
            {
                ProcessId = processId,
                Kind = referenceKind,
                TargetStock = stock,
                ResolvedProcess = resolvedProcess,
                MatchesDefinitionGraph = matchesDefinition,
                SourceWorksheet = worksheetName,
                SourceRow = rowNumber,
            });

            if (resolvedProcess is null)
            {
                context.AddDiagnostic(
                    SemanticDiagnosticSeverity.Info,
                    "future_process_reference",
                    $"Worksheet references process '{processId}' which is not implemented in the current domain.",
                    worksheetName,
                    rowNumber);
                continue;
            }

            if (!matchesDefinition)
            {
                context.AddDiagnostic(
                    SemanticDiagnosticSeverity.Warning,
                    "process_reference_mismatch",
                    $"Worksheet marks process '{processId}' as {referenceKind}, but the current definition graph does not touch '{stock.AttributeKey.Kind}.{stock.AttributeKey.AttributeId}' that way.",
                    worksheetName,
                    rowNumber);
            }
        }
    }

    private static bool ProcessTouchesStock(
        DefinitionProcessNode process,
        DefinitionAttributeKey stockKey,
        SemanticProcessReferenceKind referenceKind)
    {
        var refs = referenceKind == SemanticProcessReferenceKind.Reads
            ? process.Reads
            : process.Writes;

        return refs.Any(reference =>
            reference.Kind == stockKey.Kind &&
            reference.ValueId is AttributeValueId { AttributeId: var attributeId } &&
            attributeId == stockKey.AttributeId);
    }

    private static void ValidateVerbConsistency(ImportContext context)
    {
        foreach (var verb in context.Verbs.Values)
        {
            if (verb.ImplementedProcess is not null &&
                verb.OwnerKind is EntityKind inferredOwner &&
                verb.ImplementedProcess.TargetKind != inferredOwner)
            {
                context.AddDiagnostic(
                    SemanticDiagnosticSeverity.Warning,
                    "verb_owner_mismatch",
                    $"Semantic verb '{verb.Id}' infers owner '{inferredOwner}' from its id, but implemented process targets '{verb.ImplementedProcess.TargetKind}'.",
                    null,
                    null);
            }
        }
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        values.Add(current.ToString().Trim());
        return values;
    }

    private static IEnumerable<string> SplitTopLevelSegments(string cell)
    {
        if (string.IsNullOrWhiteSpace(cell))
        {
            yield break;
        }

        var current = new StringBuilder();
        var depth = 0;
        foreach (var ch in cell)
        {
            switch (ch)
            {
                case '(':
                    depth++;
                    current.Append(ch);
                    break;
                case ')':
                    depth = Math.Max(0, depth - 1);
                    current.Append(ch);
                    break;
                case ';' when depth == 0:
                    var segment = current.ToString().Trim();
                    if (segment.Length > 0)
                    {
                        yield return segment;
                    }
                    current.Clear();
                    break;
                default:
                    current.Append(ch);
                    break;
            }
        }

        var trailing = current.ToString().Trim();
        if (trailing.Length > 0)
        {
            yield return trailing;
        }
    }

    private static bool TryParseNamedEntry(string segment, out string id, out string? rawSignature, out string? rawForm)
    {
        id = string.Empty;
        rawSignature = null;
        rawForm = null;

        var match = NamedEntryPattern().Match(segment);
        if (match.Success)
        {
            id = match.Groups["id"].Value.Trim();
            var body = match.Groups["body"].Value.Trim();
            var splitIndex = body.AsSpan().IndexOf(';');
            if (splitIndex >= 0)
            {
                rawSignature = body[..splitIndex].Trim();
                rawForm = body[(splitIndex + 1)..].Trim();
            }
            else
            {
                rawSignature = body;
            }

            return id.Length > 0;
        }

        if (IdentifierPattern().IsMatch(segment.Trim()))
        {
            id = segment.Trim();
            return true;
        }

        return false;
    }

    private static string GetCell(string columnName, IReadOnlyDictionary<string, int> indexByHeader, IReadOnlyList<string> values)
    {
        if (!indexByHeader.TryGetValue(columnName, out var index) || index >= values.Count)
        {
            return string.Empty;
        }

        return values[index].Trim();
    }

    private static SemanticStockClass ParseStockClass(string rawValue)
    {
        var normalized = rawValue.Trim().ToUpperInvariant();
        return normalized switch
        {
            "ANCHOR" => SemanticStockClass.Anchor,
            "FREE" => SemanticStockClass.Free,
            _ when normalized.StartsWith("PLANE", StringComparison.Ordinal) => SemanticStockClass.Plane,
            _ => SemanticStockClass.Unknown,
        };
    }

    private static SemanticFeedbackClass ParseFeedbackClass(string rawValue)
    {
        var normalized = rawValue.Trim().ToUpperInvariant();
        return normalized switch
        {
            "ANCHOR" => SemanticFeedbackClass.Anchor,
            "BALANCING" => SemanticFeedbackClass.Balancing,
            "DRIFT" => SemanticFeedbackClass.Drift,
            _ => SemanticFeedbackClass.Unknown,
        };
    }

    private static SemanticMechanism ParseMechanism(string? rawForm)
    {
        var normalized = (rawForm ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Contains("CONVERSION", StringComparison.Ordinal))
        {
            return SemanticMechanism.Conversion;
        }
        if (normalized.Contains("SACRIFICE", StringComparison.Ordinal))
        {
            return SemanticMechanism.Sacrifice;
        }
        if (normalized.Contains("EMANATION", StringComparison.Ordinal))
        {
            return SemanticMechanism.Emanation;
        }
        if (normalized.Contains("GATE", StringComparison.Ordinal))
        {
            return SemanticMechanism.Gate;
        }
        if (normalized.Contains("DRIFT", StringComparison.Ordinal))
        {
            return SemanticMechanism.Drift;
        }
        if (normalized.Contains("SHIFT", StringComparison.Ordinal))
        {
            return SemanticMechanism.Shift;
        }
        if (normalized.Contains("INFLOW", StringComparison.Ordinal))
        {
            return SemanticMechanism.Inflow;
        }

        return SemanticMechanism.Unknown;
    }

    private static SemanticModifierDirection ParseModifierDirection(string rawValue) =>
        rawValue.Trim() switch
        {
            "+" => SemanticModifierDirection.Positive,
            "-" => SemanticModifierDirection.Negative,
            _ => SemanticModifierDirection.Unknown,
        };

    private static void Reset(SemanticFlowGraph graph)
    {
        graph.Stocks.Clear();
        graph.Verbs.Clear();
        graph.Flows.Clear();
        graph.Modifiers.Clear();
        graph.Couplings.Clear();
        graph.ProcessReferences.Clear();
        graph.Diagnostics.Clear();
    }

    [GeneratedRegex(@"^(?<id>[A-Za-z0-9_]+)\s*\((?<body>.*)\)$", RegexOptions.Compiled)]
    private static partial Regex NamedEntryPattern();

    [GeneratedRegex(@"^[A-Za-z0-9_]+$", RegexOptions.Compiled)]
    private static partial Regex IdentifierPattern();

    [GeneratedRegex(@"^(?<id>[A-Za-z0-9_]+)\s*->\s*(?<dir>[+-])\s*\((?<form>.*)\)$", RegexOptions.Compiled)]
    private static partial Regex ModifierPattern();

    private sealed class ImportContext(DefinitionGraph graph)
    {
        public DefinitionGraph Graph { get; } = graph;
        public Dictionary<DefinitionAttributeKey, SemanticStockNode> Stocks { get; } = new();
        public Dictionary<string, SemanticVerbNode> Verbs { get; } = new(StringComparer.Ordinal);

        public SemanticStockNode GetOrCreateStock(
            DefinitionAttributeKey key,
            string rawBounds,
            string rawConservation,
            string rawFeedbackClass,
            string notes,
            string worksheetName,
            int rowNumber)
        {
            if (Stocks.TryGetValue(key, out var existing))
            {
                AddDiagnostic(
                    SemanticDiagnosticSeverity.Warning,
                    "duplicate_stock_row",
                    $"Duplicate worksheet row for '{key.Kind}.{key.AttributeId}'. Reusing first row.",
                    worksheetName,
                    rowNumber);
                return existing;
            }

            var stock = new SemanticStockNode
            {
                AttributeKey = key,
                RawBounds = rawBounds,
                RawConservation = rawConservation,
                StockClass = ParseStockClass(rawConservation),
                FeedbackClass = ParseFeedbackClass(rawFeedbackClass),
                Notes = notes,
                SourceWorksheet = worksheetName,
                SourceRow = rowNumber,
            };

            Stocks[key] = stock;
            Graph.SemanticFlows.Stocks.Add(stock);
            return stock;
        }

        public SemanticVerbNode GetOrCreateVerb(string verbId)
        {
            if (Verbs.TryGetValue(verbId, out var existing))
            {
                return existing;
            }

            Graph.Processes.TryGetValue(verbId, out var implementedProcess);
            InferVerbOwner(verbId, out var ownerType, out var ownerKind);

            var verb = new SemanticVerbNode
            {
                Id = verbId,
                OwnerType = ownerType,
                OwnerKind = ownerKind,
                ImplementedProcess = implementedProcess,
            };

            Verbs[verbId] = verb;
            Graph.SemanticFlows.Verbs.Add(verb);
            return verb;
        }

        public void AddDiagnostic(
            SemanticDiagnosticSeverity severity,
            string code,
            string message,
            string? worksheetName,
            int? rowNumber)
        {
            Graph.SemanticFlows.Diagnostics.Add(new SemanticImportDiagnostic
            {
                Severity = severity,
                Code = code,
                Message = message,
                SourceWorksheet = worksheetName,
                SourceRow = rowNumber,
            });
        }

        private static void InferVerbOwner(string verbId, out SemanticOwnerType ownerType, out EntityKind? ownerKind)
        {
            ownerType = SemanticOwnerType.Unknown;
            ownerKind = null;

        var separatorIndex = verbId.AsSpan().IndexOf('_');
            var prefix = separatorIndex > 0 ? verbId[..separatorIndex] : verbId;

            if (string.Equals(prefix, "engine", StringComparison.OrdinalIgnoreCase))
            {
                ownerType = SemanticOwnerType.Engine;
                return;
            }

            if (Enum.TryParse<EntityKind>(prefix, ignoreCase: true, out var parsedKind))
            {
                ownerType = SemanticOwnerType.Entity;
                ownerKind = parsedKind;
            }
        }
    }
}
