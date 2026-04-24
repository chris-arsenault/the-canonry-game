namespace Engine.Observability;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using Engine.Graph;
using Engine.Kinds;
using Engine.Lifecycle;
using Engine.Map;
using Engine.Messages;
using Engine.Simulation;

/// <summary>
/// Streaming per-run log. Captures enough to reason about agent behavior over
/// a sim's lifetime without dumping the full planner trace.
///
/// Per tick it emits one line per entity that either (a) executed a process,
/// (b) moved on either plane, or (c) crossed a meaningful attribute delta.
/// Silent entities are omitted to keep logs scannable. Ends each tick with a
/// one-line summary (total actions, movers, deaths).
///
/// Call <see cref="OnTickEnded"/> after every <c>SimulationRunner.Tick()</c>.
/// </summary>
public sealed class RunLogger : IDisposable
{
    private const float AttributeReportThreshold = 0.5f;

    private readonly StreamWriter _writer;
    private readonly SimulationRunner _sim;
    private readonly Dictionary<EntityId, Snapshot> _lastSnapshots = [];
    private readonly Dictionary<EntityId, List<ActionEntry>> _tickActions = [];
    private readonly List<EntityId> _tickDeaths = [];
    private readonly List<string> _tickGeneratorLines = [];
    private bool _disposed;

    public string LogPath { get; }

    public RunLogger(string logPath, SimulationRunner sim)
    {
        ArgumentNullException.ThrowIfNull(logPath);
        ArgumentNullException.ThrowIfNull(sim);

        var dir = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        LogPath = logPath;
        _sim = sim;
        _writer = new StreamWriter(logPath) { AutoFlush = true };

        sim.MessageBus.Subscribe(OnMessage);
        sim.Lifecycle.EntityDied += OnEntityDied;

        WriteHeader();
        SnapshotAllAndWriteInitialState();
    }

    /// <summary>
    /// Convenience: returns a default log path rooted at <c>.artifacts/logs/</c>
    /// with a timestamped filename. Callers can override by constructing with
    /// their own path.
    /// </summary>
    public static string DefaultLogPath(string? rootDirectory = null)
    {
        var root = rootDirectory ?? Path.Combine(".artifacts", "logs");
        var stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff", CultureInfo.InvariantCulture);
        return Path.Combine(root, $"sim-{stamp}.log");
    }

