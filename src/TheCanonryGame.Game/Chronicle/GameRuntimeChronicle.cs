namespace TheCanonryGame.Game.Chronicle;

using Engine.Constants;
using Engine.Eventing;
using Engine.Projection;

using TheCanonryGame.Game.Domain;
using TheCanonryGame.Game.Runtime;

internal sealed class GameRuntimeChronicle : IGameChronicleFeed
{
    private const int MaxLines = 160;
    private const string TickColor = "6F7684";
    private const string ChainColor = "D7B968";
    private const string EntityColor = "E7E3D5";
    private const string ConnectorColor = "B8B5A6";
    private const string NarrativeColor = "D8D4C4";

    private readonly UiChronicleProjection _baseChronicle;
    private readonly ProvenanceProjection _provenance;
    private readonly IProjectionSymbolCatalog _symbols;
    private readonly GameDomainKnowledge _knowledge;
    private readonly HashSet<int> _emittedChainInvocationIds = [];
    private readonly List<ProvenanceInvocationProjection> _pendingChainCandidates = [];
    private readonly List<UiChronicleEntry> _chainEntries = [];
    private IReadOnlyList<UiChronicleEntry> _cachedEntries = [];
    private IReadOnlyList<string> _cachedLines = [];
    private int _nextInvocationIndex;
    private long _lastBaseChronicleVersion = -1;
    private long _lastProvenanceSequence = -1;
    private long _lastCombinedChainVersion = -1;
    private long _chainVersion;

    internal GameRuntimeChronicle(
        UiChronicleProjection baseChronicle,
        ProvenanceProjection provenance,
        IProjectionSymbolCatalog symbols,
        GameDomainKnowledge knowledge)
    {
        ArgumentNullException.ThrowIfNull(baseChronicle);
        ArgumentNullException.ThrowIfNull(provenance);
        ArgumentNullException.ThrowIfNull(symbols);
        ArgumentNullException.ThrowIfNull(knowledge);

        _baseChronicle = baseChronicle;
        _provenance = provenance;
        _symbols = symbols;
        _knowledge = knowledge;
    }

    public long Version { get; private set; }

    public IReadOnlyList<UiChronicleTale> Tales => _baseChronicle.Tales;

    public long TalesVersion => _baseChronicle.TalesVersion;

    public IReadOnlyList<UiChronicleTaleView> TaleViews
    {
        get
        {
            RefreshTaleViews();
            return _cachedTaleViews;
        }
    }

    private IReadOnlyList<UiChronicleTaleView> _cachedTaleViews = [];
    private long _lastTaleViewsVersion = -1;
    private long _lastTaleViewsEntriesVersion = -1;

    private void RefreshTaleViews()
    {
        Refresh();
        if (_lastTaleViewsVersion == _baseChronicle.TalesVersion && _lastTaleViewsEntriesVersion == Version)
        {
            return;
        }

        _lastTaleViewsVersion = _baseChronicle.TalesVersion;
        _lastTaleViewsEntriesVersion = Version;
        _cachedTaleViews = _baseChronicle.Tales
            .Select(BuildTaleView)
            .ToList();
    }

    private UiChronicleTaleView BuildTaleView(UiChronicleTale tale)
    {
        var archetype = _knowledge.ChronicleArchetypesById.GetValueOrDefault(tale.ArchetypeId);
        var title = FillRoleTemplate(archetype?.TitleTemplate, archetype, tale, useReferenceNames: true)
            ?? tale.ArchetypeName;
        return new UiChronicleTaleView(
            tale,
            title,
            OutcomeNarrative(tale, archetype),
            _baseChronicle.BeatsFor(tale.Sequence)
                .Select(beat => NarrateBeat(beat, tale, archetype))
                .ToList());
    }

