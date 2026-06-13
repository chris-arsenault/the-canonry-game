namespace Engine.Projection;

using Engine.Constants;
using Engine.Eventing;

public enum UiChronicleEntryType
{
    None = 0,
    Gambit = 1,
    Generator = 2,
    HistoryChain = 3,
}

public sealed record UiChronicleEntityRef(
    int EntityId,
    int KindId,
    int SubtypeId,
    int CultureId,
    string DisplayName);

public sealed record UiChronicleEntry(
    long Sequence,
    int Tick,
    UiChronicleEntryType EntryType,
    int ProducerKindCode,
    int ProducerId,
    KernelEventState State,
    IReadOnlyList<UiChronicleEntityRef> Entities,
    string Text);

public sealed class UiChronicleProjection : KernelEventProjector
{
    private const int MaxEntries = 160;
    private const string TickColor = "6F7684";
    private const string GambitColor = "D97742";
    private const string GeneratorColor = "78C9B2";
    private const string FailedColor = "B04646";
    private const string EntityColor = "E7E3D5";
    private const string ConnectorColor = "B8B5A6";

    private readonly IProjectionSymbolCatalog? _symbols;
    private readonly List<UiChronicleEntry> _entries = [];
    private readonly List<string> _lines = [];
    private readonly Dictionary<int, EntityInfo> _entityInfo = [];
    private readonly List<int> _pendingCreatedIds = [];
    private int _pendingTick = -1;

    public UiChronicleProjection(IProjectionSymbolCatalog? symbols)
    {
        _symbols = symbols;
    }

    public IReadOnlyList<UiChronicleEntry> Entries => _entries;

    public IReadOnlyList<string> Lines => _lines;

    public long Version { get; private set; }

    public UiChronicleEntityRef EntityRef(int entityId) => BuildEntityRef(entityId);

    protected override void OnEntityCreated(long sequence, in EntityCreatedKernelEvent evt)
    {
        _entityInfo[evt.EntityId] = new EntityInfo(evt.KindId, evt.SubtypeId, evt.CultureId);

        if (evt.Tick != _pendingTick)
        {
            _pendingTick = evt.Tick;
            _pendingCreatedIds.Clear();
        }

        _pendingCreatedIds.Add(evt.EntityId);
    }

    protected override void OnGambitExecuted(long sequence, in GambitExecutedKernelEvent evt)
    {
        var succeeded = (evt.State & KernelEventState.Succeeded) != KernelEventState.None;
        var color = succeeded ? GambitColor : FailedColor;
        var gambit = Resolve(ProjectionSymbolKind.Gambit, evt.GambitId, $"gambit {evt.GambitId}");
        var actor = BuildEntityRef(evt.ActorId);
        var target = evt.AffectedEntityCount > 1 && evt.AffectedEntityId1 > 0
            ? BuildEntityRef(evt.AffectedEntityId1)
            : null;

        var stateText = succeeded ? string.Empty : $" [color=#{FailedColor}]failed[/color]";
        var line = target is null
            ? $"{TickPrefix(evt.Tick)}[b][color=#{color}]{gambit}[/color][/b]{stateText}  [color=#{EntityColor}]{actor.DisplayName}[/color]"
            : $"{TickPrefix(evt.Tick)}[b][color=#{color}]{gambit}[/color][/b]{stateText}  [color=#{EntityColor}]{actor.DisplayName}[/color] [color=#{ConnectorColor}]->[/color] [color=#{EntityColor}]{target.DisplayName}[/color]";

        Append(new UiChronicleEntry(
            sequence,
            evt.Tick,
            UiChronicleEntryType.Gambit,
            ProvenanceProducerKindCodes.Gambit,
            evt.GambitId,
            evt.State,
            EntityRefs(evt.ActorId, evt.AffectedEntityId0, evt.AffectedEntityId1),
            line));
    }

