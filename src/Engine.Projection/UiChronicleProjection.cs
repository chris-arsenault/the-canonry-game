namespace Engine.Projection;

using Engine.Eventing;

public sealed class UiChronicleProjection : KernelEventProjector
{
    private const int MaxLines = 160;
    private const string TickColor = "6F7684";
    private const string GambitColor = "D97742";
    private const string GeneratorColor = "78C9B2";
    private const string EntityColor = "E7E3D5";
    private const string ConnectorColor = "B8B5A6";

    private readonly IProjectionSymbolCatalog? _symbols;
    private readonly List<string> _lines = [];
    private readonly Dictionary<int, EntityInfo> _entityInfo = [];
    private readonly List<int> _pendingCreatedIds = [];
    private int _pendingTick = -1;

    public UiChronicleProjection(IProjectionSymbolCatalog? symbols)
    {
        _symbols = symbols;
    }

    public IReadOnlyList<string> Lines => _lines;

    public long Version { get; private set; }

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
        if ((evt.State & KernelEventState.Succeeded) == KernelEventState.None)
        {
            return;
        }

        var gambit = Resolve(ProjectionSymbolKind.Gambit, evt.GambitId, $"gambit {evt.GambitId}");
        var actor = ResolveEntity(evt.ActorId);
        var target = evt.AffectedEntityCount > 0 && evt.AffectedEntityId0 > 0
            ? ResolveEntity(evt.AffectedEntityId0)
            : null;

        var line = target is null
            ? $"{TickPrefix(evt.Tick)}[b][color=#{GambitColor}]{gambit}[/color][/b]  [color=#{EntityColor}]{actor}[/color]"
            : $"{TickPrefix(evt.Tick)}[b][color=#{GambitColor}]{gambit}[/color][/b]  [color=#{EntityColor}]{actor}[/color] [color=#{ConnectorColor}]→[/color] [color=#{EntityColor}]{target}[/color]";

        Append(line);
    }

    protected override void OnGeneratorFired(long sequence, in GeneratorFiredKernelEvent evt)
    {
        var generator = Resolve(ProjectionSymbolKind.Generator, evt.GeneratorId, $"generator {evt.GeneratorId}");
        var target = evt.TargetEntityId > 0 ? ResolveEntity(evt.TargetEntityId) : null;
        var created = TakeRecentCreations(evt.Tick, evt.CreatedEntityCount);

        var head = target is null
            ? $"{TickPrefix(evt.Tick)}[b][color=#{GeneratorColor}]{generator}[/color][/b]"
            : $"{TickPrefix(evt.Tick)}[b][color=#{GeneratorColor}]{generator}[/color][/b]  on [color=#{EntityColor}]{target}[/color]";

        if (created.Count == 0)
        {
            Append(head);
            return;
        }

        var creations = string.Join(
            $"[color=#{ConnectorColor}], [/color]",
            created.Select(name => $"[color=#{EntityColor}]{name}[/color]"));
        Append($"{head} [color=#{ConnectorColor}]+[/color] {creations}");
    }

    private List<string> TakeRecentCreations(int tick, int count)
    {
        if (count <= 0 || tick != _pendingTick || _pendingCreatedIds.Count == 0)
        {
            return [];
        }

        var take = Math.Min(count, _pendingCreatedIds.Count);
        var startIndex = _pendingCreatedIds.Count - take;
        var names = new List<string>(take);
        for (var i = startIndex; i < _pendingCreatedIds.Count; i++)
        {
            names.Add(ResolveEntity(_pendingCreatedIds[i]));
        }

        _pendingCreatedIds.RemoveRange(startIndex, take);
        return names;
    }

    private static string TickPrefix(int tick) =>
        $"[color=#{TickColor}]t{tick,4}[/color]  ";

    private void Append(string line)
    {
        _lines.Add(line);
        Version++;
        if (_lines.Count > MaxLines)
        {
            _lines.RemoveRange(0, _lines.Count - MaxLines);
        }
    }

    private string Resolve(ProjectionSymbolKind kind, int id, string fallback) =>
        _symbols?.ResolveDisplayName(kind, id, fallback) ?? fallback;

    private string ResolveEntity(int entityId)
    {
        if (_entityInfo.TryGetValue(entityId, out var info))
        {
            return _symbols?.ResolveEntityName(
                new ProjectionEntityNameKey(entityId, info.KindId, info.SubtypeId, info.CultureId, entityId),
                $"E{entityId}") ?? $"E{entityId}";
        }

        return _symbols?.ResolveEntityName(
            new ProjectionEntityNameKey(entityId, 0, 0, 0, entityId),
            $"E{entityId}") ?? $"E{entityId}";
    }

    private readonly record struct EntityInfo(int KindId, int SubtypeId, int CultureId);
}