    /// <summary>
    /// Struck beats carry authored narrative; when the archetype knows the
    /// beat's sentence, the journal shows the telling instead of the raw log
    /// line. Observed cast events keep their log form.
    /// </summary>
    private static UiChronicleEntry NarrateBeat(
        UiChronicleEntry beat,
        UiChronicleTale tale,
        GameChronicleArchetypeKnowledge? archetype)
    {
        if (beat.EntryType != UiChronicleEntryType.BeatStruck ||
            archetype is null ||
            !archetype.BeatNarrativesByCodeId.TryGetValue(beat.ProducerId, out var template))
        {
            return beat;
        }

        var narrative = FillRoleTemplate(template, archetype, tale, useReferenceNames: false);
        if (narrative is null)
        {
            return beat;
        }

        var text = $"[color=#{TickColor}]t{beat.Tick,4}[/color]  [i][color=#{NarrativeColor}]{narrative}[/color][/i]";
        return beat with { Text = text };
    }

    private static string OutcomeNarrative(UiChronicleTale tale, GameChronicleArchetypeKnowledge? archetype)
    {
        if (!tale.Resolved)
        {
            return string.Empty;
        }

        if (archetype is not null &&
            archetype.OutcomeNarrativesByCodeId.TryGetValue(tale.OutcomeCodeId, out var template))
        {
            return FillRoleTemplate(template, archetype, tale, useReferenceNames: false) ?? tale.OutcomeName;
        }

        return tale.OutcomeName;
    }

    private static string? FillRoleTemplate(
        string? template,
        GameChronicleArchetypeKnowledge? archetype,
        UiChronicleTale tale,
        bool useReferenceNames)
    {
        if (string.IsNullOrWhiteSpace(template) || archetype is null)
        {
            return null;
        }

        var result = template;
        for (var role = 0; role < archetype.RoleRefs.Count && role < tale.RoleIds.Count; role++)
        {
            var roleId = tale.RoleIds[role];
            if (roleId <= 0)
            {
                continue;
            }

            var member = tale.Cast.FirstOrDefault(candidate => candidate.EntityId == roleId);
            if (member is null)
            {
                continue;
            }

            var name = useReferenceNames ? member.ReferenceName : member.DisplayName;
            result = result.Replace($"{{{archetype.RoleRefs[role]}}}", name, StringComparison.Ordinal);
        }

        return result;
    }

    public IReadOnlyList<UiChronicleEntry> Entries
    {
        get
        {
            Refresh();
            return _cachedEntries;
        }
    }

    public IReadOnlyList<string> Lines
    {
        get
        {
            Refresh();
            return _cachedLines;
        }
    }

    public void Refresh()
    {
        var provenanceChanged = _lastProvenanceSequence != _provenance.LastProjectedSequence;
        if (provenanceChanged)
        {
            RefreshChains();
            _lastProvenanceSequence = _provenance.LastProjectedSequence;
        }

        if (_lastBaseChronicleVersion == _baseChronicle.Version &&
            _lastCombinedChainVersion == _chainVersion)
        {
            return;
        }

        _cachedEntries = CombinedEntries();
        _cachedLines = _cachedEntries.Select(static entry => entry.Text).ToArray();
        _lastBaseChronicleVersion = _baseChronicle.Version;
        _lastCombinedChainVersion = _chainVersion;
        Version++;
    }

    private void RefreshChains()
    {
        if (_knowledge.ChronicleQueries.Count == 0)
        {
            _nextInvocationIndex = _provenance.InvocationsInProjectionOrder.Count;
            return;
        }

        EnqueueNewChainCandidates();
        ProcessPendingChainCandidates();
    }

    private void EnqueueNewChainCandidates()
    {
        var invocations = _provenance.InvocationsInProjectionOrder;
        for (; _nextInvocationIndex < invocations.Count; _nextInvocationIndex++)
        {
            var invocation = invocations[_nextInvocationIndex];
            if (IsChainChildCandidate(invocation))
            {
                _pendingChainCandidates.Add(invocation);
            }
        }
    }

    private void ProcessPendingChainCandidates()
    {
        for (var i = 0; i < _pendingChainCandidates.Count;)
        {
            if (TryAppendChain(_pendingChainCandidates[i]))
            {
                _pendingChainCandidates.RemoveAt(i);
                continue;
            }

            i++;
        }
    }

    private bool IsChainChildCandidate(ProvenanceInvocationProjection invocation) =>
        _knowledge.ChronicleQueries.Any(query =>
            MatchesProducer(invocation, query.ChildProducerKindCode, query.ChildProducerId));