    protected override void OnGeneratorFired(long sequence, in GeneratorFiredKernelEvent evt)
    {
        var generator = Resolve(ProjectionSymbolKind.Generator, evt.GeneratorId, $"generator {evt.GeneratorId}");
        var target = evt.TargetEntityId > 0 ? BuildEntityRef(evt.TargetEntityId) : null;
        var created = TakeRecentCreations(evt.Tick, evt.CreatedEntityCount);

        var head = target is null
            ? $"{TickPrefix(evt.Tick)}[b][color=#{GeneratorColor}]{generator}[/color][/b]"
            : $"{TickPrefix(evt.Tick)}[b][color=#{GeneratorColor}]{generator}[/color][/b]  on [color=#{EntityColor}]{target.DisplayName}[/color]";

        var line = head;
        if (created.Count > 0)
        {
            var creations = string.Join(
                $"[color=#{ConnectorColor}], [/color]",
                created.Select(entity => $"[color=#{EntityColor}]{entity.DisplayName}[/color]"));
            line = $"{head} [color=#{ConnectorColor}]+[/color] {creations}";
        }

        var entities = new List<UiChronicleEntityRef>(created.Count + (target is null ? 0 : 1));
        if (target is not null)
        {
            entities.Add(target);
        }

        entities.AddRange(created);
        Append(new UiChronicleEntry(
            sequence,
            evt.Tick,
            UiChronicleEntryType.Generator,
            ProvenanceProducerKindCodes.Generator,
            evt.GeneratorId,
            KernelEventState.Succeeded,
            entities,
            line));
    }

    private List<UiChronicleEntityRef> TakeRecentCreations(int tick, int count)
    {
        if (count <= 0 || tick != _pendingTick || _pendingCreatedIds.Count == 0)
        {
            return [];
        }

        var take = Math.Min(count, _pendingCreatedIds.Count);
        var startIndex = _pendingCreatedIds.Count - take;
        var entities = new List<UiChronicleEntityRef>(take);
        for (var i = startIndex; i < _pendingCreatedIds.Count; i++)
        {
            entities.Add(BuildEntityRef(_pendingCreatedIds[i]));
        }

        _pendingCreatedIds.RemoveRange(startIndex, take);
        return entities;
    }

    private List<UiChronicleEntityRef> EntityRefs(params int[] entityIds)
    {
        var refs = new List<UiChronicleEntityRef>(entityIds.Length);
        var seen = new HashSet<int>();
        foreach (var entityId in entityIds)
        {
            if (entityId <= 0 || !seen.Add(entityId))
            {
                continue;
            }

            refs.Add(BuildEntityRef(entityId));
        }

        return refs;
    }

    private UiChronicleEntityRef BuildEntityRef(int entityId)
    {
        if (_entityInfo.TryGetValue(entityId, out var info))
        {
            var name = _symbols?.ResolveEntityName(
                new ProjectionEntityNameKey(entityId, info.KindId, info.SubtypeId, info.CultureId, entityId),
                $"E{entityId}") ?? $"E{entityId}";
            return new UiChronicleEntityRef(entityId, info.KindId, info.SubtypeId, info.CultureId, name);
        }

        return new UiChronicleEntityRef(
            entityId,
            0,
            0,
            0,
            _symbols?.ResolveEntityName(
                new ProjectionEntityNameKey(entityId, 0, 0, 0, entityId),
                $"E{entityId}") ?? $"E{entityId}");
    }

    private static string TickPrefix(int tick) =>
        $"[color=#{TickColor}]t{tick,4}[/color]  ";

    private void Append(UiChronicleEntry entry)
    {
        _entries.Add(entry);
        _lines.Add(entry.Text);
        Version++;
        if (_entries.Count > MaxEntries)
        {
            _entries.RemoveRange(0, _entries.Count - MaxEntries);
            _lines.RemoveRange(0, _lines.Count - MaxEntries);
        }
    }

    private string Resolve(ProjectionSymbolKind kind, int id, string fallback) =>
        _symbols?.ResolveDisplayName(kind, id, fallback) ?? fallback;

    private readonly record struct EntityInfo(int KindId, int SubtypeId, int CultureId);
}
