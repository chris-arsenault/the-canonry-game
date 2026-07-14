namespace Engine.Projection;

using Engine.Constants;
using Engine.Eventing;

public enum UiChronicleEntryType
{
    None = 0,
    Gambit = 1,
    Generator = 2,
    HistoryChain = 3,
    Tale = 4,
    Relationship = 5,
    BeatStruck = 6,
}

public sealed record UiChronicleEntityRef(
    int EntityId,
    int KindId,
    int SubtypeId,
    int CultureId,
    string DisplayName,
    string ReferenceName);

/// <summary>
/// A chronicle tale: one running or resolved story from the Chronicle
/// Director, with its cast resolved to named, linkable entity refs.
/// </summary>
public sealed record UiChronicleTale(
    int Sequence,
    int ArchetypeId,
    string ArchetypeName,
    int StartTick,
    int LastEventTick,
    int StruckBeats,
    bool Resolved,
    int OutcomeCodeId,
    string OutcomeName,
    IReadOnlyList<UiChronicleEntityRef> Cast);

/// <summary>
/// A fully narrated chronicle for presentation: forged title, outcome
/// sentence, and the beats - authored struck beats and observed cast events -
/// that actually happened.
/// </summary>
public sealed record UiChronicleTaleView(
    UiChronicleTale Tale,
    string Title,
    string OutcomeNarrative,
    IReadOnlyList<UiChronicleEntry> Beats);

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
    private const int MaxBeatsPerTale = 12;
    private const string TickColor = "6F7684";
    private const string GambitColor = "D97742";
    private const string GeneratorColor = "78C9B2";
    private const string FailedColor = "B04646";
    private const string EntityColor = "E7E3D5";
    private const string ConnectorColor = "B8B5A6";
    private const string TaleColor = "C9A2E0";
    private const string RelationshipColor = "D7B968";

    private readonly IProjectionSymbolCatalog? _symbols;
    private readonly int _relationshipCreatedCodeId;
    private readonly int _relationshipCulledCodeId;
    private readonly List<UiChronicleEntry> _entries = [];
    private readonly List<string> _lines = [];
    private readonly Dictionary<int, EntityInfo> _entityInfo = [];
    private readonly List<int> _pendingCreatedIds = [];
    private readonly Dictionary<int, UiChronicleTale> _tales = [];
    private readonly List<UiChronicleTale> _talesOrdered = [];
    private readonly Dictionary<int, List<UiChronicleEntry>> _taleBeats = [];
    private int _pendingTick = -1;

    public UiChronicleProjection(
        IProjectionSymbolCatalog? symbols,
        int relationshipCreatedCodeId = 0,
        int relationshipCulledCodeId = 0)
    {
        _symbols = symbols;
        _relationshipCreatedCodeId = relationshipCreatedCodeId;
        _relationshipCulledCodeId = relationshipCulledCodeId;
    }

    public IReadOnlyList<UiChronicleEntry> Entries => _entries;

    public IReadOnlyList<UiChronicleTale> Tales => _talesOrdered;

    public long TalesVersion { get; private set; }

    /// <summary>
    /// The beats of one chronicle: event entries captured while the tale ran,
    /// durable for the tale's whole life (unlike the rolling event log).
    /// </summary>
    public IReadOnlyList<UiChronicleEntry> BeatsFor(int chronicleSequence) =>
        _taleBeats.GetValueOrDefault(chronicleSequence) ?? (IReadOnlyList<UiChronicleEntry>)[];

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

    protected override void OnChronicleChanged(long sequence, in ChronicleChangedKernelEvent evt)
    {
        var tale = ApplyChronicleEvent(evt);
        if (evt.EventTypeCode == ChronicleEventTypeCodes.BeatStruck)
        {
            AttachBeat(evt.ChronicleSequence, tale, AppendBeatStruckEntry(sequence, evt.Tick, tale, evt.OutcomeCodeId));
            return;
        }

        AppendTaleEntry(sequence, evt.Tick, tale, evt.EventTypeCode);
    }

    private UiChronicleTale ApplyChronicleEvent(in ChronicleChangedKernelEvent evt)
    {
        var archetypeName = Resolve(ProjectionSymbolKind.Generator, evt.ArchetypeId, $"chronicle {evt.ArchetypeId}");
        var cast = EntityRefs(evt.RoleEntityId0, evt.RoleEntityId1, evt.RoleEntityId2, evt.RoleEntityId3);
        var resolved = evt.EventTypeCode == ChronicleEventTypeCodes.Resolved;
        var previous = _tales.GetValueOrDefault(evt.ChronicleSequence);
        var struckBeats = previous?.StruckBeats ?? 0;
        if (evt.EventTypeCode == ChronicleEventTypeCodes.BeatStruck)
        {
            struckBeats++;
        }

        if (cast.Count == 0 && previous is not null)
        {
            cast = new List<UiChronicleEntityRef>(previous.Cast);
        }

        var tale = new UiChronicleTale(
            evt.ChronicleSequence,
            evt.ArchetypeId,
            archetypeName,
            previous?.StartTick ?? evt.Tick,
            evt.Tick,
            struckBeats,
            resolved,
            ResolvedOutcomeCodeId(evt, resolved, previous),
            ResolvedOutcomeName(evt, resolved, previous),
            cast);
        _tales[evt.ChronicleSequence] = tale;
        RebuildTaleOrder();
        return tale;
    }

    private static int ResolvedOutcomeCodeId(in ChronicleChangedKernelEvent evt, bool resolved, UiChronicleTale? previous)
    {
        if (resolved)
        {
            return evt.OutcomeCodeId;
        }

        return previous?.OutcomeCodeId ?? 0;
    }

    private string ResolvedOutcomeName(in ChronicleChangedKernelEvent evt, bool resolved, UiChronicleTale? previous)
    {
        if (resolved && evt.OutcomeCodeId > 0)
        {
            return Resolve(ProjectionSymbolKind.ReasonCode, evt.OutcomeCodeId, $"outcome {evt.OutcomeCodeId}");
        }

        return previous?.OutcomeName ?? string.Empty;
    }

    private UiChronicleEntry AppendBeatStruckEntry(long sequence, int tick, UiChronicleTale tale, int beatCodeId)
    {
        var beatName = Resolve(ProjectionSymbolKind.ReasonCode, beatCodeId, $"beat {beatCodeId}");
        var line = $"{TickPrefix(tick)}[b][color=#{TaleColor}]{beatName}[/color][/b]  " +
            $"[color=#{ConnectorColor}]a beat of[/color] [color=#{EntityColor}]{tale.ArchetypeName}[/color]";
        var entry = new UiChronicleEntry(
            sequence,
            tick,
            UiChronicleEntryType.BeatStruck,
            ProvenanceProducerKindCodes.Generator,
            beatCodeId,
            KernelEventState.Succeeded,
            tale.Cast,
            line);
        Append(entry);
        return entry;
    }

    private void RebuildTaleOrder()
    {
        _talesOrdered.Clear();
        _talesOrdered.AddRange(_tales.Values
            .OrderBy(tale => tale.Resolved ? 1 : 0)
            .ThenByDescending(tale => tale.LastEventTick));
        const int maxResolvedRetained = 48;
        var resolvedSeen = 0;
        for (var i = 0; i < _talesOrdered.Count; i++)
        {
            if (!_talesOrdered[i].Resolved)
            {
                continue;
            }

            resolvedSeen++;
            if (resolvedSeen > maxResolvedRetained)
            {
                _tales.Remove(_talesOrdered[i].Sequence);
                _taleBeats.Remove(_talesOrdered[i].Sequence);
            }
        }

        if (resolvedSeen > maxResolvedRetained)
        {
            _talesOrdered.RemoveRange(
                _talesOrdered.Count - (resolvedSeen - maxResolvedRetained),
                resolvedSeen - maxResolvedRetained);
        }

        TalesVersion++;
    }

    private void AppendTaleEntry(long sequence, int tick, UiChronicleTale tale, int eventTypeCode)
    {
        var ending = tale.OutcomeName.Length > 0 ? $"ends: {tale.OutcomeName}" : "ends";
        var verb = eventTypeCode == ChronicleEventTypeCodes.Cast ? "begins" : ending;
        var castText = string.Join(
            $"[color=#{ConnectorColor}], [/color]",
            tale.Cast.Select(entity => $"[color=#{EntityColor}]{entity.DisplayName}[/color]"));
        var line = $"{TickPrefix(tick)}[b][color=#{TaleColor}]{tale.ArchetypeName}[/color][/b] {verb}  {castText}";
        var entry = new UiChronicleEntry(
            sequence,
            tick,
            UiChronicleEntryType.Tale,
            ProvenanceProducerKindCodes.Generator,
            tale.ArchetypeId,
            KernelEventState.Succeeded,
            tale.Cast,
            line);
        Append(entry);
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

        var entry = new UiChronicleEntry(
            sequence,
            evt.Tick,
            UiChronicleEntryType.Gambit,
            ProvenanceProducerKindCodes.Gambit,
            evt.GambitId,
            evt.State,
            EntityRefs(evt.ActorId, evt.AffectedEntityId0, evt.AffectedEntityId1),
            line);
        Append(entry);
        AttachBeats(entry, evt.ActorId);
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
        var entry = new UiChronicleEntry(
            sequence,
            evt.Tick,
            UiChronicleEntryType.Generator,
            ProvenanceProducerKindCodes.Generator,
            evt.GeneratorId,
            KernelEventState.Succeeded,
            entities,
            line);
        Append(entry);
        AttachBeats(entry, evt.TargetEntityId);
    }

    protected override void OnRelationshipChanged(long sequence, in RelationshipChangedKernelEvent evt)
    {
        var forged = _relationshipCreatedCodeId > 0 && evt.ChangeCodeId == _relationshipCreatedCodeId;
        var severed = _relationshipCulledCodeId > 0 && evt.ChangeCodeId == _relationshipCulledCodeId;
        if (!forged && !severed)
        {
            return;
        }

        UiChronicleEntry? entry = null;
        foreach (var (chronicleSequence, tale) in _tales)
        {
            if (tale.Resolved ||
                !TaleCastContains(tale, evt.SourceEntityId) ||
                !TaleCastContains(tale, evt.TargetEntityId))
            {
                continue;
            }

            entry ??= BuildRelationshipEntry(sequence, evt, forged);
            AttachBeat(chronicleSequence, tale, entry);
        }
    }

    private UiChronicleEntry BuildRelationshipEntry(
        long sequence,
        in RelationshipChangedKernelEvent evt,
        bool forged)
    {
        var kindName = Resolve(
            ProjectionSymbolKind.RelationshipKind,
            evt.RelationshipKindId,
            $"relationship {evt.RelationshipKindId}");
        var source = BuildEntityRef(evt.SourceEntityId);
        var target = BuildEntityRef(evt.TargetEntityId);
        var verb = forged ? "forged" : "severed";
        var line = $"{TickPrefix(evt.Tick)}[b][color=#{RelationshipColor}]{kindName}[/color][/b] {verb}  " +
            $"[color=#{EntityColor}]{source.DisplayName}[/color] [color=#{ConnectorColor}]->[/color] " +
            $"[color=#{EntityColor}]{target.DisplayName}[/color]";
        return new UiChronicleEntry(
            sequence,
            evt.Tick,
            UiChronicleEntryType.Relationship,
            ProvenanceProducerKindCodes.System,
            evt.RelationshipKindId,
            KernelEventState.Succeeded,
            [source, target],
            line);
    }

    private void AttachBeats(UiChronicleEntry entry, int actorEntityId)
    {
        foreach (var (chronicleSequence, tale) in _tales)
        {
            if (tale.Resolved)
            {
                continue;
            }

            if (IsBeatFor(tale, entry, actorEntityId))
            {
                AttachBeat(chronicleSequence, tale, entry);
            }
        }
    }

    /// <summary>
    /// An event is a beat when it plays out between cast members, when a cast
    /// member is the one acting, or when it lands on the tale's protagonist.
    /// </summary>
    private static bool IsBeatFor(UiChronicleTale tale, UiChronicleEntry entry, int actorEntityId)
    {
        var touched = 0;
        var primaryTouched = false;
        var primaryId = tale.Cast.Count > 0 ? tale.Cast[0].EntityId : 0;
        foreach (var member in entry.Entities)
        {
            if (!TaleCastContains(tale, member.EntityId))
            {
                continue;
            }

            touched++;
            primaryTouched |= member.EntityId == primaryId && primaryId > 0;
        }

        if (touched >= 2)
        {
            return true;
        }

        return touched == 1 && (TaleCastContains(tale, actorEntityId) || primaryTouched);
    }

    private void AttachBeat(int chronicleSequence, UiChronicleTale tale, UiChronicleEntry entry)
    {
        if (!_taleBeats.TryGetValue(chronicleSequence, out var beats))
        {
            beats = [];
            _taleBeats[chronicleSequence] = beats;
        }

        if (beats.Count >= MaxBeatsPerTale)
        {
            beats.RemoveAt(OldestMinorBeatIndex(tale, beats));
        }

        beats.Add(entry);
        TalesVersion++;
    }

    /// <summary>
    /// When a tale's beat list is full, prefer evicting the oldest beat that
    /// touched only one cast member; beats between cast members are the story.
    /// </summary>
    private static int OldestMinorBeatIndex(UiChronicleTale tale, List<UiChronicleEntry> beats)
    {
        for (var i = 0; i < beats.Count; i++)
        {
            var touched = 0;
            foreach (var member in beats[i].Entities)
            {
                if (TaleCastContains(tale, member.EntityId))
                {
                    touched++;
                }
            }

            if (touched < 2)
            {
                return i;
            }
        }

        return 0;
    }

    private static bool TaleCastContains(UiChronicleTale tale, int entityId)
    {
        if (entityId <= 0)
        {
            return false;
        }

        foreach (var member in tale.Cast)
        {
            if (member.EntityId == entityId)
            {
                return true;
            }
        }

        return false;
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
        var info = _entityInfo.GetValueOrDefault(entityId, new EntityInfo(0, 0, 0));
        var key = new ProjectionEntityNameKey(entityId, info.KindId, info.SubtypeId, info.CultureId, entityId);
        var name = _symbols?.ResolveEntityName(key, $"E{entityId}") ?? $"E{entityId}";
        var reference = _symbols?.ResolveEntityReferenceName(key, name) ?? name;
        return new UiChronicleEntityRef(entityId, info.KindId, info.SubtypeId, info.CultureId, name, reference);
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