    private bool TryAppendChain(ProvenanceInvocationProjection child)
    {
        if (_emittedChainInvocationIds.Contains(child.InvocationId))
        {
            return true;
        }

        foreach (var query in _knowledge.ChronicleQueries)
        {
            if (TryAppendChain(query, child))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryAppendChain(
        GameChronicleQuery query,
        ProvenanceInvocationProjection child)
    {
        if (!MatchesProducer(child, query.ChildProducerKindCode, query.ChildProducerId) ||
            !_provenance.TryGetParentInvocation(child.InvocationId, out var parent) ||
            !MatchesProducer(parent, query.ParentProducerKindCode, query.ParentProducerId) ||
            !_provenance.TryGetRelationshipTrigger(child.InvocationId, out var trigger) ||
            trigger.CreatedByInvocationId != parent.InvocationId ||
            trigger.RelationshipKindId != query.TriggerRelationshipKindId)
        {
            return false;
        }

        _emittedChainInvocationIds.Add(child.InvocationId);
        _chainEntries.Add(FormatChainEntry(child, parent, trigger));
        _chainVersion++;
        TrimChainEntries();
        return true;
    }

    private UiChronicleEntry FormatChainEntry(
        ProvenanceInvocationProjection child,
        ProvenanceInvocationProjection parent,
        ProvenanceRelationshipTriggerProjection trigger)
    {
        var parentName = ResolveProducer(parent.ProducerKindCode, parent.ProducerId);
        var childName = ResolveProducer(child.ProducerKindCode, child.ProducerId);
        var relationshipName = _symbols.ResolveDisplayName(
            ProjectionSymbolKind.RelationshipKind,
            trigger.RelationshipKindId,
            $"relationship {trigger.RelationshipKindId}");

        var text = $"{TickPrefix(child.Tick)}[b][color=#{ChainColor}]history chain[/color][/b] " +
            $"[color=#{EntityColor}]{parentName}[/color] [color=#{ConnectorColor}]->[/color] " +
            $"[color=#{EntityColor}]{childName}[/color] [color=#{ConnectorColor}]via[/color] " +
            $"[color=#{EntityColor}]{relationshipName}[/color]";
        return new UiChronicleEntry(
            child.Sequence,
            child.Tick,
            UiChronicleEntryType.HistoryChain,
            child.ProducerKindCode,
            child.ProducerId,
            KernelEventState.Succeeded,
            EntityRefs(trigger.SourceEntityId, trigger.TargetEntityId),
            text);
    }

    private static bool MatchesProducer(
        ProvenanceInvocationProjection invocation,
        int producerKindCode,
        int producerId) =>
        invocation.ProducerKindCode == producerKindCode &&
        invocation.ProducerId == producerId;

    private string ResolveProducer(int producerKindCode, int producerId)
    {
        var symbolKind = producerKindCode switch
        {
            ProvenanceProducerKindCodes.Gambit => ProjectionSymbolKind.Gambit,
            ProvenanceProducerKindCodes.Generator => ProjectionSymbolKind.Generator,
            _ => ProjectionSymbolKind.None,
        };

        return symbolKind == ProjectionSymbolKind.None
            ? $"producer {producerKindCode}:{producerId}"
            : _symbols.ResolveDisplayName(symbolKind, producerId, $"{symbolKind} {producerId}");
    }

    private static string TickPrefix(int tick) =>
        $"[color=#{TickColor}]t{tick,4}[/color]  ";

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

            refs.Add(_baseChronicle.EntityRef(entityId));
        }

        return refs;
    }

    private void TrimChainEntries()
    {
        if (_chainEntries.Count > MaxLines)
        {
            _chainEntries.RemoveRange(0, _chainEntries.Count - MaxLines);
        }
    }

    private IReadOnlyList<UiChronicleEntry> CombinedEntries()
    {
        var totalCount = _baseChronicle.Entries.Count + _chainEntries.Count;
        if (totalCount <= MaxLines)
        {
            return _baseChronicle.Entries.Concat(_chainEntries).ToArray();
        }

        var skip = totalCount - MaxLines;
        var entries = new List<UiChronicleEntry>(MaxLines);
        foreach (var entry in _baseChronicle.Entries.Concat(_chainEntries).Skip(skip))
        {
            entries.Add(entry);
        }

        return entries;
    }
}
