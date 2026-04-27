namespace Engine.Projection;

using Engine.Eventing;

public sealed class UiChronicleProjection : KernelEventProjector
{
    private const int MaxLines = 160;
    private readonly IProjectionSymbolCatalog? _symbols;
    private readonly List<string> _lines = [];

    public UiChronicleProjection(IProjectionSymbolCatalog? symbols)
    {
        _symbols = symbols;
    }

    public IReadOnlyList<string> Lines => _lines;

    protected override void OnEntityCreated(long sequence, in EntityCreatedKernelEvent evt)
    {
        var name = ResolveEntityName(
            evt.EntityId,
            evt.KindId,
            evt.SubtypeId,
            evt.CultureId,
            evt.EntityId);
        var kind = Resolve(ProjectionSymbolKind.Kind, evt.KindId, $"Kind {evt.KindId}");
        Add(evt.Tick, $"{name} enters as {kind}");
    }

    protected override void OnEntityStatusChanged(long sequence, in EntityStatusChangedKernelEvent evt)
    {
        var name = ResolveEntityName(
            evt.EntityId,
            evt.KindId,
            evt.SubtypeId,
            0,
            evt.EntityId);
        Add(evt.Tick, $"{name} status {evt.PreviousStatusCode} -> {evt.CurrentStatusCode}");
    }

    protected override void OnEntityMoved(long sequence, in EntityMovedKernelEvent evt)
    {
        var plane = evt.PlaneCode == (int)UiProjectionPlane.Physical ? "physical" : "semantic";
        Add(evt.Tick, $"E{evt.EntityId} moved on {plane} to ({evt.ToQ},{evt.ToR},{evt.ToS})");
    }

    protected override void OnProcessExecuted(long sequence, in ProcessExecutedKernelEvent evt)
    {
        var process = Resolve(ProjectionSymbolKind.Process, evt.ProcessId, $"Process {evt.ProcessId}");
        Add(evt.Tick, $"E{evt.ActorId} performed {process}");
    }

    protected override void OnGambitExecuted(long sequence, in GambitExecutedKernelEvent evt)
    {
        var gambit = Resolve(ProjectionSymbolKind.Gambit, evt.GambitId, $"Gambit {evt.GambitId}");
        Add(evt.Tick, $"E{evt.ActorId} committed {gambit}");
    }

    protected override void OnGeneratorFired(long sequence, in GeneratorFiredKernelEvent evt)
    {
        var generator = Resolve(ProjectionSymbolKind.Generator, evt.GeneratorId, $"Generator {evt.GeneratorId}");
        Add(evt.Tick, $"{generator} fired and created {evt.CreatedEntityCount} entities");
    }

    protected override void OnGeneratorEnabledChanged(long sequence, in GeneratorEnabledChangedKernelEvent evt)
    {
        var generator = Resolve(ProjectionSymbolKind.Generator, evt.GeneratorId, $"Generator {evt.GeneratorId}");
        var state = evt.Enabled != 0 ? "enabled" : "disabled";
        Add(evt.Tick, $"{generator} {state}");
    }

    protected override void OnDegeneracyDetected(long sequence, in DegeneracyDetectedKernelEvent evt)
    {
        var reason = Resolve(ProjectionSymbolKind.ReasonCode, evt.ReasonCodeId, $"Reason {evt.ReasonCodeId}");
        Add(evt.Tick, $"degeneracy detected: {reason}");
    }

    protected override void OnTickCompleted(long sequence, in TickCompletedKernelEvent evt)
    {
        Add(evt.Tick, $"tick {evt.Tick} completed");
    }

    private void Add(int tick, string text)
    {
        _lines.Add($"t{tick,-4} {text}");
        if (_lines.Count > MaxLines)
        {
            _lines.RemoveRange(0, _lines.Count - MaxLines);
        }
    }

    private string Resolve(ProjectionSymbolKind kind, int id, string fallback) =>
        _symbols?.ResolveDisplayName(kind, id, fallback) ?? fallback;

    private string ResolveEntityName(
        int entityId,
        int kindId,
        int subtypeId,
        int cultureId,
        int nameSeed) =>
        _symbols?.ResolveEntityName(
            new ProjectionEntityNameKey(entityId, kindId, subtypeId, cultureId, nameSeed),
            $"E{entityId}") ?? $"E{entityId}";
}