    /// <summary>
    /// Emit a tick block: every entity that acted or changed state gets one line,
    /// ending with a summary line.
    /// </summary>
    public void OnTickEnded()
    {
        if (_disposed) return;

        var tick = _sim.Clock.CurrentTick;
        _writer.WriteLine();
        _writer.WriteLine(FormattableString.Invariant($"=== t={tick} ==="));

        foreach (var generatorLine in _tickGeneratorLines)
        {
            _writer.WriteLine(generatorLine);
        }

        var movedPhys = 0;
        var movedSem = 0;
        var totalActions = 0;
        var reportedRows = 0;

        foreach (var (id, entity) in _sim.Graph.AllEntities.OrderBy(kv => kv.Key.Value))
        {
            var prev = _lastSnapshots.TryGetValue(id, out var p) ? p : null;
            var curr = SnapshotOf(entity);
            _lastSnapshots[id] = curr;

            var actions = _tickActions.TryGetValue(id, out var actionList) ? actionList : null;
            var acted = actions is { Count: > 0 };
            if (acted) totalActions += actions!.Count;

            var physicalChanged = prev is not null && prev.Physical != curr.Physical;
            var semanticChanged = prev is not null && prev.Semantic != curr.Semantic;
            if (physicalChanged) movedPhys++;
            if (semanticChanged) movedSem++;

            var attrDeltas = ComputeAttributeDeltas(prev, curr);
            if (!acted && !physicalChanged && !semanticChanged && attrDeltas.Count == 0) continue;

            const int padTo = 32;
            var line = new StringBuilder();
            line.Append(FormattableString.Invariant($"  E{id.Value,-3} [{Abbrev(entity.Kind)}] "));
            line.Append(entity.Name.PadRight(padTo));
            line.Append(' ');
            line.Append(physicalChanged
                ? FormattableString.Invariant($"phys={FormatPhysical(prev!.Physical)}→{FormatPhysical(curr.Physical)}")
                : FormattableString.Invariant($"phys={FormatPhysical(curr.Physical)}          "));
            line.Append(' ');
            line.Append(semanticChanged
                ? FormattableString.Invariant($"sem={FormatSemantic(prev!.Semantic)}→{FormatSemantic(curr.Semantic)}")
                : FormattableString.Invariant($"sem={FormatSemantic(curr.Semantic)}          "));

            if (acted)
            {
                line.Append("  act=");
                line.Append(string.Join(" + ", actions!.Select(FormatAction)));
            }

            if (attrDeltas.Count > 0)
            {
                line.Append("  Δ=");
                line.Append(string.Join(", ", attrDeltas));
            }

            _writer.WriteLine(line.ToString());
            reportedRows++;
        }

        if (_tickDeaths.Count > 0)
        {
            foreach (var deadId in _tickDeaths)
            {
                _writer.WriteLine(FormattableString.Invariant($"  † E{deadId.Value} died"));
            }
        }

        _writer.WriteLine(FormattableString.Invariant(
            $"  -- t={tick}: actions={totalActions} moved_phys={movedPhys} moved_sem={movedSem} deaths={_tickDeaths.Count} reported_rows={reportedRows} --"));

        _tickActions.Clear();
        _tickDeaths.Clear();
        _tickGeneratorLines.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            _sim.Lifecycle.EntityDied -= OnEntityDied;
        }
        catch (ObjectDisposedException) { /* sim shutdown order isn't guaranteed */ }
        _writer.Flush();
        _writer.Dispose();
    }

    private void OnMessage(SimulationMessage message)
    {
        if (message is ProcessExecutedMessage executed)
        {
            if (!_tickActions.TryGetValue(executed.ActorId, out var list))
            {
                list = [];
                _tickActions[executed.ActorId] = list;
            }
            EntityId? target = executed.AffectedEntities.Count > 0 ? executed.AffectedEntities[0] : null;
            if (target == executed.ActorId) target = null;
            list.Add(new ActionEntry(ProcessName: executed.ProcessName, Target: target));
            return;
        }

        if (message is GeneratorFiredMessage generatorFired)
        {
            _tickGeneratorLines.Add(FormatGenerator(generatorFired));
        }
    }

    private void OnEntityDied(object? sender, LifecycleDeathEventArgs e)
    {
        _tickDeaths.Add(e.Entity.Id);
    }

    private void WriteHeader()
    {
        var now = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        _writer.WriteLine("# The Canonry sim run log");
        _writer.WriteLine(FormattableString.Invariant($"# started_utc: {now}"));
        _writer.WriteLine(FormattableString.Invariant($"# entities: {_sim.Graph.EntityCount}"));
        _writer.WriteLine("# legend:");
        _writer.WriteLine("#   phys=(q,r) physical hex; sem=(q,r,s) semantic autonomy/resonance/tradition");
        _writer.WriteLine("#   act=ProcessName [+ ProcessName ...] optional tgt=Eid");
        FormattableString attrFmt =
            $"#   Δ=attr:before→after(delta) — scalar attribute deltas with |delta| ≥ {AttributeReportThreshold}";
        _writer.WriteLine(FormattableString.Invariant(attrFmt));
        _writer.WriteLine("#   silent entities (no action, no motion, no attr delta) are omitted per tick");
    }

    private void SnapshotAllAndWriteInitialState()
    {
        _writer.WriteLine();
        _writer.WriteLine("=== initial state (tick 0) ===");
        foreach (var (id, entity) in _sim.Graph.AllEntities.OrderBy(kv => kv.Key.Value))
        {
            var snap = SnapshotOf(entity);
            _lastSnapshots[id] = snap;

            var attrs = snap.Attributes
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => FormattableString.Invariant($"{kv.Key}:{kv.Value:F1}"));
            _writer.WriteLine(FormattableString.Invariant(
                $"  E{id.Value,-3} [{Abbrev(entity.Kind)}] {entity.Name.PadRight(32)} phys={FormatPhysical(snap.Physical)} sem={FormatSemantic(snap.Semantic)}  {string.Join(" ", attrs)}"));
        }
    }

    private static Snapshot SnapshotOf(Entity entity)
    {
        var attrs = new Dictionary<string, float>(StringComparer.Ordinal);
        foreach (var name in entity.Attributes.Names)
        {
            var scalar = entity.Attributes.GetScalar(name);
            if (scalar is not null)
            {
                attrs[name] = scalar.Value;
            }
        }
        return new Snapshot(
            Physical: entity.Coordinates,
            Semantic: entity.SemanticPosition,
            Attributes: attrs);
    }

    private static List<string> ComputeAttributeDeltas(Snapshot? prev, Snapshot curr)
    {
        var deltas = new List<string>();
        if (prev is null) return deltas;
        foreach (var (name, value) in curr.Attributes)
        {
            if (!prev.Attributes.TryGetValue(name, out var previous)) continue;
            var delta = value - previous;
            if (MathF.Abs(delta) < AttributeReportThreshold) continue;
            var sign = delta >= 0 ? "+" : "";
            deltas.Add(FormattableString.Invariant($"{name}:{previous:F1}→{value:F1}({sign}{delta:F1})"));
        }
        return deltas;
    }

    private string FormatAction(ActionEntry entry)
    {
        if (entry.Target is not { } targetId) return entry.ProcessName;
        var target = _sim.Graph.GetEntity(targetId);
        var targetName = target?.Name ?? targetId.Value.ToString(CultureInfo.InvariantCulture);
        return FormattableString.Invariant($"{entry.ProcessName}→E{targetId.Value}:{targetName}");
    }

    private string FormatGenerator(GeneratorFiredMessage message)
    {
        var target = message.TargetEntityId is { } targetId
            ? FormatEntityTarget(targetId)
            : "hex(?)";
        var created = message.CreatedEntityIds.Count > 0
            ? string.Join(", ", message.CreatedEntityIds.Select(FormatCreatedEntity))
            : "none";
        return FormattableString.Invariant(
            $"==  gen: {message.GeneratorId} → {target}  created {created}  relationships={message.CreatedRelationshipCount}  ==");
    }

    private string FormatEntityTarget(EntityId entityId)
    {
        var entity = _sim.Graph.GetEntity(entityId);
        return entity is null
            ? FormattableString.Invariant($"E{entityId.Value}")
            : FormattableString.Invariant($"E{entityId.Value} {entity.Name}");
    }

    private string FormatCreatedEntity(EntityId entityId)
    {
        var entity = _sim.Graph.GetEntity(entityId);
        return entity is null
            ? FormattableString.Invariant($"E{entityId.Value}")
            : FormattableString.Invariant($"E{entityId.Value} \"{entity.Name}\" [{entity.Kind}]");
    }

    private static string FormatPhysical((int Q, int R) coord)
        => FormattableString.Invariant($"({coord.Q,3},{coord.R,3})");

    private static string FormatSemantic(SemanticHexCoord coord)
        => FormattableString.Invariant($"({coord.Q,3},{coord.R,3},{coord.S,3})");

    private static string Abbrev(EntityKind kind)
    {
        var value = kind.Value;
        return value.Length <= 3 ? value : value[..3];
    }

    private sealed record Snapshot(
        (int Q, int R) Physical,
        SemanticHexCoord Semantic,
        IReadOnlyDictionary<string, float> Attributes);

    private sealed record ActionEntry(string ProcessName, EntityId? Target);
}
