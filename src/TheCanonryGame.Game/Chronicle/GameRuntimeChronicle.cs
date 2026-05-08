namespace TheCanonryGame.Game.Chronicle;

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

    private readonly UiChronicleProjection _baseChronicle;
    private readonly ProvenanceProjection _provenance;
    private readonly IProjectionSymbolCatalog _symbols;
    private readonly GameDomainKnowledge _knowledge;
    private readonly HashSet<int> _emittedChainInvocationIds = [];
    private readonly List<ProvenanceInvocationProjection> _pendingChainCandidates = [];
    private readonly List<string> _chainLines = [];
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

        _cachedLines = CombinedLines();
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
        _chainLines.Add(FormatChainLine(child, parent, trigger));
        _chainVersion++;
        TrimChainLines();
        return true;
    }

    private string FormatChainLine(
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

        return $"{TickPrefix(child.Tick)}[b][color=#{ChainColor}]history chain[/color][/b] " +
            $"[color=#{EntityColor}]{parentName}[/color] [color=#{ConnectorColor}]->[/color] " +
            $"[color=#{EntityColor}]{childName}[/color] [color=#{ConnectorColor}]via[/color] " +
            $"[color=#{EntityColor}]{relationshipName}[/color]";
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

    private void TrimChainLines()
    {
        if (_chainLines.Count > MaxLines)
        {
            _chainLines.RemoveRange(0, _chainLines.Count - MaxLines);
        }
    }

    private IReadOnlyList<string> CombinedLines()
    {
        var totalCount = _baseChronicle.Lines.Count + _chainLines.Count;
        if (totalCount <= MaxLines)
        {
            return _baseChronicle.Lines.Concat(_chainLines).ToArray();
        }

        var skip = totalCount - MaxLines;
        var lines = new List<string>(MaxLines);
        foreach (var line in _baseChronicle.Lines.Concat(_chainLines).Skip(skip))
        {
            lines.Add(line);
        }

        return lines;
    }
}
