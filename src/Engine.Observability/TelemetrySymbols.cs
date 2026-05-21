namespace Engine.Observability;

using System.Collections.ObjectModel;

using Engine.Constants;
using Engine.Eventing;
using Engine.Projection;

internal static class TelemetrySymbolIds
{
    public static int RunId(string runLabel, int? seed) =>
        seed ?? StableId(TelemetrySymbolKinds.Run, runLabel);

    public static int StableId(int symbolKind, string stableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stableName);
        unchecked
        {
            var hash = 2166136261u;
            hash ^= (uint)symbolKind;
            hash *= 16777619u;
            for (var i = 0; i < stableName.Length; i++)
            {
                hash ^= stableName[i];
                hash *= 16777619u;
            }

            var id = (int)(hash & 0x7fffffffu);
            return id == 0 ? 1 : id;
        }
    }

    public static SymbolRow Create(
        int symbolKind,
        string stableName,
        string? displayName = null,
        string? description = null,
        string? iconKey = null,
        string? colorKey = null) =>
        new(
            symbolKind,
            StableId(symbolKind, stableName),
            stableName,
            string.IsNullOrWhiteSpace(displayName)
                ? ProjectionSymbolCatalog.ToDisplayName(stableName)
                : displayName,
            description,
            iconKey,
            colorKey);

    public static SymbolRow CreateWithId(
        int symbolKind,
        int id,
        string stableName,
        string? displayName = null,
        string? description = null,
        string? iconKey = null,
        string? colorKey = null) =>
        new(
            symbolKind,
            id,
            stableName,
            string.IsNullOrWhiteSpace(displayName)
                ? ProjectionSymbolCatalog.ToDisplayName(stableName)
                : displayName,
            description,
            iconKey,
            colorKey);

    public static void AddProjectionSymbols(TelemetryBatch batch, IProjectionSymbolCatalog? symbols)
    {
        ArgumentNullException.ThrowIfNull(batch);
        if (symbols is null)
        {
            return;
        }

        foreach (var symbol in symbols.Symbols.Values)
        {
            batch.Symbols.Add(new SymbolRow(
                (int)symbol.Kind,
                symbol.Id,
                symbol.StableName,
                symbol.DisplayName,
                symbol.Description,
                symbol.IconKey,
                symbol.ColorKey));
        }
    }

    public static void AddFixedCodeSymbols(TelemetryBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        AddFixedCodeSymbols(batch.Symbols);
    }

    private static void AddFixedCodeSymbols(Collection<SymbolRow> symbols)
    {
        symbols.Add(CreateWithId(TelemetrySymbolKinds.EntityStatus, EntityStatusCodes.Active, "active", "Active"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.EntityStatus, EntityStatusCodes.Historical, "historical", "Historical"));

        symbols.Add(CreateWithId(TelemetrySymbolKinds.Plane, PlaneCodes.Physical, "physical", "Physical"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.Plane, PlaneCodes.Semantic, "semantic", "Semantic"));

        symbols.Add(CreateWithId(TelemetrySymbolKinds.RelationshipPolarity, RelationshipPolarityCodes.Positive, "positive", "Positive"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.RelationshipPolarity, RelationshipPolarityCodes.Negative, "negative", "Negative"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.RelationshipPolarity, RelationshipPolarityCodes.Neutral, "neutral", "Neutral"));

        symbols.Add(CreateWithId(TelemetrySymbolKinds.GeneratorStatus, GeneratorStatusCodes.Eligible, "eligible", "Eligible"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.GeneratorStatus, GeneratorStatusCodes.Fired, "fired", "Fired"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.GeneratorStatus, GeneratorStatusCodes.EligibleNotSelected, "eligible_not_selected", "Eligible Not Selected"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.GeneratorStatus, GeneratorStatusCodes.Blocked, "blocked", "Blocked"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.GeneratorStatus, GeneratorStatusCodes.FireFailed, "fire_failed", "Fire Failed"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.GeneratorStatus, GeneratorStatusCodes.MissingDefinition, "missing_definition", "Missing Definition"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.GeneratorStatus, GeneratorStatusCodes.MaxRuns, "max_runs", "Max Runs"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.GeneratorStatus, GeneratorStatusCodes.ZeroWeight, "zero_weight", "Zero Weight"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.GeneratorStatus, GeneratorStatusCodes.NotEvaluated, "not_evaluated", "Not Evaluated"));

        AddTraceSymbols(symbols);
        AddKernelEventTypeSymbols(symbols);
    }

    private static void AddTraceSymbols(Collection<SymbolRow> symbols)
    {
        symbols.Add(CreateWithId(TelemetrySymbolKinds.TraceSubject, TraceSubjectCodes.Goal, "goal", "Goal"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.TraceSubject, TraceSubjectCodes.ActionSummary, "action_summary", "Action Summary"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.TraceSubject, TraceSubjectCodes.TargetDetail, "target_detail", "Target Detail"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.TraceSubject, TraceSubjectCodes.Support, "support", "Support"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.TraceSubject, TraceSubjectCodes.Locality, "locality", "Locality"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.TraceSubject, TraceSubjectCodes.MovementIntent, "movement_intent", "Movement Intent"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.TraceSubject, TraceSubjectCodes.InfluenceIntent, "influence_intent", "Influence Intent"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.TraceSubject, TraceSubjectCodes.RelationshipIntent, "relationship_intent", "Relationship Intent"));

        symbols.Add(CreateWithId(TelemetrySymbolKinds.TraceStatus, TraceStatusCodes.Offered, "offered", "Offered"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.TraceStatus, TraceStatusCodes.BelowUrgency, "below_urgency", "Below Urgency"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.TraceStatus, TraceStatusCodes.OnCooldown, "on_cooldown", "On Cooldown"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.TraceStatus, TraceStatusCodes.Disabled, "disabled", "Disabled"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.TraceStatus, TraceStatusCodes.NoTarget, "no_target", "No Target"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.TraceStatus, TraceStatusCodes.NoReward, "no_reward", "No Reward"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.TraceStatus, TraceStatusCodes.PreconditionFailed, "precondition_failed", "Precondition Failed"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.TraceStatus, TraceStatusCodes.Actionable, "actionable", "Actionable"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.TraceStatus, TraceStatusCodes.SupportSelected, "support_selected", "Support Selected"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.TraceStatus, TraceStatusCodes.Selected, "selected", "Selected"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.TraceStatus, TraceStatusCodes.CommittedByHysteresis, "committed_by_hysteresis", "Committed By Hysteresis"));
        symbols.Add(CreateWithId(TelemetrySymbolKinds.TraceStatus, TraceStatusCodes.CapacityReached, "capacity_reached", "Capacity Reached"));
    }

    private static void AddKernelEventTypeSymbols(Collection<SymbolRow> symbols)
    {
        AddKernelEventType(symbols, KernelEventType.EntityCreated, "entity_created", "Entity Created");
        AddKernelEventType(symbols, KernelEventType.EntityStatusChanged, "entity_status_changed", "Entity Status Changed");
        AddKernelEventType(symbols, KernelEventType.EntityMoved, "entity_moved", "Entity Moved");
        AddKernelEventType(symbols, KernelEventType.AttributeChanged, "attribute_changed", "Attribute Changed");
        AddKernelEventType(symbols, KernelEventType.RelationshipChanged, "relationship_changed", "Relationship Changed");
        AddKernelEventType(symbols, KernelEventType.GeneratorEvaluated, "generator_evaluated", "Generator Evaluated");
        AddKernelEventType(symbols, KernelEventType.GeneratorFired, "generator_fired", "Generator Fired");
        AddKernelEventType(symbols, KernelEventType.GeneratorTargetEvaluated, "generator_target_evaluated", "Generator Target Evaluated");
        AddKernelEventType(symbols, KernelEventType.CellCreated, "cell_created", "Cell Created");
        AddKernelEventType(symbols, KernelEventType.CellChanged, "cell_changed", "Cell Changed");
        AddKernelEventType(symbols, KernelEventType.CellInfluenceChanged, "cell_influence_changed", "Cell Influence Changed");
        AddKernelEventType(symbols, KernelEventType.CellCultureInfluenceChanged, "cell_culture_influence_changed", "Cell Culture Influence Changed");
        AddKernelEventType(symbols, KernelEventType.CultureSlotOccupancyChanged, "culture_slot_occupancy_changed", "Culture Slot Occupancy Changed");
        AddKernelEventType(symbols, KernelEventType.MinorActorVerbExecuted, "minor_actor_verb_executed", "Minor Actor Verb Executed");
        AddKernelEventType(symbols, KernelEventType.MapCellStateChanged, "map_cell_state_changed", "Map Cell State Changed");
        AddKernelEventType(symbols, KernelEventType.CellRoadChanged, "cell_road_changed", "Cell Road Changed");
        AddKernelEventType(symbols, KernelEventType.CellTravelStructureChanged, "cell_travel_structure_changed", "Cell Travel Structure Changed");
        AddKernelEventType(symbols, KernelEventType.ProvenanceInvocationRecorded, "provenance_invocation_recorded", "Provenance Invocation Recorded");
        AddKernelEventType(symbols, KernelEventType.KernelStorageGrown, "kernel_storage_grown", "Kernel Storage Grown");
        AddKernelEventType(symbols, KernelEventType.Trace, "trace", "Trace");
    }

    private static void AddKernelEventType(
        Collection<SymbolRow> symbols,
        KernelEventType eventType,
        string stableName,
        string displayName) =>
        symbols.Add(CreateWithId(TelemetrySymbolKinds.KernelEventType, (int)eventType, stableName, displayName));
}
