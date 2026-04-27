namespace Engine.Observability;

using System.Globalization;
using System.IO;
using System.Text;

using Engine.Eventing;
using Engine.Projection;

public sealed class RunLogger : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly RunLogProjection _projection;
    private readonly KernelEventProjectionPump _pump;
    private bool _disposed;

    public RunLogger(
        string logPath,
        IKernelEventReader eventLog,
        IProjectionSymbolCatalog? symbols = null,
        KernelEventCursorStart cursorStart = KernelEventCursorStart.EarliestAvailable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logPath);
        ArgumentNullException.ThrowIfNull(eventLog);

        var dir = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        LogPath = logPath;
        _writer = new StreamWriter(logPath) { AutoFlush = true };
        _projection = new RunLogProjection(symbols);
        _pump = new KernelEventProjectionPump(eventLog, cursorStart, _projection);
        WriteHeader();
    }

    public string LogPath { get; }

    public static string DefaultLogPath(string? rootDirectory = null)
    {
        var root = rootDirectory ?? Path.Combine(".artifacts", "logs");
        var stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff", CultureInfo.InvariantCulture);
        return Path.Combine(root, $"sim-{stamp}.log");
    }

    public void DrainAvailable()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _pump.DrainAvailable();
    }

    public void OnTickEnded(int tick)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        DrainAvailable();

        _writer.WriteLine();
        _writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"=== t={tick} ==="));

        foreach (var line in _projection.TakeGeneratorLines(tick))
        {
            _writer.WriteLine(line);
        }

        foreach (var line in _projection.TakeActionLines(tick))
        {
            _writer.WriteLine(line);
        }

        var summary = _projection.TakeSummary(tick);
        _writer.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"  -- t={tick}: actions={summary.Actions} generator_fires={summary.GeneratorFires} deaths={summary.Deaths} --"));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _pump.Dispose();
        _writer.Flush();
        _writer.Dispose();
    }

    private void WriteHeader()
    {
        var now = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        _writer.WriteLine("# The Canonry kernel event log");
        _writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"# started_utc: {now}"));
        _writer.WriteLine("# source: KernelEventLog");
        _writer.WriteLine("# legend: act=process optional tgt=entity; gen=generator fire; death=status transition to historical");
    }

    private sealed class RunLogProjection(IProjectionSymbolCatalog? symbols) : KernelEventProjector
    {
        private readonly Dictionary<int, List<string>> _actionLinesByTick = [];
        private readonly Dictionary<int, List<string>> _generatorLinesByTick = [];
        private readonly Dictionary<int, TickSummary> _summariesByTick = [];
        private readonly Dictionary<int, ObservedEntityProjection> _entities = [];

        public List<string> TakeActionLines(int tick) => Take(_actionLinesByTick, tick);

        public List<string> TakeGeneratorLines(int tick) => Take(_generatorLinesByTick, tick);

        public TickSummary TakeSummary(int tick)
        {
            if (!_summariesByTick.Remove(tick, out var summary))
            {
                return new TickSummary();
            }

            return summary;
        }

        protected override void OnEntityCreated(long sequence, in EntityCreatedKernelEvent evt)
        {
            var entity = GetOrCreateEntity(evt.EntityId);
            entity.KindId = evt.KindId;
            entity.SubtypeId = evt.SubtypeId;
            entity.CultureId = evt.CultureId;
            entity.NameSeed = evt.EntityId;
            entity.PhysicalQ = evt.PhysicalQ;
            entity.PhysicalR = evt.PhysicalR;
            entity.SemanticQ = evt.SemanticQ;
            entity.SemanticR = evt.SemanticR;
            entity.SemanticS = evt.SemanticS;
        }

        protected override void OnEntityStatusChanged(long sequence, in EntityStatusChangedKernelEvent evt)
        {
            var entity = GetOrCreateEntity(evt.EntityId);
            entity.KindId = evt.KindId;
            entity.SubtypeId = evt.SubtypeId;
            entity.StatusCode = evt.CurrentStatusCode;
            if (evt.CurrentStatusCode == EntityStatusCodes.Historical)
            {
                Summary(evt.Tick).Deaths++;
                Add(
                    _actionLinesByTick,
                    evt.Tick,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"  death E{evt.EntityId}: {ResolveEntityName(entity)}"));
            }
        }

        protected override void OnProcessExecuted(long sequence, in ProcessExecutedKernelEvent evt)
        {
            Summary(evt.Tick).Actions++;
            var process = ResolveDisplayName(ProjectionSymbolKind.Process, evt.ProcessId, $"process_{evt.ProcessId}");
            var actor = ResolveEntityTarget(evt.ActorId);
            var target = evt.AffectedEntityCount > 0 && evt.AffectedEntityId0 != evt.ActorId
                ? string.Create(CultureInfo.InvariantCulture, $" tgt={ResolveEntityTarget(evt.AffectedEntityId0)}")
                : string.Empty;
            Add(
                _actionLinesByTick,
                evt.Tick,
                string.Create(CultureInfo.InvariantCulture, $"  act {actor} {process}{target}"));
        }

        protected override void OnGeneratorFired(long sequence, in GeneratorFiredKernelEvent evt)
        {
            Summary(evt.Tick).GeneratorFires++;
            var generator = ResolveDisplayName(ProjectionSymbolKind.Generator, evt.GeneratorId, $"generator_{evt.GeneratorId}");
            var target = evt.TargetEntityId == 0 ? "hex(?)" : ResolveEntityTarget(evt.TargetEntityId);
            Add(
                _generatorLinesByTick,
                evt.Tick,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"  gen {generator} -> {target} created_entities={evt.CreatedEntityCount} created_relationships={evt.CreatedRelationshipCount}"));
        }

        private ObservedEntityProjection GetOrCreateEntity(int entityId)
        {
            if (_entities.TryGetValue(entityId, out var entity))
            {
                return entity;
            }

            entity = new ObservedEntityProjection(entityId) { NameSeed = entityId };
            _entities.Add(entityId, entity);
            return entity;
        }

        private string ResolveEntityTarget(int entityId)
        {
            var entity = GetOrCreateEntity(entityId);
            return string.Create(CultureInfo.InvariantCulture, $"E{entityId}:{ResolveEntityName(entity)}");
        }

        private string ResolveEntityName(ObservedEntityProjection entity)
        {
            var key = new ProjectionEntityNameKey(
                entity.EntityId,
                entity.KindId,
                entity.SubtypeId,
                entity.CultureId,
                entity.NameSeed == 0 ? entity.EntityId : entity.NameSeed);
            return symbols?.ResolveEntityName(key, $"E{entity.EntityId}") ?? $"E{entity.EntityId}";
        }

        private string ResolveDisplayName(ProjectionSymbolKind kind, int id, string fallback) =>
            id == 0 ? string.Empty : symbols?.ResolveDisplayName(kind, id, fallback) ?? fallback;

        private TickSummary Summary(int tick)
        {
            if (_summariesByTick.TryGetValue(tick, out var summary))
            {
                return summary;
            }

            summary = new TickSummary();
            _summariesByTick[tick] = summary;
            return summary;
        }

        private static void Add(Dictionary<int, List<string>> linesByTick, int tick, string line)
        {
            if (!linesByTick.TryGetValue(tick, out var lines))
            {
                lines = [];
                linesByTick[tick] = lines;
            }

            lines.Add(line);
        }

        private static List<string> Take(Dictionary<int, List<string>> linesByTick, int tick)
        {
            if (!linesByTick.Remove(tick, out var lines))
            {
                return [];
            }

            return lines;
        }
    }

    private sealed class TickSummary
    {
        public int Actions { get; set; }

        public int GeneratorFires { get; set; }

        public int Deaths { get; set; }
    }
}
